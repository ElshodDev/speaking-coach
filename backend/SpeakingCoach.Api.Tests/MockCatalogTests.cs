using System.Text.Json;
using SpeakingCoach.Api.Services.Content;
using SpeakingCoach.Api.Services.Mock;

namespace SpeakingCoach.Api.Tests;

public class MockCatalogTests
{
    [Fact]
    public void Short_text_is_kept_and_long_text_is_cut_with_an_ellipsis()
    {
        Assert.Equal("A short title", MockCatalog.Short("  A short   title \n"));
        var cut = MockCatalog.Short(new string('a', 30) + " " + new string('b', 30), 20);
        Assert.True(cut.Length <= 20);
        Assert.True(cut.EndsWith("…"));
    }

    [Fact]
    public void Reading_topics_are_passage_titles()
    {
        var test = BuiltInIelts.ReadingAcademic2;
        var payload = JsonSerializer.Serialize(test, GeminiMockGenerator.Web);
        var topics = MockCatalog.Topics("reading", payload);
        Assert.Equal(test.Passages.Count, topics.Count);
        Assert.Equal(MockCatalog.Short(test.Passages[0].Title, 60), topics[0]);
    }

    [Fact]
    public void Listening_topics_are_part_contexts()
    {
        var test = BuiltInCefr.Listening3;
        var payload = JsonSerializer.Serialize(test, GeminiMockGenerator.Web);
        var topics = MockCatalog.Topics("listening", payload);
        Assert.Equal(test.Parts.Count, topics.Count);
        Assert.True(topics.All(t => t.Length > 0 && t.Length <= 70));
    }

    [Fact]
    public void Broken_payload_gives_no_topics()
    {
        Assert.Empty(MockCatalog.Topics("reading", "{not json"));
    }

    [Fact]
    public void Every_speaking_and_writing_set_has_readable_topics()
    {
        Assert.True(IeltsBank.Speaking.All(s => MockCatalog.Topics(s).All(t => t.Length > 0)));
        Assert.True(IeltsBank.Writing.All(w => MockCatalog.Topics(w).All(t => t.Length > 0)));
        Assert.True(CefrBank.Speaking.All(s => MockCatalog.Topics(s).All(t => t.Length > 0)));
        Assert.True(CefrBank.Writing.All(w => MockCatalog.Topics(w).All(t => t.Length > 0)));
    }

    [Fact]
    public void Latest_attempt_per_test_wins_and_marks_it_done()
    {
        var t0 = new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc);
        var latest = MockCatalog.Latest([
            new CatalogAttempt("a", t0, 5.5m),
            new CatalogAttempt("a", t0.AddDays(2), 7m),
            new CatalogAttempt("b", t0.AddDays(1), null),
            new CatalogAttempt("", t0, 9m),
        ]);
        Assert.Equal(2, latest.Count);
        Assert.Equal(7m, latest["a"].Overall);

        var done = MockCatalog.Entry("a", 1, null, ["x"], false, latest);
        Assert.True(done.Done);
        Assert.Equal(7m, done.LastScore);
        Assert.Equal(t0.AddDays(2), done.LastAtUtc);

        var fresh = MockCatalog.Entry("c", 3, "Bot test", [], true, latest);
        Assert.False(fresh.Done);
        Assert.Null(fresh.LastScore);
        Assert.Equal("Bot test", fresh.Title);
    }
}
