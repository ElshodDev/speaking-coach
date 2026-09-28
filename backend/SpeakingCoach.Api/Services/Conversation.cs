using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using SpeakingCoach.Api.Data;
using SpeakingCoach.Api.Services.Mock;

namespace SpeakingCoach.Api.Services;

/// <summary>Rolli suhbat mavzusi: AI kim rolida, qanday boshlaydi va nimaga erishish kerak.</summary>
public record TalkScenario(string Id, string Level, string AiRole, string Setting, string Opening);

/// <summary>Suhbatdagi bitta gap: "ai" yoki "user".</summary>
public record TalkLine(
    [property: JsonPropertyName("who")] string Who,
    [property: JsonPropertyName("text")] string Text);

/// <summary>Serverda saqlanadigan suhbat holati (PendingExercises.Payload). Tarix brauzerdan olinmaydi — uni soxtalab bo'lmaydi.</summary>
public record TalkState(
    [property: JsonPropertyName("scenario")] string Scenario,
    [property: JsonPropertyName("level")] string Level,
    [property: JsonPropertyName("userId")] Guid UserId,
    [property: JsonPropertyName("lines")] List<TalkLine> Lines,
    [property: JsonPropertyName("tips")] List<CorrectionItem> Tips);

/// <summary>Bitta navbatga AI javobi: nima eshitdi, keyingi gapi va (bo'lsa) qisqa tuzatish.</summary>
public record TalkTurnResult(
    [property: JsonPropertyName("heard")] string Heard,
    [property: JsonPropertyName("reply")] string Reply,
    [property: JsonPropertyName("tip")] CorrectionItem? Tip);

/// <summary>Suhbat yakuni: xulosa, kuchli tomonlar, tuzatishlar (takrorlash kartalariga) va keyingi qadam.</summary>
public record TalkSummary(
    [property: JsonPropertyName("summary")] string Summary,
    [property: JsonPropertyName("strengths")] string Strengths,
    [property: JsonPropertyName("topCorrections")] List<CorrectionItem> TopCorrections,
    [property: JsonPropertyName("usefulPhrases")] List<string> UsefulPhrases,
    [property: JsonPropertyName("nextFocus")] string NextFocus);

/// <summary>
/// AI suhbatdosh: foydalanuvchi ovoz (yoki matn) bilan javob beradi, Gemini bitta
/// so'rovda uni eshitadi, rolda davom etadi va bitta eng muhim xatoni qisqa
/// tuzatadi. Suhbat cheklangan (MaxTurns): boshlash — bitta mashq (kunlik limit),
/// har bir navbat — alohida kunlik navbatlar limitidan (Ai:TalkTurnsPerDay).
/// Prompt va natijani tekshirish — toza funksiyalar (unit testlangan).
/// </summary>
public static class Talk
{
    public const int MaxTurns = 8;
    public const int MaxTextChars = 600;
    public const int MaxLineChars = 400;

    public static readonly TalkScenario[] Scenarios =
    [
        new("cafe", "A2", "a friendly barista in a busy café in London", "The learner is a customer ordering drinks and food.", "Hi there! Welcome to Bean & Leaf. What can I get for you today?"),
        new("hotel", "A2", "a hotel receptionist", "The learner is checking in; ask about the booking, the room and breakfast.", "Good evening and welcome to the Riverside Hotel. Do you have a reservation with us?"),
        new("directions", "A2", "a helpful local person in a city centre", "The learner is a tourist asking the way to places.", "Hello! You look a bit lost. Can I help you find something?"),
        new("friend", "A2", "a friendly classmate", "Chat about weekend plans, hobbies and films.", "Hey! Finally it's Friday. Have you got any plans for the weekend?"),
        new("shop", "A2", "a shop assistant in a clothes shop", "The learner wants to buy or return clothes; talk about size, colour and price.", "Hi! Are you looking for anything in particular today?"),
        new("doctor", "B1", "a family doctor", "The learner describes symptoms; ask follow-up questions and give simple advice.", "Good morning. Please, have a seat. So, what seems to be the problem today?"),
        new("job", "B1", "an interviewer hiring for a junior position", "A job interview: experience, strengths, weaknesses, why this job.", "Thank you for coming in today. To start, could you tell me a little about yourself?"),
        new("ielts1", "B1", "an IELTS Speaking examiner doing Part 1", "Ask short Part 1 questions on everyday topics (home, study, free time), one at a time.", "Good afternoon. My name is Sarah. Can you tell me your full name, please? And where are you from?"),
        new("ielts3", "B2", "an IELTS Speaking examiner doing Part 3", "Ask abstract discussion questions about education and technology; push for reasons and examples.", "Let's talk about education. Do you think schools today prepare young people well for working life?"),
        new("university", "B2", "a university admissions officer", "An admissions interview for a bachelor's programme abroad: motivation, plans, experience.", "Welcome. We've read your application with interest. Why have you chosen to study abroad?"),
        new("debate", "B2", "a friendly debate partner who takes the opposite side", "A friendly debate: 'Social media does more harm than good.' Challenge the learner's arguments politely.", "Here's our topic: social media does more harm than good. I actually think it helps people more. What's your view?"),
        new("free", "A2", "a friendly English conversation partner", "Free conversation: follow whatever topic the learner brings up and keep it going.", "Hi! It's nice to meet you. What would you like to talk about today?"),
    ];

