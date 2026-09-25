namespace SpeakingCoach.Api.Data;

/// <summary>Karta qayerdan paydo bo'lgani — frontend savolni shunga qarab yozadi.</summary>
public enum ReviewCardKind
{
    /// <summary>Speaking/Writing tuzatishi: old tomonda xato ibora, orqada to'g'risi.</summary>
    Correction = 0,

    /// <summary>Reading/Listening'da xato javob berilgan savol.</summary>
    Question = 1,

    /// <summary>Foydalanuvchi o'zi qo'shgan so'z yoki ibora.</summary>
    Manual = 2,

    /// <summary>
    /// Lug'at so'zi: matnda bosilgan yoki Lug'at bo'limida qidirilgan so'z,
    /// ma'nosi AI tomonidan izohlangan. Bazada matn ("Word") sifatida
    /// saqlanadi — yangi qiymat migratsiya talab qilmaydi.
    /// </summary>
    Word = 3,
}

/// <summary>
/// Takrorlash kartasi (flashcard). Oraliqli takrorlash (spaced repetition)
/// g'oyasi: kartani "unutish arafasida" qayta ko'rsatish. Har safar to'g'ri
/// eslansa, keyingi takrorlashgacha bo'lgan oraliq uzayadi (1 kun → 3 kun →
/// ~1 hafta → ...), unutilsa — qisqaradi. Hisob-kitob ReviewScheduler'da.
/// </summary>
public class ReviewCard
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public ReviewCardKind Kind { get; set; }

    /// <summary>Qaysi mashqdan kelgan (qo'lda qo'shilgan bo'lsa — null).</summary>
    public ActivityType? Source { get; set; }

    public string Front { get; set; } = "";
    public string Back { get; set; } = "";
    public string Note { get; set; } = "";

    public DateTime CreatedAtUtc { get; set; }

    /// <summary>Qachon qayta ko'rsatilishi kerak. Yangi karta — darhol.</summary>
    public DateTime DueAtUtc { get; set; }

    public double IntervalDays { get; set; }

    /// <summary>"Osonlik" koeffitsiyenti (SM-2): oraliq har safar shunga ko'paytiriladi.</summary>
    public double Ease { get; set; } = ReviewDefaults.StartingEase;

    /// <summary>Ketma-ket muvaffaqiyatli eslashlar soni (unutilsa 0 ga tushadi).</summary>
    public int Repetitions { get; set; }

    /// <summary>Necha marta unutilgan — "qiyin kartalar"ni aniqlash uchun.</summary>
    public int Lapses { get; set; }

    public DateTime? LastReviewedAtUtc { get; set; }
}

/// <summary>
/// Har bir takrorlash — alohida yozuv. Kartaning o'zida faqat JORIY holat
/// bor; kunlik maqsad, ketma-ket kunlar (streak) va kelajakdagi statistika
/// uchun esa tarix kerak.
/// </summary>
public class ReviewLog
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid CardId { get; set; }
    public int Grade { get; set; }
    public DateTime ReviewedAtUtc { get; set; }
}

public static class ReviewDefaults
{
    public const double StartingEase = 2.5;
}
