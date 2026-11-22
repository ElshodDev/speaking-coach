using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
namespace SpeakingCoach.Application;

/// <summary>Bitta yaxshilash: o'quvchining aynan iborasi → maqsadli darajadagi variant.</summary>
public record UpgradeItem(
    [property: JsonPropertyName("original")] string Original,
    [property: JsonPropertyName("upgraded")] string Upgraded,
    [property: JsonPropertyName("kind")] string Kind,
    [property: JsonPropertyName("why")] string Why);

/// <summary>"Javobimni yaxshila" natijasi: shu fikrlar maqsadli darajada qanday aytilardi.</summary>
public record UpgradeResult(
    [property: JsonPropertyName("target")] string Target,
    [property: JsonPropertyName("upgradedText")] string UpgradedText,
    [property: JsonPropertyName("items")] List<UpgradeItem> Items,
    [property: JsonPropertyName("summary")] string Summary);

/// <summary>Yaxshilanadigan matn: qaysi mashqdan, mavzu va o'quvchining o'z matni (transkript yoki insho).</summary>
public record UpgradeSource(string Kind, string Topic, string Text);

/// <summary>Qoidalar — toza funksiyalar (unit test qilinadi).</summary>
public static partial class AnswerUpgradeRules
{
    /// <summary>Maqsad sifatida tanlash mumkin bo'lgan darajalar (A2 — maqsad emas).</summary>
    public static readonly string[] Targets = ["B1", "B2", "C1"];

    public static readonly string[] Kinds = ["vocabulary", "grammar", "linking", "structure"];

    public const int MinWords = 15;
    public const int MaxChars = 4000;
    public const int MaxItems = 8;

    /// <summary>Standart maqsad — o'quvchi darajasidan bir pog'ona yuqori (C1 bo'lsa — C1).</summary>
    public static string DefaultTarget(string? level) => LearnerLevel.Normalize(level) switch
    {
        "A2" => "B1",
        "B1" => "B2",
        _ => "C1",
    };

    public static string? NormalizeTarget(string? target)
    {
        var t = (target ?? "").Trim().ToUpperInvariant();
        return Targets.Contains(t) ? t : null;
    }

    /// <summary>
    /// Saqlangan mashqdan manba matn. Speaking va Part 2 — transkript (ResponseData), Writing — insho (PromptData).
    /// Boshqa turlar yoki o'qib bo'lmaydigan yozuv — null.
    /// </summary>
    public static UpgradeSource? SourceOf(ActivityType type, string promptJson, string responseJson)
    {
        try
        {
            using var prompt = JsonDocument.Parse(promptJson);
            var topic = Str(prompt.RootElement, "topic");
            if (type == ActivityType.Writing) return Make("writing", topic, Str(prompt.RootElement, "text"));
            if (type != ActivityType.Speaking) return null;
            using var resp = JsonDocument.Parse(responseJson);
            var kind = prompt.RootElement.TryGetProperty("part2", out var p2) && p2.ValueKind == JsonValueKind.True ? "part2" : "speaking";
            return Make(kind, topic, Str(resp.RootElement, "transcript"));
        }
        catch (JsonException)
        {
            return null;
        }

        static UpgradeSource? Make(string kind, string topic, string text) =>
            string.IsNullOrWhiteSpace(text) ? null : new UpgradeSource(kind, topic, text.Trim());
    }

    public static int WordCount(string text) => WordRegex().Matches(text).Count;

    /// <summary>
    /// AI javobini tekshiradi: "original" o'quvchi matnida aynan bo'lishi shart (o'ylab topilgan iboralar
    /// tushiriladi), o'zgarmaganlari va takrorlar tashlanadi, tur — ro'yxatdagilardan biri, ko'pi bilan 8 ta.
    /// </summary>
    public static UpgradeResult Sanitize(UpgradeResult ai, string sourceText, string target)
    {
        var haystack = Norm(sourceText);
        var items = (ai.Items ?? [])
            .Where(i => !string.IsNullOrWhiteSpace(i.Original) && !string.IsNullOrWhiteSpace(i.Upgraded))
            .Select(i => i with
            {
                Original = Cut(i.Original.Trim(), 200),
                Upgraded = Cut(i.Upgraded.Trim(), 240),
                Kind = i.Kind?.Trim().ToLowerInvariant() is { } kind && Kinds.Contains(kind) ? kind : "vocabulary",
                Why = Cut((i.Why ?? "").Trim(), 300),
            })
            .Where(i => haystack.Contains(Norm(i.Original), StringComparison.Ordinal))
            .Where(i => !string.Equals(Norm(i.Original), Norm(i.Upgraded), StringComparison.Ordinal))
            .DistinctBy(i => Norm(i.Original))
            .Take(MaxItems)
            .ToList();
        return new UpgradeResult(target, Cut((ai.UpgradedText ?? "").Trim(), MaxChars * 2), items, Cut((ai.Summary ?? "").Trim(), 400));
    }

