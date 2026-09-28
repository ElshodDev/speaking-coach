using SpeakingCoach.Api.Services;

namespace SpeakingCoach.Api.Tests;

public class MistakeAnalyzerTests
{
    [Theory]
    [InlineData("I have car", "I have a car", MistakeAnalyzer.Articles)]
    [InlineData("the life is hard", "life is hard", MistakeAnalyzer.Articles)]
    [InlineData("I am good in football", "I am good at football", MistakeAnalyzer.Prepositions)]
    [InlineData("we arrived to Tashkent", "we arrived in Tashkent", MistakeAnalyzer.Prepositions)]
    [InlineData("people spends too much time", "people spend too much time", MistakeAnalyzer.Agreement)]
    [InlineData("she work in a bank", "she works in a bank", MistakeAnalyzer.Agreement)]
    [InlineData("there is many problems", "there are many problems", MistakeAnalyzer.Agreement)]
    [InlineData("yesterday I go to school", "yesterday I went to school", MistakeAnalyzer.Tense)]
    [InlineData("I live here since 2020", "I have lived here since 2020", MistakeAnalyzer.Tense)]
    [InlineData("last year we visit Samarkand", "last year we visited Samarkand", MistakeAnalyzer.Tense)]
    [InlineData("I am agree", "I agree", MistakeAnalyzer.Tense)]
    [InlineData("many student", "many students", MistakeAnalyzer.Plural)]
    [InlineData("two book", "two books", MistakeAnalyzer.Plural)]
    [InlineData("more cheap", "cheaper", MistakeAnalyzer.Comparatives)]
    [InlineData("she is more tall than me", "she is taller than me", MistakeAnalyzer.Comparatives)]
    [InlineData("I very like it", "I like it very", MistakeAnalyzer.WordOrder)]
    [InlineData("It is beatiful", "It is beautiful", MistakeAnalyzer.Spelling)]
    [InlineData("I did a mistake", "I made a mistake", MistakeAnalyzer.WordChoice)]
    public void Corrections_are_sorted_into_mistake_types(string original, string corrected, string expected)
    {
        Assert.Equal(expected, MistakeAnalyzer.Classify(original, corrected, ""));
    }

    [Theory]
    [InlineData("Use an article before a singular noun.", MistakeAnalyzer.Articles)]
    [InlineData("Birlikdagi otdan oldin artikl kerak.", MistakeAnalyzer.Articles)]
    [InlineData("После he/she/it глагол получает -s.", MistakeAnalyzer.Agreement)]
    [InlineData("Oʻtgan zamon kerak.", MistakeAnalyzer.Tense)]
    [InlineData("Здесь нужен предлог at.", MistakeAnalyzer.Prepositions)]
    public void Explanation_keywords_in_three_languages_decide_first(string explanation, string expected)
    {
        Assert.Equal(expected, MistakeAnalyzer.Classify("x y", "x z", explanation));
    }

    [Fact]
    public void Diff_finds_removed_and_added_words()
    {
        var (removed, added) = MistakeAnalyzer.Diff(["i", "go", "to", "school"], ["i", "went", "to", "the", "school"]);
        Assert.Equal(["go"], removed);
        Assert.Equal(["went", "the"], added);
    }

    [Fact]
    public void Corrections_are_read_from_activity_results()
    {
        var at = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        var json = """{"overall":6.5,"topCorrections":[{"original":"I have car","corrected":"I have a car","explanation":"x"},{"original":"same","corrected":"Same","explanation":""},{"original":"","corrected":"a"}]}""";
        var facts = MistakeAnalyzer.FromResponse(json, at).ToList();
        Assert.Equal(1, facts.Count);
        Assert.Equal("I have car", facts[0].Original);
        Assert.Empty(MistakeAnalyzer.FromResponse("{broken", at));
        Assert.Empty(MistakeAnalyzer.FromResponse("""{"score":3}""", at));
    }

    [Fact]
    public void Report_counts_types_windows_and_keeps_latest_distinct_examples()
    {
        var now = new DateTime(2026, 9, 28, 0, 0, 0, DateTimeKind.Utc);
        var facts = new List<MistakeFact>
        {
            new("I have car", "I have a car", "", now.AddDays(-1)),
            new("I have car", "I have a car", "", now.AddDays(-2)),
            new("she is doctor", "she is a doctor", "", now.AddDays(-40)),
            new("yesterday I go", "yesterday I went", "", now.AddDays(-3)),
            new("I did a mistake", "I made a mistake", "", now.AddDays(-1)),
            new("I did a mistake", "I made a mistake", "", now.AddDays(-2)),
            new("I did a mistake", "I made a mistake", "", now.AddDays(-3)),
        };
        var report = MistakeAnalyzer.Summarize(facts, now);
        Assert.Equal(7, report.Total);
        Assert.Equal(6, report.Last30);
        // So'z tanlash eng ko'p bo'lsa ham, aniq grammatik mavzular oldinda.
        Assert.Equal(MistakeAnalyzer.Articles, report.Categories[0].Id);
        var articles = report.Categories[0];
        Assert.Equal(3, articles.Count);
        Assert.Equal(2, articles.Last30);
        Assert.Equal(1, articles.Prev30);
        Assert.Equal(2, articles.Examples.Count);
        Assert.Equal("I have car", articles.Examples[0].Original);
        Assert.Equal(MistakeAnalyzer.WordChoice, report.Categories[^1].Id);
    }
}
