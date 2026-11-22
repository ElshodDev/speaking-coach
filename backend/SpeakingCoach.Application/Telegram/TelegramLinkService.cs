using Microsoft.EntityFrameworkCore;

namespace SpeakingCoach.Application.Telegram;

public record TelegramStatus(bool Enabled, string BotUsername, bool Linked, string? Username, int? ReminderHour);

/// <summary>Bir martalik ulash havolasi natijasi: Url — tayyor; Disabled — bot o'chiq; DemoBlocked — demo hisob ulay olmaydi.</summary>
public record TelegramLinkResult(string? Url, bool Disabled, bool DemoBlocked);

/// <summary>
/// Saytdan (Profil → Telegram) botni ulash: holat, bir martalik havola, eslatma soati, uzish.
/// Webhook va cron (Telegram → server) — Api'dagi TelegramEndpoints'da; botning o'zi — TelegramBot.
/// </summary>
public class TelegramLinkService(IAppDbContext db, TelegramOptions options)
{
    public async Task<TelegramStatus> StatusAsync(Guid userId)
    {
        var account = await db.TelegramAccounts.FirstOrDefaultAsync(a => a.UserId == userId);
        return new TelegramStatus(options.Enabled, options.BotUsername, account is not null, account?.Username, account?.ReminderHour);
    }

    /// <summary>Bir martalik havola: t.me/Bot?start=TOKEN (BotLogic.LinkTokenLifetime amal qiladi). Foydalanuvchining eski va muddati o'tgan tokenlari o'chiriladi.</summary>
    public async Task<TelegramLinkResult> CreateLinkAsync(User user, int tzOffsetMinutes, string? lang, string fallbackLang)
    {
        if (!options.Enabled) return new TelegramLinkResult(null, true, false);
        if (DemoAccount.IsDemo(user.Email)) return new TelegramLinkResult(null, false, true);

        var now = DateTime.UtcNow;
        db.TelegramLinkTokens.RemoveRange(await db.TelegramLinkTokens
            .Where(t => t.UserId == user.Id || t.ExpiresAtUtc < now).ToListAsync());
        var token = BotLogic.NewLinkToken();
        db.TelegramLinkTokens.Add(new TelegramLinkToken
        {
            TokenHash = BotLogic.HashToken(token),
            UserId = user.Id,
            Lang = Texts.Langs.Contains(lang) ? lang! : fallbackLang,
            TzOffsetMinutes = Math.Clamp(tzOffsetMinutes, -14 * 60, 14 * 60),
            ExpiresAtUtc = now.Add(BotLogic.LinkTokenLifetime),
        });
        await db.SaveChangesAsync();
        return new TelegramLinkResult($"https://t.me/{options.BotUsername}?start={token}", false, false);
    }

    /// <summary>Eslatma soati (0–23) yoki o'chirish (null). Ulangan hisob bo'lmasa — (false, null).</summary>
    public async Task<(bool Found, int? ReminderHour)> SetReminderHourAsync(Guid userId, int? hour)
    {
        var account = await db.TelegramAccounts.FirstOrDefaultAsync(a => a.UserId == userId);
        if (account is null) return (false, null);
        account.ReminderHour = hour is int h ? Math.Clamp(h, 0, 23) : null;
        await db.SaveChangesAsync();
        return (true, account.ReminderHour);
    }

    public async Task UnlinkAsync(Guid userId)
    {
        db.TelegramAccounts.RemoveRange(await db.TelegramAccounts.Where(a => a.UserId == userId).ToListAsync());
        await db.SaveChangesAsync();
    }
}
