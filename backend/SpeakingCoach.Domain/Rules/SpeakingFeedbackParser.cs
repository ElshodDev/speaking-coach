using System.Text.Json;

namespace SpeakingCoach.Domain.Rules;

/// <summary>Gapirish natijasining YANGI (ixtiyoriy) qismlari: talaffuz, 4 mezonli band, bitta asosiy tuzatish.</summary>
public record SpeakingExtras(ScoreWithReasoning? Pronunciation, SpeakingBands? Bands, PriorityFix? Fix, decimal? OverallBand);

/// <summary>
/// Gemini javobidan yangi qismlarni TOLERANT o'qiydi: asosiy natija (ravonlik/grammatika/lug'at) qat'iy o'qiladi
/// va o'zgarmagan; bu qismlar yo'q yoki noto'g'ri bo'lsa — shunchaki null (butun natija yiqilmaydi).
/// Toza funksiya (tarmoqsiz) — testlanadi.
/// </summary>
public static class SpeakingFeedbackParser
{
    public const int MaxTextChars = 300;

    public static SpeakingExtras Parse(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return Empty;
            var bands = ReadBands(root);
            return new SpeakingExtras(
                ReadPronunciation(root),
                bands,
                ReadFix(root),
                bands is null ? null : IeltsBand.RoundHalfBand((bands.Fluency + bands.Lexical + bands.Grammar + bands.Pronunciation) / 4m));
        }
        catch (JsonException)
        {
            return Empty;
        }
    }

    private static SpeakingExtras Empty => new(null, null, null, null);

    private static ScoreWithReasoning? ReadPronunciation(JsonElement root)
    {
        if (!root.TryGetProperty("pronunciation", out var p) || p.ValueKind != JsonValueKind.Object) return null;
        if (!p.TryGetProperty("score", out var s) || s.ValueKind != JsonValueKind.Number || !s.TryGetDecimal(out var score)) return null;
        var reasoning = Text(p, "reasoning");
        if (score is < 0 or > 100 || string.IsNullOrWhiteSpace(reasoning)) return null;
        return new ScoreWithReasoning((int)Math.Round(score, MidpointRounding.AwayFromZero), reasoning);
    }

    private static SpeakingBands? ReadBands(JsonElement root)
    {
        if (!root.TryGetProperty("bands", out var b) || b.ValueKind != JsonValueKind.Object) return null;
        decimal? Band(string name) =>
            b.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetDecimal(out var d) && d is >= 0 and <= 9
                ? IeltsBand.RoundHalfBand(d)
                : null;
        var (f, l, g, p) = (Band("fluency"), Band("lexical"), Band("grammar"), Band("pronunciation"));
        return f is null || l is null || g is null || p is null ? null : new SpeakingBands(f.Value, l.Value, g.Value, p.Value);
    }

    private static PriorityFix? ReadFix(JsonElement root)
    {
        if (!root.TryGetProperty("priorityFix", out var f) || f.ValueKind != JsonValueKind.Object) return null;
        var (issue, corrected, tip) = (Text(f, "issue"), Text(f, "corrected"), Text(f, "tip"));
        if (string.IsNullOrWhiteSpace(issue) || string.IsNullOrWhiteSpace(corrected) || string.IsNullOrWhiteSpace(tip)) return null;
        return new PriorityFix(issue, Text(f, "original"), corrected, tip);
    }

    private static string Text(JsonElement o, string name)
    {
        var s = o.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";
        s = s.Trim();
        return s.Length > MaxTextChars ? s[..MaxTextChars] : s;
    }
}
