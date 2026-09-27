using System.Text.Json;
using Microsoft.Extensions.Configuration;
using SpeakingCoach.Api.Services;
using SpeakingCoach.Api.Services.Telegram;

namespace SpeakingCoach.Api.Tests;

/// <summary>Mini App: initData imzosini tekshirish, sayt tugmalari (web_app) va /guide.</summary>
public class TelegramWebAppTests
{
    private const string Token = "123456:TEST-token";
    private static readonly DateTime Now = new(2026, 9, 27, 12, 0, 0, DateTimeKind.Utc);

    private static string InitData(DateTime authDate, string token = Token, long userId = 777, bool includeUser = true, string? tamper = null)
    {
        var fields = new Dictionary<string, string>
        {
            ["query_id"] = "AAH",
            ["auth_date"] = new DateTimeOffset(authDate).ToUnixTimeSeconds().ToString(),
            ["signature"] = "sig-part",
        };
        if (includeUser) fields["user"] = $"{{\"id\":{userId},\"first_name\":\"Elshod\",\"username\":\"elshod_dev\",\"language_code\":\"uz\"}}";
        var hash = TelegramWebAppAuth.Sign(TelegramWebAppAuth.DataCheckString(fields), token);
        if (tamper is not null) fields["query_id"] = tamper;
        fields["hash"] = hash;
        return string.Join("&", fields.Select(f => $"{Uri.EscapeDataString(f.Key)}={Uri.EscapeDataString(f.Value)}"));
    }

    [Fact]
    public void Valid_init_data_gives_the_telegram_user()
    {
        Assert.Equal(777L, TelegramWebAppAuth.Validate(InitData(Now.AddMinutes(-10)), Token, Now));
    }

    [Fact]
    public void Forged_old_or_incomplete_init_data_is_rejected()
    {
        Assert.Null(TelegramWebAppAuth.Validate(InitData(Now.AddMinutes(-10), token: "999:OTHER"), Token, Now));  // boshqa bot
        Assert.Null(TelegramWebAppAuth.Validate(InitData(Now.AddMinutes(-10), tamper: "BBB"), Token, Now));      // o'zgartirilgan
        Assert.Null(TelegramWebAppAuth.Validate(InitData(Now.AddDays(-2)), Token, Now));                         // eskirgan
        Assert.Null(TelegramWebAppAuth.Validate(InitData(Now.AddHours(1)), Token, Now));                          // kelajakdan
        Assert.Null(TelegramWebAppAuth.Validate(InitData(Now, includeUser: false), Token, Now));                  // user yo'q
        Assert.Null(TelegramWebAppAuth.Validate(InitData(Now), null, Now));                                       // bot o'chiq
        Assert.Null(TelegramWebAppAuth.Validate("", Token, Now));
        Assert.Null(TelegramWebAppAuth.Validate("garbage", Token, Now));
        Assert.Null(TelegramWebAppAuth.Validate("a=1&a=2", Token, Now));
    }

    [Fact]
    public void Data_check_string_is_sorted_and_skips_only_the_hash()
    {
        var s = TelegramWebAppAuth.DataCheckString([new("user", "u"), new("hash", "h"), new("auth_date", "1"), new("signature", "s")]);
        Assert.Equal("auth_date=1\nsignature=s\nuser=u", s);
    }

    private static TelegramOptions Options(string frontend) => new(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["Telegram:BotToken"] = Token,
        ["FrontendOrigin"] = frontend,
    }).Build());

    [Fact]
    public void Site_buttons_open_inside_telegram_on_https()
    {
        var b = Options("https://speaking-coach.vercel.app/").SiteButton("Go", "mock/cefr-reading");
        Assert.Null(b.Url);
        Assert.Equal("https://speaking-coach.vercel.app/?tgroute=mock%2Fcefr-reading", b.WebApp!.Url);
        var json = JsonSerializer.Serialize(b);
        Assert.Contains("\"web_app\":{\"url\":", json);
        Assert.DoesNotContain("callback_data", json);

        var local = Options("http://localhost:5173").SiteButton("Go", "review");
        Assert.Null(local.WebApp);
        Assert.Equal("http://localhost:5173/#/review", local.Url);
    }

    [Fact]
    public void Guide_lists_every_section_with_a_button_and_an_app_button()
    {
        var o = Options("https://x.app");
        var rows = BotGuide.Buttons("uz", o);
        var all = rows.SelectMany(r => r).ToList();
        Assert.Equal(BotGuide.Sections.Length + 1, all.Count);
        Assert.All(rows, r => Assert.True(r.Count <= 2));
        Assert.Equal("https://x.app/?tgroute=", all.Last().WebApp!.Url);
        Assert.Contains(all, b => b.WebApp!.Url.EndsWith("tgroute=shadowing"));
        foreach (var lang in Texts.Langs)
        {
            var text = BotGuide.Message(lang);
            Assert.Contains("Shadowing", text);
            Assert.All(BotGuide.Sections, s => Assert.DoesNotContain("bot.sec_", Texts.Get(lang, $"bot.sec_{s.Key}")));
        }
    }

    [Fact]
    public void Guide_callback_round_trips()
    {
        Assert.Equal(new BotCallback.ShowGuide(), BotLogic.Decode(BotLogic.Encode(new BotCallback.ShowGuide())));
    }
}
