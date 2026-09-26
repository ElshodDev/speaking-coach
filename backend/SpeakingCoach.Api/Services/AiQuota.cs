using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using SpeakingCoach.Api.Data;

namespace SpeakingCoach.Api.Services;

public enum AiKind
{
    Exercise,
    Word,
}

public record QuotaCounter(int Used, int Limit)
{
    public int Left => Math.Max(0, Limit - Used);
}

public record QuotaStatus(QuotaCounter Exercises, QuotaCounter Words, bool Unlimited, bool Guest);

/// <summary>
/// Kunlik limitlar (sozlanadi): Ai:ExercisesPerDay, Ai:WordsPerDay,
/// Ai:GuestExercisesPerDay, Ai:GuestWordsPerDay. Mehmonga kamroq — hisob
/// ochishga qo'shimcha sabab.
/// </summary>
public class AiQuotaOptions
{
    public int Exercises { get; }
    public int Words { get; }
    public int GuestExercises { get; }
    public int GuestWords { get; }

    public AiQuotaOptions(IConfiguration config)
    {
        int Read(string key, int fallback) => int.TryParse(config[key], out var v) && v >= 0 ? v : fallback;
        Exercises = Read("Ai:ExercisesPerDay", 30);
        Words = Read("Ai:WordsPerDay", 100);
        GuestExercises = Read("Ai:GuestExercisesPerDay", 5);
        GuestWords = Read("Ai:GuestWordsPerDay", 20);
    }

    public int LimitFor(AiKind kind, bool guest) => (kind, guest) switch
    {
        (AiKind.Exercise, false) => Exercises,
        (AiKind.Word, false) => Words,
        (AiKind.Exercise, true) => GuestExercises,
        _ => GuestWords,
    };
}

/// <summary>
/// Mehmonlar hisoblagichi — IP bo'yicha, xotirada. Server qayta ishga
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

    public AiQuotaService(AppDbContext db, AuthService auth, AdminOptions admins, AiQuotaOptions options, GuestQuotaStore guests)
    {
        _db = db;
        _auth = auth;
        _admins = admins;
        _options = options;
        _guests = guests;
    }

    public static DateOnly Today(DateTime utcNow) => DateOnly.FromDateTime(utcNow);

    private static string IpOf(HttpRequest request) => request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    // ---- Sayt (HTTP so'rov): foydalanuvchi tokendan, mehmon — IP bo'yicha ----

    public async Task<QuotaStatus> GetStatusAsync(HttpRequest request) =>
        await GetStatusAsync(await _auth.GetCurrentUserAsync(request), IpOf(request));

    /// <summary>Limit tugagan bo'lsa — 429 va tushunarli xabar; aks holda null (davom etaverish mumkin).</summary>
    public async Task<IResult?> CheckAsync(HttpRequest request, AiKind kind)
    {
        var denied = await CheckAsync(await _auth.GetCurrentUserAsync(request), IpOf(request), kind);
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
                Guest: true);
        }

        var usage = await _db.AiUsages.FirstOrDefaultAsync(u => u.UserId == user.Id && u.Day == day);
        return new QuotaStatus(
            new QuotaCounter(usage?.Exercises ?? 0, _options.Exercises),
            new QuotaCounter(usage?.Words ?? 0, _options.Words),
            Unlimited: _admins.IsAdmin(user),
            Guest: false);
    }

    /// <summary>null — ruxsat; aks holda xabar kaliti (Texts) va parametrlari.</summary>
    public async Task<(string Key, object?[] Args)?> CheckAsync(User? user, string guestKey, AiKind kind)
    {
        var status = await GetStatusAsync(user, guestKey);
        if (status.Unlimited) return null;
        var counter = kind == AiKind.Exercise ? status.Exercises : status.Words;
        if (counter.Left > 0) return null;

        var key = (kind, status.Guest) switch
        {
            (AiKind.Exercise, true) => "ai.limit_guest",
            (AiKind.Exercise, false) => "ai.limit_exercises",
            (AiKind.Word, true) => "ai.limit_words_guest",
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

        var usage = await _db.AiUsages.FirstOrDefaultAsync(u => u.UserId == user.Id && u.Day == day);
        if (usage is null)
        {
            usage = new AiUsage { UserId = user.Id, Day = day };
            _db.AiUsages.Add(usage);
        }
        if (kind == AiKind.Exercise) usage.Exercises++;
        else usage.Words++;

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Shu soniyada parallel so'rov xuddi shu qatorni yaratgan — bu
            // hisobni o'tkazib yuboramiz (limit uchun bitta farq muhim emas).
            _db.ChangeTracker.Clear();
        }
    }
}
