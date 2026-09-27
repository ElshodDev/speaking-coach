using System.Text;
using System.Text.Json;

namespace SpeakingCoach.Api.Services.Mock;

/// <summary>Qo'shiladigan test turi: imtihon, bo'lim va (IELTS Reading/Writing uchun) variant.</summary>
public record AuthorKind(string Exam, string Module, string Variant)
{
    public string Key => $"{Exam}:{Module}:{Variant}";

    public static readonly AuthorKind[] All =
    [
        new("ielts", "speaking", ""),
        new("ielts", "writing", IeltsBank.Academic),
        new("ielts", "writing", IeltsBank.General),
        new("ielts", "reading", IeltsBank.Academic),
        new("ielts", "reading", IeltsBank.General),
        new("ielts", "listening", ""),
        new("cefr", "speaking", ""),
        new("cefr", "writing", ""),
        new("cefr", "reading", ""),
        new("cefr", "listening", ""),
    ];

    public static AuthorKind? Parse(string? key) => All.FirstOrDefault(k => k.Key == key);

    public bool Objective => Module is "reading" or "listening";

    /// <summary>"IELTS Reading (Academic)", "CEFR Speaking".</summary>
    public string Label => $"{(Exam == "cefr" ? "CEFR" : "IELTS")} {char.ToUpperInvariant(Module[0])}{Module[1..]}"
        + (Variant.Length > 0 ? $" ({char.ToUpperInvariant(Variant[0])}{Variant[1..]})" : "");
}

/// <summary>
/// O'qituvchi yuborgan narsa: matn, fayl (PDF, rasm, .txt) yoki faqat mavzu.
/// Topic bo'lsa — Gemini test yaratadi; aks holda materialni formatga o'giradi.
/// </summary>
public record AuthorSource(string? Text, byte[]? File, string? MimeType, string? Topic)
{
    public bool IsTopic => Topic is not null;
}

/// <summary>Tayyor test: bazaga yoziladigan JSON va qisqa nom.</summary>
public record AuthoredTest(AuthorKind Kind, string Payload, string Title);

public interface IMockAuthoring
{
    Task<AuthoredTest> CreateAsync(AuthorKind kind, AuthorSource source, CancellationToken ct = default);
}

/// <summary>
/// Bot orqali test qo'shish — toza qismlar: fayl qabul qilish qoidalari,
/// Speaking/Writing uchun prompt va qat'iy tekshiruv, ko'rib chiqish matni.
/// </summary>
public static class MockAuthoringRules
{
    public const int MaxFileBytes = 5 * 1024 * 1024; // L/R qismlari parallel — har so'rovda nusxa (Render xotirasi)
    public const int MaxTextChars = 60_000;
    public const int MaxTopicChars = 200;

    /// <summary>Telegram'dan qabul qilinadigan fayl turlari (Gemini o'qiy oladiganlari).</summary>
    public static string? NormalizeMime(string? mime, string? fileName)
    {
        var m = (mime ?? "").ToLowerInvariant();
        var ext = Path.GetExtension(fileName ?? "").ToLowerInvariant();
        if (m == "application/pdf" || ext == ".pdf") return "application/pdf";
        if (m is "image/jpeg" or "image/jpg" || ext is ".jpg" or ".jpeg") return "image/jpeg";
        if (m == "image/png" || ext == ".png") return "image/png";
        if (m == "image/webp" || ext == ".webp") return "image/webp";
        if (m.StartsWith("text/") || ext is ".txt" or ".md") return "text/plain";
        return null;
    }

    /// <summary>Gemini'ga material qismi: fayl — inline_data, matn — alohida matn qismi.</summary>
    public static object[] SourceParts(AuthorSource src)
    {
        if (src.File is { Length: > 0 } file && src.MimeType is { } mime)
        {
            if (mime == "text/plain") return [new { text = "SOURCE MATERIAL:\n" + Truncate(Encoding.UTF8.GetString(file), MaxTextChars) }];
            return [new { inline_data = new { mime_type = mime, data = Convert.ToBase64String(file) } }];
        }
        if (!string.IsNullOrWhiteSpace(src.Text)) return [new { text = "SOURCE MATERIAL:\n" + Truncate(src.Text, MaxTextChars) }];
        return [];
    }

