using System.Text.Json;
using FreshDay.LeadBot.Telegram;
using Microsoft.Extensions.Options;

namespace FreshDay.LeadBot.Leads;

/// <summary>
/// POST /api/lead — принимает форму сайта (multipart/form-data, x-www-form-urlencoded или JSON).
/// С заголовком Accept: application/json отвечает {"ok":true}; без него (форма без JavaScript)
/// перенаправляет на страницу «Спасибо» или обратно на «Контакты» с ошибкой.
/// </summary>
public static class LeadEndpoint
{
    public static async Task<IResult> HandleAsync(
        HttpContext ctx,
        ILeadNotifier notifier,
        LeadArchive archive,
        IOptions<LeadBotOptions> options,
        TimeProvider clock,
        ILogger<Lead> log)
    {
        var req = ctx.Request;
        var ct = ctx.RequestAborted;
        var wantsJson = req.Headers.Accept.ToString().Contains("application/json") || req.HasJsonContentType();
        var site = options.Value.SiteUrl.TrimEnd('/');

        IResult Respond(bool ok, string? error = null, int status = StatusCodes.Status400BadRequest) =>
            wantsJson
                ? Results.Json(new { ok, error }, statusCode: ok ? StatusCodes.Status200OK : status)
                : new SeeOtherResult(ok ? $"{site}/spasibo/" : $"{site}/kontakty/?error=1");

        Func<string, string?> field;
        try
        {
            if (req.HasFormContentType)
            {
                var form = await req.ReadFormAsync(ct);
                field = k => form.TryGetValue(k, out var v) ? v.ToString() : null;
            }
            else if (req.HasJsonContentType())
            {
                var json = await req.ReadFromJsonAsync<Dictionary<string, JsonElement>>(ct) ?? [];
                field = k => json.TryGetValue(k, out var v)
                    ? v.ValueKind == JsonValueKind.String ? v.GetString() : v.ValueKind is JsonValueKind.Null ? null : v.ToString()
                    : null;
            }
            else
            {
                return Respond(false, "unsupported_media_type", StatusCodes.Status415UnsupportedMediaType);
            }
        }
        catch (Exception ex) when (ex is JsonException or InvalidDataException or BadHttpRequestException)
        {
            return Respond(false, "bad_request");
        }

        var parsed = LeadParser.Parse(field);
        if (parsed.IsSpam)
        {
            log.LogInformation("Отброшена заявка-спам (заполнено поле-ловушка)");
            return Respond(true); // боту не сообщаем, что его поймали
        }
        if (!parsed.IsValid) return Respond(false, string.Join(",", parsed.Errors));

        if (!notifier.IsConfigured)
        {
            log.LogError("Заявка не отправлена: не указан Telegram:Chats");
            await archive.AppendAsync(parsed.Lead!, clock.GetUtcNow(), delivered: false, ct);
            return Respond(false, "not_configured", StatusCodes.Status503ServiceUnavailable);
        }

        var lead = parsed.Lead!;
        var receivedAt = clock.GetUtcNow();
        var html = LeadFormatter.ToTelegramHtml(lead, LeadFormatter.TodayInMinsk(clock), site);

        // Заявку доводим до конца, даже если посетитель закрыл страницу.
        var delivered = await notifier.NotifyAsync(html, CancellationToken.None);
        await archive.AppendAsync(lead, receivedAt, delivered, CancellationToken.None);

        // Имя и телефон в журнал не пишем — это персональные данные.
        log.LogInformation("Заявка «{Format}», гостей {Guests}: {Result}",
            lead.Format ?? "—", lead.Guests?.ToString() ?? "—", delivered ? "отправлена в Telegram" : "НЕ доставлена");

        return delivered ? Respond(true) : Respond(false, "delivery_failed", StatusCodes.Status502BadGateway);
    }

    /// <summary>303 See Other: после POST браузер откроет страницу обычным GET.</summary>
    private sealed class SeeOtherResult(string location) : IResult
    {
        public Task ExecuteAsync(HttpContext httpContext)
        {
            httpContext.Response.StatusCode = StatusCodes.Status303SeeOther;
            httpContext.Response.Headers.Location = location;
            return Task.CompletedTask;
        }
    }
}
