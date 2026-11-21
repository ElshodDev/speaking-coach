using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
namespace SpeakingCoach.Application;

public enum AiKind
{
    Exercise,
    Word,

    /// <summary>Shadowing: bitta gap talaffuzini tekshirish (qisqa audio, arzon so'rov).</summary>
    Shadowing,
}

public record QuotaCounter(int Used, int Limit)
{
    public int Left => Math.Max(0, Limit - Used);
}

public record QuotaStatus(QuotaCounter Exercises, QuotaCounter Words, bool Unlimited, bool Guest, QuotaCounter? Shadowing = null);

/// <summary>
/// Kunlik limitlar (sozlanadi): Ai:ExercisesPerDay, Ai:WordsPerDay,
/// Ai:GuestExercisesPerDay, Ai:GuestWordsPerDay. Mehmonga kamroq — hisob
/// ochishga qo'shimcha sabab. Qo'shimcha (xotirada): Ai:TalkTurnsPerDay —
/// AI suhbatdagi navbatlar, Ai:AuthoringPerDay — botda test tayyorlash urinishlari.
/// Kun — Toshkent vaqti bilan (TashkentTime).
/// </summary>
public class AiQuotaOptions
{
    public int Exercises { get; }
    public int Words { get; }
    public int GuestExercises { get; }
    public int GuestWords { get; }
    public int Shadowing { get; }
    public int GuestShadowing { get; }
    public int TalkTurns { get; }
    public int Authoring { get; }

    /// <summary>Barcha mehmonlar birgalikda kuniga (har bir tur uchun) — IP almashtirib umumiy Gemini byudjetini tugatib bo'lmasin.</summary>
    public int GuestTotalExercises { get; }
    public int GuestTotalWords { get; }
    public int GuestTotalShadowing { get; }

    public AiQuotaOptions(IConfiguration config)
    {
        int Read(string key, int fallback) => int.TryParse(config[key], out var v) && v >= 0 ? v : fallback;
        Exercises = Read("Ai:ExercisesPerDay", 30);
        Words = Read("Ai:WordsPerDay", 100);
        GuestExercises = Read("Ai:GuestExercisesPerDay", 5);
        GuestWords = Read("Ai:GuestWordsPerDay", 20);
        Shadowing = Read("Ai:ShadowingPerDay", 60);
        GuestShadowing = Read("Ai:GuestShadowingPerDay", 10);
        TalkTurns = Read("Ai:TalkTurnsPerDay", 40);
        Authoring = Read("Ai:AuthoringPerDay", 10);
        GuestTotalExercises = Read("Ai:GuestTotalExercisesPerDay", 300);
        GuestTotalWords = Read("Ai:GuestTotalWordsPerDay", 1000);
        GuestTotalShadowing = Read("Ai:GuestTotalShadowingPerDay", 500);
    }

    public int GuestTotalFor(AiKind kind) => kind switch
    {
        AiKind.Exercise => GuestTotalExercises,
        AiKind.Shadowing => GuestTotalShadowing,
        _ => GuestTotalWords,
    };

    public int LimitFor(AiKind kind, bool guest) => (kind, guest) switch
    {
        (AiKind.Exercise, false) => Exercises,
        (AiKind.Word, false) => Words,
        (AiKind.Exercise, true) => GuestExercises,
        (AiKind.Shadowing, false) => Shadowing,
        (AiKind.Shadowing, true) => GuestShadowing,
        _ => GuestWords,
    };
}

/// <summary>
/// Mehmonlar hisoblagichi — IP bo'yicha, xotirada, Toshkent kuni bo'yicha. Server qayta ishga
/// tushsa nolga tushadi — mehmon uchun bu yetarli (asosiy himoya —
/// daqiqalik rate limit), hisobi borlar uchun esa bazada saqlanadi.
/// </summary>
public class GuestQuotaStore
{
    private readonly ConcurrentDictionary<(string Ip, DateOnly Day, AiKind Kind), int> _counts = new();

    private readonly ConcurrentDictionary<(DateOnly Day, AiKind Kind), int> _totals = new();

