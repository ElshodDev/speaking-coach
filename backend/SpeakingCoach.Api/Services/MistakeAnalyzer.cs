using System.Text.Json;
using System.Text.RegularExpressions;

namespace SpeakingCoach.Api.Services;

/// <summary>Bitta tuzatish (Speaking/Writing/mock/suhbat natijasidan) va qachon qilingani.</summary>
public record MistakeFact(string Original, string Corrected, string Explanation, DateTime AtUtc);

public record MistakeExample(string Original, string Corrected, string Explanation, DateTime AtUtc);

/// <summary>Bitta xato turi: jami, oxirgi 30 kun, undan oldingi 30 kun va eng yangi misollar.</summary>
public record MistakeCategory(string Id, int Count, int Last30, int Prev30, List<MistakeExample> Examples);

public record MistakeReport(int Total, int Last30, List<MistakeCategory> Categories);

/// <summary>
/// "Mening xatolarim": AI bergan tuzatishlarni xato turlariga ajratadi (artikl,
/// predlog, zamon…). AI'siz — asl va tuzatilgan matn farqi hamda izohdagi kalit
/// so'zlar bo'yicha. Toza funksiyalar, unit testlangan. Aniq bo'lmagan holat —
/// "word_choice" yoki "other"; xato turini "to'qib chiqarishdan" ko'ra shu yaxshi.
/// </summary>
public static partial class MistakeAnalyzer
{
    public const string Articles = "articles";
    public const string Prepositions = "prepositions";
    public const string Agreement = "agreement";
    public const string Tense = "tense";
    public const string Plural = "plural";
    public const string Comparatives = "comparatives";
    public const string WordOrder = "word_order";
    public const string Spelling = "spelling";
    public const string WordChoice = "word_choice";
    public const string Other = "other";

    public static readonly string[] All = [Articles, Prepositions, Agreement, Tense, Plural, Comparatives, WordOrder, Spelling, WordChoice, Other];

