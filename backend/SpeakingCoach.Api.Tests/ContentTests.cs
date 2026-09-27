using System.Text.RegularExpressions;
using SpeakingCoach.Api.Data;
using SpeakingCoach.Api.Services;
using SpeakingCoach.Api.Services.Content;
using SpeakingCoach.Api.Services.Mock;

namespace SpeakingCoach.Api.Tests;

/// <summary>Ilova bilan keladigan tayyor materiallar Gemini yaratganlari bilan bir xil qat'iy qoidalardan o'tadi.</summary>
public class ContentTests
{
    public const int PerLevel = 11;

    private static int Words(string s) => s.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;

    [Theory]
    [InlineData(ActivityType.Reading)]
    [InlineData(ActivityType.Listening)]
    public void Practice_library_has_enough_valid_exercises_per_level(ActivityType type)
    {
        var items = PracticeLibrary.For(type);
        foreach (var level in LearnerLevel.All)
            Assert.True(items.Count(x => x.Level == level) >= PerLevel, $"{type} {level}: {items.Count(x => x.Level == level)} ta");

        Assert.Equal(items.Count, items.Select(x => x.Id).Distinct().Count());
        Assert.Equal(items.Count, items.Select(x => x.Exercise.Title).Distinct().Count());

        foreach (var item in items)
        {
            var e = item.Exercise;
            GeminiComprehensionService.Validate(e);
            var (min, max) = type == ActivityType.Reading ? LearnerLevel.ReadingWords(item.Level) : LearnerLevel.ListeningWords(item.Level);
            var n = Words(e.Passage);
            Assert.True(n >= min - 10 && n <= max + 15, $"{item.Id}: {n} so'z, kutilgan {min}-{max}");
            Assert.All(e.Questions, q => Assert.False(string.IsNullOrWhiteSpace(q.Explanation), $"{item.Id}: izoh bo'sh"));
            Assert.All(e.Questions, q => Assert.Equal(q.Options.Count, q.Options.Distinct().Count()));
            // To'g'ri javob har doim bir joyda turmasin.
            Assert.True(e.Questions.Select(q => q.CorrectIndex).Distinct().Count() >= 2, $"{item.Id}: to'g'ri javob o'rni bir xil");
            if (type == ActivityType.Listening)
                Assert.False(Regex.IsMatch(e.Passage, @"\d"), $"{item.Id}: skriptda raqam bor (ovoz uchun so'z bilan yozing)");
        }
    }

    [Fact]
    public void Practice_library_serves_fresh_items_first_then_the_oldest()
    {
        var level = "B1";
        var items = PracticeLibrary.For(ActivityType.Reading).Where(x => x.Level == level).ToList();
        var first = PracticeLibrary.Next(ActivityType.Reading, level, [])!;
        Assert.Equal(level, first.Level);
        var done = items.Select(x => x.Exercise.Title).Reverse().ToList(); // eng yangisi birinchi
        var again = PracticeLibrary.Next(ActivityType.Reading, level, done)!;
        Assert.Equal(done.Last(), again.Exercise.Title);
        Assert.Equal((items.Count, items.Count), PracticeLibrary.Progress(ActivityType.Reading, level, done));
    }

    /// <summary>
    /// Har bir tayyor mock test — AI testlari bilan bir xil validatorlardan. Xato bo'lsa,
    /// xabarda test nomi va sababi (masalan "IELTS Listening — Test 7: …") ko'rinadi.
    /// </summary>
    [Fact]
    public void Built_in_mocks_pass_the_generator_validators()
    {
        var errors = new List<string>();
        foreach (var m in BuiltInMocks.All)
        {
            try
            {
                switch (m.Exam, m.Module)
                {
                    case ("ielts", "listening"):
                        var il = (ListeningTest)m.Content;
                        Assert.Equal(4, il.Parts.Count);
                        for (var i = 0; i < 4; i++) GeminiMockGenerator.ValidatePart(il.Parts[i], i + 1);
                        break;
                    case ("ielts", "reading"):
                        var ir = (ReadingTest)m.Content;
                        Assert.Equal(IeltsBank.Academic, ir.Variant);
                        Assert.Equal(3, ir.Passages.Count);
                        (int First, int Count)[] layout = [(1, 13), (14, 13), (27, 14)];
                        for (var i = 0; i < 3; i++) GeminiMockGenerator.ValidatePassage(ir.Passages[i], layout[i].First, layout[i].Count);
                        break;
                    case ("cefr", "listening"):
                        var cl = (ListeningTest)m.Content;
                        Assert.Equal(6, cl.Parts.Count);
                        for (var i = 0; i < 6; i++) CefrObjective.ValidateListening(cl.Parts[i], i + 1);
                        break;
                    case ("cefr", "reading"):
                        var cr = (ReadingTest)m.Content;
                        Assert.Equal(5, cr.Passages.Count);
                        for (var i = 0; i < 5; i++) CefrObjective.ValidateReading(cr.Passages[i], i + 1);
                        break;
                }
            }
            catch (Exception ex)
            {
                errors.Add($"{m.Title}: {ex.Message}");
            }
        }
        Assert.True(errors.Count == 0, string.Join(" || ", errors));
    }

