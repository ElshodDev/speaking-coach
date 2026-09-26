using System.Globalization;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace SpeakingCoach.Api.Services.Mock;

// ---- Listening / Reading testi tuzilishi (AI yaratadi, server tekshiradi) ----
//
// Savol turlari (ielts.org ro'yxatidagi eng keng tarqalganlari):
//   "mcq"  — variantli (A, B, C, D), bitta javob;
//   "tfng" — TRUE / FALSE / NOT GIVEN (Reading: ma'lumotni aniqlash);
//   "ynng" — YES / NO / NOT GIVEN (Reading: muallif fikri);
//   "gap"  — bo'sh joyni to'ldirish (forma/eslatma/gap), so'z chegarasi bilan.

public record ObjQuestion(
    [property: JsonPropertyName("number")] int Number,
    [property: JsonPropertyName("prompt")] string Prompt,
    [property: JsonPropertyName("options")] List<string>? Options,
    [property: JsonPropertyName("answers")] List<string> Answers,
    [property: JsonPropertyName("explanation")] string Explanation);

public record QuestionGroup(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("instructions")] string Instructions,
    [property: JsonPropertyName("maxWords")] int? MaxWords,
    [property: JsonPropertyName("questions")] List<ObjQuestion> Questions);

public record ReadingPassage(
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("text")] string Text,
    [property: JsonPropertyName("groups")] List<QuestionGroup> Groups);

public record ScriptLine(
    [property: JsonPropertyName("speaker")] string Speaker,
    [property: JsonPropertyName("voice")] string Voice,
    [property: JsonPropertyName("text")] string Text);

public record ListeningPart(
    [property: JsonPropertyName("part")] int Part,
    [property: JsonPropertyName("context")] string Context,
    [property: JsonPropertyName("script")] List<ScriptLine> Script,
    [property: JsonPropertyName("groups")] List<QuestionGroup> Groups);

public record ReadingTest(
    [property: JsonPropertyName("variant")] string Variant,
    [property: JsonPropertyName("passages")] List<ReadingPassage> Passages);

public record ListeningTest(
    [property: JsonPropertyName("parts")] List<ListeningPart> Parts);

// ---- Natija ----

public record QuestionReview(int Number, string Given, List<string> Accepted, bool Correct, string Explanation);

public record ObjectiveResult(
    string Exam, string Module, string TestId, string Variant, int Score, int Total, decimal Overall, int SecondsUsed,
    List<QuestionReview> Questions);

public static partial class ObjectiveGrading
{
    public static readonly string[] TfngAnswers = ["TRUE", "FALSE", "NOT GIVEN"];
    public static readonly string[] YnngAnswers = ["YES", "NO", "NOT GIVEN"];

