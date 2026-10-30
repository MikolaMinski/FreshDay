using System.Text;

namespace FreshDay.LeadBot.Leads;

/// <summary>Оформляет заявку в сообщение Telegram (parse_mode = HTML).</summary>
public static class LeadFormatter
{
    /// <summary>В Беларуси UTC+3 круглый год, без перехода на летнее время.</summary>
    public static readonly TimeSpan MinskOffset = TimeSpan.FromHours(3);

    private static readonly string[] WeekDays = ["вс", "пн", "вт", "ср", "чт", "пт", "сб"];

    public static DateOnly TodayInMinsk(TimeProvider clock) =>
        DateOnly.FromDateTime(clock.GetUtcNow().ToOffset(MinskOffset).DateTime);

    public static string ToTelegramHtml(Lead lead, DateOnly today, string? siteUrl)
    {
        var sb = new StringBuilder();
        sb.AppendLine("🍽 <b>Новая заявка с сайта</b> #заявка");
        sb.AppendLine();
        sb.Append("👤 Имя: <b>").Append(Escape(lead.Name)).AppendLine("</b>");
        sb.Append("📞 Телефон: ").AppendLine(Escape(FormatPhone(lead.Phone)));
        if (lead.Format is not null) sb.Append("🎉 Формат: ").AppendLine(Escape(lead.Format));
        if (lead.Guests is not null) sb.Append("👥 Гостей: ").AppendLine(lead.Guests.Value.ToString());
        if (lead.Date is not null) sb.Append("📅 Дата: ").AppendLine(FormatDate(lead.Date.Value, today));

        if (lead.Comment is not null)
        {
            sb.AppendLine();
            sb.AppendLine("💬 Комментарий:");
            sb.Append("<blockquote>").Append(Escape(lead.Comment)).AppendLine("</blockquote>");
        }

        var pageUrl = PageUrl(siteUrl, lead.Page);
        if (pageUrl is not null)
        {
            sb.AppendLine();
            sb.Append("🔗 Со страницы: <a href=\"").Append(Escape(pageUrl)).Append("\">")
              .Append(Escape(lead.Page!)).AppendLine("</a>");
        }

        return sb.ToString().TrimEnd();
    }

    /// <summary>Приводит белорусский номер к виду +375 (29) 686-66-92. Иной номер оставляет как есть.</summary>
    public static string FormatPhone(string raw)
    {
        var digits = new string(raw.Where(char.IsAsciiDigit).ToArray());

        if (digits.Length == 11 && digits.StartsWith("80")) digits = "375" + digits[2..];   // 8 029 …
        else if (digits.Length == 9 && !raw.TrimStart().StartsWith('+')) digits = "375" + digits; // 29 …

        return digits.Length == 12 && digits.StartsWith("375")
            ? $"+375 ({digits[3..5]}) {digits[5..8]}-{digits[8..10]}-{digits[10..12]}"
            : raw.Trim();
    }

    /// <summary>12.12.2026, сб (через 43 дн.)</summary>
    public static string FormatDate(DateOnly date, DateOnly today)
    {
        var days = date.DayNumber - today.DayNumber;
        var relative = days switch
        {
            0 => "сегодня",
            1 => "завтра",
            > 1 => $"через {days} дн.",
            _ => "дата уже прошла",
        };
        return $"{date:dd.MM.yyyy}, {WeekDays[(int)date.DayOfWeek]} ({relative})";
    }

    internal static string? PageUrl(string? siteUrl, string? page)
    {
        if (page is null || !Uri.TryCreate(siteUrl, UriKind.Absolute, out var site)) return null;
        // Путь страницы уже содержит префикс сайта (например /FreshDay/uslugi/) — берём только домен.
        return Uri.TryCreate(site, page, out var full) && full.Scheme is "https" or "http"
            ? full.ToString()
            : null;
    }

    /// <summary>Telegram требует экранировать в HTML только эти символы.</summary>
    public static string Escape(string text) => text
        .Replace("&", "&amp;")
        .Replace("<", "&lt;")
        .Replace(">", "&gt;")
        .Replace("\"", "&quot;");
}
