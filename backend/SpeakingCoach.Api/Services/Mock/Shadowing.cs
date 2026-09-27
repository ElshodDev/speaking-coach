using System.Globalization;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace SpeakingCoach.Api.Services.Mock;

// ---- Shadowing: gapma-gap tinglab, darhol takrorlash ----
//
// Ikki xil dars:
//   "youtube"   — YouTube videosi (o'z pleyerida), har gapning boshlanish va
//                 tugash vaqti (soniya) bilan; Gemini videoni tinglab yozadi;
//   "character" — ilovaning multfilm qahramonlari (mushuk, boyo'g'li, robot...)
//                 brauzer ovozida navbatma-navbat gapiradi (TTS).
// Bazada MockTests jadvalida saqlanadi (Exam = "shadowing") — bot orqali
// qo'shish, admin tasdig'i va /bank o'sha oqimdan foydalanadi.

public record ShadowLine(
    [property: JsonPropertyName("text")] string Text,
    [property: JsonPropertyName("start")] double? Start = null,
    [property: JsonPropertyName("end")] double? End = null,
    [property: JsonPropertyName("speaker")] int Speaker = 0);

public record ShadowingLesson(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("kind")] string Kind,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("level")] string Level,
    [property: JsonPropertyName("videoId")] string? VideoId,
    [property: JsonPropertyName("from")] double? From,
    [property: JsonPropertyName("to")] double? To,
    [property: JsonPropertyName("characters")] string[] Characters,
    [property: JsonPropertyName("lines")] List<ShadowLine> Lines);

/// <summary>Talaffuz tekshiruvi natijasi: umumiy ball, eshitilgan matn, har so'z holati va bitta maslahat.</summary>
public record ShadowWord(
    [property: JsonPropertyName("word")] string Word,
    [property: JsonPropertyName("ok")] bool Ok);

public record ShadowCheck(
    [property: JsonPropertyName("score")] int Score,
    [property: JsonPropertyName("heard")] string Heard,
    [property: JsonPropertyName("words")] List<ShadowWord> Words,
    [property: JsonPropertyName("tip")] string Tip);

public static partial class ShadowingRules
{
    public const string YouTube = "youtube";
    public const string Character = "character";

    public static readonly string[] Levels = ["A1", "A2", "B1", "B2", "C1"];

    /// <summary>Qahramonlar (frontend'da SVG + ovoz balandligi). Tartibi — bot tanlashi uchun.</summary>
    public static readonly string[] Characters = ["cat", "owl", "robot", "fox", "bear", "penguin"];

    public const double DefaultClip = 180;
    public const double MaxClip = 300;
    public const double MaxLineSeconds = 15;
    public const int MaxLineWords = 30;

    // ---------------- YouTube havolasi ----------------

    [GeneratedRegex(@"(?:youtube\.com/(?:watch\?(?:\S*?&)?v=|shorts/|embed/|live/)|youtu\.be/)([A-Za-z0-9_-]{11})", RegexOptions.IgnoreCase)]
    private static partial Regex VideoPattern();

    [GeneratedRegex(@"[?&#]t=(?:(\d+)h)?(?:(\d+)m)?(\d+)?s?", RegexOptions.IgnoreCase)]
    private static partial Regex TimeParam();

    [GeneratedRegex(@"(?<![\w:])(\d{1,2}:\d{2}(?::\d{2})?|\d{1,5})\s*(?:-|–|—)\s*(\d{1,2}:\d{2}(?::\d{2})?|\d{1,5})(?![\w:])")]
    private static partial Regex RangePattern();

    [GeneratedRegex(@"https?://\S+|\S*youtu\S*", RegexOptions.IgnoreCase)]
    private static partial Regex UrlPattern();

    /// <summary>"1:20" → 80, "1:02:03" → 3723, "95" → 95.</summary>
    public static double? ParseTime(string s)
    {
        var parts = s.Split(':');
        if (parts.Any(p => !int.TryParse(p, NumberStyles.None, CultureInfo.InvariantCulture, out _))) return null;
        return parts.Aggregate(0.0, (acc, p) => acc * 60 + int.Parse(p, CultureInfo.InvariantCulture));
    }

    public record YouTubeClip(string VideoId, double From, double To)
    {
        public string Url => $"https://www.youtube.com/watch?v={VideoId}";
    }

