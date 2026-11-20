using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace SpeakingCoach.Api.Services;

public record FluencyFiller(
    [property: JsonPropertyName("word")] string Word,
    [property: JsonPropertyName("count")] int Count);

/// <summary>
/// Gapirish ravonligi o'lchovlari — AI bahosiga qo'shimcha, ob'ektiv sonlar.
/// Seconds/Wpm/LongPauses — brauzer yozuv uzunligi va pauzalarni yuborgan bo'lsa (aks holda null).
/// </summary>
public record FluencyStats(
    [property: JsonPropertyName("words")] int Words,
    [property: JsonPropertyName("seconds")] double? Seconds,
    [property: JsonPropertyName("wpm")] int? Wpm,
    [property: JsonPropertyName("pace")] string Pace,
    [property: JsonPropertyName("fillers")] int Fillers,
    [property: JsonPropertyName("fillersPerMinute")] double? FillersPerMinute,
    [property: JsonPropertyName("topFillers")] List<FluencyFiller> TopFillers,
    [property: JsonPropertyName("repetitions")] int Repetitions,
    [property: JsonPropertyName("selfCorrections")] int SelfCorrections,
    [property: JsonPropertyName("longPauses")] int? LongPauses,
    [property: JsonPropertyName("pauseSeconds")] double? PauseSeconds);

/// <summary>Progress grafigi uchun bitta urinish.</summary>
public record FluencyPoint(
    [property: JsonPropertyName("at")] DateTime At,
    [property: JsonPropertyName("wpm")] int? Wpm,
    [property: JsonPropertyName("fillersPerMinute")] double? FillersPerMinute,
    [property: JsonPropertyName("longPauses")] int? LongPauses,
    // Qayerdan: "speaking" (mashq), "mock" (Speaking imtihoni), "talk" (AI suhbat).
    [property: JsonPropertyName("source")] string Source = "speaking");

/// <summary>
/// Ravonlik o'lchovlari transkript (Gemini uni to'ldiruvchi so'zlar va takrorlar bilan
/// "aynan" yozadi) va yozuv uzunligidan hisoblanadi — qo'shimcha AI so'rovisiz.
/// Toza funksiyalar — unit test qilinadi.
/// </summary>
public static partial class FluencyMetrics
{
    /// <summary>Daqiqasiga so'z: shundan sekin — "sekin", shundan tez — "tez" (ma'noli so'zlar bo'yicha).</summary>
    public const int SlowBelowWpm = 90;
    public const int FastAboveWpm = 170;

    /// <summary>Juda qisqa yozuvda (yoki vaqt noma'lum bo'lsa) sur'at hisoblanmaydi.</summary>
    public const double MinSecondsForRate = 5;

    public const double MaxSeconds = 900;

    /// <summary>
    /// Bitta so'zli to'ldiruvchilar: ingliz tilidagi va o'zbek/rus tilida so'zlashuvchilar
    /// ko'p qo'shib yuboradiganlari. "like", "so", "well" — ataylab yo'q: ular ko'pincha ma'noli.
    /// </summary>
    private static readonly HashSet<string> SingleFillers = new(StringComparer.Ordinal)
    {
        "um", "umm", "ummm", "uh", "uhh", "uhm", "er", "erm", "err", "ah", "ahh", "eh", "hmm", "hm", "mm", "mmm",
        "anaqa", "haligi", "xullas", "ну", "короче", "типа", "вот",
    };

    /// <summary>Ikki so'zli to'ldiruvchilar.</summary>
    private static readonly string[][] PhraseFillers = [["you", "know"], ["i", "mean"]];

    /// <summary>Xatosini tuzatish belgilari ("sorry", "I mean" — to'ldiruvchi sifatida ham sanaladi).</summary>
    private static readonly HashSet<string> CorrectionMarkers = new(StringComparer.Ordinal) { "sorry", "pardon" };

    // So'z: harflar (lotin, kirill, o'zbek ʻ/' bilan), ichida apostrof yoki defis; oxiridagi "-" — uzilgan so'z.
    [GeneratedRegex(@"[\p{L}\p{M}][\p{L}\p{M}'’ʻʼ]*(?:-[\p{L}\p{M}'’ʻʼ]+)*-?", RegexOptions.CultureInvariant)]
    private static partial Regex WordRegex();

    public static List<string> Tokenize(string? transcript) =>
        WordRegex().Matches(transcript ?? "").Select(m => m.Value.ToLowerInvariant()).ToList();

