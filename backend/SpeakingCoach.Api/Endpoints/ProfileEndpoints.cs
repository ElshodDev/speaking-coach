namespace SpeakingCoach.Api.Endpoints;

public record ReferralClaimRequest(string? Ref);
public record ProfileUpdateRequest(string? DisplayName, string? Level, bool ShowOnLeaderboard);
public record WordExplainRequest(string Word, string? Sentence, string? Level);
public record OnboardingRequest(bool Skip, string? Goal, string? Level, string? TargetScore, string? ExamDate, int? DailyMinutes, int TzOffsetMinutes = 0);

public static class ProfileEndpoints
{
    private static IResult Unauthorized(HttpRequest request) =>
        Results.Json(request.Error("login.required"), statusCode: StatusCodes.Status401Unauthorized);

    public static void MapProfileEndpoints(this IEndpointRouteBuilder app)
    {
        // Bugungi AI limiti: qancha ishlatildi va qancha qoldi (mehmon uchun ham).
        app.MapGet("/api/usage", async (HttpRequest request, AiQuotaService quotas) =>
        {
            var s = await quotas.GetStatusAsync(request);
            return Results.Ok(new
            {
                exercises = new { used = s.Exercises.Used, limit = s.Exercises.Limit, left = s.Exercises.Left },
                words = new { used = s.Words.Used, limit = s.Words.Limit, left = s.Words.Left },
                shadowing = s.Shadowing is { } sh ? new { used = sh.Used, limit = sh.Limit, left = sh.Left } : null,
                unlimited = s.Unlimited,
                guest = s.Guest,
            });
        });

        // Taklif (referral): mening kodim, nechta do'st qo'shildi, mukofot faolmi. Feature flag o'chiq bo'lsa enabled=false.
        app.MapGet("/api/referral", async (HttpRequest request, AuthService auth, ReferralService referrals) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return Unauthorized(request);
            return Results.Ok(await referrals.GetAsync(user));
        });

