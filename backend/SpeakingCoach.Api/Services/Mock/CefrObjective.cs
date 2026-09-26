using System.Text.RegularExpressions;

namespace SpeakingCoach.Api.Services.Mock;

// CEFR (Multilevel) Listening va Reading — tuzilishi va ballash.
//
// RASMIY MANBALAR:
// 1. Vazirlar Mahkamasining 16.02.2022 dagi 73-son qarori, "Chet tilini bilish
//    darajasini aniqlash bo'yicha davlat xizmatlari ko'rsatishning ma'muriy
//    reglamenti" (lex.uz/docs/-5863626): ko'p darajali formatda "tinglab
//    tushunish" va "o'qish" bo'limlarining har biri 35 ta topshiriq;
//    vaqt — tinglash 45 daqiqa, o'qish 60 daqiqa.
// 2. Agentlikning "Baholash mezonlari" (16.03.2023): tinglash va o'qish
//    natijalari Rasch metodi bilan hisoblanadi, T = Z × 10 + 50, eng yuqori
//    ball 75 (C2 topshiriqlari yo'q).
// 3. Qismlar tuzilishi va topshiriq ko'rsatmalari — agentlik formatidagi
//    imtihon materiallari: Listening 6 qism (8 + 6 + 4 + 5 + 6 + 6),
//    har yozuv IKKI marta eshittiriladi; Reading 5 qism (6 + 8 + 6 + 9 + 6).
//
// 4. O'sha hujjatdagi daraja jadvali: "To'g'ri javoblar taxminiy soni
//    (tinglab tushunish va o'qish bo'limlari)": C1 65–75 ↔ 28–35,
//    B2 51–64 ↔ 18–27, B1 38–50 ↔ 10–17, B1 dan quyi 0–37 ↔ 0–9.
//    Umumiy natija — 4 bo'lim ballarining o'rtachasi.
//
// TAXMIN (natija sahifasida yozilgan): Rasch uchun savollarning qiyinlik
// statistikasi kerak — bizda yo'q. Shuning uchun to'g'ri javoblar soni
// 4-banddagi rasmiy oraliqlar ichida chiziqli o'tkaziladi: daraja rasmiy
// jadvalga to'liq mos, oraliq ichidagi aniq ball — taxminiy.

public record CefrPartLayout(int Part, int First, int Count);

public static partial class CefrObjective
{
    public const int Questions = 35;
    public const int ListeningMinutes = 45;
    public const int ReadingMinutes = 60;

    public static readonly CefrPartLayout[] ListeningLayout =
        [new(1, 1, 8), new(2, 9, 6), new(3, 15, 4), new(4, 19, 5), new(5, 24, 6), new(6, 30, 6)];

    public static readonly CefrPartLayout[] ReadingLayout =
        [new(1, 1, 6), new(2, 7, 8), new(3, 15, 6), new(4, 21, 9), new(5, 30, 6)];

    /// <summary>Rasmiy jadval: (to'g'ri javoblar oralig'i) ↔ (ball oralig'i).</summary>
    public static readonly (int RawFrom, int RawTo, int ScoreFrom, int ScoreTo)[] Bands =
        [(0, 9, 0, 37), (10, 17, 38, 50), (18, 27, 51, 64), (28, 35, 65, 75)];

    /// <summary>
    /// To'g'ri javoblar soni (35 dan) → 0–75. Daraja chegaralari rasmiy
    /// jadvalga mos; oraliq ichida chiziqli (taxmin, rasmiy — Rasch).
    /// </summary>
    public static int Score(int correct, int total = Questions)
    {
        if (total <= 0) return 0;
        var raw = total == Questions ? Math.Clamp(correct, 0, Questions) : (int)Math.Round(Math.Clamp(correct, 0, total) * (decimal)Questions / total, MidpointRounding.AwayFromZero);
        var (r0, r1, s0, s1) = Bands.First(b => raw <= b.RawTo);
        if (r1 == r0) return s0;
        return s0 + (int)Math.Round((raw - r0) * (decimal)(s1 - s0) / (r1 - r0), MidpointRounding.AwayFromZero);
    }