    public int Get(string ip, DateOnly day, AiKind kind) => _counts.TryGetValue((ip, day, kind), out var n) ? n : 0;

    /// <summary>Bugun barcha mehmonlar birgalikda.</summary>
    public int Total(DateOnly day, AiKind kind) => _totals.TryGetValue((day, kind), out var n) ? n : 0;

    public void Add(string ip, DateOnly day, AiKind kind)
    {
        _counts.AddOrUpdate((ip, day, kind), 1, (_, n) => n + 1);
        _totals.AddOrUpdate((day, kind), 1, (_, n) => n + 1);
        foreach (var key in _totals.Keys.Where(k => k.Day < day)) _totals.TryRemove(key, out _);
        // Eski kunlarni tozalaymiz — lug'at cheksiz o'smasin.
        foreach (var key in _counts.Keys.Where(k => k.Day < day)) _counts.TryRemove(key, out _);
    }
}

/// <summary>
/// Bazasiz kunlik hisoblagichlar (sxema o'zgarmasin): AI suhbat navbatlari va
/// botda test tayyorlash. Bitta server — xotirada yetarli; qayta ishga tushsa
/// nolga tushadi (asosiy himoya — umumiy AI byudjeti va AiUsage).
/// TryTake — atomar: "joy bormi" va "yozish" bitta qadamda (parallel so'rovlar
/// limitni aylanib o'ta olmaydi); AI xato bersa — Refund.
/// </summary>
public class AiDailyCaps
{
    private readonly object _lock = new();
    private readonly Dictionary<(string Key, DateOnly Day), int> _counts = new();

    public int Used(string key, DateOnly day)
    {
        lock (_lock) return _counts.TryGetValue((key, day), out var n) ? n : 0;
    }

    public bool TryTake(string key, DateOnly day, int limit)
    {
        lock (_lock)
        {
            var n = _counts.TryGetValue((key, day), out var v) ? v : 0;
            if (n >= limit) return false;
            _counts[(key, day)] = n + 1;
            // Eski kunlar — lug'at cheksiz o'smasin.
            if (_counts.Count > 1000)
                foreach (var old in _counts.Keys.Where(k => k.Day < day).ToList()) _counts.Remove(old);
            return true;
        }
    }

    public void Refund(string key, DateOnly day)
    {
        lock (_lock)
        {
            if (_counts.TryGetValue((key, day), out var n) && n > 0) _counts[(key, day)] = n - 1;
        }
    }

    public static string TalkKey(Guid userId) => "talk:" + userId.ToString("N");
    public static string AuthoringKey(Guid userId) => "author:" + userId.ToString("N");
}

/// <summary>
/// Hozir bajarilayotgan (hali hisobga yozilmagan) AI so'rovlari — xotirada.
/// "Joy bormi" tekshiruvi Gemini javobidan OLDIN, hisobga yozish esa KEYIN
/// bo'ladi; oradagi bir necha soniyada parallel so'rovlar limitni aylanib
/// o'tmasligi uchun tekshiruv "ishlatilgan + bajarilayotgan" ni hisoblaydi va
/// joyni atomar band qiladi. Band qilingan joy so'rov tugaganda (muvaffaqiyatli
/// yoki xato) bo'shatiladi — muvaffaqiyatli bo'lsa, u allaqachon bazaga yozilgan.
/// </summary>
public class AiInFlight
{
    private readonly object _lock = new();
    private readonly Dictionary<(string Key, DateOnly Day, AiKind Kind), int> _counts = new();

    public int Count(string key, DateOnly day, AiKind kind)
    {
        lock (_lock) return _counts.TryGetValue((key, day, kind), out var n) ? n : 0;
    }

    /// <summary>Joy bo'lsa — band qiladi va bo'shatish uchun lease qaytaradi; aks holda null.</summary>
    public IDisposable? TryReserve(string key, DateOnly day, AiKind kind, int used, int limit)
    {
        lock (_lock)
        {
            var slot = (key, day, kind);
            var n = _counts.TryGetValue(slot, out var v) ? v : 0;
            if (used + n >= limit) return null;
            _counts[slot] = n + 1;
            return new Lease(this, slot);
        }
    }

