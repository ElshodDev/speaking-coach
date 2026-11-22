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
        app.MapPost("/api/dictation/result", async (DictationResultRequest body, HttpRequest request, AuthService auth, PracticeStore store) =>
        {
            var userId = await auth.GetCurrentUserIdAsync(request);
            if (userId is null) return Results.Ok(new { saved = false });
            if (!XpGuard.ValidDictation(body.Sentences, body.CorrectWords, body.TotalWords))
                return Results.BadRequest(request.Error("dictation.bad"));

            return await store.SaveDictationAsync(userId.Value, new DictationInput(body.Level, body.Sentences, body.CorrectWords, body.TotalWords))
                ? Results.Ok(new { saved = true })
                : Results.Ok(new { saved = false, capped = true });
        }).RequireRateLimiting(RateLimits.WritePolicy);
    }
}
