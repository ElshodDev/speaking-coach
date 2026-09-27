namespace SpeakingCoach.Api.Services.Mock;

/// <summary>
/// AI bilan test yaratishni himoyalaydi (Gemini qimmat va sekin):
/// <list type="bullet">
/// <item>bir foydalanuvchi uchun bir vaqtda faqat bitta yaratish — parallel
/// so'rovlar bilan kunlik limitni aylanib o'tib bo'lmaydi;</item>
/// <item>24 soatlik urinishlar soni — muvaffaqiyatsiz urinishlar ham sanaladi
/// (bazadagi limit faqat saqlangan testlarni ko'radi).</item>
/// </list>
/// Xotirada (bitta server) — qayta ishga tushsa nolga tushadi, bu yetarli:
/// asosiy kunlik limit baribir bazada.
/// </summary>
public sealed class GenerationGate(TimeProvider? clock = null)
{
    public enum Outcome { Ok, Busy, Exhausted }

    public static readonly TimeSpan Window = TimeSpan.FromHours(24);

    private readonly TimeProvider _clock = clock ?? TimeProvider.System;
    private readonly object _lock = new();
    private readonly Dictionary<Guid, List<DateTime>> _attempts = new();
    private readonly HashSet<Guid> _running = new();

    /// <summary>Ruxsat bo'lsa — lease (Dispose qilinganda "band" holati bo'shaydi).</summary>
    public Outcome TryEnter(Guid userId, int maxAttempts, out IDisposable? lease)
    {
        lease = null;
        var now = _clock.GetUtcNow().UtcDateTime;
        lock (_lock)
        {
            if (_running.Contains(userId)) return Outcome.Busy;
            if (!_attempts.TryGetValue(userId, out var list)) _attempts[userId] = list = [];
            list.RemoveAll(t => now - t >= Window);
            if (list.Count >= maxAttempts) return Outcome.Exhausted;
            list.Add(now);
            _running.Add(userId);
            if (_attempts.Count > 5000) Prune(now);
        }
        lease = new Lease(this, userId);
        return Outcome.Ok;
    }

    public int AttemptsOf(Guid userId)
    {
        var now = _clock.GetUtcNow().UtcDateTime;
        lock (_lock) return _attempts.TryGetValue(userId, out var l) ? l.Count(t => now - t < Window) : 0;
    }

    private void Prune(DateTime now)
    {
        foreach (var (id, list) in _attempts.ToList())
        {
            list.RemoveAll(t => now - t >= Window);
            if (list.Count == 0 && !_running.Contains(id)) _attempts.Remove(id);
        }
    }

    private void Release(Guid userId)
    {
        lock (_lock) _running.Remove(userId);
    }

    private sealed class Lease(GenerationGate gate, Guid userId) : IDisposable
    {
        private int _disposed;
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0) gate.Release(userId);
        }
    }
}