    private static readonly HashSet<string> ArticleWords = ["a", "an", "the"];
    private static readonly HashSet<string> PrepositionWords =
        ["in", "on", "at", "to", "for", "of", "with", "about", "from", "by", "into", "onto", "during", "since", "until", "till", "towards", "toward", "among", "between", "through", "without", "under", "over", "across", "after", "before"];
    private static readonly HashSet<string> AgreementWords = ["is", "are", "was", "were", "has", "have", "does", "do", "doesn't", "don't", "isn't", "aren't", "wasn't", "weren't", "hasn't", "haven't"];
    private static readonly HashSet<string> Auxiliaries = ["will", "would", "have", "has", "had", "been", "did", "was", "were", "am", "going", "won't", "didn't", "shall", "be"];
    private static readonly HashSet<string> PluralCues =
        ["many", "these", "those", "several", "few", "both", "all", "some", "various", "different", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten", "hundreds", "thousands", "lots", "number", "most", "other", "such"];
    private static readonly HashSet<string> ComparativeWords = ["more", "most", "than", "less", "least", "better", "best", "worse", "worst"];

    /// <summary>Kuchli fe'llarning shakllari (bir oila — zamon xatosi).</summary>
    private static readonly string[][] IrregularVerbs =
    [
        ["go", "went", "gone"], ["be", "was", "were", "been"], ["have", "had"], ["do", "did", "done"], ["see", "saw", "seen"],
        ["come", "came"], ["take", "took", "taken"], ["make", "made"], ["get", "got", "gotten"], ["give", "gave", "given"],
        ["know", "knew", "known"], ["think", "thought"], ["buy", "bought"], ["bring", "brought"], ["teach", "taught"],
        ["write", "wrote", "written"], ["speak", "spoke", "spoken"], ["eat", "ate", "eaten"], ["drink", "drank", "drunk"],
        ["begin", "began", "begun"], ["find", "found"], ["tell", "told"], ["say", "said"], ["feel", "felt"], ["leave", "left"],
        ["meet", "met"], ["pay", "paid"], ["run", "ran"], ["sit", "sat"], ["stand", "stood"], ["understand", "understood"],
        ["win", "won"], ["lose", "lost"], ["spend", "spent"], ["send", "sent"], ["build", "built"], ["choose", "chose", "chosen"],
        ["forget", "forgot", "forgotten"], ["grow", "grew", "grown"], ["become", "became"], ["keep", "kept"], ["sleep", "slept"],
        ["swim", "swam", "swum"], ["sing", "sang", "sung"], ["fly", "flew", "flown"], ["drive", "drove", "driven"], ["ride", "rode", "ridden"],
        ["break", "broke", "broken"], ["wear", "wore", "worn"], ["hear", "heard"], ["catch", "caught"], ["fall", "fell", "fallen"],
    ];

    /// <summary>Izohdagi kalit so'zlar (ingliz, o'zbek, rus tillarida) — birinchi mos kelgani.</summary>
    private static readonly (string Category, string[] Words)[] Keywords =
    [
        (WordOrder, ["word order", "so'z tartib", "soʻz tartib", "порядок слов"]),
        (Articles, ["article", "artikl", "артикл"]),
        (Prepositions, ["preposition", "predlog", "предлог"]),
        (Agreement, ["third person", "3rd person", "he/she/it", "subject-verb", "agreement", "uchinchi shaxs", "3-shaxs", "третьего лица", "3-го лица", "согласован"]),
        (Plural, ["plural", "ko'plik", "koʻplik", "множествен"]),
        (Comparatives, ["comparative", "superlative", "qiyosiy", "orttirma", "сравнительн", "превосходн"]),
        (Tense, ["tense", "zamon", "время", "времени", "past simple", "present perfect", "past participle"]),
        (Spelling, ["spelling", "spelled", "spelt", "imlo", "орфограф", "пишется"]),
    ];

    [GeneratedRegex(@"[a-z]+(?:'[a-z]+)?")]
    private static partial Regex WordRx();

    public static List<string> Tokens(string? text) =>
        WordRx().Matches((text ?? "").ToLowerInvariant().Replace('’', '\'')).Select(m => m.Value).ToList();

    public static string Classify(string? original, string? corrected, string? explanation)
    {
        var o = Tokens(original);
        var c = Tokens(corrected);

        var expl = (explanation ?? "").ToLowerInvariant();
        foreach (var (category, words) in Keywords)
            if (words.Any(expl.Contains)) return category;

        if (o.Count > 1 && o.Count == c.Count && !o.SequenceEqual(c) && o.OrderBy(x => x).SequenceEqual(c.OrderBy(x => x)))
            return WordOrder;

        var (removed, added) = Diff(o, c);
        var changed = removed.Concat(added).ToList();
        if (changed.Count == 0) return Other;

        if (changed.All(ArticleWords.Contains)) return Articles;
        if (changed.All(PrepositionWords.Contains)) return Prepositions;
        if (removed.Count == added.Count && changed.All(AgreementWords.Contains)) return Agreement;
        if (changed.Any(ComparativeWords.Contains) && !changed.All(Auxiliaries.Contains)) return Comparatives;

        if (removed.Count == 1 && added.Count == 1)
        {
            var r = removed[0];
            var a = added[0];
            if (IsSSuffix(r, a))
            {
                var index = c.IndexOf(a);
                var before = index > 0 ? c[index - 1] : "";
                var beforeOriginal = o.IndexOf(r) is var ri and > 0 ? o[ri - 1] : "";
                return PluralCues.Contains(before) || PluralCues.Contains(beforeOriginal) || before.All(char.IsDigit) && before.Length > 0
                    ? Plural
                    : Agreement;
            }
            if (SameVerb(r, a)) return Tense;
            if (a.EndsWith("er") || a.EndsWith("est")) return Comparatives;
            if (r.Length >= 4 && a.Length >= 4 && EditDistance(r, a) <= 2) return Spelling;
            return WordChoice;
        }

        if (changed.Any(Auxiliaries.Contains) || removed.Zip(added).Any(p => SameVerb(p.First, p.Second))) return Tense;
        if (changed.Any(ArticleWords.Contains) && changed.All(t => ArticleWords.Contains(t) || PrepositionWords.Contains(t)))
            return changed.Count(ArticleWords.Contains) >= changed.Count(PrepositionWords.Contains) ? Articles : Prepositions;
        return Other;
    }

    private static bool IsSSuffix(string r, string a) =>
        a == r + "s" || a == r + "es" || r == a + "s" || r == a + "es"
        || (r.EndsWith('y') && a == r[..^1] + "ies") || (a.EndsWith('y') && r == a[..^1] + "ies");

    /// <summary>Bir fe'lning ikki shakli (go/went, work/worked, play/playing) — zamon yoki shakl xatosi.</summary>
    public static bool SameVerb(string x, string y)
    {
        if (x == y) return false;
        if (IrregularVerbs.Any(f => f.Contains(x) && f.Contains(y))) return true;
        var sx = Stem(x);
        var sy = Stem(y);
        return sx.Length >= 3 && sx == sy && (x.EndsWith("ed") || y.EndsWith("ed") || x.EndsWith("ing") || y.EndsWith("ing"));
    }

    private static string Stem(string w)
    {
        if (w.EndsWith("ing") && w.Length > 5) w = w[..^3];
        else if (w.EndsWith("ied") && w.Length > 4) w = w[..^3] + "y";
        else if (w.EndsWith("ed") && w.Length > 4) w = w[..^2];
        else if (w.EndsWith("es") && w.Length > 4) w = w[..^2];
        else if (w.EndsWith('s') && w.Length > 3) w = w[..^1];
        // "stopped" → "stop", "making" → "mak(e)": oxirgi takror/"e" ni olib tashlaymiz.
        if (w.Length > 3 && w[^1] == w[^2]) w = w[..^1];
        return w.TrimEnd('e');
    }

    /// <summary>Eng uzun umumiy ketma-ketlik bo'yicha farq: olib tashlangan va qo'shilgan so'zlar.</summary>
    public static (List<string> Removed, List<string> Added) Diff(IReadOnlyList<string> a, IReadOnlyList<string> b)
    {
        var n = a.Count;
        var m = b.Count;
        var lcs = new int[n + 1, m + 1];
        for (var i = n - 1; i >= 0; i--)
            for (var j = m - 1; j >= 0; j--)
                lcs[i, j] = a[i] == b[j] ? lcs[i + 1, j + 1] + 1 : Math.Max(lcs[i + 1, j], lcs[i, j + 1]);
        var removed = new List<string>();
        var added = new List<string>();
        int x = 0, y = 0;
        while (x < n && y < m)
        {
            if (a[x] == b[y]) { x++; y++; }
            else if (lcs[x + 1, y] >= lcs[x, y + 1]) removed.Add(a[x++]);
            else added.Add(b[y++]);
        }
        while (x < n) removed.Add(a[x++]);
        while (y < m) added.Add(b[y++]);
        return (removed, added);
    }

    public static int EditDistance(string s, string t)
    {
        var d = new int[s.Length + 1, t.Length + 1];
        for (var i = 0; i <= s.Length; i++) d[i, 0] = i;
        for (var j = 0; j <= t.Length; j++) d[0, j] = j;
        for (var i = 1; i <= s.Length; i++)
            for (var j = 1; j <= t.Length; j++)
                d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + (s[i - 1] == t[j - 1] ? 0 : 1));
        return d[s.Length, t.Length];
    }

