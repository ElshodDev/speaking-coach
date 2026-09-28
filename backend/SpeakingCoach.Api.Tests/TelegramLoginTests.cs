using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using SpeakingCoach.Api.Data;
using SpeakingCoach.Api.Endpoints;
using SpeakingCoach.Api.Services;
using SpeakingCoach.Api.Services.Telegram;

namespace SpeakingCoach.Api.Tests;

/// <summary>
/// Saytga Telegram orqali kirish, sintetik emaillar, sirpanuvchi sessiya,
/// Google bilan kirish qoidasi, kunlik xat limiti, statistika va fikrlar.
/// </summary>
public class TelegramLoginTests
{
    private static readonly DateTime Now = new(2026, 9, 28, 12, 0, 0, DateTimeKind.Utc);

    private static TelegramLogin Login(Action<TelegramLogin>? change = null)
    {
        var l = new TelegramLogin
        {
            Id = Guid.NewGuid(),
            Code = "42",
            CreatedAtUtc = Now.AddMinutes(-1),
            ExpiresAtUtc = Now.AddMinutes(4),
            Lang = "uz",
        };
        change?.Invoke(l);
        return l;
    }

    // ---------------- Kod va tugmalar ----------------

    [Fact]
    public void Code_is_two_digits_from_10_to_99()
    {
        for (var i = 0; i < 300; i++)
        {
            var code = TelegramLoginRules.NewCode();
            Assert.Equal(2, code.Length);
            var n = int.Parse(code);
            Assert.True(n is >= 10 and <= 99, code);
        }
    }

    [Fact]
    public void Choices_contain_the_code_and_two_distinct_decoys()
    {
        for (var i = 0; i < 300; i++)
        {
            var code = TelegramLoginRules.NewCode();
            var choices = TelegramLoginRules.Choices(code);
            Assert.Equal(3, choices.Count);
            Assert.Equal(3, choices.Distinct().Count());
            Assert.Contains(code, choices);
            Assert.All(choices, c => Assert.True(int.Parse(c) is >= 10 and <= 99, c));
        }
    }

    [Fact]
    public void Choices_are_shuffled_so_the_code_is_not_always_first()
    {
        var positions = new HashSet<int>();
        for (var i = 0; i < 300; i++)
        {
            positions.Add(TelegramLoginRules.Choices("42").ToList().IndexOf("42"));
        }
        Assert.Equal(new[] { 0, 1, 2 }, positions.OrderBy(p => p).ToArray());
    }

    [Fact]
    public void Choices_stay_distinct_even_with_a_stuck_random_source()
    {
        // Tasodif manbai doim bir xil qiymat qaytarsa ham — 3 ta farqli raqam.
        var choices = TelegramLoginRules.Choices("10", (min, max) => min);
        Assert.Equal(3, choices.Distinct().Count());
        Assert.Contains("10", choices);
    }

    [Theory]
    [InlineData("42", 42, true)]
    [InlineData("42", 24, false)]
    [InlineData("42", null, false)]
    [InlineData("42", 142, false)]
    public void Only_the_shown_number_confirms(string code, int? number, bool expected)
    {
        Assert.Equal(expected, TelegramLoginRules.Matches(code, number));
    }

    // ---------------- Havola va callback ----------------