    private static string Truncate(string s, int max) => s.Length > max ? s[..max] : s;

    /// <summary>Materialdan o'girishda qo'shiladigan ko'rsatma (qaysi qism olinishi).</summary>
    public static string SourceNote(string partLabel) =>
        $"""
        SOURCE MATERIAL: a teacher attached exam material (text, a PDF or a photo) after this prompt. Use its {partLabel} as the basis:
        keep the original texts, script, questions and correct answers exactly where they exist (fix only obvious typos and OCR errors).
        If the material has no {partLabel} or it is incomplete (for example answers or some questions are missing), write the missing content yourself in the same style and level.
        Ignore the other parts of the material. The instructions below describe the REQUIRED output format — follow them even if the material is laid out differently.
        """;

    private static string Mode(AuthorSource src) => src.IsTopic
        ? $"Write an ORIGINAL test on this topic: {src.Topic}. Do not copy published tests."
        : SourceNote("content");

    // ---------------- Prompt'lar (Speaking va Writing) ----------------

    public static string IeltsSpeakingPrompt(AuthorSource src) => $$$"""
        You are an experienced IELTS Speaking examiner and item writer. Prepare one complete IELTS Speaking test (Part 1, Part 2 cue card, Part 3).
        {{{Mode(src)}}}
        Return exactly this JSON:
        {"id": "new",
         "part1Topic": "<a familiar everyday topic, e.g. Your home town>",
         "part1": ["<4 or 5 short questions on that topic>"],
         "part2": {"topic": "Describe <...>.", "points": ["<3 or 4 short prompts such as: where it is / when you went there>"], "explain": "and explain <...>."},
         "part3": ["<4 or 5 more abstract discussion questions linked to the Part 2 topic>"]}
        Part 1 and Part 3 items are questions ending with "?". Natural spoken English. Return ONLY valid JSON, no markdown fences.
        """;

    public static string IeltsWritingPrompt(string variant, AuthorSource src)
    {
        var task1 = variant == IeltsBank.General
            ? """
              "task1": {"prompt": "<a letter task: a situation, then 'Write a letter to ... In your letter:' and three bullet points (•), then 'Begin your letter as follows: Dear ...,'>", "chart": null},
              """
            : """
              "task1": {"prompt": "The chart below shows <...>.\n\nSummarise the information by selecting and reporting the main features, and make comparisons where relevant.",
                        "chart": {"kind": "bar", "title": "<chart title>", "unit": "<unit, e.g. % or millions>", "categories": ["<2 to 8 x-axis labels>"],
                                  "series": [{"name": "<series name>", "values": [<one number per category>]}]}},
              """;
        var chartRule = variant == IeltsBank.General
            ? "Task 1 is a letter (General Training), so chart is null."
            : "Task 1 must be a bar chart with real numbers: 1 to 5 series, each with exactly one value per category. If the material's Task 1 is a line graph, table or pie chart, put its numbers into this bar chart format. If it is a map or a process diagram, write a new bar-chart Task 1 on the same topic.";
        return $$$"""
            You are an experienced IELTS Writing item writer. Prepare one complete IELTS {{{(variant == IeltsBank.General ? "General Training" : "Academic")}}} Writing test (Task 1 and Task 2).
            {{{Mode(src)}}}
            Return exactly this JSON:
            {"id": "new", "variant": "{{{variant}}}",
             {{{task1}}} "task2": "<an essay question: a statement, then the official instruction such as 'Discuss both these views and give your own opinion.' or 'To what extent do you agree or disagree?'>"}
            {{{chartRule}}}
            Separate paragraphs inside strings with \n\n. Return ONLY valid JSON, no markdown fences.
            """;
    }

