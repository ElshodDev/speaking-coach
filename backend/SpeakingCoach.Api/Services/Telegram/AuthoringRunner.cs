using System.Collections.Concurrent;

namespace SpeakingCoach.Api.Services.Telegram;

/// <summary>
/// Test tayyorlash 1–3 daqiqa oladi — Telegram webhook javobini shuncha
/// kutmaydi (qayta yuboradi). Shuning uchun ish fonda, alohida DI scope'da
/// bajariladi; webhook darhol "tayyorlanmoqda" deb javob beradi.
/// Bitta chatda bir vaqtda bitta ish.
/// </summary>
public class AuthoringRunner(IServiceScopeFactory scopes, ILogger<AuthoringRunner> logger)
{
    public static readonly TimeSpan Timeout = TimeSpan.FromMinutes(8);

    private readonly ConcurrentDictionary<long, byte> _running = new();

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
