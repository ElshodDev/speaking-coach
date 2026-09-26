using SpeakingCoach.Api.Services;
using SpeakingCoach.Api.Services.Telegram;

namespace SpeakingCoach.Api.Tests;

public class FunTests
{
    [Fact]
    public void Daily_words_are_complete_and_unique()
    {
        Assert.True(DailyWords.All.Length >= 60);
        Assert.Equal(DailyWords.All.Length, DailyWords.All.Select(w => w.Word).Distinct().Count());
        foreach (var w in DailyWords.All)
        {
            Assert.Contains(w.Level, new[] { "B1", "B2", "C1" });
            Assert.Contains(w.Pos, new[] { "noun", "verb", "adjective", "adverb" });
            Assert.False(string.IsNullOrWhiteSpace(w.Uz));
            Assert.False(string.IsNullOrWhiteSpace(w.Ru));
            Assert.DoesNotContain("'", w.Uz);                 // o'zbekcha: ʻ/ʼ, oddiy ' emas
            Assert.True(w.Example.Contains(w.Word, StringComparison.OrdinalIgnoreCase), $"misolda so'z yo'q: {w.Word}");
        }
    }

    [Fact]
    public void Word_of_the_day_is_the_same_for_everyone_and_changes_daily()
    {
        var d = new DateOnly(2026, 9, 27);
        Assert.Equal(DailyWords.ForDate(d), DailyWords.ForDate(d));
        Assert.NotEqual(DailyWords.ForDate(d).Word, DailyWords.ForDate(d.AddDays(1)).Word);
        var cycle = Enumerable.Range(0, DailyWords.All.Length).Select(i => DailyWords.ForDate(d.AddDays(i)).Word).ToHashSet();
        Assert.Equal(DailyWords.All.Length, cycle.Count);   // bir aylanishda har so'z bir marta
        Assert.Equal("спорить; утверждать", DailyWords.All.First(w => w.Word == "argue").Translation("ru"));
    }

    private static List<QuizPair> Pairs(int n) => Enumerable.Range(0, n).Select(i => new QuizPair($"word{i}", $"meaning {i}")).ToList();

    [Fact]
    public void Quiz_questions_have_four_unique_options_and_one_right_answer()
    {
        var pool = Pairs(20);
        var qs = QuizLogic.Build(pool, 10, new Random(1));
        Assert.Equal(10, qs.Count);
        Assert.Equal(10, qs.Select(q => q.Prompt).Distinct().Count());
        foreach (var q in qs)
        {
            Assert.Equal(4, q.Options.Count);
            Assert.Equal(4, q.Options.Distinct().Count());
            Assert.True(q.Answer is >= 0 and <= 3);
            var pair = pool.Single(p => p.Word == q.Prompt || p.Meaning == q.Prompt);
            Assert.Equal(q.Direction == "word" ? pair.Meaning : pair.Word, q.Options[q.Answer]);
        }
        Assert.Equal(["word", "meaning"], qs.Take(2).Select(q => q.Direction).ToArray());   // navbatma-navbat
    }

    [Fact]
    public void Quiz_needs_at_least_four_pairs()
    {
        Assert.Empty(QuizLogic.Build(Pairs(3), 10, new Random(1)));
        Assert.Equal(4, QuizLogic.Build(Pairs(4), 10, new Random(1)).Count);
    }

    [Fact]
    public void Small_vocabulary_is_topped_up_with_daily_words()
    {
        var own = Pairs(3);
        var pool = QuizLogic.Pool(own, "uz");
        Assert.True(pool.Count >= QuizLogic.MinPool);
        Assert.Contains(pool, p => p.Word == "word0");
        Assert.Contains(pool, p => p.Word == "achieve" && p.Meaning == "erishmoq");
        Assert.Equal(Pairs(15), QuizLogic.Pool(Pairs(15), "uz"));   // yetarli — faqat o'z so'zlari
        Assert.Contains(QuizLogic.Pool([], "en"), p => p.Word == "achieve" && p.Meaning.StartsWith("to succeed"));
    }

    [Fact]
    public void Clean_drops_duplicates_and_junk()
    {
        var cleaned = QuizLogic.Clean([
            new("Book", "kitob"), new("book", "kitob 2"), new("pen", "kitob"), new(" ", "x"), new("same", "Same"), new(new string('a', 50), "x"),
        ]);
        Assert.Equal([new QuizPair("Book", "kitob")], cleaned);
    }

    [Fact]
    public void Word_command()
    {
        Assert.True(BotLogic.IsCommand("/word", "/word"));
        Assert.True(BotLogic.IsCommand("/word@SpeakingCoachUzBot", "/word"));
        Assert.False(BotLogic.IsCommand("/words", "/word"));
        Assert.False(BotLogic.IsCommand("word", "/word"));
    }
}
