using System.Text.Json;
using SpeakingCoach.Api.Data;
using SpeakingCoach.Api.Endpoints;
using SpeakingCoach.Api.Services.Mock;

namespace SpeakingCoach.Api.Tests;

public class CefrObjectiveTests
{
    private static string Words(int n, string word = "word") => string.Join(" ", Enumerable.Repeat(word, n));

    private static ObjQuestion Q(int n, string answer, string prompt = "Question", List<string>? options = null) =>
        new(n, prompt, options, [answer], "because");

    private static QuestionGroup Mcq(int first, int count, int options, string instructions = "Choose.") =>
        new("mcq", instructions, null,
            Enumerable.Range(first, count).Select(n => Q(n, "A", "Choose the correct reply.", Enumerable.Range(0, options).Select(i => $"option {i}").ToList())).ToList());

    private static QuestionGroup Gap(int first, int count, int maxWords, string answer = "museum") =>
        new("gap", "Write ONE WORD.", maxWords, Enumerable.Range(first, count).Select(n => Q(n, answer, "The ___ opens at nine.")).ToList());

    private static QuestionGroup Match(int first, int count, int options) =>
        new("match", "Match.", null,
            Enumerable.Range(0, count).Select(i => Q(first + i, ((char)('A' + i)).ToString(), $"Speaker {i + 1}")).ToList(),
            Options: Enumerable.Range(0, options).Select(i => $"statement {i}").ToList());

    private static MapPlan Map(int spots = 8) => new("City park",
        [new("Main entrance", 2, 4), new("Lake", 2, 2)],
        Enumerable.Range(0, spots).Select(i => new MapSpot(((char)('A' + i)).ToString(), i % 5, i / 5 == 0 ? 0 : 1)).ToList());

    private static QuestionGroup MapGroup(int first, int count, MapPlan? map = null) =>
        new("map", "Label the map.", null,
            Enumerable.Range(0, count).Select(i => Q(first + i, ((char)('A' + i)).ToString(), $"Place {i}")).ToList(),
            Map: map ?? Map());

    private static ListeningPart Part(int part, List<QuestionGroup> groups, int lines = 4, int wordsPerLine = 100, string extra = "museum") =>
        new(part, "You will hear…",
            Enumerable.Range(0, lines).Select(i => new ScriptLine($"Speaker {i + 1}", i % 2 == 0 ? "male" : "female", Words(wordsPerLine) + " " + extra)).ToList(),
            groups);

    public static ListeningTest ValidListening() => new(
    [
        Part(1, [Mcq(1, 8, 3)], lines: 8, wordsPerLine: 10),
        Part(2, [Gap(9, 6, 2)], lines: 3),
        Part(3, [Match(15, 4, 6)], lines: 4),
        Part(4, [MapGroup(19, 5)], lines: 3),
        Part(5, [Mcq(24, 6, 3)], lines: 5),
        Part(6, [Gap(30, 6, 1)], lines: 5),
    ]);

    private static string Numbered(int first, int count, int words) =>
        string.Join("\n\n", Enumerable.Range(first, count).Select(n => $"{n}. {Words(words)}"));

    public static ReadingTest ValidReading() => new("",
    [
        new("Notice", string.Join(" ", Enumerable.Range(1, 6).Select(n => $"({n}) ______")) + " " + Words(200) + " museum",
            [Gap(1, 6, 1)]),
        new("People", Numbered(7, 8, 60), [Match(7, 8, 10)]),
        new("Article", Numbered(15, 6, 80), [Match(15, 6, 8)]),
        new("Article", Words(600) + " museum", [Mcq(21, 4, 4), new("tfng", "TRUE/FALSE/NOT GIVEN", null, Enumerable.Range(25, 5).Select(n => Q(n, "NOT GIVEN", "Statement")).ToList())]),
        new("Article", Words(600) + " museum", [Gap(30, 4, 2), Mcq(34, 2, 4)]),
    ]);

    [Fact]
    public void Layouts_follow_the_official_35_question_format()
    {
        Assert.Equal(35, CefrObjective.ListeningLayout.Sum(l => l.Count));
        Assert.Equal(35, CefrObjective.ReadingLayout.Sum(l => l.Count));
        Assert.Equal([8, 6, 4, 5, 6, 6], CefrObjective.ListeningLayout.Select(l => l.Count).ToArray());
        Assert.Equal([6, 8, 6, 9, 6], CefrObjective.ReadingLayout.Select(l => l.Count).ToArray());
        // Raqamlar uzluksiz: har qism oldingisidan keyin boshlanadi.
        foreach (var layout in new[] { CefrObjective.ListeningLayout, CefrObjective.ReadingLayout })
        {
            for (var i = 1; i < layout.Length; i++) Assert.Equal(layout[i - 1].First + layout[i - 1].Count, layout[i].First);
        }
        Assert.Equal(45, CefrObjective.ListeningMinutes);
        Assert.Equal(60, CefrObjective.ReadingMinutes);
    }