    public static string CefrSpeakingPrompt(AuthorSource src) => $$$"""
        You are an experienced item writer for the Uzbekistan national Multilevel (CEFR) Speaking exam, new format:
        Part 1.1 — 3 short personal questions; Part 1.2 — two pictures to compare and 3 questions (the first is "Describe what you can see in the two pictures.");
        Part 2 — a topic with 3 questions and one picture; Part 3 — a debatable statement with a FOR/AGAINST table.
        {{{Mode(src)}}}
        Pictures are shown to candidates as an emoji plus a short caption.
        Return exactly this JSON:
        {"id": "new",
         "part11": ["<3 questions>"],
         "pictures": [{"emoji": "<one emoji>", "caption": "Picture A: <what it shows>"}, {"emoji": "<one emoji>", "caption": "Picture B: <what it shows>"}],
         "part12": ["Describe what you can see in the two pictures.", "<a comparison/preference question>", "<a wider question>"],
         "part2Topic": "Talk about <...>.",
         "part2Questions": ["<3 questions>"],
         "part3Statement": "<a debatable statement>",
         "for": ["<3 short arguments for>"],
         "against": ["<3 short arguments against>"],
         "part2Picture": {"emoji": "<one emoji>", "caption": "A picture of <...>"}}
        Return ONLY valid JSON, no markdown fences.
        """;

    public static string CefrWritingPrompt(AuthorSource src) => $$$"""
        You are an experienced item writer for the Uzbekistan national Multilevel (CEFR) Writing exam, new format:
        Part 1 — a situation and an email the candidate received (100-160 words); Task 1.1 — an informal letter to a friend (about 50 words);
        Task 1.2 — a formal letter to the sender (120-150 words); Part 2 — an online discussion post giving an opinion (180-200 words).
        {{{Mode(src)}}}
        Return exactly this JSON:
        {"id": "new",
         "role": "<the situation, e.g. You are a member of a local sports club. You received an email from the club manager.>",
         "emailFrom": "<sender, e.g. The Club Manager>",
         "email": "<the full email with greeting and signature; separate paragraphs with \n\n>",
         "task11": "Write a letter to your friend, <...>. Write about 50 words.",
         "task12": "Write a letter to <the sender>, <...>. Write 120-150 words.",
         "task2": "You are taking part in an online discussion <...>. The question is: \"<...>\" Write a post giving your opinion with reasons and examples. Write 180-200 words."}
        Return ONLY valid JSON, no markdown fences.
        """;

    // ---------------- Tekshiruvlar ----------------

    private static void Require(bool ok, string message)
    {
        if (!ok) throw new InvalidOperationException(message);
    }

    private static bool Filled(string? s, int max = 400) => !string.IsNullOrWhiteSpace(s) && s.Length <= max;

    public static void ValidateIeltsSpeaking(SpeakingSet s)
    {
        Require(Filled(s.Part1Topic, 80), "part1Topic bo'sh yoki juda uzun");
        Require(s.Part1.Length is >= 4 and <= 6 && s.Part1.All(q => Filled(q, 200)), "part1: 4–6 ta savol kerak");
        Require(Filled(s.Part2.Topic, 200), "part2.topic bo'sh");
        Require(s.Part2.Points.Length is >= 3 and <= 4 && s.Part2.Points.All(p => Filled(p, 120)), "part2.points: 3–4 ta band kerak");
        Require(Filled(s.Part2.Explain, 200), "part2.explain bo'sh");
        Require(s.Part3.Length is >= 4 and <= 6 && s.Part3.All(q => Filled(q, 250)), "part3: 4–6 ta savol kerak");
    }

