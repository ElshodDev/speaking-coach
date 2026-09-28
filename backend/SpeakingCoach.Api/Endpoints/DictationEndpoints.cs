using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SpeakingCoach.Api.Data;
using SpeakingCoach.Api.Services;

namespace SpeakingCoach.Api.Endpoints;

public record DictationResultRequest(string? Level, int Sentences, int CorrectWords, int TotalWords);

/// <summary>
/// Diktant brauzerda ishlaydi (AI'siz); server faqat natijani tarixga yozadi — XP, seriya va reja uchun.
/// XP yig'ishga qarshi: natija mantiqan tekshiriladi va Toshkent kuniga ko'pi bilan 30 ta natija
/// saqlanadi (keyingilari — saved=false, capped=true; ekrandagi natija baribir ko'rinadi).
/// </summary>
public static class DictationEndpoints
{
    public static void MapDictationEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/dictation/result", async (DictationResultRequest body, HttpRequest request, AuthService auth, AppDbContext db) =>
        {
            var userId = await auth.GetCurrentUserIdAsync(request);
            if (userId is null) return Results.Ok(new { saved = false });
            if (!XpGuard.ValidDictation(body.Sentences, body.CorrectWords, body.TotalWords))
                return Results.BadRequest(request.Error("dictation.bad"));

            var dayStart = TashkentTime.DayStartUtc(DateTime.UtcNow);
            var today = await db.Activities.CountAsync(a => a.UserId == userId && a.Type == ActivityType.Dictation && a.CreatedAtUtc >= dayStart);
            if (today >= XpGuard.DictationsPerDay) return Results.Ok(new { saved = false, capped = true });

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
        }).RequireRateLimiting(RateLimits.WritePolicy);
    }
}