    /// <summary>To'liq imtihon: 4 bo'lim ballarining o'rtachasi (rasmiy), butun songa yaxlitlanadi.</summary>
    public static int Overall(IReadOnlyCollection<int> sections) =>
        sections.Count == 0 ? 0 : (int)Math.Round(sections.Average(x => (decimal)x), MidpointRounding.AwayFromZero);

    // ---- Topshiriq ko'rsatmalari (imtihon materiallaridagi matn) ----

    public const string L1 = "You will hear some sentences. You will hear each sentence twice. Choose the correct reply to each sentence (A, B or C).";
    public const string L2 = "You will hear someone giving a talk. For each question, fill in the missing information in the numbered space. Write ONE WORD and / or A NUMBER for each answer.";
    public const string L3 = "You will hear people speaking in different situations. Match each speaker (15–18) to the statements (A–F). There are TWO EXTRA options which you do not need to use.";
    public const string L4 = "You will hear someone giving a talk. Label the places (19–23) on the map (A–H). There are THREE EXTRA options which you do not need to use.";
    public const string L5 = "You will hear three extracts. Choose the correct answer (A, B or C) for each question (24–29). There are TWO questions for each extract.";
    public const string L6 = "You will hear a part of a lecture. For each question, fill in the missing information in the numbered space. Write no more than ONE WORD for each answer.";

    public const string R1 = "Read the text. Fill in each gap with ONE word. You must use a word which is somewhere in the rest of the text.";
    public const string R2 = "Read the texts 7–14 and the statements A–J. Decide which text matches each statement. Each statement can be used ONCE only. There are TWO extra statements which you do not need to use.";
    public const string R3 = "Read the text. Choose the correct heading for each paragraph (15–20) from the list of headings (A–H). There are TWO extra headings which you do not need to use.";
    public const string R4Mcq = "Read the text. For questions 21–24, choose the correct answer A, B, C or D.";
    public const string R4Tfng = "For questions 25–29, decide if the statements agree with the information in the text. Write TRUE, FALSE or NOT GIVEN.";
    public const string R5Gap = "Read the text. Complete the summary (30–33). Write no more than ONE WORD and / or A NUMBER for each answer.";
    public const string R5Mcq = "For questions 34–35, choose the correct answer A, B, C or D.";

    private const string Shape = """
        Question groups use exactly this JSON shape:
        {"type": "mcq" | "tfng" | "gap" | "match" | "map",
         "instructions": "<copy the instruction given below exactly>",
         "maxWords": <for gap: the word limit; otherwise null>,
         "options": ["<option text without letter>", ...] — ONLY for match (shared list A, B, C...); otherwise null,
         "map": <ONLY for map, see below; otherwise null>,
         "questions": [{"number": <int>, "prompt": "<question text; for gap it must contain ___ where the answer goes>",
                        "options": ["<option text without letter>", ...] only for mcq, otherwise null,
                        "answers": ["<correct answer>", "<accepted alternative, optional>"],
                        "explanation": "<one short sentence quoting the exact words that prove the answer>"}]}
        Rules: mcq, match and map answers are a single capital letter. tfng answers are TRUE, FALSE or NOT GIVEN.
        gap answers must be copied EXACTLY from the text/script and respect maxWords; add number alternatives such as "15" and "fifteen".
        In a match or map group every letter is used at most once. Every question has exactly one correct answer.
        Return ONLY valid JSON, no markdown fences.
        """;

    private const string Level = "The test is the Uzbekistan national Multilevel English exam (CEFR B1–C1): difficulty rises from B1 in the first questions to C1 in the last ones. Write ORIGINAL material; do not copy published tests.";

