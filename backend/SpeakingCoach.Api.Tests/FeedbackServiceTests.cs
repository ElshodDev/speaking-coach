using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
namespace SpeakingCoach.Api.Tests;

/// <summary>FeedbackService: fikr va baholarni saqlash, adminga xabar va admin xulosalari (xotiradagi baza bilan).</summary>
public class FeedbackServiceTests
{
    /// <summary>ITelegramApi o'rniga: faqat SendMessageAsync chaqiruvlarini yozib boradi, qolganlari — bo'sh natija.</summary>
    private class RecordingApi : DispatchProxy
    {
        public List<(long ChatId, string Html)> Sent { get; } = [];
        public TaskCompletionSource<bool> First { get; } = new();

        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            if (method!.Name == nameof(ITelegramApi.SendMessageAsync))
            {
                lock (Sent) Sent.Add(((long)args![0]!, (string)args[1]!));
                First.TrySetResult(true);
                return Task.FromResult<long?>(1);
            }
            return method.ReturnType == typeof(Task) ? Task.CompletedTask : null;
        }
    }

    private static (FeedbackService Svc, AppDbContext Db, RecordingApi Api) Make(bool telegram = false, string? adminEmails = null, string? adminIds = null)
    {
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var cfg = new Dictionary<string, string?> { ["Admin:Emails"] = adminEmails, ["Telegram:Admins"] = adminIds };
        if (telegram) cfg["Telegram:BotToken"] = "123:ABC";
        var config = new ConfigurationBuilder().AddInMemoryCollection(cfg).Build();
        var api = DispatchProxy.Create<ITelegramApi, RecordingApi>();
        return (new FeedbackService(db, new AdminOptions(config), new TelegramOptions(config), api, NullLogger<FeedbackService>.Instance), db, (RecordingApi)(object)api);
    }

    // ---- Baholar ----

    [Fact]
    public async Task Once_only_ratings_are_not_repeated_for_the_same_user_but_others_are()
    {
        var (svc, db, _) = Make();
        var me = Guid.NewGuid();
        Assert.False(await svc.SaveRatingAsync(me, "first_practice:up", "/#/practice", "uz", "1.2.3.4"));
        Assert.True(await svc.SaveRatingAsync(me, "first_practice:down", null, "uz", null));        // bir marta beriladigan tur — takror
        Assert.False(await svc.SaveRatingAsync(Guid.NewGuid(), "first_practice:up", null, "uz", null));
        Assert.False(await svc.SaveRatingAsync(me, "ai_fair:speaking:fair", null, "uz", null));      // har natija uchun alohida
        Assert.False(await svc.SaveRatingAsync(me, "ai_fair:speaking:fair", null, "uz", null));
        Assert.False(await svc.SaveRatingAsync(null, "source:friend", null, "ru", null));            // mehmon ham baholaydi
        Assert.Equal(5, await db.Feedbacks.CountAsync());
        var first = await db.Feedbacks.OrderBy(f => f.CreatedAtUtc).FirstAsync();
        Assert.Equal(RatingRules.Kind, first.Kind);
        Assert.Equal("1.2.3.4", first.Ip);
    }

    [Fact]
    public async Task Rating_summary_groups_by_key_inside_the_window_and_clamps_days()
    {
        var (svc, db, _) = Make();
        var now = DateTime.UtcNow;
        db.Feedbacks.AddRange(
            new Feedback { Id = Guid.NewGuid(), Kind = RatingRules.Kind, Message = "lesson:up", Lang = "uz", CreatedAtUtc = now.AddDays(-1) },
            new Feedback { Id = Guid.NewGuid(), Kind = RatingRules.Kind, Message = "lesson:up", Lang = "uz", CreatedAtUtc = now.AddDays(-2) },
            new Feedback { Id = Guid.NewGuid(), Kind = RatingRules.Kind, Message = "lesson:down", Lang = "uz", CreatedAtUtc = now.AddDays(-40) },   // oynadan tashqari
            new Feedback { Id = Guid.NewGuid(), Kind = "bug", Message = "lesson:up", Lang = "uz", CreatedAtUtc = now });                            // boshqa tur
        await db.SaveChangesAsync();
        var (days, items) = await svc.RatingSummaryAsync(30);
        Assert.Equal(30, days);
        var only = Assert.Single(items);
        Assert.Equal(("lesson:up", 2), (only.Key, only.Count));
        Assert.Equal(365, (await svc.RatingSummaryAsync(99999)).Days);
        Assert.Equal(1, (await svc.RatingSummaryAsync(-5)).Days);
    }

    // ---- Fikr ----

    [Fact]
    public async Task Feedback_is_normalized_and_saved()
    {
        var (svc, db, _) = Make();
        var user = new User { Id = Guid.NewGuid(), Email = "a@site.uz" };
        var saved = await svc.SaveFeedbackAsync(user, " IDEA ", "  Add dark mode  ", "/#/profile", "en", "9.9.9.9");
        var row = await db.Feedbacks.SingleAsync();
        Assert.Equal(saved.Id, row.Id);
        Assert.Equal("idea", row.Kind);
        Assert.Equal("Add dark mode", row.Message);
        Assert.Equal(user.Id, row.UserId);
        Assert.Equal("en", row.Lang);
        Assert.Null((await svc.SaveFeedbackAsync(null, "bug", "oops", null, "uz", null)).UserId);   // mehmon
    }

    [Fact]
    public async Task Admins_are_not_notified_when_the_bot_is_off()
    {
        var (svc, _, api) = Make(telegram: false, adminIds: "123456789");
        await svc.NotifyAdminsAsync(new Feedback { Id = Guid.NewGuid(), Kind = "bug", Message = "x", Lang = "uz" }, null);
        await Task.Delay(100);
        Assert.Empty(api.Sent);
    }

    [Fact]
    public async Task Admins_get_a_short_telegram_message_with_the_sender()
    {
        var (svc, db, api) = Make(telegram: true, adminEmails: "boss@site.uz", adminIds: "555");
        var boss = new User { Id = Guid.NewGuid(), Email = "boss@site.uz" };
        db.Users.Add(boss);
        db.TelegramAccounts.Add(new TelegramAccount { ChatId = 777, UserId = boss.Id, Lang = "uz" });
        await db.SaveChangesAsync();
        var sender = new User { Id = Guid.NewGuid(), Email = "user@site.uz", DisplayName = "Ali" };

        await svc.NotifyAdminsAsync(new Feedback { Id = Guid.NewGuid(), Kind = "bug", Message = "Button <b> broken", Page = "/#/mock", Lang = "uz" }, sender);
        await api.First.Task.WaitAsync(TimeSpan.FromSeconds(3));
        await Task.Delay(100);

        lock (api.Sent)
        {
            Assert.Equal([555L, 777L], api.Sent.Select(s => s.ChatId).OrderBy(x => x).ToArray());   // Telegram:Admins + ulangan admin hisobi
            Assert.All(api.Sent, s =>
            {
                Assert.Contains("user@site.uz", s.Html);
                Assert.Contains("&lt;b&gt;", s.Html);                                                  // HTML qochirilgan
            });
        }
    }

    [Fact]
    public async Task Admin_list_hides_ratings_orders_newest_first_and_shows_the_sender_contact()
    {
        var (svc, db, _) = Make();
        var u = new User { Id = Guid.NewGuid(), Email = "user@site.uz" };
        db.Users.Add(u);
        db.TelegramAccounts.Add(new TelegramAccount { ChatId = 1, UserId = u.Id, Username = "ali" });
        var t = DateTime.UtcNow;
        db.Feedbacks.AddRange(
            new Feedback { Id = Guid.NewGuid(), UserId = u.Id, Kind = "idea", Message = "older", Lang = "uz", CreatedAtUtc = t.AddMinutes(-5) },
            new Feedback { Id = Guid.NewGuid(), Kind = "bug", Message = "newer", Lang = "uz", CreatedAtUtc = t },
            new Feedback { Id = Guid.NewGuid(), Kind = RatingRules.Kind, Message = "lesson:up", Lang = "uz", CreatedAtUtc = t });
        await db.SaveChangesAsync();

        var list = await svc.RecentFeedbackAsync(50);
        Assert.Equal(["newer", "older"], list.Select(x => x.Message));
        Assert.Null(list[0].UserEmail);                                  // mehmon
        Assert.Equal("user@site.uz · @ali", list[1].UserEmail);          // email + Telegram
        Assert.Single(await svc.RecentFeedbackAsync(1));
    }
}
