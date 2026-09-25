using System.Text.Json.Serialization;
using SpeakingCoach.Api.Data;

namespace SpeakingCoach.Api.Services;

// Reading va Listening bir xil tuzilishga ega: matn + 4 ta test savoli.
// Farqi faqat matn qanday "iste'mol qilinishida": Reading'da ko'z bilan
// o'qiladi, Listening'da brauzer uni ovoz chiqarib o'qiydi (matnning o'zi
// javob berilgunicha ko'rsatilmaydi). Shuning uchun ikkalasi bitta servis.

public record ComprehensionQuestion(
    [property: JsonPropertyName("question")] string Question,
    [property: JsonPropertyName("options")] List<string> Options,
    [property: JsonPropertyName("correctIndex")] int CorrectIndex,
    [property: JsonPropertyName("explanation")] string Explanation);

public record ComprehensionExercise(
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("passage")] string Passage,
    [property: JsonPropertyName("questions")] List<ComprehensionQuestion> Questions);

public record QuestionResult(
    [property: JsonPropertyName("chosenIndex")] int ChosenIndex,
    [property: JsonPropertyName("correctIndex")] int CorrectIndex,
    [property: JsonPropertyName("isCorrect")] bool IsCorrect,
    [property: JsonPropertyName("explanation")] string Explanation);

public record ComprehensionResult(
    [property: JsonPropertyName("score")] int Score,
    [property: JsonPropertyName("total")] int Total,
    [property: JsonPropertyName("results")] List<QuestionResult> Results);

public interface IComprehensionService
{
    Task<ComprehensionExercise> GenerateAsync(ActivityType type, string level, CancellationToken ct = default);
}

public class GeminiComprehensionService : IComprehensionService
{
    public const int QuestionCount = 4;
    public const int OptionCount = 4;

    private readonly GeminiClient _geminiClient;

    // Mavzuni server tanlaydi: Gemini'ga "istalgan mavzu" desak, u ko'pincha
    // bir xil 2-3 mavzuga qaytaveradi. Ro'yxatdan tasodifiy tanlash xilma-
    // xillikni kafolatlaydi.
    private static readonly string[] Topics =
    {
        "a small business that changed its town", "how sleep affects learning", "a famous bridge and how it was built",
        "volunteering abroad", "the history of tea", "working from home", "a surprising animal behaviour",
        "city parks and mental health", "a student's first job", "recycling plastic", "learning a musical instrument as an adult",
        "the Silk Road cities", "online shopping habits", "a festival in another country", "why people collect things",
    };

    private const string ReadingTemplate = """
        You are creating an English READING comprehension exercise for Uzbek-speaking learners at level {3}.
        Topic: {0}

        1. Write an original passage of {4}-{5} words on this topic, in natural English whose vocabulary
           and grammar suit a {3} learner,
           split into 2-4 paragraphs (separate paragraphs with a blank line).
        2. Write exactly {1} multiple-choice questions about the passage: one about the main idea,
           one about a specific detail, one about the meaning of a word or phrase in context,
           and one that requires a simple inference.
        3. Each question has exactly {2} options and exactly one correct option. The wrong options
           must be plausible but clearly contradicted by the passage. Vary the position of the
           correct option across questions.
        4. For each question, add a one-sentence explanation that points to the relevant part of the passage.

        Return ONLY valid JSON of this exact shape, no other text, no markdown fences:
        {{
          "title": "<short title>",
          "passage": "<the passage>",
          "questions": [
            {{"question": "<question>", "options": ["<A>", "<B>", "<C>", "<D>"], "correctIndex": <0-3>, "explanation": "<one sentence>"}}
          ]
        }}
        """;

    private const string ListeningTemplate = """
        You are creating an English LISTENING comprehension exercise for Uzbek-speaking learners at level {3}.
        The script will be read aloud by a text-to-speech voice, and the learner will NOT see it.
        Topic: {0}

        1. Write a spoken-style script of {4}-{5} words, suited to a {3} learner: a short talk, story,
           announcement or voicemail.
           Use short sentences. Do not use headings, lists, brackets, abbreviations, symbols or numbers
           written as digits (write "twenty", not "20"), so that the voice reads it naturally.
        2. Write exactly {1} multiple-choice questions that can be answered only by listening carefully:
           one about the main idea and the rest about specific details (who, what, when, why).
        3. Each question has exactly {2} options and exactly one correct option. Vary the position
           of the correct option across questions.
        4. For each question, add a one-sentence explanation quoting what the speaker said.

        Return ONLY valid JSON of this exact shape, no other text, no markdown fences:
        {{
          "title": "<short title>",
          "passage": "<the script>",
          "questions": [
            {{"question": "<question>", "options": ["<A>", "<B>", "<C>", "<D>"], "correctIndex": <0-3>, "explanation": "<one sentence>"}}
          ]
        }}
        """;