    /// <summary>Taqqoslash uchun: kichik harf, ortiqcha bo'shliq va chetdagi tinish belgilarisiz.</summary>
    public static string Normalize(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        var t = s.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormKC);
        t = t.Replace('’', '\'');
        t = Spaces().Replace(t, " ");
        return t.Trim(' ', '.', ',', ';', ':', '!', '?', '"', '\'', '(', ')');
    }

    public static int WordCount(string s) => Normalize(s).Length == 0 ? 0 : Normalize(s).Split(' ').Length;

    /// <summary>
    /// Bitta javobni tekshiradi. "gap" turida so'z chegarasidan oshgan javob —
    /// noto'g'ri (IELTS qoidasi: "NO MORE THAN TWO WORDS" bo'lsa, uch so'z xato).
    /// </summary>
    public static bool IsCorrect(QuestionGroup group, ObjQuestion q, string? given)
    {
        var g = Normalize(given);
        if (g.Length == 0) return false;
        if (group.Type == "gap" && group.MaxWords is int max && WordCount(g) > max) return false;
        return q.Answers.Any(a => Normalize(a) == g);
    }

    public static IEnumerable<(QuestionGroup Group, ObjQuestion Q)> AllQuestions(IEnumerable<QuestionGroup> groups) =>
        groups.SelectMany(g => g.Questions.Select(q => (g, q)));

    public static List<QuestionReview> Grade(IEnumerable<QuestionGroup> groups, IReadOnlyDictionary<string, string> answers) =>
        AllQuestions(groups)
            .OrderBy(x => x.Q.Number)
            .Select(x =>
            {
                answers.TryGetValue(x.Q.Number.ToString(CultureInfo.InvariantCulture), out var given);
                return new QuestionReview(x.Q.Number, given ?? "", x.Q.Answers, IsCorrect(x.Group, x.Q, given), x.Q.Explanation);
            })
            .ToList();

    // ---- Xom ball → band ----
    //
    // ielts.org ("IELTS scoring in detail") aniq jadval bermaydi — faqat
    // o'rtacha nuqtalar: Listening va Academic Reading: 16→5, 23→6, 30→7,
    // 35→8; General Training Reading: 15→4, 23→5, 30→6, 35→7. Shu
    // nuqtalar orasini chiziqli to'ldiramiz (0→0, 40→9 — shkala chetlari)
    // va rasmiy .25/.75 qoidasi bilan yarim band'gacha yaxlitlaymiz.
    // "Aniq raqamlar test versiyasiga qarab biroz farq qiladi" (ielts.org),
    // shuning uchun natija — TAXMINIY.

    public static readonly (int Raw, decimal Band)[] ListeningAnchors = [(0, 0m), (16, 5m), (23, 6m), (30, 7m), (35, 8m), (40, 9m)];
    public static readonly (int Raw, decimal Band)[] AcademicReadingAnchors = ListeningAnchors;
    public static readonly (int Raw, decimal Band)[] GeneralReadingAnchors = [(0, 0m), (15, 4m), (23, 5m), (30, 6m), (35, 7m), (40, 9m)];

    public static decimal BandFor(int raw, (int Raw, decimal Band)[] anchors, int total = 40)
    {
        // Savollar soni 40 dan farq qilsa (masalan AI kam savol bergan), 40 ga moslaymiz.
        var scaled = total == 40 ? raw : (decimal)raw * 40 / Math.Max(1, total);
        scaled = Math.Clamp(scaled, 0, 40);
        for (var i = 1; i < anchors.Length; i++)
        {
            var (r0, b0) = anchors[i - 1];
            var (r1, b1) = anchors[i];
            if (scaled <= r1)
            {
                var band = b0 + (b1 - b0) * (scaled - r0) / (r1 - r0);
                return IeltsBand.RoundHalfBand(band);
            }
        }
        return 9m;
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Spaces();
}

