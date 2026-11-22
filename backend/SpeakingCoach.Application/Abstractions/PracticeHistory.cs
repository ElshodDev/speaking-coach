namespace SpeakingCoach.Application.Abstractions;

/// <summary>Ravonlik grafigi uchun bitta urinish: vaqt, tur va natija JSON'i (o'lchovlar ichida).</summary>
public record FluencyRow(DateTime CreatedAtUtc, ActivityType Type, string ResponseData);

/// <summary>Foydalanuvchi bajargan mashqlar tarixi bo'yicha provayderga xos so'rovlar (amalga oshirish — Infrastructure'da).</summary>
public interface IPracticeHistory
{
    /// <summary>
    /// Bajargan mashqlar sarlavhalari (eng yangisi birinchi) — tayyor mashqlardan hali ishlanmaganini tanlash uchun.
    /// Butun matn (passage, savollar) bazadan tortilmaydi: faqat sarlavha.
    /// </summary>
    Task<List<string>> DoneTitlesAsync(Guid userId, ActivityType type);

    /// <summary>IELTS Part 2 urinishlarining PromptData'si (eng yangisi birinchi, ko'pi bilan 300 ta) — qaysi kartochkalar bajarilganini bilish uchun.</summary>
    Task<List<string>> Part2PromptsAsync(Guid userId);

    /// <summary>Ravonlik o'lchovi ("metrics") bor oxirgi urinishlar: Speaking, Mock va Suhbat (eng yangisi birinchi).</summary>
    Task<List<FluencyRow>> FluencyRowsAsync(Guid userId, int take);
}
