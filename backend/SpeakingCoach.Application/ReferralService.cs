using Microsoft.EntityFrameworkCore;
using SpeakingCoach.Domain.Rules;
namespace SpeakingCoach.Application;

/// <summary>GET /api/referral javobi.</summary>
public record ReferralInfo(bool Enabled, string Code, int Invited, bool BonusActive, int BonusDays, int BonusExtraExercises);

/// <summary>
/// Taklif (referral): kod, nechta do'st qo'shilgani va mukofot. Mukofot bazada alohida saqlanmaydi —
/// User.ReferredByUserId va hisob ochilgan vaqtdan hisoblanadi, shuning uchun qo'shimcha jadval/ustun kerak emas.
/// Suiiste'mol cheklovlari: taklif qilingan hisob emaili TASDIQLANGAN bo'lishi shart; mukofot qo'shilmaydi
/// (bir necha taklif ham bir xil +5); o'z-o'zini taklif qilib bo'lmaydi (kod faqat YANGI hisob ochilganda qo'llanadi).
/// </summary>
public class ReferralService(IAppDbContext db, FeatureFlags flags)
{
    public bool Enabled => flags.Referral;

    /// <summary>Foydalanuvchining kunlik AI mashq limitiga qo'shiladigan qo'shimcha urinishlar (0 yoki BonusExtraExercises).</summary>
    public static async Task<int> ExtraExercisesAsync(IAppDbContext db, User user, DateTime nowUtc)
    {
        // Taklif qilingan odam: o'z hisobi ochilganidan BonusDays kun ichida (bazaga murojaatsiz).
        var asReferred = ReferralRules.ExtraExercises(user.ReferredByUserId is not null, user.CreatedAtUtc, false, nowUtc);
        if (asReferred > 0) return asReferred;
        // Taklif qilgan odam: oxirgi BonusDays kunda tasdiqlangan do'sti qo'shilgan bo'lsa.
        var cutoff = ReferralRules.RecentSignupCutoff(nowUtc);
        var has = await db.Users.AnyAsync(r => r.ReferredByUserId == user.Id && r.EmailVerifiedAtUtc != null && r.CreatedAtUtc > cutoff);
        return ReferralRules.ExtraExercises(false, user.CreatedAtUtc, has, nowUtc);
    }

    public async Task<ReferralInfo> GetAsync(User user)
    {
        var code = ReferralRules.Encode(user.Id);
        if (!Enabled || DemoAccount.IsDemo(user.Email))
            return new ReferralInfo(false, code, 0, false, ReferralRules.BonusDays, ReferralRules.BonusExtraExercises);
        var invited = await db.Users.CountAsync(r => r.ReferredByUserId == user.Id && r.EmailVerifiedAtUtc != null);
        var extra = await ExtraExercisesAsync(db, user, DateTime.UtcNow);
        return new ReferralInfo(true, code, invited, extra > 0, ReferralRules.BonusDays, ReferralRules.BonusExtraExercises);
    }

    /// <summary>
    /// Ro'yxatdan o'tishda: kod haqiqiy, tasdiqlangan (demo emas) hisobga tegishli va o'zi emas bo'lsa — uning ID'si.
    /// Aks holda null (noto'g'ri kod ro'yxatdan o'tishni to'xtatmaydi).
    /// </summary>
    public static async Task<Guid?> ResolveReferrerAsync(IAppDbContext db, string? code, string newEmail)
    {
        var id = ReferralRules.Decode(code);
        if (id is null) return null;
        var referrer = await db.Users
            .Where(u => u.Id == id.Value && u.EmailVerifiedAtUtc != null)
            .Select(u => new { u.Id, u.Email })
            .FirstOrDefaultAsync();
        if (referrer is null || referrer.Email == newEmail || DemoAccount.IsDemo(referrer.Email)) return null;
        return referrer.Id;
    }

    /// <summary>
    /// Telegram orqali yangi ochilgan hisobga taklifni biriktiradi (bir marta, atomar: faqat ReferredByUserId hali bo'sh bo'lsa).
    /// true — biriktirildi; false — qoidalarga to'g'ri kelmadi (flag o'chiq, eski hisob, noto'g'ri kod va h.k.) — xato emas.
    /// </summary>
    public async Task<bool> ClaimAsync(User user, string? code)
    {
        if (!Enabled || DemoAccount.IsDemo(user.Email)) return false;
        var now = DateTime.UtcNow;
        if (!ReferralRules.CanClaim(user.ReferredByUserId, user.SignupMethod, user.CreatedAtUtc, now)) return false;
        var referrerId = await ResolveReferrerAsync(db, code, user.Email);
        if (referrerId is null || referrerId == user.Id) return false;
        var updated = await db.Users
            .Where(u => u.Id == user.Id && u.ReferredByUserId == null)
            .ExecuteUpdateAsync(x => x.SetProperty(u => u.ReferredByUserId, referrerId));
        return updated > 0;
    }
}
