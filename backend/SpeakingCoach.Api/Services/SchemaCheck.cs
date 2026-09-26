using Microsoft.EntityFrameworkCore;
using SpeakingCoach.Api.Data;

namespace SpeakingCoach.Api.Services;

/// <summary>
/// Baza sxemasi kod bilan mosmi. Render migratsiyalarni o'zi qo'llamaydi —
/// jadval yetishmasa, xato faqat o'sha jadvalga murojaat qilinganda chiqadi
/// (masalan: "relation TelegramAccounts does not exist"). Bu tekshiruv
/// muammoni server ishga tushgan zahoti logda va admin panelda ko'rsatadi.
/// </summary>
public record SchemaStatus(
    bool Checked,
    IReadOnlyList<string> PendingMigrations,
    IReadOnlyList<string> MissingTables,
    string? Error)
{
    public bool Ok => Checked && Error is null && PendingMigrations.Count == 0 && MissingTables.Count == 0;
}

public static class SchemaCheck
{
    /// <summary>Kod kutayotgan, lekin bazada yo'q jadvallar (Postgres nomlari katta-kichik harfga sezgir).</summary>
    public static List<string> Missing(IEnumerable<string> expected, IEnumerable<string> existing)
    {
        var have = existing.ToHashSet(StringComparer.Ordinal);
        return expected.Where(t => !have.Contains(t)).Distinct().OrderBy(t => t, StringComparer.Ordinal).ToList();
    }

    public static async Task<SchemaStatus> InspectAsync(AppDbContext db, CancellationToken ct = default)
    {
        try
        {
            var pending = (await db.Database.GetPendingMigrationsAsync(ct)).ToList();
            var expected = db.Model.GetEntityTypes()
                .Select(e => e.GetTableName())
                .Where(n => n is not null)
                .Select(n => n!)
                .ToList();
            var existing = await db.Database
                .SqlQueryRaw<string>("SELECT table_name::text AS \"Value\" FROM information_schema.tables WHERE table_schema = current_schema()")
                .ToListAsync(ct);
            return new SchemaStatus(true, pending, Missing(expected, existing), null);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new SchemaStatus(false, [], [], ex.GetType().Name + ": " + ex.Message);
        }
    }
}

/// <summary>
/// Ishga tushganda bir marta tekshiradi. Ilovani to'xtatmaydi (baza vaqtincha
/// ishlamasa ham sayt ochilishi kerak) — faqat logga aniq xabar yozadi.
/// </summary>
public class SchemaStartupCheck(IServiceScopeFactory scopes, ILogger<SchemaStartupCheck> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var status = await SchemaCheck.InspectAsync(db, stoppingToken);

        if (!status.Checked)
        {
            logger.LogWarning("Baza sxemasini tekshirib bo'lmadi: {Error}", status.Error);
        }
        else if (status.Ok)
        {
            logger.LogInformation("Baza sxemasi kod bilan mos");
        }
        else
        {
            logger.LogError(
                "BAZA SXEMASI ESKI! Qo'llanmagan migratsiyalar: [{Pending}]; yetishmayotgan jadvallar: [{Missing}]. " +
                "Kompyuterda bajaring: dotnet ef database update",
                string.Join(", ", status.PendingMigrations), string.Join(", ", status.MissingTables));
        }
    }
}
