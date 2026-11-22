using System.Text.Json;
using Microsoft.EntityFrameworkCore;
namespace SpeakingCoach.Api.Tests;

/// <summary>PracticeStore va ReviewService.AddCardsAndSaveAsync: endpointlardan ko'chirilgan saqlash qoidalari (xotiradagi baza bilan).</summary>
public class PracticeStoreTests
{
    private sealed class FakeHistory : IPracticeHistory
    {
        public List<string> Part2 { get; } = [];
        public List<FluencyRow> Fluency { get; } = [];
        public Task<List<string>> DoneTitlesAsync(Guid userId, ActivityType type) => Task.FromResult(new List<string>());
        public Task<List<string>> Part2PromptsAsync(Guid userId) => Task.FromResult(Part2);
        public Task<List<FluencyRow>> FluencyRowsAsync(Guid userId, int take) => Task.FromResult(Fluency.Take(take).ToList());
    }

    private static (PracticeStore Store, AppDbContext Db, FakeHistory History, ReviewService Reviews) Make()
    {
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var history = new FakeHistory();
        var reviews = new ReviewService(db);
        return (new PracticeStore(db, history, reviews), db, history, reviews);
    }

    // ---- Diktant ----

    [Fact]
    public async Task Dictation_saves_the_result_with_accuracy_and_stops_at_the_daily_cap()
    {
        var (store, db, _, _) = Make();
        var me = Guid.NewGuid();
        Assert.True(await store.SaveDictationAsync(me, new DictationInput("b1", 5, 40, 50)));
        var row = await db.Activities.SingleAsync();
        Assert.Equal(ActivityType.Dictation, row.Type);
        using var result = JsonDocument.Parse(row.ResponseData);
        Assert.Equal(80, result.RootElement.GetProperty("accuracy").GetInt32());

        for (var i = 1; i < XpGuard.DictationsPerDay; i++) Assert.True(await store.SaveDictationAsync(me, new DictationInput("A2", 5, 5, 5)));
        Assert.False(await store.SaveDictationAsync(me, new DictationInput("A2", 5, 5, 5)));            // chegaradan keyin saqlanmaydi
        Assert.Equal(XpGuard.DictationsPerDay, await db.Activities.CountAsync());
        Assert.True(await store.SaveDictationAsync(Guid.NewGuid(), new DictationInput("A2", 5, 5, 5)));  // boshqa foydalanuvchiga ta'sir qilmaydi
    }

    // ---- Shadowing ----

    private static ShadowingLesson Lesson(string id, int lines = 3) =>
        new(id, "video", "Title " + id, "A2", null, null, null, [], Enumerable.Range(0, lines).Select(i => new ShadowLine("line " + i)).ToList());

    [Fact]
    public async Task Shadowing_done_is_saved_once_per_lesson_in_20_hours_and_values_are_clamped()
    {
        var (store, db, _, _) = Make();
        var me = Guid.NewGuid();
        var l1 = Lesson("l1");
        Assert.True(await store.SaveShadowingDoneAsync(me, l1, 99, 250));
        Assert.False(await store.SaveShadowingDoneAsync(me, l1, 2, 80));                  // xuddi shu dars — takror
        Assert.True(await store.SaveShadowingDoneAsync(me, Lesson("l2"), 2, null));       // boshqa dars
        Assert.True(await store.SaveShadowingDoneAsync(Guid.NewGuid(), l1, 1, 50));       // boshqa foydalanuvchi

        var mine = (await db.Activities.Where(a => a.UserId == me).ToListAsync()).OrderBy(a => a.PromptData).ToList();
        Assert.Equal(2, mine.Count);
        using var first = JsonDocument.Parse(mine[0].ResponseData);
        Assert.Equal(3, first.RootElement.GetProperty("lines").GetInt32());               // 99 → dars gaplari soniga qisqardi
        Assert.Equal(100, first.RootElement.GetProperty("average").GetInt32());           // 250 → 100
        using var second = JsonDocument.Parse(mine[1].ResponseData);
        Assert.Equal(JsonValueKind.Null, second.RootElement.GetProperty("average").ValueKind);

        var progress = await store.ShadowingProgressAsync(me);
        Assert.Equal(2, progress.Count);
        Assert.Equal(100, progress.Single(p => p.LessonId == "l1").Best);
    }

    // ---- Part 2 ----

    [Fact]
    public async Task Part2_done_cards_come_from_saved_prompts_and_a_submission_is_stored_as_speaking()
    {
        var (store, db, history, _) = Make();
        var me = Guid.NewGuid();
        history.Part2.Add("""{"topic":"t","part2":true,"cardId":"c1"}""");
        history.Part2.Add("""{"topic":"x"}""");
        history.Part2.Add("not json");
        Assert.Equal(["c1"], (await store.Part2DoneAsync(me)).ToArray());

        var card = new Part2Card("c9", "A place", ["where", "why"], "explain");
        var corrections = new List<CorrectionItem> { new("I goed home", "I went home", "go → went") };
        var id = Guid.NewGuid();
        var added = await store.SavePart2Async(me, id, card, """{"x":1}""", corrections);
        Assert.Equal(1, added);
        var row = await db.Activities.SingleAsync(a => a.Id == id);
        Assert.Equal(ActivityType.Speaking, row.Type);
        Assert.Equal("c9", Part2Rules.CardIdOf(row.PromptData));
        Assert.Equal(1, await db.ReviewCards.CountAsync(c => c.UserId == me));
    }

