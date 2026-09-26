namespace SpeakingCoach.Api.Data;

/// <summary>
/// AI yaratgan Listening/Reading testi — "test banki". Bir marta yaratiladi
/// va boshqa foydalanuvchilarga ham beriladi (har kim hali ishlamaganini
/// oladi) — Gemini kvotasi tejaladi. To'g'ri javoblar faqat shu yerda
/// (Payload) saqlanadi; brauzerga javobsiz nusxa yuboriladi.
/// </summary>
public class MockTest
{
    public Guid Id { get; set; }

    /// <summary>"ielts" (keyinroq "cefr").</summary>
    public string Exam { get; set; } = "ielts";

    /// <summary>"listening" yoki "reading".</summary>
    public string Module { get; set; } = "";

    /// <summary>"academic" / "general"; Listening uchun bo'sh.</summary>
    public string Variant { get; set; } = "";

    /// <summary>To'liq test JSON ko'rinishida (to'g'ri javoblar bilan).</summary>
    public string Payload { get; set; } = "{}";

    /// <summary>Kim yaratishga sabab bo'lgan — yaratish limitini hisoblash uchun.</summary>
    public Guid? CreatedByUserId { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
