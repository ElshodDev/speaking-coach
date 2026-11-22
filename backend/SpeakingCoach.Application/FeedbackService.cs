using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace SpeakingCoach.Application;

/// <summary>Fikr-mulohaza qoidalari — sof funksiyalar (testlanadi).</summary>
public static class FeedbackRules
{
    public static readonly string[] Kinds = { "bug", "idea", "content", "ai", "other" };
    public const int MinMessage = 3;
    public const int MaxMessage = 2000;
    public const int MaxPage = 200;

    /// <summary>Xato kaliti (Texts) yoki null — hammasi joyida.</summary>
    public static string? Validate(string? kind, string? message)
    {
        if (kind is null || !Kinds.Contains(kind.Trim().ToLowerInvariant())) return "feedback.bad_kind";
        var m = (message ?? "").Trim();
        if (m.Length < MinMessage || m.Length > MaxMessage) return "feedback.bad_message";
        return null;
    }

    public static string NormalizeKind(string kind) => kind.Trim().ToLowerInvariant();

    /// <summary>Sahifa manzili — ixtiyoriy, 200 belgigacha qisqartiriladi.</summary>
    public static string? NormalizePage(string? page) => TelegramLoginRules.Clip(page, MaxPage);

    /// <summary>Adminga Telegram xabari uchun qisqa ko'rinish (xabar 500 belgigacha).</summary>
    public static string Preview(string message, int max = 500) =>
        message.Length > max ? message[..max] + "…" : message;
}

/// <summary>
/// Adminlarga fikr haqida Telegram xabarlari uchun server darajasidagi chegara:
/// soatiga <see cref="MaxPerHour"/> ta. Fikrlarning o'zi bazaga baribir yoziladi
/// (admin paneli ko'rsatadi) — faqat bildirishnomalar oqimi to'xtaydi.
/// </summary>
public sealed class FeedbackNotifyThrottle
{
    public const int MaxPerHour = 30;
    private static readonly FeedbackNotifyThrottle Shared = new();
    private readonly Queue<DateTime> _sent = new();
    private readonly object _lock = new();

    public static bool TryTake(DateTime nowUtc) => Shared.Take(nowUtc);

    public bool Take(DateTime nowUtc)
    {
        lock (_lock)
        {
            while (_sent.Count > 0 && nowUtc - _sent.Peek() >= TimeSpan.FromHours(1)) _sent.Dequeue();
            if (_sent.Count >= MaxPerHour) return false;
            _sent.Enqueue(nowUtc);
            return true;
        }
    }
}

/// <summary>
/// Bir bosishli baholar (👍/👎, "AI bahosi adolatlimi?", "qayerdan topdingiz?"). Ular mavjud Feedbacks jadvaliga
/// Kind = "rating" bilan yoziladi (sxema o'zgarmaydi), kalit esa qat'iy ro'yxatdan (allow-list): erkin matn yo'q,
/// shuning uchun admin xulosasi oddiy guruhlash bilan hisoblanadi. Matnli izoh — mavjud /api/feedback orqali.
/// </summary>
public static partial class RatingRules
{
    public const string Kind = FeedbackKinds.Rating;
    public const int MaxKey = 64;

    [GeneratedRegex(@"^(first_practice:(up|down)|ai_fair:(speaking|writing|part2|mock):(fair|high|low)|lesson:(up|down)|source:(instagram|telegram|friend|teacher|search|other))\z", RegexOptions.CultureInvariant)]
    private static partial Regex Allowed();

    public static bool IsValid(string? key) => key is not null && key.Length <= MaxKey && Allowed().IsMatch(key);

    /// <summary>
    /// Bir foydalanuvchi bir marta bera oladigan baholar uchun prefiks ("first_practice:", "source:"); aks holda null
    /// (masalan, AI bahosini har natija uchun alohida berish mumkin).
    /// </summary>
    public static string? OncePrefix(string key) =>
        key.StartsWith("first_practice:", StringComparison.Ordinal) ? "first_practice:"
        : key.StartsWith("source:", StringComparison.Ordinal) ? "source:"
        : null;
}

public record RatingCount(string Key, int Count);
public record AdminFeedbackRow(Guid Id, string Kind, string Message, string? Page, string Lang, DateTime CreatedAtUtc, string? UserEmail);