    [Fact]
    public void Valid_tests_pass_validation()
    {
        var l = ValidListening();
        foreach (var p in l.Parts) CefrObjective.ValidateListening(p, p.Part);
        var r = ValidReading();
        for (var i = 0; i < r.Passages.Count; i++) CefrObjective.ValidateReading(r.Passages[i], i + 1);
    }

    [Fact]
    public void Listening_part1_needs_one_sentence_per_question_and_three_options()
    {
        var ok = ValidListening().Parts[0];
        Assert.Throws<InvalidOperationException>(() => CefrObjective.ValidateListening(ok with { Script = ok.Script.Take(7).ToList() }, 1));
        Assert.Throws<InvalidOperationException>(() => CefrObjective.ValidateListening(ok with { Groups = [Mcq(1, 8, 4)] }, 1));
        Assert.Throws<InvalidOperationException>(() => CefrObjective.ValidateListening(ok with { Groups = [Gap(1, 8, 1)] }, 1));
    }

    [Fact]
    public void Matching_needs_two_extra_options_and_unique_answers()
    {
        var p3 = ValidListening().Parts[2];
        Assert.Throws<InvalidOperationException>(() => CefrObjective.ValidateListening(p3 with { Groups = [Match(15, 4, 5)] }, 3));
        var dup = Match(15, 4, 6) with { Questions = [Q(15, "A"), Q(16, "A"), Q(17, "B"), Q(18, "C")] };
        Assert.Throws<InvalidOperationException>(() => CefrObjective.ValidateListening(p3 with { Groups = [dup] }, 3));
        var outOfRange = Match(15, 4, 6) with { Questions = [Q(15, "A"), Q(16, "B"), Q(17, "C"), Q(18, "G")] };
        Assert.Throws<InvalidOperationException>(() => CefrObjective.ValidateListening(p3 with { Groups = [outOfRange] }, 3));
    }

    [Fact]
    public void Map_needs_eight_lettered_spots_on_separate_cells()
    {
        var p4 = ValidListening().Parts[3];
        Assert.Throws<InvalidOperationException>(() => CefrObjective.ValidateListening(p4 with { Groups = [MapGroup(19, 5, Map(7))] }, 4));
        var clash = Map() with { Landmarks = [new("Main entrance", 0, 0), new("Lake", 2, 2)] }; // A ham (0,0) da
        Assert.Throws<InvalidOperationException>(() => CefrObjective.ValidateListening(p4 with { Groups = [MapGroup(19, 5, clash)] }, 4));
        var outside = Map() with { Landmarks = [new("Main entrance", 5, 4), new("Lake", 2, 2)] };
        Assert.Throws<InvalidOperationException>(() => CefrObjective.ValidateListening(p4 with { Groups = [MapGroup(19, 5, outside)] }, 4));
        var noMap = MapGroup(19, 5) with { Map = null };
        Assert.Throws<InvalidOperationException>(() => CefrObjective.ValidateListening(p4 with { Groups = [noMap] }, 4));
    }

    [Fact]
    public void Reading_part1_answer_must_appear_elsewhere_in_the_text_as_a_whole_word()
    {
        var p1 = ValidReading().Passages[0];
        Assert.Throws<InvalidOperationException>(() => CefrObjective.ValidateReading(p1 with { Groups = [Gap(1, 6, 1, "garden")] }, 1));
        // "use" — "museum" ichida bor, lekin alohida so'z sifatida yo'q.
        Assert.False(CefrObjective.HasWord("the museum is open", "use"));
        Assert.True(CefrObjective.HasWord("The Museum is open.", "museum"));
        Assert.Throws<InvalidOperationException>(() => CefrObjective.ValidateReading(p1 with { Text = p1.Text.Replace("(6)", "(7)") }, 1));
    }

    [Fact]
    public void Reading_parts_2_and_3_need_numbered_paragraphs()
    {
        var p2 = ValidReading().Passages[1];
        Assert.Throws<InvalidOperationException>(() => CefrObjective.ValidateReading(p2 with { Text = p2.Text.Replace("14.", "Fourteen:") }, 2));
        var p3 = ValidReading().Passages[2];
        Assert.Throws<InvalidOperationException>(() => CefrObjective.ValidateReading(p3 with { Groups = [Match(15, 6, 7)] }, 3));
    }

