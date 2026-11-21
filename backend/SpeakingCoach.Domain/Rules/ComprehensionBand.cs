namespace SpeakingCoach.Domain.Rules;

/// <summary>
/// O'qish/Tinglash mashqidan TAXMINIY IELTS band (boshlang'ich baho uchun). Ilovaning tayyor mashqlari darajali
/// (A2–C1): matn darajasini to'liq tushungan o'quvchi shu darajaga mos bandni ko'rsatadi, qolgani chiziqli.
/// Bu rasmiy moslik EMAS, qo'pol hisob — tayyorlik bahosida har doim "past ishonch" (keng oraliq) bilan ishlatiladi.
/// </summary>
public static class ComprehensionBand
{
    /// <summary>Hech narsa tushunilmagan holat uchun taxminiy pastki chegara.</summary>
    public const decimal Floor = 2.5m;

    /// <summary>Daraja → to'liq to'g'ri javobdagi band: A2 4.5, B1 5.5, B2 6.5, C1 7.5.</summary>
    public static decimal? Ceiling(string? level) => level switch
    {
        "A2" => 4.5m,
        "B1" => 5.5m,
        "B2" => 6.5m,
        "C1" => 7.5m,
        _ => null,
    };

    public static decimal? Estimate(string? level, int score, int total)
    {
        if (total <= 0 || score < 0 || score > total) return null;
        if (Ceiling(level) is not { } ceiling) return null;
        return IeltsBand.RoundHalfBand(Floor + (ceiling - Floor) * score / total);
    }
}