    [Fact]
    public void Start_parameter_fits_telegram_limits()
    {
        for (var i = 0; i < 50; i++)
        {
            var nonce = TelegramLoginRules.NewNonce();
            Assert.True(nonce.Length >= 32, nonce);
            var start = TelegramLoginRules.StartPrefix + nonce;
            Assert.True(start.Length <= 64, start);
            Assert.Matches("^[A-Za-z0-9_-]+$", start);
            Assert.Equal(nonce, TelegramLoginRules.NonceFromStart(start));
            Assert.Equal(start, BotLogic.StartPayload("/start " + start));
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("abcDEF123")]                      // oddiy ulash tokeni
    [InlineData("login_")]
    [InlineData("login_short")]
    [InlineData("login_aaaaaaaaaaaaaaaaaaaa$aaaaaaaaaaa")]
    public void Other_start_payloads_are_not_login_nonces(string? payload)
    {
        Assert.Null(TelegramLoginRules.NonceFromStart(payload));
    }

    [Fact]
    public void Login_callbacks_round_trip_and_fit_64_bytes()
    {
        var id = Guid.NewGuid();
        foreach (var cb in new BotCallback[] { new BotCallback.WebLogin(id, 42), new BotCallback.WebLogin(id, 10), new BotCallback.WebLogin(id, 99), new BotCallback.WebLogin(id, null) })
        {
            var data = BotLogic.Encode(cb);
            Assert.True(System.Text.Encoding.UTF8.GetByteCount(data) <= 64, data);
            Assert.Equal(cb, BotLogic.Decode(data));
        }
        Assert.Equal($"tl:{id:N}:42", BotLogic.Encode(new BotCallback.WebLogin(id, 42)));
        Assert.Equal($"tl:{id:N}:x", BotLogic.Encode(new BotCallback.WebLogin(id, null)));
    }

    [Theory]
    [InlineData("tl:nothex:42")]
    [InlineData("tl:{0}:05")]
    [InlineData("tl:{0}:100")]
    [InlineData("tl:{0}:4a")]
    [InlineData("tl:{0}:")]
    [InlineData("tl:{0}:y")]
    [InlineData("tl:{0}")]
    public void Broken_login_callbacks_are_ignored(string template)
    {
        Assert.Null(BotLogic.Decode(string.Format(template, Guid.NewGuid().ToString("N"))));
    }

    // ---------------- Holatlar ----------------

    [Fact]
    public void Status_follows_the_login_lifecycle()
    {
        Assert.Equal(TelegramLoginStatus.Pending, TelegramLoginRules.StatusOf(Login(), Now));
        Assert.Equal(TelegramLoginStatus.Expired, TelegramLoginRules.StatusOf(Login(l => l.ExpiresAtUtc = Now), Now));
        Assert.Equal(TelegramLoginStatus.Rejected, TelegramLoginRules.StatusOf(Login(l => l.RejectedAtUtc = Now), Now));
        // Tasdiqlandi, hisob hali ochilmoqda — kutish.
        Assert.Equal(TelegramLoginStatus.Pending, TelegramLoginRules.StatusOf(Login(l => l.ConfirmedAtUtc = Now), Now));
        var ready = Login(l => { l.ConfirmedAtUtc = Now.AddSeconds(-5); l.UserId = Guid.NewGuid(); });
        Assert.Equal(TelegramLoginStatus.Confirmed, TelegramLoginRules.StatusOf(ready, Now));
        // Tasdiqlangan, lekin muddati o'tgandan keyin ham — qisqa oyna ichida olinadi.
        Assert.Equal(TelegramLoginStatus.Confirmed, TelegramLoginRules.StatusOf(ready, Now.AddMinutes(4)));
        Assert.Equal(TelegramLoginStatus.Expired, TelegramLoginRules.StatusOf(ready, Now.AddMinutes(6)));
    }

    [Fact]
    public void A_login_gives_a_session_only_once()
    {
        var consumed = Login(l => { l.ConfirmedAtUtc = Now; l.UserId = Guid.NewGuid(); l.ConsumedAtUtc = Now; });
        Assert.Equal(TelegramLoginStatus.Expired, TelegramLoginRules.StatusOf(consumed, Now));
        Assert.False(TelegramLoginRules.IsOpen(consumed, Now));
    }

    [Fact]
    public void Only_an_open_login_can_be_confirmed_or_rejected()
    {
        Assert.True(TelegramLoginRules.IsOpen(Login(), Now));
        Assert.False(TelegramLoginRules.IsOpen(Login(l => l.ExpiresAtUtc = Now.AddSeconds(-1)), Now));
        Assert.False(TelegramLoginRules.IsOpen(Login(l => l.ConfirmedAtUtc = Now), Now));
        Assert.False(TelegramLoginRules.IsOpen(Login(l => l.RejectedAtUtc = Now), Now));
    }

    [Theory]
    [InlineData(TelegramLoginStatus.Pending, "pending")]
    [InlineData(TelegramLoginStatus.Expired, "expired")]
    [InlineData(TelegramLoginStatus.Rejected, "rejected")]
    [InlineData(TelegramLoginStatus.Confirmed, "confirmed")]
    public void Status_names_match_the_api_contract(TelegramLoginStatus status, string name)
    {
        Assert.Equal(name, TelegramLoginRules.StatusName(status));
    }

    [Fact]
    public void Poll_token_and_nonce_are_stored_only_as_hashes()
    {
        var token = TelegramLoginRules.NewPollToken();
        var hash = TelegramLoginRules.Hash(token);
        Assert.Equal(64, hash.Length);
        Assert.NotEqual(token, hash);
        Assert.Equal(hash, TelegramLoginRules.Hash(token));
        Assert.NotEqual(TelegramLoginRules.NewPollToken(), token);
    }

    [Theory]
    [InlineData("  Ali   Valiyev ", "Ali Valiyev")]
    [InlineData("Oʻlmas", "Oʻlmas")]
    [InlineData("🔥🔥", null)]
    [InlineData("A", null)]
    [InlineData("   ", null)]
    [InlineData(null, null)]
    public void Telegram_first_name_becomes_a_safe_nickname(string? first, string? expected)
    {
        Assert.Equal(expected, TelegramLoginRules.CleanDisplayName(first));
    }

    [Fact]
    public void Long_first_name_is_cut_to_the_nickname_limit()
    {
        var name = TelegramLoginRules.CleanDisplayName(new string('a', 64));
        Assert.Equal(30, name!.Length);
    }

    // ---------------- Sintetik emaillar ----------------

    [Fact]
    public void Telegram_accounts_get_a_synthetic_email_that_is_never_valid()
    {
        var email = TelegramLoginRules.SyntheticEmail(123456789);
        Assert.Equal("tg-123456789@telegram.invalid", email);
        Assert.True(AuthService.IsSyntheticEmail(email));
        Assert.True(AuthService.IsSyntheticEmail("TG-1@Telegram.Invalid"));
        Assert.False(AuthService.IsValidEmail(email));
        Assert.False(AuthService.IsValidEmail("someone@anything.invalid"));
    }

    [Fact]
    public void Real_and_demo_emails_are_classified_correctly()
    {
        Assert.False(AuthService.IsSyntheticEmail("ali@gmail.com"));
        Assert.True(AuthService.IsValidEmail("ali@gmail.com"));
        Assert.True(AuthService.IsSyntheticEmail(DemoAccount.NewEmail()));
        Assert.True(AuthService.IsSyntheticEmail(""));
        Assert.True(AuthService.IsSyntheticEmail(null));
    }

    [Theory]
    [InlineData("tg-5@telegram.invalid", "DELETE", true)]
    [InlineData("tg-5@telegram.invalid", " delete ", true)]
    [InlineData("tg-5@telegram.invalid", "tg-5@telegram.invalid", true)]
    [InlineData("tg-5@telegram.invalid", "yes", false)]
    [InlineData("tg-5@telegram.invalid", null, false)]
    [InlineData("ali@gmail.com", "DELETE", false)]
    [InlineData("ali@gmail.com", " Ali@Gmail.com ", true)]
    [InlineData("ali@gmail.com", "ali@mail.com", false)]
    public void Account_deletion_confirmation_depends_on_the_email_kind(string email, string? typed, bool ok)
    {
        Assert.Equal(ok, AccountEndpoints.DeleteConfirmed(email, typed));
    }

    // ---------------- Sessiya va Google ----------------

    [Fact]
    public void Session_is_renewed_only_when_less_than_20_days_are_left()
    {
        Assert.Null(AuthService.RenewedExpiry(Now.AddDays(25), Now));
        Assert.Null(AuthService.RenewedExpiry(Now.AddDays(20), Now));
        Assert.Equal(Now.AddDays(30), AuthService.RenewedExpiry(Now.AddDays(19), Now));
        Assert.Equal(Now.AddDays(30), AuthService.RenewedExpiry(Now.AddMinutes(5), Now));
        // Muddati o'tgan sessiya tiriltirilmaydi.
        Assert.Null(AuthService.RenewedExpiry(Now.AddSeconds(-1), Now));
    }

    [Fact]
    public void An_active_user_writes_the_session_at_most_every_10_days()
    {
        var expires = Now.AddDays(30);
        var writes = 0;
        // 90 kun davomida har soatda so'rov.
        for (var t = Now; t < Now.AddDays(90); t = t.AddHours(1))
        {
            if (AuthService.RenewedExpiry(expires, t) is { } renewed)
            {
                writes++;
                expires = renewed;
            }
            Assert.True(expires > t);
        }
        Assert.True(writes <= 9, writes.ToString());
        Assert.True(writes >= 7, writes.ToString());
    }

    [Theory]
    [InlineData(false, true, true)]    // email tasdiqlanmagan — parolni begona odam qo'ygan bo'lishi mumkin
    [InlineData(false, false, true)]   // tasdiqlash o'chiq bo'lsa ham (aks holda hisobni oldindan egallab olish mumkin)
    [InlineData(true, true, false)]
    [InlineData(true, false, false)]
    public void Google_sign_in_wipes_the_password_of_an_unverified_account(bool verified, bool verificationRequired, bool wipe)
    {
        Assert.Equal(wipe, AuthService.ShouldWipePasswordOnGoogle(verified, verificationRequired));
    }

    // ---------------- Kunlik xat limiti ----------------

    [Fact]
    public void Daily_email_cap_blocks_after_the_limit_and_resets_next_tashkent_day()
    {
        var cap = new EmailDailyCap();
        // 18:00 UTC = 23:00 Toshkent.
        var evening = new DateTime(2026, 9, 28, 18, 0, 0, DateTimeKind.Utc);
        Assert.True(cap.TryTake(2, evening));
        Assert.True(cap.TryTake(2, evening));
        Assert.False(cap.TryTake(2, evening));
        Assert.True(cap.IsReached(2, evening));
        Assert.Equal(2, cap.Count(evening));
        // 19:00 UTC = 00:00 Toshkent — yangi kun.
        var midnight = new DateTime(2026, 9, 28, 19, 0, 0, DateTimeKind.Utc);
        Assert.False(cap.IsReached(2, midnight));
        Assert.True(cap.TryTake(2, midnight));
        Assert.Equal(1, cap.Count(midnight));
    }

    [Fact]
    public void Failed_send_returns_its_slot()
    {
        var cap = new EmailDailyCap();
        Assert.True(cap.TryTake(1, Now));
        cap.Release(Now);
        Assert.True(cap.TryTake(1, Now));
        Assert.False(cap.TryTake(1, Now));
    }

    [Fact]
    public void Tashkent_day_is_utc_plus_five()
    {
        Assert.Equal(new DateOnly(2026, 9, 28), EmailDailyCap.TashkentDay(new DateTime(2026, 9, 28, 18, 59, 0, DateTimeKind.Utc)));
        Assert.Equal(new DateOnly(2026, 9, 29), EmailDailyCap.TashkentDay(new DateTime(2026, 9, 28, 19, 0, 0, DateTimeKind.Utc)));
    }

    [Theory]
    [InlineData(null, 250)]
    [InlineData("", 250)]
    [InlineData("abc", 250)]
    [InlineData("-3", 250)]
    [InlineData("0", 0)]
    [InlineData("280", 280)]
    public void Daily_cap_comes_from_configuration(string? value, int expected)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Email:DailyCap"] = value })
            .Build();
        Assert.Equal(expected, EmailDailyCap.CapFrom(config));
    }

