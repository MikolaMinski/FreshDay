using System.Net;
using System.Net.Http.Json;
using FreshDay.LeadBot.Telegram;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FreshDay.LeadBot.Tests;

public sealed class FakeNotifier : ILeadNotifier
{
    public List<string> Sent { get; } = [];
    public bool Configured { get; set; } = true;
    public bool Succeeds { get; set; } = true;

    public bool IsConfigured => Configured;

    public Task<bool> NotifyAsync(string html, CancellationToken ct)
    {
        lock (Sent) Sent.Add(html);
        return Task.FromResult(Succeeds);
    }
}

public class LeadEndpointTests : IDisposable
{
    private const string Site = "https://example.github.io/FreshDay";
    private const string Origin = "https://example.github.io";

    private readonly FakeNotifier _notifier = new();
    private readonly WebApplicationFactory<Program> _factory;

    public LeadEndpointTests()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(web =>
        {
            web.UseEnvironment("Testing");
            web.UseSetting("Telegram:BotToken", "123456789:AAHtesttesttesttesttesttesttesttest");
            web.UseSetting("Telegram:ChatIdHelper", "false");
            web.UseSetting("LeadBot:SiteUrl", Site);
            web.UseSetting("LeadBot:AllowedOrigins:0", Origin);
            web.UseSetting("LeadBot:ArchivePath", "");
            web.UseSetting("LeadBot:RequestsPerMinutePerIp", "100");
            web.ConfigureTestServices(services =>
            {
                services.RemoveAll<ILeadNotifier>();
                services.AddSingleton<ILeadNotifier>(_notifier);
                // Не ходим в настоящий Telegram из тестов.
                var helper = services.Single(d => d.ImplementationType == typeof(ChatIdHelperService));
                services.Remove(helper);
            });
        });
    }

    public void Dispose() => _factory.Dispose();

    private static MultipartFormDataContent ValidForm(params (string Key, string Value)[] extra)
    {
        var form = new MultipartFormDataContent
        {
            { new StringContent("Ольга"), "name" },
            { new StringContent("+375 (29) 686-66-92"), "phone" },
            { new StringContent("Фуршет"), "format" },
            { new StringContent("40"), "guests" },
            { new StringContent("yes"), "consent" },
            { new StringContent("/FreshDay/"), "page" },
        };
        foreach (var (k, v) in extra) form.Add(new StringContent(v), k);
        return form;
    }

    private HttpClient Client() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    private static HttpRequestMessage Post(HttpContent content, bool json = true)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/lead") { Content = content };
        if (json) req.Headers.Accept.ParseAdd("application/json");
        req.Headers.Add("Origin", Origin);
        return req;
    }

    private sealed record Reply(bool Ok, string? Error);

    [Fact]
    public async Task Valid_lead_is_sent_to_telegram()
    {
        var res = await Client().SendAsync(Post(ValidForm()));

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.True((await res.Content.ReadFromJsonAsync<Reply>())!.Ok);
        Assert.Equal(Origin, res.Headers.GetValues("Access-Control-Allow-Origin").Single());
        var message = Assert.Single(_notifier.Sent);
        Assert.Contains("Ольга", message);
        Assert.Contains($"{Site}/", message);
    }

    [Fact]
    public async Task Json_body_is_accepted()
    {
        var res = await Client().PostAsJsonAsync("/api/lead",
            new { name = "Ян", phone = "+375296866692", consent = "yes", guests = 15 });

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Contains("Гостей: 15", Assert.Single(_notifier.Sent));
    }

    [Fact]
    public async Task Form_without_javascript_is_redirected_to_thank_you_page()
    {
        var res = await Client().SendAsync(Post(ValidForm(), json: false));

        Assert.Equal(HttpStatusCode.SeeOther, res.StatusCode);
        Assert.Equal($"{Site}/spasibo/", res.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Invalid_lead_returns_errors_and_is_not_sent()
    {
        var form = new MultipartFormDataContent { { new StringContent("Оля"), "name" } };

        var res = await Client().SendAsync(Post(form));

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        var reply = await res.Content.ReadFromJsonAsync<Reply>();
        Assert.Contains("invalid_phone", reply!.Error);
        Assert.Contains("consent_required", reply.Error);
        Assert.Empty(_notifier.Sent);
    }

    [Fact]
    public async Task Invalid_lead_without_javascript_goes_back_to_contacts()
    {
        var res = await Client().SendAsync(Post(new MultipartFormDataContent(), json: false));

        Assert.Equal(HttpStatusCode.SeeOther, res.StatusCode);
        Assert.Equal($"{Site}/kontakty/?error=1", res.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Spam_gets_fake_success_and_is_not_sent()
    {
        var res = await Client().SendAsync(Post(ValidForm(("website", "http://spam.example"))));

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Empty(_notifier.Sent);
    }

    [Fact]
    public async Task Telegram_failure_is_reported_to_visitor()
    {
        _notifier.Succeeds = false;

        var res = await Client().SendAsync(Post(ValidForm()));

        Assert.Equal(HttpStatusCode.BadGateway, res.StatusCode);
        Assert.Equal("delivery_failed", (await res.Content.ReadFromJsonAsync<Reply>())!.Error);
    }

    [Fact]
    public async Task Missing_chat_config_returns_503()
    {
        _notifier.Configured = false;

        var res = await Client().SendAsync(Post(ValidForm()));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, res.StatusCode);
        Assert.Empty(_notifier.Sent);
    }

    [Fact]
    public async Task Foreign_origin_gets_no_cors_header()
    {
        var req = Post(ValidForm());
        req.Headers.Remove("Origin");
        req.Headers.Add("Origin", "https://evil.example");

        var res = await Client().SendAsync(req);

        Assert.False(res.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task Health_endpoint_works()
    {
        var res = await Client().GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }
}

public class StartupValidationTests
{
    [Fact]
    public void Missing_token_stops_the_service_with_clear_message()
    {
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(web =>
        {
            web.UseEnvironment("Testing");
            web.UseSetting("Telegram:BotToken", "");
        });

        var ex = Assert.ThrowsAny<Exception>(() => factory.CreateClient());
        Assert.Contains("BotToken", ex.ToString());
    }
}