    /// <summary>Brauzer yuborgan qiymatlarni tekshiradi: noto'g'ri yoki mantiqsiz — null.</summary>
    public static (double? Seconds, int? LongPauses, double? PauseSeconds) Sanitize(string? seconds, string? longPauses, string? pauseSeconds)
    {
        static double? Num(string? s) =>
            double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) && double.IsFinite(v) ? v : null;

        var sec = Num(seconds) is { } x && x > 0 && x <= MaxSeconds ? Math.Round(x, 1) : (double?)null;
        int? pauses = int.TryParse(longPauses, NumberStyles.Integer, CultureInfo.InvariantCulture, out var p) && p >= 0 && p <= 500 ? p : null;
        var pauseSec = Num(pauseSeconds) is { } ps && ps >= 0 && (sec is null || ps <= sec) ? Math.Round(ps, 1) : (double?)null;
        if (sec is null) (pauses, pauseSec) = (null, null); // vaqt noma'lum — pauzalar ham ishonchsiz
        return (sec, pauses, pauseSec);
    }

    public static FluencyStats Analyze(string? transcript, double? seconds, int? longPauses = null, double? pauseSeconds = null)
    {
        var tokens = Tokenize(transcript);
        var fillerCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        var isFiller = new bool[tokens.Count];

        for (var i = 0; i < tokens.Count; i++)
        {
            if (isFiller[i]) continue;
            var phrase = PhraseFillers.FirstOrDefault(pf => i + pf.Length <= tokens.Count && pf.Select((w, k) => tokens[i + k] == w).All(x => x));
            if (phrase is not null)
            {
                for (var k = 0; k < phrase.Length; k++) isFiller[i + k] = true;
                Count(fillerCounts, string.Join(' ', phrase));
                continue;
            }
            if (SingleFillers.Contains(tokens[i].TrimEnd('-')))
            {
                isFiller[i] = true;
                Count(fillerCounts, Canonical(tokens[i].TrimEnd('-')));
            }
        }

        var content = tokens.Where((_, i) => !isFiller[i]).ToList();

        var repetitions = CountRepetitions(content);

        // O'zini tuzatish: uzilgan so'z ("wen- went") yoki "sorry".
        var selfCorrections = tokens.Count(t => t.Length > 1 && t.EndsWith('-')) + tokens.Count(CorrectionMarkers.Contains);

        var words = content.Count(t => !t.EndsWith('-'));
        var fillers = fillerCounts.Values.Sum();
        var rateKnown = seconds is { } s && s >= MinSecondsForRate;
        var minutes = rateKnown ? seconds!.Value / 60.0 : 0;
        int? wpm = rateKnown ? (int)Math.Round(words / minutes) : null;
        double? fpm = rateKnown ? Math.Round(fillers / minutes, 1) : null;

        return new FluencyStats(
            Words: words,
            Seconds: seconds,
            Wpm: wpm,
            Pace: PaceOf(wpm),
            Fillers: fillers,
            FillersPerMinute: fpm,
            TopFillers: fillerCounts.OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key, StringComparer.Ordinal)
                .Take(5).Select(kv => new FluencyFiller(kv.Key, kv.Value)).ToList(),
            Repetitions: repetitions,
            SelfCorrections: selfCorrections,
            LongPauses: rateKnown ? longPauses : null,
            PauseSeconds: rateKnown ? pauseSeconds : null);
    }

    /// <summary>
    /// Takror: darhol qaytarilgan 1–3 so'zlik guruh — "I I think", "I think I think",
    /// "I think that I think that". Bitta takror bir marta sanaladi (eng uzun guruh bo'yicha).
    /// </summary>
    public static int CountRepetitions(IReadOnlyList<string> words)
    {
        var count = 0;
        var i = 0;
        while (i < words.Count)
        {
            var step = 1;
            for (var n = 3; n >= 1; n--)
            {
                if (i + 2 * n > words.Count) continue;
                var same = true;
                for (var k = 0; k < n && same; k++)
                    same = words[i + k] == words[i + n + k] && !words[i + k].EndsWith('-');
                if (!same) continue;
                count++;
                step = n;
                break;
            }
            i += step;
        }
        return count;
    }

    public static string PaceOf(int? wpm) => wpm switch
    {
        null => "unknown",
        < SlowBelowWpm => "slow",
        > FastAboveWpm => "fast",
        _ => "natural",
    };

    /// <summary>Saqlangan natija (ResponseData) ichidagi "metrics" — eski yozuvlarda yo'q (null).</summary>
    public static FluencyStats? FromResponse(string? responseJson)
    {
        if (string.IsNullOrWhiteSpace(responseJson)) return null;
        try
        {
            using var doc = JsonDocument.Parse(responseJson);
            return doc.RootElement.ValueKind == JsonValueKind.Object
                && doc.RootElement.TryGetProperty("metrics", out var m) && m.ValueKind == JsonValueKind.Object
                ? m.Deserialize<FluencyStats>()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>Baholash natijasi + "metrics" — bitta JSON obyekt (ResponseData uchun).</summary>
    public static string WithMetrics<T>(T evaluation, FluencyStats metrics, JsonSerializerOptions? options = null) =>
        WithMetricsNode(evaluation, metrics, options).ToJsonString(options);

    /// <summary>
    /// Natija + "metrics" JSON obyekt sifatida. options — natijaning o'z nomlash qoidasi
    /// (mock natijalari camelCase bilan saqlanadi); "metrics" maydonlari doim bir xil nomda.
    /// </summary>
    public static System.Text.Json.Nodes.JsonObject WithMetricsNode<T>(T evaluation, FluencyStats metrics, JsonSerializerOptions? options = null)
    {
        var node = JsonSerializer.SerializeToNode(evaluation, options)!.AsObject();
        node["metrics"] = JsonSerializer.SerializeToNode(metrics);
        return node;
    }

    /// <summary>Bir nechta javob (mock savollari, suhbat navbatlari) — bitta umumiy o'lchov.</summary>
    public static FluencyStats? Combine(IReadOnlyCollection<FluencyStats> parts)
    {
        if (parts.Count == 0) return null;
        var timed = parts.Where(p => p.Seconds is not null && p.Wpm is not null).ToList();
        double? seconds = timed.Count > 0 ? Math.Round(timed.Sum(p => p.Seconds!.Value), 1) : null;
        var minutes = seconds / 60.0;
        int? wpm = seconds is not null ? (int)Math.Round(timed.Sum(p => p.Words) / minutes!.Value) : null;
        double? fpm = seconds is not null ? Math.Round(timed.Sum(p => p.Fillers) / minutes!.Value, 1) : null;
        // Pauzalar — faqat hammasi o'lchangan bo'lsa (aks holda yig'indi kam ko'rinardi).
        var allPauses = timed.Count > 0 && timed.All(p => p.LongPauses is not null);
        return new FluencyStats(
            Words: parts.Sum(p => p.Words),
            Seconds: seconds,
            Wpm: wpm,
            Pace: PaceOf(wpm),
            Fillers: parts.Sum(p => p.Fillers),
            FillersPerMinute: fpm,
            TopFillers: parts.SelectMany(p => p.TopFillers)
                .GroupBy(f => f.Word)
                .Select(g => new FluencyFiller(g.Key, g.Sum(f => f.Count)))
                .OrderByDescending(f => f.Count).ThenBy(f => f.Word, StringComparer.Ordinal)
                .Take(5).ToList(),
            Repetitions: parts.Sum(p => p.Repetitions),
            SelfCorrections: parts.Sum(p => p.SelfCorrections),
            LongPauses: allPauses ? timed.Sum(p => p.LongPauses!.Value) : null,
            PauseSeconds: allPauses ? Math.Round(timed.Sum(p => p.PauseSeconds ?? 0), 1) : null);
    }

    /// <summary>
    /// Mock imtihon javoblari: har bir javob (transkript + soniyalar) alohida o'lchanadi, keyin
    /// birlashtiriladi. Pauzalar formada "p{i}" (soni) va "ps{i}" (soniyalari) — brauzerdan.
    /// </summary>
    public static FluencyStats? ForAnswers(IEnumerable<(int Index, string Transcript, int Seconds)> answers, Func<string, string?> form)
    {
        var parts = answers
            .Where(a => !string.IsNullOrWhiteSpace(a.Transcript) && a.Seconds > 0)
            .Select(a =>
            {
                var (sec, pauses, pauseSec) = Sanitize(
                    a.Seconds.ToString(CultureInfo.InvariantCulture), form($"p{a.Index}"), form($"ps{a.Index}"));
                return Analyze(a.Transcript, sec, pauses, pauseSec);
            })
            .ToList();
        return Combine(parts);
    }

    private static string Canonical(string filler) => filler switch
    {
        "umm" or "ummm" or "uhm" => "um",
        "uhh" => "uh",
        "erm" or "err" => "er",
        "ahh" => "ah",
        "hm" or "mm" or "mmm" => "hmm",
        _ => filler,
    };

    private static void Count(Dictionary<string, int> counts, string key) => counts[key] = counts.GetValueOrDefault(key) + 1;
}
