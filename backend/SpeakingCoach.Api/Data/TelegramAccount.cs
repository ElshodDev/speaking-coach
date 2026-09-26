namespace SpeakingCoach.Api.Data;

/// <summary>
/// Telegram chati ilovadagi hisobga ulangan. Bitta hisob — bitta chat
/// (UserId unique), bitta chat — bitta hisob (ChatId — kalit).
/// </summary>
public class TelegramAccount
{
    /// <summary>Telegram chat ID (shaxsiy chatda = foydalanuvchi ID).</summary>
    public long ChatId { get; set; }

    public Guid UserId { get; set; }

    /// <summary>@username (bo'lmasligi mumkin) — Profil sahifasida ko'rsatish uchun.</summary>
    public string? Username { get; set; }

    /// <summary>Bot xabarlari tili: uz, ru, en.</summary>
    public string Lang { get; set; } = "uz";

    /// <summary>
    /// Kunlik eslatma soati (mahalliy vaqt, 0–23). null — eslatma o'chiq.
    /// Standart — 20:00 (kechqurun, kun yakunida "seriya uzilmasin").
    /// </summary>
    public int? ReminderHour { get; set; } = 20;

    /// <summary>JavaScript getTimezoneOffset() qiymati (Toshkent: −300). Ulashda brauzerdan olinadi.</summary>
    public int TzOffsetMinutes { get; set; } = -300;

    /// <summary>Oxirgi eslatma yuborilgan mahalliy kun — kuniga bittadan ortiq yubormaslik uchun.</summary>
    public DateOnly? LastReminderDate { get; set; }

    public DateTime LinkedAtUtc { get; set; }
}

/// <summary>
/// Saytdagi "Telegram'ni ulash" tugmasi yaratadigan bir martalik token.
/// Havola: t.me/Bot?start=TOKEN. Bazada faqat SHA-256 xeshi, 15 daqiqa amal qiladi.
/// </summary>
public class TelegramLinkToken
{
    public string TokenHash { get; set; } = "";
    public Guid UserId { get; set; }
    public string Lang { get; set; } = "uz";
    public int TzOffsetMinutes { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
}
