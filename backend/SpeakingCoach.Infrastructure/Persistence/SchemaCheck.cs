using Microsoft.EntityFrameworkCore;
namespace SpeakingCoach.Infrastructure.Persistence;

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
/// <summary>ISchemaInspector amalga oshirilishi (admin "Tizim holati" sahifasi uchun).</summary>
public class SchemaInspector(AppDbContext db) : ISchemaInspector
{
    public Task<SchemaStatus> InspectAsync(CancellationToken ct = default) => SchemaCheck.InspectAsync(db, ct);
}

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