/// <summary>
/// Fikr-mulohazalar va bir bosishli baholar: saqlash, adminlarga Telegram xabari va admin xulosalari.
/// HTTP (so'rovdan til, IP, foydalanuvchi) — Api'da; bu yerda faqat qoidalar va saqlash.
/// </summary>
public class FeedbackService(IAppDbContext db, AdminOptions admins, TelegramOptions telegram, ITelegramApi api, ILogger<FeedbackService> logger)
{
    /// <summary>Bir bosishli baho (mehmon ham baholay oladi). Bir marta beriladigan turlar takrorlansa — true (saqlanmaydi).</summary>
    public async Task<bool> SaveRatingAsync(Guid? userId, string key, string? page, string lang, string? ip)
    {
        var once = RatingRules.OncePrefix(key);
        if (once is not null && userId is not null &&
            await db.Feedbacks.AnyAsync(f => f.UserId == userId && f.Kind == RatingRules.Kind && f.Message.StartsWith(once)))
            return true;

        db.Feedbacks.Add(new Feedback
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Kind = RatingRules.Kind,
            Message = key,
            Page = FeedbackRules.NormalizePage(page),
            Lang = lang,
            CreatedAtUtc = DateTime.UtcNow,
            Ip = TelegramLoginRules.Clip(ip, 64),
        });
        await db.SaveChangesAsync();
        return false;
    }

    /// <summary>Baholar xulosasi (kalit → soni), oxirgi N kun.</summary>
    public async Task<(int Days, List<RatingCount> Items)> RatingSummaryAsync(int days)
    {
        days = Math.Clamp(days, 1, 365);
        var since = DateTime.UtcNow.AddDays(-days);
        var rows = await db.Feedbacks
            .Where(f => f.Kind == RatingRules.Kind && f.CreatedAtUtc >= since)
            .GroupBy(f => f.Message)
            .Select(g => new { key = g.Key, count = g.Count() })
            .ToListAsync();
        return (days, rows.OrderBy(r => r.key, StringComparer.Ordinal).Select(r => new RatingCount(r.key, r.count)).ToList());
    }

    /// <summary>Matnli fikrni saqlaydi (qiymatlar oldindan <see cref="FeedbackRules.Validate"/> bilan tekshirilgan).</summary>
    public async Task<Feedback> SaveFeedbackAsync(User? user, string kind, string message, string? page, string lang, string? ip)
    {
        var feedback = new Feedback
        {
            Id = Guid.NewGuid(),
            UserId = user?.Id,
            Kind = FeedbackRules.NormalizeKind(kind),
            Message = message.Trim(),
            Page = FeedbackRules.NormalizePage(page),
            Lang = lang,
            CreatedAtUtc = DateTime.UtcNow,
            Ip = TelegramLoginRules.Clip(ip, 64),
        };
        db.Feedbacks.Add(feedback);
        await db.SaveChangesAsync();
        return feedback;
    }

    /// <summary>
    /// Adminlarga Telegram'da qisqa xabar: Telegram:Admins ID'lari va Admin:Emails hisoblari ulangan chatlar. Chatlar ro'yxati hozir
    /// (so'rov ichida) olinadi, yuborish esa javobni kutdirmaydi (fire-and-forget, xatolar faqat logga). Soatiga <see cref="FeedbackNotifyThrottle.MaxPerHour"/> tagacha.
    /// </summary>
    public async Task NotifyAdminsAsync(Feedback feedback, User? user)
    {
        if (!telegram.Enabled || !FeedbackNotifyThrottle.TryTake(DateTime.UtcNow)) return;
        try
        {
            var adminEmails = admins.Emails.ToList();
            var adminIds = adminEmails.Count == 0
                ? new List<Guid>()
                : await db.Users.Where(u => adminEmails.Contains(u.Email)).Select(u => u.Id).ToListAsync();
            var chats = adminIds.Count == 0
                ? []
                : await db.TelegramAccounts
                    .Where(a => adminIds.Contains(a.UserId))
                    .Select(a => new { a.ChatId, a.Lang })
                    .ToListAsync();
            var targets = chats.Select(c => (c.ChatId, c.Lang))
                .Concat(telegram.AdminIds.Select(id => (ChatId: id, Lang: Texts.DefaultLang)))
                .DistinctBy(t => t.ChatId)
                .ToList();
            var tgName = user is null ? null : await db.TelegramAccounts.Where(a => a.UserId == user.Id).Select(a => a.Username).FirstOrDefaultAsync();
            var from = user is null ? "guest" : AdminService.Contact(user.Email, tgName, user.DisplayName);
            var tail = $"{from} · {feedback.Page ?? "—"} · {feedback.Lang}";
            _ = Task.Run(async () =>
            {
                foreach (var (chatId, lang) in targets)
                {
                    try
                    {
                        await api.SendMessageAsync(chatId, Texts.Get(lang, "feedback.admin_notify",
                            BotLogic.Html(feedback.Kind), BotLogic.Html(FeedbackRules.Preview(feedback.Message)), BotLogic.Html(tail)));
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Fikr haqida adminga yozib bo'lmadi: chat {Chat}", chatId);
                    }
                }
            });
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Fikr haqida adminlarni aniqlab bo'lmadi");
        }
    }

    /// <summary>Admin: oxirgi fikrlar (eng yangisi birinchi). Bir bosishli baholar alohida xulosada, bu ro'yxatni to'ldirmaydi.</summary>
    public async Task<List<AdminFeedbackRow>> RecentFeedbackAsync(int take)
    {
        take = Math.Clamp(take, 1, 200);
        var rows = await db.Feedbacks
            .Where(f => f.Kind != RatingRules.Kind)
            .OrderByDescending(f => f.CreatedAtUtc)
            .Take(take)
            .ToListAsync();
        var userIds = rows.Where(f => f.UserId is not null).Select(f => f.UserId!.Value).Distinct().ToList();
        // Admin fikr yozgan odamni to'liq ko'radi: email yoki Telegram (@username / ism / ID).
        var tgNames = userIds.Count == 0
            ? new Dictionary<Guid, string?>()
            : (await db.TelegramAccounts.Where(a => userIds.Contains(a.UserId)).Select(a => new { a.UserId, a.Username }).ToListAsync())
                .ToDictionary(a => a.UserId, a => a.Username);
        var emails = userIds.Count == 0
            ? new Dictionary<Guid, string>()
            : (await db.Users.Where(u => userIds.Contains(u.Id)).Select(u => new { u.Id, u.Email, u.DisplayName }).ToListAsync())
                .ToDictionary(u => u.Id, u => AdminService.Contact(u.Email, tgNames.GetValueOrDefault(u.Id), u.DisplayName));
        return rows.Select(f => new AdminFeedbackRow(f.Id, f.Kind, f.Message, f.Page, f.Lang, f.CreatedAtUtc,
            f.UserId is Guid id && emails.TryGetValue(id, out var e) ? e : null)).ToList();
    }
}
