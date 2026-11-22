namespace SpeakingCoach.Application.Abstractions;

/// <summary>Foydalanuvchi bajargan mashqlar tarixi bo'yicha provayderga xos so'rovlar (amalga oshirish — Infrastructure'da).</summary>
public interface IPracticeHistory
{
    /// <summary>
    /// Bajargan mashqlar sarlavhalari (eng yangisi birinchi) — tayyor mashqlardan hali ishlanmaganini tanlash uchun.
    /// Butun matn (passage, savollar) bazadan tortilmaydi: faqat sarlavha.
    /// </summary>
    Task<List<string>> DoneTitlesAsync(Guid userId, ActivityType type);
}
