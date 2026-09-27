using Microsoft.EntityFrameworkCore;
using SpeakingCoach.Api.Data;

namespace SpeakingCoach.Api.Services;

/// <summary>
/// Muddati o'tgan yozuvlarni tozalash (soatlik cron'da): baza cheksiz
/// o'smasin va eski sessiya/kodlar bekorga saqlanib yotmasin.
/// </summary>
public static class Maintenance
{
    /// <summary>Kodlar 2 kun saqlanadi — kunlik yuborish limiti ularni sanaydi.</summary>
    public static readonly TimeSpan EmailCodeKeep = TimeSpan.FromDays(2);

    public static async Task<int> CleanupAsync(AppDbContext db, DateTime nowUtc)
    {
        var codesBefore = nowUtc - EmailCodeKeep;
        var removed = 0;
        removed += await db.Sessions.Where(s => s.ExpiresAtUtc < nowUtc).ExecuteDeleteAsync();
        removed += await db.EmailCodes.Where(c => c.ExpiresAtUtc < nowUtc && c.CreatedAtUtc < codesBefore).ExecuteDeleteAsync();
        removed += await db.TelegramLinkTokens.Where(t => t.ExpiresAtUtc < nowUtc).ExecuteDeleteAsync();
        removed += await DeleteExpiredDemosAsync(db, nowUtc);
        return removed;
    }

    /// <summary>24 soatdan eski demo hisoblar — barcha ma'lumotlari bilan (bazadagi cascade).</summary>
    public static Task<int> DeleteExpiredDemosAsync(AppDbContext db, DateTime nowUtc)
    {
        var cutoff = nowUtc - DemoAccount.Lifetime;
        const string suffix = "@" + DemoAccount.Domain;
        return db.Users.Where(u => u.Email.EndsWith(suffix) && u.CreatedAtUtc < cutoff).ExecuteDeleteAsync();
    }
}
