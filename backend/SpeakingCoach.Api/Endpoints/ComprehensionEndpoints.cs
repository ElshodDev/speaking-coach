namespace SpeakingCoach.Api.Endpoints;

public record ComprehensionSubmitRequest(Guid ExerciseId, List<int> Answers);
/// <summary>Source: "bank" — ilovaning tayyor mashqi (AI limiti sarflanmaydi), "ai" (yoki bo'sh) — Gemini yangisini yaratadi. Seen — mehmon ko'rgan tayyor mashqlar (brauzerda saqlanadi).</summary>
/// <summary>BankId — ro'yxatdan tanlangan aniq tayyor mashq.</summary>
public record ComprehensionGenerateRequest(string? Level, string? Source = null, List<string>? Seen = null, string? BankId = null);

public static class ComprehensionEndpoints
{
    /// <summary>
    /// /api/reading/... va /api/listening/... — bir xil mantiq, faqat ActivityType farq qiladi, shuning uchun ikkalasi bitta
    /// sikldan ro'yxatga olinadi. Mantiq — Application'dagi <see cref="ComprehensionPractice"/>'da; bu yerda faqat HTTP,
    /// AI chaqiruvi va kunlik limitlar.
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
                ComprehensionPractice practice,
                AuthService auth,
                AiQuotaService quotas,
                ILogger<Program> logger) =>
            {
                var level = LearnerLevel.Normalize(body?.Level);
                await practice.CleanStaleAsync();

                async Task<IResult> Serve(ComprehensionExercise exercise, object? bank)
                {
                    var id = await practice.StoreAsync(type, exercise);
                    // Brauzerga to'g'ri javoblar (correctIndex) va izohlar YUBORILMAYDI — faqat savol va variantlar.
                    return Results.Ok(new
                    {
                        exerciseId = id,
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
                    var pick = await practice.PickAsync(type, level, await auth.GetCurrentUserIdAsync(request), body?.Seen, body?.BankId);
                    if (pick is null) return Results.NotFound(request.Error("exercise.not_found"));
                    return await Serve(pick.Item.Exercise, new { id = pick.Item.Id, level = pick.Item.Level, done = pick.Done, total = pick.Total, repeated = pick.Repeated });
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

            // Tayyor mashqlar: tanlangan darajada nechtasi bajarilgan va barcha darajalardagi ro'yxat.
            app.MapGet($"/api/{path}/bank", async (HttpRequest request, ComprehensionPractice practice, AuthService auth, string? level) =>
            {
                var bank = await practice.BankAsync(type, level, await auth.GetCurrentUserIdAsync(request));
                return Results.Ok(new
                {
                    level = bank.Level,
                    done = bank.Done,
                    total = bank.Total,
                    items = bank.Items.Select(x => new { id = x.Id, level = x.Level, title = x.Title, done = x.Done }),
                });
            });

            app.MapPost($"/api/{path}/submit", async (ComprehensionSubmitRequest body, HttpRequest request, ComprehensionPractice practice, AuthService auth) =>
            {
                var r = await practice.SubmitAsync(type, body.ExerciseId, body.Answers, await auth.GetCurrentUserIdAsync(request));
                if (!r.Ok) return Results.Json(request.Error(r.Failure!.Key, r.Failure.Args), statusCode: r.Failure.Status);
                var done = r.Value!;
                return Results.Ok(new
                {
                    done.Result.Score,
                    done.Result.Total,
                    done.Result.Results,
                    // Listening'da matn javobdan keyingina ko'rsatiladi.
                    passage = done.Passage,
                    saved = done.Saved,
                    capped = done.Capped,
                    newCards = done.NewCards,
                });
            }).RequireRateLimiting(RateLimits.WritePolicy); // AI'siz — javoblar serverda tekshiriladi

            app.MapGet($"/api/{path}/history", (HttpRequest request, ComprehensionPractice practice, AuthService auth) =>
                HistoryQueries.GetHistoryAsync(type, request, practice, auth));
        }
    }
}
