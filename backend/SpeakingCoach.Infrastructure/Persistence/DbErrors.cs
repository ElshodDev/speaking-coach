using Microsoft.EntityFrameworkCore;

namespace SpeakingCoach.Infrastructure.Persistence;

/// <summary>Postgres'ga xos xatolarni tanish (provayder tafsiloti — faqat Infrastructure'da).</summary>
public static class DbErrors
{
    /// <summary>Saqlashda noyoblik buzildi (Postgres 23505) — masalan, parallel so'rov xuddi shu taxallus yoki a'zolikni yozib ulgurdi.</summary>
    public static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is Npgsql.PostgresException { SqlState: Npgsql.PostgresErrorCodes.UniqueViolation };
}
