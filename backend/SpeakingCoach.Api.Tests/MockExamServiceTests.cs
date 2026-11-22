using System.Text.Json;
using Microsoft.EntityFrameworkCore;
namespace SpeakingCoach.Api.Tests;

/// <summary>
/// MockExamService: endpointdan ko'chirilgan mantiq uchun xarakter testlar (haqiqiy Postgres'siz, xotiradagi baza bilan).
/// Postgres'ga xos jsonb so'rovi (IMockRows) soxta ro'yxat bilan almashtirilgan.
/// </summary>
public class MockExamServiceTests
{
    private sealed class FakeRows : IMockRows
    {
        public List<MockQueries.MockRow> Rows { get; } = [];
        public Task<List<MockQueries.MockRow>> ForUserAsync(Guid userId, int take, CancellationToken ct = default) =>
            Task.FromResult(Rows.OrderByDescending(r => r.CreatedAtUtc).Take(take).ToList());
    }

    private static (MockExamService Svc, AppDbContext Db, FakeRows Rows) Make()
    {
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var rows = new FakeRows();
        return (new MockExamService(db, rows, new MockSets(db), new ReviewService(db)), db, rows);
    }

    private const string Passage = "The tour starts at the main gate and takes two hours.";
    private static string ReadingPayload() => JsonSerializer.Serialize(new ReadingTest("academic",
    [
        new ReadingPassage("Tours", Passage,
        [
            new QuestionGroup("gap", "Write NO MORE THAN TWO WORDS.", 2, [new ObjQuestion(1, "Meet at the ___", null, ["main gate"], "because")]),
        ]),
    ]), GeminiMockGenerator.Web);

    private static MockTest Published(string exam, string module, string variant, DateTime at, string? payload = null) =>
        new() { Id = Guid.NewGuid(), Exam = exam, Module = module, Variant = variant, Status = MockTestStatus.Published, CreatedAtUtc = at, Payload = payload ?? ReadingPayload() };

    private static MockQueries.MockRow Row(string module, string setId, DateTime at, string exam = "ielts", string? session = null, decimal? overall = null) =>
        new() { Id = Guid.NewGuid(), CreatedAtUtc = at, Exam = exam, Module = module, SetId = setId, SessionId = session, Overall = overall };

    // ---- Bank tanlash ----

    [Fact]
    public async Task Pick_prefers_a_fresh_test_then_a_chosen_one_then_reports_exhausted()
    {
        var (svc, db, rows) = Make();
        var user = Guid.NewGuid();
        var t1 = Published("ielts", "reading", "academic", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var t2 = Published("ielts", "reading", "academic", new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc));
        db.MockTests.AddRange(t1, t2);
        await db.SaveChangesAsync();

        // Hech narsa ishlanmagan: eng birinchi (eng eski) test.
        var first = await svc.PickFromBankAsync(user, "ielts", "reading", "academic", "bank", null);
        Assert.Equal(t1.Id, first.TestId);
        Assert.False(first.Repeated);
        Assert.Equal(2, first.BankCount);

        // t1 ishlangan: yangisi — t2.
        rows.Rows.Add(Row("reading", t1.Id.ToString(), DateTime.UtcNow.AddHours(-2)));
        Assert.Equal(t2.Id, (await svc.PickFromBankAsync(user, "ielts", "reading", "academic", "bank", null)).TestId);

        // Foydalanuvchi ishlangan testni o'zi tanladi: repeated = true.
        var chosen = await svc.PickFromBankAsync(user, "ielts", "reading", "academic", "bank", t1.Id.ToString());
        Assert.Equal(t1.Id, chosen.TestId);
        Assert.True(chosen.Repeated);

        // Ikkalasi ham ishlangan: "bank" — bo'sh natija, "repeat" — eng uzoq vaqt oldin ishlangani (t1 eskiroq).
        rows.Rows.Add(Row("reading", t2.Id.ToString(), DateTime.UtcNow.AddHours(-1)));
        var none = await svc.PickFromBankAsync(user, "ielts", "reading", "academic", "bank", null);
        Assert.Null(none.TestId);
        Assert.Equal(2, none.BankCount);
        var again = await svc.PickFromBankAsync(user, "ielts", "reading", "academic", "repeat", null);
        Assert.Equal(t1.Id, again.TestId);
        Assert.True(again.Repeated);
    }

    [Fact]
    public async Task Pick_ignores_unpublished_tests_and_other_variants()
    {
        var (svc, db, _) = Make();
        var draft = Published("ielts", "reading", "academic", DateTime.UtcNow);
        draft.Status = MockTestStatus.Personal;
        db.MockTests.AddRange(draft, Published("ielts", "reading", "general", DateTime.UtcNow));
        await db.SaveChangesAsync();
        var r = await svc.PickFromBankAsync(Guid.NewGuid(), "ielts", "reading", "academic", "bank", null);
        Assert.Null(r.TestId);
        Assert.Equal(0, r.BankCount);
    }