    /// <summary>
    /// Xabardan video va oraliqni oladi: havola (youtu.be, watch?v=, shorts),
    /// ixtiyoriy "?t=80" yoki matndagi "1:20-3:40". Oraliq yo'q — boshlanishdan
    /// 3 daqiqa; ko'pi bilan 5 daqiqa. null — havola topilmadi yoki oraliq noto'g'ri.
    /// </summary>
    public static YouTubeClip? ParseYouTube(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var m = VideoPattern().Match(text);
        if (!m.Success) return null;
        var id = m.Groups[1].Value;

        double from = 0;
        var t = TimeParam().Match(text);
        if (t.Success && t.Value.Length > 3)
        {
            double Num(Group g) => g.Success ? double.Parse(g.Value, CultureInfo.InvariantCulture) : 0;
            from = Num(t.Groups[1]) * 3600 + Num(t.Groups[2]) * 60 + Num(t.Groups[3]);
        }

        var rest = UrlPattern().Replace(text, " ");
        var r = RangePattern().Match(rest);
        double to;
        if (r.Success)
        {
            var a = ParseTime(r.Groups[1].Value);
            var b = ParseTime(r.Groups[2].Value);
            if (a is null || b is null || b <= a || b - a < 10 || b - a > MaxClip) return null;
            (from, to) = (a.Value, b.Value);
        }
        else
        {
            to = from + DefaultClip;
        }
        return new YouTubeClip(id, from, to);
    }

    public static string Clock(double seconds)
    {
        var s = (int)Math.Floor(seconds);
        return s >= 3600 ? $"{s / 3600}:{s / 60 % 60:00}:{s % 60:00}" : $"{s / 60}:{s % 60:00}";
    }

    /// <summary>Daraja matndan: "B1: travel" → B1 (yo'q bo'lsa — null).</summary>
    public static string? LevelIn(string? text)
    {
        if (text is null) return null;
        var m = Regex.Match(text, @"\b(A1|A2|B1|B2|C1)\b", RegexOptions.IgnoreCase);
        return m.Success ? m.Value.ToUpperInvariant() : null;
    }

    // ---------------- Vaqtlar ----------------

    /// <summary>
    /// Gemini kesilgan qism vaqtlarini ba'zan videoning boshidan emas, qismning
    /// boshidan hisoblaydi. Gaplar [from, to] ichida emas, lekin [0, to−from]
    /// ichida bo'lsa — hammasini "from" ga suramiz.
    /// </summary>
    public static List<ShadowLine> NormalizeTimes(List<ShadowLine> lines, double from, double to)
    {
        if (from < 5 || lines.Count == 0 || lines.Any(l => l.Start is null || l.End is null)) return lines;
        var span = to - from;
        bool Inside(double lo, double hi) => lines.All(l => l.Start >= lo - 2 && l.End <= hi + 3);
        if (Inside(from, to) || !Inside(0, span)) return lines;
        return lines.Select(l => l with { Start = Math.Round(l.Start!.Value + from, 2), End = Math.Round(l.End!.Value + from, 2) }).ToList();
    }

    public static int Words(string text) => text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;

    private static void Require(bool ok, string message)
    {
        if (!ok) throw new InvalidOperationException(message);
    }

