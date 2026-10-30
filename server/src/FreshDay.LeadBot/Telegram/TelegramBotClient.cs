using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace FreshDay.LeadBot.Telegram;

/// <summary>
/// Минимальный клиент Telegram Bot API — без сторонних библиотек.
/// Токен входит в адрес запроса, поэтому журнал HttpClient понижен до Warning (см. appsettings.json).
/// </summary>
public sealed class TelegramBotClient(HttpClient http, IOptionsMonitor<TelegramOptions> options)
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public Task<TgResponse<TgMessage>> SendMessageAsync(
        string chatId, string html, int? threadId, CancellationToken ct) =>
        CallAsync<TgMessage>("sendMessage", new SendMessageRequest(
            ChatId: chatId,
            Text: html,
            ParseMode: "HTML",
            MessageThreadId: threadId,
            LinkPreviewOptions: new LinkPreviewOptions(IsDisabled: true)), ct);

    public Task<TgResponse<TgUser>> GetMeAsync(CancellationToken ct) =>
        CallAsync<TgUser>("getMe", new { }, ct);

    public Task<TgResponse<TgUpdate[]>> GetUpdatesAsync(long offset, int timeoutSeconds, CancellationToken ct) =>
        CallAsync<TgUpdate[]>("getUpdates", new
        {
            offset,
            timeout = timeoutSeconds,
            allowed_updates = new[] { "message", "my_chat_member" },
        }, ct);

    public async Task<TgResponse<T>> CallAsync<T>(string method, object payload, CancellationToken ct)
    {
        var token = options.CurrentValue.BotToken;
        using var response = await http.PostAsJsonAsync(
            $"https://api.telegram.org/bot{token}/{method}", payload, Json, ct);

        try
        {
            // Telegram отвечает JSON и при ошибках (400, 403, 429 …).
            var body = await response.Content.ReadFromJsonAsync<TgResponse<T>>(Json, ct);
            if (body is not null) return body;
        }
        catch (JsonException) { }
        catch (NotSupportedException) { } // не JSON (например, HTML-страница шлюза)

        return new TgResponse<T>(false, default, (int)response.StatusCode,
            $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}", null);
    }

    private sealed record SendMessageRequest(
        string ChatId,
        string Text,
        string ParseMode,
        int? MessageThreadId,
        LinkPreviewOptions LinkPreviewOptions);

    private sealed record LinkPreviewOptions(bool IsDisabled);
}

public sealed record TgResponse<T>(
    bool Ok,
    T? Result,
    int? ErrorCode,
    string? Description,
    TgResponseParameters? Parameters);

public sealed record TgResponseParameters(long? MigrateToChatId, int? RetryAfter);

public sealed record TgUser(long Id, string? Username, string FirstName);

public sealed record TgChat(long Id, string Type, string? Title);

public sealed record TgMessage(
    long MessageId,
    TgChat Chat,
    string? Text,
    int? MessageThreadId,
    bool? IsTopicMessage);

public sealed record TgChatMember(string Status);

public sealed record TgChatMemberUpdated(TgChat Chat, TgChatMember NewChatMember);

public sealed record TgUpdate(long UpdateId, TgMessage? Message, TgChatMemberUpdated? MyChatMember);
