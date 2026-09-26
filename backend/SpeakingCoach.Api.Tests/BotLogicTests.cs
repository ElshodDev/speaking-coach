using Microsoft.Extensions.Configuration;
using SpeakingCoach.Api.Services;
using SpeakingCoach.Api.Services.Telegram;

namespace SpeakingCoach.Api.Tests;

public class BotLogicTests
{
    private static readonly Guid Card = Guid.Parse("0f8fad5b-d9cb-469f-a165-70867728950e");

    public static IEnumerable<object[]> Callbacks() => new[]
    {
        new object[] { new BotCallback.StartReview() },
        new object[] { new BotCallback.ShowAnswer(Card) },
        new object[] { new BotCallback.Grade(Card, ReviewGrade.Easy) },
        new object[] { new BotCallback.AddWord("well-known") },
        new object[] { new BotCallback.SetReminder(20) },
        new object[] { new BotCallback.SetReminder(null) },
        new object[] { new BotCallback.SetLang("ru") },
        new object[] { new BotCallback.Unlink() },
    };

    [Theory]
    [MemberData(nameof(Callbacks))]
    public void Callback_data_round_trips_and_fits_telegram_limit(BotCallback cb)
    {
        var data = BotLogic.Encode(cb);

        Assert.True(data.Length <= 64, data);
        Assert.Equal(cb, BotLogic.Decode(data));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("rv:g:not-a-guid:2")]
    [InlineData("rv:g:0f8fad5bd9cb469fa16570867728950e:9")]
    [InlineData("set:h:24")]
    [InlineData("set:l:de")]
    [InlineData("w:add:<script>")]
    [InlineData("drop table")]
    public void Garbage_callbacks_are_ignored(string? data)
    {
        Assert.Null(BotLogic.Decode(data));
    }

    [Theory]
    [InlineData("/start", "")]
    [InlineData("/start abc_DEF-123", "abc_DEF-123")]
    [InlineData("/start@SpeakingCoachUzBot abc", "abc")]
    [InlineData("/start@SpeakingCoachUzBot", "")]
    [InlineData("hello", null)]
    [InlineData(null, null)]
    public void Start_payload_is_extracted(string? text, string? expected)
    {
        Assert.Equal(expected, BotLogic.StartPayload(text));
    }

    [Theory]
    [InlineData("🔁 Takrorlash", MenuAction.Review)]
    [InlineData("🔁 Повторение", MenuAction.Review)]
    [InlineData("/review", MenuAction.Review)]
    [InlineData("📊 Progress", MenuAction.Stats)]
    [InlineData("/stats@SpeakingCoachUzBot", MenuAction.Stats)]
    [InlineData("⚙️ Настройки", MenuAction.Settings)]
    [InlineData("/help", MenuAction.Help)]
    [InlineData("reliable", MenuAction.None)]
    public void Menu_buttons_work_in_every_language(string text, MenuAction expected)
    {
        Assert.Equal(expected, BotLogic.ParseMenu(text));
    }

    [Fact]
    public void Main_menu_has_two_rows_in_the_chosen_language()
    {
        var menu = BotLogic.MainMenu("en");

        Assert.Equal(2, menu.Count);
        Assert.Equal("🔁 Review", menu[0][0]);
    }

    [Theory]
    [InlineData("ru", "ru")]
    [InlineData("ru-RU", "ru")]
    [InlineData("uz", "uz")]
    [InlineData("en-US", "en")]
    [InlineData("tr", "uz")]
    [InlineData(null, "uz")]
    public void Telegram_language_maps_to_ours(string? code, string expected)
    {
        Assert.Equal(expected, BotLogic.LangFromTelegram(code));
    }

    [Theory]
    [InlineData("reliable", true)]
    [InlineData(" don't ", true)]
    [InlineData("two words", false)]
    [InlineData("привет", false)]
    [InlineData("🔁 Takrorlash", false)]
    public void Only_single_english_words_are_looked_up(string text, bool expected)
    {
        Assert.Equal(expected, BotLogic.IsLookupWord(text));
    }

    [Fact]
    public void Link_tokens_are_url_safe_unique_and_hashed()
    {
        var a = BotLogic.NewLinkToken();
        var b = BotLogic.NewLinkToken();

        Assert.NotEqual(a, b);
        Assert.Matches("^[A-Za-z0-9_-]{32}$", a);
        Assert.Equal(64, BotLogic.HashToken(a).Length);
        Assert.NotEqual(BotLogic.HashToken(a), BotLogic.HashToken(b));
    }

    // Toshkent: UTC+5 → getTimezoneOffset() = −300.
    private const int Tashkent = -300;

    [Fact]
    public void Reminder_is_sent_once_the_hour_has_come()
    {
        var at1945 = new DateTime(2026, 9, 26, 14, 45, 0, DateTimeKind.Utc);
        var at2010 = new DateTime(2026, 9, 26, 15, 10, 0, DateTimeKind.Utc);

        Assert.False(BotLogic.ShouldRemind(20, null, at1945, Tashkent));
        Assert.True(BotLogic.ShouldRemind(20, null, at2010, Tashkent));
    }

    [Fact]
    public void A_late_cron_still_sends_but_only_once_a_day()
    {
        var at2140 = new DateTime(2026, 9, 26, 16, 40, 0, DateTimeKind.Utc);
        var today = new DateOnly(2026, 9, 26);

        Assert.True(BotLogic.ShouldRemind(20, today.AddDays(-1), at2140, Tashkent));
        Assert.False(BotLogic.ShouldRemind(20, today, at2140, Tashkent));
    }

    [Fact]
    public void No_reminders_late_at_night_or_when_turned_off()
    {
        var at2310 = new DateTime(2026, 9, 26, 18, 10, 0, DateTimeKind.Utc);

        Assert.False(BotLogic.ShouldRemind(20, null, at2310, Tashkent));
        Assert.False(BotLogic.ShouldRemind(null, null, at2310.AddHours(-3), Tashkent));
    }

    [Fact]
    public void Local_day_follows_the_users_time_zone()
    {
        // 20:30 UTC = ertasi kun 01:30 Toshkentda.
        Assert.Equal(new DateOnly(2026, 9, 27), BotLogic.LocalToday(new DateTime(2026, 9, 26, 20, 30, 0, DateTimeKind.Utc), Tashkent));
    }

    [Fact]
    public void Html_is_escaped_for_telegram()
    {
        Assert.Equal("a &lt;b&gt; &amp; c", BotLogic.Html("a <b> & c"));
    }

    [Fact]
    public void Options_use_render_url_and_derive_a_valid_webhook_secret()
    {
        var o = new TelegramOptions(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Telegram:BotToken"] = "123:ABC",
            ["RENDER_EXTERNAL_URL"] = "https://speaking-coach.onrender.com/",
            ["Telegram:BotUsername"] = "@SpeakingCoachUzBot",
        }).Build());

        Assert.True(o.Enabled);
        Assert.Equal("https://speaking-coach.onrender.com", o.PublicUrl);
        Assert.Equal("SpeakingCoachUzBot", o.BotUsername);
        Assert.Matches("^[A-Za-z0-9_-]{1,256}$", o.WebhookSecret!);
        Assert.DoesNotContain("123:ABC", o.WebhookSecret!);
    }

    [Fact]
    public void Bot_is_off_without_a_token()
    {
        var o = new TelegramOptions(new ConfigurationBuilder().Build());

        Assert.False(o.Enabled);
        Assert.Null(o.WebhookSecret);
    }
}
