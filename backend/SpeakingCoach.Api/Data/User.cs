namespace SpeakingCoach.Api.Data;

/// <summary>
/// Ro'yxatdan o'tgan foydalanuvchi. Parolning o'zi HECH QACHON saqlanmaydi —
/// faqat PasswordHash (ASP.NET Core'ning PasswordHasher'i: PBKDF2 + tuz
/// (salt) + ko'p marta takrorlash). Baza sizib chiqsa ham, xeshdan asl
/// parolni tiklash amalda juda qimmat.
/// </summary>
public class User
{
    public Guid Id { get; set; }

    /// <summary>Doim kichik harflarda va bo'sh joylarsiz saqlanadi (Ali@Mail.com == ali@mail.com).</summary>
    public string Email { get; set; } = "";

    public string PasswordHash { get; set; } = "";

    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Musobaqa jadvalida ko'rinadigan taxallus. Email hech qachon boshqalarga
    /// ko'rsatilmaydi — shuning uchun alohida maydon.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>Ingliz tili darajasi (A2, B1, B2, C1) — matnlar va baholash shunga moslashadi.</summary>
    public string Level { get; set; } = LearnerLevel.Default;

    /// <summary>
    /// Haftalik musobaqada qatnashish — ixtiyoriy (opt-in). Standart holatda
    /// yopiq: hech kim roziligisiz reytingda ko'rinmaydi.
    /// </summary>
    public bool ShowOnLeaderboard { get; set; }
}

/// <summary>
/// CEFR darajalari. Foydalanuvchi tanlagan daraja Gemini'ga yuboriladigan
/// barcha promptlarga qo'shiladi: yaratiladigan matn uzunligi va
/// murakkabligi hamda baholash qat'iyligi shunga moslashadi.
/// </summary>
public static class LearnerLevel
{
    public const string Default = "B1";
    public static readonly string[] All = { "A2", "B1", "B2", "C1" };

    /// <summary>Noma'lum yoki bo'sh qiymat — standart B1 (xato bermaymiz, chunki bu shunchaki sozlama).</summary>
    public static string Normalize(string? level)
    {
        var l = (level ?? "").Trim().ToUpperInvariant();
        return All.Contains(l) ? l : Default;
    }

    public static bool IsValid(string? level) => All.Contains((level ?? "").Trim().ToUpperInvariant());

    /// <summary>Prompt uchun inglizcha tavsif.</summary>
    public static string Describe(string level) => Normalize(level) switch
    {
        "A2" => "A2 (elementary)",
        "B1" => "B1 (intermediate)",
        "B2" => "B2 (upper-intermediate)",
        _ => "C1 (advanced)",
    };

    /// <summary>O'qish matni uzunligi (so'z) — darajaga qarab.</summary>
    public static (int Min, int Max) ReadingWords(string level) => Normalize(level) switch
    {
        "A2" => (120, 160),
        "B1" => (180, 250),
        "B2" => (250, 320),
        _ => (300, 380),
    };

    /// <summary>Tinglash matni uzunligi (so'z) — darajaga qarab.</summary>
    public static (int Min, int Max) ListeningWords(string level) => Normalize(level) switch
    {
        "A2" => (90, 120),
        "B1" => (120, 160),
        "B2" => (160, 200),
        _ => (180, 230),
    };
}

/// <summary>
/// Kirish sessiyasi. Foydalanuvchi kirganda tasodifiy token yaratiladi va
/// brauzerga beriladi; bazada esa tokenning O'ZI emas, SHA-256 XESHI
/// saqlanadi. Sabab xuddi parolnikidek: baza sizib chiqsa ham, undagi
/// xeshlar bilan hech kimning nomidan kirib bo'lmaydi.
/// </summary>
public class Session
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
}
