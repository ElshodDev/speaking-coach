using SpeakingCoach.Api.Data;
using SpeakingCoach.Api.Services;

namespace SpeakingCoach.Api.Tests;

public class ReviewCardFactoryTests
{
    [Fact]
    public void Corrections_become_cards_with_original_on_front()
    {
        var cards = ReviewCardFactory.FromCorrections(new[]
        {
            new CorrectionItem("I goes to school", "I go to school", "Use 'go' with 'I'."),
        });

        var card = Assert.Single(cards);
        Assert.Equal(ReviewCardKind.Correction, card.Kind);
        Assert.Equal("I goes to school", card.Front);
        Assert.Equal("I go to school", card.Back);
        Assert.Equal("Use 'go' with 'I'.", card.Note);
    }

    [Fact]
    public void Empty_unchanged_and_duplicate_corrections_are_skipped()
    {
        var cards = ReviewCardFactory.FromCorrections(new[]
        {
            new CorrectionItem("  ", "x", "empty front"),
            new CorrectionItem("Hello", "hello", "only case differs — nothing to learn"),
            new CorrectionItem("He don't know", "He doesn't know", "a"),
            new CorrectionItem("he don't know", "He doesn't know", "same phrase, different case"),
        });

        var card = Assert.Single(cards);
        Assert.Equal("He don't know", card.Front);
    }

    [Fact]
    public void Long_text_is_trimmed_to_column_limits()
    {
        var longText = new string('a', 2000);

        var card = Assert.Single(ReviewCardFactory.FromCorrections(new[] { new CorrectionItem(longText, "b", longText) }));

        Assert.Equal(ReviewCardFactory.MaxFrontLength, card.Front.Length);
        Assert.Equal(ReviewCardFactory.MaxNoteLength, card.Note.Length);
    }

    [Fact]
    public void Only_wrong_answers_become_question_cards()
    {
        var exercise = new ComprehensionExercise("Tea", "passage", new List<ComprehensionQuestion>
        {
            new("Q1?", new List<string> { "a", "b", "c", "d" }, 0, "e1"),
            new("Q2?", new List<string> { "a", "b", "c", "d" }, 2, "e2"),
        });
        var result = GeminiComprehensionService.Grade(
            new ComprehensionExercise(exercise.Title, exercise.Passage, exercise.Questions), new[] { 0, 1 });

        var card = Assert.Single(ReviewCardFactory.FromWrongAnswers(exercise, result));

        Assert.Equal(ReviewCardKind.Question, card.Kind);
        Assert.Equal("Q2? (Tea)", card.Front);
        Assert.Equal("c", card.Back);
        Assert.Equal("e2", card.Note);
    }
}