    [Theory]
    [InlineData("ielts", "listening")]
    [InlineData("ielts", "reading")]
    [InlineData("cefr", "listening")]
    [InlineData("cefr", "reading")]
    public void Each_mock_kind_has_more_than_ten_built_in_tests(string exam, string module)
    {
        var n = BuiltInMocks.All.Count(m => m.Exam == exam && m.Module == module);
        Assert.True(n > 10, $"{exam} {module}: {n} ta tayyor test");
        // Matnlar takrorlanmasin (bir testni ikki marta ko'chirib qo'yish xatosi).
        var texts = BuiltInMocks.All.Where(m => m.Exam == exam && m.Module == module)
            .Select(m => m.Content is ReadingTest r ? r.Passages[0].Title : ((ListeningTest)m.Content).Parts[0].Context).ToList();
        Assert.Equal(texts.Count, texts.Distinct().Count());
    }

    [Fact]
    public void Built_in_mock_ids_are_unique_and_stable()
    {
        Assert.Equal(BuiltInMocks.All.Length, BuiltInMocks.All.Select(x => x.Id).Distinct().Count());
        Assert.All(BuiltInMocks.All, m => Assert.True(BuiltInMocks.IsBuiltIn(m.Id)));
    }

    [Fact]
    public void Ai_topic_is_validated_and_cue_cards_need_points()
    {
        var ok = ContentAi.ValidateTopic(new SuggestedTopic("  Describe a festival you enjoyed.  ", ["when it was", "who you were with", "and explain why you enjoyed it"]), "ielts-part2");
        Assert.Equal("Describe a festival you enjoyed.", ok.Text);
        Assert.Equal(3, ok.Points!.Count);
        Assert.Throws<InvalidOperationException>(() => ContentAi.ValidateTopic(new SuggestedTopic("Describe a festival.", []), "ielts-part2"));
        Assert.Throws<InvalidOperationException>(() => ContentAi.ValidateTopic(new SuggestedTopic("short", null), "everyday"));
        Assert.Throws<InvalidOperationException>(() => ContentAi.ValidateTopic(new SuggestedTopic(new string('x', 301), null), "everyday"));
        // Cue card bo'lmasa punktlar tashlanadi.
        Assert.Null(ContentAi.ValidateTopic(new SuggestedTopic("Do you like cooking at home?", ["a", "b", "c"]), "ielts-part1").Points);
        Assert.True(ContentAi.ValidCategory("writing", "letter-formal"));
        Assert.False(ContentAi.ValidCategory("speaking", "letter-formal"));
        Assert.False(ContentAi.ValidCategory("speaking", null));
        Assert.Contains("Describe", ContentAi.TopicPrompt("speaking", "ielts-part2", "B2", ["Describe a book"]));
    }

    [Fact]
    public void Ai_grammar_quiz_is_validated()
    {
        GrammarQuizQuestion Q(string p, int answer, params string[] o) => new(p, [.. o], answer, "Because.");
        var good = new GrammarQuiz([.. Enumerable.Range(0, 6).Select(i => Q("She ___ here since May.", i % 3, "has lived", "lives", "lived"))]);
        Assert.True(ReferenceEquals(good, ContentAi.ValidateQuiz(good)));
        Assert.Throws<InvalidOperationException>(() => ContentAi.ValidateQuiz(new GrammarQuiz([.. good.Questions.Take(5)])));
        Assert.Throws<InvalidOperationException>(() => ContentAi.ValidateQuiz(new GrammarQuiz([.. good.Questions.Take(5), Q("No gap here.", 0, "a", "b", "c")])));
        Assert.Throws<InvalidOperationException>(() => ContentAi.ValidateQuiz(new GrammarQuiz([.. good.Questions.Take(5), Q("A ___ b.", 3, "a", "b", "c")])));
        Assert.Throws<InvalidOperationException>(() => ContentAi.ValidateQuiz(new GrammarQuiz([.. good.Questions.Take(5), Q("A ___ b.", 0, "a", "a", "c")])));
        Assert.Contains("Russian", ContentAi.QuizPrompt("Passive voice", "B2", "ru"));
    }

    [Fact]
    public void Speaking_and_writing_mock_banks_offer_more_than_ten_choices()
    {
        Assert.True(IeltsBank.Speaking.Length > 10, $"IELTS Speaking: {IeltsBank.Speaking.Length}");
        Assert.True(IeltsBank.Writing.Count(w => w.Variant == IeltsBank.Academic) > 10, "IELTS Writing Academic");
        Assert.True(IeltsBank.Writing.Count(w => w.Variant == IeltsBank.General) > 10, "IELTS Writing General");
        Assert.True(CefrBank.Speaking.Length > 10, "CEFR Speaking");
        Assert.True(CefrBank.Writing.Length > 10, "CEFR Writing");
        Assert.Equal(IeltsBank.Writing.Length, IeltsBank.Writing.Select(w => w.Id).Distinct().Count());
    }
}
