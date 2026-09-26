using System.Text.Json;

namespace SpeakingCoach.Api.Services.Mock;

public interface IMockGenerator
{
    Task<ReadingTest> GenerateReadingAsync(string variant, CancellationToken ct = default);
    Task<ListeningTest> GenerateListeningAsync(CancellationToken ct = default);
    Task<ReadingTest> GenerateCefrReadingAsync(CancellationToken ct = default);
    Task<ListeningTest> GenerateCefrListeningAsync(CancellationToken ct = default);
}

/// <summary>
/// IELTS Listening/Reading testlarini Gemini bilan yaratadi. Har bir matn
/// (passage) yoki qism (part) — alohida so'rov, parallel: bitta ulkan
/// so'rovdan ko'ra tezroq va ishonchliroq. Har bir bo'lak qat'iy
/// tekshiriladi (ObjectiveValidator); o'tmasa — bir marta qayta yaratiladi.
/// </summary>
public partial class GeminiMockGenerator(GeminiClient gemini, ILogger<GeminiMockGenerator> logger) : IMockGenerator
{
    private static readonly string[] AcademicTopics =
    [
        "the history of glass making", "how bees communicate", "urban heat islands", "the invention of the printing press",
        "coral reef restoration", "the psychology of habits", "ancient water management systems", "the economics of fast fashion",
        "sleep and memory research", "the origins of writing systems", "deep-sea mining", "language learning in early childhood",
        "the rise of vertical farming", "migration of monarch butterflies", "noise pollution and health", "the Silk Road trade network",
    ];

    private static readonly string[] GeneralTopics =
    [
        "courses and events at a community centre", "rules and services of a public swimming pool", "choosing a mobile phone plan",
        "health and safety guidance for new warehouse staff", "applying for annual leave in a large company", "a guide for new employees at a hotel",
    ];

    private static readonly string[] ListeningSituations =
    [
        "booking a room at a guest house", "joining a sports club", "renting a bicycle for a week", "enrolling in an evening language course",
        "reporting a lost bag at a train station", "arranging a home delivery of furniture", "registering at a local library",
    ];

    private static readonly string[] ListeningTalks =
    [
        "a guided tour of a botanical garden", "a radio talk about a city festival", "an introduction for new volunteers at a museum",
        "information about changes to local bus services", "a talk about a community recycling scheme",
    ];

    private const string JsonShape = """
        Question groups use exactly this JSON shape:
        {"type": "mcq" | "tfng" | "ynng" | "gap",
         "instructions": "<official-style instructions, e.g. Choose the correct letter, A, B, C or D. / Write NO MORE THAN TWO WORDS for each answer.>",
         "maxWords": <for gap: 1, 2 or 3; otherwise null>,
         "questions": [{"number": <int>, "prompt": "<question; for gap it must contain ___ where the answer goes; for tfng/ynng a statement>",
                        "options": ["<option text without letter>", ...] or null (only mcq has 4 options),
                        "answers": ["<correct answer>", "<accepted alternative spelling, optional>"],
                        "explanation": "<one short sentence quoting the exact words that prove the answer>"}]}
        Rules: mcq answers are a single letter (A-D). tfng answers are TRUE, FALSE or NOT GIVEN. ynng answers are YES, NO or NOT GIVEN.
        gap answers must be copied EXACTLY from the text (same spelling) and respect maxWords; include number alternatives such as "15" and "fifteen" when relevant.
        Questions within a group follow the order of the information in the text. Every question must have exactly one correct answer.
        Return ONLY valid JSON, no markdown fences.
        """;

    public static string ReadingPrompt(string variant, int passageNo, int first, int count, string topic)
    {
        var (kind, types) = (variant, passageNo) switch
        {
            ("general", 1) => ("two or three short everyday texts (for example notices, advertisements or a timetable) combined into one passage with short headings", "tfng and gap"),
            ("general", 2) => ("a workplace text (for example staff guidelines, a job description or training information)", "mcq and gap"),
            ("general", _) => ("a longer general-interest article", "ynng or tfng, and mcq"),
            (_, 1) => ("an academic-style article for a general educated reader", "tfng and gap"),
            (_, 2) => ("an academic-style article for a general educated reader", "mcq and gap"),
            _ => ("an academic-style article that presents the writer's views and arguments", "ynng and mcq"),
        };
        var variantName = variant == "general" ? "General Training" : "Academic";
        return $"""
            You are an experienced IELTS item writer. Create Reading Passage {passageNo} of an original IELTS {variantName} Reading mock test.
            The passage is {kind}, topic: {topic}. Length: 750-900 words. Use a clear title. Write original text; do not copy published tests.
            Then write exactly {count} questions numbered {first} to {first + count - 1}, split into 2 or 3 groups using the types {types}.
            Difficulty should rise slightly from the first to the last question, matching real IELTS level (bands 5-8).
            Return: {"{"}"title": "...", "text": "<passage; separate paragraphs with a blank line>", "groups": [<question groups>]{"}"}
            {JsonShape}
            """;
    }