    [Fact]
    public void Email_change_code_is_bound_to_the_new_address()
    {
        var userId = Guid.NewGuid();
        var code = new EmailCode { UserId = userId, Purpose = EmailCodePurpose.ChangeEmail, CodeHash = EmailCodeRules.Hash(userId, "123456", "new@mail.com") };
        Assert.True(EmailCodeRules.Matches(code, userId, "123456", "new@mail.com"));
        Assert.False(EmailCodeRules.Matches(code, userId, "123456", "other@mail.com"));
        Assert.False(EmailCodeRules.Matches(code, userId, "123456"));
        // Oddiy kodlar avvalgidek.
        Assert.Equal(EmailCodeRules.Hash(userId, "123456"), EmailCodeRules.Hash(userId, "123456", null));
    }

    // ---------------- Statistika ----------------

    [Theory]
    [InlineData("google", "a@b.com", true, "google")]
    [InlineData("telegram_app", "tg-1@telegram.invalid", false, "telegram_app")]
    [InlineData(null, "a@b.com", true, "email")]
    [InlineData(null, "a@b.com", false, "other")]
    [InlineData(null, "tg-1@telegram.invalid", false, "telegram")]
    [InlineData(null, "demo-abc@demo.speakingcoach.invalid", false, "demo")]
    [InlineData("", "a@b.com", true, "email")]
    public void Signup_method_is_inferred_for_old_accounts(string? method, string email, bool hasPassword, string expected)
    {
        Assert.Equal(expected, AdminService.InferSignup(method, email, hasPassword));
    }