    private void Release((string Key, DateOnly Day, AiKind Kind) slot)
    {
        lock (_lock)
        {
            if (!_counts.TryGetValue(slot, out var n)) return;
            if (n <= 1) _counts.Remove(slot);
            else _counts[slot] = n - 1;
        }
    }

    private sealed class Lease(AiInFlight owner, (string Key, DateOnly Day, AiKind Kind) slot) : IDisposable
    {
        private int _released;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _released, 1) == 0) owner.Release(slot);
        }
    }
}

/// <summary>
/// Tekshirish ikki qadamda: avval "joy bormi" (so'rovdan OLDIN), keyin
/// "hisobga yozish" (faqat MUVAFFAQIYATLI javobdan keyin). Gemini xato
/// qaytarsa, foydalanuvchining limiti kamaymaydi.
/// </summary>
public class AiQuotaService
{
    private readonly IAppDbContext _db;
    private readonly AdminOptions _admins;
    private readonly AiQuotaOptions _options;
    private readonly GuestQuotaStore _guests;
    private readonly AiInFlight _inFlight;

    public AiQuotaService(IAppDbContext db, AdminOptions admins, AiQuotaOptions options, GuestQuotaStore guests, AiInFlight inFlight)
    {
        _db = db;
        _admins = admins;
        _options = options;
        _guests = guests;
        _inFlight = inFlight;
    }

    /// <summary>Bajarilayotgan so'rovlar hisoblagichi kaliti: foydalanuvchi ID'si yoki mehmon kaliti (IP / "tg:CHAT").</summary>
    public static string PrincipalKey(User? user, string guestKey) => user is null ? "g:" + guestKey : "u:" + user.Id.ToString("N");

    /// <summary>Limit kuni — Toshkent vaqti bilan (UTC+5): yarim tunda yangilanadi.</summary>
    public static DateOnly Today(DateTime utcNow) => TashkentTime.Today(utcNow);

    // HTTP so'rovdan foydalanuvchi va IP'ni olish — Api qatlamida (AiQuotaHttpExtensions).

    // ---- Umumiy yadro (Telegram bot ham ishlatadi: mehmon kaliti — "tg:CHAT_ID") ----

    public async Task<QuotaStatus> GetStatusAsync(User? user, string guestKey)
    {
        var day = Today(DateTime.UtcNow);
        if (user is null)
        {
            return new QuotaStatus(
                new QuotaCounter(_guests.Get(guestKey, day, AiKind.Exercise), _options.GuestExercises),
                new QuotaCounter(_guests.Get(guestKey, day, AiKind.Word), _options.GuestWords),
                Unlimited: false,
                Guest: true,
                new QuotaCounter(_guests.Get(guestKey, day, AiKind.Shadowing), _options.GuestShadowing));
        }

        var usage = await _db.AiUsages.FirstOrDefaultAsync(u => u.UserId == user.Id && u.Day == day);
        // Demo hisob — mehmon limitlari bilan (bepul Gemini kvotasi haqiqiy foydalanuvchilarga qolsin).
        var demo = DemoAccount.IsDemo(user.Email);
        return new QuotaStatus(
            new QuotaCounter(usage?.Exercises ?? 0, demo ? _options.GuestExercises : _options.Exercises),
            new QuotaCounter(usage?.Words ?? 0, demo ? _options.GuestWords : _options.Words),
            Unlimited: _admins.IsAdmin(user),
            Guest: false,
            new QuotaCounter(usage?.Shadowing ?? 0, demo ? _options.GuestShadowing : _options.Shadowing));
    }

    /// <summary>null — ruxsat; aks holda xabar kaliti (Texts) va parametrlari.</summary>
    public Task<(string Key, object?[] Args)?> CheckAsync(User? user, string guestKey, AiKind kind) =>
        CheckCoreAsync(user, guestKey, kind, registerLease: null);