    public static TalkScenario? Find(string? id) => Scenarios.FirstOrDefault(s => s.Id == id);

    public static string BuildTurnPrompt(TalkScenario s, string level, IReadOnlyList<TalkLine> history, string lang, string? typed)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"You are role-playing as {s.AiRole}. {s.Setting}");
        sb.AppendLine($"The learner is an Uzbek or Russian speaker at {LearnerLevel.Describe(level)} level of English.");
        sb.AppendLine("Stay in character. Speak naturally but simply for that level: at most 2 short sentences, and usually end with a question that keeps the conversation going.");
        sb.AppendLine("Never mention that you are an AI or a language model. Never switch to another language in \"reply\".");
        sb.AppendLine();
        sb.AppendLine("Conversation so far:");
        foreach (var line in history) sb.AppendLine($"{(line.Who == "ai" ? "YOU" : "LEARNER")}: {line.Text}");
        sb.AppendLine();
        sb.AppendLine(typed is null
            ? "The learner's new answer is in the attached audio. Transcribe it exactly as spoken into \"heard\" (keep their mistakes; do not correct them). If the audio is silent or not English, set \"heard\" to \"\"."
            : $"The learner typed this answer: \"{typed}\". Copy it exactly into \"heard\".");
        sb.AppendLine("Then reply in character to what they said.");
        sb.AppendLine($"If their answer has a clear grammar or word-choice mistake, put the single most useful one in \"tip\" with \"original\" (their exact words), \"corrected\" and a one-sentence \"explanation\" in {GeminiIeltsEvaluator.FeedbackLanguage(lang)}. If there is no real mistake, set \"tip\" to null. Do not correct pronunciation or punctuation.");
        sb.AppendLine("""Return ONLY JSON: {"heard": "...", "reply": "...", "tip": {"original": "...", "corrected": "...", "explanation": "..."} or null}""");
        return sb.ToString();
    }

    public static string BuildSummaryPrompt(TalkScenario s, string level, IReadOnlyList<TalkLine> lines, string lang)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"You are an English speaking coach. Below is a role-play where you played {s.AiRole}. {s.Setting}");
        sb.AppendLine($"The learner is at {LearnerLevel.Describe(level)} level. Judge ONLY the LEARNER's lines, and never invent things they did not say.");
        sb.AppendLine();
        foreach (var line in lines) sb.AppendLine($"{(line.Who == "ai" ? "PARTNER" : "LEARNER")}: {line.Text}");
        sb.AppendLine();
        sb.AppendLine($"Write \"summary\", \"strengths\", every correction \"explanation\" and \"nextFocus\" in {GeminiIeltsEvaluator.FeedbackLanguage(lang)}. Keep them short and concrete.");
        sb.AppendLine("\"topCorrections\": up to 5 real mistakes from the learner's lines, quoting their exact words in \"original\". \"usefulPhrases\": 3 natural English phrases they could have used in this situation.");
        sb.AppendLine("""Return ONLY JSON: {"summary": "...", "strengths": "...", "topCorrections": [{"original": "...", "corrected": "...", "explanation": "..."}], "usefulPhrases": ["...", "...", "..."], "nextFocus": "..."}""");
        return sb.ToString();
    }

    public static int UserTurns(TalkState state) => state.Lines.Count(l => l.Who == "user");

    /// <summary>AI javobini tekshiradi: bo'sh javob — xato; juda uzun matnlar qisqartiriladi; noto'g'ri "tip" — olib tashlanadi.</summary>
    public static TalkTurnResult CleanTurn(TalkTurnResult r, string? typed)
    {
        var reply = Cut(r.Reply, MaxLineChars);
        if (reply.Length == 0) throw new InvalidOperationException("empty reply");
        var heard = typed is not null ? Cut(typed, MaxTextChars) : Cut(r.Heard, MaxTextChars);
        var tip = r.Tip is { } t && Cut(t.Original, 200).Length > 0 && Cut(t.Corrected, 200).Length > 0
            && !string.Equals(t.Original.Trim(), t.Corrected.Trim(), StringComparison.OrdinalIgnoreCase)
            ? new CorrectionItem(Cut(t.Original, 200), Cut(t.Corrected, 200), Cut(t.Explanation, 300))
            : null;
        return new TalkTurnResult(heard, reply, tip);
    }

    public static TalkSummary CleanSummary(TalkSummary s)
    {
        if (string.IsNullOrWhiteSpace(s.Summary)) throw new InvalidOperationException("empty summary");
        return s with
        {
            Summary = Cut(s.Summary, 800),
            Strengths = Cut(s.Strengths, 500),
            NextFocus = Cut(s.NextFocus, 300),
            TopCorrections = (s.TopCorrections ?? [])
                .Where(c => c is not null && !string.IsNullOrWhiteSpace(c.Original) && !string.IsNullOrWhiteSpace(c.Corrected)
                    && !string.Equals(c.Original.Trim(), c.Corrected.Trim(), StringComparison.OrdinalIgnoreCase))
                .Take(5)
                .Select(c => new CorrectionItem(Cut(c.Original, 200), Cut(c.Corrected, 200), Cut(c.Explanation, 300)))
                .ToList(),
            UsefulPhrases = (s.UsefulPhrases ?? []).Select(p => Cut(p, 120)).Where(p => p.Length > 0).Take(5).ToList(),
        };
    }

    private static string Cut(string? s, int max)
    {
        var v = (s ?? "").Trim();
        return v.Length <= max ? v : v[..max].TrimEnd() + "…";
    }

    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
}

