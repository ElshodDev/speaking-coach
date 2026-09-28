using SpeakingCoach.Api.Services.Mock;

namespace SpeakingCoach.Api.Tests;

/// <summary>
/// Xom ball → band jadvallari ommaviy (taxminiy) IELTS jadvallariga mos:
/// ielts.org o'rtacha nuqtalari va keng tarqalgan to'liq oraliqlar.
/// </summary>
public class BandTableTests
{
    [Theory]
    [InlineData(15, 4.5)] [InlineData(16, 5.0)] [InlineData(17, 5.0)] [InlineData(18, 5.5)] [InlineData(22, 5.5)]
    [InlineData(26, 6.5)] [InlineData(31, 7.0)] [InlineData(32, 7.5)] [InlineData(37, 8.5)] [InlineData(39, 9.0)]
    public void Listening_follows_the_published_ranges(int raw, double band) =>
        Assert.Equal((decimal)band, ObjectiveGrading.BandFor(raw, ObjectiveGrading.ListeningAnchors));

    [Theory]
    [InlineData(14, 4.5)] [InlineData(15, 5.0)] [InlineData(18, 5.0)] [InlineData(19, 5.5)] [InlineData(23, 6.0)]
    [InlineData(26, 6.0)] [InlineData(27, 6.5)] [InlineData(30, 7.0)] [InlineData(32, 7.0)] [InlineData(33, 7.5)] [InlineData(35, 8.0)]
    public void Academic_reading_follows_the_published_ranges(int raw, double band) =>
        Assert.Equal((decimal)band, ObjectiveGrading.BandFor(raw, ObjectiveGrading.AcademicReadingAnchors));

    [Theory]
    [InlineData(14, 3.5)] [InlineData(15, 4.0)] [InlineData(23, 5.0)] [InlineData(30, 6.0)] [InlineData(33, 6.5)]
    [InlineData(34, 7.0)] [InlineData(35, 7.0)] [InlineData(36, 7.5)] [InlineData(39, 8.5)] [InlineData(40, 9.0)]
    public void General_training_reading_follows_the_published_ranges(int raw, double band) =>
        Assert.Equal((decimal)band, ObjectiveGrading.BandFor(raw, ObjectiveGrading.GeneralReadingAnchors));

    [Fact]
    public void Tables_never_go_down_as_the_raw_score_rises()
    {
        foreach (var t in new[] { ObjectiveGrading.ListeningAnchors, ObjectiveGrading.AcademicReadingAnchors, ObjectiveGrading.GeneralReadingAnchors })
        {
            Assert.Equal(0m, ObjectiveGrading.BandFor(0, t));
            for (var raw = 1; raw <= 40; raw++)
                Assert.True(ObjectiveGrading.BandFor(raw, t) >= ObjectiveGrading.BandFor(raw - 1, t), $"{raw}");
        }
    }
}
