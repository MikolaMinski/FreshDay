using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace FreshDay.LeadBot.Leads;

/// <summary>Заявка с сайта после проверки и очистки.</summary>
public sealed record Lead(
    string Name,
    string Phone,
    string? Format,
    int? Guests,
    DateOnly? Date,
    string? Comment,
    string? Page);

public sealed record LeadParseResult(Lead? Lead, IReadOnlyList<string> Errors, bool IsSpam)
{
    public bool IsValid => Lead is not null && Errors.Count == 0;
}

/// <summary>
/// Разбирает поля формы (те же имена, что в src/components/LeadForm.astro).
/// Обязательны только имя, телефон и согласие — остальное необязательно и при ошибке просто
/// отбрасывается, чтобы не потерять заявку из-за второстепенного поля.
/// </summary>
public static partial class LeadParser
{
    public const string ErrorName = "invalid_name";
    public const string ErrorPhone = "invalid_phone";
    public const string ErrorConsent = "consent_required";

    private static readonly string[] ConsentValues = ["yes", "on", "true", "1"];

    public static LeadParseResult Parse(Func<string, string?> field)
    {
        // Ловушки для ботов: люди эти поля не видят и не заполняют.
        if (!string.IsNullOrWhiteSpace(field("website")) || !string.IsNullOrWhiteSpace(field("botcheck")))
            return new LeadParseResult(null, [], IsSpam: true);

        var errors = new List<string>();

        var name = Clean(field("name"), 60);
        if (name is null || name.Length < 2) errors.Add(ErrorName);

        var phone = Clean(field("phone"), 30);
        if (phone is null || !PhoneChars().IsMatch(phone) || phone.Count(char.IsAsciiDigit) < 9)
            errors.Add(ErrorPhone);

        var consent = field("consent")?.Trim().ToLowerInvariant();
        if (consent is null || !ConsentValues.Contains(consent)) errors.Add(ErrorConsent);

        if (errors.Count > 0) return new LeadParseResult(null, errors, IsSpam: false);

        int? guests = int.TryParse(field("guests"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var g)
                      && g is >= 1 and <= 5000 ? g : null;

        DateOnly? date = DateOnly.TryParseExact(field("date")?.Trim(), "yyyy-MM-dd",
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : null;

        var page = Clean(field("page"), 200);
        if (page is not null && (!page.StartsWith('/') || page.StartsWith("//"))) page = null;

        var lead = new Lead(
            Name: name!,
            Phone: phone!,
            Format: Clean(field("format"), 60),
            Guests: guests,
            Date: date,
            Comment: Clean(field("message"), 2000, multiline: true),
            Page: page);

        return new LeadParseResult(lead, [], IsSpam: false);
    }

    /// <summary>Обрезает пробелы, убирает управляющие символы, ограничивает длину. Пустое → null.</summary>
    internal static string? Clean(string? value, int maxLength, bool multiline = false)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        var sb = new StringBuilder(Math.Min(value.Length, maxLength));
        foreach (var ch in value.Replace("\r\n", "\n").Replace('\r', '\n'))
        {
            if (ch == '\n' && multiline) sb.Append(ch);
            else if (ch is '\n' or '\t') sb.Append(' ');
            else if (!char.IsControl(ch)) sb.Append(ch);
        }

        var result = sb.ToString().Trim();
        if (multiline) result = ExtraBlankLines().Replace(result, "\n\n");
        if (result.Length > maxLength) result = result[..maxLength].TrimEnd();
        return result.Length == 0 ? null : result;
    }

    [GeneratedRegex(@"^[+\d\s()\-]{9,20}$")]
    private static partial Regex PhoneChars();

    [GeneratedRegex(@"\n{3,}")]
    private static partial Regex ExtraBlankLines();
}