    public static string ListeningPrompt(int part, int first, string topic)
    {
        var (setting, speakers, types) = part switch
        {
            1 => ($"a conversation between two people in an everyday social situation: {topic}",
                  "two speakers (one male, one female), e.g. a customer and an employee", "one gap group (form or note completion, maxWords 1 or 2), 10 questions"),
            2 => ($"a monologue in an everyday social context: {topic}", "one main speaker",
                  "an mcq group (4-5 questions) and a gap group (5-6 questions, maxWords 2)"),
            3 => ($"a conversation between two students (and possibly a tutor) about an academic assignment on: {topic}",
                  "two main speakers (one male, one female)", "an mcq group (5-6 questions) and a gap group (4-5 questions, maxWords 2)"),
            _ => ($"a university lecture on: {topic}", "one lecturer", "one gap group (note completion, maxWords 1), 10 questions"),
        };
        return $"""
            You are an experienced IELTS item writer. Create Part {part} of an original IELTS Listening mock test.
            The recording is {setting}, with {speakers}. Script length: 500-700 words of natural spoken English
            (include realistic features: a speaker correcting themselves, a spelled name or a number said aloud, distractors that are mentioned then rejected).
            Questions: exactly 10, numbered {first} to {first + 9}: {types}. Answers appear in the script in question order.
            Return: {"{"}"part": {part}, "context": "<one sentence the candidate reads before listening, e.g. You will hear a woman phoning a sports centre.>",
                     "script": [{"{"}"speaker": "<name or role>", "voice": "male" | "female", "text": "<what they say>"{"}"}],
                     "groups": [<question groups>]{"}"}
            {JsonShape}
            """;
    }

    private async Task<T> AskAsync<T>(string prompt, Action<T> validate, CancellationToken ct, int attempts = 2)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                var body = new
                {
                    contents = new[] { new { parts = new object[] { new { text = prompt } } } },
                    generationConfig = new { temperature = 0.7, responseMimeType = "application/json" },
                };
                var response = await gemini.SendWithFallbackAsync(body, ct);
                var result = GeminiClient.DeserializeStrict<T>(await GeminiClient.ExtractTextAsync(response, ct));
                validate(result);
                return result;
            }
            catch (InvalidOperationException ex) when (attempt < attempts)
            {
                logger.LogWarning("Mock bo'lagi yaroqsiz, qayta yaratilmoqda: {Error}", ex.Message);
            }
        }
    }

    public static void ValidatePassage(ReadingPassage p, int first, int count)
    {
        var words = ObjectiveValidator.Words(p.Text);
        if (words is < 550 or > 1200) throw new InvalidOperationException($"Matn uzunligi mos emas: {words} so'z");
        if (string.IsNullOrWhiteSpace(p.Title)) throw new InvalidOperationException("Sarlavha yo'q");
        ObjectiveValidator.ValidateGroups(p.Groups, first, count, p.Text, allowYesNo: true);
    }

    public static void ValidatePart(ListeningPart p, int part)
    {
        if (p.Part != part) throw new InvalidOperationException($"Qism raqami {p.Part}, kutilgan {part}");
        var text = string.Join(" ", p.Script.Select(l => l.Text));
        var words = ObjectiveValidator.Words(text);
        if (words is < 350 or > 1100) throw new InvalidOperationException($"Skript uzunligi mos emas: {words} so'z");
        if (p.Script.Any(l => l.Voice is not ("male" or "female"))) throw new InvalidOperationException("voice male/female bo'lishi kerak");
        if (p.Groups.Any(g => g.Type is "tfng" or "ynng")) throw new InvalidOperationException("Listening'da tfng/ynng bo'lmaydi");
        ObjectiveValidator.ValidateGroups(p.Groups, (part - 1) * 10 + 1, 10, text, allowYesNo: false);
    }

    public async Task<ReadingTest> GenerateReadingAsync(string variant, CancellationToken ct = default)
    {
        var academic = AcademicTopics.OrderBy(_ => Random.Shared.Next()).Take(3).ToArray();
        var general = GeneralTopics.OrderBy(_ => Random.Shared.Next()).Take(2).ToArray();
        // General Training: 1-2 bo'limlar — kundalik va ish matnlari, 3-bo'lim — umumiy maqola.
        var topics = variant == "general" ? [general[0], general[1], academic[0]] : academic;
        (int First, int Count)[] layout = [(1, 13), (14, 13), (27, 14)];
        var tasks = layout.Select((l, i) => AskAsync<ReadingPassage>(
            ReadingPrompt(variant, i + 1, l.First, l.Count, topics[i]),
            p => ValidatePassage(p, l.First, l.Count), ct));
        var passages = await Task.WhenAll(tasks);
        return new ReadingTest(variant, passages.ToList());
    }

    public async Task<ListeningTest> GenerateListeningAsync(CancellationToken ct = default)
    {
        var situation = ListeningSituations[Random.Shared.Next(ListeningSituations.Length)];
        var talk = ListeningTalks[Random.Shared.Next(ListeningTalks.Length)];
        var academic = AcademicTopics.OrderBy(_ => Random.Shared.Next()).Take(2).ToArray();
        string[] topics = [situation, talk, academic[0], academic[1]];
        var tasks = Enumerable.Range(1, 4).Select(part => AskAsync<ListeningPart>(
            ListeningPrompt(part, (part - 1) * 10 + 1, topics[part - 1]),
            p => ValidatePart(p, part), ct));
        var parts = await Task.WhenAll(tasks);
        return new ListeningTest(parts.ToList());
    }

    public static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);
}
