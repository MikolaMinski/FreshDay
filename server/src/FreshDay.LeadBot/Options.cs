using System.Text.RegularExpressions;

namespace FreshDay.LeadBot;

/// <summary>Настройки приёма заявок (секция "LeadBot" в appsettings.json).</summary>
public sealed class LeadBotOptions
{
    public const string Section = "LeadBot";

    /// <summary>
    /// Адрес сайта вместе с префиксом, например https://freshday.by или https://mikolaminski.github.io/FreshDay.
    /// Нужен для перехода на «Спасибо» без JavaScript и для ссылки на страницу в сообщении.
    /// </summary>
    public string SiteUrl { get; set; } = "https://freshday.by";

    /// <summary>Откуда разрешено отправлять форму (CORS): только схема и домен, без пути.</summary>
    public string[] AllowedOrigins { get; set; } = [];

    /// <summary>Файл-архив заявок (по строке JSON на заявку). Пусто — не сохранять.</summary>
    public string? ArchivePath { get; set; } = "data/leads.jsonl";

    /// <summary>Сколько заявок в минуту принимать с одного IP.</summary>
    public int RequestsPerMinutePerIp { get; set; } = 3;

    /// <summary>true, если сервис стоит за nginx / Caddy / Docker-прокси (берём IP из X-Forwarded-For).</summary>
    public bool BehindProxy { get; set; }
}

/// <summary>Настройки Telegram (секция "Telegram").</summary>
public sealed partial class TelegramOptions
{
    public const string Section = "Telegram";

    /// <summary>Токен бота от @BotFather.</summary>
    public string BotToken { get; set; } = "";

    /// <summary>Куда присылать заявки: группы, каналы или личные чаты.</summary>
    public List<TelegramChat> Chats { get; set; } = [];

    /// <summary>
    /// Бот отвечает на /chatid и здоровается при добавлении в группу, сообщая ID чата.
    /// Удобно при настройке; после можно выключить.
    /// </summary>
    public bool ChatIdHelper { get; set; } = true;

    [GeneratedRegex(@"^\d{5,}:[A-Za-z0-9_-]{30,}$")]
    public static partial Regex TokenPattern();
}

public sealed class TelegramChat
{
    /// <summary>ID чата: у групп отрицательный, например -1001234567890.</summary>
    public string Id { get; set; } = "";

    /// <summary>ID темы, если в группе включены темы (форум). Иначе пусто.</summary>
    public int? ThreadId { get; set; }
}