    /// <summary>Qat'iy tekshiruv (AI javobi va bazadan o'qilgan dars uchun bir xil).</summary>
    public static void Validate(ShadowingLesson l)
    {
        Require(l.Kind is YouTube or Character, $"kind noto'g'ri: {l.Kind}");
        Require(!string.IsNullOrWhiteSpace(l.Title) && l.Title.Length <= 120, "title bo'sh yoki juda uzun");
        Require(Levels.Contains(l.Level), $"level A1–C1 bo'lishi kerak: {l.Level}");
        Require(l.Lines.Count is >= 3 and <= 100, $"gaplar soni 3–100 bo'lishi kerak: {l.Lines.Count}");
        foreach (var (line, i) in l.Lines.Select((x, i) => (x, i + 1)))
        {
            Require(!string.IsNullOrWhiteSpace(line.Text), $"{i}-gap bo'sh");
            Require(Words(line.Text) <= MaxLineWords && line.Text.Length <= 220, $"{i}-gap juda uzun ({Words(line.Text)} so'z) — {MaxLineWords} so'zgacha bo'lsin");
        }

        if (l.Kind == YouTube)
        {
            Require(l.VideoId is { Length: 11 }, "videoId yo'q");
            Require(l.From is not null && l.To is not null && l.To > l.From, "from/to yo'q");
            double prev = -1;
            foreach (var (line, i) in l.Lines.Select((x, i) => (x, i + 1)))
            {
                Require(line.Start is not null && line.End is not null, $"{i}-gapda start/end yo'q");
                var (s, e) = (line.Start!.Value, line.End!.Value);
                Require(e > s && e - s <= MaxLineSeconds, $"{i}-gap vaqti noto'g'ri: {s}–{e} (0–{MaxLineSeconds} s)");
                Require(s >= l.From!.Value - 2 && e <= l.To!.Value + 3, $"{i}-gap tanlangan oraliqdan tashqarida: {s}–{e}");
                Require(s >= prev - 0.5, $"{i}-gap vaqti oldingisidan oldin");
                prev = s;
            }
            return;
        }

        Require(l.Characters.Length is 1 or 2 && l.Characters.All(Characters.Contains) && l.Characters.Distinct().Count() == l.Characters.Length,
            "characters: 1–2 ta ma'lum qahramon");
        Require(l.Lines.All(x => x.Speaker >= 0 && x.Speaker < l.Characters.Length), "speaker raqami noto'g'ri");
    }

    /// <summary>Taxminiy davomiylik (soniya): video — oraliq; qahramonlar — ~2.6 so'z/s + pauzalar.</summary>
    public static int Seconds(ShadowingLesson l) => l.Kind == YouTube && l.From is { } f && l.To is { } t
        ? (int)Math.Round(t - f)
        : (int)Math.Round(l.Lines.Sum(x => Words(x.Text) / 2.6 + 0.6));

    // ---------------- Talaffuz tekshiruvi ----------------

    [GeneratedRegex(@"[A-Za-z0-9]+(?:['’][A-Za-z]+)*")]
    private static partial Regex WordPattern();

    /// <summary>Gapning so'zlari (tinish belgilarisiz, apostrof saqlanadi): "Don't stop, Bob!" → [Don't, stop, Bob].</summary>
    public static List<string> Tokens(string text) => WordPattern().Matches(text).Select(m => m.Value).ToList();

    private static string Norm(string w) => w.ToLowerInvariant().Replace('’', '\'');

    /// <summary>
    /// Gemini javobini gapning haqiqiy so'zlariga moslaydi: soni teng bo'lsa —
    /// tartib bo'yicha; aks holda "to'g'ri" deb belgilangan so'zlar to'plami
    /// bo'yicha. Ball 0–100 oralig'iga keltiriladi. Shunday qilib ekranda doim
    /// asl gap ko'rsatiladi, AI qanday bo'lmasin.
    /// </summary>
    public static ShadowCheck Align(string target, ShadowCheck ai)
    {
        var tokens = Tokens(target);
        var aiWords = ai.Words.Where(w => Tokens(w.Word).Count > 0).ToList();
        List<ShadowWord> words;
        if (aiWords.Count == tokens.Count)
        {
            words = tokens.Select((t, i) => new ShadowWord(t, aiWords[i].Ok)).ToList();
        }
        else
        {
            var ok = aiWords.Where(w => w.Ok).SelectMany(w => Tokens(w.Word)).Select(Norm).ToHashSet();
            words = tokens.Select(t => new ShadowWord(t, ok.Contains(Norm(t)))).ToList();
        }
        var tip = (ai.Tip ?? "").Trim();
        if (tip.Length > 300) tip = tip[..299] + "…";
        var heard = (ai.Heard ?? "").Trim();
        if (heard.Length > 400) heard = heard[..399] + "…";
        return new ShadowCheck(Math.Clamp(ai.Score, 0, 100), heard, words, tip);
    }

