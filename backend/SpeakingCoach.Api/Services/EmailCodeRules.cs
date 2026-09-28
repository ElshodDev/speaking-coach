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

    /// <summary>
    /// Manzilga bog'langan kod (emailni almashtirish): kod faqat shu manzil
    /// bilan birga to'g'ri — boshqa emailni tasdiqlash uchun ishlatib bo'lmaydi.
    /// target null — oddiy <see cref="Hash(Guid, string)"/>.
    /// </summary>
    public static string Hash(Guid userId, string code, string? target) =>
        target is null ? Hash(userId, code)
        : Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{userId:N}:{target}:{code}")));

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
    public static bool Matches(EmailCode code, Guid userId, string? input, string? target = null)
    {
        var normalized = Normalize(input);
        if (normalized.Length != 6) return false;
        return CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(code.CodeHash), Encoding.ASCII.GetBytes(Hash(userId, normalized, target)));
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

/// <summary>
/// Butun server bo'yicha kunlik xatlar hisoblagichi (Brevo bepul tarifi —
/// kuniga 300 ta). Kun — Toshkent vaqti bilan (UTC+5). Server bitta nusxada
/// ishlaydi (Render free), shuning uchun xotiradagi hisoblagich yetarli;
/// qayta ishga tushsa noldan boshlanadi (bu kamdan-kam va zararsiz).
/// Sozlama: Email:DailyCap (standart 250).
/// </summary>
public sealed class EmailDailyCap
{
    public const int DefaultCap = 250;

    /// <summary>Jarayon bo'yicha yagona nusxa (DI'da ro'yxatdan o'tkazish shart emas).</summary>
    public static readonly EmailDailyCap Shared = new();

    private readonly object _lock = new();
    private DateOnly _day;
    private int _count;

    public static DateOnly TashkentDay(DateTime nowUtc) => DateOnly.FromDateTime(nowUtc.AddHours(5));

    /// <summary>Email:DailyCap → son (manfiy yoki noto'g'ri — standart 250).</summary>
    public static int CapFrom(IConfiguration config) =>
        int.TryParse(config["Email:DailyCap"], System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var cap) && cap >= 0
            ? cap
            : DefaultCap;

    private void Roll(DateTime nowUtc)
    {
        var day = TashkentDay(nowUtc);
        if (day != _day)
        {
            _day = day;
            _count = 0;
        }
    }

    /// <summary>Bugun yuborilgan xatlar soni.</summary>
    public int Count(DateTime nowUtc)
    {
        lock (_lock)
        {
            Roll(nowUtc);
            return _count;
        }
    }

    public bool IsReached(int cap, DateTime nowUtc)
    {
        lock (_lock)
        {
            Roll(nowUtc);
            return _count >= cap;
        }
    }

    /// <summary>Bitta xat uchun joy olish: limit tugagan bo'lsa — false.</summary>
    public bool TryTake(int cap, DateTime nowUtc)
    {
        lock (_lock)
        {
            Roll(nowUtc);
            if (_count >= cap) return false;
            _count++;
            return true;
        }
    }

    /// <summary>Xat yuborilmay qolsa — joyni qaytarish (o'sha kun ichida).</summary>
    public void Release(DateTime nowUtc)
    {
        lock (_lock)
        {
            Roll(nowUtc);
            if (_count > 0) _count--;
        }
    }
}
