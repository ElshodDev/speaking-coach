using System.Text.Json;
using Microsoft.EntityFrameworkCore;
namespace SpeakingCoach.Api.Endpoints;

public record WritingMockRequest(string SetId, string? Task1, string? Task2, int SecondsUsed, string? SessionId = null);
public record CefrWritingRequest(string SetId, string? Task11, string? Task12, string? Task2, int SecondsUsed, string? SessionId = null);
public record ObjectiveMockRequest(Guid TestId, Dictionary<string, string>? Answers, int SecondsUsed, string? SessionId = null);

public static class MockEndpoints
{
    /// <summary>Mock speaking: barcha javoblar birga (IELTS ~14 daqiqagacha nutq) va har bir javob alohida (8 MB).</summary>
    private const int MaxAudioMb = 15;
    private const long MaxSpeakingBodyBytes = MaxAudioMb * Uploads.Mb + Uploads.FormOverheadBytes;

    public static void MapMockEndpoints(this IEndpointRouteBuilder app)
    {
        static IResult LoginFirst(HttpRequest r) => Results.Json(r.Error("mock.login"), statusCode: StatusCodes.Status401Unauthorized);

        // null — ruxsat; aks holda 429 javobi (qachon yana mumkinligi bilan).
        async Task<IResult?> LimitAsync(HttpRequest request, MockExamService mocks, User user, AdminOptions admins, IConfiguration config)
        {
            if (admins.IsAdmin(user)) return null;
            var perDay = MockLimit.PerDay(config);
            var now = DateTime.UtcNow;
            var starts = await mocks.AiStartsAsync(user.Id);
            var (allowed, retryAt) = MockLimit.Check(starts, perDay, now);
            if (allowed)
            {
                // Natija faqat baholashdan keyin saqlanadi (bir necha Gemini chaqiruvi) —
                // shu orada parallel yuborilgan imtihonlar ham limitga kirsin: joy so'rov
                // oxirigacha band qilinadi.
                var inFlight = request.HttpContext.RequestServices.GetRequiredService<AiInFlight>();
                var used = starts.Count(t => t > now.AddHours(-24));
                if (inFlight.TryReserve(MockLimit.InFlightKey(user.Id), DateOnly.MinValue, AiKind.Exercise, used, perDay) is { } lease)
                {
                    request.HttpContext.Response.RegisterForDispose(lease);
                    return null;
                }
            }
            return Results.Json(
                new { error = request.T("mock.limit", perDay), retryAtUtc = retryAt },
                statusCode: StatusCodes.Status429TooManyRequests);
        }

        // Holat: nechta imtihon qoldi (sahifa tepasida ko'rsatiladi).
        app.MapGet("/api/mock/status", async (HttpRequest request, AuthService auth, MockExamService mocks, AdminOptions admins, IConfiguration config) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            return user is null ? LoginFirst(request) : Results.Ok(await mocks.StatusAsync(user.Id, MockLimit.PerDay(config), admins.IsAdmin(user)));
        });