    /// <summary>Faoliyat natijasidan (ResponseData) "topCorrections" ro'yxati; shakli boshqacha bo'lsa — bo'sh.</summary>
    public static IEnumerable<MistakeFact> FromResponse(string json, DateTime atUtc)
    {
        List<MistakeFact> list = [];
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object
                || !doc.RootElement.TryGetProperty("topCorrections", out var arr)
                || arr.ValueKind != JsonValueKind.Array) return list;
            foreach (var item in arr.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object) continue;
                string Get(string name) => item.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString()!.Trim() : "";
                var original = Get("original");
                var corrected = Get("corrected");
                if (original.Length == 0 || corrected.Length == 0 || string.Equals(original, corrected, StringComparison.OrdinalIgnoreCase)) continue;
                list.Add(new MistakeFact(original, corrected, Get("explanation"), atUtc));
            }
        }
        catch (JsonException)
        {
            // eski yoki buzilgan yozuv — o'tkazib yuboramiz
        }
        return list;
    }

    public static MistakeReport Summarize(IEnumerable<MistakeFact> facts, DateTime nowUtc, int examplesPerCategory = 3)
    {
        var list = facts.ToList();
        var since30 = nowUtc.AddDays(-30);
        var since60 = nowUtc.AddDays(-60);
        var categories = list
            .GroupBy(f => Classify(f.Original, f.Corrected, f.Explanation))
            .Select(g => new MistakeCategory(
                g.Key,
                g.Count(),
                g.Count(f => f.AtUtc >= since30),
                g.Count(f => f.AtUtc >= since60 && f.AtUtc < since30),
                g.OrderByDescending(f => f.AtUtc)
                    .DistinctBy(f => f.Original.ToLowerInvariant())
                    .Take(examplesPerCategory)
                    .Select(f => new MistakeExample(f.Original, f.Corrected, f.Explanation, f.AtUtc))
                    .ToList()))
            // "Boshqa" va "so'z tanlash" ro'yxat oxirida — ular aniq grammatik mavzu emas.
            .OrderBy(c => c.Id is Other or WordChoice ? 1 : 0)
            .ThenByDescending(c => c.Count)
            .ThenBy(c => Array.IndexOf(All, c.Id))
            .ToList();
        return new MistakeReport(list.Count, list.Count(f => f.AtUtc >= since30), categories);
    }
}
