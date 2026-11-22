using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
namespace SpeakingCoach.Api.Tests;

/// <summary>TelegramLinkService (Profil → Telegram): xotiradagi baza bilan xarakter testlar.</summary>
public class TelegramLinkServiceTests
{
    private static TelegramLinkService Make(bool enabled, out AppDbContext db)
    {
        db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var cfg = new Dictionary<string, string?> { ["Telegram:BotUsername"] = "@TestBot" };
        if (enabled)
        {
            cfg["Telegram:BotToken"] = "123:ABC";
            cfg["RENDER_EXTERNAL_URL"] = "https://api.example.com";
        }
        return new TelegramLinkService(db, new TelegramOptions(new ConfigurationBuilder().AddInMemoryCollection(cfg).Build()));
    }

    private static User NewUser(string email = "a@site.uz") => new() { Id = Guid.NewGuid(), Email = email };

    [Fact]
    public async Task Status_reports_the_bot_and_the_linked_account()
    {
        var svc = Make(true, out var db);
        var user = NewUser();
        var before = await svc.StatusAsync(user.Id);
        Assert.True(before.Enabled);
        Assert.Equal("TestBot", before.BotUsername);
        Assert.False(before.Linked);
        Assert.Null(before.ReminderHour);

        db.TelegramAccounts.Add(new TelegramAccount { ChatId = 42, UserId = user.Id, Username = "elshod", ReminderHour = 9 });
        await db.SaveChangesAsync();
        var after = await svc.StatusAsync(user.Id);
        Assert.True(after.Linked);
        Assert.Equal("elshod", after.Username);
        Assert.Equal(9, after.ReminderHour);
        Assert.False((await svc.StatusAsync(Guid.NewGuid())).Linked);   // boshqa foydalanuvchi ulanmagan
    }

    [Fact]
    public async Task Link_is_refused_when_the_bot_is_off_or_for_demo_accounts()
    {
        var off = Make(false, out _);
        Assert.True((await off.CreateLinkAsync(NewUser(), 300, "uz", "uz")).Disabled);

        var on = Make(true, out var db);
        var demo = await on.CreateLinkAsync(NewUser("demo123@" + DemoAccount.Domain), 300, "uz", "uz");
        Assert.True(demo.DemoBlocked);
        Assert.Null(demo.Url);
        Assert.Empty(db.TelegramLinkTokens);
    }

    [Fact]
    public async Task Link_creates_one_token_per_user_and_stores_only_its_hash()
    {
        var svc = Make(true, out var db);
        var user = NewUser();
        var other = NewUser("b@site.uz");
        var first = await svc.CreateLinkAsync(user, 300, "ru", "uz");
        Assert.StartsWith("https://t.me/TestBot?start=", first.Url);
        var token = first.Url![(first.Url.IndexOf("start=") + 6)..];

        var row = await db.TelegramLinkTokens.SingleAsync();
        Assert.Equal(BotLogic.HashToken(token), row.TokenHash);
        Assert.NotEqual(token, row.TokenHash);                 // token o'zi bazada saqlanmaydi
        Assert.Equal("ru", row.Lang);
        Assert.Equal(300, row.TzOffsetMinutes);

        await svc.CreateLinkAsync(other, 0, null, "en");
        await svc.CreateLinkAsync(user, 0, "xx", "en");         // eski token almashadi; noma'lum til — zaxira til
        var rows = await db.TelegramLinkTokens.ToListAsync();
        Assert.Equal(2, rows.Count);
        Assert.Equal("en", rows.Single(r => r.UserId == user.Id).Lang);
        Assert.Equal("en", rows.Single(r => r.UserId == other.Id).Lang);
    }

    [Fact]
    public async Task Link_clamps_the_time_zone_and_drops_expired_tokens()
    {
        var svc = Make(true, out var db);
        db.TelegramLinkTokens.Add(new TelegramLinkToken { TokenHash = "old", UserId = Guid.NewGuid(), ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1) });
        await db.SaveChangesAsync();
        var user = NewUser();
        await svc.CreateLinkAsync(user, 99999, "uz", "uz");
        var row = await db.TelegramLinkTokens.SingleAsync();       // muddati o'tgan "old" o'chdi
        Assert.Equal(user.Id, row.UserId);
        Assert.Equal(14 * 60, row.TzOffsetMinutes);
        Assert.True(row.ExpiresAtUtc > DateTime.UtcNow);
    }

    [Theory]
    [InlineData(7, 7)]
    [InlineData(-5, 0)]
    [InlineData(99, 23)]
    [InlineData(null, null)]
    public async Task Reminder_hour_is_clamped_or_cleared(int? input, int? expected)
    {
        var svc = Make(true, out var db);
        var user = NewUser();
        db.TelegramAccounts.Add(new TelegramAccount { ChatId = 1, UserId = user.Id, ReminderHour = 12 });
        await db.SaveChangesAsync();
        var (found, hour) = await svc.SetReminderHourAsync(user.Id, input);
        Assert.True(found);
        Assert.Equal(expected, hour);
        Assert.Equal(expected, (await db.TelegramAccounts.SingleAsync()).ReminderHour);
    }

    [Fact]
    public async Task Reminder_hour_needs_a_linked_account_and_unlink_removes_only_mine()
    {
        var svc = Make(true, out var db);
        var me = NewUser();
        var other = NewUser("b@site.uz");
        Assert.False((await svc.SetReminderHourAsync(me.Id, 8)).Found);

        db.TelegramAccounts.AddRange(new TelegramAccount { ChatId = 1, UserId = me.Id }, new TelegramAccount { ChatId = 2, UserId = other.Id });
        await db.SaveChangesAsync();
        await svc.UnlinkAsync(me.Id);
        var left = await db.TelegramAccounts.SingleAsync();
        Assert.Equal(other.Id, left.UserId);
    }
}
