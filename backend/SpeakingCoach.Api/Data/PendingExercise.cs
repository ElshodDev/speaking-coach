namespace SpeakingCoach.Api.Data;

/// <summary>
/// Yaratilgan, lekin hali javob berilmagan Reading/Listening mashqi.
///
/// Nega alohida jadval: Gemini matn, savollar VA to'g'ri javoblarni birga
/// yaratadi. To'g'ri javoblarni brauzerga yuborsak, ularni DevTools'da
/// ko'rish mumkin bo'lardi. Shuning uchun to'liq mashq (javoblar bilan)
/// shu yerda saqlanadi, brauzerga esa faqat matn va savollar ketadi.
/// Foydalanuvchi javob berganda server shu yozuvni o'qiydi, tekshiradi va
/// o'chiradi. Javob berilmay tashlab ketilgan mashqlar keyingi "generate"
/// so'rovida avtomatik tozalanadi (1 kundan eskilari).
/// </summary>
public class PendingExercise
{
    public Guid Id { get; set; }
    public ActivityType Type { get; set; }

    /// <summary>ComprehensionExercise'ning JSON ko'rinishi (to'g'ri javoblar bilan).</summary>
    public string Payload { get; set; } = "{}";

    public DateTime CreatedAtUtc { get; set; }
}
