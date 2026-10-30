using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace FreshDay.LeadBot.Telegram;

/// <summary>
/// Фоновая служба:
///  - при запуске проверяет токен (getMe) и пишет в журнал имя бота;
///  - если включено Telegram:ChatIdHelper — отвечает на /chatid и сообщает ID группы,
///    когда бота туда добавляют. Так не нужно искать ID вручную через getUpdates.
/// </summary>
public sealed partial class ChatIdHelperService(
    TelegramBotClient bot,
    IOptionsMonitor<TelegramOptions> options,
    ILeadNotifier notifier,
    ILogger<ChatIdHelperService> log) : BackgroundService
{
    private const int PollTimeoutSeconds = 50;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        string? botName = null;
        try
        {
            var me = await bot.GetMeAsync(ct);
            if (me.Ok)
            {
                botName = me.Result!.Username;
                log.LogInformation("Telegram-бот @{Bot} подключён", botName);
            }
            else
            {
                log.LogError("Telegram не принял токен: {Code} {Description}. Проверьте Telegram:BotToken.",
                    me.ErrorCode, me.Description);
            }
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            log.LogWarning("Не удалось связаться с Telegram при запуске: {Error}", ex.Message);
        }

        if (!notifier.IsConfigured)
            log.LogWarning(
                "Не указан чат для заявок (Telegram:Chats). Добавьте бота в группу и отправьте там /chatid@{Bot}",
                botName ?? "имя_бота");

        long offset = 0;
        var conflictLogged = false;

        while (!ct.IsCancellationRequested)
        {
            if (!options.CurrentValue.ChatIdHelper)
            {
                await Task.Delay(TimeSpan.FromMinutes(1), ct);
                continue;
            }

            try
            {
                var res = await bot.GetUpdatesAsync(offset, PollTimeoutSeconds, ct);
                if (!res.Ok)
                {
                    // 409: запущен второй экземпляр сервиса или у бота настроен webhook.
                    if (res.ErrorCode == 409 && !conflictLogged)
                    {
                        log.LogWarning("Помощник /chatid отключён: {Description}", res.Description);
                        conflictLogged = true;
                    }
                    await Task.Delay(TimeSpan.FromSeconds(res.ErrorCode == 409 ? 60 : 10), ct);
                    continue;
                }

                foreach (var update in res.Result ?? [])
                {
                    offset = update.UpdateId + 1;
                    await HandleAsync(update, ct);
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                log.LogWarning("Ошибка опроса Telegram: {Error}", ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(10), ct);
            }
        }
    }

    private async Task HandleAsync(TgUpdate update, CancellationToken ct)
    {
        if (update.MyChatMember is { } member
            && member.Chat.Type is "group" or "supergroup"
            && member.NewChatMember.Status is "member" or "administrator")
        {
            await ReplyAsync(member.Chat, null, ct);
            return;
        }

        if (update.Message is { Text: { } text } msg && ChatIdCommand().IsMatch(text))
            await ReplyAsync(msg.Chat, msg.IsTopicMessage == true ? msg.MessageThreadId : null, ct);
    }

    private async Task ReplyAsync(TgChat chat, int? threadId, CancellationToken ct)
    {
        var id = chat.Id.ToString();
        var already = options.CurrentValue.Chats.Any(c => c.Id.Trim() == id);

        var text = $"""
            👋 Я бот заявок сайта Fresh Day.

            ID этого чата: <code>{id}</code>
            """;
        if (threadId is not null) text += $"\nID темы: <code>{threadId}</code>";
        text += already
            ? "\n\n✅ Заявки с сайта уже приходят сюда."
            : "\n\nЧтобы заявки приходили сюда, укажите этот ID в настройках сервиса " +
              "(<code>Telegram__Chats__0__Id</code>)" +
              (threadId is not null ? " и ID темы (<code>Telegram__Chats__0__ThreadId</code>)" : "") +
              " и перезапустите его.";

        var res = await bot.SendMessageAsync(id, text, threadId, ct);
        if (res.Ok) log.LogInformation("Сообщил ID чата {Chat} «{Title}»", id, chat.Title);
        else log.LogWarning("Не смог ответить в чат {Chat}: {Description}", id, res.Description);
    }

    /// <summary>/chatid, /id или /start, в группе — с упоминанием бота: /chatid@FreshDayBot</summary>
    [GeneratedRegex(@"^/(chatid|id|start)(@\w+)?(\s|$)", RegexOptions.IgnoreCase)]
    private static partial Regex ChatIdCommand();
}
