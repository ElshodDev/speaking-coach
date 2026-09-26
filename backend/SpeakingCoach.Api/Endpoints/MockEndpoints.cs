using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SpeakingCoach.Api.Data;
using SpeakingCoach.Api.Services;
using SpeakingCoach.Api.Services.Mock;

namespace SpeakingCoach.Api.Endpoints;

public record WritingMockRequest(string SetId, string? Task1, string? Task2, int SecondsUsed);

/// <summary>
/// Mock imtihon uchun sutkalik cheklov: to'liq imtihon Gemini'ning katta
/// so'rovi (10 daqiqagacha audio), shuning uchun alohida, kichik limit.
/// Oyna — oxirgi 24 soat (kun chegarasi va vaqt zonasidan mustaqil).
/// </summary>
public static class MockLimit
{
    public static int PerDay(IConfiguration config) =>
        int.TryParse(config["Mock:PerDay"], out var n) && n > 0 ? n : 3;

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
    private const int MaxAudioMb = 15;

    public static void MapMockEndpoints(this IEndpointRouteBuilder app)
    {
        static IResult LoginFirst(HttpRequest r) => Results.Json(r.Error("mock.login"), statusCode: StatusCodes.Status401Unauthorized);

        async Task<List<(DateTime At, string Module, string SetId)>> RecentAsync(AppDbContext db, Guid userId)
        {
            var rows = await db.Activities
                .Where(a => a.UserId == userId && a.Type == ActivityType.MockExam)
                .OrderByDescending(a => a.CreatedAtUtc)
                .Take(30)
                .Select(a => new { a.CreatedAtUtc, a.PromptData })
                .ToListAsync();
            return rows.Select(r =>
            {
                var p = ParsePrompt(r.PromptData);
                return (r.CreatedAtUtc, p.Module, p.SetId);
            }).ToList();
        }

        // null — ruxsat; aks holda 429 javobi (qachon yana mumkinligi bilan).
        async Task<IResult?> LimitAsync(HttpRequest request, AppDbContext db, User user, AdminOptions admins, IConfiguration config)
        {
            if (admins.IsAdmin(user)) return null;
            var perDay = MockLimit.PerDay(config);
            var recent = await RecentAsync(db, user.Id);
            var (allowed, retryAt) = MockLimit.Check(recent.Select(r => r.At), perDay, DateTime.UtcNow);
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
            var starts = (await RecentAsync(db, user.Id)).Select(r => r.At).ToList();
            var now = DateTime.UtcNow;
            var used = starts.Count(t => t > now.AddHours(-24));
            var (_, retryAt) = MockLimit.Check(starts, perDay, now);
            return Results.Ok(new { perDay, remaining = (int?)Math.Max(0, perDay - used), retryAtUtc = retryAt });
        });

        app.MapGet("/api/mock/ielts/speaking/new", async (HttpRequest request, AuthService auth, AppDbContext db) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return LoginFirst(request);
            var recent = (await RecentAsync(db, user.Id)).Where(r => r.Module == "speaking").Select(r => r.SetId).ToList();
            var set = IeltsBank.PickFresh(IeltsBank.Speaking, s => s.Id, recent, Random.Shared);
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
        app.MapGet("/api/mock/ielts/writing/new", async (HttpRequest request, AuthService auth, AppDbContext db, string? variant, string? setId) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return LoginFirst(request);
            var v = variant == IeltsBank.General ? IeltsBank.General : IeltsBank.Academic;
            var resumed = IeltsBank.FindWriting(setId);
            var recent = (await RecentAsync(db, user.Id)).Where(r => r.Module == "writing").Select(r => r.SetId).ToList();
            var set = resumed is not null && resumed.Variant == v
                ? resumed
                : IeltsBank.PickFresh(IeltsBank.Writing.Where(w => w.Variant == v).ToList(), w => w.Id, recent, Random.Shared);
            return Results.Ok(new
            {
                set,
                timing = new { minutes = IeltsBank.WritingMinutes, task1MinWords = IeltsBank.Task1MinWords, task2MinWords = IeltsBank.Task2MinWords },
            });
        });

