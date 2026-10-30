using FreshDay.LeadBot.Leads;

namespace FreshDay.LeadBot.Tests;

public class LeadFormatterTests
{
    private static readonly DateOnly Today = new(2026, 10, 30); // пятница

    [Theory]
    [InlineData("+375296866692", "+375 (29) 686-66-92")]
    [InlineData("+375 (29) 686-66-92", "+375 (29) 686-66-92")]
    [InlineData("8 029 686 66 92", "+375 (29) 686-66-92")]
    [InlineData("29 686 66 92", "+375 (29) 686-66-92")]
    [InlineData("+7 916 123-45-67", "+7 916 123-45-67")]
    public void Phone_is_normalized(string raw, string expected) =>
        Assert.Equal(expected, LeadFormatter.FormatPhone(raw));

    [Theory]
    [InlineData(2026, 10, 30, "30.10.2026, пт (сегодня)")]
    [InlineData(2026, 10, 31, "31.10.2026, сб (завтра)")]
    [InlineData(2026, 12, 12, "12.12.2026, сб (через 43 дн.)")]
    [InlineData(2026, 10, 1, "01.10.2026, чт (дата уже прошла)")]
    public void Date_shows_weekday_and_days_left(int y, int m, int d, string expected) =>
        Assert.Equal(expected, LeadFormatter.FormatDate(new DateOnly(y, m, d), Today));

    [Fact]
    public void Message_contains_all_fields_and_escapes_html()
    {
        var lead = new Lead("<b>Оля</b> & Ко", "+375296866692", "Фуршет", 40,
            new DateOnly(2026, 12, 12), "Без <script>", "/FreshDay/uslugi/furshet/");

        var html = LeadFormatter.ToTelegramHtml(lead, Today, "https://mikolaminski.github.io/FreshDay");

        Assert.Contains("👤 Имя: <b>&lt;b&gt;Оля&lt;/b&gt; &amp; Ко</b>", html);
        Assert.Contains("📞 Телефон: +375 (29) 686-66-92", html);
        Assert.Contains("🎉 Формат: Фуршет", html);
        Assert.Contains("👥 Гостей: 40", html);
        Assert.Contains("📅 Дата: 12.12.2026, сб (через 43 дн.)", html);
        Assert.Contains("<blockquote>Без &lt;script&gt;</blockquote>", html);
        Assert.Contains("<a href=\"https://mikolaminski.github.io/FreshDay/uslugi/furshet/\">", html);
        Assert.DoesNotContain("<script>", html);
    }

    [Fact]
    public void Empty_optional_fields_are_omitted()
    {
        var html = LeadFormatter.ToTelegramHtml(
            new Lead("Ян", "+375296866692", null, null, null, null, null), Today, null);

        Assert.DoesNotContain("Формат", html);
        Assert.DoesNotContain("Гостей", html);
        Assert.DoesNotContain("Дата", html);
        Assert.DoesNotContain("Комментарий", html);
        Assert.DoesNotContain("страницы", html);
    }
}
