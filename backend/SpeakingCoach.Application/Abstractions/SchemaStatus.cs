using Microsoft.EntityFrameworkCore;

namespace SpeakingCoach.Application.Abstractions;

/// <summary>Baza sxemasi holati (qo'llanmagan migratsiyalar, yetishmayotgan jadvallar) — Infrastructure tekshiradi.</summary>
public interface ISchemaInspector
{
    Task<SchemaStatus> InspectAsync(CancellationToken ct = default);
}

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
