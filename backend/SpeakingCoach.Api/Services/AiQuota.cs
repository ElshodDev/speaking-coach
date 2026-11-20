using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using SpeakingCoach.Api.Data;

namespace SpeakingCoach.Api.Services;

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
    }

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

    public int Get(string ip, DateOnly day, AiKind kind) => _counts.TryGetValue((ip, day, kind), out var n) ? n : 0;

    public void Add(string ip, DateOnly day, AiKind kind)
    {
        _counts.AddOrUpdate((ip, day, kind), 1, (_, n) => n + 1);
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
    private readonly AppDbContext _db;
    private readonly AuthService _auth;
    private readonly AdminOptions _admins;
    private readonly AiQuotaOptions _options;
    private readonly GuestQuotaStore _guests;
    private readonly AiInFlight _inFlight;

    public AiQuotaService(AppDbContext db, AuthService auth, AdminOptions admins, AiQuotaOptions options, GuestQuotaStore guests, AiInFlight inFlight)
    {
        _db = db;
        _auth = auth;
        _admins = admins;
        _options = options;
        _guests = guests;
        _inFlight = inFlight;
    }

    /// <summary>Bajarilayotgan so'rovlar hisoblagichi kaliti: foydalanuvchi ID'si yoki mehmon kaliti (IP / "tg:CHAT").</summary>
    public static string PrincipalKey(User? user, string guestKey) => user is null ? "g:" + guestKey : "u:" + user.Id.ToString("N");

    /// <summary>Limit kuni — Toshkent vaqti bilan (UTC+5): yarim tunda yangilanadi.</summary>
    public static DateOnly Today(DateTime utcNow) => TashkentTime.Today(utcNow);

    private static string IpOf(HttpRequest request) => request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    // ---- Sayt (HTTP so'rov): foydalanuvchi tokendan, mehmon — IP bo'yicha ----

    public async Task<QuotaStatus> GetStatusAsync(HttpRequest request) =>
        await GetStatusAsync(await _auth.GetCurrentUserAsync(request), IpOf(request));

    /// <summary>
    /// Limit tugagan bo'lsa — 429 va tushunarli xabar; aks holda null (davom etaverish mumkin).
    /// Ruxsat berilsa, joy so'rov oxirigacha band qilinadi (parallel so'rovlar limitdan oshmaydi).
    /// </summary>
    public async Task<IResult?> CheckAsync(HttpRequest request, AiKind kind)
    {
        var denied = await CheckCoreAsync(await _auth.GetCurrentUserAsync(request), IpOf(request), kind, request.HttpContext);
        return denied is null
            ? null
            : Results.Json(request.Error(denied.Value.Key, denied.Value.Args), statusCode: StatusCodes.Status429TooManyRequests);
    }

    /// <summary>Muvaffaqiyatli AI javobidan keyin chaqiriladi.</summary>
    public async Task RecordAsync(HttpRequest request, AiKind kind) =>
        await RecordAsync(await _auth.GetCurrentUserAsync(request), IpOf(request), kind);

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
        CheckCoreAsync(user, guestKey, kind, http: null);

    private async Task<(string Key, object?[] Args)?> CheckCoreAsync(User? user, string guestKey, AiKind kind, HttpContext? http)
    {
        var status = await GetStatusAsync(user, guestKey);
        if (status.Unlimited) return null;
        var counter = kind switch
        {
            AiKind.Exercise => status.Exercises,
            AiKind.Shadowing => status.Shadowing!,
            _ => status.Words,
        };
        if (http is null)
        {
            if (counter.Left > 0) return null;
        }
        else if (_inFlight.TryReserve(PrincipalKey(user, guestKey), Today(DateTime.UtcNow), kind, counter.Used, counter.Limit) is { } lease)
        {
            // So'rov tugaganda (javob yuborilgach yoki xato bilan) joy bo'shaydi.
            http.Response.RegisterForDispose(lease);
            return null;
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