    public static string ListeningPrompt(int part, string topic)
    {
        var body = part switch
        {
            1 => $"""
                Part 1 (questions 1–8). The script is EIGHT separate short sentences (one per question, 6–20 words each), each said by a different everyday speaker, e.g. "Would you mind closing the window?".
                The script has exactly 8 lines, in question order. For each question write "prompt": "Choose the correct reply." and three short possible replies (options); only one is a natural, appropriate reply.
                One mcq group, instructions: "{L1}"
                """,
            2 => $"""
                Part 2 (questions 9–14). A monologue (250–450 words), someone giving a short talk about: {topic}.
                One gap group (maxWords 2), instructions: "{L2}". Prompts are short note lines such as "Tickets cost ___ for students." Answers appear in the talk in question order.
                """,
            3 => $"""
                Part 3 (questions 15–18). FOUR different speakers (60–100 words each) talk about: {topic}. Script speakers are named "Speaker 1" … "Speaker 4".
                One match group: six options A–F (short statements, e.g. about each speaker's opinion, job or place), four questions with prompts "Speaker 1" … "Speaker 4". Two options are not used, but mention ideas close to them as distractors.
                Instructions: "{L3}"
                """,
            4 => $"""
                Part 4 (questions 19–23). A guide describes the layout of {topic} using directions (next to, opposite, behind, on the corner of, at the end of the path…). Script 200–450 words.
                One map group. "map": {"{"}"title": "<place name>", "landmarks": [{"{"}"label": "<short name, e.g. Main entrance>", "x": 0-4, "y": 0-4{"}"}, …3–4 items],
                "spots": [{"{"}"label": "A", "x": 0-4, "y": 0-4{"}"}, … exactly 8 spots labelled A to H]{"}"}. x grows to the right, y grows downwards; y = 4 is the bottom (where the entrance usually is). Every spot and landmark has its own cell.
                The directions in the script MUST match these coordinates exactly. Five questions whose prompts are place names (e.g. "Café"); the answer is the letter of the spot. Three spots are not used.
                Instructions: "{L4}"
                """,
            5 => $"""
                Part 5 (questions 24–29). THREE separate extracts (100–180 words each), each a short conversation or monologue on a different subject related to: {topic}.
                Before each extract add a narrator line (speaker "Narrator") saying "Extract One." / "Extract Two." / "Extract Three.".
                One mcq group, two questions per extract (24–25 extract one, 26–27 extract two, 28–29 extract three), three options each, testing attitude, opinion, purpose or gist.
                Instructions: "{L5}"
                """,
            _ => $"""
                Part 6 (questions 30–35). Part of a university lecture (400–650 words) on: {topic}.
                One gap group (maxWords 1), instructions: "{L6}". Prompts are lecture-note lines such as "The first ___ were built in 1850." Answers appear in the lecture in question order.
                """,
        };
        var layout = ListeningLayout[part - 1];
        return $"""
            You are an experienced item writer for English listening exams. Create Listening Part {part} of a mock test.
            {Level}
            {body}
            Questions are numbered {layout.First} to {layout.First + layout.Count - 1}.
            Return: {"{"}"part": {part}, "context": "<one sentence the candidate reads before listening>",
                     "script": [{"{"}"speaker": "<name or role>", "voice": "male" | "female", "text": "<what they say>"{"}"}],
                     "groups": [<question groups>]{"}"}
            {Shape}
            """;
    }