    [Fact]
    public async Task Bank_lists_built_tests_with_done_flags_and_last_score()
    {
        var (svc, db, rows) = Make();
        var user = Guid.NewGuid();
        var t1 = Published("ielts", "reading", "academic", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var t2 = Published("ielts", "reading", "academic", new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc));
        db.MockTests.AddRange(t1, t2);
        await db.SaveChangesAsync();
        rows.Rows.Add(Row("reading", t1.Id.ToString(), DateTime.UtcNow.AddHours(-1), overall: 6.5m));
        rows.Rows.Add(Row("reading", Guid.NewGuid().ToString(), DateTime.UtcNow, exam: "cefr", overall: 40));   // boshqa imtihon — hisobga olinmaydi

        var bank = await svc.BankAsync(user, "ielts", "reading", general: false);
        Assert.Equal(2, bank.Total);
        Assert.Equal(1, bank.Done);
        Assert.Equal(2, bank.Items.Count);
        Assert.True(bank.Items[0].Done);
        Assert.False(bank.Items[1].Done);
    }

    // ---- Listening/Reading javobini qabul qilish ----

    [Fact]
    public async Task Submit_rejects_an_unknown_test()
    {
        var (svc, _, _) = Make();
        var r = await svc.SubmitObjectiveAsync(Guid.NewGuid(), "ielts", "reading", Guid.NewGuid(), new() { ["1"] = "x" }, 10, null);
        Assert.False(r.Ok);
        Assert.Equal("mock.bad_set", r.Failure!.Key);
        Assert.Equal(400, r.Failure.Status);
    }

    [Fact]
    public async Task Submit_saves_once_and_returns_the_same_result_for_an_identical_resend()
    {
        var (svc, db, _) = Make();
        var user = Guid.NewGuid();
        var t = Published("ielts", "reading", "academic", DateTime.UtcNow);
        db.MockTests.Add(t);
        await db.SaveChangesAsync();

        var first = await svc.SubmitObjectiveAsync(user, "ielts", "reading", t.Id, new() { ["1"] = "Main gate" }, 120, null);
        Assert.True(first.Ok);
        Assert.False(first.Value!.Duplicate);
        var saved = await db.Activities.SingleAsync();
        Assert.Equal(ActivityType.MockExam, saved.Type);
        Assert.Equal(user, saved.UserId);
        Assert.Contains("\"setId\":\"" + t.Id + "\"", saved.PromptData);
        Assert.Contains("\"exam\":\"ielts\"", saved.PromptData);

        // Xuddi shunday javoblar qayta yuborildi: yangi qator yozilmaydi.
        var again = await svc.SubmitObjectiveAsync(user, "ielts", "reading", t.Id, new() { ["1"] = "Main gate" }, 130, null);
        Assert.True(again.Ok);
        Assert.True(again.Value!.Duplicate);
        Assert.Equal(first.Value.Id, again.Value.Id);
        Assert.Equal(1, await db.Activities.CountAsync());
    }

    [Fact]
    public async Task Submit_cleans_the_answers_and_the_session_id()
    {
        var (svc, db, _) = Make();
        var user = Guid.NewGuid();
        var t = Published("ielts", "reading", "academic", DateTime.UtcNow);
        db.MockTests.Add(t);
        await db.SaveChangesAsync();
        var session = Guid.NewGuid();
        var r = await svc.SubmitObjectiveAsync(user, "ielts", "reading", t.Id,
            new() { ["1"] = "main gate", ["toolong"] = "ignored" }, 99999, session.ToString());
        Assert.True(r.Ok);
        var saved = await db.Activities.SingleAsync();
        Assert.Contains(session.ToString("N"), saved.PromptData);
        using var result = JsonDocument.Parse(saved.ResponseData);
        Assert.Equal(3 * 60 * 60, result.RootElement.GetProperty("secondsUsed").GetInt32());   // 3 soatga qisqartirildi
    }

    // ---- Holat, sessiya, tarix, bitta natija ----