/// <summary>
/// AI yaratgan testni saqlashdan OLDIN qat'iy tekshirish: savollar soni va
/// raqamlari, javob turlari, variantlar, va "gap" javobi haqiqatan matnda
/// bormi (bo'lmasa — talaba uni topa olmaydi). Xato bo'lsa — test bankka
/// tushmaydi, qayta yaratiladi.
/// </summary>
public static class ObjectiveValidator
{
    public static void ValidateGroups(List<QuestionGroup> groups, int firstNumber, int count, string sourceText, bool allowYesNo)
    {
        var all = ObjectiveGrading.AllQuestions(groups).Select(x => x.Q.Number).ToList();
        var expected = Enumerable.Range(firstNumber, count).ToList();
        if (!all.SequenceEqual(expected))
            throw new InvalidOperationException($"Savol raqamlari noto'g'ri: kutilgan {firstNumber}-{firstNumber + count - 1}, keldi [{string.Join(",", all)}]");

        var normalizedSource = ObjectiveGrading.Normalize(sourceText);
        foreach (var g in groups)
        {
            if (string.IsNullOrWhiteSpace(g.Instructions)) throw new InvalidOperationException("Ko'rsatma bo'sh");
            foreach (var q in g.Questions)
            {
                if (string.IsNullOrWhiteSpace(q.Prompt) && g.Type != "tfng" && g.Type != "ynng")
                    throw new InvalidOperationException($"{q.Number}-savol matni bo'sh");
                if (q.Answers.Count == 0 || q.Answers.Any(string.IsNullOrWhiteSpace))
                    throw new InvalidOperationException($"{q.Number}-savolda javob yo'q");
                switch (g.Type)
                {
                    case "mcq":
                        if (q.Options is not { Count: >= 3 and <= 5 })
                            throw new InvalidOperationException($"{q.Number}-savolda variantlar 3-5 ta bo'lishi kerak");
                        var letters = Enumerable.Range(0, q.Options.Count).Select(i => ((char)('A' + i)).ToString()).ToList();
                        if (q.Answers.Count != 1 || !letters.Contains(q.Answers[0].Trim().ToUpperInvariant()))
                            throw new InvalidOperationException($"{q.Number}-savol javobi variant harfi emas: {string.Join(",", q.Answers)}");
                        break;
                    case "tfng":
                        if (q.Answers.Count != 1 || !ObjectiveGrading.TfngAnswers.Contains(q.Answers[0].Trim().ToUpperInvariant()))
                            throw new InvalidOperationException($"{q.Number}-savol javobi TRUE/FALSE/NOT GIVEN emas");
                        break;
                    case "ynng" when allowYesNo:
                        if (q.Answers.Count != 1 || !ObjectiveGrading.YnngAnswers.Contains(q.Answers[0].Trim().ToUpperInvariant()))
                            throw new InvalidOperationException($"{q.Number}-savol javobi YES/NO/NOT GIVEN emas");
                        break;
                    case "gap":
                        if (g.MaxWords is not (>= 1 and <= 3)) throw new InvalidOperationException("gap: maxWords 1-3 bo'lishi kerak");
                        if (!q.Prompt.Contains("___")) throw new InvalidOperationException($"{q.Number}-savolda bo'sh joy (___) yo'q");
                        foreach (var a in q.Answers)
                        {
                            if (ObjectiveGrading.WordCount(a) > g.MaxWords)
                                throw new InvalidOperationException($"{q.Number}-savol javobi so'z chegarasidan uzun: {a}");
                        }
                        if (!q.Answers.Any(a => normalizedSource.Contains(ObjectiveGrading.Normalize(a))))
                            throw new InvalidOperationException($"{q.Number}-savol javobi matnda yo'q: {q.Answers[0]}");
                        break;
                    default:
                        throw new InvalidOperationException($"Noma'lum savol turi: {g.Type}");
                }
            }
        }
    }

    public static int Words(string text) => text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
}

/// <summary>Brauzerga yuboriladigan nusxa — to'g'ri javoblar va izohlarsiz.</summary>
public static class ClientView
{
    public record Q(int Number, string Prompt, List<string>? Options);
    public record G(string Type, string Instructions, int? MaxWords, List<Q> Questions);

    public static List<G> Groups(IEnumerable<QuestionGroup> groups) =>
        groups.Select(g => new G(g.Type, g.Instructions, g.MaxWords,
            g.Questions.Select(q => new Q(q.Number, q.Prompt, q.Options)).ToList())).ToList();

    public static object Reading(Guid id, ReadingTest t) => new
    {
        id,
        variant = t.Variant,
        passages = t.Passages.Select(p => new { p.Title, p.Text, groups = Groups(p.Groups) }),
    };

    /// <summary>
    /// Listening skripti brauzerga yuboriladi — ovozni brauzer o'zi o'qiydi
    /// (bepul TTS). Skript sahifada imtihon tugamaguncha KO'RSATILMAYDI;
    /// to'g'ri javoblar ro'yxati esa umuman yuborilmaydi.
    /// </summary>
    public static object Listening(Guid id, ListeningTest t) => new
    {
        id,
        parts = t.Parts.Select(p => new { p.Part, p.Context, p.Script, groups = Groups(p.Groups) }),
    };
}