    [Fact]
    public void Signup_summary_lists_the_biggest_first()
    {
        var s = AdminService.SignupSummary(new Dictionary<string, int> { ["google"] = 5, ["email"] = 12, ["telegram"] = 5 });
        Assert.Equal("email 12 · google 5 · telegram 5", s);
        Assert.Equal("—", AdminService.SignupSummary(null));
    }

    [Fact]
    public void Last_seen_is_written_at_most_once_an_hour()
    {
        Assert.True(BotLogic.ShouldTouchLastSeen(null, Now));
        Assert.False(BotLogic.ShouldTouchLastSeen(Now.AddMinutes(-59), Now));
        Assert.True(BotLogic.ShouldTouchLastSeen(Now.AddMinutes(-60), Now));
    }

    [Fact]
    public void Admin_overview_keeps_old_fields_and_defaults_new_ones()
    {
        var o = new AdminOverview(1, 1, 0, 0, 0, 0, new(), new(), 0, 0, 0, new(), new());
        Assert.Equal(0, o.NewUsersToday);
        Assert.Null(o.Signups);
        Assert.Equal(0, o.TelegramLinked);
        Assert.Equal(0, o.TelegramActive7d);
        Assert.Equal(0, o.Feedback7d);
    }

    // ---------------- Fikrlar ----------------

