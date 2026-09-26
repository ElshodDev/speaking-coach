namespace SpeakingCoach.Api.Data;

/// <summary>
/// Har bir "faoliyat turi" — hozircha faqat Speaking, lekin Writing/Reading/
/// Listening qo'shilganda shu ro'yxatga yangi qiymat qo'shiladi. Bitta
/// jadval (Activities) barcha turlarni saqlaydi — har biriga alohida
/// jadval ochish o'rniga (bu "god table" muammosini keltirib chiqaradi,
/// chunki ko'p nullable ustun kerak bo'lar edi).
/// </summary>
public enum ActivityType
{
    Speaking = 0,
    Writing = 1,
    Reading = 2,
    Listening = 3,

    /// <summary>To'liq mock imtihon (IELTS Speaking yoki Writing) — natija ResponseData'da.</summary>
    MockExam = 4,
}

/// <summary>
/// Bitta foydalanuvchi urinishini ifodalaydi (masalan bitta Speaking
/// yozuvi). PromptData va ResponseData — Postgres'ning jsonb ustunlari:
/// har bir faoliyat turi o'ziga xos shaklda JSON saqlaydi (masalan Speaking
/// uchun ResponseData = SpeakingEvaluationResult'ning JSON ko'rinishi),
/// shuning uchun bu ikki ustun uchun C# tomonida qat'iy tip yo'q — buning
/// o'rniga har bir endpoint o'zi JsonSerializer bilan serialize/deserialize
/// qiladi. Kelajakda Writing qo'shilganda, shu jadvalga Type=Writing bilan
/// yangi qator qo'shiladi, yangi jadval kerak emas.
/// </summary>
public class Activity
{
    public Guid Id { get; set; }
    public ActivityType Type { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Qaysi foydalanuvchiga tegishli. Nullable — login qo'shilishidan
    /// OLDIN saqlangan eski yozuvlar egasiz qoladi (ular hech kimning
    /// tarixida ko'rinmaydi, lekin o'chirilmaydi ham).
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>So'rov ma'lumotlari — masalan Speaking uchun {"topic": "..."}.</summary>
    public string PromptData { get; set; } = "{}";

    /// <summary>Gemini'dan kelgan baholash natijasi, JSON ko'rinishida.</summary>
    public string ResponseData { get; set; } = "{}";
}
