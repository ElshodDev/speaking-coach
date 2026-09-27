using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SpeakingCoach.Api.Data;

namespace SpeakingCoach.Api.Services.Mock;

/// <summary>Ro'yxatdagi qisqa ma'lumot (gaplarsiz).</summary>
public record ShadowingSummary(string Id, string Kind, string Title, string Level, string? VideoId, string[] Characters, int Lines, int Seconds);

/// <summary>
/// Shadowing darslari: koddagi tayyor darslar (ShadowingBank) + bot orqali
/// qo'shilib, chop etilganlari (MockTests, Exam = "shadowing"). Bazadagi
/// darsning Id si — qator Id si (32 belgili GUID).
/// </summary>
public class ShadowingService(AppDbContext db)
{
    public const string Exam = "shadowing";

    private static ShadowingLesson? Parse(Guid id, string payload)
    {
        try
        {
            var l = JsonSerializer.Deserialize<ShadowingLesson>(payload, MockSets.Web);
            if (l is null) return null;
            l = l with { Id = MockSets.IdOf(id) };
            ShadowingRules.Validate(l);
            return l;
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException)
        {
            return null;
        }
    }

    public static ShadowingSummary Summarize(ShadowingLesson l) =>
        new(l.Id, l.Kind, l.Title, l.Level, l.VideoId, l.Characters, l.Lines.Count, ShadowingRules.Seconds(l));

    /// <summary>Hamma chop etilgan darslar: avval darajasi bo'yicha, keyin eng yangilari.</summary>
    public async Task<List<ShadowingLesson>> LessonsAsync()
    {
        var rows = await db.MockTests
            .Where(t => t.Exam == Exam && t.Status == MockTestStatus.Published)
            .OrderByDescending(t => t.CreatedAtUtc)
            .Select(t => new { t.Id, t.Payload })
            .ToListAsync();
        var stored = rows.Select(r => Parse(r.Id, r.Payload)).OfType<ShadowingLesson>();
        return stored.Concat(ShadowingBank.Lessons)
            .OrderBy(l => Array.IndexOf(ShadowingRules.Levels, l.Level))
            .ToList();
    }

    public async Task<ShadowingLesson?> FindAsync(string? id)
    {
        if (ShadowingBank.Find(id) is { } builtIn) return builtIn;
        if (!Guid.TryParseExact(id, "N", out var gid)) return null;
        var row = await db.MockTests
            .Where(t => t.Id == gid && t.Exam == Exam && t.Status == MockTestStatus.Published)
            .Select(t => new { t.Id, t.Payload })
            .FirstOrDefaultAsync();
        return row is null ? null : Parse(row.Id, row.Payload);
    }
}

public interface IShadowingCoach
{
    Task<ShadowCheck> CheckAsync(string target, byte[] audio, string mimeType, string lang, CancellationToken ct = default);
}

/// <summary>Bitta gap talaffuzini Gemini bilan baholaydi (audio to'g'ridan-to'g'ri, alohida STT yo'q).</summary>
public class GeminiShadowingCoach(GeminiClient gemini) : IShadowingCoach
{
    public async Task<ShadowCheck> CheckAsync(string target, byte[] audio, string mimeType, string lang, CancellationToken ct = default)
    {
        var body = new
        {
            contents = new[]
            {
                new
                {
                    parts = new object[]
                    {
                        new { text = ShadowingRules.CheckPrompt(target, lang) },
                        new { inline_data = new { mime_type = mimeType.Split(';')[0], data = Convert.ToBase64String(audio) } },
                    },
                },
            },
            generationConfig = new { temperature = 0.2, responseMimeType = "application/json" },
        };
        var response = await gemini.SendWithFallbackAsync(body, ct);
        var result = GeminiClient.DeserializeStrict<ShadowCheck>(await GeminiClient.ExtractTextAsync(response, ct));
        ShadowingRules.ValidateCheck(result);
        return ShadowingRules.Align(target, result);
    }
}
