namespace SpeakingCoach.Api.Data;

/// <summary>
/// Bir foydalanuvchining bir kundagi (UTC) sun'iy intellekt so'rovlari soni.
/// Gemini'ning bepul kvotasi hamma uchun umumiy — bitta odam uni tugatib
/// qo'ymasligi uchun har kimga kunlik limit. Kalit: (UserId, Day).
/// </summary>
public class AiUsage
{
    public Guid UserId { get; set; }
    public DateOnly Day { get; set; }

    /// <summary>Baholash va mashq yaratish (gapirish, yozish, o'qish, tinglash).</summary>
    public int Exercises { get; set; }

    /// <summary>So'z izohlari (arzon so'rov — alohida, kattaroq limit).</summary>
    public int Words { get; set; }
}
