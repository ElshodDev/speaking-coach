namespace SpeakingCoach.Application.Mock;

/// <summary>
/// Mock imtihon uchun sutkalik cheklov: to'liq imtihon Gemini'ning katta
/// so'rovi (10 daqiqagacha audio), shuning uchun alohida, kichik limit.
/// Oyna — oxirgi 24 soat (kun chegarasi va vaqt zonasidan mustaqil).
/// </summary>
public static class MockLimit
{
    /// <summary>Sutkasiga nechta Speaking/Writing mock (AI baholaydi). To'liq imtihonda ikkalasi bor — 4 ta = 2 ta to'liq imtihon.</summary>
    public static int PerDay(IConfiguration config) =>
        int.TryParse(config["Mock:PerDay"], out var n) && n > 0 ? n : 4;

    /// <summary>Listening/Reading javoblari server tomonida tekshiriladi (AI'siz) — limitga kirmaydi.</summary>
    public static bool IsAiAssessed(string module) => module is "speaking" or "writing";

    /// <summary>Test bankida yangi test qolmaganda — AI bilan yaratish (sutkasiga).</summary>
    public static int GeneratePerDay(IConfiguration config) =>
        int.TryParse(config["Mock:GeneratePerDay"], out var n) && n > 0 ? n : 2;

    /// <summary>Sessiya identifikatori: faqat GUID ko'rinishidagi qator (aks holda e'tiborsiz).</summary>
    public static string? CleanSession(string? s) => Guid.TryParse(s, out var g) ? g.ToString("N") : null;

    /// <summary>Bajarilayotgan (hali saqlanmagan) Speaking/Writing mock'lar hisoblagichi kaliti.</summary>
    public static string InFlightKey(Guid userId) => "mock:" + userId.ToString("N");

    public static (bool Allowed, DateTime? RetryAtUtc) Check(IEnumerable<DateTime> startsUtc, int perDay, DateTime nowUtc)
    {
        var window = startsUtc.Where(t => t > nowUtc.AddHours(-24)).OrderBy(t => t).ToList();
        if (window.Count < perDay) return (true, null);
        // Eng eski yozuv 24 soatdan chiqqanda bitta joy bo'shaydi.
        return (false, window[window.Count - perDay].AddHours(24));
    }
}
