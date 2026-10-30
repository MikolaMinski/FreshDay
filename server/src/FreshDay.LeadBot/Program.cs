using System.Threading.RateLimiting;
using FreshDay.LeadBot;
using FreshDay.LeadBot.Leads;
using FreshDay.LeadBot.Telegram;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

builder.Services.AddOptions<TelegramOptions>()
    .Bind(config.GetSection(TelegramOptions.Section))
    .Validate(o => TelegramOptions.TokenPattern().IsMatch(o.BotToken.Trim()),
        "Telegram:BotToken не задан или имеет неверный формат (вид: 123456789:AAH…). " +
        "Получите токен у @BotFather и укажите его в настройках — см. server/README.md.")
    .ValidateOnStart();

builder.Services.AddOptions<LeadBotOptions>()
    .Bind(config.GetSection(LeadBotOptions.Section))
    .Validate(o => Uri.TryCreate(o.SiteUrl, UriKind.Absolute, out _), "LeadBot:SiteUrl должен быть полным адресом сайта")
    .ValidateOnStart();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHttpClient<TelegramBotClient>(c => c.Timeout = TimeSpan.FromSeconds(70));
builder.Services.AddSingleton<ILeadNotifier, TelegramLeadNotifier>();
builder.Services.AddSingleton<LeadArchive>();
builder.Services.AddHostedService<ChatIdHelperService>();

// Настройки ниже читаются из LeadBotOptions при старте приложения (а не при сборке builder),
// поэтому их можно задавать любым способом: appsettings, переменные окружения, user-secrets.
builder.Services.AddCors();
builder.Services.AddOptions<CorsOptions>().Configure<IOptions<LeadBotOptions>>((cors, lead) =>
    cors.AddPolicy("site", p => p
        .WithOrigins(lead.Value.AllowedOrigins.Select(x => x.Trim().TrimEnd('/')).ToArray())
        .WithMethods("POST")
        .WithHeaders("Accept", "Content-Type")));

builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.OnRejected = (ctx, ct) => new ValueTask(ctx.HttpContext.Response.WriteAsJsonAsync(
        new { ok = false, error = "too_many_requests" }, ct));
    o.AddPolicy("lead", ctx =>
    {
        var limit = ctx.RequestServices.GetRequiredService<IOptions<LeadBotOptions>>().Value.RequestsPerMinutePerIp;
        return RateLimitPartition.GetFixedWindowLimiter(
            ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = Math.Max(1, limit),
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            });
    });
});

builder.Services.AddOptions<ForwardedHeadersOptions>().Configure<IOptions<LeadBotOptions>>((o, lead) =>
{
    if (!lead.Value.BehindProxy) return; // по умолчанию заголовки X-Forwarded-* игнорируются
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.ForwardLimit = 1;
    // Прокси (nginx, Caddy) в соседнем Docker-контейнере имеет не loopback-адрес.
    // Сам сервис наружу не открывайте — только через прокси.
    o.KnownIPNetworks.Clear();
    o.KnownProxies.Clear();
});

// Форма маленькая — большие запросы не принимаем.
builder.WebHost.ConfigureKestrel(k => k.Limits.MaxRequestBodySize = 64 * 1024);

var app = builder.Build();

app.UseForwardedHeaders();
app.UseCors();
app.UseRateLimiter();

app.MapGet("/", () => Results.Text("Fresh Day: приём заявок работает. Заявки — POST /api/lead"));
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapPost("/api/lead", LeadEndpoint.HandleAsync)
    .RequireCors("site")
    .RequireRateLimiting("lead")
    .DisableAntiforgery();

app.Run();

/// <summary>Нужен тестам (WebApplicationFactory).</summary>
public partial class Program;
