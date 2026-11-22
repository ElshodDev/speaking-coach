using Microsoft.EntityFrameworkCore;
namespace SpeakingCoach.Infrastructure.Persistence;

/// <summary>Oxirgi tuzatishlar: faqat "topCorrections" ustuni SQL bilan olinadi (ResponseData to'liq tortilmaydi).</summary>
public class EfMistakeSource(AppDbContext db) : IMistakeSource
{
    public async Task<List<MistakeFact>> RecentAsync(Guid userId, CancellationToken ct = default)
    {
        var rows = await db.Database.SqlQueryRaw<CorrectionsRow>(MistakeQuery.CorrectionsSql, userId, MistakeQuery.MaxActivities).ToListAsync(ct);
        return [.. rows.SelectMany(r => MistakeAnalyzer.FromResponse(r.Json, r.CreatedAtUtc))];
    }
}
