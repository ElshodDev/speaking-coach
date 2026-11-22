using System.Text.Json;

namespace SpeakingCoach.Api.Tests;

public class LessonAssignmentTests
{
    private static readonly DateTime T0 = new(2026, 11, 22, 10, 0, 0, DateTimeKind.Utc);

    private static Assignment Lesson(string topic = "Modal verbs", DateTime? due = null) =>
        new() { Id = Guid.NewGuid(), GroupId = Guid.NewGuid(), Kind = GroupLogic.LessonKind(topic), Instructions = "", DueAtUtc = due, CreatedAtUtc = T0 };

    private static ActivityRef Done(string? topic, DateTime at, string? score = "3/4", ActivityType type = ActivityType.Lesson) =>
        new(Guid.NewGuid(), type, at, null, null, score, topic);

    // ---- Tur ----

    [Theory]
    [InlineData("lesson:Modal verbs", "Modal verbs")]
    [InlineData("lesson:Past Simple vs Past Continuous", "Past Simple vs Past Continuous")]   // 30 belgi
    [InlineData("lesson:Oʻtgan zamon", "Oʻtgan zamon")]
    [InlineData("lesson:Time: past/present", "Time: past/present")]                              // ikki nuqta ham mumkin
    public void Valid_lesson_kinds(string kind, string topic)
    {
        Assert.True(GroupLogic.IsValidKind(kind));
        Assert.Equal(topic, GroupLogic.LessonTopic(kind));
    }

    [Theory]
    [InlineData("lesson:")]
    [InlineData("lesson:a")]
    [InlineData("lesson: Modal verbs")]        // tozalanmagan: ikki xil yozuv bitta mavzuni ikkiga bo'lmasin
    [InlineData("lesson:Modal  verbs")]
    [InlineData("lesson:Modal verbs ")]
    [InlineData("lesson:bad\u0007char")]
    [InlineData("Lesson:Modal verbs")]
    [InlineData("lessons:Modal verbs")]
    public void Invalid_lesson_kinds(string kind)
    {
        Assert.False(GroupLogic.IsValidKind(kind));
        Assert.Null(GroupLogic.LessonTopic(kind));
    }

    [Fact]
    public void Topic_must_fit_the_kind_column_of_40_characters()
    {
        var longest = new string('a', GroupLogic.MaxLessonTopicChars);
        Assert.True(GroupLogic.LessonKind(longest).Length <= 40);
        Assert.True(GroupLogic.IsValidKind(GroupLogic.LessonKind(longest)));
        Assert.False(GroupLogic.IsValidKind(GroupLogic.LessonKind(new string('a', GroupLogic.MaxLessonTopicChars + 1))));
    }

    [Fact]
    public void Clean_topic_normalizes_whitespace()
    {
        Assert.Equal("Modal verbs", GroupLogic.CleanLessonTopic("  Modal   verbs \n"));
        Assert.Null(GroupLogic.CleanLessonTopic("x"));
        Assert.Null(GroupLogic.CleanLessonTopic(null));
    }

    [Fact]
    public void Existing_kinds_still_validate()
    {
        foreach (var k in new[] { "review", "practice:speaking", "mock:ielts:reading", "mock:cefr:writing" }) Assert.True(GroupLogic.IsValidKind(k));
        foreach (var k in new[] { "practice:dancing", "mock:ielts", "", null }) Assert.False(GroupLogic.IsValidKind(k));
    }

    [Fact]
    public void Lesson_assignments_need_lesson_activities_and_route_to_the_tutor()
    {
        Assert.Equal([ActivityType.Lesson], GroupLogic.NeededTypes([Lesson()]).ToArray());
        var mixed = GroupLogic.NeededTypes([Lesson(), new Assignment { Kind = "practice:speaking" }]);
        Assert.Contains(ActivityType.Lesson, mixed);
        Assert.Contains(ActivityType.Speaking, mixed);
        Assert.Equal("tutor", GroupLogic.RouteFor("lesson:Modal verbs"));
        Assert.Equal("practice/speaking", GroupLogic.RouteFor("practice:speaking"));
    }

    // ---- Holat ----

    [Fact]
    public void Lesson_is_done_by_the_first_matching_lesson_after_the_assignment()
    {
        var a = Lesson();
        var hit = Done("Modal verbs", T0.AddHours(2), "3/4");
        var st = GroupLogic.StatusFor(a, [Done("Modal verbs", T0.AddHours(5), "4/4"), hit], []);
        Assert.True(st.Done);
        Assert.False(st.Late);
        Assert.Equal(hit.Id, st.ActivityId);
        Assert.Equal("3/4", st.Score);
        Assert.Equal(T0.AddHours(2), st.DoneAtUtc);
    }