    public static string CheckPrompt(string target, string lang) => $$$"""
        You are a friendly English pronunciation coach for Uzbek-speaking learners.
        The learner is doing shadowing: they listened to this sentence and tried to repeat it exactly, copying the pronunciation, rhythm and intonation:
        "{{{target}}}"
        Listen to the attached recording and compare it with the sentence. Judge only what you hear.
        Return exactly this JSON:
        {"score": <0-100: word accuracy, clear sounds, word stress, rhythm and intonation compared with natural native speech; 90+ only if nearly native-like>,
         "heard": "<exactly what the learner said>",
         "words": [{"word": "<each word of the TARGET sentence, in order, without punctuation>", "ok": <true if said clearly and correctly, false if missing, wrong or hard to understand>}],
         "tip": "<ONE short, concrete tip (max 25 words) about the most important problem — a sound, a word stress, linking or intonation — written in {{{LanguageName(lang)}}}; praise briefly if it was very good>"}
        If the recording is silent, unclear or a different sentence, return score 0 and ok=false for every word.
        Return ONLY valid JSON, no markdown fences.
        """;

    public static string LanguageName(string lang) => lang switch
    {
        "ru" => "Russian",
        "en" => "English",
        _ => "Uzbek (Latin script)",
    };

    public static void ValidateCheck(ShadowCheck c)
    {
        Require(c.Words is { Count: > 0 }, "words bo'sh");
        Require(c.Score is >= 0 and <= 100, "score 0–100 bo'lishi kerak");
    }

    // ---------------- Yaratish (bot) ----------------

    public static string YouTubePrompt(YouTubeClip clip, string? levelHint) => $$$"""
        You are preparing a SHADOWING lesson for English learners from the attached YouTube video.
        Work only with the part of the video from {{{Clock(clip.From)}}} to {{{Clock(clip.To)}}} ({{{clip.From:0}}} s to {{{clip.To:0}}} s from the start of the video).
        Transcribe the English speech in that part EXACTLY as spoken (correct only obvious slips; no music or sound descriptions) and split it into short lines
        that a learner can repeat in one breath: one sentence or a natural phrase, 3 to 20 words, at most {{{MaxLineSeconds:0}}} seconds long.
        For every line give the start and end time in SECONDS FROM THE START OF THE WHOLE VIDEO, with one decimal (for example 83.4),
        as precisely as you can hear: start just before the first word, end just after the last word. Lines follow each other in time and do not overlap.
        Estimate the CEFR level of the language (A1, A2, B1, B2 or C1){{{(levelHint is null ? "" : $"; the teacher suggests {levelHint}")}}}.
        Return exactly this JSON:
        {"id": "new", "kind": "youtube", "title": "<short English title for the clip, max 60 characters>", "level": "<A1|A2|B1|B2|C1>",
         "videoId": "{{{clip.VideoId}}}", "from": {{{clip.From.ToString(CultureInfo.InvariantCulture)}}}, "to": {{{clip.To.ToString(CultureInfo.InvariantCulture)}}}, "characters": [],
         "lines": [{"text": "<line>", "start": <seconds>, "end": <seconds>, "speaker": 0}]}
        Return ONLY valid JSON, no markdown fences.
        """;

    public static string CharacterPrompt(AuthorSource src, string[] characters, string level) => $$$"""
        You are writing a SHADOWING lesson for English learners at CEFR level {{{level}}}. Two friendly cartoon characters,
        a {{{characters[0]}}} (speaker 0) and a {{{characters[1]}}} (speaker 1), talk like ordinary people in a real everyday or study situation.
        {{{(src.IsTopic
            ? $"Topic: {src.Topic}. Write an ORIGINAL natural dialogue."
            : "SOURCE MATERIAL: a teacher attached a text or dialogue after this prompt. Keep its wording exactly, only split it into lines; if it is one speaker's text, give every line to speaker 0 and use only the first character; if a dialogue, map its two speakers to speakers 0 and 1.")}}}
        8 to 14 lines; each line is ONE natural sentence of 4 to 18 words that a learner can repeat in one breath; useful, frequent phrases for level {{{level}}}.
        Return exactly this JSON:
        {"id": "new", "kind": "character", "title": "<short English title, max 60 characters>", "level": "{{{level}}}",
         "videoId": null, "from": null, "to": null, "characters": ["{{{characters[0]}}}", "{{{characters[1]}}}"],
         "lines": [{"text": "<line>", "start": null, "end": null, "speaker": <0 or 1>}]}
        Return ONLY valid JSON, no markdown fences.
        """;

