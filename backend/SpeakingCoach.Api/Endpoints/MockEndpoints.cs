using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SpeakingCoach.Api.Data;
using SpeakingCoach.Api.Services;
using SpeakingCoach.Api.Services.Mock;
using SpeakingCoach.Api.Services.Content;

namespace SpeakingCoach.Api.Endpoints;

public record WritingMockRequest(string SetId, string? Task1, string? Task2, int SecondsUsed, string? SessionId = null);
public record CefrWritingRequest(string SetId, string? Task11, string? Task12, string? Task2, int SecondsUsed, string? SessionId = null);
public record ObjectiveMockRequest(Guid TestId, Dictionary<string, string>? Answers, int SecondsUsed, string? SessionId = null);

/// <summary>
/// Mock imtihon uchun sutkalik cheklov: to'liq imtihon Gemini'ning katta
/// so'rovi (10 daqiqagacha audio), shuning uchun alohida, kichik limit.
/// Oyna — oxirgi 24 soat (kun chegarasi va vaqt zonasidan mustaqil).
/// </summary>
public static class MockLimit
{
    /// <summary>Sutkasiga nechta Speaking/Writing mock (AI baholaydi). To'liq imtihonda ikkalasi bor — 4 ta = 2 ta to'liq imtihon.</summary>
    public static int PerDay(IConfiguration config) =>
        int.TryParse(config["Mock:PerDay"], out var n) && n > 0 ? n : 4;

    /// <summary>Listening/Reading javoblari server tomonida tekshiriladi (AI'siz) — limitga kirmaydi.</summary>
    public static bool IsAiAssessed(string module) => module is "speaking" or "writing";

    /// <summary>Test bankida yangi test qolmaganda — AI bilan yaratish (sutkasiga).</summary>
    public static int GeneratePerDay(IConfiguration config) =>
        int.TryParse(config["Mock:GeneratePerDay"], out var n) && n > 0 ? n : 2;

    /// <summary>Sessiya identifikatori: faqat GUID ko'rinishidagi qator (aks holda e'tiborsiz).</summary>
    public static string? CleanSession(string? s) => Guid.TryParse(s, out var g) ? g.ToString("N") : null;

    public static (bool Allowed, DateTime? RetryAtUtc) Check(IEnumerable<DateTime> startsUtc, int perDay, DateTime nowUtc)
    {
        var window = startsUtc.Where(t => t > nowUtc.AddHours(-24)).OrderBy(t => t).ToList();
        if (window.Count < perDay) return (true, null);
        // Eng eski yozuv 24 soatdan chiqqanda bitta joy bo'shaydi.
        return (false, window[window.Count - perDay].AddHours(24));
    }
}

public static class MockEndpoints
{
    /// <summary>Mock speaking: barcha javoblar birga (IELTS ~14 daqiqagacha nutq) va har bir javob alohida (8 MB).</summary>
    private const int MaxAudioMb = 15;
    private const long MaxSpeakingBodyBytes = MaxAudioMb * Uploads.Mb + Uploads.FormOverheadBytes;

    public static void MapMockEndpoints(this IEndpointRouteBuilder app)
    {
        static IResult LoginFirst(HttpRequest r) => Results.Json(r.Error("mock.login"), statusCode: StatusCodes.Status401Unauthorized);

        async Task<List<(DateTime At, string Module, string SetId)>> RecentAsync(AppDbContext db, Guid userId) =>
            (await MockRowsAsync(db, userId, 300)).Select(r => (r.CreatedAtUtc, r.Module ?? "", r.SetId ?? "")).ToList();

        // Bitta imtihon/bo'lim bo'yicha urinishlar (natija bilan) — ro'yxatda ✓ va oxirgi ball uchun.
        async Task<List<CatalogAttempt>> AttemptsAsync(AppDbContext db, Guid userId, string exam, string module) =>
            (await MockRowsAsync(db, userId, 300))
                .Where(r => (r.Exam ?? "ielts") == exam && r.Module == module)
                .Select(r => new CatalogAttempt(r.SetId ?? "", r.CreatedAtUtc, r.Overall))
                .ToList();

        // null — ruxsat; aks holda 429 javobi (qachon yana mumkinligi bilan).
        async Task<IResult?> LimitAsync(HttpRequest request, AppDbContext db, User user, AdminOptions admins, IConfiguration config)
        {
            if (admins.IsAdmin(user)) return null;
            var perDay = MockLimit.PerDay(config);
            var recent = await RecentAsync(db, user.Id);
            var (allowed, retryAt) = MockLimit.Check(recent.Where(r => MockLimit.IsAiAssessed(r.Module)).Select(r => r.At), perDay, DateTime.UtcNow);
            return allowed ? null : Results.Json(
                new { error = request.T("mock.limit", perDay), retryAtUtc = retryAt },
                statusCode: StatusCodes.Status429TooManyRequests);
        }

        // Holat: nechta imtihon qoldi (sahifa tepasida ko'rsatiladi).
        app.MapGet("/api/mock/status", async (HttpRequest request, AuthService auth, AppDbContext db, AdminOptions admins, IConfiguration config) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return LoginFirst(request);
            var perDay = MockLimit.PerDay(config);
            if (admins.IsAdmin(user)) return Results.Ok(new { perDay, remaining = (int?)null, retryAtUtc = (DateTime?)null });
            var starts = (await RecentAsync(db, user.Id)).Where(r => MockLimit.IsAiAssessed(r.Module)).Select(r => r.At).ToList();
            var now = DateTime.UtcNow;
            var used = starts.Count(t => t > now.AddHours(-24));
            var (_, retryAt) = MockLimit.Check(starts, perDay, now);
            return Results.Ok(new { perDay, remaining = (int?)Math.Max(0, perDay - used), retryAtUtc = retryAt });
        });

