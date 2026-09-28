using System.Text.Json;
using SpeakingCoach.Api.Data;
using SpeakingCoach.Api.Services;

namespace SpeakingCoach.Api.Endpoints;

public record DictationResultRequest(string? Level, int Sentences, int CorrectWords, int TotalWords);

/// <summary>Diktant brauzerda ishlaydi (AI'siz); server faqat natijani tarixga yozadi — XP, seriya va reja uchun.</summary>
public static class DictationEndpoints
{
    public static void MapDictationEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/dictation/result", async (DictationResultRequest body, HttpRequest request, AuthService auth, AppDbContext db) =>
        {
            var userId = await auth.GetCurrentUserIdAsync(request);
            if (userId is null) return Results.Ok(new { saved = false });
            if (body.Sentences is < 1 or > 50 || body.TotalWords is < 1 or > 2000 || body.CorrectWords < 0 || body.CorrectWords > body.TotalWords)
                return Results.BadRequest(request.Error("dictation.bad"));
            db.Activities.Add(new Activity
            {
                Id = Guid.NewGuid(),
                Type = ActivityType.Dictation,
                UserId = userId,
                CreatedAtUtc = DateTime.UtcNow,
                PromptData = JsonSerializer.Serialize(new { level = LearnerLevel.Normalize(body.Level) }, Talk.Json),
                ResponseData = JsonSerializer.Serialize(new
                {
                    sentences = body.Sentences,
                    correctWords = body.CorrectWords,
                    totalWords = body.TotalWords,
                    accuracy = (int)Math.Round(100.0 * body.CorrectWords / body.TotalWords),
                }, Talk.Json),
            });
            await db.SaveChangesAsync();
            return Results.Ok(new { saved = true });
        }).RequireRateLimiting("auth");
    }
}
