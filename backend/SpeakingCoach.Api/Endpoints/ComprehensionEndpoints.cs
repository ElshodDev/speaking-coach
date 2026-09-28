using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SpeakingCoach.Api.Data;
using SpeakingCoach.Api.Services;
using SpeakingCoach.Api.Services.Content;

namespace SpeakingCoach.Api.Endpoints;

public record ComprehensionSubmitRequest(Guid ExerciseId, List<int> Answers);
/// <summary>Source: "bank" — ilovaning tayyor mashqi (AI limiti sarflanmaydi), "ai" (yoki bo'sh) — Gemini yangisini yaratadi. Seen — mehmon ko'rgan tayyor mashqlar (brauzerda saqlanadi).</summary>
/// <summary>BankId — ro'yxatdan tanlangan aniq tayyor mashq.</summary>
public record ComprehensionGenerateRequest(string? Level, string? Source = null, List<string>? Seen = null, string? BankId = null);

public static class ComprehensionEndpoints
{
    /// <summary>
    /// /api/reading/... va /api/listening/... — bir xil mantiq, faqat
    /// ActivityType farq qiladi, shuning uchun ikkalasi bitta sikldan
    /// ro'yxatga olinadi.
    /// </summary>
    public static void MapComprehensionEndpoints(this IEndpointRouteBuilder app)
    {
        var kinds = new[] { ("reading", ActivityType.Reading), ("listening", ActivityType.Listening) };

        foreach (var (path, type) in kinds)
        {
            app.MapPost($"/api/{path}/generate", async (
                ComprehensionGenerateRequest? body,
                HttpRequest request,
                IComprehensionService service,
                AppDbContext db,
                AuthService auth,
                AiQuotaService quotas,
                ILogger<Program> logger) =>
            {
                var level = LearnerLevel.Normalize(body?.Level);

                // Javob berilmay tashlab ketilgan eski mashqlarni tozalaymiz —
                // alohida fon vazifasi (background job) o'rniga shu yerda,
                // oddiy va yetarli.
                var cutoff = DateTime.UtcNow.AddDays(-1);
                await db.PendingExercises.Where(p => p.CreatedAtUtc < cutoff).ExecuteDeleteAsync();

                async Task<IResult> Serve(ComprehensionExercise exercise, object? bank)
                {
                    var pending = new PendingExercise
                    {
                        Id = Guid.NewGuid(),
                        Type = type,
                        Payload = JsonSerializer.Serialize(exercise),
                        CreatedAtUtc = DateTime.UtcNow,
                    };
                    db.PendingExercises.Add(pending);
                    await db.SaveChangesAsync();

                    // Brauzerga to'g'ri javoblar (correctIndex) va izohlar
                    // YUBORILMAYDI — faqat savol va variantlar.
                    return Results.Ok(new
                    {
                        exerciseId = pending.Id,
                        title = exercise.Title,
                        passage = exercise.Passage,
                        questions = exercise.Questions.Select(q => new { question = q.Question, options = q.Options }),
                        source = bank is null ? "ai" : "bank",
                        bank,
                    });
                }

                // ---- Tayyor mashq (ilovaning o'z kutubxonasi) ----
                if (string.Equals(body?.Source, "bank", StringComparison.OrdinalIgnoreCase))
                {
                    var userId = await auth.GetCurrentUserIdAsync(request);
                    var done = userId is null
                        ? (body?.Seen ?? []).Take(200).Select(id => PracticeLibrary.Find(type, id)?.Exercise.Title).OfType<string>().ToList()
                        : await DoneTitlesAsync(db, userId.Value, type);
                    var item = PracticeLibrary.Find(type, body?.BankId) ?? PracticeLibrary.Next(type, level, done);
                    if (item is null) return Results.NotFound(request.Error("exercise.not_found"));
                    var (doneCount, total) = PracticeLibrary.Progress(type, item.Level, done);
                    return await Serve(item.Exercise, new { id = item.Id, level = item.Level, done = doneCount, total, repeated = done.Contains(item.Exercise.Title) });
                }

                // ---- AI yangi mashq yaratadi (kunlik limit bilan) ----
                var limited = await quotas.CheckAsync(request, AiKind.Exercise);
                if (limited is not null) return limited;

                try
                {
                    var exercise = await service.GenerateAsync(type, level);
                    var result = await Serve(exercise, null);
                    await quotas.RecordAsync(request, AiKind.Exercise);
                    return result;
                }
                catch (Exception ex) when (AiErrors.Handles(ex, request))
                {
                    logger.LogError(ex, "{Kind} mashqini yaratishda xato", path);
                    return Results.Problem(detail: request.T("ai_unavailable"), statusCode: 502);
                }
            }).RequireRateLimiting(RateLimits.AiPolicy);

            // Tayyor mashqlar: tanlangan darajada nechtasi bajarilgan va barcha darajalardagi ro'yxat
            // (foydalanuvchi o'zi tanlaydi). Mehmon ishlaganlari brauzerda saqlanadi.
            app.MapGet($"/api/{path}/bank", async (HttpRequest request, AppDbContext db, AuthService auth, string? level) =>
            {
                var userId = await auth.GetCurrentUserIdAsync(request);
                var done = userId is null ? [] : await DoneTitlesAsync(db, userId.Value, type);
                var lv = LearnerLevel.Normalize(level);
                var (doneCount, total) = PracticeLibrary.Progress(type, lv, done);
                var items = PracticeLibrary.For(type).Select(x => new { id = x.Id, level = x.Level, title = x.Exercise.Title, done = done.Contains(x.Exercise.Title) });
                return Results.Ok(new { level = lv, done = doneCount, total, items });
            });

            app.MapPost($"/api/{path}/submit", async (
                ComprehensionSubmitRequest body,
                HttpRequest request,
                AppDbContext db,
                AuthService auth,
                ReviewService reviews) =>
            {
                var pending = await db.PendingExercises
                    .FirstOrDefaultAsync(p => p.Id == body.ExerciseId && p.Type == type);
                if (pending is null)
                {
                    return Results.NotFound(request.Error("exercise.not_found"));
                }

                var exercise = GeminiClient.DeserializeStrict<ComprehensionExercise>(pending.Payload);

                ComprehensionResult result;
                try
                {
                    result = GeminiComprehensionService.Grade(exercise, body.Answers ?? new List<int>());
                }
                catch (UserInputException ex)
                {
                    return Results.BadRequest(request.Error(ex.Key, ex.Args));
                }

                // Mashq bir marta ishlatiladi: javob berildi — o'chiramiz.
                db.PendingExercises.Remove(pending);

                var userId = await auth.GetCurrentUserIdAsync(request);
                var newCards = 0;
                if (userId is not null)
                {
                    // Xato javob berilgan savollar takrorlash kartalariga aylanadi.
                    newCards = await reviews.AddCardsAsync(
                        userId.Value, type, ReviewCardFactory.FromWrongAnswers(exercise, result));

                    db.Activities.Add(new Activity
                    {
                        Id = Guid.NewGuid(),
                        Type = type,
                        UserId = userId,
                        CreatedAtUtc = DateTime.UtcNow,
                        PromptData = pending.Payload,
                        ResponseData = JsonSerializer.Serialize(result),
                    });
                }

                // O'chirish va (bo'lsa) tarixga yozish — bitta tranzaksiyada.
                try
                {
                    await db.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    // Ikki marta yuborildi (tugma ikki marta bosildi): birinchisi
                    // allaqachon saqlandi — ikkinchisi XP/kartalarni takrorlamasin.
                    return Results.Conflict(request.Error("exercise.not_found"));
                }

                return Results.Ok(new
                {
                    result.Score,
                    result.Total,
                    result.Results,
                    // Listening'da matn javobdan keyingina ko'rsatiladi.
                    passage = exercise.Passage,
                    saved = userId is not null,
                    newCards,
                });
            }).RequireRateLimiting(RateLimits.WritePolicy); // AI'siz — javoblar serverda tekshiriladi

            app.MapGet($"/api/{path}/history", (HttpRequest request, AppDbContext db, AuthService auth) =>
                HistoryQueries.GetHistoryAsync(type, request, db, auth));
        }
    }

    /// <summary>
    /// Foydalanuvchi bajargan mashqlar sarlavhalari (eng yangisi birinchi) — tayyor mashqlardan hali ishlanmaganini tanlash uchun.
    /// Butun matn (passage, savollar) bazadan tortilmaydi: sarlavha Postgres'da jsonb'dan o'qiladi.
    /// </summary>
    public const string DoneTitlesSql = """
        SELECT "PromptData"->>'title' AS "Value"
        FROM "Activities"
        WHERE "UserId" = {0} AND "Type" = {1} AND jsonb_typeof("PromptData"->'title') = 'string'
        ORDER BY "CreatedAtUtc" DESC
        LIMIT 300
        """;

    private static Task<List<string>> DoneTitlesAsync(AppDbContext db, Guid userId, ActivityType type) =>
        db.Database.SqlQueryRaw<string>(DoneTitlesSql, userId, type.ToString()).ToListAsync();
}
