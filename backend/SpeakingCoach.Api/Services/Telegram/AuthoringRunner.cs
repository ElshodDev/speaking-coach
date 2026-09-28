using System.Collections.Concurrent;

namespace SpeakingCoach.Api.Services.Telegram;

/// <summary>
/// Test tayyorlash 1–3 daqiqa oladi — Telegram webhook javobini shuncha
/// kutmaydi (qayta yuboradi). Shuning uchun ish fonda, alohida DI scope'da
/// bajariladi; webhook darhol "tayyorlanmoqda" deb javob beradi.
/// Bitta chatda bir vaqtda bitta ish. Har bir urinish (admin ham) — kunlik
/// AI limitidan (Ai:AuthoringPerDay, Toshkent kuni): bitta test 2–8 ta Gemini so'rovi.
/// </summary>
public class AuthoringRunner(IServiceScopeFactory scopes, ILogger<AuthoringRunner> logger, AiDailyCaps caps, AiQuotaOptions limits)
{
    public static readonly TimeSpan Timeout = TimeSpan.FromMinutes(8);

    private readonly ConcurrentDictionary<long, byte> _running = new();

    /// <summary>Kunlik urinishlar chegarasi (Ai:AuthoringPerDay, standart 10).</summary>
    public int PerDay => limits.Authoring;

    /// <summary>Bitta urinish uchun joy (false — bugungi limit tugagan).</summary>
    public bool TryTakeAttempt(Guid userId) =>
        caps.TryTake(AiDailyCaps.AuthoringKey(userId), AiQuotaService.Today(DateTime.UtcNow), limits.Authoring);

    /// <summary>Urinish boshlanmadi yoki Gemini'ga yetib bormadi (AI band) — qaytariladi.</summary>
    public void RefundAttempt(Guid userId) =>
        caps.Refund(AiDailyCaps.AuthoringKey(userId), AiQuotaService.Today(DateTime.UtcNow));

    public bool IsRunning(long chatId) => _running.ContainsKey(chatId);

    public bool TryStart(long chatId, Func<IServiceProvider, CancellationToken, Task> work)
    {
        if (!_running.TryAdd(chatId, 0)) return false;
        _ = Task.Run(async () =>
        {
            try
            {
                using var cts = new CancellationTokenSource(Timeout);
                using var scope = scopes.CreateScope();
                await work(scope.ServiceProvider, cts.Token);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Fondagi test tayyorlash ishi yiqildi: chat {Chat}", chatId);
            }
            finally
            {
                _running.TryRemove(chatId, out _);
            }
        });
        return true;
    }
}