    /// <summary>
    /// Sayt so'rovi uchun: ruxsat berilsa, joy so'rov oxirigacha band qilinadi — band qilingan
    /// "lease" registerLease orqali so'rov tugaganda bo'shatiladigan qilib topshiriladi.
    /// </summary>
    public Task<(string Key, object?[] Args)?> CheckAndReserveAsync(User? user, string guestKey, AiKind kind, Action<IDisposable> registerLease) =>
        CheckCoreAsync(user, guestKey, kind, registerLease);

    private async Task<(string Key, object?[] Args)?> CheckCoreAsync(User? user, string guestKey, AiKind kind, Action<IDisposable>? registerLease)
    {
        var status = await GetStatusAsync(user, guestKey);
        if (status.Unlimited) return null;
        var counter = kind switch
        {
            AiKind.Exercise => status.Exercises,
            AiKind.Shadowing => status.Shadowing!,
            _ => status.Words,
        };
        var today = Today(DateTime.UtcNow);
        // Mehmonlarning umumiy kunlik byudjeti (IP almashtirib Gemini kvotasini tugatishga qarshi):
        // tugagan bo'lsa — mehmonga odatdagi "limit tugadi, hisob oching" xabari.
        var guestPoolEmpty = status.Guest && _guests.Total(today, kind) >= _options.GuestTotalFor(kind);
        if (!guestPoolEmpty)
        {
            if (registerLease is null)
            {
                if (counter.Left > 0) return null;
            }
            else if (_inFlight.TryReserve(PrincipalKey(user, guestKey), today, kind, counter.Used, counter.Limit) is { } lease)
            {
                // So'rov tugaganda (javob yuborilgach yoki xato bilan) joy bo'shaydi.
                registerLease(lease);
                return null;
            }
        }

        var key = (kind, status.Guest) switch
        {
            (AiKind.Exercise, true) => "ai.limit_guest",
            (AiKind.Exercise, false) => "ai.limit_exercises",
            (AiKind.Word, true) => "ai.limit_words_guest",
            (AiKind.Shadowing, true) => "ai.limit_shadowing_guest",
            (AiKind.Shadowing, false) => "ai.limit_shadowing",
            _ => "ai.limit_words",
        };
        return (key, new object?[] { counter.Limit, _options.LimitFor(kind, guest: false) });
    }

    public async Task RecordAsync(User? user, string guestKey, AiKind kind)
    {
        var day = Today(DateTime.UtcNow);
        if (user is null)
        {
            _guests.Add(guestKey, day, kind);
            return;
        }

        // Atomar oshirish (o'qib-oshirib-yozish emas): parallel so'rovlar bir-birining
        // hisobini yo'qotmaydi. Qator bo'lmasa — yaratamiz; shu paytda parallel so'rov
        // ham yaratgan bo'lsa — qayta oshiramiz.
        for (var attempt = 0; attempt < 3; attempt++)
        {
            if (await IncrementAsync(user.Id, day, kind) > 0) return;
            try
            {
                _db.AiUsages.Add(new AiUsage
                {
                    UserId = user.Id,
                    Day = day,
                    Exercises = kind == AiKind.Exercise ? 1 : 0,
                    Shadowing = kind == AiKind.Shadowing ? 1 : 0,
                    Words = kind == AiKind.Word ? 1 : 0,
                });
                await _db.SaveChangesAsync();
                return;
            }
            catch (DbUpdateException)
            {
                _db.ChangeTracker.Clear();
            }
        }
    }

    private Task<int> IncrementAsync(Guid userId, DateOnly day, AiKind kind)
    {
        var row = _db.AiUsages.Where(u => u.UserId == userId && u.Day == day);
        return kind switch
        {
            AiKind.Exercise => row.ExecuteUpdateAsync(x => x.SetProperty(u => u.Exercises, u => u.Exercises + 1)),
            AiKind.Shadowing => row.ExecuteUpdateAsync(x => x.SetProperty(u => u.Shadowing, u => u.Shadowing + 1)),
            _ => row.ExecuteUpdateAsync(x => x.SetProperty(u => u.Words, u => u.Words + 1)),
        };
    }
}