    /// <summary>Faqat haqiqatan gapirgan qahramonlar qoladi (monolog — bitta qahramon).</summary>
    public static ShadowingLesson TrimCharacters(ShadowingLesson l)
    {
        if (l.Kind != Character || l.Characters.Length < 2) return l;
        var used = l.Lines.Select(x => x.Speaker).Distinct().OrderBy(x => x).ToList();
        if (used.Count != 1) return l;
        var only = l.Characters[Math.Clamp(used[0], 0, l.Characters.Length - 1)];
        return l with { Characters = [only], Lines = l.Lines.Select(x => x with { Speaker = 0 }).ToList() };
    }
}

/// <summary>Ilovaning o'z darslari (qahramonlar) — bot orqali qo'shilganlarsiz ham bo'lim bo'sh qolmasin.</summary>
public static class ShadowingBank
{
    private static ShadowingLesson Dialogue(string id, string title, string level, string a, string b, params string[] lines) =>
        new(id, ShadowingRules.Character, title, level, null, null, null, [a, b],
            lines.Select((t, i) => new ShadowLine(t, Speaker: i % 2)).ToList());

    private static ShadowingLesson Monologue(string id, string title, string level, string who, params string[] lines) =>
        new(id, ShadowingRules.Character, title, level, null, null, null, [who], lines.Select(t => new ShadowLine(t)).ToList());

    public static readonly ShadowingLesson[] Lessons =
    [
        Dialogue("c-cafe", "At the café", "A2", "cat", "bear",
            "Good morning! What would you like to drink?",
            "Hi! Can I have a large hot chocolate, please?",
            "Of course. Would you like some cream on top?",
            "Yes, please. And a piece of honey cake.",
            "Great choice. Is that for here or to go?",
            "For here. I'm meeting a friend at ten.",
            "No problem. That's six dollars, please.",
            "Here you are. Thanks a lot!"),
        Dialogue("c-weekend", "Weekend plans", "A2", "fox", "penguin",
            "What are you doing this weekend?",
            "I'm going to the beach with my family.",
            "Lucky you! Isn't it too cold for swimming?",
            "Not for me. I love cold water!",
            "I'm staying at home. I need to clean my room.",
            "Why don't you come with us on Sunday?",
            "Really? That sounds like fun.",
            "Great. We're leaving at nine in the morning."),
        Dialogue("c-interview", "A job interview", "B1", "owl", "robot",
            "Thank you for coming in today. Please take a seat.",
            "Thank you for inviting me. I'm really glad to be here.",
            "Could you tell me a little about your experience?",
            "I've worked in a busy library for three years.",
            "Interesting. What did you enjoy most about that job?",
            "Helping people find exactly what they were looking for.",
            "And how do you usually deal with stress?",
            "I make a list and focus on one task at a time.",
            "That sounds sensible. Do you have any questions for us?",
            "Yes. What would a typical day look like here?"),
        Dialogue("c-directions", "Asking for directions", "B1", "penguin", "fox",
            "Excuse me, could you help me? I think I'm lost.",
            "Sure. Where are you trying to go?",
            "I'm looking for the train station.",
            "Go straight ahead and turn left at the traffic lights.",
            "Is it far from here? My bags are really heavy.",
            "It's about ten minutes on foot, or you could take the number five bus.",
            "Where's the nearest bus stop?",
            "Just across the road, in front of the bakery.",
            "Thank you so much. You've saved my day!"),
        Dialogue("c-sleep", "Screens and sleep", "B2", "owl", "robot",
            "You look exhausted. Did you stay up late again?",
            "I did. I kept scrolling through videos until two in the morning.",
            "Apparently, the light from screens makes it harder to fall asleep.",
            "I've heard that, but it's become a habit I can't seem to break.",
            "Why not leave your phone in another room overnight?",
            "Then I'd need an old-fashioned alarm clock to wake me up.",
            "That's a small price to pay for a decent night's sleep.",
            "Fair point. I'll give it a try for a week and see what happens."),
        Monologue("c-city", "Living in a big city", "B2", "bear",
            "Living in a big city has its advantages, but it certainly isn't for everyone.",
            "On the one hand, you have access to excellent jobs, museums and restaurants.",
            "Public transport often means you don't even need a car.",
            "On the other hand, rents are usually extremely high.",
            "The noise and the crowds can also be quite overwhelming.",
            "Personally, I think the best option is to live just outside the city.",
            "That way, you get peace and quiet, and the city is still within reach."),
    ];

    public static ShadowingLesson? Find(string? id) => Lessons.FirstOrDefault(l => l.Id == id);
}
