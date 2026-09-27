using System.Text.Json;

namespace SpeakingCoach.Api.Services.Mock;

/// <summary>Ro'yxatdagi bitta test yoki variant: raqami, mavzulari va foydalanuvchi uni ishlaganmi.</summary>
public record CatalogEntry(
    string Id,
    int Number,
    string? Title,
    List<string> Topics,
    bool Ai,
    bool Done,
    decimal? LastScore,
    DateTime? LastAtUtc);

/// <summary>Foydalanuvchining bitta urinishi (test/variant bo'yicha).</summary>
public record CatalogAttempt(string SetId, DateTime AtUtc, decimal? Overall);

/// <summary>
/// "Testni ro'yxatdan tanlash" sahifasi uchun sof mantiq: har test/variantdan
/// qisqa mavzular (Reading — matn sarlavhalari, Listening — qismlar
/// konteksti, Speaking/Writing — asosiy savol) va oxirgi natija.
/// </summary>
public static class MockCatalog
{
    public static string Short(string? text, int max = 80)
    {
        var s = string.Join(' ', (text ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return s.Length <= max ? s : s[..(max - 1)].TrimEnd(' ', ',', '.', ';', ':') + "…";
    }

    /// <summary>Listening/Reading testining mavzulari (JSON buzilgan bo'lsa — bo'sh ro'yxat).</summary>
    public static List<string> Topics(string module, string payload)
    {
        try
        {
            if (module == "reading")
                return JsonSerializer.Deserialize<ReadingTest>(payload, GeminiMockGenerator.Web)?.Passages
                    .Select(p => Short(p.Title, 60)).Where(s => s.Length > 0).ToList() ?? [];
            return JsonSerializer.Deserialize<ListeningTest>(payload, GeminiMockGenerator.Web)?.Parts
                .Select(p => Short(p.Context, 70)).Where(s => s.Length > 0).ToList() ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public static List<string> Topics(SpeakingSet s) => [Short(s.Part1Topic, 40), Short(s.Part2.Topic, 80)];

    public static List<string> Topics(WritingSet w) =>
        [.. (w.Task1.Chart is { } c ? [Short(c.Title, 70)] : new[] { Short(w.Task1.Prompt, 70) }), Short(w.Task2, 110)];

    public static List<string> Topics(CefrSpeakingSet s) => [Short(s.Part2Topic, 80), Short(s.Part3Statement, 80)];

    public static List<string> Topics(CefrWritingSet w) => [Short(w.Role, 70), Short(w.Task2, 110)];

    /// <summary>Har test uchun eng oxirgi urinish (kirish — ixtiyoriy tartibda).</summary>
    public static Dictionary<string, CatalogAttempt> Latest(IEnumerable<CatalogAttempt> attempts) =>
        attempts
            .Where(a => a.SetId.Length > 0)
            .GroupBy(a => a.SetId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(a => a.AtUtc).First());

    public static CatalogEntry Entry(string id, int number, string? title, List<string> topics, bool ai, IReadOnlyDictionary<string, CatalogAttempt> latest) =>
        latest.TryGetValue(id, out var a)
            ? new CatalogEntry(id, number, title, topics, ai, true, a.Overall, a.AtUtc)
            : new CatalogEntry(id, number, title, topics, ai, false, null, null);
}
