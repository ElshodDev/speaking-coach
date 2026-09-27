using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SpeakingCoach.Api.Data;

namespace SpeakingCoach.Api.Services.Mock;

/// <summary>
/// Speaking va Writing variantlari: koddagi tayyor to'plam (IeltsBank,
/// CefrBank) + bot orqali qo'shilib, tasdiqlangan variantlar (MockTests
/// jadvali, Status = published). Bazadagi variantning Id si — qator Id si
/// (32 belgili GUID), shuning uchun ikkala manba to'qnashmaydi.
/// </summary>
public class MockSets(AppDbContext db)
{
    public static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    public static string IdOf(Guid id) => id.ToString("N");

    private async Task<List<(Guid Id, string Payload)>> RowsAsync(string exam, string module, string? variant = null)
    {
        var q = db.MockTests.Where(t => t.Exam == exam && t.Module == module && t.Status == MockTestStatus.Published);
        if (variant is not null) q = q.Where(t => t.Variant == variant);
        return (await q.OrderBy(t => t.CreatedAtUtc).Select(t => new { t.Id, t.Payload }).ToListAsync())
            .Select(r => (r.Id, r.Payload)).ToList();
    }

    private async Task<(Guid Id, string Payload)?> RowAsync(string exam, string module, string? setId)
    {
        if (!Guid.TryParseExact(setId, "N", out var id)) return null;
        var r = await db.MockTests
            .Where(t => t.Id == id && t.Exam == exam && t.Module == module && t.Status == MockTestStatus.Published)
            .Select(t => new { t.Id, t.Payload })
            .FirstOrDefaultAsync();
        return r is null ? null : (r.Id, r.Payload);
    }

    private static T? Parse<T>(string payload) where T : class
    {
        try
        {
            return JsonSerializer.Deserialize<T>(payload, Web);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public async Task<List<SpeakingSet>> IeltsSpeakingAsync() =>
        [.. IeltsBank.Speaking, .. (await RowsAsync("ielts", "speaking")).Select(r => Parse<SpeakingSet>(r.Payload) is { } s ? s with { Id = IdOf(r.Id) } : null).OfType<SpeakingSet>()];

    public async Task<List<WritingSet>> IeltsWritingAsync(string variant) =>
        [.. IeltsBank.Writing.Where(w => w.Variant == variant), .. (await RowsAsync("ielts", "writing", variant)).Select(r => Parse<WritingSet>(r.Payload) is { } s ? s with { Id = IdOf(r.Id), Variant = variant } : null).OfType<WritingSet>()];

    public async Task<List<CefrSpeakingSet>> CefrSpeakingAsync() =>
        [.. CefrBank.Speaking, .. (await RowsAsync("cefr", "speaking")).Select(r => Parse<CefrSpeakingSet>(r.Payload) is { } s ? s with { Id = IdOf(r.Id) } : null).OfType<CefrSpeakingSet>()];

    public async Task<List<CefrWritingSet>> CefrWritingAsync() =>
        [.. CefrBank.Writing, .. (await RowsAsync("cefr", "writing")).Select(r => Parse<CefrWritingSet>(r.Payload) is { } s ? s with { Id = IdOf(r.Id) } : null).OfType<CefrWritingSet>()];

    public async Task<SpeakingSet?> FindIeltsSpeakingAsync(string? id) =>
        IeltsBank.FindSpeaking(id) ?? (await RowAsync("ielts", "speaking", id) is { } r && Parse<SpeakingSet>(r.Payload) is { } s ? s with { Id = IdOf(r.Id) } : null);

    public async Task<WritingSet?> FindIeltsWritingAsync(string? id)
    {
        if (IeltsBank.FindWriting(id) is { } w) return w;
        if (!Guid.TryParseExact(id, "N", out var gid)) return null;
        var r = await db.MockTests
            .Where(t => t.Id == gid && t.Exam == "ielts" && t.Module == "writing" && t.Status == MockTestStatus.Published)
            .Select(t => new { t.Id, t.Payload, t.Variant })
            .FirstOrDefaultAsync();
        return r is not null && Parse<WritingSet>(r.Payload) is { } s ? s with { Id = IdOf(r.Id), Variant = r.Variant } : null;
    }

    public async Task<CefrSpeakingSet?> FindCefrSpeakingAsync(string? id) =>
        CefrBank.FindSpeaking(id) ?? (await RowAsync("cefr", "speaking", id) is { } r && Parse<CefrSpeakingSet>(r.Payload) is { } s ? s with { Id = IdOf(r.Id) } : null);

    public async Task<CefrWritingSet?> FindCefrWritingAsync(string? id) =>
        CefrBank.FindWriting(id) ?? (await RowAsync("cefr", "writing", id) is { } r && Parse<CefrWritingSet>(r.Payload) is { } s ? s with { Id = IdOf(r.Id) } : null);
}
