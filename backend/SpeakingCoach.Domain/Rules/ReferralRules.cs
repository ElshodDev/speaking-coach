namespace SpeakingCoach.Domain.Rules;

/// <summary>
/// Taklif (referral) qoidalari: kod, mukofot muddati va miqdori. Toza mantiq — bazasiz test qilinadi.
/// Kod — foydalanuvchi ID'sining (Guid) base64url ko'rinishi (22 belgi): alohida jadval kerak emas,
/// qidirish birlamchi kalit bo'yicha. Kod ID'ni oshkor qiladi, lekin ID tasodifiy va email/maxfiy
/// ma'lumotga olib kelmaydi.
/// </summary>
public static class ReferralRules
{
    /// <summary>Mukofot necha kun amal qiladi (yangi hisob ochilgan paytdan).</summary>
    public const int BonusDays = 7;

    /// <summary>Mukofot: kuniga qo'shimcha AI mashq (baholash) urinishlari. Bir necha taklif qo'shilmaydi.</summary>
    public const int BonusExtraExercises = 5;

    public static string Encode(Guid userId) =>
        Convert.ToBase64String(userId.ToByteArray()).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    /// <summary>Noto'g'ri yoki bo'sh kod — null (xato emas: ro'yxatdan o'tish baribir davom etadi).</summary>
    public static Guid? Decode(string? code)
    {
        var c = (code ?? "").Trim();
        if (c.Length != 22) return null;
        foreach (var ch in c)
        {
            if (!(char.IsAsciiLetterOrDigit(ch) || ch == '-' || ch == '_')) return null;
        }
        try
        {
            var bytes = Convert.FromBase64String(c.Replace('-', '+').Replace('_', '/') + "==");
            return bytes.Length == 16 ? new Guid(bytes) : null;
        }
        catch (FormatException)
        {
            return null;
        }
    }

    /// <summary>Taklif qilingan hisob uchun mukofot hali amal qiladimi (ochilganidan BonusDays kun ichida).</summary>
    public static bool BonusWindowOpen(DateTime createdAtUtc, DateTime nowUtc) =>
        nowUtc >= createdAtUtc && nowUtc < createdAtUtc.AddDays(BonusDays);

    /// <summary>Taklif qilgan odam uchun: eng yangi taklif qilingan do'sti shu oynada bo'lsa mukofot bor.</summary>
    public static DateTime RecentSignupCutoff(DateTime nowUtc) => nowUtc.AddDays(-BonusDays);

    /// <summary>
    /// Kunlik mashq limitiga qo'shiladigan urinishlar: taklif qilingan hisob o'z oynasida YOKI taklif qilgan odamning
    /// yaqinda tasdiqlangan do'sti bor — BonusExtraExercises (qo'shilmaydi); aks holda 0.
    /// </summary>
    public static int ExtraExercises(bool referred, DateTime createdAtUtc, bool hasRecentVerifiedReferral, DateTime nowUtc) =>
        (referred && BonusWindowOpen(createdAtUtc, nowUtc)) || hasRecentVerifiedReferral ? BonusExtraExercises : 0;
}
