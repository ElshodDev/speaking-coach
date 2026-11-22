using System.Text.Json;
using Microsoft.EntityFrameworkCore;
namespace SpeakingCoach.Api.Tests;

/// <summary>TalkSessions: suhbat holatini saqlash va yakunlash (xotiradagi baza bilan). CleanStaleAsync Postgres'ga xos ExecuteDelete'ni ishlatadi — shu sababli bu yerda tekshirilmaydi.</summary>
public class TalkSessionsTests
{
    private static (TalkSessions Svc, AppDbContext Db) Make()
    {
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        return (new TalkSessions(db, new ReviewService(db)), db);
    }

    private static TalkScenario Scenario() => Talk.Scenarios[0];
    private static TalkState NewState(Guid user) => new(Scenario().Id, "B1", user, [new TalkLine("ai", Scenario().Opening)], []);

    [Fact]
    public async Task Start_stores_the_state_and_only_the_owner_can_load_it()
    {
        var (svc, db) = Make();
        var me = Guid.NewGuid();
        var id = await svc.StartAsync(NewState(me));
        var row = await db.PendingExercises.SingleAsync();
        Assert.Equal(ActivityType.Conversation, row.Type);

        var mine = await svc.LoadAsync(id, me);
        Assert.NotNull(mine);
        Assert.Equal(Scenario().Id, mine!.State.Scenario);
        Assert.Equal("B1", mine.State.Level);
        Assert.Null(await svc.LoadAsync(id, Guid.NewGuid()));     // begona suhbat ko'rinmaydi
        Assert.Null(await svc.LoadAsync(Guid.NewGuid(), me));
    }

    [Fact]
    public async Task Load_ignores_other_kinds_of_pending_exercises_and_corrupt_payloads()
    {
        var (svc, db) = Make();
        var me = Guid.NewGuid();
        var reading = new PendingExercise { Id = Guid.NewGuid(), Type = ActivityType.Reading, Payload = "{}", CreatedAtUtc = DateTime.UtcNow };
        var broken = new PendingExercise { Id = Guid.NewGuid(), Type = ActivityType.Conversation, Payload = "not json", CreatedAtUtc = DateTime.UtcNow };
        db.PendingExercises.AddRange(reading, broken);
        await db.SaveChangesAsync();
        Assert.Null(await svc.LoadAsync(reading.Id, me));
        Assert.Null(await svc.LoadAsync(broken.Id, me));
    }

    [Fact]
    public async Task A_saved_turn_is_visible_on_the_next_load()
    {
        var (svc, _) = Make();
        var me = Guid.NewGuid();
        var id = await svc.StartAsync(NewState(me));
        var talk = (await svc.LoadAsync(id, me))!;
        talk.State.Lines.Add(new TalkLine("user", "I would like a table."));
        talk.State.Lines.Add(new TalkLine("ai", "Of course!"));
        await svc.SaveTurnAsync(talk.Row, talk.State);

        var again = (await svc.LoadAsync(id, me))!;
        Assert.Equal(3, again.State.Lines.Count);
        Assert.Equal(1, Talk.UserTurns(again.State));
    }

    [Fact]
    public async Task Finish_writes_the_activity_removes_the_pending_state_and_creates_cards()
    {
        var (svc, db) = Make();
        var me = Guid.NewGuid();
        var id = await svc.StartAsync(NewState(me));
        var talk = (await svc.LoadAsync(id, me))!;
        var summary = new TalkSummary("Good job", "fluent", [], ["table for two"], "past tense");
        var fixes = new List<CorrectionItem> { new("I goed", "I went", "go → went") };

        var done = await svc.FinishAsync(me, talk, Scenario(), summary, fixes, null);
        Assert.NotNull(done);
        Assert.Equal(1, done!.Value.NewCards);

        Assert.Empty(db.PendingExercises);
        var act = await db.Activities.SingleAsync();
        Assert.Equal(done.Value.ActivityId, act.Id);
        Assert.Equal(ActivityType.Conversation, act.Type);
        using var result = JsonDocument.Parse(act.ResponseData);
        Assert.Equal("Good job", result.RootElement.GetProperty("summary").GetString());
        Assert.Equal("I went", result.RootElement.GetProperty("topCorrections")[0].GetProperty("corrected").GetString());   // xulosa tuzatishlari almashtirildi
        Assert.Equal(1, await db.ReviewCards.CountAsync(c => c.UserId == me));
        Assert.Null(await svc.LoadAsync(id, me));                                                                           // yakunlangan suhbat yo'q
    }
}