    [Theory]
    [InlineData("bug", "Tugma ishlamayapti", null)]
    [InlineData(" IDEA ", "Yana test qoʻshing", null)]
    [InlineData("content", "abc", null)]
    [InlineData("ai", "AI bahosi juda qattiq", null)]
    [InlineData("other", "Rahmat!", null)]
    [InlineData("spam", "Hello there", "feedback.bad_kind")]
    [InlineData(null, "Hello there", "feedback.bad_kind")]
    [InlineData("bug", "ab", "feedback.bad_message")]
    [InlineData("bug", "   ab   ", "feedback.bad_message")]
    [InlineData("bug", null, "feedback.bad_message")]
    public void Feedback_is_validated(string? kind, string? message, string? error)
    {
        Assert.Equal(error, FeedbackRules.Validate(kind, message));
    }

    [Fact]
    public void Feedback_message_and_page_limits()
    {
        Assert.Null(FeedbackRules.Validate("bug", new string('x', 2000)));
        Assert.Equal("feedback.bad_message", FeedbackRules.Validate("bug", new string('x', 2001)));
        Assert.Equal(200, FeedbackRules.NormalizePage(new string('p', 300))!.Length);
        Assert.Null(FeedbackRules.NormalizePage("  "));
        Assert.Equal("idea", FeedbackRules.NormalizeKind(" Idea "));
        Assert.Equal(501, FeedbackRules.Preview(new string('m', 900)).Length);
    }