    [Fact]
    public void Topic_match_ignores_case_but_not_other_topics()
    {
        var a = Lesson("Modal verbs");
        Assert.True(GroupLogic.StatusFor(a, [Done("MODAL VERBS", T0.AddMinutes(1))], []).Done);
        Assert.False(GroupLogic.StatusFor(a, [Done("Present Perfect", T0.AddMinutes(1))], []).Done);
        Assert.False(GroupLogic.StatusFor(a, [Done(null, T0.AddMinutes(1))], []).Done);
    }

    [Fact]
    public void Lessons_before_the_assignment_and_other_activity_types_do_not_count()
    {
        var a = Lesson();
        Assert.False(GroupLogic.StatusFor(a, [Done("Modal verbs", T0.AddMinutes(-1))], []).Done);
        Assert.False(GroupLogic.StatusFor(a, [Done("Modal verbs", T0.AddMinutes(1), type: ActivityType.Conversation)], []).Done);
    }

    [Fact]
    public void Late_lessons_are_marked_late()
    {
        var a = Lesson(due: T0.AddDays(1));
        Assert.False(GroupLogic.StatusFor(a, [Done("Modal verbs", T0.AddHours(5))], []).Late);
        Assert.True(GroupLogic.StatusFor(a, [Done("Modal verbs", T0.AddDays(2))], []).Late);
    }

    [Fact]
    public void Other_kinds_are_unaffected_by_lesson_activities()
    {
        var a = new Assignment { Kind = "practice:speaking", CreatedAtUtc = T0 };
        Assert.False(GroupLogic.StatusFor(a, [Done("Modal verbs", T0.AddMinutes(1))], []).Done);
    }

    // ---- Ball matni ----

    [Fact]
    public void Score_text_shows_the_mini_test()
    {
        Assert.Equal("3/4", GroupLogic.ScoreText("""{"result":{"practiceCorrect":5,"practiceTotal":6,"testCorrect":3,"testTotal":4,"mastery":"strong"},"writing":null}"""));
        // Eski ko'rinishlar o'zgarmagan.
        Assert.Equal("6.5", GroupLogic.ScoreText("""{"overall":6.5}"""));
        Assert.Equal("3/4", GroupLogic.ScoreText("""{"score":3,"total":4}"""));
        Assert.Null(GroupLogic.ScoreText("""{"result":{"x":1}}"""));
    }

    // ---- Tahlil ----

    private static string Response(int testCorrect, int testTotal, string mastery, bool wrote, params (string Original, string Corrected)[] mistakes) =>
        JsonSerializer.Serialize(new
        {
            result = new
            {
                practiceCorrect = 5, practiceTotal = 6, testCorrect, testTotal, mastery, wroteOwn = wrote,
                mistakes = mistakes.Select(m => new { original = m.Original, corrected = m.Corrected, explanation = "why " + m.Corrected }),
            },
            writing = (object?)null,
        }, Tutor.Json);

    [Fact]
    public void Topic_is_read_from_the_prompt_data()
    {
        Assert.Equal("Modal verbs", LessonInsights.TopicOf("""{"level":"B1","topic":"Modal verbs"}"""));
        Assert.Null(LessonInsights.TopicOf("""{"level":"B1"}"""));
        Assert.Null(LessonInsights.TopicOf("not json"));
        Assert.Null(LessonInsights.TopicOf(null));
        Assert.Null(LessonInsights.TopicOf("""{"topic":5}"""));
    }

    [Fact]
    public void Insights_aggregate_scores_mastery_and_common_mistakes()
    {
        var r = LessonInsights.Build(10,
        [
            Response(4, 4, "strong", true, ("She cans swim", "She can swim"), ("He go", "He goes")),
            Response(2, 4, "ok", false, ("She cans swim", "She can swim")),
            Response(1, 4, "weak", false, ("She canning swim", "she CAN swim"), ("It rain", "It rains")),
        ]);
        Assert.Equal(10, r.Members);
        Assert.Equal(3, r.Completed);
        Assert.Equal(58, r.AvgTestPercent);                       // (100+50+25)/3 = 58.3
        Assert.Equal((1, 1, 1), (r.Strong, r.Ok, r.Weak));
        Assert.Equal(1, r.WroteOwn);
        Assert.Equal("She can swim", r.TopMistakes[0].Corrected, ignoreCase: true);
        Assert.Equal(3, r.TopMistakes[0].Students);              // registr farqi bitta xato
        Assert.Equal(3, r.TopMistakes.Count);
        Assert.All(r.TopMistakes.Skip(1), m => Assert.Equal(1, m.Students));
    }