    /// <summary>ResponseData ichidagi keshlangan natija ("upgrades" → maqsad); yo'q bo'lsa — null.</summary>
    public static UpgradeResult? Cached(string responseJson, string target)
    {
        try
        {
            var node = JsonNode.Parse(responseJson) as JsonObject;
            return node?["upgrades"]?[target]?.Deserialize<UpgradeResult>();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>ResponseData'ga natijani "upgrades" → maqsad ostida qo'shadi (boshqa maydonlar o'zgarmaydi).</summary>
    public static string WithCached(string responseJson, UpgradeResult result)
    {
        var node = (JsonNode.Parse(responseJson) as JsonObject) ?? new JsonObject();
        if (node["upgrades"] is not JsonObject upgrades)
        {
            upgrades = new JsonObject();
            node["upgrades"] = upgrades;
        }
        upgrades[result.Target] = JsonSerializer.SerializeToNode(result);
        return node.ToJsonString();
    }

    /// <summary>Tanlangan yaxshilashlar → takrorlash kartalari (old: o'quvchi iborasi, orqa: yaxshi variant).</summary>
    public static List<NewCard> Cards(UpgradeResult result, IEnumerable<int> indices) =>
        ReviewCardFactory.FromCorrections(indices
            .Where(i => i >= 0 && i < result.Items.Count)
            .Distinct()
            .Select(i => result.Items[i])
            .Select(i => new CorrectionItem(i.Original, i.Upgraded, i.Why)));

    private static string Str(JsonElement e, string name) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";

    private static string Norm(string s) => WhitespaceRegex().Replace(s.ToLowerInvariant().Replace('’', '\'').Trim(' ', '.', ',', '!', '?', '"'), " ");

    private static string Cut(string s, int max) => s.Length > max ? s[..max] : s;

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex(@"[\p{L}\p{M}'’]+")]
    private static partial Regex WordRegex();
}

public interface IAnswerUpgrader
{
    Task<UpgradeResult> UpgradeAsync(UpgradeSource source, string level, string target, string lang, CancellationToken ct = default);
}

/// <summary>Bitta Gemini so'rovi (faqat matn, audio yo'q — arzon): qayta yozilgan javob va eng foydali yaxshilashlar.</summary>
public class GeminiAnswerUpgrader(IGeminiClient gemini) : IAnswerUpgrader
{
    public static string BuildPrompt(UpgradeSource s, string level, string target, string lang)
    {
        var style = s.Kind == "writing"
            ? "It is a piece of writing: keep the same genre and paragraphing, and keep the length within about 30% of the original."
            : "It is a transcript of speech: rewrite it as natural SPOKEN English the learner could actually say (no fillers like \"um\", no written-only style), about the same length.";
        return $$"""
            You are an English teacher for Uzbek- and Russian-speaking learners. The learner's current level is {{LearnerLevel.Describe(level)}}.
            Task / topic: {{s.Topic}}
            {{style}}

            Rewrite the learner's answer below as it would sound at {{LearnerLevel.Describe(target)}} level.
            - Keep THEIR ideas, facts and opinions; do not add new arguments or invent personal details.
            - Use vocabulary, grammar and linking that are typical for {{target}}, not far above it.
            Then list the {{AnswerUpgradeRules.MaxItems}} or fewer most useful upgrades. For each upgrade:
            - "original": an EXACT quote copied from the learner's answer (a short phrase or one sentence);
            - "upgraded": how that part is said in your rewrite;
            - "kind": one of vocabulary, grammar, linking, structure;
            - "why": one short sentence in {{GeminiIeltsEvaluator.FeedbackLanguage(lang)}} explaining what makes it better.
            "summary": one or two sentences in {{GeminiIeltsEvaluator.FeedbackLanguage(lang)}} on what changed most.

            The learner's answer:
            <<<
            {{s.Text}}
            >>>

            Return ONLY valid JSON, no markdown fences, in exactly this shape:
            {"target": "{{target}}", "upgradedText": "<the rewrite>", "items": [{"original": "...", "upgraded": "...", "kind": "...", "why": "..."}], "summary": "..."}
            """;
    }

    public async Task<UpgradeResult> UpgradeAsync(UpgradeSource source, string level, string target, string lang, CancellationToken ct = default)
    {
        var body = new
        {
            contents = new[] { new { parts = new object[] { new { text = BuildPrompt(source, level, target, lang) } } } },
            generationConfig = new { temperature = 0.4, responseMimeType = "application/json" },
        };
        var response = await gemini.SendWithFallbackAsync(body, ct);
        var text = await GeminiJson.ExtractTextAsync(response, ct);
        var r = GeminiJson.DeserializeStrict<UpgradeResult>(text);
        var clean = AnswerUpgradeRules.Sanitize(r, source.Text, target);
        if (clean.UpgradedText.Length == 0) throw new InvalidOperationException("Gemini bo'sh qayta yozilgan matn qaytardi");
        return clean;
    }
}
