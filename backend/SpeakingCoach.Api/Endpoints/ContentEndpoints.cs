using SpeakingCoach.Api.Data;
using SpeakingCoach.Api.Services;
using SpeakingCoach.Api.Services.Content;

namespace SpeakingCoach.Api.Endpoints;

public record TopicSuggestRequest(string Kind, string? Category, string? Level, List<string>? Avoid);
public record GrammarQuizRequest(string Topic, string? Level);

/// <summary>Tayyor materiallarga ixtiyoriy AI qo'shimchasi: yangi mavzu va grammatika savollari.</summary>
public static class ContentEndpoints
{
    public static void MapContentEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/topics/suggest", async (TopicSuggestRequest body, HttpRequest request, ContentAi ai, AiQuotaService quotas, ILogger<Program> logger) =>
        {
            var kind = body.Kind == "writing" ? "writing" : "speaking";
            if (!ContentAi.ValidCategory(kind, body.Category)) return Results.BadRequest(request.Error("topic.empty"));
            var limited = await quotas.CheckAsync(request, AiKind.Word);
            if (limited is not null) return limited;
            var avoid = (body.Avoid ?? []).Where(a => !string.IsNullOrWhiteSpace(a)).Select(a => a.Length > 300 ? a[..300] : a).Take(8).ToList();
            try
            {
                var topic = await ai.SuggestTopicAsync(kind, body.Category!, LearnerLevel.Normalize(body.Level), avoid, request.HttpContext.RequestAborted);
                await quotas.RecordAsync(request, AiKind.Word);
                return Results.Ok(topic);
            }
            catch (Exception ex) when (AiErrors.Handles(ex, request))
            {
                logger.LogWarning(ex, "AI mavzu taklif qila olmadi");
                return Results.Problem(detail: request.T("ai_unavailable"), statusCode: 502);
            }
        }).RequireRateLimiting(RateLimits.AiPolicy);

        app.MapPost("/api/grammar/quiz", async (GrammarQuizRequest body, HttpRequest request, ContentAi ai, AiQuotaService quotas, ILogger<Program> logger) =>
        {
            var topic = (body.Topic ?? "").Trim();
            if (topic.Length is 0 or > 120) return Results.BadRequest(request.Error("topic.empty"));
            var limited = await quotas.CheckAsync(request, AiKind.Exercise);
            if (limited is not null) return limited;
            try
            {
                var quiz = await ai.QuizAsync(topic, LearnerLevel.Normalize(body.Level), Texts.LangOf(request), request.HttpContext.RequestAborted);
                await quotas.RecordAsync(request, AiKind.Exercise);
                return Results.Ok(quiz);
            }
            catch (Exception ex) when (AiErrors.Handles(ex, request))
            {
                logger.LogWarning(ex, "AI grammatika savollarini tuza olmadi");
                return Results.Problem(detail: request.T("ai_unavailable"), statusCode: 502);
            }
        }).RequireRateLimiting(RateLimits.AiPolicy);
    }
}
