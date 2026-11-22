using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace SpeakingCoach.Application.Mock;

public record RecentMock(DateTime At, string Module, string SetId);
public record MockStatus(int PerDay, int? Remaining, DateTime? RetryAtUtc);
public record BankView(List<CatalogEntry> Items, int Done, int Total);
/// <summary>Bankdan tanlangan test: TestId bo'sh bo'lsa — hammasi ishlangan (yoki bank bo'sh), BankCount — bankdagi testlar soni.</summary>
public record ObjectivePick(Guid? TestId, string? Payload, bool Repeated, int BankCount);
public record ObjectiveSubmitted(Guid Id, bool Duplicate);
public record SessionModule(Guid Id, DateTime CreatedAtUtc, decimal? Overall, string? Variant);
public record MockSessionView(string SessionId, string Exam, Dictionary<string, SessionModule> Modules, object? Overall, string? Level);
public record MockHistoryItem(Guid Id, DateTime CreatedAtUtc, string Exam, string Module, string? Variant, string SetId, string? SessionId, decimal? Overall);
public record MockResultView(Guid Id, DateTime CreatedAtUtc, string? SessionId, JsonElement Result, JsonElement? Test);

/// <summary>
/// Mock imtihonlar: urinishlar tarixi, test banki, Listening/Reading javoblarini qabul qilish, natijalarni saqlash.
/// HTTP, audio yuklash, AI chaqiruvi va sutkalik limit "joyi" (AiInFlight) — Api'dagi MockEndpoints'da.
/// </summary>
public class MockExamService(IAppDbContext db, IMockRows rows, MockSets sets, ReviewService reviews)
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    // ---------------- Urinishlar ----------------

    /// <summary>Oxirgi urinishlar (eng yangisi birinchi): vaqt, bo'lim va test id.</summary>
    public async Task<List<RecentMock>> RecentAsync(Guid userId) =>
        (await rows.ForUserAsync(userId, 300)).Select(r => new RecentMock(r.CreatedAtUtc, r.Module ?? "", r.SetId ?? "")).ToList();

    /// <summary>Bitta bo'lim bo'yicha ishlangan test id'lari (eng yangisi birinchi).</summary>
    public async Task<List<string>> RecentSetIdsAsync(Guid userId, string module) =>
        (await RecentAsync(userId)).Where(r => r.Module == module).Select(r => r.SetId).ToList();

    /// <summary>AI baholaydigan (Speaking/Writing) urinishlarning boshlanish vaqtlari — sutkalik limit uchun.</summary>
    public async Task<List<DateTime>> AiStartsAsync(Guid userId) =>
        (await RecentAsync(userId)).Where(r => MockLimit.IsAiAssessed(r.Module)).Select(r => r.At).ToList();

    /// <summary>Holat: nechta imtihon qoldi (sahifa tepasida ko'rsatiladi). Admin uchun cheklov yo'q.</summary>
    public async Task<MockStatus> StatusAsync(Guid userId, int perDay, bool isAdmin)
    {
        if (isAdmin) return new MockStatus(perDay, null, null);
        var starts = await AiStartsAsync(userId);
        var now = DateTime.UtcNow;
        var used = starts.Count(t => t > now.AddHours(-24));
        var (_, retryAt) = MockLimit.Check(starts, perDay, now);
        return new MockStatus(perDay, Math.Max(0, perDay - used), retryAt);
    }

    // ---------------- Ro'yxat (bank) ----------------

    /// <summary>Barcha testlar/variantlar, qaysi biri ishlangan va oxirgi natija. Ilova bilan keladigan testlar oldindan bankka solingan bo'lishi kerak.</summary>
    public async Task<BankView> BankAsync(Guid userId, string exam, string module, bool general)
    {
        var attempts = (await rows.ForUserAsync(userId, 300))
            .Where(r => (r.Exam ?? "ielts") == exam && r.Module == module)
            .Select(r => new CatalogAttempt(r.SetId ?? "", r.CreatedAtUtc, r.Overall))
            .ToList();
        var latest = MockCatalog.Latest(attempts);
        List<CatalogEntry> items;
        if (module is "listening" or "reading")
        {
            var v = exam == "ielts" && module == "reading" ? (general ? IeltsBank.General : IeltsBank.Academic) : "";
            var found = await db.MockTests
                .Where(t => t.Exam == exam && t.Module == module && t.Variant == v && t.Status == MockTestStatus.Published)
                .OrderBy(t => t.CreatedAtUtc)
                .Select(t => new { t.Id, t.Title, t.Payload })
                .ToListAsync();
            items = found.Select((r, i) => MockCatalog.Entry(
                r.Id.ToString(), i + 1,
                // Tayyor testlar "Test N" deb ko'rsatiladi; bot orqali qo'shilganlarning o'z nomi bor.
                BuiltInMocks.IsBuiltIn(r.Id) ? null : r.Title,
                MockCatalog.Topics(module, r.Payload), r.Title is null, latest)).ToList();
        }
        else
        {
            items = (exam, module) switch
            {
                ("ielts", "speaking") => (await sets.IeltsSpeakingAsync()).Select((s, i) => MockCatalog.Entry(s.Id, i + 1, null, MockCatalog.Topics(s), false, latest)).ToList(),
                ("ielts", _) => (await sets.IeltsWritingAsync(general ? IeltsBank.General : IeltsBank.Academic)).Select((s, i) => MockCatalog.Entry(s.Id, i + 1, null, MockCatalog.Topics(s), false, latest)).ToList(),
                ("cefr", "speaking") => (await sets.CefrSpeakingAsync()).Select((s, i) => MockCatalog.Entry(s.Id, i + 1, null, MockCatalog.Topics(s), false, latest)).ToList(),
                _ => (await sets.CefrWritingAsync()).Select((s, i) => MockCatalog.Entry(s.Id, i + 1, null, MockCatalog.Topics(s), false, latest)).ToList(),
            };
        }
        return new BankView(items, items.Count(x => x.Done), items.Count);
    }

    // ---------------- Listening / Reading ----------------

    /// <summary>
    /// Bankdan test tanlash (AI'siz): foydalanuvchi tanlagan aniq test, yoki hali ishlanmagani;
    /// source "repeat" bo'lsa va hammasi ishlangan bo'lsa — eng uzoq vaqt oldin ishlangani. Hech biri bo'lmasa TestId bo'sh.
    /// </summary>
    public async Task<ObjectivePick> PickFromBankAsync(Guid userId, string exam, string module, string variant, string source, string? testId)
    {
        var done = (await RecentSetIdsAsync(userId, module)).ToHashSet();
        // Avval faqat Id'lar (yengil), keyin tanlangan bitta testning matni.
        var candidates = await db.MockTests
            .Where(t => t.Exam == exam && t.Module == module && t.Variant == variant && t.Status == MockTestStatus.Published)
            .OrderBy(t => t.CreatedAtUtc)
            .Select(t => t.Id)
            .ToListAsync();
        var freshId = candidates.Cast<Guid?>().FirstOrDefault(t => !done.Contains(t!.Value.ToString()));

        async Task<ObjectivePick> Picked(Guid id, bool repeated) =>
            new(id, await db.MockTests.Where(t => t.Id == id).Select(t => t.Payload).FirstAsync(), repeated, candidates.Count);

        // Foydalanuvchi ro'yxatdan aniq testni tanladi.
        if (Guid.TryParse(testId, out var picked) && candidates.Contains(picked))
            return await Picked(picked, done.Contains(picked.ToString()));
        if (freshId is { } fid) return await Picked(fid, false);
        if (source == "repeat" && candidates.Count > 0)
        {
            // Eng uzoq vaqt oldin ishlangani (RecentSetIdsAsync — eng yangisi birinchi).
            var order = await RecentSetIdsAsync(userId, module);
            var oldest = candidates.OrderByDescending(id => order.IndexOf(id.ToString())).First();
            return await Picked(oldest, true);
        }
        // Bankdagi hammasi ishlangan (yoki bank bo'sh) — foydalanuvchi tanlaydi: qayta yoki AI.
        return new ObjectivePick(null, null, false, candidates.Count);
    }

    /// <summary>Oxirgi 24 soatda foydalanuvchi AI bilan yaratgan (nomsiz) testlar soni — sutkalik yaratish limiti uchun.</summary>
    public Task<int> GeneratedSinceAsync(Guid userId, DateTime sinceUtc) =>
        db.MockTests.CountAsync(t => t.CreatedByUserId == userId && t.CreatedAtUtc > sinceUtc && t.Title == null);

    /// <summary>AI yaratgan testni bankka saqlaydi.</summary>
    public async Task<Guid> StoreGeneratedAsync(string exam, string module, string variant, string payload, Guid userId)
    {
        var id = Guid.NewGuid();
        db.MockTests.Add(new MockTest { Id = id, Exam = exam, Module = module, Variant = variant, Payload = payload, CreatedByUserId = userId, CreatedAtUtc = DateTime.UtcNow });
        await db.SaveChangesAsync(CancellationToken.None);
        return id;
    }

    /// <summary>
    /// Listening/Reading javoblarini tekshirib, natijani saqlaydi. XP yig'ishga qarshi: aynan shu javoblar yaqinda
    /// yuborilgan bo'lsa — o'sha natija (ikki marta bosish); kunlik chegaradan keyin — xato.
    /// </summary>
    public async Task<Outcome<ObjectiveSubmitted>> SubmitObjectiveAsync(Guid userId, string exam, string module, Guid testId,
        Dictionary<string, string>? rawAnswers, int secondsUsed, string? rawSessionId)
    {
        var test = await db.MockTests.FirstOrDefaultAsync(t => t.Id == testId && t.Module == module && t.Exam == exam && t.Status == MockTestStatus.Published);
        if (test is null) return Outcome<ObjectiveSubmitted>.Error("mock.bad_set", 400);

        var answers = (rawAnswers ?? new())
            .Where(kv => kv.Key.Length <= 3 && kv.Value is not null)
            .Take(200)
            .ToDictionary(kv => kv.Key, kv => kv.Value.Length > 200 ? kv.Value[..200] : kv.Value);
        var seconds = Math.Clamp(secondsUsed, 0, 3 * 60 * 60);
        var sessionId = MockLimit.CleanSession(rawSessionId);
        var setId = test.Id.ToString();
        object graded = exam == "cefr" ? MockGrading.GradeCefrObjective(module, test, answers, seconds) : MockGrading.GradeObjective(module, test, answers, seconds);
        var review = graded is CefrObjectiveResult c ? c.Questions : ((ObjectiveResult)graded).Questions;

        // XP yig'ishga qarshi (yangi qator = yangi XP): bugungi natijalar (Toshkent kuni) va oxirgi 10 daqiqa.
        var now = DateTime.UtcNow;
        var dayStart = TashkentTime.DayStartUtc(now);
        var dupSince = now - XpGuard.DuplicateWindow;
        var since = dayStart < dupSince ? dayStart : dupSince;
        var todayRows = await db.Activities
            .Where(a => a.UserId == userId && a.Type == ActivityType.MockExam && a.CreatedAtUtc >= since)
            .OrderByDescending(a => a.CreatedAtUtc)
            .Take(200)
            .Select(a => new { a.Id, a.CreatedAtUtc, a.PromptData, Response = a.CreatedAtUtc >= dupSince ? a.ResponseData : null })
            .ToListAsync();
        var recent = todayRows
            .Select(r => (Row: r, P: MockQueries.ParsePrompt(r.PromptData)))
            .Select(x => new XpGuard.RecentResult(x.Row.Id, x.Row.CreatedAtUtc, x.P.SetId, x.P.Module, x.Row.Response))
            .ToList();
        // Aynan shu javoblar yaqinda yuborilgan — o'sha natija (ikki marta bosish yoki qayta yuborish).
        if (XpGuard.FindDuplicate(recent, setId, module, XpGuard.Fingerprint(review), now) is Guid existing)
            return Outcome<ObjectiveSubmitted>.Success(new ObjectiveSubmitted(existing, true));
        var objectiveToday = recent.Count(r => r.CreatedAtUtc >= dayStart && r.Module is "listening" or "reading");
        if (objectiveToday >= XpGuard.ObjectiveMocksPerDay)
            return Outcome<ObjectiveSubmitted>.Error("mock.objective_limit", 429, XpGuard.ObjectiveMocksPerDay);

        var id = graded is CefrObjectiveResult cefr
            ? await SaveResultAsync("cefr", userId, module, setId, null, sessionId, cefr, [], null)
            : await SaveResultAsync("ielts", userId, module, setId, test.Variant.Length == 0 ? null : test.Variant, sessionId, (ObjectiveResult)graded, [], null);
        return Outcome<ObjectiveSubmitted>.Success(new ObjectiveSubmitted(id, false));
    }

    // ---------------- Natijalarni saqlash ----------------

    /// <summary>Natijani tarixga yozadi; tuzatishlar — oddiy mashqlardagidek takrorlash kartalariga (bitta tranzaksiyada).</summary>
    public async Task<Guid> SaveResultAsync<T>(string exam, Guid userId, string module, string setId, string? variant, string? sessionId,
        T result, List<CorrectionItem> corrections, ActivityType? cardSource)
    {
        var id = Guid.NewGuid();
        db.Activities.Add(new Activity
        {
            Id = id,
            Type = ActivityType.MockExam,
            UserId = userId,
            CreatedAtUtc = DateTime.UtcNow,
            PromptData = JsonSerializer.Serialize(new { exam, module, setId, variant, sessionId }, Web),
            ResponseData = JsonSerializer.Serialize(result, Web),
        });
        if (corrections.Count > 0) await reviews.AddCardsAsync(userId, cardSource, ReviewCardFactory.FromCorrections(corrections));
        await db.SaveChangesAsync();
        return id;
    }

    // ---------------- Sessiya, tarix, bitta natija ----------------

    /// <summary>To'liq imtihon (sessiya): 4 ta modul natijasi va umumiy ball.</summary>
    public async Task<MockSessionView> SessionAsync(Guid userId, string sid)
    {
        var all = await rows.ForUserAsync(userId, 300);
        var inSession = all
            .Select(r => new { r.Id, r.CreatedAtUtc, P = r.Prompt, r.Overall })
            .Where(r => r.P.SessionId == sid)
            .ToList();
        var exam = inSession.FirstOrDefault()?.P.Exam ?? "ielts";
        var modules = inSession
            .Where(r => r.P.Exam == exam)
            .GroupBy(r => r.P.Module)
            .Select(g => g.First())
            .ToDictionary(r => r.P.Module, r => new SessionModule(r.Id, r.CreatedAtUtc, r.Overall, r.P.Variant));
        if (exam == "cefr")
        {
            // Rasmiy: daraja 4 bo'lim ballarining o'rtachasi bo'yicha aniqlanadi.
            var scores = FullMock.Modules.Select(m => modules.TryGetValue(m, out var x) ? x.Overall : null).ToList();
            int? total = scores.Any(v => v is null) ? null : CefrObjective.Overall(scores.Select(v => (int)v!.Value).ToList());
            return new MockSessionView(sid, exam, modules, total, total is int t ? CefrScale.Level(t) : null);
        }
        decimal? overall = FullMock.Overall(modules.ToDictionary(kv => kv.Key, kv => kv.Value.Overall));
        return new MockSessionView(sid, exam, modules, overall, null);
    }

    public async Task<List<MockHistoryItem>> HistoryAsync(Guid userId) =>
        (await rows.ForUserAsync(userId, 50))
            .Select(r =>
            {
                var p = r.Prompt;
                return new MockHistoryItem(r.Id, r.CreatedAtUtc, p.Exam, p.Module, p.Variant, p.SetId, p.SessionId, r.Overall);
            })
            .ToList();

    /// <summary>Bitta natija (to'liq). Listening/Reading: natija bilan birga testning o'zi (matn/skript, javoblar) — tahlil uchun.</summary>
    public async Task<MockResultView?> ResultAsync(Guid userId, Guid id)
    {
        var row = await db.Activities.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId && a.Type == ActivityType.MockExam);
        if (row is null) return null;
        using var doc = JsonDocument.Parse(row.ResponseData);
        JsonElement? test = null;
        var prompt = MockQueries.ParsePrompt(row.PromptData);
        if (prompt.Module is "listening" or "reading" && Guid.TryParse(prompt.SetId, out var testId))
        {
            var t = await db.MockTests.FirstOrDefaultAsync(x => x.Id == testId);
            if (t is not null)
            {
                using var tdoc = JsonDocument.Parse(t.Payload);
                test = tdoc.RootElement.Clone();
            }
        }
        return new MockResultView(row.Id, row.CreatedAtUtc, prompt.SessionId, doc.RootElement.Clone(), test);
    }
}