    [Fact]
    public void One_student_counts_a_repeated_mistake_once()
    {
        var r = LessonInsights.Build(1, [Response(1, 4, "weak", false, ("a", "X"), ("b", "X"), ("c", "x"))]);
        Assert.Single(r.TopMistakes);
        Assert.Equal(1, r.TopMistakes[0].Students);
    }

    [Fact]
    public void Top_mistakes_are_limited_and_ordered()
    {
        var many = Enumerable.Range(1, 9).Select(i => ($"o{i}", $"m{i}")).ToArray();
        var r = LessonInsights.Build(3, [Response(1, 4, "weak", false, many), Response(1, 4, "weak", false, ("o9", "m9"))]);
        Assert.Equal(LessonInsights.MaxTopMistakes, r.TopMistakes.Count);
        Assert.Equal("m9", r.TopMistakes[0].Corrected);
        Assert.Equal(2, r.TopMistakes[0].Students);
    }

    [Fact]
    public void Insights_ignore_broken_records_and_handle_nobody()
    {
        var empty = LessonInsights.Build(5, []);
        Assert.Equal(0, empty.Completed);
        Assert.Null(empty.AvgTestPercent);
        Assert.Empty(empty.TopMistakes);

        var r = LessonInsights.Build(5, ["not json", "[]", """{"result":5}""", """{"other":1}""", Response(3, 4, "strong", false)]);
        Assert.Equal(1, r.Completed);
        Assert.Equal(75, r.AvgTestPercent);
    }

    // ---- Umumiy dars keshi ----

    private static TutorLesson L(string topic = "Modal verbs") => new(true, topic, [new("a", "b"), new("c", "d")], [], [], "task", "");

    [Fact]
    public async Task Cache_generates_once_for_a_whole_class()
    {
        var cache = new TutorLessonCache();
        var calls = 0;
        var gate = new TaskCompletionSource();
        async Task<TutorLesson> Create()
        {
            Interlocked.Increment(ref calls);
            await gate.Task;
            return L();
        }
        var key = TutorLessonCache.Key("Modal verbs", "B1", "uz");
        var tasks = Enumerable.Range(0, 30).Select(_ => cache.GetOrCreateAsync(key, Create, DateTime.UtcNow)).ToList();
        gate.SetResult();
        var all = await Task.WhenAll(tasks);
        Assert.Equal(1, calls);
        Assert.All(all, l => Assert.Same(all[0], l));
        await cache.GetOrCreateAsync(key, Create, DateTime.UtcNow);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task Cache_does_not_keep_failures()
    {
        var cache = new TutorLessonCache();
        var key = TutorLessonCache.Key("x", "B1", "uz");
        await Assert.ThrowsAsync<InvalidOperationException>(() => cache.GetOrCreateAsync(key, () => throw new InvalidOperationException("ai down"), DateTime.UtcNow));
        Assert.Equal(0, cache.Count);
        var ok = await cache.GetOrCreateAsync(key, () => Task.FromResult(L("x")), DateTime.UtcNow);
        Assert.Equal("x", ok.Topic);
    }

    [Fact]
    public async Task Cache_entries_expire_and_are_separate_per_level_and_language()
    {
        var cache = new TutorLessonCache();
        var now = DateTime.UtcNow;
        var calls = 0;
        Task<TutorLesson> Create() { calls++; return Task.FromResult(L()); }
        var key = TutorLessonCache.Key("Modal verbs", "B1", "uz");
        await cache.GetOrCreateAsync(key, Create, now);
        await cache.GetOrCreateAsync(key, Create, now.AddHours(11));
        Assert.Equal(1, calls);
        await cache.GetOrCreateAsync(key, Create, now.AddHours(13));
        Assert.Equal(2, calls);
        await cache.GetOrCreateAsync(TutorLessonCache.Key("Modal verbs", "B2", "uz"), Create, now);
        await cache.GetOrCreateAsync(TutorLessonCache.Key("Modal verbs", "B1", "ru"), Create, now);
        Assert.Equal(4, calls);
        Assert.Equal(TutorLessonCache.Key("MODAL VERBS ", "B1", "uz"), key);
    }

    [Fact]
    public async Task Cache_size_is_bounded()
    {
        var cache = new TutorLessonCache();
        for (var i = 0; i < TutorLessonCache.MaxEntries + 20; i++)
            await cache.GetOrCreateAsync(TutorLessonCache.Key($"topic {i}", "B1", "uz"), () => Task.FromResult(L()), DateTime.UtcNow);
        Assert.True(cache.Count <= TutorLessonCache.MaxEntries);
    }
}
