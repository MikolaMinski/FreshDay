using System.Collections.Concurrent;
using Microsoft.Extensions.Options;

namespace FreshDay.LeadBot.Telegram;

/// <summary>Куда-то доставляет готовое сообщение о заявке. Возвращает true, если дошло хотя бы в один чат.</summary>
public interface ILeadNotifier
{
    bool IsConfigured { get; }
    Task<bool> NotifyAsync(string html, CancellationToken ct);
}

public sealed class TelegramLeadNotifier(
    TelegramBotClient bot,
    IOptionsMonitor<TelegramOptions> options,
    TimeProvider clock,
    ILogger<TelegramLeadNotifier> log) : ILeadNotifier
{
    private const int MaxAttempts = 3;
    private static readonly TimeSpan MaxRetryAfter = TimeSpan.FromSeconds(15);

    /// <summary>Группа стала супергруппой и сменила ID — запоминаем новый до перезапуска.</summary>
    private readonly ConcurrentDictionary<string, string> _migrated = new();

    public bool IsConfigured => options.CurrentValue.Chats.Any(c => !string.IsNullOrWhiteSpace(c.Id));

    public async Task<bool> NotifyAsync(string html, CancellationToken ct)
    {
        var delivered = false;
        foreach (var chat in options.CurrentValue.Chats.Where(c => !string.IsNullOrWhiteSpace(c.Id)))
            delivered |= await SendToChatAsync(chat, html, ct);
        return delivered;
    }

    private async Task<bool> SendToChatAsync(TelegramChat chat, string html, CancellationToken ct)
    {
        var chatId = _migrated.GetValueOrDefault(chat.Id.Trim(), chat.Id.Trim());

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            TgResponse<TgMessage> res;
            try
            {
                res = await bot.SendMessageAsync(chatId, html, chat.ThreadId, ct);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
            {
                log.LogWarning("Telegram недоступен (попытка {Attempt}/{Max}): {Error}", attempt, MaxAttempts, ex.Message);
                await DelayAsync(TimeSpan.FromSeconds(attempt * 2), ct);
                continue;
            }

            if (res.Ok) return true;

            if (res.Parameters?.MigrateToChatId is { } newId)
            {
                log.LogWarning(
                    "Группа {Old} стала супергруппой, новый ID {New}. Замените Telegram:Chats Id в настройках.",
                    chat.Id, newId);
                chatId = newId.ToString();
                _migrated[chat.Id.Trim()] = chatId;
                continue;
            }

            if (res.Parameters?.RetryAfter is { } wait)
            {
                await DelayAsync(TimeSpan.FromSeconds(wait) < MaxRetryAfter ? TimeSpan.FromSeconds(wait) : MaxRetryAfter, ct);
                continue;
            }

            if (res.ErrorCode is >= 500 or null)
            {
                log.LogWarning("Telegram ответил ошибкой {Code} (попытка {Attempt}/{Max}): {Description}",
                    res.ErrorCode, attempt, MaxAttempts, res.Description);
                await DelayAsync(TimeSpan.FromSeconds(attempt * 2), ct);
                continue;
            }

            // 400/401/403: неверный ID, бота удалили из группы, неверный токен — повтор не поможет.
            log.LogError("Не удалось отправить заявку в чат {Chat}: {Code} {Description}",
                chatId, res.ErrorCode, res.Description);
            return false;
        }

        log.LogError("Не удалось отправить заявку в чат {Chat} после {Max} попыток", chatId, MaxAttempts);
        return false;
    }

    private Task DelayAsync(TimeSpan delay, CancellationToken ct) => Task.Delay(delay, clock, ct);
}
