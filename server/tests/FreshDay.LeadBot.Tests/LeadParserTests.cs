using FreshDay.LeadBot.Leads;

namespace FreshDay.LeadBot.Tests;

public class LeadParserTests
{
    private static Func<string, string?> Form(params (string Key, string Value)[] fields)
    {
        var dict = fields.ToDictionary(f => f.Key, f => f.Value);
        return k => dict.GetValueOrDefault(k);
    }

    private static readonly (string, string)[] Valid =
    [
        ("name", "  Ольга "), ("phone", "+375 (29) 686-66-92"), ("consent", "yes"),
        ("format", "Фуршет"), ("guests", "40"), ("date", "2026-12-12"),
        ("message", "Офис на Немиге\r\n\r\n\r\n\r\nбез орехов"), ("page", "/FreshDay/uslugi/furshet/"),
    ];

    [Fact]
    public void Valid_form_is_parsed_and_cleaned()
    {
        var result = LeadParser.Parse(Form(Valid));

        Assert.True(result.IsValid);
        var lead = result.Lead!;
        Assert.Equal("Ольга", lead.Name);
        Assert.Equal("+375 (29) 686-66-92", lead.Phone);
        Assert.Equal("Фуршет", lead.Format);
        Assert.Equal(40, lead.Guests);
        Assert.Equal(new DateOnly(2026, 12, 12), lead.Date);
        Assert.Equal("Офис на Немиге\n\nбез орехов", lead.Comment);
        Assert.Equal("/FreshDay/uslugi/furshet/", lead.Page);
    }

    [Fact]
    public void Only_required_fields_are_enough()
    {
        var result = LeadParser.Parse(Form(("name", "Ян"), ("phone", "8029 686 66 92"), ("consent", "on")));

        Assert.True(result.IsValid);
        Assert.Null(result.Lead!.Format);
        Assert.Null(result.Lead.Guests);
        Assert.Null(result.Lead.Date);
        Assert.Null(result.Lead.Comment);
    }

    [Theory]
    [InlineData("name", "", LeadParser.ErrorName)]
    [InlineData("name", "Я", LeadParser.ErrorName)]
    [InlineData("phone", "12345", LeadParser.ErrorPhone)]
    [InlineData("phone", "позвоните мне", LeadParser.ErrorPhone)]
    [InlineData("phone", "+---------------", LeadParser.ErrorPhone)]
    [InlineData("consent", "", LeadParser.ErrorConsent)]
    [InlineData("consent", "no", LeadParser.ErrorConsent)]
    public void Invalid_required_field_is_reported(string key, string value, string error)
    {
        var fields = Valid.Where(f => f.Item1 != key).Append((key, value)).ToArray();

        var result = LeadParser.Parse(Form(fields));

        Assert.False(result.IsValid);
        Assert.Contains(error, result.Errors);
    }

    [Theory]
    [InlineData("guests", "много")]
    [InlineData("guests", "0")]
    [InlineData("guests", "999999")]
    [InlineData("date", "12.12.2026")]
    [InlineData("page", "https://evil.example/")]
    [InlineData("page", "//evil.example/")]
    public void Bad_optional_field_is_dropped_but_lead_is_kept(string key, string value)
    {
        var fields = Valid.Where(f => f.Item1 != key).Append((key, value)).ToArray();

        var result = LeadParser.Parse(Form(fields));

        Assert.True(result.IsValid);
        var lead = result.Lead!;
        Assert.Null(key switch { "guests" => (object?)lead.Guests, "date" => lead.Date, _ => lead.Page });
    }

    [Theory]
    [InlineData("website")]
    [InlineData("botcheck")]
    public void Honeypot_marks_spam(string trap)
    {
        var result = LeadParser.Parse(Form([.. Valid, (trap, "http://spam")]));

        Assert.True(result.IsSpam);
        Assert.Null(result.Lead);
    }

    [Fact]
    public void Long_values_are_truncated_and_control_chars_removed()
    {
        var name = "Ан\u0000на" + new string('я', 200);
        var result = LeadParser.Parse(Form([.. Valid.Where(f => f.Item1 != "name"), ("name", name)]));

        Assert.True(result.IsValid);
        Assert.Equal(60, result.Lead!.Name.Length);
        Assert.StartsWith("Анна", result.Lead.Name);
    }
}