    public static string ReadingPrompt(int part, string topic)
    {
        var body = part switch
        {
            1 => $"""
                Part 1 (questions 1–6). A short text (180–280 words), e.g. an email, notice or short article about: {topic}.
                Remove SIX words from the text and replace each with a numbered gap written exactly as "(1) ______" … "(6) ______".
                Each missing word MUST also appear somewhere else in the text (the candidate finds it there).
                One gap group (maxWords 1), instructions: "{R1}". Each prompt is the sentence containing the gap, with ___ in place of the gap.
                """,
            2 => $"""
                Part 2 (questions 7–14). EIGHT short texts (40–80 words each), e.g. descriptions of people, adverts or places related to: {topic}.
                In "text" start each short text on its own paragraph with its number: "7. …", "8. …" … "14. …".
                One match group: TEN statements A–J as "options" (e.g. "This person wants to learn a new skill."), eight questions with prompts "Text 7" … "Text 14".
                Instructions: "{R2}"
                """,
            3 => $"""
                Part 3 (questions 15–20). An article (400–600 words) about: {topic}, in SIX paragraphs.
                In "text" start each paragraph with its number: "15. …" … "20. …".
                One match group: EIGHT headings A–H as "options", six questions with prompts "Paragraph 15" … "Paragraph 20".
                Instructions: "{R3}"
                """,
            4 => $"""
                Part 4 (questions 21–29). An article (550–800 words) about: {topic}.
                Two groups: an mcq group with questions 21–24 (four options A–D each), instructions: "{R4Mcq}";
                and a tfng group with questions 25–29, instructions: "{R4Tfng}".
                """,
            _ => $"""
                Part 5 (questions 30–35). An academic-style article (550–800 words) about: {topic}.
                Two groups: a gap group, questions 30–33, maxWords 2 — prompts are sentences of a summary of the text, each with ___ — instructions: "{R5Gap}";
                and an mcq group with questions 34–35 (four options A–D each), instructions: "{R5Mcq}".
                """,
        };
        return $"""
            You are an experienced item writer for English reading exams. Create Reading Part {part} of a mock test.
            {Level}
            {body}
            Return: {"{"}"title": "<short title>", "text": "<the text; separate paragraphs with a blank line>", "groups": [<question groups>]{"}"}
            {Shape}
            """;
    }

    private static readonly (int Min, int Max)[] ListeningWords = [(50, 260), (220, 600), (220, 600), (180, 600), (320, 800), (350, 850)];
    private static readonly (int Min, int Max)[] ReadingWords = [(150, 380), (300, 800), (350, 800), (480, 1000), (480, 1000)];

    private static readonly string[][] ListeningTypes = [["mcq"], ["gap"], ["match"], ["map"], ["mcq"], ["gap"]];
    private static readonly string[][] ReadingTypes = [["gap"], ["match"], ["match"], ["mcq", "tfng"], ["gap", "mcq"]];

    private static void CheckTypes(List<QuestionGroup> groups, string[] types, string where)
    {
        var got = groups.Select(g => g.Type).ToArray();
        if (!got.SequenceEqual(types))
            throw new InvalidOperationException($"{where}: savol turlari [{string.Join(",", got)}], kutilgan [{string.Join(",", types)}]");
    }

    private static void CheckMcqOptions(List<QuestionGroup> groups, int count)
    {
        foreach (var q in groups.Where(g => g.Type == "mcq").SelectMany(g => g.Questions))
        {
            if (q.Options?.Count != count) throw new InvalidOperationException($"{q.Number}-savolda {count} ta variant bo'lishi kerak");
        }
    }

    public static void ValidateListening(ListeningPart p, int part)
    {
        var layout = ListeningLayout[part - 1];
        if (p.Part != part) throw new InvalidOperationException($"Qism raqami {p.Part}, kutilgan {part}");
        if (p.Script.Count == 0 || p.Script.Any(l => string.IsNullOrWhiteSpace(l.Text)))
            throw new InvalidOperationException("Skript bo'sh");
        if (p.Script.Any(l => l.Voice is not ("male" or "female"))) throw new InvalidOperationException("voice male/female bo'lishi kerak");
        var text = string.Join(" ", p.Script.Select(l => l.Text));
        var words = ObjectiveValidator.Words(text);
        var (min, max) = ListeningWords[part - 1];
        if (words < min || words > max) throw new InvalidOperationException($"Skript uzunligi mos emas: {words} so'z");
        CheckTypes(p.Groups, ListeningTypes[part - 1], $"Listening {part}");
        if (part == 1 && p.Script.Count != layout.Count)
            throw new InvalidOperationException($"1-qismda {layout.Count} ta gap bo'lishi kerak, keldi {p.Script.Count}");
        if (part is 1 or 5) CheckMcqOptions(p.Groups, 3);
        if (part == 3 && p.Groups[0].Options?.Count != 6) throw new InvalidOperationException("3-qismda 6 ta variant (A–F) bo'lishi kerak");
        if (part == 4 && p.Groups[0].Map?.Spots.Count != 8) throw new InvalidOperationException("4-qismda xaritada 8 ta joy (A–H) bo'lishi kerak");
        ObjectiveValidator.ValidateGroups(p.Groups, layout.First, layout.Count, text, allowYesNo: false);
    }