        // Speaking: multipart — setId, har bir javob "a{index}" fayli va "s{index}" (soniya).
        app.MapPost("/api/mock/ielts/speaking", async (
            HttpRequest request, AuthService auth, AppDbContext db, AdminOptions admins, IConfiguration config,
            IIeltsEvaluator evaluator, ReviewService reviews, ILogger<Program> logger) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return LoginFirst(request);
            if (!request.HasFormContentType) return Results.BadRequest(request.Error("speaking.multipart"));

            var form = await request.ReadFormAsync();
            var set = IeltsBank.FindSpeaking(form["setId"].ToString());
            if (set is null) return Results.BadRequest(request.Error("mock.bad_set"));

            if (form.Files.Sum(f => f.Length) > MaxAudioMb * 1024L * 1024L)
                return Results.BadRequest(request.Error("mock.too_big", MaxAudioMb));

            var questionCount = set.Questions().Count;
            var answers = new List<SpokenAnswer>();
            for (var i = 0; i < questionCount; i++)
            {
                var file = form.Files.GetFile($"a{i}");
                int.TryParse(form[$"s{i}"].ToString(), out var seconds);
                byte[]? bytes = null;
                if (file is { Length: > 0 })
                {
                    using var ms = new MemoryStream();
                    await file.CopyToAsync(ms);
                    bytes = ms.ToArray();
                }
                answers.Add(new SpokenAnswer(i, bytes, file?.ContentType, Math.Clamp(seconds, 0, 600)));
            }
            if (answers.All(a => a.Audio is null)) return Results.BadRequest(request.Error("mock.no_answers"));

            var limited = await LimitAsync(request, db, user, admins, config);
            if (limited is not null) return limited;

            try
            {
                var result = await evaluator.EvaluateSpeakingAsync(set, answers, Texts.LangOf(request), request.HttpContext.RequestAborted);
                var id = await SaveAsync(db, reviews, user.Id, "speaking", set.Id, null, result, result.TopCorrections, ActivityType.Speaking);
                return Results.Ok(new { id, result });
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "IELTS speaking mock baholanmadi");
                return Results.Problem(detail: request.T("ai_unavailable"), statusCode: 502);
            }
        }).RequireRateLimiting("ai");

        app.MapPost("/api/mock/ielts/writing", async (
            WritingMockRequest body, HttpRequest request, AuthService auth, AppDbContext db, AdminOptions admins,
            IConfiguration config, IIeltsEvaluator evaluator, ReviewService reviews, ILogger<Program> logger) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return LoginFirst(request);
            var set = IeltsBank.FindWriting(body.SetId);
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
                var id = await SaveAsync(db, reviews, user.Id, "writing", set.Id, set.Variant, result, result.TopCorrections, ActivityType.Writing);
                return Results.Ok(new { id, result });
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "IELTS writing mock baholanmadi");
                return Results.Problem(detail: request.T("ai_unavailable"), statusCode: 502);
            }
        }).RequireRateLimiting("ai");

        // Natijalar tarixi (ro'yxat) va bitta natija (to'liq).
        app.MapGet("/api/mock/history", async (HttpRequest request, AuthService auth, AppDbContext db) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return LoginFirst(request);
            var rows = await db.Activities
                .Where(a => a.UserId == user.Id && a.Type == ActivityType.MockExam)
                .OrderByDescending(a => a.CreatedAtUtc)
                .Take(50)
                .Select(a => new { a.Id, a.CreatedAtUtc, a.PromptData, a.ResponseData })
                .ToListAsync();
            return Results.Ok(rows.Select(r =>
            {
                var p = ParsePrompt(r.PromptData);
                return new { r.Id, r.CreatedAtUtc, exam = p.Exam, module = p.Module, variant = p.Variant, setId = p.SetId, overall = ReadOverall(r.ResponseData) };
            }));
        });

        app.MapGet("/api/mock/{id:guid}", async (Guid id, HttpRequest request, AuthService auth, AppDbContext db) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return LoginFirst(request);
            var row = await db.Activities.FirstOrDefaultAsync(a => a.Id == id && a.UserId == user.Id && a.Type == ActivityType.MockExam);
            if (row is null) return Results.NotFound();
            using var doc = JsonDocument.Parse(row.ResponseData);
            return Results.Ok(new { id = row.Id, row.CreatedAtUtc, result = doc.RootElement.Clone() });
        });
    }

    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    private static async Task<Guid> SaveAsync<T>(AppDbContext db, ReviewService reviews, Guid userId, string module, string setId,
        string? variant, T result, List<CorrectionItem> corrections, ActivityType cardSource)
    {
        var id = Guid.NewGuid();
        db.Activities.Add(new Activity
        {
            Id = id,
            Type = ActivityType.MockExam,
            UserId = userId,
            CreatedAtUtc = DateTime.UtcNow,
            PromptData = JsonSerializer.Serialize(new { exam = "ielts", module, setId, variant }, Web),
            ResponseData = JsonSerializer.Serialize(result, Web),
        });
        // Tuzatishlar — oddiy mashqlardagidek takrorlash kartalariga.
        await reviews.AddCardsAsync(userId, cardSource, ReviewCardFactory.FromCorrections(corrections));
        await db.SaveChangesAsync();
        return id;
    }

    public record MockPrompt(string Exam, string Module, string SetId, string? Variant);

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