/// <summary>
/// Bitta suhbatda bir vaqtda faqat bitta navbat (yoki yakun): ikkita parallel
/// so'rov bir xil holatni o'qib, bir-birining yozuvini o'chirib yubormasin va
/// ikki barobar Gemini so'rovi ketmasin. Ikkinchisi darhol 409 oladi.
/// Bitta server — xotiradagi qulf yetarli.
/// </summary>
public class TalkLocks
{
    private readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, byte> _busy = new();

    /// <summary>null — suhbat hozir band (boshqa navbat bajarilmoqda).</summary>
    public IDisposable? TryEnter(Guid talkId) =>
        _busy.TryAdd(talkId, 0) ? new Lease(this, talkId) : null;

    public bool IsBusy(Guid talkId) => _busy.ContainsKey(talkId);

    private sealed class Lease(TalkLocks owner, Guid id) : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0) owner._busy.TryRemove(id, out _);
        }
    }
}

public interface ITalkAi
{
    Task<TalkTurnResult> TurnAsync(TalkScenario s, string level, IReadOnlyList<TalkLine> history, string lang, byte[]? audio, string? mime, string? typed, CancellationToken ct);
    Task<TalkSummary> SummarizeAsync(TalkScenario s, string level, IReadOnlyList<TalkLine> lines, string lang, CancellationToken ct);
}

public class GeminiTalkAi(GeminiClient gemini) : ITalkAi
{
    public async Task<TalkTurnResult> TurnAsync(TalkScenario s, string level, IReadOnlyList<TalkLine> history, string lang, byte[]? audio, string? mime, string? typed, CancellationToken ct)
    {
        var parts = new List<object> { new { text = Talk.BuildTurnPrompt(s, level, history, lang, typed) } };
        if (typed is null && audio is not null)
            parts.Add(new { inline_data = new { mime_type = (mime ?? "audio/webm").Split(';')[0], data = Convert.ToBase64String(audio) } });
        var body = new
        {
            contents = new[] { new { parts } },
            generationConfig = new { temperature = 0.7, responseMimeType = "application/json" },
        };
        var response = await gemini.SendWithFallbackAsync(body, ct);
        var text = await GeminiClient.ExtractTextAsync(response, ct);
        return Talk.CleanTurn(GeminiClient.DeserializeStrict<TalkTurnResult>(text), typed);
    }

    public async Task<TalkSummary> SummarizeAsync(TalkScenario s, string level, IReadOnlyList<TalkLine> lines, string lang, CancellationToken ct)
    {
        var body = new
        {
            contents = new[] { new { parts = new object[] { new { text = Talk.BuildSummaryPrompt(s, level, lines, lang) } } } },
            generationConfig = new { temperature = 0.3, responseMimeType = "application/json" },
        };
        var response = await gemini.SendWithFallbackAsync(body, ct);
        var text = await GeminiClient.ExtractTextAsync(response, ct);
        return Talk.CleanSummary(GeminiClient.DeserializeStrict<TalkSummary>(text));
    }
}
