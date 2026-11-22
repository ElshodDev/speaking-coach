using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;

namespace SpeakingCoach.Application;

public record OnboardingInput(bool Skip, string? Goal, string? Level, string? TargetScore, string? ExamDate, int? DailyMinutes, int TzOffsetMinutes);
public record OnboardingSaved(string? Goal, string Level, string? TargetScore, bool Onboarded);
public record ProfileSaved(string? DisplayName, string Level, bool ShowOnLeaderboard);

/// <summary>Profil: tanishtiruv (maqsad, daraja, kunlik vaqt) va taxallus/daraja/reyting sozlamalari. HTTP — Api'dagi ProfileEndpoints'da.</summary>
public partial class ProfileService(IAppDbContext db)
{
    // Taxallus: harflar (o'zbekcha ham), raqamlar, bo'sh joy va . _ - ' belgilari.
    [GeneratedRegex(@"^[\p{L}\p{N} ._'\-]{2,30}$")]
    private static partial Regex DisplayNamePattern();

    private static Outcome<T> Bad<T>(string key) => Outcome<T>.Error(key, 400);

    /// <summary>Tanishtiruv. "Keyinroq" (Skip) — faqat qayta ko'rsatilmasligi uchun belgilanadi.</summary>
    public async Task<Outcome<OnboardingSaved>> SaveOnboardingAsync(User user, OnboardingInput body)
    {
        if (!body.Skip)
        {
            var today = ReviewScheduler.ToLocalDate(DateTime.UtcNow, Math.Clamp(body.TzOffsetMinutes, -840, 840));
            var target = string.IsNullOrWhiteSpace(body.TargetScore) ? null : body.TargetScore.Trim().ToUpperInvariant();
            var date = string.IsNullOrWhiteSpace(body.ExamDate) ? null : PlanLogic.ParseDate(body.ExamDate.Trim());
            if (!PlanLogic.IsValidGoal(body.Goal)) return Bad<OnboardingSaved>("goal.bad_goal");
            if (body.Level is not null && !LearnerLevel.IsValid(body.Level)) return Bad<OnboardingSaved>("profile.bad_level");
            if (!PlanLogic.IsValidTarget(body.Goal, target)) return Bad<OnboardingSaved>("goal.bad_target");
            if ((!string.IsNullOrWhiteSpace(body.ExamDate) && date is null) || !PlanLogic.IsValidExamDate(date, today) || (body.Goal == "general" && date is not null))
                return Bad<OnboardingSaved>("goal.bad_date");
            if (!PlanLogic.IsValidMinutes(body.DailyMinutes)) return Bad<OnboardingSaved>("goal.bad_minutes");

            user.Goal = body.Goal;
            if (body.Level is not null) user.Level = LearnerLevel.Normalize(body.Level);
            user.TargetScore = target;
            user.ExamDate = date;
            user.DailyMinutes = body.DailyMinutes ?? PlanLogic.DefaultMinutes;
        }
        user.OnboardedAtUtc ??= DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Outcome<OnboardingSaved>.Success(new OnboardingSaved(user.Goal, user.Level, user.TargetScore, true));
    }

    /// <summary>Taxallus (noyob, reytingda ko'rinishi uchun), daraja va reytingda ko'rinish. Demo hisob reytingga chiqmaydi.</summary>
    public async Task<Outcome<ProfileSaved>> UpdateAsync(User user, string? displayName, string? level, bool showOnLeaderboard)
    {
        if (level is not null && !LearnerLevel.IsValid(level)) return Bad<ProfileSaved>("profile.bad_level");

        var name = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim();
        if (name is not null && !DisplayNamePattern().IsMatch(name)) return Bad<ProfileSaved>("profile.bad_nickname");
        if (showOnLeaderboard && DemoAccount.IsDemo(user.Email)) return Bad<ProfileSaved>("demo.not_allowed");
        if (showOnLeaderboard && name is null) return Bad<ProfileSaved>("profile.nickname_required");
        if (name is not null)
        {
            var lower = name.ToLower();
            if (await db.Users.AnyAsync(u => u.Id != user.Id && u.DisplayName != null && u.DisplayName.ToLower() == lower))
                return Bad<ProfileSaved>("profile.nickname_taken");
        }

        user.DisplayName = name;
        if (level is not null) user.Level = LearnerLevel.Normalize(level);
        user.ShowOnLeaderboard = showOnLeaderboard;
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (db.IsUniqueViolation(ex))
        {
            // Tekshiruvdan keyin, saqlashdan oldin boshqa kimdir shu taxallusni oldi (noyob indeks).
            return Bad<ProfileSaved>("profile.nickname_taken");
        }
        return Outcome<ProfileSaved>.Success(new ProfileSaved(user.DisplayName, user.Level, user.ShowOnLeaderboard));
    }
}
