using System.Text.RegularExpressions;
using SpeakingCoach.Api.Data;
using SpeakingCoach.Api.Services;
using SpeakingCoach.Api.Services.Content;
using SpeakingCoach.Api.Services.Mock;

namespace SpeakingCoach.Api.Tests;

/// <summary>Ilova bilan keladigan tayyor materiallar Gemini yaratganlari bilan bir xil qat'iy qoidalardan o'tadi.</summary>
public class ContentTests
{
    public const int PerLevel = 5;

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

    [Fact]
    public void Built_in_ielts_tests_pass_the_generator_validators()
    {
        var l = BuiltInIelts.Listening1;
        Assert.Equal(4, l.Parts.Count);
        for (var i = 0; i < 4; i++) GeminiMockGenerator.ValidatePart(l.Parts[i], i + 1);

        var r = BuiltInIelts.ReadingAcademic1;
        Assert.Equal(IeltsBank.Academic, r.Variant);
        Assert.Equal(3, r.Passages.Count);
        (int First, int Count)[] layout = [(1, 13), (14, 13), (27, 14)];
        for (var i = 0; i < 3; i++) GeminiMockGenerator.ValidatePassage(r.Passages[i], layout[i].First, layout[i].Count);
    }

    [Fact]
    public void Built_in_cefr_tests_pass_the_generator_validators()
    {
        var l = BuiltInCefr.Listening1;
        Assert.Equal(6, l.Parts.Count);
        for (var i = 0; i < 6; i++) CefrObjective.ValidateListening(l.Parts[i], i + 1);

        var r = BuiltInCefr.Reading1;
        Assert.Equal(5, r.Passages.Count);
        for (var i = 0; i < 5; i++) CefrObjective.ValidateReading(r.Passages[i], i + 1);
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
}
