using System.Security.Cryptography;
using System.Text;
using SpeakingCoach.Api.Data;

namespace SpeakingCoach.Api.Services;

public enum CodeCheck
{
    Ok,
    NotFound,
    Wrong,
    Expired,
    TooManyAttempts,
}

/// <summary>
/// Email kodlari qoidalari — bazaga tegmaydigan sof funksiyalar (testlanadi).
/// </summary>
public static class EmailCodeRules
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(15);
    public const int MaxAttempts = 5;

    /// <summary>Ketma-ket ikki xat orasidagi eng kam vaqt (spam va pul sarfini oldini oladi).</summary>
    public static readonly TimeSpan Cooldown = TimeSpan.FromSeconds(60);

    /// <summary>Bir foydalanuvchiga bir soatda ko'pi bilan shuncha xat.</summary>
    public const int MaxPerHour = 5;

    /// <summary>000000–999999, kriptografik tasodifiy (Random() emas).</summary>
    public static string Generate() => RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

    /// <summary>Foydalanuvchi ID bilan "tuzlangan" xesh: bir xil kod turli foydalanuvchilarda turli xesh beradi.</summary>
    public static string Hash(Guid userId, string code) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{userId:N}:{code}")));

    /// <summary>Faqat raqamlar: "123 456" yoki "123-456" ham qabul qilinadi.</summary>
    public static string Normalize(string? input) => new((input ?? "").Where(char.IsAsciiDigit).ToArray());

    /// <summary>
    /// Solishtirishdan oldingi shartlar: kod bor, ishlatilmagan, muddati
    /// o'tmagan, urinish qolgan. null — solishtirish mumkin.
    /// </summary>
    public static CodeCheck? Precheck(EmailCode? latest, DateTime now)
    {
        if (latest is null || latest.UsedAtUtc is not null) return CodeCheck.NotFound;
        if (latest.ExpiresAtUtc <= now) return CodeCheck.Expired;
        if (latest.Attempts >= MaxAttempts) return CodeCheck.TooManyAttempts;
        return null;
    }

    /// <summary>
    /// Kod mos keladimi. Taqqoslash doimiy vaqtda (FixedTimeEquals) — javob
    /// vaqtidan kodni taxmin qilib bo'lmaydi.
    /// </summary>
    public static bool Matches(EmailCode code, Guid userId, string? input)
    {
        var normalized = Normalize(input);
        if (normalized.Length != 6) return false;
        return CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(code.CodeHash), Encoding.ASCII.GetBytes(Hash(userId, normalized)));
    }

    /// <summary>Eng oxirgi (ishlatilmagan) kodni to'liq tekshiradi (Precheck + Matches).</summary>
    public static CodeCheck Check(EmailCode? latest, Guid userId, string? input, DateTime now) =>
        Precheck(latest, now) ?? (Matches(latest!, userId, input) ? CodeCheck.Ok : CodeCheck.Wrong);

    /// <summary>
    /// Yangi xat yuborish mumkinmi (cooldown va soatlik chegara). Mumkin
    /// bo'lmasa — qancha kutish kerakligi.
    /// </summary>
    public static bool CanSend(IReadOnlyCollection<DateTime> recentSendsUtc, DateTime now, out TimeSpan wait)
    {
        wait = TimeSpan.Zero;
        if (recentSendsUtc.Count == 0) return true;

        var last = recentSendsUtc.Max();
        if (now - last < Cooldown)
        {
            wait = Cooldown - (now - last);
            return false;
        }

        var lastHour = recentSendsUtc.Where(t => t > now.AddHours(-1)).OrderBy(t => t).ToList();
        if (lastHour.Count >= MaxPerHour)
        {
            wait = lastHour[0].AddHours(1) - now;
            return false;
        }
        return true;
    }

    public static string KeyFor(CodeCheck result) => result switch
    {
        CodeCheck.Expired => "code.expired",
        CodeCheck.TooManyAttempts => "code.too_many",
        CodeCheck.NotFound => "code.not_found",
        _ => "code.wrong",
    };
}
