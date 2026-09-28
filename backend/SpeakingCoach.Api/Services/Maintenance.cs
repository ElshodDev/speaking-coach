using Microsoft.EntityFrameworkCore;
using SpeakingCoach.Api.Data;

namespace SpeakingCoach.Api.Services;

/// <summary>
/// Muddati o'tgan yozuvlarni tozalash: baza cheksiz o'smasin va eski
/// sessiya/kodlar bekorga saqlanib yotmasin. Ikki joydan chaqiriladi:
/// soatlik cron (/api/telegram/cron, Telegram yoqilgan bo'lsa) va server
/// ichidagi fon xizmati (MaintenanceService — cron bo'lmasa ham ishlaydi).
/// Hammasi qayta chaqirilsa zararsiz (idempotent).
/// </summary>
public static class Maintenance
{
    /// <summary>Kodlar 2 kun saqlanadi — kunlik yuborish limiti ularni sanaydi.</summary>
    public static readonly TimeSpan EmailCodeKeep = TimeSpan.FromDays(2);

    /// <summary>Javob berilmagan mashqlar va tugallanmagan AI suhbatlar.</summary>
    public static readonly TimeSpan PendingKeep = TimeSpan.FromDays(2);

    /// <summary>Kod so'ralgan, lekin hech qachon kirilmagan (tasdiqlanmagan) hisoblar.</summary>
    public static readonly TimeSpan AbandonedSignupKeep = TimeSpan.FromDays(7);

    public static async Task<int> CleanupAsync(AppDbContext db, DateTime nowUtc)
    {
        var codesBefore = nowUtc - EmailCodeKeep;
        var pendingBefore = nowUtc - PendingKeep;
        var removed = 0;
        removed += await db.Sessions.Where(s => s.ExpiresAtUtc < nowUtc).ExecuteDeleteAsync();
        removed += await db.EmailCodes.Where(c => c.ExpiresAtUtc < nowUtc && c.CreatedAtUtc < codesBefore).ExecuteDeleteAsync();
        removed += await db.TelegramLinkTokens.Where(t => t.ExpiresAtUtc < nowUtc).ExecuteDeleteAsync();
        removed += await db.PendingExercises.Where(p => p.CreatedAtUtc < pendingBefore).ExecuteDeleteAsync();
        removed += await DeleteExpiredDemosAsync(db, nowUtc);
        removed += await DeleteAbandonedSignupsAsync(db, nowUtc);
        return removed;
    }

    /// <summary>24 soatdan eski demo hisoblar — barcha ma'lumotlari bilan (bazadagi cascade).</summary>
    public static Task<int> DeleteExpiredDemosAsync(AppDbContext db, DateTime nowUtc)
    {
        var cutoff = nowUtc - DemoAccount.Lifetime;
        const string suffix = "@" + DemoAccount.Domain;
        return db.Users.Where(u => u.Email.EndsWith(suffix) && u.CreatedAtUtc < cutoff).ExecuteDeleteAsync();
    }

    /// <summary>
    /// Parolsiz (email kodi) oqimda ochilib, hech qachon ishlatilmagan hisoblar —
    /// 7 kundan keyin o'chiriladi (email kodlari cascade bilan). Bitta SQL so'rov:
    /// tanlash va o'chirish orasida hisob ishlatila boshlasa ham, o'chirilmaydi.
    /// </summary>
    public static Task<int> DeleteAbandonedSignupsAsync(AppDbContext db, DateTime nowUtc) =>
        AbandonedSignups(db.Users, db.Sessions, db.Activities, db.ReviewCards, db.ReviewLogs, db.AiUsages,
            db.TelegramAccounts, db.TelegramLinkTokens, db.MockTests, db.Groups, db.GroupMembers, nowUtc)
        .ExecuteDeleteAsync();

    /// <summary>
    /// "Tashlab ketilgan ro'yxatdan o'tish" tanlovi (toza — testda ro'yxatlar bilan tekshiriladi).
    /// Ehtiyotkor: Users'ga bog'langan HAR BIR jadval tekshiriladi (EmailCodes'dan
    /// tashqari — ular aynan shu oqimda yaratiladi). Shartlar:
    /// email tasdiqlanmagan (demak Google ham emas — Google kirishi tasdiqlaydi),
    /// parol yo'q, 7 kundan eski, demo emas va hech qanday bog'liq yozuv yo'q
    /// (sessiya, faoliyat, karta, takrorlash, AI hisobi, Telegram, test, guruh).
    /// Yangi jadval Users'ga bog'lansa — shu yerga ham qo'shilishi kerak.
    /// </summary>
    /// <summary>Telegram orqali ochilgan hisoblarning sun'iy emaili (ular baribir tasdiqlangan — qo'shimcha ehtiyot).</summary>
    public const string TelegramSyntheticSuffix = "@telegram.invalid";

    public static IQueryable<User> AbandonedSignups(
        IQueryable<User> users,
        IQueryable<Session> sessions,
        IQueryable<Activity> activities,
        IQueryable<ReviewCard> cards,
        IQueryable<ReviewLog> reviewLogs,
        IQueryable<AiUsage> aiUsages,
        IQueryable<TelegramAccount> telegram,
        IQueryable<TelegramLinkToken> telegramTokens,
        IQueryable<MockTest> mockTests,
        IQueryable<Group> groups,
        IQueryable<GroupMember> members,
        DateTime nowUtc)
    {
        var cutoff = nowUtc - AbandonedSignupKeep;
        const string demoSuffix = "@" + DemoAccount.Domain;
        return users.Where(u =>
            u.EmailVerifiedAtUtc == null
            && u.PasswordHash == ""
            && u.CreatedAtUtc < cutoff
            && !u.Email.EndsWith(demoSuffix)
            && !u.Email.EndsWith(TelegramSyntheticSuffix)
            && u.OnboardedAtUtc == null
            && u.DisplayName == null
            && !sessions.Any(s => s.UserId == u.Id)
            && !activities.Any(a => a.UserId == u.Id)
            && !cards.Any(c => c.UserId == u.Id)
            && !reviewLogs.Any(l => l.UserId == u.Id)
            && !aiUsages.Any(x => x.UserId == u.Id)
            && !telegram.Any(t => t.UserId == u.Id)
            && !telegramTokens.Any(t => t.UserId == u.Id)
            && !mockTests.Any(t => t.CreatedByUserId == u.Id)
            && !groups.Any(g => g.TeacherId == u.Id)
            && !members.Any(m => m.UserId == u.Id));
    }
}

/// <summary>
/// Server ichidagi tozalash: ishga tushgandan 5 daqiqa o'tib, keyin har 6 soatda.
/// Render bepul tarifida server uxlab qolsa — uyg'onganda davom etadi.
/// </summary>
public class MaintenanceService(IServiceScopeFactory scopes, ILogger<MaintenanceService> logger) : BackgroundService
{
    public static readonly TimeSpan FirstDelay = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan Interval = TimeSpan.FromHours(6);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(FirstDelay, stoppingToken);
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = scopes.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var removed = await Maintenance.CleanupAsync(db, DateTime.UtcNow);
                    if (removed > 0) logger.LogInformation("Tozalash: {Count} ta eski yozuv o'chirildi", removed);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogWarning(ex, "Tozalash bajarilmadi (keyingi safar qayta uriniladi)");
                }
                await Task.Delay(Interval, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Server to'xtayapti.
        }
    }
}