        // setId — foydalanuvchi ro'yxatdan tanlagan variant (bo'lmasa — hali ishlanmagani).
        app.MapGet("/api/mock/ielts/speaking/new", async (HttpRequest request, AuthService auth, MockExamService mocks, MockSets sets, string? setId) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return LoginFirst(request);
            var recent = await mocks.RecentSetIdsAsync(user.Id, "speaking");
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
        app.MapGet("/api/mock/ielts/writing/new", async (HttpRequest request, AuthService auth, MockExamService mocks, MockSets sets, string? variant, string? setId) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return LoginFirst(request);
            var v = variant == IeltsBank.General ? IeltsBank.General : IeltsBank.Academic;
            var resumed = await sets.FindIeltsWritingAsync(setId);
            var recent = await mocks.RecentSetIdsAsync(user.Id, "writing");
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
            HttpRequest request, AuthService auth, MockExamService mocks, AdminOptions admins, IConfiguration config,
            IIeltsEvaluator evaluator, MockSets sets, ILogger<Program> logger) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return LoginFirst(request);
            if (!request.HasFormContentType) return Results.BadRequest(request.Error("speaking.multipart"));
            if (Uploads.DeclaredTooLarge(request, MaxSpeakingBodyBytes))
                return Results.Json(request.Error("mock.too_big", MaxAudioMb), statusCode: StatusCodes.Status413PayloadTooLarge);

            var ct = request.HttpContext.RequestAborted;
            // "Katta so'rov" joyi — audio xotiraga o'qilishidan oldin.
            var form = await Uploads.ReadFormAsync(request, ct);
            using var slot = await Uploads.EnterAsync(ct);
            var set = await sets.FindIeltsSpeakingAsync(form["setId"].ToString());
            if (set is null) return Results.BadRequest(request.Error("mock.bad_set"));

            var sizeError = CheckAudioSizes(request, form);
            if (sizeError is not null) return sizeError;

            // Limit — audio xotiraga o'qilishidan oldin.
            var limited = await LimitAsync(request, mocks, user, admins, config);
            if (limited is not null) return limited;

            var answers = await ReadAnswersAsync(form, set.Questions().Count, ct);
            if (answers.All(a => a.Audio is null)) return Results.BadRequest(request.Error("mock.no_answers"));

            try
            {
                var result = await evaluator.EvaluateSpeakingAsync(set, answers, RequestTexts.LangOf(request), ct);
                // Ravonlik o'lchovlari — har bir javobning transkripti, vaqti va pauzalaridan (AI'siz).
                var metrics = FluencyMetrics.ForAnswers(result.Answers.Select(a => (a.Index, a.Transcript, a.Seconds)), k => form[k].ToString());
                var id = await mocks.SaveResultAsync("ielts", user.Id, "speaking", set.Id, null, MockLimit.CleanSession(form["sessionId"].ToString()),
                    WithMetrics(result, metrics), result.TopCorrections, ActivityType.Speaking);
                return Results.Ok(new { id, result, metrics });
            }
            catch (Exception ex) when (AiErrors.Handles(ex, request))
            {
                logger.LogError(ex, "IELTS speaking mock baholanmadi");
                return Results.Problem(detail: request.T("ai_unavailable"), statusCode: 502);
            }
        }).RequireRateLimiting(RateLimits.AiPolicy).WithBodyLimit(MaxSpeakingBodyBytes);

        app.MapPost("/api/mock/ielts/writing", async (
            WritingMockRequest body, HttpRequest request, AuthService auth, MockExamService mocks, AdminOptions admins,
            IConfiguration config, IIeltsEvaluator evaluator, MockSets sets, ILogger<Program> logger) =>
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

            var limited = await LimitAsync(request, mocks, user, admins, config);
            if (limited is not null) return limited;

            try
            {
                var seconds = Math.Clamp(body.SecondsUsed, 0, 3 * 60 * 60);
                var result = await evaluator.EvaluateWritingAsync(set, t1, t2, seconds, RequestTexts.LangOf(request), request.HttpContext.RequestAborted);
                var id = await mocks.SaveResultAsync("ielts", user.Id, "writing", set.Id, set.Variant, MockLimit.CleanSession(body.SessionId), result, result.TopCorrections, ActivityType.Writing);
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
            string exam, string module, HttpRequest request, AuthService auth, MockExamService mocks, MockSets sets,
            IServiceScopeFactory scopes, ILogger<Program> logger, string? variant) =>
        {
            if (exam is not ("ielts" or "cefr") || module is not ("listening" or "reading" or "speaking" or "writing")) return Results.NotFound();
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return LoginFirst(request);

            // Ilova bilan keladigan tayyor testlar bankda bo'lsin (bir marta).
            if (module is "listening" or "reading") await BuiltInMocks.EnsureSeededAsync(scopes, logger);
            var bank = await mocks.BankAsync(user.Id, exam, module, variant == IeltsBank.General);
            return Results.Ok(new { items = bank.Items, done = bank.Done, total = bank.Total });
        });

        // ---- Listening va Reading: test bankdan (bo'lmasa — AI yaratadi) ----
        // IELTS va CEFR uchun bitta yo'l: /api/mock/{ielts|cefr}/{listening|reading}/new
        // (/api/mock/cefr/speaking/new kabi aniq yo'llar ustun turadi).
        app.MapGet("/api/mock/{exam}/{module}/new", async (
            string exam, string module, HttpRequest request, AuthService auth, MockExamService mocks, AdminOptions admins, IConfiguration config,
            IMockGenerator generator, GenerationGate gate, IServiceScopeFactory scopes, ILogger<Program> logger, string? variant, string? source, string? testId) =>
        {
            if (exam is not ("ielts" or "cefr") || module is not ("listening" or "reading")) return Results.NotFound();
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return LoginFirst(request);
            var v = exam == "ielts" && module == "reading" ? (variant == IeltsBank.General ? IeltsBank.General : IeltsBank.Academic) : "";

            // Ilova bilan keladigan tayyor testlar bankda bo'lsin (bir marta).
            await BuiltInMocks.EnsureSeededAsync(scopes, logger);

            // Manbani foydalanuvchi tanlaydi: "bank" (standart) — tayyor testlar,
            // "repeat" — hammasi ishlangan bo'lsa, eng eskisini qayta, "ai" — Gemini yangisini yaratadi.
            var src = (source ?? "bank").ToLowerInvariant();

            if (src != "ai")
            {
                var pick = await mocks.PickFromBankAsync(user.Id, exam, module, v, src, testId);
                if (pick.TestId is { } picked)
                    return Results.Ok(new { test = MockGrading.ClientPayload(module, picked, pick.Payload!), repeated = pick.Repeated, source = "bank" });
                // Bankdagi hammasi ishlangan (yoki bank bo'sh) — foydalanuvchi tanlaydi: qayta yoki AI.
                return Results.Ok(new { test = (object?)null, exhausted = true, bankCount = pick.BankCount });
            }

            // ---- AI yangi test yaratadi (sutkalik limit bilan) ----
            var isAdmin = admins.IsAdmin(user);
            var perDay = MockLimit.GeneratePerDay(config);
            if (!isAdmin && await mocks.GeneratedSinceAsync(user.Id, DateTime.UtcNow.AddHours(-24)) >= perDay)
                return Results.Json(request.Error("mock.generate_limit"), statusCode: StatusCodes.Status429TooManyRequests);

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
                    var payload = JsonSerializer.Serialize(content, content.GetType(), GeminiMockGenerator.Web);
                    var id = await mocks.StoreGeneratedAsync(exam, module, v, payload, user.Id);
                    logger.LogInformation("Yangi {Exam} {Module} testi yaratildi: {Id}", exam, module, id);
                    return Results.Ok(new { test = MockGrading.ClientPayload(module, id, payload), repeated = false, source = "ai" });
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
            string exam, string module, ObjectiveMockRequest body, HttpRequest request, AuthService auth, MockExamService mocks) =>
        {
            if (exam is not ("ielts" or "cefr") || module is not ("listening" or "reading")) return Results.NotFound();
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return LoginFirst(request);
            var r = await mocks.SubmitObjectiveAsync(user.Id, exam, module, body.TestId, body.Answers, body.SecondsUsed, body.SessionId);
            if (!r.Ok) return Results.Json(request.Error(r.Failure!.Key, r.Failure.Args), statusCode: r.Failure.Status);
            return r.Value!.Duplicate ? Results.Ok(new { id = r.Value.Id, duplicate = true }) : Results.Ok(new { id = r.Value.Id });
        }).RequireRateLimiting(RateLimits.WritePolicy);

        // To'liq imtihon (sessiya): 4 ta modul natijasi va umumiy band.
        app.MapGet("/api/mock/session/{sessionId}", async (string sessionId, HttpRequest request, AuthService auth, MockExamService mocks) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return LoginFirst(request);
            var sid = MockLimit.CleanSession(sessionId);
            return sid is null ? Results.NotFound() : Results.Ok(await mocks.SessionAsync(user.Id, sid));
        });

        // ---- CEFR (Multilevel): Speaking va Writing ----
        app.MapGet("/api/mock/cefr/speaking/new", async (HttpRequest request, AuthService auth, MockExamService mocks, MockSets sets, string? setId) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return LoginFirst(request);
            var recent = await mocks.RecentSetIdsAsync(user.Id, "speaking");
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

        app.MapGet("/api/mock/cefr/writing/new", async (HttpRequest request, AuthService auth, MockExamService mocks, MockSets sets, string? setId) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return LoginFirst(request);
            var recent = await mocks.RecentSetIdsAsync(user.Id, "writing");
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
            HttpRequest request, AuthService auth, MockExamService mocks, AdminOptions admins, IConfiguration config,
            ICefrEvaluator evaluator, MockSets sets, ILogger<Program> logger) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return LoginFirst(request);
            if (!request.HasFormContentType) return Results.BadRequest(request.Error("speaking.multipart"));
            if (Uploads.DeclaredTooLarge(request, MaxSpeakingBodyBytes))
                return Results.Json(request.Error("mock.too_big", MaxAudioMb), statusCode: StatusCodes.Status413PayloadTooLarge);
            var ct = request.HttpContext.RequestAborted;
            var form = await Uploads.ReadFormAsync(request, ct);
            using var slot = await Uploads.EnterAsync(ct);
            var set = await sets.FindCefrSpeakingAsync(form["setId"].ToString());
            if (set is null) return Results.BadRequest(request.Error("mock.bad_set"));
            var sizeError = CheckAudioSizes(request, form);
            if (sizeError is not null) return sizeError;
            var limited = await LimitAsync(request, mocks, user, admins, config);
            if (limited is not null) return limited;
            var answers = await ReadAnswersAsync(form, set.Questions().Count, ct);
            if (answers.All(a => a.Audio is null)) return Results.BadRequest(request.Error("mock.no_answers"));
            try
            {
                var result = await evaluator.EvaluateSpeakingAsync(set, answers, RequestTexts.LangOf(request), ct);
                var metrics = FluencyMetrics.ForAnswers(result.Answers.Select(a => (a.Index, a.Transcript, a.Seconds)), k => form[k].ToString());
                var id = await mocks.SaveResultAsync("cefr", user.Id, "speaking", set.Id, null, MockLimit.CleanSession(form["sessionId"].ToString()),
                    WithMetrics(result, metrics), result.TopCorrections, ActivityType.Speaking);
                return Results.Ok(new { id, result, metrics });
            }
            catch (Exception ex) when (AiErrors.Handles(ex, request))
            {
                logger.LogError(ex, "CEFR speaking mock baholanmadi");
                return Results.Problem(detail: request.T("ai_unavailable"), statusCode: 502);
            }
        }).RequireRateLimiting(RateLimits.AiPolicy).WithBodyLimit(MaxSpeakingBodyBytes);

        app.MapPost("/api/mock/cefr/writing", async (
            CefrWritingRequest body, HttpRequest request, AuthService auth, MockExamService mocks, AdminOptions admins,
            IConfiguration config, ICefrEvaluator evaluator, MockSets sets, ILogger<Program> logger) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return LoginFirst(request);
            var set = await sets.FindCefrWritingAsync(body.SetId);
            if (set is null) return Results.BadRequest(request.Error("mock.bad_set"));
            string[] texts = [(body.Task11 ?? "").Trim(), (body.Task12 ?? "").Trim(), (body.Task2 ?? "").Trim()];
            if (texts.All(t => t.Length == 0)) return Results.BadRequest(request.Error("mock.empty_text"));
            const int maxChars = 8000;
            if (texts.Any(t => t.Length > maxChars)) return Results.BadRequest(request.Error("text.too_long", maxChars));
            var limited = await LimitAsync(request, mocks, user, admins, config);
            if (limited is not null) return limited;
            try
            {
                var seconds = Math.Clamp(body.SecondsUsed, 0, 3 * 60 * 60);
                var result = await evaluator.EvaluateWritingAsync(set, texts[0], texts[1], texts[2], seconds, RequestTexts.LangOf(request), request.HttpContext.RequestAborted);
                var id = await mocks.SaveResultAsync("cefr", user.Id, "writing", set.Id, null, MockLimit.CleanSession(body.SessionId),
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
        app.MapGet("/api/mock/history", async (HttpRequest request, AuthService auth, MockExamService mocks) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            return user is null ? LoginFirst(request) : Results.Ok(await mocks.HistoryAsync(user.Id));
        });

        app.MapGet("/api/mock/{id:guid}", async (Guid id, HttpRequest request, AuthService auth, MockExamService mocks) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return LoginFirst(request);
            return await mocks.ResultAsync(user.Id, id) is { } view ? Results.Ok(view) : Results.NotFound();
        });
    }

    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    /// <summary>Speaking javoblari: "a{i}" audio fayllari va "s{i}" soniyalar (IELTS va CEFR uchun umumiy).</summary>
    /// <summary>Saqlanadigan natija: o'lchov bo'lsa — ichida "metrics" maydoni bilan (natija o'z camelCase nomlarida).</summary>
    private static object WithMetrics<T>(T result, FluencyStats? metrics) =>
        metrics is null ? result! : FluencyMetrics.WithMetricsNode(result, metrics, Web);

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
}
