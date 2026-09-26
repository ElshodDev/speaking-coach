using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SpeakingCoach.Api.Data;
using SpeakingCoach.Api.Services;

namespace SpeakingCoach.Api.Endpoints;

public record ComprehensionSubmitRequest(Guid ExerciseId, List<int> Answers);
public record ComprehensionGenerateRequest(string? Level);

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
                AiQuotaService quotas,
                ILogger<Program> logger) =>
            {
                // Kunlik AI limiti: yangi matn yaratish Gemini'ga murojaat.
                var limited = await quotas.CheckAsync(request, AiKind.Exercise);
                if (limited is not null) return limited;

                // Javob berilmay tashlab ketilgan eski mashqlarni tozalaymiz —
                // alohida fon vazifasi (background job) o'rniga shu yerda,
                // oddiy va yetarli.
                var cutoff = DateTime.UtcNow.AddDays(-1);
                await db.PendingExercises.Where(p => p.CreatedAtUtc < cutoff).ExecuteDeleteAsync();

                try
                {
                    var exercise = await service.GenerateAsync(type, LearnerLevel.Normalize(body?.Level));
                    var pending = new PendingExercise
                    {
                        Id = Guid.NewGuid(),
                        Type = type,
                        Payload = JsonSerializer.Serialize(exercise),
                        CreatedAtUtc = DateTime.UtcNow,
                    };
                    db.PendingExercises.Add(pending);
                    await db.SaveChangesAsync();
                    await quotas.RecordAsync(request, AiKind.Exercise);

                    // Brauzerga to'g'ri javoblar (correctIndex) va izohlar
                    // YUBORILMAYDI — faqat savol va variantlar.
                    return Results.Ok(new
                    {
                        exerciseId = pending.Id,
                        title = exercise.Title,
                        passage = exercise.Passage,
                        questions = exercise.Questions.Select(q => new { question = q.Question, options = q.Options }),
                    });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "{Kind} mashqini yaratishda xato", path);
                    return Results.Problem(detail: request.T("ai_unavailable"), statusCode: 502);
                }
            }).RequireRateLimiting("ai");

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
                await db.SaveChangesAsync();

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
            }).RequireRateLimiting("ai");

            app.MapGet($"/api/{path}/history", (HttpRequest request, AppDbContext db, AuthService auth) =>
                HistoryQueries.GetHistoryAsync(type, request, db, auth));
        }
    }
}
