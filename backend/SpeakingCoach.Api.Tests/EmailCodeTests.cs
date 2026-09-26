using SpeakingCoach.Api.Data;
using SpeakingCoach.Api.Services;

namespace SpeakingCoach.Api.Tests;

public class EmailCodeTests
{
    private static readonly DateTime T = new(2026, 9, 25, 10, 0, 0, DateTimeKind.Utc);
    private static readonly Guid User = Guid.Parse("11111111-2222-3333-4444-555555555555");

    private static EmailCode Code(string code, DateTime? created = null, int attempts = 0, DateTime? used = null)
    {
        var at = created ?? T;
        return new EmailCode
        {
            UserId = User,
            CodeHash = EmailCodeRules.Hash(User, code),
            CreatedAtUtc = at,
            ExpiresAtUtc = at.Add(EmailCodeRules.Lifetime),
            Attempts = attempts,
            UsedAtUtc = used,
        };
    }

    [Fact]
    public void Generated_codes_are_six_digits()
    {
        for (var i = 0; i < 200; i++)
        {
            var code = EmailCodeRules.Generate();
            Assert.Equal(6, code.Length);
            Assert.True(code.All(char.IsAsciiDigit));
        }
    }

    [Fact]
    public void Same_code_hashes_differently_for_different_users()
    {
        Assert.NotEqual(EmailCodeRules.Hash(User, "123456"), EmailCodeRules.Hash(Guid.NewGuid(), "123456"));
        Assert.Equal(64, EmailCodeRules.Hash(User, "123456").Length);
    }

    [Theory]
    [InlineData("123456", "123456")]
    [InlineData(" 123 456 ", "123456")]
    [InlineData("123-456", "123456")]
    [InlineData(null, "")]
    public void Code_input_is_normalized_to_digits(string? input, string expected)
    {
        Assert.Equal(expected, EmailCodeRules.Normalize(input));
    }

    [Fact]
    public void Correct_code_passes_even_with_spaces()
    {
        Assert.Equal(CodeCheck.Ok, EmailCodeRules.Check(Code("042917"), User, "042 917", T.AddMinutes(5)));
    }

    [Fact]
    public void Wrong_code_is_rejected()
    {
        Assert.Equal(CodeCheck.Wrong, EmailCodeRules.Check(Code("042917"), User, "042918", T));
        Assert.Equal(CodeCheck.Wrong, EmailCodeRules.Check(Code("042917"), User, "4291", T));
    }

    [Fact]
    public void Code_of_another_user_does_not_work()
    {
        Assert.Equal(CodeCheck.Wrong, EmailCodeRules.Check(Code("042917"), Guid.NewGuid(), "042917", T));
    }

    [Fact]
    public void Expired_code_is_rejected_even_if_correct()
    {
        Assert.Equal(CodeCheck.Expired, EmailCodeRules.Check(Code("042917"), User, "042917", T.Add(EmailCodeRules.Lifetime)));
    }

    [Fact]
    public void After_max_attempts_even_the_right_code_is_rejected()
    {
        var code = Code("042917", attempts: EmailCodeRules.MaxAttempts);
        Assert.Equal(CodeCheck.TooManyAttempts, EmailCodeRules.Check(code, User, "042917", T));
    }

    [Fact]
    public void Used_or_missing_code_is_not_found()
    {
        Assert.Equal(CodeCheck.NotFound, EmailCodeRules.Check(null, User, "042917", T));
        Assert.Equal(CodeCheck.NotFound, EmailCodeRules.Check(Code("042917", used: T), User, "042917", T));
    }

    [Fact]
    public void First_code_can_always_be_sent()
    {
        Assert.True(EmailCodeRules.CanSend(Array.Empty<DateTime>(), T, out _));
    }

    [Fact]
    public void Cooldown_between_emails()
    {
        Assert.False(EmailCodeRules.CanSend(new[] { T.AddSeconds(-20) }, T, out var wait));
        Assert.Equal(40, (int)wait.TotalSeconds);
        Assert.True(EmailCodeRules.CanSend(new[] { T.AddSeconds(-61) }, T, out _));
    }

    [Fact]
    public void At_most_five_emails_per_hour()
    {
        var sends = Enumerable.Range(1, 5).Select(i => T.AddMinutes(-10 * i)).ToArray(); // -10 … -50 daqiqa
        Assert.False(EmailCodeRules.CanSend(sends, T, out var wait));
        Assert.Equal(10, (int)Math.Round(wait.TotalMinutes)); // eng eskisi (-50) bir soatdan keyin chiqadi
        Assert.True(EmailCodeRules.CanSend(sends.Skip(1).ToArray(), T, out _));
    }

    [Fact]
    public void Each_failure_maps_to_a_message()
    {
        foreach (var r in new[] { CodeCheck.Wrong, CodeCheck.Expired, CodeCheck.TooManyAttempts, CodeCheck.NotFound })
        {
            var key = EmailCodeRules.KeyFor(r);
            Assert.NotEqual(key, Texts.Get("en", key)); // kalit Texts'da bor
        }
    }

    [Theory]
    [InlineData("uz", "tasdiqlash kodi")]
    [InlineData("ru", "код подтверждения")]
    [InlineData("en", "verification code")]
    public void Verification_email_in_three_languages(string lang, string subjectPart)
    {
        var m = EmailTemplates.Code(lang, EmailCodePurpose.Verify, "042917");

        Assert.StartsWith("042917", m.Subject);
        Assert.Contains(subjectPart, m.Subject);
        Assert.Contains("042917", m.Text);
        Assert.Contains("042917", m.Html);
        Assert.Contains("15", m.Text);
    }

    [Fact]
    public void Reset_email_differs_from_verification_email()
    {
        var verify = EmailTemplates.Code("en", EmailCodePurpose.Verify, "111111");
        var reset = EmailTemplates.Code("en", EmailCodePurpose.ResetPassword, "111111");

        Assert.NotEqual(verify.Subject, reset.Subject);
        Assert.Contains("reset", reset.Subject);
    }
}
