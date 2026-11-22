namespace SpeakingCoach.Domain.Rules;

/// <summary>AI ustozga tavsiya: xato turi, mavzu, nechta xato (jami va oxirgi 30 kunda) va eng yangi misol.</summary>
public record TopicRecommendation(string Category, string Topic, int Count, int Last30, MistakeExample? Example);

/// <summary>
/// "Xatolaringiz asosida sizga shu mavzu kerak": xato turlarini (MistakeAnalyzer) AI ustoz mavzulariga bog'laydi.
/// AI'siz — oddiy qoidalar, toza funksiyalar (unit testlangan). Aniq grammatik mavzusi yo'q turlar
/// (imlo, so'z tanlash, boshqa) tavsiya qilinmaydi: mavzuni "to'qib chiqarishdan" ko'ra shu yaxshi.
/// Mavzular ustoz vazifasiga ham sig'adi (≤ 33 belgi).
/// </summary>
public static class TopicAdvisor
{
    /// <summary>Tavsiya uchun kamida shuncha xato kerak (bitta tasodifiy xatoga mavzu tavsiya qilinmasin).</summary>
    public const int MinMistakes = 2;

    public const int DefaultMax = 3;

    /// <summary>Xato turi → ingliz tilidagi mavzu nomi (AI ustoz va ustoz vazifasi uchun).</summary>
    public static readonly IReadOnlyDictionary<string, string> TopicOf = new Dictionary<string, string>
    {
        [MistakeAnalyzer.Articles] = "Articles (a/an/the)",
        [MistakeAnalyzer.Prepositions] = "Prepositions of time and place",
        [MistakeAnalyzer.Agreement] = "Subject-verb agreement",
        [MistakeAnalyzer.Tense] = "Past Simple vs Present Perfect",
        [MistakeAnalyzer.Plural] = "Countable and uncountable nouns",
        [MistakeAnalyzer.Comparatives] = "Comparatives and superlatives",
        [MistakeAnalyzer.WordOrder] = "English word order",
    };

    /// <summary>
    /// Eng ko'p (avval oxirgi 30 kunda) takrorlangan turlar. <paramref name="mastered"/> — yaqinda yaxshi o'zlashtirilgan mavzular
    /// (qayta tavsiya qilinmaydi; registr farqi hisobga olinmaydi).
    /// </summary>
    public static List<TopicRecommendation> Recommend(MistakeReport report, IReadOnlyCollection<string>? mastered = null, int max = DefaultMax)
    {
        var skip = new HashSet<string>(mastered ?? [], StringComparer.OrdinalIgnoreCase);
        return report.Categories
            .Where(c => TopicOf.ContainsKey(c.Id) && c.Count >= MinMistakes && !skip.Contains(TopicOf[c.Id]))
            .OrderByDescending(c => c.Last30)
            .ThenByDescending(c => c.Count)
            .ThenBy(c => Array.IndexOf(MistakeAnalyzer.All, c.Id))
            .Take(Math.Max(0, max))
            .Select(c => new TopicRecommendation(c.Id, TopicOf[c.Id], c.Count, c.Last30, c.Examples.FirstOrDefault()))
            .ToList();
    }
}