        // Telegram orqali yangi ochilgan hisobga taklifni biriktiradi (kod brauzerda saqlangan; hisob ochilgach yuboriladi).
        app.MapPost("/api/referral/claim", async (ReferralClaimRequest body, HttpRequest request, AuthService auth, ReferralService referrals) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return Unauthorized(request);
            return Results.Ok(new { claimed = await referrals.ClaimAsync(user, body.Ref) });
        }).RequireRateLimiting(RateLimits.WritePolicy);

        app.MapGet("/api/profile", async (HttpRequest request, AuthService auth, AdminOptions admins) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return Unauthorized(request);
            return Results.Ok(new
            {
                email = user.Email,
                displayName = user.DisplayName,
                level = LearnerLevel.Normalize(user.Level),
                showOnLeaderboard = user.ShowOnLeaderboard,
                isAdmin = admins.IsAdmin(user),
                isDemo = DemoAccount.IsDemo(user.Email),
                goal = user.Goal,
                targetScore = user.TargetScore,
                examDate = user.ExamDate?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                dailyMinutes = user.DailyMinutes ?? PlanLogic.DefaultMinutes,
                onboarded = user.OnboardedAtUtc is not null,
                // Kirish usullari: realEmail=false — email sintetik (Telegram/demo), frontend uni ko'rsatmaydi.
                signIn = await auth.GetSignInMethodsAsync(user),
            });
        });

        // Tanishtiruv: maqsad, daraja, maqsad ball, imtihon sanasi, kunlik vaqt.
        // "Keyinroq" (Skip) — faqat qayta ko'rsatilmasligi uchun belgilanadi.
        app.MapPut("/api/profile/onboarding", async (OnboardingRequest body, HttpRequest request, AuthService auth, ProfileService profiles) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return Unauthorized(request);
            var r = await profiles.SaveOnboardingAsync(user, new OnboardingInput(body.Skip, body.Goal, body.Level, body.TargetScore, body.ExamDate, body.DailyMinutes, body.TzOffsetMinutes));
            if (!r.Ok) return Results.BadRequest(request.Error(r.Failure!.Key));
            var o = r.Value!;
            return Results.Ok(new { goal = o.Goal, level = o.Level, targetScore = o.TargetScore, onboarded = o.Onboarded });
        }).RequireRateLimiting(RateLimits.WritePolicy);

        // Bugungi reja: takrorlash, kunning ko'nikmasi, (imtihonga tayyorlanayotganlarga) mock.
        // Imtihonga tayyorlik bahosi (Progress sahifasi): ko'nikmalar bo'yicha taxminiy natija, maqsadgacha farq, fokus.
        app.MapGet("/api/readiness", async (HttpRequest request, AuthService auth, ReadinessService readiness, int tzOffsetMinutes = 0) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return Unauthorized(request);
            var now = DateTime.UtcNow;
            var today = ReviewScheduler.ToLocalDate(now, Math.Clamp(tzOffsetMinutes, -840, 840));
            return Results.Ok(await readiness.ForUserAsync(user, today, now));
        });

        app.MapGet("/api/plan/today", async (HttpRequest request, AuthService auth, TodayService todayService, int tzOffsetMinutes = 0) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return Unauthorized(request);
            var (plan, _) = await todayService.PlanAsync(user, tzOffsetMinutes);
            return Results.Ok(new
            {
                plan.Goal,
                user.TargetScore,
                examDate = user.ExamDate?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                plan.DaysToExam,
                plan.FocusSkill,
                plan.FocusReason,
                dailyMinutes = user.DailyMinutes ?? PlanLogic.DefaultMinutes,
                onboarded = user.OnboardedAtUtc is not null,
                items = plan.Items,
            });
        });

        app.MapPut("/api/profile", async (ProfileUpdateRequest body, HttpRequest request, AuthService auth, ProfileService profiles) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return Unauthorized(request);
            var r = await profiles.UpdateAsync(user, body.DisplayName, body.Level, body.ShowOnLeaderboard);
            if (!r.Ok) return Results.BadRequest(request.Error(r.Failure!.Key));
            var p = r.Value!;
            return Results.Ok(new { displayName = p.DisplayName, level = p.Level, showOnLeaderboard = p.ShowOnLeaderboard });
        }).RequireRateLimiting(RateLimits.WritePolicy);

        app.MapGet("/api/progress", async (HttpRequest request, AuthService auth, ProgressService progress, int tzOffsetMinutes = 0) =>
        {
            var userId = await auth.GetCurrentUserIdAsync(request);
            if (userId is null) return Unauthorized(request);
            return Results.Ok(await progress.GetProgressAsync(userId.Value, Math.Clamp(tzOffsetMinutes, -840, 840)));
        });

        app.MapGet("/api/leaderboard", async (HttpRequest request, AuthService auth, ProgressService progress) =>
        {
            var userId = await auth.GetCurrentUserIdAsync(request);
            if (userId is null) return Unauthorized(request);
            return Results.Ok(await progress.GetLeaderboardAsync(userId.Value));
        });

        // Admin: 401 — kirmagan; 403 — kirgan, lekin admin emas.
        app.MapGet("/api/admin/overview", async (HttpRequest request, AuthService auth, AdminOptions admins, AdminService admin, int tzOffsetMinutes = 0) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return Unauthorized(request);
            if (!admins.IsAdmin(user))
            {
                return Results.Json(request.Error("admin.only"), statusCode: StatusCodes.Status403Forbidden);
            }
            return Results.Ok(await admin.GetOverviewAsync(Math.Clamp(tzOffsetMinutes, -840, 840)));
        });

        // Retention (D1/D7): mavjud jadvallardan hisoblanadi, sxema o'zgarmaydi. Faqat admin.
        app.MapGet("/api/admin/retention", async (HttpRequest request, AuthService auth, AdminOptions admins, RetentionService retention, int days = RetentionService.DefaultDays) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return Unauthorized(request);
            if (!admins.IsAdmin(user))
            {
                return Results.Json(request.Error("admin.only"), statusCode: StatusCodes.Status403Forbidden);
            }
            return Results.Ok(await retention.GetAsync(days, DateTime.UtcNow, request.HttpContext.RequestAborted));
        });

        // Tizim holati: baza sxemasi, Telegram bot, sozlamalar. Sirlar qaytarilmaydi.
        app.MapGet("/api/admin/system", async (
            HttpRequest request, AuthService auth, AdminOptions admins, IAppDbContext db, ISchemaInspector schema,
            ITelegramApi telegram, TelegramOptions tg,
            IEmailSender email, IGoogleSignIn google, IConfiguration config) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return Unauthorized(request);
            if (!admins.IsAdmin(user))
            {
                return Results.Json(request.Error("admin.only"), statusCode: StatusCodes.Status403Forbidden);
            }
            return Results.Ok(await SystemHealth.GetAsync(db, schema, telegram, tg, email, google, config, request.HttpContext.RequestAborted));
        });

        // So'z ma'nosi — mehmonga ham ochiq (faqat kartaga saqlash uchun kirish kerak).
        // Tarjima so'rov tilida: o'zbekcha, ruscha yoki (en) sodda inglizcha sinonim.
        app.MapPost("/api/words/explain", async (WordExplainRequest body, HttpRequest request, IWordService words, AiQuotaService quotas, ILogger<Program> logger) =>
        {
            var clean = GeminiWordService.Sanitize(body.Word, body.Sentence, out var errorKey);
            if (clean is null) return Results.BadRequest(request.Error(errorKey!));
            var limited = await quotas.CheckAsync(request, AiKind.Word);
            if (limited is not null) return limited;
            try
            {
                var explanation = await words.ExplainAsync(
                    clean.Value.Word, clean.Value.Sentence, LearnerLevel.Normalize(body.Level), RequestTexts.LangOf(request));
                await quotas.RecordAsync(request, AiKind.Word);
                return Results.Ok(explanation);
            }
            catch (Exception ex) when (AiErrors.Handles(ex, request))
            {
                logger.LogError(ex, "So'z izohida xato: {Word}", clean.Value.Word);
                return Results.Problem(detail: request.T("ai_unavailable"), statusCode: 502);
            }
        }).RequireRateLimiting(RateLimits.AiPolicy);
    }
}