    public static void ValidateReading(ReadingPassage p, int part)
    {
        var layout = ReadingLayout[part - 1];
        if (string.IsNullOrWhiteSpace(p.Title)) throw new InvalidOperationException("Sarlavha yo'q");
        var words = ObjectiveValidator.Words(p.Text);
        var (min, max) = ReadingWords[part - 1];
        if (words < min || words > max) throw new InvalidOperationException($"Matn uzunligi mos emas: {words} so'z");
        CheckTypes(p.Groups, ReadingTypes[part - 1], $"Reading {part}");
        CheckMcqOptions(p.Groups, 4);
        switch (part)
        {
            case 1:
                for (var n = layout.First; n < layout.First + layout.Count; n++)
                {
                    if (!p.Text.Contains($"({n})", StringComparison.Ordinal))
                        throw new InvalidOperationException($"Matnda ({n}) bo'sh joy yo'q");
                }
                // Rasmiy qoida: so'z matnning boshqa joyida bor — alohida so'z sifatida.
                foreach (var q in p.Groups[0].Questions)
                {
                    if (!q.Answers.Any(a => HasWord(p.Text, a)))
                        throw new InvalidOperationException($"{q.Number}-javob matnning boshqa joyida yo'q: {q.Answers[0]}");
                }
                break;
            case 2:
                if (p.Groups[0].Options?.Count != 10) throw new InvalidOperationException("2-qismda 10 ta gap (A–J) bo'lishi kerak");
                RequireNumberedParagraphs(p.Text, layout);
                break;
            case 3:
                if (p.Groups[0].Options?.Count != 8) throw new InvalidOperationException("3-qismda 8 ta sarlavha (A–H) bo'lishi kerak");
                RequireNumberedParagraphs(p.Text, layout);
                break;
        }
        ObjectiveValidator.ValidateGroups(p.Groups, layout.First, layout.Count, p.Text, allowYesNo: false);
    }

    public static bool HasWord(string text, string word)
    {
        var w = ObjectiveGrading.Normalize(word);
        return w.Length > 0 && Regex.IsMatch(text, $@"(?<![\p{{L}}\d]){Regex.Escape(w)}(?![\p{{L}}\d])", RegexOptions.IgnoreCase);
    }

    private static void RequireNumberedParagraphs(string text, CefrPartLayout layout)
    {
        var paragraphs = text.Split(["\n\n", "\r\n\r\n"], StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToList();
        for (var n = layout.First; n < layout.First + layout.Count; n++)
        {
            if (!paragraphs.Any(x => x.StartsWith($"{n}.", StringComparison.Ordinal) || x.StartsWith($"{n} ", StringComparison.Ordinal)))
                throw new InvalidOperationException($"{n}-raqamli xatboshi yo'q");
        }
    }

    // ---- Mavzular (umumiy, O'zbekistondagi o'quvchiga tanish) ----

    public static readonly string[] Topics =
    [
        "a new public library opening in the city", "healthy eating for students", "volunteering in the local community",
        "the history of the Silk Road cities", "learning a foreign language online", "saving water at home", "working from home",
        "a weekend trip to the mountains", "the benefits of reading for children", "a science museum for young people",
        "planning a career after university", "the future of electric cars", "traditional crafts and modern design",
        "how sleep affects learning", "city parks and public spaces", "sport clubs for teenagers", "shopping habits in the digital age",
    ];

    public static readonly string[] MapPlaces =
    [
        "a new city park", "a university campus", "a sports centre", "a shopping centre", "a museum", "a hotel and its grounds", "a zoo",
    ];
}

public record CefrPartScore(int Part, int Score, int Total);

/// <summary>CEFR Listening/Reading natijasi. Overall — 0–75 (taxminiy), Level — C1/B2/B1/below B1.</summary>
public record CefrObjectiveResult(
    string Exam, string Module, string TestId, string Variant, int Score, int Total, int Overall, string Level, int SecondsUsed,
    List<CefrPartScore> Parts, List<QuestionReview> Questions);
