using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SpeakingCoach.Api.Data;
using SpeakingCoach.Api.Services;

namespace SpeakingCoach.Api.Endpoints;

public record TalkStartRequest(string Scenario, string? Level);

/// <summary>
/// AI suhbatdosh: /start — suhbat ochiladi (kunlik limitdan bitta mashq), /turn — foydalanuvchi
/// javobi (ovoz yoki matn) va AI'ning keyingi gapi, /finish — xulosa, tuzatishlar kartaga, tarixga.
/// Suhbat tarixi serverda (PendingExercises) — brauzer uni o'zgartira olmaydi.
/// Har bir navbat — kunlik navbatlar limitidan (Ai:TalkTurnsPerDay); bitta suhbatda
/// bir vaqtda bitta navbat (ikkinchisi — 409).
/// </summary>
public static class TalkEndpoints
{
    private const long MaxAudioBytes = 5 * Uploads.Mb;
    private const long MaxBodyBytes = MaxAudioBytes + Uploads.FormOverheadBytes;

    public static void MapTalkEndpoints(this IEndpointRouteBuilder app)
    {
        static IResult LoginFirst(HttpRequest r) => Results.Json(r.Error("talk.login"), statusCode: StatusCodes.Status401Unauthorized);
        static IResult Busy(HttpRequest r) => Results.Json(r.Error("talk.busy"), statusCode: StatusCodes.Status409Conflict);
        static IResult DailyLimit(HttpRequest r, int limit) =>
            Results.Json(r.Error("talk.daily_limit", limit), statusCode: StatusCodes.Status429TooManyRequests);

        async Task<(PendingExercise Row, TalkState State)?> LoadAsync(AppDbContext db, Guid id, Guid userId)
        {
            var row = await db.PendingExercises.FirstOrDefaultAsync(p => p.Id == id && p.Type == ActivityType.Conversation);
            if (row is null) return null;
            TalkState? state;
            try
            {
                state = JsonSerializer.Deserialize<TalkState>(row.Payload, Talk.Json);
            }
            catch (JsonException)
            {
                return null;
            }
            return state is null || state.UserId != userId ? null : (row, state);
        }

        app.MapPost("/api/talk/start", async (TalkStartRequest body, HttpRequest request, AuthService auth, AppDbContext db, AiQuotaService quotas,
            AiQuotaOptions limits, AiDailyCaps caps, AdminOptions admins) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return LoginFirst(request);
            var scenario = Talk.Find(body.Scenario);
            if (scenario is null) return Results.BadRequest(request.Error("talk.not_found"));

            // Bugungi navbatlar tugagan bo'lsa — mashq limitini behuda sarflamaymiz.
            if (!admins.IsAdmin(user) && caps.Used(AiDailyCaps.TalkKey(user.Id), AiQuotaService.Today(DateTime.UtcNow)) >= limits.TalkTurns)
                return DailyLimit(request, limits.TalkTurns);

            // Bitta suhbat = bitta mashq (kunlik AI limiti). Navbatlar alohida sanaladi.
            var limited = await quotas.CheckAsync(request, AiKind.Exercise);
            if (limited is not null) return limited;

            var cutoff = DateTime.UtcNow.AddDays(-1);
            await db.PendingExercises.Where(p => p.CreatedAtUtc < cutoff).ExecuteDeleteAsync();

            var level = LearnerLevel.Normalize(body.Level ?? user.Level);
            var state = new TalkState(scenario.Id, level, user.Id, [new TalkLine("ai", scenario.Opening)], []);
            var row = new PendingExercise
            {
                Id = Guid.NewGuid(),
                Type = ActivityType.Conversation,
                Payload = JsonSerializer.Serialize(state, Talk.Json),
                CreatedAtUtc = DateTime.UtcNow,
            };
            db.PendingExercises.Add(row);
            await db.SaveChangesAsync();
            await quotas.RecordAsync(request, AiKind.Exercise);
            return Results.Ok(new { talkId = row.Id, scenario = scenario.Id, level, opening = scenario.Opening, maxTurns = Talk.MaxTurns });
        }).RequireRateLimiting(RateLimits.AiPolicy);

