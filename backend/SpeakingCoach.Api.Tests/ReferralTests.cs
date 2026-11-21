using Microsoft.Extensions.Configuration;
namespace SpeakingCoach.Api.Tests;

public class ReferralTests
{
    private static readonly DateTime Now = new(2026, 11, 21, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Code_round_trips_and_is_22_url_safe_chars()
    {
        for (var i = 0; i < 50; i++)
        {
            var id = Guid.NewGuid();
            var code = ReferralRules.Encode(id);
            Assert.Equal(22, code.Length);
            Assert.DoesNotContain('+', code);
            Assert.DoesNotContain('/', code);
            Assert.DoesNotContain('=', code);
            Assert.Equal(id, ReferralRules.Decode(code));
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("short")]
    [InlineData("0123456789012345678901234")]   // uzun
    [InlineData("!!!!!!!!!!!!!!!!!!!!!!")]       // noto'g'ri belgilar
    [InlineData("AAAAAAAAAAAAAAAAAAAAA/")]       // '/' base64url emas
    public void Bad_codes_decode_to_null_and_never_throw(string? code)
    {
        Assert.Null(ReferralRules.Decode(code));
    }

    [Fact]
    public void Surrounding_whitespace_is_ignored()
    {
        var id = Guid.NewGuid();
        Assert.Equal(id, ReferralRules.Decode("  " + ReferralRules.Encode(id) + " "));
    }

    [Fact]
    public void Referred_account_gets_the_bonus_only_inside_its_window()
    {
        var created = Now.AddDays(-3);
        Assert.Equal(ReferralRules.BonusExtraExercises, ReferralRules.ExtraExercises(true, created, false, Now));
        // Oyna tugagach (7 kun) — mukofot yo'q.
        Assert.Equal(0, ReferralRules.ExtraExercises(true, Now.AddDays(-7), false, Now));
        Assert.Equal(0, ReferralRules.ExtraExercises(true, Now.AddDays(-30), false, Now));
        // Taklif qilinmagan hisob — mukofot yo'q.
        Assert.Equal(0, ReferralRules.ExtraExercises(false, created, false, Now));
        // Kelajakdagi sana (soat siljishi) — mukofot berilmaydi.
        Assert.Equal(0, ReferralRules.ExtraExercises(true, Now.AddHours(1), false, Now));
    }

    [Fact]
    public void Referrer_gets_a_flat_bonus_that_does_not_stack()
    {
        Assert.Equal(ReferralRules.BonusExtraExercises, ReferralRules.ExtraExercises(false, Now.AddDays(-100), true, Now));
        // Ham taklif qilingan, ham taklif qilgan — baribir bitta mukofot (qo'shilmaydi).
        Assert.Equal(ReferralRules.BonusExtraExercises, ReferralRules.ExtraExercises(true, Now.AddDays(-1), true, Now));
        Assert.Equal(0, ReferralRules.ExtraExercises(false, Now.AddDays(-100), false, Now));
    }

    [Fact]
    public void Recent_signup_cutoff_matches_the_bonus_days()
    {
        Assert.Equal(Now.AddDays(-ReferralRules.BonusDays), ReferralRules.RecentSignupCutoff(Now));
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("true", true)]
    [InlineData("garbage", true)]
    [InlineData("false", false)]
    [InlineData("False", false)]
    public void Feature_flag_is_on_by_default_and_off_only_with_false(string? value, bool expected)
    {
        var cfg = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Features:Referral"] = value }).Build();
        Assert.Equal(expected, new FeatureFlags(cfg).Referral);
    }

    [Fact]
    public void Demo_accounts_are_recognised_so_they_cannot_refer_or_earn_the_bonus()
    {
        Assert.True(DemoAccount.IsDemo(DemoAccount.NewEmail()));
        Assert.False(DemoAccount.IsDemo("real@gmail.com"));
    }

    // ---- Telegram: hisob ochilgandan keyin taklifni biriktirish -------------

    [Theory]
    [InlineData("telegram", 5, true)]
    [InlineData("telegram_app", 59, true)]
    [InlineData("telegram", 61, false)]   // oyna (1 soat) tugagan
    [InlineData("email", 5, false)]       // email/Google'da kod ro'yxatdan o'tishning o'zida ketadi
    [InlineData("google", 5, false)]
    [InlineData("demo", 5, false)]
    [InlineData(null, 5, false)]
    public void Claim_is_only_for_fresh_telegram_accounts(string? method, int minutesOld, bool expected)
    {
        Assert.Equal(expected, ReferralRules.CanClaim(null, method, Now.AddMinutes(-minutesOld), Now));
    }

    [Fact]
    public void Claim_never_overwrites_an_existing_referrer_or_accepts_future_dates()
    {
        Assert.False(ReferralRules.CanClaim(Guid.NewGuid(), "telegram", Now.AddMinutes(-1), Now));
        Assert.False(ReferralRules.CanClaim(null, "telegram", Now.AddMinutes(5), Now));
    }
}