    public static void ValidateIeltsWriting(WritingSet w, string variant)
    {
        Require(w.Variant == variant, $"variant {variant} bo'lishi kerak");
        Require(Filled(w.Task1.Prompt, 1500) && w.Task1.Prompt.Length >= 40, "task1.prompt juda qisqa");
        Require(Filled(w.Task2, 1500) && w.Task2.Length >= 60, "task2 juda qisqa");
        if (variant == IeltsBank.General)
        {
            Require(w.Task1.Chart is null, "General Training Task 1 — xat, chart null bo'lishi kerak");
            Require(w.Task1.Prompt.Contains("letter", StringComparison.OrdinalIgnoreCase), "General Training Task 1 xat topshirig'i emas");
            return;
        }
        var c = w.Task1.Chart;
        Require(c is not null, "Academic Task 1 uchun chart kerak");
        Require(c!.Kind == "bar", "chart.kind \"bar\" bo'lishi kerak");
        Require(Filled(c.Title, 150), "chart.title bo'sh");
        Require(c.Categories.Length is >= 2 and <= 8 && c.Categories.All(x => Filled(x, 40)), "chart.categories: 2–8 ta");
        Require(c.Series.Length is >= 1 and <= 5, "chart.series: 1–5 ta");
        foreach (var sr in c.Series)
        {
            Require(Filled(sr.Name, 40), "series.name bo'sh");
            Require(sr.Values.Length == c.Categories.Length, $"'{sr.Name}' qiymatlari soni toifalar soniga teng emas");
            Require(sr.Values.All(v => double.IsFinite(v) && v >= 0), "qiymatlar musbat son bo'lishi kerak");
        }
    }

    public static void ValidateCefrSpeaking(CefrSpeakingSet s)
    {
        Require(s.Part11.Length == 3 && s.Part11.All(q => Filled(q, 200)), "part11: 3 ta savol kerak");
        Require(s.Pictures.Length == 2 && s.Pictures.All(p => Filled(p.Emoji, 16) && Filled(p.Caption, 200)), "pictures: 2 ta rasm (emoji + izoh) kerak");
        Require(s.Part12.Length == 3 && s.Part12.All(q => Filled(q, 200)), "part12: 3 ta savol kerak");
        Require(Filled(s.Part2Topic, 200), "part2Topic bo'sh");
        Require(s.Part2Questions.Length == 3 && s.Part2Questions.All(q => Filled(q, 200)), "part2Questions: 3 ta savol kerak");
        Require(Filled(s.Part3Statement, 250), "part3Statement bo'sh");
        Require(s.For.Length is >= 2 and <= 4 && s.For.All(x => Filled(x, 120)), "for: 2–4 ta dalil kerak");
        Require(s.Against.Length is >= 2 and <= 4 && s.Against.All(x => Filled(x, 120)), "against: 2–4 ta dalil kerak");
        Require(Filled(s.Part2Picture.Emoji, 16) && Filled(s.Part2Picture.Caption, 200), "part2Picture bo'sh");
    }

    public static void ValidateCefrWriting(CefrWritingSet w)
    {
        Require(Filled(w.Role, 400), "role bo'sh");
        Require(Filled(w.EmailFrom, 80), "emailFrom bo'sh");
        var words = ObjectiveValidator.Words(w.Email);
        Require(words is >= 40 and <= 230, $"email uzunligi mos emas: {words} so'z (40–230)");
        Require(Filled(w.Task11, 600) && w.Task11.Contains("50"), "task11: \"about 50 words\" bo'lishi kerak");
        Require(Filled(w.Task12, 600) && w.Task12.Contains("120"), "task12: \"120-150 words\" bo'lishi kerak");
        Require(Filled(w.Task2, 800) && w.Task2.Contains("180"), "task2: \"180-200 words\" bo'lishi kerak");
    }

    // ---------------- Nom va ko'rib chiqish ----------------

    private static string Short(string s, int max = 80)
    {
        var line = s.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim() ?? "";
        return line.Length > max ? line[..(max - 1)] + "…" : line;
    }

    public static string TitleOf(AuthorKind kind, string payload)
    {
        var o = MockSets.Web;
        return (kind.Exam, kind.Module) switch
        {
            ("ielts", "speaking") => Short(JsonSerializer.Deserialize<SpeakingSet>(payload, o)!.Part2.Topic),
            ("ielts", "writing") => Short(JsonSerializer.Deserialize<WritingSet>(payload, o)!.Task2),
            ("cefr", "speaking") => Short(JsonSerializer.Deserialize<CefrSpeakingSet>(payload, o)!.Part2Topic),
            ("cefr", "writing") => Short(JsonSerializer.Deserialize<CefrWritingSet>(payload, o)!.Role),
            (_, "reading") => Short(string.Join(" · ", JsonSerializer.Deserialize<ReadingTest>(payload, o)!.Passages.Select(p => p.Title)), 120),
            _ => Short(JsonSerializer.Deserialize<ListeningTest>(payload, o)!.Parts[0].Context),
        };
    }