    [Fact]
    public async Task Status_counts_only_ai_assessed_attempts_of_the_last_day_and_skips_admins()
    {
        var (svc, _, rows) = Make();
        var user = Guid.NewGuid();
        var now = DateTime.UtcNow;
        rows.Rows.Add(Row("speaking", "s1", now.AddHours(-1)));
        rows.Rows.Add(Row("writing", "w1", now.AddHours(-2)));
        rows.Rows.Add(Row("reading", "r1", now.AddHours(-3)));       // AI'siz — limitga kirmaydi
        rows.Rows.Add(Row("speaking", "s0", now.AddHours(-30)));     // 24 soatdan oldin
        var st = await svc.StatusAsync(user, 4, isAdmin: false);
        Assert.Equal(4, st.PerDay);
        Assert.Equal(2, st.Remaining);

        var admin = await svc.StatusAsync(user, 4, isAdmin: true);
        Assert.Null(admin.Remaining);
        Assert.Null(admin.RetryAtUtc);
    }

    [Fact]
    public async Task Session_collects_the_four_modules_and_rounds_the_ielts_overall()
    {
        var (svc, _, rows) = Make();
        var sid = Guid.NewGuid().ToString("N");
        var t = DateTime.UtcNow;
        rows.Rows.Add(Row("listening", "a", t.AddMinutes(-4), session: sid, overall: 6.5m));
        rows.Rows.Add(Row("reading", "b", t.AddMinutes(-3), session: sid, overall: 6.5m));
        rows.Rows.Add(Row("writing", "c", t.AddMinutes(-2), session: sid, overall: 5.0m));
        rows.Rows.Add(Row("speaking", "d", t.AddMinutes(-1), session: sid, overall: 7.0m));
        rows.Rows.Add(Row("speaking", "x", t, session: Guid.NewGuid().ToString("N"), overall: 9m));   // boshqa sessiya

        var s = await svc.SessionAsync(Guid.NewGuid(), sid);
        Assert.Equal("ielts", s.Exam);
        Assert.Equal(4, s.Modules.Count);
        Assert.Equal(6.5m, s.Overall);   // 6.25 → 6.5 (rasmiy yaxlitlash)
        Assert.Null(s.Level);

        // Bitta modul yetishmasa — umumiy ball yo'q.
        var partial = await svc.SessionAsync(Guid.NewGuid(), Guid.NewGuid().ToString("N"));
        Assert.Empty(partial.Modules);
        Assert.Null(partial.Overall);
    }

    [Fact]
    public async Task History_lists_the_newest_first_with_the_prompt_fields()
    {
        var (svc, _, rows) = Make();
        rows.Rows.Add(Row("reading", "old", DateTime.UtcNow.AddDays(-2), overall: 5.5m));
        rows.Rows.Add(Row("writing", "new", DateTime.UtcNow, exam: "cefr", overall: 55));
        var h = await svc.HistoryAsync(Guid.NewGuid());
        Assert.Equal(["new", "old"], h.Select(x => x.SetId));
        Assert.Equal("cefr", h[0].Exam);
        Assert.Equal("writing", h[0].Module);
        Assert.Equal(5.5m, h[1].Overall);
    }

    [Fact]
    public async Task Result_returns_the_stored_attempt_with_its_test_only_to_the_owner()
    {
        var (svc, db, _) = Make();
        var owner = Guid.NewGuid();
        var t = Published("ielts", "reading", "academic", DateTime.UtcNow);
        db.MockTests.Add(t);
        await db.SaveChangesAsync();
        var submitted = await svc.SubmitObjectiveAsync(owner, "ielts", "reading", t.Id, new() { ["1"] = "main gate" }, 60, null);
        var id = submitted.Value!.Id;

        var mine = await svc.ResultAsync(owner, id);
        Assert.NotNull(mine);
        Assert.Equal(id, mine!.Id);
        Assert.NotNull(mine.Test);                                    // Listening/Reading: testning o'zi ham qaytadi
        Assert.Equal(JsonValueKind.Object, mine.Result.ValueKind);

        Assert.Null(await svc.ResultAsync(Guid.NewGuid(), id));       // boshqa foydalanuvchi ko'rmaydi
        Assert.Null(await svc.ResultAsync(owner, Guid.NewGuid()));
    }

    [Fact]
    public async Task Generated_tests_are_stored_and_counted_per_user()
    {
        var (svc, db, _) = Make();
        var user = Guid.NewGuid();
        var id = await svc.StoreGeneratedAsync("ielts", "listening", "", ReadingPayload(), user);
        var row = await db.MockTests.SingleAsync(x => x.Id == id);
        Assert.Equal(user, row.CreatedByUserId);
        Assert.Equal(1, await svc.GeneratedSinceAsync(user, DateTime.UtcNow.AddHours(-24)));
        Assert.Equal(0, await svc.GeneratedSinceAsync(Guid.NewGuid(), DateTime.UtcNow.AddHours(-24)));
        Assert.Equal(0, await svc.GeneratedSinceAsync(user, DateTime.UtcNow.AddMinutes(1)));
    }
}
