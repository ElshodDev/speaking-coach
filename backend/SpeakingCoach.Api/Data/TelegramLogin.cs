namespace SpeakingCoach.Api.Data;

/// <summary>
/// Saytga Telegram orqali kirish urinishi (5 daqiqa). Sayt nonce'li bot
/// havolasini va 2 xonali kodni ko'rsatadi; foydalanuvchi botda shu raqamni
/// bosadi; sayt esa poll token bilan natijani so'raydi. Nonce ham, poll token
/// ham bazada faqat SHA-256 xeshi sifatida saqlanadi.
/// </summary>
public class TelegramLogin
{
    public Guid Id { get; set; }

    /// <summary>Bot havolasidagi nonce'ning SHA-256 xeshi (hex, 64 belgi).</summary>
    public string NonceHash { get; set; } = "";

    /// <summary>Sayt natijani shu token bilan so'raydi — SHA-256 xeshi (hex, 64 belgi).</summary>
    public string PollTokenHash { get; set; } = "";

    /// <summary>Saytda ko'rsatiladigan raqam ("10"–"99") — botda aynan shu tugma bosilishi kerak.</summary>
    public string Code { get; set; } = "";

    public DateTime CreatedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }

    /// <summary>Havolani ochgan chat (birinchi ochgan chatga bog'lanadi).</summary>
    public long? ChatId { get; set; }

    public string? TgUsername { get; set; }
    public string? TgFirstName { get; set; }

    public DateTime? ConfirmedAtUtc { get; set; }
    public DateTime? RejectedAtUtc { get; set; }

    /// <summary>Sessiya berilgan vaqt — bitta urinish bilan faqat bir marta kiriladi.</summary>
    public DateTime? ConsumedAtUtc { get; set; }

    /// <summary>Tasdiqlangandan keyin — kiriladigan hisob.</summary>
    public Guid? UserId { get; set; }

    public string? Ip { get; set; }

    /// <summary>Sayt tili (uz, ru, en) — yangi hisob va bot xabari uchun.</summary>
    public string Lang { get; set; } = "uz";

    /// <summary>Hisob shu urinishda ochildimi (sayt "Xush kelibsiz"ni ko'rsatadi).</summary>
    public bool IsNewUser { get; set; }
}