    /// <summary>
    /// Testning to'liq, o'qiladigan matni (to'g'ri javoblar bilan) — admin
    /// yoki o'qituvchi chop etishdan oldin .txt fayl sifatida ko'rib chiqadi.
    /// </summary>
    public static string FullText(AuthorKind kind, string payload)
    {
        var o = MockSets.Web;
        var sb = new StringBuilder();
        sb.AppendLine(kind.Label).AppendLine(new string('=', kind.Label.Length)).AppendLine();
        switch (kind.Exam, kind.Module)
        {
            case ("ielts", "speaking"):
            {
                var s = JsonSerializer.Deserialize<SpeakingSet>(payload, o)!;
                sb.AppendLine($"PART 1 — {s.Part1Topic}");
                foreach (var q in s.Part1) sb.AppendLine($"  • {q}");
                sb.AppendLine().AppendLine("PART 2 (1 min preparation, up to 2 min)").AppendLine($"  {s.Part2.Topic}").AppendLine("  You should say:");
                foreach (var p in s.Part2.Points) sb.AppendLine($"    – {p}");
                sb.AppendLine($"  {s.Part2.Explain}").AppendLine().AppendLine("PART 3");
                foreach (var q in s.Part3) sb.AppendLine($"  • {q}");
                break;
            }
            case ("ielts", "writing"):
            {
                var w = JsonSerializer.Deserialize<WritingSet>(payload, o)!;
                sb.AppendLine("TASK 1").AppendLine(w.Task1.Prompt);
                if (w.Task1.Chart is { } c)
                {
                    sb.AppendLine().AppendLine($"[Chart: {c.Title}, {c.Unit}]");
                    sb.AppendLine("  " + string.Join(" | ", c.Categories.Prepend("")));
                    foreach (var sr in c.Series) sb.AppendLine("  " + string.Join(" | ", sr.Values.Select(v => v.ToString(System.Globalization.CultureInfo.InvariantCulture)).Prepend(sr.Name)));
                }
                sb.AppendLine().AppendLine("TASK 2").AppendLine(w.Task2);
                break;
            }
            case ("cefr", "speaking"):
            {
                var s = JsonSerializer.Deserialize<CefrSpeakingSet>(payload, o)!;
                sb.AppendLine("PART 1.1");
                foreach (var q in s.Part11) sb.AppendLine($"  • {q}");
                sb.AppendLine().AppendLine("PART 1.2 — pictures:");
                foreach (var p in s.Pictures) sb.AppendLine($"  {p.Emoji} {p.Caption}");
                foreach (var q in s.Part12) sb.AppendLine($"  • {q}");
                sb.AppendLine().AppendLine($"PART 2 — {s.Part2Topic}").AppendLine($"  {s.Part2Picture.Emoji} {s.Part2Picture.Caption}");
                foreach (var q in s.Part2Questions) sb.AppendLine($"  • {q}");
                sb.AppendLine().AppendLine($"PART 3 — {s.Part3Statement}").AppendLine("  FOR:");
                foreach (var x in s.For) sb.AppendLine($"    + {x}");
                sb.AppendLine("  AGAINST:");
                foreach (var x in s.Against) sb.AppendLine($"    – {x}");
                break;
            }
            case ("cefr", "writing"):
            {
                var w = JsonSerializer.Deserialize<CefrWritingSet>(payload, o)!;
                sb.AppendLine(w.Role).AppendLine().AppendLine($"From: {w.EmailFrom}").AppendLine(w.Email).AppendLine();
                sb.AppendLine("TASK 1.1").AppendLine(w.Task11).AppendLine().AppendLine("TASK 1.2").AppendLine(w.Task12).AppendLine();
                sb.AppendLine("PART 2").AppendLine(w.Task2);
                break;
            }
            case (_, "reading"):
            {
                var t = JsonSerializer.Deserialize<ReadingTest>(payload, o)!;
                var i = 0;
                foreach (var p in t.Passages)
                {
                    sb.AppendLine($"PART / PASSAGE {++i}: {p.Title}").AppendLine().AppendLine(p.Text).AppendLine();
                    AppendGroups(sb, p.Groups);
                }
                break;
            }
            default:
            {
                var t = JsonSerializer.Deserialize<ListeningTest>(payload, o)!;
                foreach (var p in t.Parts)
                {
                    sb.AppendLine($"PART {p.Part}: {p.Context}").AppendLine("Script:");
                    foreach (var l in p.Script) sb.AppendLine($"  {l.Speaker} ({l.Voice}): {l.Text}");
                    sb.AppendLine();
                    AppendGroups(sb, p.Groups);
                }
                break;
            }
        }
        return sb.ToString();
    }

