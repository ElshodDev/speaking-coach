namespace SpeakingCoach.Api.Endpoints;

public record UpgradeRequest(string? Target);
public record UpgradeCardsRequest(string? Target, List<int>? Items);

/// <summary>
/// "Javobimni yaxshila": saqlangan Speaking / Part 2 / Writing urinishini maqsadli darajada qayta yozish.
/// Faqat kirgan foydalanuvchining o'z urinishi (ID bo'yicha, egasi tekshiriladi). Natija urinish ichida
/// keshlanadi ("upgrades" → maqsad) — qayta ochilganda AI limiti sarflanmaydi.
/// </summary>
public static class UpgradeEndpoints
{
    public static void MapUpgradeEndpoints(this IEndpointRouteBuilder app)
    {
        static IResult LoginFirst(HttpRequest r) => Results.Json(r.Error("login.required"), statusCode: StatusCodes.Status401Unauthorized);

        app.MapPost("/api/upgrade/{id:guid}", async (Guid id, UpgradeRequest body, HttpRequest request, AuthService auth,
            PracticeStore store, AiQuotaService quotas, IAnswerUpgrader upgrader, ILogger<Program> logger) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return LoginFirst(request);
            var activity = await store.OwnAttemptAsync(id, user.Id);
            if (activity is null) return Results.NotFound(request.Error("upgrade.not_found"));
            var target = AnswerUpgradeRules.NormalizeTarget(body.Target) ?? AnswerUpgradeRules.DefaultTarget(user.Level);

            if (AnswerUpgradeRules.Cached(activity.ResponseData, target) is { } cached)
                return Results.Ok(new { result = cached, cached = true });

            var source = AnswerUpgradeRules.SourceOf(activity.Type, activity.PromptData, activity.ResponseData);
            if (source is null) return Results.BadRequest(request.Error("upgrade.no_text"));
            if (AnswerUpgradeRules.WordCount(source.Text) < AnswerUpgradeRules.MinWords)
                return Results.BadRequest(request.Error("upgrade.too_short", AnswerUpgradeRules.MinWords));
            if (source.Text.Length > AnswerUpgradeRules.MaxChars) source = source with { Text = source.Text[..AnswerUpgradeRules.MaxChars] };

            var limited = await quotas.CheckAsync(request, AiKind.Exercise);
            if (limited is not null) return limited;
            try
            {
                var result = await upgrader.UpgradeAsync(source, user.Level, target, RequestTexts.LangOf(request), request.HttpContext.RequestAborted);
                await store.SaveUpgradeAsync(activity, AnswerUpgradeRules.WithCached(activity.ResponseData, result));
                await quotas.RecordAsync(request, AiKind.Exercise);
                return Results.Ok(new { result, cached = false });
            }
            catch (Exception ex) when (AiErrors.Handles(ex, request))
            {
                logger.LogError(ex, "Javobni yaxshilab bo'lmadi: {Id}", id);
                return Results.Problem(detail: request.T("ai_unavailable"), statusCode: 502);
            }
        }).RequireRateLimiting(RateLimits.AiPolicy);

        // Tanlangan yaxshilashlar — takrorlash kartalariga (AI'siz, keshlangan natijadan).
        app.MapPost("/api/upgrade/{id:guid}/cards", async (Guid id, UpgradeCardsRequest body, HttpRequest request, AuthService auth,
            PracticeStore store, ReviewService reviews) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return LoginFirst(request);
            var activity = await store.OwnAttemptAsync(id, user.Id);
            var target = AnswerUpgradeRules.NormalizeTarget(body.Target);
            var result = activity is null || target is null ? null : AnswerUpgradeRules.Cached(activity.ResponseData, target);
            if (result is null) return Results.NotFound(request.Error("upgrade.not_found"));
            var cards = AnswerUpgradeRules.Cards(result, body.Items ?? []);
            var added = await reviews.AddCardsAndSaveAsync(user.Id, activity!.Type, cards);
            return Results.Ok(new { added });
        }).RequireRateLimiting(RateLimits.WritePolicy);
    }
}