    // ---------------- Mini App va matnlar ----------------

    [Fact]
    public void Mini_app_init_data_gives_name_username_and_language()
    {
        const string token = "123456:TEST-token";
        var fields = new Dictionary<string, string>
        {
            ["auth_date"] = new DateTimeOffset(Now.AddMinutes(-2)).ToUnixTimeSeconds().ToString(),
            ["user"] = "{\"id\":555,\"first_name\":\"Madina\",\"username\":\"madi\",\"language_code\":\"ru\"}",
        };
        fields["hash"] = TelegramWebAppAuth.Sign(TelegramWebAppAuth.DataCheckString(fields), token);
        var initData = string.Join("&", fields.Select(f => $"{Uri.EscapeDataString(f.Key)}={Uri.EscapeDataString(f.Value)}"));

        var user = TelegramWebAppAuth.ValidateUser(initData, token, Now);
        Assert.NotNull(user);
        Assert.Equal(555L, user!.Id);
        Assert.Equal("Madina", user.FirstName);
        Assert.Equal("madi", user.Username);
        Assert.Equal("ru", user.LanguageCode);
        Assert.Equal(555L, TelegramWebAppAuth.Validate(initData, token, Now));
        Assert.Null(TelegramWebAppAuth.ValidateUser(initData, "999:OTHER", Now));
    }

    [Fact]
    public void Error_codes_are_stable()
    {
        Assert.Equal("email_cap", Texts.CodeOf("email.cap"));
        Assert.Equal("tglogin_disabled", Texts.CodeOf("tglogin.disabled"));
        Assert.Equal("email_disabled", Texts.CodeOf("email.unavailable"));
        Assert.Equal("account_need_email", Texts.CodeOf("account.need_email"));
    }

    [Fact]
    public void New_messages_exist_in_all_languages()
    {
        foreach (var key in new[]
        {
            "tglogin.disabled", "tglogin.prompt", "tglogin.not_me", "tglogin.done", "tglogin.cancelled", "tglogin.expired", "tglogin.private_only",
            "email.cap", "account.need_email", "account.wrong_password", "account.email_same", "account.email_taken",
            "account.confirm_delete_word", "feedback.bad_kind", "feedback.bad_message", "feedback.admin_notify",
        })
        {
            Assert.Contains(key, Texts.Keys);
        }
        Assert.Contains("Telegram", Texts.Get("uz", "auth.email_taken"));
        Assert.Contains("Telegram", Texts.Get("uz", "auth.wrong_credentials"));
        Assert.Contains("2 xonali raqamni", Texts.Get("uz", "tglogin.prompt"));
    }

    [Fact]
    public void Uzbek_messages_do_not_use_a_typographic_quote_as_a_letter()
    {
        // "Chrome’da" emas — "Chromeʼda" (U+02BC); oʻ/gʻ — U+02BB.
        foreach (var key in Texts.Keys)
        {
            Assert.DoesNotMatch(new Regex(@"\p{L}[’']\p{L}"), Texts.Get("uz", key));
        }
    }

    [Theory]
    [InlineData("47", true)]
    [InlineData(" 10 ", true)]
    [InlineData("99", true)]
    [InlineData("05", false)]
    [InlineData("4", false)]
    [InlineData("470", false)]
    [InlineData("4a", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void Only_a_two_digit_message_counts_as_a_typed_login_code(string? text, bool expected)
    {
        Assert.Equal(expected, TelegramLoginRules.IsTypedCode(text));
    }
}
