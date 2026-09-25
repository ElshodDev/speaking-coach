using SpeakingCoach.Api.Data;
using SpeakingCoach.Api.Services;

namespace SpeakingCoach.Api.Tests;

public class VocabularyTests
{
    private static readonly DateTime T = new(2026, 9, 25, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Never_reviewed_word_is_new()
    {
        Assert.Equal(VocabStatus.New, Vocabulary.StatusOf(null, 0));
    }

    [Theory]
    [InlineData(1, VocabStatus.Learning)]
    [InlineData(20.9, VocabStatus.Learning)]
    [InlineData(21, VocabStatus.Known)]
    [InlineData(60, VocabStatus.Known)]
    public void Status_follows_the_review_interval(double intervalDays, VocabStatus expected)
    {
        Assert.Equal(expected, Vocabulary.StatusOf(T, intervalDays));
    }

    [Fact]
    public void Only_words_and_own_phrases_belong_to_the_vocabulary()
    {
        Assert.True(Vocabulary.IsVocab(ReviewCardKind.Word));
        Assert.True(Vocabulary.IsVocab(ReviewCardKind.Manual));
        Assert.False(Vocabulary.IsVocab(ReviewCardKind.Correction));
        Assert.False(Vocabulary.IsVocab(ReviewCardKind.Question));
        Assert.All(Vocabulary.Kinds, k => Assert.True(Vocabulary.IsVocab(k)));
    }

    [Fact]
    public void Note_combines_part_of_speech_definition_and_example()
    {
        var note = Vocabulary.ComposeNote("noun", "a substance used to treat illness", "Take this medicine twice a day.");

        Assert.Equal("(noun) a substance used to treat illness — Take this medicine twice a day.", note);
    }

    [Theory]
    [InlineData(null, "a definition", null, "a definition")]
    [InlineData("verb", null, null, "(verb)")]
    [InlineData(null, null, "An example.", "An example.")]
    [InlineData(null, null, null, "")]
    public void Note_skips_missing_parts(string? pos, string? def, string? ex, string expected)
    {
        Assert.Equal(expected, Vocabulary.ComposeNote(pos, def, ex));
    }

    [Theory]
    [InlineData("Reading", ActivityType.Reading)]
    [InlineData("listening", ActivityType.Listening)]
    public void Source_accepts_reading_and_listening(string input, ActivityType expected)
    {
        Assert.Equal(expected, Vocabulary.ParseSource(input));
    }

    [Theory]
    [InlineData("Speaking")]
    [InlineData("hack")]
    [InlineData(null)]
    public void Other_sources_are_ignored(string? input)
    {
        Assert.Null(Vocabulary.ParseSource(input));
    }

    [Fact]
    public void Word_prompt_uses_the_sentence_when_there_is_one()
    {
        var prompt = GeminiWordService.BuildPrompt("bank", "We sat on the river bank.", "B1");

        Assert.Contains("\"bank\"", prompt);
        Assert.Contains("We sat on the river bank.", prompt);
        Assert.Contains("B1", prompt);
    }

    [Fact]
    public void Word_prompt_asks_for_the_common_meaning_without_a_sentence()
    {
        var prompt = GeminiWordService.BuildPrompt("bank", "", "A2");

        Assert.Contains("most common everyday meaning", prompt);
        Assert.Contains("A2", prompt);
        Assert.Contains("\"translation\"", prompt);
        Assert.Contains("Uzbek translation", prompt);
    }
}
