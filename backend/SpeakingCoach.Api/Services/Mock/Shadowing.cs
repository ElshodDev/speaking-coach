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

/// <summary>
/// Bitta gap. Tr — tarjimalar ("uz", "ru"); Keys — o'rganishga arziydigan
/// 0–2 so'z (qatorda rang bilan ajratiladi). Ikkalasi ham ixtiyoriy.
/// </summary>
public record ShadowLine(
    [property: JsonPropertyName("text")] string Text,
    [property: JsonPropertyName("start")] double? Start = null,
    [property: JsonPropertyName("end")] double? End = null,
    [property: JsonPropertyName("speaker")] int Speaker = 0,
    [property: JsonPropertyName("tr")] Dictionary<string, string>? Tr = null,
    [property: JsonPropertyName("keys")] string[]? Keys = null);

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

    private static string Key(string text) => Regex.Replace(text.ToLowerInvariant(), @"[^a-z0-9']+", " ").Trim();

    /// <summary>
    /// AI ba'zan bir gapni ketma-ket bir necha marta yozib yuboradi (transkripsiya
    /// "tiqilib qolishi"). Bo'sh gaplar olib tashlanadi, ketma-ket takrorlar
    /// bittaga qisqaradi (birinchisining vaqti qoladi).
    /// </summary>
    public static List<ShadowLine> CleanLines(List<ShadowLine> lines)
    {
        var result = new List<ShadowLine>();
        foreach (var l in lines)
        {
            var text = (l.Text ?? "").Trim();
            if (text.Length == 0) continue;
            if (result.Count > 0 && Key(result[^1].Text) == Key(text)) continue;
            result.Add(Extras(l with { Text = text }));
        }
        return result;
    }

    public static readonly string[] TranslationLangs = ["uz", "ru"];

    /// <summary>
    /// Tarjima va kalit so'zlarni tartibga soladi: faqat uz/ru, 300 belgigacha;
    /// kalit so'z — shu gapda haqiqatan bor so'z, ko'pi bilan 3 ta.
    /// </summary>
    public static ShadowLine Extras(ShadowLine l)
    {
        Dictionary<string, string>? tr = null;
        if (l.Tr is { Count: > 0 })
        {
            tr = l.Tr
                .Where(kv => TranslationLangs.Contains(kv.Key) && !string.IsNullOrWhiteSpace(kv.Value))
                .ToDictionary(kv => kv.Key, kv => kv.Value.Trim().Length > 300 ? kv.Value.Trim()[..300] : kv.Value.Trim());
            if (tr.Count == 0) tr = null;
        }
        string[]? keys = null;
        if (l.Keys is { Length: > 0 })
        {
            var words = Tokens(l.Text).Select(Norm).ToHashSet();
            keys = l.Keys
                .Select(k => (k ?? "").Trim())
                .Where(k => k.Length > 1 && words.Contains(Norm(k)))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(3)
                .ToArray();
            if (keys.Length == 0) keys = null;
        }
        return l with { Tr = tr, Keys = keys };
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

        // Bir xil gap ko'p marta — ishonchsiz transkripsiya belgisi.
        var repeats = l.Lines.GroupBy(x => Key(x.Text)).Max(g => g.Count());
        Require(repeats <= Math.Max(2, l.Lines.Count / 5), $"bir xil gap {repeats} marta takrorlangan — videoni qayta, diqqat bilan tinglab yozing");

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
         "lines": [{"text": "<line>", "start": <seconds>, "end": <seconds>, "speaker": 0, {{{ExtrasShape}}}}]}
        {{{ExtrasRule}}}
        Return ONLY valid JSON, no markdown fences.
        """;

    private const string ExtrasShape = "\"tr\": {\"uz\": \"<natural Uzbek translation, Latin script with oʻ gʻ and ʼ>\", \"ru\": \"<natural Russian translation>\"}, \"keys\": [\"<0 to 2 words from this line worth learning>\"]";

    private const string ExtrasRule = "Translations are natural and short, not word for word. \"keys\" are useful words for this level copied exactly from the line (skip very common words; an empty list is fine).";

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
         "lines": [{"text": "<line>", "start": null, "end": null, "speaker": <0 or 1>, {{{ExtrasShape}}}}]}
        {{{ExtrasRule}}}
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
    /// <summary>Gap: matn, o'zbekcha va ruscha tarjima, o'rganishga arziydigan so'zlar.</summary>
    private static ShadowLine L(string text, string uz, string ru, params string[] keys) =>
        new(text, Tr: new Dictionary<string, string> { ["uz"] = uz, ["ru"] = ru }, Keys: keys.Length == 0 ? null : keys);

    private static ShadowingLesson Dialogue(string id, string title, string level, string a, string b, params ShadowLine[] lines) =>
        new(id, ShadowingRules.Character, title, level, null, null, null, [a, b],
            lines.Select((l, i) => l with { Speaker = i % 2 }).ToList());

    private static ShadowingLesson Monologue(string id, string title, string level, string who, params ShadowLine[] lines) =>
        new(id, ShadowingRules.Character, title, level, null, null, null, [who], lines.ToList());

    public static readonly ShadowingLesson[] Lessons =
    [
        Dialogue("c-cafe", "At the café", "A2", "cat", "bear",
            L("Good morning! What would you like to drink?", "Xayrli tong! Nima ichishni xohlaysiz?", "Доброе утро! Что будете пить?"),
            L("Hi! Can I have a large hot chocolate, please?", "Salom! Menga katta issiq shokolad bera olasizmi?", "Привет! Можно мне большой горячий шоколад?", "chocolate"),
            L("Of course. Would you like some cream on top?", "Albatta. Ustiga qaymoq qoʻshaymi?", "Конечно. Добавить сверху сливки?", "cream"),
            L("Yes, please. And a piece of honey cake.", "Ha, iltimos. Yana bir boʻlak asalli tort.", "Да, пожалуйста. И кусочек медового торта.", "piece", "honey"),
            L("Great choice. Is that for here or to go?", "Zoʻr tanlov. Shu yerda yeysizmi yoki olib ketasizmi?", "Отличный выбор. Здесь или с собой?", "choice"),
            L("For here. I'm meeting a friend at ten.", "Shu yerda. Soat oʻnda doʻstim bilan uchrashaman.", "Здесь. В десять я встречаюсь с другом.", "meeting"),
            L("No problem. That's six dollars, please.", "Muammo yoʻq. Olti dollar boʻladi.", "Без проблем. С вас шесть долларов."),
            L("Here you are. Thanks a lot!", "Mana, marhamat. Katta rahmat!", "Вот, пожалуйста. Большое спасибо!")),
        Dialogue("c-weekend", "Weekend plans", "A2", "fox", "penguin",
            L("What are you doing this weekend?", "Shu dam olish kunlari nima qilyapsan?", "Что ты делаешь на этих выходных?", "weekend"),
            L("I'm going to the beach with my family.", "Oilam bilan dengiz boʻyiga boraman.", "Я еду на пляж с семьёй.", "beach"),
            L("Lucky you! Isn't it too cold for swimming?", "Omading bor ekan! Suzish uchun juda sovuq emasmi?", "Везёт тебе! Не слишком холодно для купания?", "lucky"),
            L("Not for me. I love cold water!", "Men uchun emas. Men sovuq suvni yaxshi koʻraman!", "Не для меня. Я обожаю холодную воду!"),
            L("I'm staying at home. I need to clean my room.", "Men uyda qolaman. Xonamni yigʻishtirishim kerak.", "Я остаюсь дома. Мне нужно убраться в комнате.", "clean"),
            L("Why don't you come with us on Sunday?", "Yakshanba kuni biz bilan bormaysanmi?", "Почему бы тебе не поехать с нами в воскресенье?"),
            L("Really? That sounds like fun.", "Rostdanmi? Qiziq boʻlsa kerak.", "Правда? Звучит весело.", "sounds"),
            L("Great. We're leaving at nine in the morning.", "Zoʻr. Ertalab soat toʻqqizda yoʻlga chiqamiz.", "Отлично. Мы выезжаем в девять утра.", "leaving")),
        Dialogue("c-interview", "A job interview", "B1", "owl", "robot",
            L("Thank you for coming in today. Please take a seat.", "Bugun kelganingiz uchun rahmat. Marhamat, oʻtiring.", "Спасибо, что пришли сегодня. Присаживайтесь, пожалуйста.", "seat"),
            L("Thank you for inviting me. I'm really glad to be here.", "Taklif qilganingiz uchun rahmat. Bu yerda boʻlganimdan juda xursandman.", "Спасибо за приглашение. Я очень рад быть здесь.", "inviting", "glad"),
            L("Could you tell me a little about your experience?", "Tajribangiz haqida biroz gapirib bera olasizmi?", "Не могли бы вы немного рассказать о своём опыте?", "experience"),
            L("I've worked in a busy library for three years.", "Men uch yil gavjum kutubxonada ishlaganman.", "Я три года работал в оживлённой библиотеке.", "busy", "library"),
            L("Interesting. What did you enjoy most about that job?", "Qiziq. Oʻsha ishda sizga eng koʻp nima yoqardi?", "Интересно. Что вам больше всего нравилось в той работе?", "enjoy"),
            L("Helping people find exactly what they were looking for.", "Odamlarga aynan izlagan narsasini topishda yordam berish.", "Помогать людям находить именно то, что они искали.", "exactly"),
            L("And how do you usually deal with stress?", "Stressni odatda qanday yengasiz?", "А как вы обычно справляетесь со стрессом?", "deal", "stress"),
            L("I make a list and focus on one task at a time.", "Roʻyxat tuzaman va har safar bitta vazifaga eʼtibor qarataman.", "Я составляю список и сосредотачиваюсь на одной задаче за раз.", "focus", "task"),
            L("That sounds sensible. Do you have any questions for us?", "Oqilona fikr. Bizga savollaringiz bormi?", "Звучит разумно. У вас есть к нам вопросы?", "sensible"),
            L("Yes. What would a typical day look like here?", "Ha. Bu yerda odatiy kun qanday oʻtadi?", "Да. Как здесь выглядит обычный рабочий день?", "typical")),
        Dialogue("c-directions", "Asking for directions", "B1", "penguin", "fox",
            L("Excuse me, could you help me? I think I'm lost.", "Kechirasiz, yordam bera olasizmi? Adashib qoldim shekilli.", "Извините, не могли бы вы помочь? Кажется, я заблудился.", "lost"),
            L("Sure. Where are you trying to go?", "Albatta. Qayerga bormoqchisiz?", "Конечно. Куда вы хотите попасть?"),
            L("I'm looking for the train station.", "Temir yoʻl vokzalini qidiryapman.", "Я ищу железнодорожный вокзал.", "station"),
            L("Go straight ahead and turn left at the traffic lights.", "Toʻgʻriga yuring va svetoforda chapga buriling.", "Идите прямо и на светофоре поверните налево.", "straight", "traffic"),
            L("Is it far from here? My bags are really heavy.", "Bu yerdan uzoqmi? Sumkalarim juda ogʻir.", "Это далеко отсюда? У меня очень тяжёлые сумки.", "heavy"),
            L("It's about ten minutes on foot, or you could take the number five bus.", "Piyoda taxminan oʻn daqiqa, yoki beshinchi avtobusga chiqishingiz mumkin.", "Минут десять пешком, или можно сесть на пятый автобус.", "foot"),
            L("Where's the nearest bus stop?", "Eng yaqin avtobus bekati qayerda?", "Где ближайшая автобусная остановка?", "nearest"),
            L("Just across the road, in front of the bakery.", "Yoʻlning naryogʻida, novvoyxona oldida.", "Прямо через дорогу, напротив пекарни.", "across", "bakery"),
            L("Thank you so much. You've saved my day!", "Katta rahmat. Meni qutqardingiz!", "Большое спасибо. Вы меня очень выручили!", "saved")),
        Dialogue("c-sleep", "Screens and sleep", "B2", "owl", "robot",
            L("You look exhausted. Did you stay up late again?", "Juda charchagan koʻrinasan. Yana kech yotdingmi?", "Ты выглядишь измотанным. Опять допоздна не спал?", "exhausted"),
            L("I did. I kept scrolling through videos until two in the morning.", "Ha. Tungi soat ikkigacha videolarni varaqlab oʻtirdim.", "Да. Листал видео до двух часов ночи.", "scrolling"),
            L("Apparently, the light from screens makes it harder to fall asleep.", "Aytishlaricha, ekran nuri uxlab qolishni qiyinlashtirar ekan.", "Говорят, свет от экранов мешает заснуть.", "apparently", "asleep"),
            L("I've heard that, but it's become a habit I can't seem to break.", "Eshitganman, lekin bu tashlay olmayotgan odatimga aylanib qoldi.", "Я слышал, но это стало привычкой, от которой никак не избавиться.", "habit"),
            L("Why not leave your phone in another room overnight?", "Telefoningni tunda boshqa xonada qoldirsang-chi?", "Почему бы не оставлять телефон на ночь в другой комнате?", "overnight"),
            L("Then I'd need an old-fashioned alarm clock to wake me up.", "Unda meni uygʻotish uchun eski usuldagi budilnik kerak boʻladi.", "Тогда мне понадобится старомодный будильник.", "alarm"),
            L("That's a small price to pay for a decent night's sleep.", "Yaxshi uyqu uchun bu arzimas narsa.", "Это небольшая плата за нормальный сон.", "price", "decent"),
            L("Fair point. I'll give it a try for a week and see what happens.", "Toʻgʻri gap. Bir hafta sinab koʻraman, nima boʻlishini koʻramiz.", "Справедливо. Попробую неделю и посмотрю, что будет.", "fair")),
        Monologue("c-city", "Living in a big city", "B2", "bear",
            L("Living in a big city has its advantages, but it certainly isn't for everyone.", "Katta shaharda yashashning afzalliklari bor, lekin bu hamma uchun emas.", "У жизни в большом городе есть свои плюсы, но она точно подходит не всем.", "advantages", "certainly"),
            L("On the one hand, you have access to excellent jobs, museums and restaurants.", "Bir tomondan, zoʻr ish joylari, muzeylar va restoranlar qoʻl ostingizda.", "С одной стороны, у вас есть доступ к отличной работе, музеям и ресторанам.", "access"),
            L("Public transport often means you don't even need a car.", "Jamoat transporti tufayli koʻpincha mashina ham kerak boʻlmaydi.", "Благодаря общественному транспорту часто даже не нужна машина.", "transport"),
            L("On the other hand, rents are usually extremely high.", "Boshqa tomondan, ijara narxlari odatda juda baland.", "С другой стороны, аренда обычно очень дорогая.", "rents", "extremely"),
            L("The noise and the crowds can also be quite overwhelming.", "Shovqin va olomon ham ancha charchatishi mumkin.", "Шум и толпы тоже могут изрядно утомлять.", "crowds", "overwhelming"),
            L("Personally, I think the best option is to live just outside the city.", "Menimcha, eng yaxshi yoʻl — shahar chetida yashash.", "Лично я считаю, что лучше всего жить недалеко от города.", "personally", "option"),
            L("That way, you get peace and quiet, and the city is still within reach.", "Shunda ham tinchlik-osoyishtalik boʻladi, ham shahar yaqin boʻladi.", "Так у вас есть тишина и покой, а город всё равно рядом.", "peace", "reach")),
    ];

    public static ShadowingLesson? Find(string? id) => Lessons.FirstOrDefault(l => l.Id == id);
}