    /// <summary>Prompt'ni yig'adi — alohida metod, chunki uni unit test bilan tekshirish mumkin.</summary>
    public static string BuildPrompt(ActivityType type, string level, string topic)
    {
        var (template, words) = type switch
        {
            ActivityType.Reading => (ReadingTemplate, LearnerLevel.ReadingWords(level)),
            ActivityType.Listening => (ListeningTemplate, LearnerLevel.ListeningWords(level)),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Faqat Reading yoki Listening"),
        };
        return string.Format(
            template, topic, QuestionCount, OptionCount, LearnerLevel.Describe(level), words.Min, words.Max);
    }

    public GeminiComprehensionService(GeminiClient geminiClient)
    {
        _geminiClient = geminiClient;
    }

    public async Task<ComprehensionExercise> GenerateAsync(ActivityType type, string level, CancellationToken ct = default)
    {
        var topic = Topics[Random.Shared.Next(Topics.Length)];
        var prompt = BuildPrompt(type, level, topic);

        var requestBody = new
        {
            contents = new[] { new { parts = new object[] { new { text = prompt } } } },
            generationConfig = new
            {
                // Baholashda temperature past (0.2) — natija barqaror bo'lsin.
                // Bu yerda esa YARATYAPMIZ: har safar yangi matn kerak,
                // shuning uchun yuqoriroq.
                temperature = 0.9,
                responseMimeType = "application/json"
            }
        };

        var response = await _geminiClient.SendWithFallbackAsync(requestBody, ct);
        var text = await GeminiClient.ExtractTextAsync(response, ct);
        var exercise = GeminiClient.DeserializeStrict<ComprehensionExercise>(text);
        Validate(exercise);
        return exercise;
    }

    /// <summary>
    /// Gemini "aynan 4 ta savol, har birida 4 ta variant" degan talabni
    /// ba'zan buzishi mumkin. Buzilgan mashqni foydalanuvchiga ko'rsatgandan
    /// ko'ra, xato qaytarib qayta so'ratish yaxshiroq.
    /// </summary>
    public static void Validate(ComprehensionExercise exercise)
    {
        if (string.IsNullOrWhiteSpace(exercise.Title) || string.IsNullOrWhiteSpace(exercise.Passage))
        {
            throw new InvalidOperationException("Gemini sarlavha yoki matnni bo'sh qaytardi");
        }
        if (exercise.Questions.Count != QuestionCount)
        {
            throw new InvalidOperationException($"Gemini {QuestionCount} ta o'rniga {exercise.Questions.Count} ta savol qaytardi");
        }
        foreach (var q in exercise.Questions)
        {
            if (string.IsNullOrWhiteSpace(q.Question) || q.Options.Count != OptionCount
                || q.Options.Any(string.IsNullOrWhiteSpace)
                || q.CorrectIndex < 0 || q.CorrectIndex >= OptionCount)
            {
                throw new InvalidOperationException($"Gemini noto'g'ri tuzilgan savol qaytardi: {q.Question}");
            }
        }
    }

    /// <summary>
    /// Tekshirish — oddiy C# kodi, LLM EMAS. Test savolida to'g'ri javob
    /// oldindan ma'lum, shuning uchun uni modelga qayta so'rash sekin, pullik
    /// va (barqarorlik testida ko'rganimizdek) tasodifiy bo'lardi. Qoida:
    /// LLM — yaratish va ochiq javoblarni baholash uchun; aniq javobli
    /// narsalarni esa deterministik kod tekshirsin.
    /// </summary>
    public static ComprehensionResult Grade(ComprehensionExercise exercise, IReadOnlyList<int> answers)
    {
        if (answers.Count != exercise.Questions.Count)
        {
            throw new UserInputException("exercise.answers_count", exercise.Questions.Count, answers.Count);
        }

        var results = new List<QuestionResult>();
        for (var i = 0; i < exercise.Questions.Count; i++)
        {
            var q = exercise.Questions[i];
            var chosen = answers[i];
            if (chosen < 0 || chosen >= q.Options.Count)
            {
                throw new UserInputException("exercise.bad_answer", i + 1);
            }
            results.Add(new QuestionResult(chosen, q.CorrectIndex, chosen == q.CorrectIndex, q.Explanation));
        }

        return new ComprehensionResult(results.Count(r => r.IsCorrect), results.Count, results);
    }
}
