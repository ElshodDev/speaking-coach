using System.Text.Json.Serialization;
using SpeakingCoach.Api.Data;

namespace SpeakingCoach.Api.Services.Content;

public record SuggestedTopic(
    [property: JsonPropertyName("text")] string Text,
    [property: JsonPropertyName("points")] List<string>? Points);

public record GrammarQuizQuestion(
    [property: JsonPropertyName("prompt")] string Prompt,
    [property: JsonPropertyName("options")] List<string> Options,
    [property: JsonPropertyName("answer")] int Answer,
    [property: JsonPropertyName("why")] string Why);

public record GrammarQuiz([property: JsonPropertyName("questions")] List<GrammarQuizQuestion> Questions);

/// <summary>
/// Foydalanuvchi "✨ AI" ni tanlaganda: yangi mashq mavzusi yoki grammatika
/// darsiga qo'shimcha savollar. Tayyor materiallar (topics.ts, grammar.ts)
/// ilovaning o'zida; bu — ixtiyoriy qo'shimcha. Prompt va tekshiruv sof —
/// unit test bilan tekshiriladi.
/// </summary>
public class ContentAi(GeminiClient gemini)
{
    public static readonly string[] SpeakingCategories = ["everyday", "ielts-part1", "ielts-part2", "ielts-part3", "cefr"];
    public static readonly string[] WritingCategories = ["paragraph", "essay-opinion", "essay-discussion", "letter-informal", "letter-formal"];
    public const int MaxTopicChars = 300;
    public const int QuizSize = 6;

    private static readonly Dictionary<string, string> CategoryText = new()
    {
        ["everyday"] = "a simple everyday conversation prompt",
        ["ielts-part1"] = "an IELTS Speaking Part 1 set of two or three short personal questions on one theme",
        ["ielts-part2"] = "an IELTS Speaking Part 2 cue card starting with \"Describe\", plus exactly 3 or 4 \"You should say\" points (the last one starting with \"and explain\")",
        ["ielts-part3"] = "an IELTS Speaking Part 3 abstract discussion question",
        ["cefr"] = "an Uzbekistan CEFR Multilevel speaking question (personal question, comparison, or a for/against statement to discuss)",
        ["paragraph"] = "a one-paragraph writing task",
        ["essay-opinion"] = "an IELTS Writing Task 2 opinion (agree or disagree) essay question",
        ["essay-discussion"] = "an IELTS Writing Task 2 question asking to discuss both views, advantages and disadvantages, or problems and solutions",
        ["letter-informal"] = "an informal letter or email task to a friend, with the situation explained",
        ["letter-formal"] = "a formal letter task (request, complaint or application), with the situation explained",
    };

    public static bool ValidCategory(string kind, string? category) =>
        category is not null && (kind == "writing" ? WritingCategories : SpeakingCategories).Contains(category);

    public static string TopicPrompt(string kind, string category, string level, IReadOnlyCollection<string> avoid) =>
        $$"""
        You write original English practice tasks for learners in Uzbekistan.
        Write ONE new {{(kind == "writing" ? "writing" : "speaking")}} task: {{CategoryText[category]}}, suitable for a {{LearnerLevel.Describe(level)}} learner.
        Pick a varied, everyday or academic theme (study, work, technology, environment, health, travel, city life, culture, sport, money, media, science, food, family).
        Avoid politics, religion and sensitive topics. Do not copy real exam questions.{{(avoid.Count > 0 ? "\nDo not repeat these recent topics: " + string.Join(" | ", avoid.Take(8)) : "")}}
        The task text must be at most 250 characters.
        Return ONLY JSON: {"text": "<the task>", "points": [<"You should say" points, only for a cue card, otherwise an empty list>]}
        """;