    [Fact]
    public void Wrong_question_types_are_rejected()
    {
        var p4 = ValidReading().Passages[3];
        Assert.Throws<InvalidOperationException>(() => CefrObjective.ValidateReading(p4 with { Groups = [Mcq(21, 9, 4)] }, 4));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(9, 37)]
    [InlineData(10, 38)]
    [InlineData(17, 50)]
    [InlineData(18, 51)]
    [InlineData(27, 64)]
    [InlineData(28, 65)]
    [InlineData(35, 75)]
    [InlineData(22, 57)]
    public void Score_follows_the_official_correct_answers_table(int correct, int expected) => Assert.Equal(expected, CefrObjective.Score(correct));

    [Fact]
    public void Level_from_score_matches_the_official_correct_answers_ranges()
    {
        // Rasmiy jadval: C1 28–35, B2 18–27, B1 10–17, B1 dan quyi 0–9 (tinglash va o'qish).
        for (var raw = 0; raw <= 35; raw++)
        {
            var expected = raw >= 28 ? "C1" : raw >= 18 ? "B2" : raw >= 10 ? "B1" : "below B1";
            Assert.Equal(expected, CefrScale.Level(CefrObjective.Score(raw)));
        }
        // Monoton: ko'proq to'g'ri javob — kamroq ball bermaydi.
        for (var raw = 1; raw <= 35; raw++) Assert.True(CefrObjective.Score(raw) > CefrObjective.Score(raw - 1));
    }

    [Theory]
    [InlineData(new[] { 60, 55, 48, 51 }, 54)]
    [InlineData(new[] { 64, 65, 65, 65 }, 65)]   // 64.75 → 65
    [InlineData(new[] { 50, 51 }, 51)]            // 50.5 → 51
    public void Full_exam_overall_is_the_average(int[] sections, int expected) => Assert.Equal(expected, CefrObjective.Overall(sections));

    [Fact]
    public void Grading_counts_parts_and_level()
    {
        var test = new MockTest { Id = Guid.NewGuid(), Exam = "cefr", Module = "listening", Payload = JsonSerializer.Serialize(ValidListening(), GeminiMockGenerator.Web) };
        var answers = new Dictionary<string, string>();
        for (var n = 1; n <= 8; n++) answers[n.ToString()] = "a";          // 1-qism: hammasi to'g'ri (kichik harf ham)
        answers["15"] = "A"; answers["16"] = "B"; answers["17"] = "B";     // 3-qism: 2 ta to'g'ri
        answers["19"] = "A";                                               // 4-qism: 1 ta
        answers["30"] = "Museum";                                          // 6-qism: 1 ta
        var r = MockEndpoints.GradeCefrObjective("listening", test, answers, 1200);
        Assert.Equal(12, r.Score);
        Assert.Equal(35, r.Total);
        Assert.Equal(CefrObjective.Score(12), r.Overall);
        Assert.Equal("B1", r.Level);   // 12 ta to'g'ri — rasmiy jadvalda B1 (10–17)
        Assert.Equal([8, 0, 2, 1, 0, 1], r.Parts.Select(p => p.Score).ToArray());
        Assert.Equal(6, r.Parts.Count);

        // Guruh vazifasi ballni "41/75 · B1" ko'rinishida ko'rsatadi.
        var json = JsonSerializer.Serialize(r, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.Equal($"{r.Overall}/75 · B1", SpeakingCoach.Api.Services.GroupLogic.ScoreText(json));
    }

    [Fact]
    public void Client_view_keeps_options_and_map_but_not_answers()
    {
        var json = JsonSerializer.Serialize(ClientView.Listening(Guid.NewGuid(), ValidListening()), GeminiMockGenerator.Web);
        Assert.DoesNotContain("\"answers\"", json);
        Assert.DoesNotContain("\"explanation\"", json);
        Assert.Contains("\"spots\"", json);
        Assert.Contains("statement 5", json);
    }

    [Fact]
    public void Old_ielts_tests_without_the_new_fields_still_load()
    {
        const string old = """{"variant":"academic","passages":[{"title":"T","text":"x","groups":[{"type":"gap","instructions":"i","maxWords":2,"questions":[]}]}]}""";
        var t = JsonSerializer.Deserialize<ReadingTest>(old, GeminiMockGenerator.Web)!;
        Assert.Null(t.Passages[0].Groups[0].Options);
        Assert.Null(t.Passages[0].Groups[0].Map);
    }

    [Fact]
    public void Prompts_carry_the_official_instructions()
    {
        Assert.Contains("TWO EXTRA options", CefrObjective.ListeningPrompt(3, "sport"));
        Assert.Contains("\"spots\"", CefrObjective.ListeningPrompt(4, "a zoo"));
        Assert.Contains("somewhere in the rest of the text", CefrObjective.ReadingPrompt(1, "books"));
        Assert.Contains("TRUE, FALSE or NOT GIVEN", CefrObjective.ReadingPrompt(4, "books"));
        for (var p = 1; p <= 6; p++) Assert.Contains($"Part {p}", CefrObjective.ListeningPrompt(p, "x"));
    }
}
