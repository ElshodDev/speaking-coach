using SpeakingCoach.Api.Data;
using SpeakingCoach.Api.Services;

namespace SpeakingCoach.Api.Tests;

public class GroupTests
{
    [Fact]
    public void Join_codes_avoid_ambiguous_characters()
    {
        var rng = new Random(7);
        for (var i = 0; i < 500; i++)
        {
            var code = GroupLogic.NewJoinCode(rng);
            Assert.Equal(6, code.Length);
            Assert.DoesNotContain(code, ch => "01OIL".Contains(ch));
            Assert.Equal(code, GroupLogic.NormalizeCode(code));
        }
    }

    [Theory]
    [InlineData("abc234", "ABC234")]
    [InlineData(" AB C-234 ", "ABC234")]
    [InlineData("ABC23", null)]      // qisqa
    [InlineData("ABC2O4", null)]     // O — alifboda yo'q
    [InlineData("", null)]
    [InlineData(null, null)]
    public void Normalize_code(string? input, string? expected) => Assert.Equal(expected, GroupLogic.NormalizeCode(input));

    [Theory]
    [InlineData("review", true)]
    [InlineData("practice:speaking", true)]
    [InlineData("practice:listening", true)]
    [InlineData("practice:dance", false)]
    [InlineData("mock:ielts:reading", true)]
    [InlineData("mock:ielts:speaking", true)]
    [InlineData("mock:cefr:writing", true)]
    [InlineData("mock:cefr:listening", false)]   // CEFR Listening hali yo'q
    [InlineData("mock:toefl:writing", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void Valid_kinds(string? kind, bool ok) => Assert.Equal(ok, GroupLogic.IsValidKind(kind));

    [Theory]
    [InlineData("  8-A sinf  ", "8-A sinf")]
    [InlineData("A", null)]
    [InlineData("", null)]
    public void Clean_name(string input, string? expected) => Assert.Equal(expected, GroupLogic.CleanName(input));

    private static readonly DateTime T0 = new(2026, 9, 26, 10, 0, 0, DateTimeKind.Utc);

    private static Assignment A(string kind, DateTime? due = null, int? target = null) =>
        new() { Id = Guid.NewGuid(), Kind = kind, CreatedAtUtc = T0, DueAtUtc = due, Target = target };

    private static ActivityRef Act(ActivityType type, double hoursAfter, string? exam = null, string? module = null, string? score = null) =>
        new(Guid.NewGuid(), type, T0.AddHours(hoursAfter), exam, module, score);

    [Fact]
    public void Practice_is_done_by_the_first_matching_attempt_after_the_assignment()
    {
        var a = A("practice:writing", due: T0.AddDays(1));
        var before = Act(ActivityType.Writing, -1, score: "90/100");
        var speaking = Act(ActivityType.Speaking, 1);
        var first = Act(ActivityType.Writing, 2, score: "72/100");
        var second = Act(ActivityType.Writing, 3, score: "80/100");
        var s = GroupLogic.StatusFor(a, [before, speaking, second, first], []);
        Assert.True(s.Done);
        Assert.False(s.Late);
        Assert.Equal(first.Id, s.ActivityId);
        Assert.Equal("72/100", s.Score);

        Assert.False(GroupLogic.StatusFor(a, [before, speaking], []).Done);   // oldingi urinish hisoblanmaydi
    }

    [Fact]
    public void Late_when_done_after_the_due_date()
    {
        var a = A("practice:reading", due: T0.AddHours(5));
        var s = GroupLogic.StatusFor(a, [Act(ActivityType.Reading, 6, score: "3/4")], []);
        Assert.True(s.Done);
        Assert.True(s.Late);
    }

    [Fact]
    public void Mock_matches_exam_and_module()
    {
        var a = A("mock:cefr:writing");
        Assert.False(GroupLogic.StatusFor(a, [Act(ActivityType.MockExam, 1, "ielts", "writing")], []).Done);
        Assert.False(GroupLogic.StatusFor(a, [Act(ActivityType.MockExam, 1, "cefr", "speaking")], []).Done);
        var s = GroupLogic.StatusFor(a, [Act(ActivityType.MockExam, 1, "cefr", "writing", "55/75 · B2")], []);
        Assert.True(s.Done);
        Assert.Equal("55/75 · B2", s.Score);
    }

    [Fact]
    public void Review_counts_reviews_since_the_assignment()
    {
        var a = A("review", due: T0.AddHours(10), target: 3);
        var reviews = new[] { T0.AddHours(-1), T0.AddHours(1), T0.AddHours(2) };
        var s = GroupLogic.StatusFor(a, [], reviews);
        Assert.False(s.Done);
        Assert.Equal(2, s.Progress);
        Assert.Equal(3, s.Target);

        var done = GroupLogic.StatusFor(a, [], [.. reviews, T0.AddHours(11), T0.AddHours(12)]);
        Assert.True(done.Done);
        Assert.True(done.Late);                       // 3-takrorlash muddatdan keyin
        Assert.Equal(T0.AddHours(11), done.DoneAtUtc);
    }

    [Theory]
    [InlineData("""{"overall":6.5,"module":"speaking"}""", "6.5")]
    [InlineData("""{"overall":7,"module":"writing"}""", "7.0")]
    [InlineData("""{"overall":55,"level":"B2"}""", "55/75 · B2")]
    [InlineData("""{"score":3,"total":4,"results":[]}""", "3/4")]
    [InlineData("""{"fluency":{"score":80,"reasoning":"x"},"grammar":{"score":70,"reasoning":"y"},"vocabulary":{"score":75,"reasoning":"z"}}""", "75/100")]
    [InlineData("""{"transcript":"hi"}""", null)]
    [InlineData("not json", null)]
    public void Score_text(string json, string? expected) => Assert.Equal(expected, GroupLogic.ScoreText(json));

    [Fact]
    public void Teacher_sees_nickname_or_the_local_part_of_the_email_only()
    {
        Assert.Equal("Aziza", GroupLogic.DisplayNameOf(" Aziza ", "aziza@mail.com"));
        Assert.Equal("aziza.k", GroupLogic.DisplayNameOf(null, "aziza.k@mail.com"));
        Assert.Equal("aziza.k", GroupLogic.DisplayNameOf("  ", "aziza.k@mail.com"));
    }
}