    private static void AppendGroups(StringBuilder sb, IEnumerable<QuestionGroup> groups)
    {
        foreach (var g in groups)
        {
            sb.AppendLine($"[{g.Type}] {g.Instructions}");
            if (g.Options is { } opts)
                for (var k = 0; k < opts.Count; k++) sb.AppendLine($"    {(char)('A' + k)}. {opts[k]}");
            if (g.Map is { } map)
                sb.AppendLine($"    Map \"{map.Title}\": " + string.Join(", ", map.Landmarks.Select(l => $"{l.Label}({l.X},{l.Y})").Concat(map.Spots.Select(s => $"{s.Label}({s.X},{s.Y})"))));
            foreach (var q in g.Questions)
            {
                sb.AppendLine($"  {q.Number}. {q.Prompt}");
                if (q.Options is { } qo)
                    for (var k = 0; k < qo.Count; k++) sb.AppendLine($"      {(char)('A' + k)}) {qo[k]}");
                sb.AppendLine($"      ✔ {string.Join(" / ", q.Answers)} — {q.Explanation}");
            }
            sb.AppendLine();
        }
    }

    /// <summary>Qisqa ko'rinish (bot xabari uchun, ≤ 1500 belgi).</summary>
    public static string Summary(AuthorKind kind, string payload)
    {
        var o = MockSets.Web;
        string body = (kind.Exam, kind.Module) switch
        {
            ("ielts", "speaking") => JsonSerializer.Deserialize<SpeakingSet>(payload, o) is { } s
                ? $"Part 1: {s.Part1Topic} ({s.Part1.Length})\nPart 2: {s.Part2.Topic}\nPart 3: {s.Part3.Length}"
                : "",
            ("ielts", "writing") => JsonSerializer.Deserialize<WritingSet>(payload, o) is { } w
                ? $"Task 1: {Short(w.Task1.Prompt, 160)}{(w.Task1.Chart is { } c ? $"\n📊 {c.Title} ({c.Categories.Length}×{c.Series.Length})" : "")}\nTask 2: {Short(w.Task2, 200)}"
                : "",
            ("cefr", "speaking") => JsonSerializer.Deserialize<CefrSpeakingSet>(payload, o) is { } s
                ? $"1.1: {Short(s.Part11[0])}\n1.2: {s.Pictures[0].Emoji} / {s.Pictures[1].Emoji}\n2: {s.Part2Topic}\n3: {s.Part3Statement}"
                : "",
            ("cefr", "writing") => JsonSerializer.Deserialize<CefrWritingSet>(payload, o) is { } w
                ? $"{Short(w.Role, 160)}\n2: {Short(w.Task2, 200)}"
                : "",
            (_, "reading") => JsonSerializer.Deserialize<ReadingTest>(payload, o) is { } t
                ? string.Join("\n", t.Passages.Select((p, i) => $"{i + 1}. {p.Title} — {ObjectiveValidator.Words(p.Text)} words, {p.Groups.Sum(g => g.Questions.Count)} q."))
                : "",
            _ => JsonSerializer.Deserialize<ListeningTest>(payload, o) is { } t
                ? string.Join("\n", t.Parts.Select(p => $"{p.Part}. {Short(p.Context, 100)} — {p.Groups.Sum(g => g.Questions.Count)} q."))
                : "",
        };
        return body.Length > 1500 ? body[..1500] + "…" : body;
    }
}