    // ---- Speaking / Writing ----

    [Fact]
    public async Task Attempt_and_its_cards_are_saved_together()
    {
        var (store, db, _, _) = Make();
        var me = Guid.NewGuid();
        var attempt = new Activity { Id = Guid.NewGuid(), Type = ActivityType.Writing, UserId = me, CreatedAtUtc = DateTime.UtcNow, PromptData = "{}", ResponseData = "{}" };
        var n = await store.SaveAttemptAsync(attempt, [new CorrectionItem("He go", "He goes", "3rd person -s")]);
        Assert.Equal(1, n);
        Assert.Equal(1, await db.Activities.CountAsync());
        Assert.Equal(1, await db.ReviewCards.CountAsync());
        Assert.Equal(0, await store.SaveAttemptAsync(new Activity { Id = Guid.NewGuid(), Type = ActivityType.Writing, UserId = me, CreatedAtUtc = DateTime.UtcNow, PromptData = "{}", ResponseData = "{}" }, []));
    }

    [Fact]
    public async Task Fluency_points_are_sorted_oldest_first_with_the_right_source_and_skip_rows_without_wpm()
    {
        var (store, _, history, _) = Make();
        string Resp(string transcript) => FluencyMetrics.WithMetrics(new { ok = true }, FluencyMetrics.Analyze(transcript, 20, 0, 0));
        var t = new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc);
        history.Fluency.Add(new FluencyRow(t.AddDays(2), ActivityType.Conversation, Resp("I like to go to the park every day with my friends")));
        history.Fluency.Add(new FluencyRow(t.AddDays(1), ActivityType.MockExam, Resp("My name is Ali and I live in Tashkent with my family")));
        history.Fluency.Add(new FluencyRow(t, ActivityType.Speaking, Resp("I think English is useful for my future career plans")));
        history.Fluency.Add(new FluencyRow(t.AddDays(3), ActivityType.Speaking, "{}"));      // o'lchovsiz — tashlanadi

        var points = await store.FluencyAsync(Guid.NewGuid(), 20);
        Assert.Equal(["speaking", "mock", "talk"], points.Select(p => p.Source));
        Assert.True(points.SequenceEqual(points.OrderBy(p => p.At)));
        Assert.All(points, p => Assert.NotNull(p.Wpm));
    }

    // ---- Javobni yaxshila ----

    [Fact]
    public async Task Upgrade_only_touches_the_owners_speaking_or_writing_attempts()
    {
        var (store, db, _, _) = Make();
        var me = Guid.NewGuid();
        var mine = new Activity { Id = Guid.NewGuid(), Type = ActivityType.Speaking, UserId = me, CreatedAtUtc = DateTime.UtcNow, PromptData = "{}", ResponseData = "{}" };
        var other = new Activity { Id = Guid.NewGuid(), Type = ActivityType.Speaking, UserId = Guid.NewGuid(), CreatedAtUtc = DateTime.UtcNow, PromptData = "{}", ResponseData = "{}" };
        var quiz = new Activity { Id = Guid.NewGuid(), Type = ActivityType.Dictation, UserId = me, CreatedAtUtc = DateTime.UtcNow, PromptData = "{}", ResponseData = "{}" };
        db.Activities.AddRange(mine, other, quiz);
        await db.SaveChangesAsync();

        Assert.NotNull(await store.OwnAttemptAsync(mine.Id, me));
        Assert.Null(await store.OwnAttemptAsync(other.Id, me));      // begona urinish
        Assert.Null(await store.OwnAttemptAsync(quiz.Id, me));       // boshqa tur
        await store.SaveUpgradeAsync(mine, """{"upgrades":{}}""");
        Assert.Equal("""{"upgrades":{}}""", (await db.Activities.SingleAsync(a => a.Id == mine.Id)).ResponseData);
    }

    // ---- ReviewService.AddCardsAndSaveAsync ----

    [Fact]
    public async Task Cards_are_added_and_saved_once_and_duplicates_add_nothing()
    {
        var (_, db, _, reviews) = Make();
        var me = Guid.NewGuid();
        var card = new NewCard(ReviewCardKind.Word, "reluctant", "istamaydigan", "adj");
        Assert.Equal(1, await reviews.AddCardsAndSaveAsync(me, null, [card]));
        Assert.Equal(1, await db.ReviewCards.CountAsync());
        Assert.Equal(0, await reviews.AddCardsAndSaveAsync(me, null, [card]));      // xuddi shu ibora qayta qo'shilmaydi
        Assert.Equal(1, await db.ReviewCards.CountAsync());
        Assert.Equal(1, await reviews.AddCardsAndSaveAsync(Guid.NewGuid(), null, [card]));   // boshqa foydalanuvchi — o'zida
    }
}
