namespace SpeakingCoach.Api.Data;

public enum EmailCodePurpose
{
    /// <summary>Ro'yxatdan o'tgandan keyin email haqiqatan foydalanuvchiniki ekanini tasdiqlash.</summary>
    Verify = 0,

    /// <summary>Parolni tiklash ("Parolni unutdim").</summary>
    ResetPassword = 1,
}

/// <summary>
/// Emailga yuborilgan 6 xonali bir martalik kod. Kodning o'zi saqlanmaydi —
/// faqat SHA-256 xeshi (xuddi sessiya tokenlari kabi): baza sizib chiqsa ham
/// kodlarni o'qib bo'lmaydi. Muddat (15 daqiqa) va urinishlar soni (5 ta)
/// cheklangan — 6 xonali kodni taxmin qilib topishning iloji qolmaydi.
/// </summary>
public class EmailCode
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public EmailCodePurpose Purpose { get; set; }
    public string CodeHash { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }

    /// <summary>Noto'g'ri kiritishlar soni.</summary>
    public int Attempts { get; set; }

    /// <summary>Kod ishlatilgan vaqt — ikkinchi marta ishlatib bo'lmaydi.</summary>
    public DateTime? UsedAtUtc { get; set; }
}