    public static SuggestedTopic ValidateTopic(SuggestedTopic t, string category)
    {
        var text = (t.Text ?? "").Trim();
        if (text.Length < 10 || text.Length > MaxTopicChars) throw new InvalidOperationException($"Mavzu uzunligi mos emas: {text.Length}");
        var points = (t.Points ?? []).Select(p => p.Trim()).Where(p => p.Length > 0).ToList();
        if (category == "ielts-part2")
        {
            if (points.Count is < 3 or > 4 || points.Any(p => p.Length > 120)) throw new InvalidOperationException("Cue card punktlari 3-4 ta bo'lishi kerak");
        }
        else points = [];
        return new SuggestedTopic(text, points.Count == 0 ? null : points);
    }

    public static string QuizPrompt(string topic, string level, string lang)
    {
        var why = lang switch
        {
            "ru" => "in Russian",
            "en" => "in simple English",
            _ => "in Uzbek (Latin script, use the letters oʻ and gʻ)",
        };
        return $$"""
            You are an English grammar teacher. Write {{QuizSize}} NEW multiple-choice practice questions on the grammar topic "{{topic}}" for a {{LearnerLevel.Describe(level)}} learner.
            Each question is one natural English sentence with a gap written as ___ and 3 or 4 options (exactly one correct, the others typical learner mistakes).
            Vary the position of the correct option. "why" = one short sentence {{why}} explaining the answer.
            Return ONLY JSON: {"questions": [{"prompt": "<sentence with ___>", "options": ["...", "..."], "answer": <index of the correct option>, "why": "<explanation>"}]}
            """;
    }

    public static GrammarQuiz ValidateQuiz(GrammarQuiz quiz)
    {
        if (quiz.Questions.Count != QuizSize) throw new InvalidOperationException($"{QuizSize} ta savol kerak, keldi {quiz.Questions.Count}");
        foreach (var q in quiz.Questions)
        {
            if (string.IsNullOrWhiteSpace(q.Prompt) || !q.Prompt.Contains("___")) throw new InvalidOperationException("Savolda ___ yo'q");
            if (q.Options.Count is < 3 or > 4 || q.Options.Any(string.IsNullOrWhiteSpace) || q.Options.Distinct(StringComparer.OrdinalIgnoreCase).Count() != q.Options.Count)
                throw new InvalidOperationException("Variantlar 3-4 ta, har xil bo'lishi kerak");
            if (q.Answer < 0 || q.Answer >= q.Options.Count) throw new InvalidOperationException("To'g'ri javob indeksi noto'g'ri");
            if (string.IsNullOrWhiteSpace(q.Why)) throw new InvalidOperationException("Izoh bo'sh");
        }
        return quiz;
    }

    private async Task<T> AskAsync<T>(string prompt, double temperature, CancellationToken ct)
    {
        var body = new
        {
            contents = new[] { new { parts = new object[] { new { text = prompt } } } },
            generationConfig = new { temperature, responseMimeType = "application/json" },
        };
        var response = await gemini.SendWithFallbackAsync(body, ct);
        return GeminiClient.DeserializeStrict<T>(await GeminiClient.ExtractTextAsync(response, ct));
    }

    public async Task<SuggestedTopic> SuggestTopicAsync(string kind, string category, string level, IReadOnlyCollection<string> avoid, CancellationToken ct)
    {
        Exception? last = null;
        for (var i = 0; i < 2; i++)
        {
            try
            {
                return ValidateTopic(await AskAsync<SuggestedTopic>(TopicPrompt(kind, category, level, avoid), 0.9, ct), category);
            }
            catch (InvalidOperationException ex)
            {
                last = ex;
            }
        }
        throw last!;
    }

    public async Task<GrammarQuiz> QuizAsync(string topic, string level, string lang, CancellationToken ct)
    {
        Exception? last = null;
        for (var i = 0; i < 2; i++)
        {
            try
            {
                return ValidateQuiz(await AskAsync<GrammarQuiz>(QuizPrompt(topic, level, lang), 0.7, ct));
            }
            catch (InvalidOperationException ex)
            {
                last = ex;
            }
        }
        throw last!;
    }
}
