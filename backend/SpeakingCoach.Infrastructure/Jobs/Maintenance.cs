using Microsoft.EntityFrameworkCore;
namespace SpeakingCoach.Infrastructure.Jobs;

/// <summary>
/// Server ichidagi tozalash: ishga tushgandan 5 daqiqa o'tib, keyin har 6 soatda.
/// Render bepul tarifida server uxlab qolsa — uyg'onganda davom etadi.
/// </summary>
public class MaintenanceService(IServiceScopeFactory scopes, ILogger<MaintenanceService> logger) : BackgroundService
{
    public static readonly TimeSpan FirstDelay = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan Interval = TimeSpan.FromHours(6);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(FirstDelay, stoppingToken);
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = scopes.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var removed = await Maintenance.CleanupAsync(db, DateTime.UtcNow);
                    if (removed > 0) logger.LogInformation("Tozalash: {Count} ta eski yozuv o'chirildi", removed);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogWarning(ex, "Tozalash bajarilmadi (keyingi safar qayta uriniladi)");
                }
                await Task.Delay(Interval, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Server to'xtayapti.
        }
    }
}
