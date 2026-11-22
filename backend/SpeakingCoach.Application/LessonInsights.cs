using System.Text.Json;
namespace SpeakingCoach.Application;

public record LessonMistake(string Original, string Corrected, string Explanation, int Students);

/// <summary>O'qituvchi uchun AI dars vazifasi xulosasi: kim bajardi, mini-test o'rtachasi, o'zlashtirish, eng ko'p xatolar.</summary>
public record LessonInsightsResult(
    int Members, int Completed, int? AvgTestPercent, int Strong, int Ok, int Weak, int WroteOwn, List<LessonMistake> TopMistakes);

/// <summary>Toza funksiyalar: Activities'dagi dars yozuvlaridan (PromptData/ResponseData) xulosa — testlanadi.</summary>
public static class LessonInsights
{
    public const int MaxTopMistakes = 5;

    /// <summary>Dars yozuvidagi mavzu (PromptData: {"topic","level"}); buzuq yozuv — null.</summary>
    public static string? TopicOf(string? promptJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(promptJson ?? "");
            return doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty("topic", out var t) && t.ValueKind == JsonValueKind.String
                ? t.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>Dars natijasidagi o'zlashtirish darajasi ("strong" | "ok" | "weak"); buzuq yozuv — null.</summary>
    public static string? MasteryOf(string? responseJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(responseJson ?? "");
            return doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty("result", out var r) && r.ValueKind == JsonValueKind.Object
                && r.TryGetProperty("mastery", out var m) && m.ValueKind == JsonValueKind.String
                ? m.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>Har bir bajargan o'quvchining dars natijasi (ResponseData) bo'yicha jamlama. Buzuq yozuvlar e'tiborsiz.</summary>
    public static LessonInsightsResult Build(int members, IEnumerable<string> responseJsons)
    {
        int completed = 0, strong = 0, ok = 0, weak = 0, wrote = 0;
        double percentSum = 0;
        var percents = 0;
        var mistakes = new Dictionary<string, (string Original, string Corrected, string Explanation, int Count)>(StringComparer.OrdinalIgnoreCase);

        foreach (var json in responseJsons)
        {
            JsonDocument doc;
            try
            {
                doc = JsonDocument.Parse(json);
            }
            catch (JsonException)
            {
                continue;
            }
            using (doc)
            {
                if (doc.RootElement.ValueKind != JsonValueKind.Object || !doc.RootElement.TryGetProperty("result", out var r) || r.ValueKind != JsonValueKind.Object) continue;
                completed++;
                if (r.TryGetProperty("testCorrect", out var tc) && tc.TryGetInt32(out var c) && r.TryGetProperty("testTotal", out var tt) && tt.TryGetInt32(out var total) && total > 0)
                {
                    percentSum += 100.0 * c / total;
                    percents++;
                }
                switch (r.TryGetProperty("mastery", out var m) && m.ValueKind == JsonValueKind.String ? m.GetString() : null)
                {
                    case "strong": strong++; break;
                    case "ok": ok++; break;
                    default: weak++; break;
                }
                if (r.TryGetProperty("wroteOwn", out var w) && w.ValueKind == JsonValueKind.True) wrote++;

                if (!r.TryGetProperty("mistakes", out var list) || list.ValueKind != JsonValueKind.Array) continue;
                // Bir o'quvchida bir xil xato bir marta sanaladi.
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var item in list.EnumerateArray())
                {
                    string Get(string name) => item.ValueKind == JsonValueKind.Object && item.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";
                    var corrected = Get("corrected").Trim();
                    if (corrected.Length == 0 || !seen.Add(corrected)) continue;
                    mistakes[corrected] = mistakes.TryGetValue(corrected, out var cur)
                        ? (cur.Original, cur.Corrected, cur.Explanation, cur.Count + 1)
                        : (Get("original").Trim(), corrected, Get("explanation").Trim(), 1);
                }
            }
        }

        var top = mistakes.Values
            .OrderByDescending(x => x.Count).ThenBy(x => x.Corrected, StringComparer.OrdinalIgnoreCase)
            .Take(MaxTopMistakes)
            .Select(x => new LessonMistake(x.Original, x.Corrected, x.Explanation, x.Count))
            .ToList();
        return new LessonInsightsResult(members, completed, percents == 0 ? null : (int)Math.Round(percentSum / percents), strong, ok, weak, wrote, top);
    }
}