        // multipart: "audio" (ovoz) yoki "text" (yozib javob berish).
        app.MapPost("/api/talk/{id:guid}/turn", async (Guid id, HttpRequest request, AuthService auth, AppDbContext db, ITalkAi ai,
            TalkLocks locks, AiDailyCaps caps, AiQuotaOptions limits, AdminOptions admins, ILogger<Program> logger) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return LoginFirst(request);
            if (!request.HasFormContentType) return Results.BadRequest(request.Error("speaking.multipart"));
            if (Uploads.DeclaredTooLarge(request, MaxBodyBytes))
                return Results.Json(request.Error("audio.too_big", MaxAudioBytes / Uploads.Mb), statusCode: StatusCodes.Status413PayloadTooLarge);

            // Bitta suhbatda bir vaqtda bitta navbat.
            using var turnLock = locks.TryEnter(id);
            if (turnLock is null) return Busy(request);

            var loaded = await LoadAsync(db, id, user.Id);
            if (loaded is not { } l) return Results.NotFound(request.Error("talk.not_found"));
            var (row, state) = l;
            var scenario = Talk.Find(state.Scenario);
            if (scenario is null) return Results.NotFound(request.Error("talk.not_found"));
            if (Talk.UserTurns(state) >= Talk.MaxTurns) return Results.BadRequest(request.Error("talk.limit", Talk.MaxTurns));

            var ct = request.HttpContext.RequestAborted;
            // Audio xotiraga o'qilishidan oldin — "katta so'rov" joyi.
            using var slot = await Uploads.EnterIfLargeAsync(request, ct);
            var form = await request.ReadFormAsync(ct);
            var typed = form["text"].ToString().Trim();
            var file = form.Files.GetFile("audio");
            byte[]? audio = null;
            if (typed.Length == 0)
            {
                if (file is null || file.Length == 0) return Results.BadRequest(request.Error("talk.empty"));
                if (file.Length > MaxAudioBytes) return Results.BadRequest(request.Error("audio.too_big", MaxAudioBytes / Uploads.Mb));
            }
            else if (typed.Length > Talk.MaxTextChars)
            {
                return Results.BadRequest(request.Error("text.too_long", Talk.MaxTextChars));
            }

            // Kunlik navbatlar (xotirada, Toshkent kuni). Xato yoki "eshitilmadi" — qaytariladi.
            var capKey = AiDailyCaps.TalkKey(user.Id);
            var day = AiQuotaService.Today(DateTime.UtcNow);
            var charged = !admins.IsAdmin(user);
            if (charged && !caps.TryTake(capKey, day, limits.TalkTurns)) return DailyLimit(request, limits.TalkTurns);

            TalkTurnResult result;
            try
            {
                if (typed.Length == 0) audio = await Uploads.ReadAsync(file!, ct);
                result = await ai.TurnAsync(scenario, state.Level, state.Lines, Texts.LangOf(request), audio, file?.ContentType,
                    typed.Length > 0 ? typed : null, ct);
            }
            catch (Exception ex)
            {
                if (charged) caps.Refund(capKey, day);
                if (!AiErrors.Handles(ex, request)) throw;
                logger.LogError(ex, "Suhbat navbati bajarilmadi");
                return Results.Problem(detail: request.T("ai_unavailable"), statusCode: 502);
            }
            // Ovozda inglizcha gap eshitilmadi — navbat sanalmaydi, qayta yozib olsin.
            if (result.Heard.Length == 0)
            {
                if (charged) caps.Refund(capKey, day);
                return Results.Json(request.Error("talk.not_heard"), statusCode: StatusCodes.Status422UnprocessableEntity);
            }

            state.Lines.Add(new TalkLine("user", result.Heard));
            state.Lines.Add(new TalkLine("ai", result.Reply));
            if (result.Tip is not null) state.Tips.Add(result.Tip);
            row.Payload = JsonSerializer.Serialize(state, Talk.Json);
            await db.SaveChangesAsync(CancellationToken.None);
            return Results.Ok(new { heard = result.Heard, reply = result.Reply, tip = result.Tip, turn = Talk.UserTurns(state), maxTurns = Talk.MaxTurns });
        }).RequireRateLimiting(RateLimits.AiPolicy).WithBodyLimit(MaxBodyBytes);

        app.MapPost("/api/talk/{id:guid}/finish", async (Guid id, HttpRequest request, AuthService auth, AppDbContext db, ITalkAi ai, ReviewService reviews,
            TalkLocks locks, ILogger<Program> logger) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return LoginFirst(request);
            // Navbat hali bajarilayotgan bo'lsa — yakunlamaymiz (oxirgi javob yo'qolmasin).
            using var turnLock = locks.TryEnter(id);
            if (turnLock is null) return Busy(request);
            var loaded = await LoadAsync(db, id, user.Id);
            if (loaded is not { } l) return Results.NotFound(request.Error("talk.not_found"));
            var (row, state) = l;
            var scenario = Talk.Find(state.Scenario);
            if (scenario is null) return Results.NotFound(request.Error("talk.not_found"));
            if (Talk.UserTurns(state) == 0) return Results.BadRequest(request.Error("talk.no_turns"));

            TalkSummary summary;
            try
            {
                summary = await ai.SummarizeAsync(scenario, state.Level, state.Lines, Texts.LangOf(request), request.HttpContext.RequestAborted);
            }
            catch (Exception ex) when (AiErrors.Handles(ex, request))
            {
                logger.LogError(ex, "Suhbat xulosasi tuzilmadi");
                return Results.Problem(detail: request.T("ai_unavailable"), statusCode: 502);
            }

            // Xulosadagi tuzatishlar + suhbat davomidagi maslahatlar (takrorlanmasin).
            var corrections = summary.TopCorrections
                .Concat(state.Tips)
                .DistinctBy(c => c.Original.Trim().ToLowerInvariant())
                .Take(8)
                .ToList();
            var activityId = Guid.NewGuid();
            db.Activities.Add(new Activity
            {
                Id = activityId,
                Type = ActivityType.Conversation,
                UserId = user.Id,
                CreatedAtUtc = DateTime.UtcNow,
                PromptData = JsonSerializer.Serialize(new { scenario = scenario.Id, level = state.Level, lines = state.Lines }, Talk.Json),
                ResponseData = JsonSerializer.Serialize(summary with { TopCorrections = corrections }, Talk.Json),
            });
            db.PendingExercises.Remove(row);
            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return Results.Conflict(request.Error("talk.not_found"));
            }
            var newCards = await reviews.AddCardsAsync(user.Id, ActivityType.Conversation, ReviewCardFactory.FromCorrections(corrections));
            return Results.Ok(new { id = activityId, result = summary with { TopCorrections = corrections }, turns = Talk.UserTurns(state), newCards });
        }).RequireRateLimiting(RateLimits.AiPolicy);
    }
}