        // setId — foydalanuvchi ro'yxatdan tanlagan variant (bo'lmasa — hali ishlanmagani).
        app.MapGet("/api/mock/ielts/speaking/new", async (HttpRequest request, AuthService auth, AppDbContext db, MockSets sets, string? setId) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return LoginFirst(request);
            var recent = (await RecentAsync(db, user.Id)).Where(r => r.Module == "speaking").Select(r => r.SetId).ToList();
            var set = await sets.FindIeltsSpeakingAsync(setId) ?? IeltsBank.PickFresh(await sets.IeltsSpeakingAsync(), s => s.Id, recent, Random.Shared);
            return Results.Ok(new
            {
                set,
                timing = new
                {
                    part1AnswerSeconds = IeltsBank.Part1AnswerSeconds,
                    part2PrepSeconds = IeltsBank.Part2PrepSeconds,
                    part2SpeakSeconds = IeltsBank.Part2SpeakSeconds,
                    part3AnswerSeconds = IeltsBank.Part3AnswerSeconds,
                },
            });
        });

        // setId — sahifa yangilanganda boshlangan imtihonni davom ettirish uchun.
        app.MapGet("/api/mock/ielts/writing/new", async (HttpRequest request, AuthService auth, AppDbContext db, MockSets sets, string? variant, string? setId) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return LoginFirst(request);
            var v = variant == IeltsBank.General ? IeltsBank.General : IeltsBank.Academic;
            var resumed = await sets.FindIeltsWritingAsync(setId);
            var recent = (await RecentAsync(db, user.Id)).Where(r => r.Module == "writing").Select(r => r.SetId).ToList();
            var set = resumed is not null && resumed.Variant == v
                ? resumed
                : IeltsBank.PickFresh(await sets.IeltsWritingAsync(v), w => w.Id, recent, Random.Shared);
            return Results.Ok(new
            {
                set,
                timing = new { minutes = IeltsBank.WritingMinutes, task1MinWords = IeltsBank.Task1MinWords, task2MinWords = IeltsBank.Task2MinWords },
            });
        });

        // Speaking: multipart — setId, har bir javob "a{index}" fayli va "s{index}" (soniya).
        app.MapPost("/api/mock/ielts/speaking", async (
            HttpRequest request, AuthService auth, AppDbContext db, AdminOptions admins, IConfiguration config,
            IIeltsEvaluator evaluator, ReviewService reviews, MockSets sets, ILogger<Program> logger) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return LoginFirst(request);
            if (!request.HasFormContentType) return Results.BadRequest(request.Error("speaking.multipart"));
            if (Uploads.DeclaredTooLarge(request, MaxSpeakingBodyBytes))
                return Results.Json(request.Error("mock.too_big", MaxAudioMb), statusCode: StatusCodes.Status413PayloadTooLarge);

            var ct = request.HttpContext.RequestAborted;
            // "Katta so'rov" joyi — audio xotiraga o'qilishidan oldin.
            using var slot = await Uploads.EnterAsync(ct);
            var form = await request.ReadFormAsync(ct);
            var set = await sets.FindIeltsSpeakingAsync(form["setId"].ToString());
            if (set is null) return Results.BadRequest(request.Error("mock.bad_set"));

            var sizeError = CheckAudioSizes(request, form);
            if (sizeError is not null) return sizeError;

            // Limit — audio xotiraga o'qilishidan oldin.
            var limited = await LimitAsync(request, db, user, admins, config);
            if (limited is not null) return limited;

            var answers = await ReadAnswersAsync(form, set.Questions().Count, ct);
            if (answers.All(a => a.Audio is null)) return Results.BadRequest(request.Error("mock.no_answers"));

            try
            {
                var result = await evaluator.EvaluateSpeakingAsync(set, answers, Texts.LangOf(request), ct);
                var id = await SaveAsync(db, reviews, user.Id, "speaking", set.Id, null, MockLimit.CleanSession(form["sessionId"].ToString()), result, result.TopCorrections, ActivityType.Speaking);
                return Results.Ok(new { id, result });
            }
            catch (Exception ex) when (AiErrors.Handles(ex, request))
            {
                logger.LogError(ex, "IELTS speaking mock baholanmadi");
                return Results.Problem(detail: request.T("ai_unavailable"), statusCode: 502);
            }
        }).RequireRateLimiting(RateLimits.AiPolicy).WithBodyLimit(MaxSpeakingBodyBytes);

        app.MapPost("/api/mock/ielts/writing", async (
            WritingMockRequest body, HttpRequest request, AuthService auth, AppDbContext db, AdminOptions admins,
            IConfiguration config, IIeltsEvaluator evaluator, ReviewService reviews, MockSets sets, ILogger<Program> logger) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return LoginFirst(request);
            var set = await sets.FindIeltsWritingAsync(body.SetId);
            if (set is null) return Results.BadRequest(request.Error("mock.bad_set"));

            var t1 = (body.Task1 ?? "").Trim();
            var t2 = (body.Task2 ?? "").Trim();
            if (t1.Length == 0 && t2.Length == 0) return Results.BadRequest(request.Error("mock.empty_text"));
            const int maxChars = 8000;
            if (t1.Length > maxChars || t2.Length > maxChars) return Results.BadRequest(request.Error("text.too_long", maxChars));

            var limited = await LimitAsync(request, db, user, admins, config);
            if (limited is not null) return limited;

            try
            {
                var seconds = Math.Clamp(body.SecondsUsed, 0, 3 * 60 * 60);
                var result = await evaluator.EvaluateWritingAsync(set, t1, t2, seconds, Texts.LangOf(request), request.HttpContext.RequestAborted);
                var id = await SaveAsync(db, reviews, user.Id, "writing", set.Id, set.Variant, MockLimit.CleanSession(body.SessionId), result, result.TopCorrections, ActivityType.Writing);
                return Results.Ok(new { id, result });
            }
            catch (Exception ex) when (AiErrors.Handles(ex, request))
            {
                logger.LogError(ex, "IELTS writing mock baholanmadi");
                return Results.Problem(detail: request.T("ai_unavailable"), statusCode: 502);
            }
        }).RequireRateLimiting(RateLimits.AiPolicy);

        // ---- Ro'yxat: barcha testlar/variantlar, qaysi biri ishlangan va oxirgi natija ----
        // Foydalanuvchi o'zi tanlaydi (to'liq imtihon esa avtomatik — hali ishlanmaganini oladi).
        app.MapGet("/api/mock/{exam}/{module}/bank", async (
            string exam, string module, HttpRequest request, AuthService auth, AppDbContext db, MockSets sets,
            IServiceScopeFactory scopes, ILogger<Program> logger, string? variant) =>
        {
            if (exam is not ("ielts" or "cefr") || module is not ("listening" or "reading" or "speaking" or "writing")) return Results.NotFound();
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return LoginFirst(request);

            var latest = MockCatalog.Latest(await AttemptsAsync(db, user.Id, exam, module));
            var general = variant == IeltsBank.General;
            List<CatalogEntry> items;
            if (module is "listening" or "reading")
            {
                await BuiltInMocks.EnsureSeededAsync(scopes, logger);
                var v = exam == "ielts" && module == "reading" ? (general ? IeltsBank.General : IeltsBank.Academic) : "";
                var rows = await db.MockTests
                    .Where(t => t.Exam == exam && t.Module == module && t.Variant == v && t.Status == MockTestStatus.Published)
                    .OrderBy(t => t.CreatedAtUtc)
                    .Select(t => new { t.Id, t.Title, t.Payload })
                    .ToListAsync();
                items = rows.Select((r, i) => MockCatalog.Entry(
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
            return Results.Ok(new { items, done = items.Count(x => x.Done), total = items.Count });
        });

        // ---- Listening va Reading: test bankdan (bo'lmasa — AI yaratadi) ----
        // IELTS va CEFR uchun bitta yo'l: /api/mock/{ielts|cefr}/{listening|reading}/new
        // (/api/mock/cefr/speaking/new kabi aniq yo'llar ustun turadi).
        app.MapGet("/api/mock/{exam}/{module}/new", async (
            string exam, string module, HttpRequest request, AuthService auth, AppDbContext db, AdminOptions admins, IConfiguration config,
            IMockGenerator generator, GenerationGate gate, IServiceScopeFactory scopes, ILogger<Program> logger, string? variant, string? source, string? testId) =>
        {
            if (exam is not ("ielts" or "cefr") || module is not ("listening" or "reading")) return Results.NotFound();
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return LoginFirst(request);
            var v = exam == "ielts" && module == "reading" ? (variant == IeltsBank.General ? IeltsBank.General : IeltsBank.Academic) : "";

            // Ilova bilan keladigan tayyor testlar bankda bo'lsin (bir marta).
            await BuiltInMocks.EnsureSeededAsync(scopes, logger);

            var done = (await RecentAsync(db, user.Id)).Where(r => r.Module == module).Select(r => r.SetId).ToHashSet();
            // Avval faqat Id'lar (yengil), keyin tanlangan bitta testning matni.
            var candidates = await db.MockTests
                .Where(t => t.Exam == exam && t.Module == module && t.Variant == v && t.Status == MockTestStatus.Published)
                .OrderBy(t => t.CreatedAtUtc)
                .Select(t => t.Id)
                .ToListAsync();
            var freshId = candidates.Cast<Guid?>().FirstOrDefault(t => !done.Contains(t!.Value.ToString()));

            async Task<string> PayloadOf(Guid testId) =>
                await db.MockTests.Where(t => t.Id == testId).Select(t => t.Payload).FirstAsync();

            // Manbani foydalanuvchi tanlaydi: "bank" (standart) — tayyor testlar,
            // "repeat" — hammasi ishlangan bo'lsa, eng eskisini qayta, "ai" — Gemini yangisini yaratadi.
            var src = (source ?? "bank").ToLowerInvariant();

            // Foydalanuvchi ro'yxatdan aniq testni tanladi.
            if (src != "ai" && Guid.TryParse(testId, out var picked) && candidates.Contains(picked))
                return Results.Ok(new { test = ClientPayload(module, picked, await PayloadOf(picked)), repeated = done.Contains(picked.ToString()), source = "bank" });

            if (src != "ai")
            {
                if (freshId is { } fid)
                    return Results.Ok(new { test = ClientPayload(module, fid, await PayloadOf(fid)), repeated = false, source = "bank" });
                if (src == "repeat" && candidates.Count > 0)
                {
                    // Eng uzoq vaqt oldin ishlangani (RecentAsync — eng yangisi birinchi).
                    var order = (await RecentAsync(db, user.Id)).Where(r => r.Module == module).Select(r => r.SetId).ToList();
                    var oldest = candidates.OrderByDescending(id => order.IndexOf(id.ToString())).First();
                    return Results.Ok(new { test = ClientPayload(module, oldest, await PayloadOf(oldest)), repeated = true, source = "bank" });
                }
                // Bankdagi hammasi ishlangan (yoki bank bo'sh) — foydalanuvchi tanlaydi: qayta yoki AI.
                return Results.Ok(new { test = (object?)null, exhausted = true, bankCount = candidates.Count });
            }

            // ---- AI yangi test yaratadi (sutkalik limit bilan) ----
            var isAdmin = admins.IsAdmin(user);
            var perDay = MockLimit.GeneratePerDay(config);
            if (!isAdmin)
            {
                var since = DateTime.UtcNow.AddHours(-24);
                var made = await db.MockTests.CountAsync(t => t.CreatedByUserId == user.Id && t.CreatedAtUtc > since && t.Title == null);
                if (made >= perDay) return Results.Json(request.Error("mock.generate_limit"), statusCode: StatusCodes.Status429TooManyRequests);
            }

            // Parallel so'rovlar va muvaffaqiyatsiz urinishlar ham hisobda.
            var gateResult = gate.TryEnter(user.Id, isAdmin ? int.MaxValue : perDay * 2 + 2, out var lease);
            if (gateResult == GenerationGate.Outcome.Busy)
                return Results.Json(request.Error("mock.generating"), statusCode: StatusCodes.Status409Conflict);
            if (gateResult == GenerationGate.Outcome.Exhausted)
                return Results.Json(request.Error("mock.generate_limit"), statusCode: StatusCodes.Status429TooManyRequests);

            using (lease)
            {
                try
                {
                    var ct = request.HttpContext.RequestAborted;
                    object content = (exam, module) switch
                    {
                        ("cefr", "reading") => await generator.GenerateCefrReadingAsync(ct),
                        ("cefr", _) => await generator.GenerateCefrListeningAsync(ct),
                        (_, "reading") => await generator.GenerateReadingAsync(v, ct),
                        _ => await generator.GenerateListeningAsync(ct),
                    };
                    var id = Guid.NewGuid();
                    var payload = JsonSerializer.Serialize(content, content.GetType(), GeminiMockGenerator.Web);
                    db.MockTests.Add(new MockTest
                    {
                        Id = id, Exam = exam, Module = module, Variant = v, Payload = payload,
                        CreatedByUserId = user.Id, CreatedAtUtc = DateTime.UtcNow,
                    });
                    await db.SaveChangesAsync(CancellationToken.None);
                    logger.LogInformation("Yangi {Exam} {Module} testi yaratildi: {Id}", exam, module, id);
                    return Results.Ok(new { test = ClientPayload(module, id, payload), repeated = false, source = "ai" });
                }
                catch (AiBusyException)
                {
                    // Gemini'ga yetib bormadi (byudjet/saqlagich/kvota) — urinish sanalmaydi; javob — markaziy 503.
                    gate.Refund(user.Id);
                    throw;
                }
                catch (Exception ex) when (AiErrors.Handles(ex, request))
                {
                    logger.LogError(ex, "{Exam} {Module} testini yaratib bo'lmadi", exam, module);
                    return Results.Problem(detail: request.T("ai_unavailable"), statusCode: 502);
                }
            }
        }).RequireRateLimiting(RateLimits.AiPolicy);

        app.MapPost("/api/mock/{exam}/{module}", async (
            string exam, string module, ObjectiveMockRequest body, HttpRequest request, AuthService auth, AppDbContext db) =>
        {
            if (exam is not ("ielts" or "cefr") || module is not ("listening" or "reading")) return Results.NotFound();
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return LoginFirst(request);
            var test = await db.MockTests.FirstOrDefaultAsync(t => t.Id == body.TestId && t.Module == module && t.Exam == exam && t.Status == MockTestStatus.Published);
            if (test is null) return Results.BadRequest(request.Error("mock.bad_set"));

            var answers = (body.Answers ?? new())
                .Where(kv => kv.Key.Length <= 3 && kv.Value is not null)
                .Take(200)
                .ToDictionary(kv => kv.Key, kv => kv.Value.Length > 200 ? kv.Value[..200] : kv.Value);
            var seconds = Math.Clamp(body.SecondsUsed, 0, 3 * 60 * 60);
            var sessionId = MockLimit.CleanSession(body.SessionId);
            var setId = test.Id.ToString();
            object graded = exam == "cefr" ? GradeCefrObjective(module, test, answers, seconds) : GradeObjective(module, test, answers, seconds);
            var review = graded is CefrObjectiveResult c ? c.Questions : ((ObjectiveResult)graded).Questions;

            // XP yig'ishga qarshi (yangi qator = yangi XP): bugungi natijalar (Toshkent kuni) va oxirgi 10 daqiqa.
            var now = DateTime.UtcNow;
            var dayStart = TashkentTime.DayStartUtc(now);
            var dupSince = now - XpGuard.DuplicateWindow;
            var since = dayStart < dupSince ? dayStart : dupSince;
            var todayRows = await db.Activities
                .Where(a => a.UserId == user.Id && a.Type == ActivityType.MockExam && a.CreatedAtUtc >= since)
                .OrderByDescending(a => a.CreatedAtUtc)
                .Take(200)
                .Select(a => new { a.Id, a.CreatedAtUtc, a.PromptData, Response = a.CreatedAtUtc >= dupSince ? a.ResponseData : null })
                .ToListAsync();
            var recent = todayRows
                .Select(r => (Row: r, P: ParsePrompt(r.PromptData)))
                .Select(x => new XpGuard.RecentResult(x.Row.Id, x.Row.CreatedAtUtc, x.P.SetId, x.P.Module, x.Row.Response))
                .ToList();
            // Aynan shu javoblar yaqinda yuborilgan — o'sha natija (ikki marta bosish yoki qayta yuborish).
            if (XpGuard.FindDuplicate(recent, setId, module, XpGuard.Fingerprint(review), now) is Guid existing)
                return Results.Ok(new { id = existing, duplicate = true });
            var objectiveToday = recent.Count(r => r.CreatedAtUtc >= dayStart && r.Module is "listening" or "reading");
            if (objectiveToday >= XpGuard.ObjectiveMocksPerDay)
                return Results.Json(request.Error("mock.objective_limit", XpGuard.ObjectiveMocksPerDay), statusCode: StatusCodes.Status429TooManyRequests);

            var id = graded is CefrObjectiveResult cefr
                ? await SaveAsync(db, null!, "cefr", user.Id, module, setId, null, sessionId, cefr, [], null)
                : await SaveAsync(db, null!, user.Id, module, setId, test.Variant.Length == 0 ? null : test.Variant,
                    sessionId, (ObjectiveResult)graded, [], null);
            return Results.Ok(new { id });
        }).RequireRateLimiting(RateLimits.WritePolicy);

        // To'liq imtihon (sessiya): 4 ta modul natijasi va umumiy band.
        app.MapGet("/api/mock/session/{sessionId}", async (string sessionId, HttpRequest request, AuthService auth, AppDbContext db) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return LoginFirst(request);
            var sid = MockLimit.CleanSession(sessionId);
            if (sid is null) return Results.NotFound();
            var rows = await MockRowsAsync(db, user.Id, 300);
            var inSession = rows
                .Select(r => new { r.Id, r.CreatedAtUtc, P = r.Prompt, r.Overall })
                .Where(r => r.P.SessionId == sid)
                .ToList();
            var exam = inSession.FirstOrDefault()?.P.Exam ?? "ielts";
            var modules = inSession
                .Where(r => r.P.Exam == exam)
                .GroupBy(r => r.P.Module)
                .Select(g => g.First())
                .ToDictionary(r => r.P.Module, r => new { r.Id, r.CreatedAtUtc, overall = r.Overall, variant = r.P.Variant });
            if (exam == "cefr")
            {
                // Rasmiy: daraja 4 bo'lim ballarining o'rtachasi bo'yicha aniqlanadi.
                var scores = FullMock.Modules.Select(m => modules.TryGetValue(m, out var x) ? x.overall : null).ToList();
                int? total = scores.Any(v => v is null) ? null : CefrObjective.Overall(scores.Select(v => (int)v!.Value).ToList());
                return Results.Ok(new { sessionId = sid, exam, modules, overall = total, level = total is int t ? CefrScale.Level(t) : null });
            }
            decimal? overall = FullMock.Overall(modules.ToDictionary(kv => kv.Key, kv => kv.Value.overall));
            return Results.Ok(new { sessionId = sid, exam, modules, overall, level = (string?)null });
        });

        // ---- CEFR (Multilevel): Speaking va Writing ----
        app.MapGet("/api/mock/cefr/speaking/new", async (HttpRequest request, AuthService auth, AppDbContext db, MockSets sets, string? setId) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return LoginFirst(request);
            var recent = (await RecentAsync(db, user.Id)).Where(r => r.Module == "speaking").Select(r => r.SetId).ToList();
            var set = await sets.FindCefrSpeakingAsync(setId) ?? IeltsBank.PickFresh(await sets.CefrSpeakingAsync(), s => s.Id, recent, Random.Shared);
            return Results.Ok(new
            {
                set,
                timing = new
                {
                    part11Seconds = CefrBank.Part11Seconds, part12FirstSeconds = CefrBank.Part12FirstSeconds, part12Seconds = CefrBank.Part12Seconds,
                    part2PrepSeconds = CefrBank.Part2PrepSeconds, part2SpeakSeconds = CefrBank.Part2SpeakSeconds,
                    part3PrepSeconds = CefrBank.Part3PrepSeconds, part3SpeakSeconds = CefrBank.Part3SpeakSeconds,
                },
            });
        });

        app.MapGet("/api/mock/cefr/writing/new", async (HttpRequest request, AuthService auth, AppDbContext db, MockSets sets, string? setId) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return LoginFirst(request);
            var recent = (await RecentAsync(db, user.Id)).Where(r => r.Module == "writing").Select(r => r.SetId).ToList();
            var set = await sets.FindCefrWritingAsync(setId) ?? IeltsBank.PickFresh(await sets.CefrWritingAsync(), w => w.Id, recent, Random.Shared);
            return Results.Ok(new
            {
                set,
                timing = new
                {
                    minutes = CefrBank.WritingMinutes,
                    words11 = new { min = CefrBank.Words11.Min, max = CefrBank.Words11.Max },
                    words12 = new { min = CefrBank.Words12.Min, max = CefrBank.Words12.Max },
                    words2 = new { min = CefrBank.Words2.Min, max = CefrBank.Words2.Max },
                },
            });
        });

        app.MapPost("/api/mock/cefr/speaking", async (
            HttpRequest request, AuthService auth, AppDbContext db, AdminOptions admins, IConfiguration config,
            ICefrEvaluator evaluator, ReviewService reviews, MockSets sets, ILogger<Program> logger) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return LoginFirst(request);
            if (!request.HasFormContentType) return Results.BadRequest(request.Error("speaking.multipart"));
            if (Uploads.DeclaredTooLarge(request, MaxSpeakingBodyBytes))
                return Results.Json(request.Error("mock.too_big", MaxAudioMb), statusCode: StatusCodes.Status413PayloadTooLarge);
            var ct = request.HttpContext.RequestAborted;
            using var slot = await Uploads.EnterAsync(ct);
            var form = await request.ReadFormAsync(ct);
            var set = await sets.FindCefrSpeakingAsync(form["setId"].ToString());
            if (set is null) return Results.BadRequest(request.Error("mock.bad_set"));
            var sizeError = CheckAudioSizes(request, form);
            if (sizeError is not null) return sizeError;
            var limited = await LimitAsync(request, db, user, admins, config);
            if (limited is not null) return limited;
            var answers = await ReadAnswersAsync(form, set.Questions().Count, ct);
            if (answers.All(a => a.Audio is null)) return Results.BadRequest(request.Error("mock.no_answers"));
            try
            {
                var result = await evaluator.EvaluateSpeakingAsync(set, answers, Texts.LangOf(request), ct);
                var id = await SaveAsync(db, reviews, "cefr", user.Id, "speaking", set.Id, null, MockLimit.CleanSession(form["sessionId"].ToString()),
                    result, result.TopCorrections, ActivityType.Speaking);
                return Results.Ok(new { id, result });
            }
            catch (Exception ex) when (AiErrors.Handles(ex, request))
            {
                logger.LogError(ex, "CEFR speaking mock baholanmadi");
                return Results.Problem(detail: request.T("ai_unavailable"), statusCode: 502);
            }
        }).RequireRateLimiting(RateLimits.AiPolicy).WithBodyLimit(MaxSpeakingBodyBytes);

        app.MapPost("/api/mock/cefr/writing", async (
            CefrWritingRequest body, HttpRequest request, AuthService auth, AppDbContext db, AdminOptions admins,
            IConfiguration config, ICefrEvaluator evaluator, ReviewService reviews, MockSets sets, ILogger<Program> logger) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return LoginFirst(request);
            var set = await sets.FindCefrWritingAsync(body.SetId);
            if (set is null) return Results.BadRequest(request.Error("mock.bad_set"));
            string[] texts = [(body.Task11 ?? "").Trim(), (body.Task12 ?? "").Trim(), (body.Task2 ?? "").Trim()];
            if (texts.All(t => t.Length == 0)) return Results.BadRequest(request.Error("mock.empty_text"));
            const int maxChars = 8000;
            if (texts.Any(t => t.Length > maxChars)) return Results.BadRequest(request.Error("text.too_long", maxChars));
            var limited = await LimitAsync(request, db, user, admins, config);
            if (limited is not null) return limited;
            try
            {
                var seconds = Math.Clamp(body.SecondsUsed, 0, 3 * 60 * 60);
                var result = await evaluator.EvaluateWritingAsync(set, texts[0], texts[1], texts[2], seconds, Texts.LangOf(request), request.HttpContext.RequestAborted);
                var id = await SaveAsync(db, reviews, "cefr", user.Id, "writing", set.Id, null, MockLimit.CleanSession(body.SessionId),
                    result, result.TopCorrections, ActivityType.Writing);
                return Results.Ok(new { id, result });
            }
            catch (Exception ex) when (AiErrors.Handles(ex, request))
            {
                logger.LogError(ex, "CEFR writing mock baholanmadi");
                return Results.Problem(detail: request.T("ai_unavailable"), statusCode: 502);
            }
        }).RequireRateLimiting(RateLimits.AiPolicy);

        // Natijalar tarixi (ro'yxat) va bitta natija (to'liq).
        app.MapGet("/api/mock/history", async (HttpRequest request, AuthService auth, AppDbContext db) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return LoginFirst(request);
            var rows = await MockRowsAsync(db, user.Id, 50);
            return Results.Ok(rows.Select(r =>
            {
                var p = r.Prompt;
                return new { r.Id, r.CreatedAtUtc, exam = p.Exam, module = p.Module, variant = p.Variant, setId = p.SetId, sessionId = p.SessionId, overall = r.Overall };
            }));
        });

        app.MapGet("/api/mock/{id:guid}", async (Guid id, HttpRequest request, AuthService auth, AppDbContext db) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return LoginFirst(request);
            var row = await db.Activities.FirstOrDefaultAsync(a => a.Id == id && a.UserId == user.Id && a.Type == ActivityType.MockExam);
            if (row is null) return Results.NotFound();
            using var doc = JsonDocument.Parse(row.ResponseData);
            // Listening/Reading: natija bilan birga testning o'zi (matn/skript, javoblar) — tahlil uchun.
            JsonElement? test = null;
            var prompt = ParsePrompt(row.PromptData);
            if (prompt.Module is "listening" or "reading" && Guid.TryParse(prompt.SetId, out var testId))
            {
                var t = await db.MockTests.FirstOrDefaultAsync(x => x.Id == testId);
                if (t is not null)
                {
                    using var tdoc = JsonDocument.Parse(t.Payload);
                    test = tdoc.RootElement.Clone();
                }
            }
            return Results.Ok(new { id = row.Id, row.CreatedAtUtc, sessionId = prompt.SessionId, result = doc.RootElement.Clone(), test });
        });
    }

    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    private static Task<Guid> SaveAsync<T>(AppDbContext db, ReviewService reviews, Guid userId, string module, string setId,
        string? variant, string? sessionId, T result, List<CorrectionItem> corrections, ActivityType? cardSource) =>
        SaveAsync(db, reviews, "ielts", userId, module, setId, variant, sessionId, result, corrections, cardSource);

    private static async Task<Guid> SaveAsync<T>(AppDbContext db, ReviewService reviews, string exam, Guid userId, string module, string setId,
        string? variant, string? sessionId, T result, List<CorrectionItem> corrections, ActivityType? cardSource)
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
        // Tuzatishlar — oddiy mashqlardagidek takrorlash kartalariga.
        if (corrections.Count > 0) await reviews.AddCardsAsync(userId, cardSource, ReviewCardFactory.FromCorrections(corrections));
        await db.SaveChangesAsync();
        return id;
    }

    /// <summary>Speaking javoblari: "a{i}" audio fayllari va "s{i}" soniyalar (IELTS va CEFR uchun umumiy).</summary>
    public static async Task<List<SpokenAnswer>> ReadAnswersAsync(IFormCollection form, int count, CancellationToken ct = default)
    {
        var answers = new List<SpokenAnswer>();
        for (var i = 0; i < count; i++)
        {
            var file = form.Files.GetFile($"a{i}");
            int.TryParse(form[$"s{i}"].ToString(), out var seconds);
            var bytes = file is { Length: > 0 } ? await Uploads.ReadAsync(file, ct) : null;
            answers.Add(new SpokenAnswer(i, bytes, file?.ContentType, Math.Clamp(seconds, 0, 600)));
        }
        return answers;
    }

    /// <summary>Jami audio — 15 MB gacha, har bir javob — 8 MB gacha (xotiraga o'qishdan oldin).</summary>
    private static IResult? CheckAudioSizes(HttpRequest request, IFormCollection form)
    {
        if (form.Files.Sum(f => f.Length) > MaxAudioMb * Uploads.Mb)
            return Results.BadRequest(request.Error("mock.too_big", MaxAudioMb));
        if (form.Files.Any(f => f.Length > Uploads.MaxRecordingBytes))
            return Results.BadRequest(request.Error("audio.too_big", Uploads.MaxRecordingBytes / Uploads.Mb));
        return null;
    }

    /// <summary>
    /// Mock natijalarining yengil ko'rinishi: katta JSON (javoblar, izohlar) bazadan
    /// tortilmaydi — kerakli maydonlar (exam, module, setId, overall...) Postgres'ning
    /// o'zida jsonb'dan o'qiladi.
    /// </summary>
    public sealed class MockRow
    {
        public Guid Id { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public string? Exam { get; set; }
        public string? Module { get; set; }
        public string? SetId { get; set; }
        public string? Variant { get; set; }
        public string? SessionId { get; set; }
        public decimal? Overall { get; set; }

        public MockPrompt Prompt => new(Exam ?? "ielts", Module ?? "", SetId ?? "", Variant, SessionId);
    }

    public const string MockRowsSql = """
        SELECT "Id", "CreatedAtUtc",
               "PromptData"->>'exam' AS "Exam",
               "PromptData"->>'module' AS "Module",
               "PromptData"->>'setId' AS "SetId",
               "PromptData"->>'variant' AS "Variant",
               "PromptData"->>'sessionId' AS "SessionId",
               CASE WHEN jsonb_typeof("ResponseData"->'overall') = 'number' THEN ("ResponseData"->>'overall')::numeric END AS "Overall"
        FROM "Activities"
        WHERE "UserId" = {0} AND "Type" = 'MockExam'
        ORDER BY "CreatedAtUtc" DESC
        LIMIT {1}
        """;

    private static Task<List<MockRow>> MockRowsAsync(AppDbContext db, Guid userId, int take) =>
        db.Database.SqlQueryRaw<MockRow>(MockRowsSql, userId, take).ToListAsync();

    public static object ClientPayload(string module, Guid id, string payload) => module == "reading"
        ? ClientView.Reading(id, JsonSerializer.Deserialize<ReadingTest>(payload, GeminiMockGenerator.Web)!)
        : ClientView.Listening(id, JsonSerializer.Deserialize<ListeningTest>(payload, GeminiMockGenerator.Web)!);

    public static ObjectiveResult GradeObjective(string module, MockTest test, IReadOnlyDictionary<string, string> answers, int seconds)
    {
        List<QuestionGroup> groups;
        (int, decimal)[] anchors;
        if (module == "reading")
        {
            var t = JsonSerializer.Deserialize<ReadingTest>(test.Payload, GeminiMockGenerator.Web)!;
            groups = t.Passages.SelectMany(p => p.Groups).ToList();
            anchors = t.Variant == IeltsBank.General ? ObjectiveGrading.GeneralReadingAnchors : ObjectiveGrading.AcademicReadingAnchors;
        }
        else
        {
            var t = JsonSerializer.Deserialize<ListeningTest>(test.Payload, GeminiMockGenerator.Web)!;
            groups = t.Parts.SelectMany(p => p.Groups).ToList();
            anchors = ObjectiveGrading.ListeningAnchors;
        }
        var review = ObjectiveGrading.Grade(groups, answers);
        var score = review.Count(q => q.Correct);
        return new ObjectiveResult("ielts", module, test.Id.ToString(), test.Variant, score, review.Count,
            ObjectiveGrading.BandFor(score, anchors, review.Count), seconds, review);
    }

    /// <summary>CEFR Listening/Reading: to'g'ri javoblar soni, 75 ballik taxminiy natija va daraja.</summary>
    public static CefrObjectiveResult GradeCefrObjective(string module, MockTest test, IReadOnlyDictionary<string, string> answers, int seconds)
    {
        var layout = module == "reading" ? CefrObjective.ReadingLayout : CefrObjective.ListeningLayout;
        var groups = module == "reading"
            ? JsonSerializer.Deserialize<ReadingTest>(test.Payload, GeminiMockGenerator.Web)!.Passages.SelectMany(p => p.Groups).ToList()
            : JsonSerializer.Deserialize<ListeningTest>(test.Payload, GeminiMockGenerator.Web)!.Parts.SelectMany(p => p.Groups).ToList();
        var review = ObjectiveGrading.Grade(groups, answers);
        var score = review.Count(q => q.Correct);
        var overall = CefrObjective.Score(score, review.Count);
        var parts = layout.Select(l => new CefrPartScore(l.Part,
            review.Count(q => q.Correct && q.Number >= l.First && q.Number < l.First + l.Count), l.Count)).ToList();
        return new CefrObjectiveResult("cefr", module, test.Id.ToString(), "", score, review.Count, overall, CefrScale.Level(overall), seconds, parts, review);
    }

    public record MockPrompt(string Exam, string Module, string SetId, string? Variant, string? SessionId = null);

    public static MockPrompt ParsePrompt(string json)
    {
        try
        {
            var p = JsonSerializer.Deserialize<MockPrompt>(json, Web);
            return p is null ? new("ielts", "", "", null) : p with { Exam = p.Exam ?? "ielts", Module = p.Module ?? "", SetId = p.SetId ?? "" };
        }
        catch (JsonException)
        {
            return new("ielts", "", "", null);
        }
    }

    public static decimal? ReadOverall(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("overall", out var o) && o.TryGetDecimal(out var d) ? d : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
