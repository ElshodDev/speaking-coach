using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using SpeakingCoach.Api.Data;

namespace SpeakingCoach.Api.Services;

public record WordExplanation(
    [property: JsonPropertyName("word")] string Word,
    [property: JsonPropertyName("partOfSpeech")] string PartOfSpeech,
    [property: JsonPropertyName("meaningUz")] string MeaningUz,
    [property: JsonPropertyName("definitionEn")] string DefinitionEn,
    [property: JsonPropertyName("example")] string Example);

public interface IWordService
{
    Task<WordExplanation> ExplainAsync(string word, string sentence, string level, CancellationToken ct = default);
}

/// <summary>
/// O'qish/Tinglash matnidagi so'zni kontekstda tushuntiradi: o'zbekcha
/// ma'nosi, sodda inglizcha ta'rif va yangi misol. Oddiy lug'atdan farqi —
/// so'z aynan shu gapdagi ma'nosida izohlanadi ("bank" — daryo qirg'og'imi
/// yoki moliya bankimi).
/// </summary>
public partial class GeminiWordService : IWordService
{
    public const int MaxWordLength = 40;
    public const int MaxSentenceLength = 400;

    private readonly GeminiClient _geminiClient;

    private const string PromptTemplate = """
        You are helping an Uzbek-speaking learner of English at level {2}.
        Explain the English word "{0}" as it is used in this sentence:
        "{1}"

        Return ONLY valid JSON of this exact shape, no other text, no markdown fences:
        {{
          "word": "<the dictionary form of the word>",
          "partOfSpeech": "<noun | verb | adjective | adverb | phrase | ...>",
          "meaningUz": "<short Uzbek translation in Latin script, 1-5 words, for THIS meaning>",
          "definitionEn": "<simple English definition, at most 15 words>",
          "example": "<one new short example sentence using the word, suited to the level>"
        }}
        """;

    public GeminiWordService(GeminiClient geminiClient)
    {
        _geminiClient = geminiClient;
    }

    // So'z: faqat harflar, apostrof va chiziqcha (masalan "don't", "well-known").
    [GeneratedRegex(@"^[A-Za-z][A-Za-z'\-]*$")]
    private static partial Regex WordPattern();

    /// <summary>
    /// Foydalanuvchi kiritgan so'z va gap prompt ichiga tushadi, shuning uchun
    /// qat'iy tekshiriladi: so'z faqat harflardan, gap cheklangan uzunlikda,
    /// qo'shtirnoqlar olib tashlanadi (prompt tuzilishini buzmasligi uchun).
    /// Xato bo'lsa — null va sababi.
    /// </summary>
    public static (string Word, string Sentence)? Sanitize(string? word, string? sentence, out string? error)
    {
        var w = (word ?? "").Trim().Trim('.', ',', '!', '?', ';', ':', '"', '“', '”', '(', ')');
        if (w.Length == 0 || w.Length > MaxWordLength || !WordPattern().IsMatch(w))
        {
            error = "So'z noto'g'ri (faqat inglizcha harflar, ko'pi bilan 40 belgi)";
            return null;
        }
        var s = (sentence ?? "").Replace('"', '\'').Replace('\n', ' ').Trim();
        if (s.Length > MaxSentenceLength) s = s[..MaxSentenceLength];
        error = null;
        return (w, s);
    }

    public static string BuildPrompt(string word, string sentence, string level) =>
        string.Format(PromptTemplate, word, sentence, LearnerLevel.Describe(level));

    public async Task<WordExplanation> ExplainAsync(string word, string sentence, string level, CancellationToken ct = default)
    {
        var requestBody = new
        {
            contents = new[] { new { parts = new object[] { new { text = BuildPrompt(word, sentence, level) } } } },
            generationConfig = new { temperature = 0.2, responseMimeType = "application/json" },
        };

        var response = await _geminiClient.SendWithFallbackAsync(requestBody, ct);
        var text = await GeminiClient.ExtractTextAsync(response, ct);
        var result = GeminiClient.DeserializeStrict<WordExplanation>(text);
        if (string.IsNullOrWhiteSpace(result.MeaningUz) || string.IsNullOrWhiteSpace(result.Word))
        {
            throw new InvalidOperationException("Gemini so'z ma'nosini bermadi");
        }
        return result;
    }
}
