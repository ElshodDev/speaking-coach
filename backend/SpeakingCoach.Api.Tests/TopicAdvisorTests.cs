using SpeakingCoach.Application.Telegram;

namespace SpeakingCoach.Api.Tests;

public class TopicAdvisorTests
{
    private static readonly DateTime Now = new(2026, 11, 22, 12, 0, 0, DateTimeKind.Utc);

    private static MistakeFact F(string original, string corrected, string explanation = "", int daysAgo = 1) =>
        new(original, corrected, explanation, Now.AddDays(-daysAgo));

    private static MistakeReport Report(params MistakeFact[] facts) => MistakeAnalyzer.Summarize(facts, Now);

    private static IEnumerable<MistakeFact> Many(int n, string original, string corrected, string explanation = "", int daysAgo = 1) =>
        Enumerable.Range(1, n).Select(i => F($"{original} {i}", $"{corrected} {i}", explanation, daysAgo));

    // ---- Mavzular ----

    [Fact]
    public void Every_topic_is_valid_for_tutor_and_for_teacher_assignments()
    {
        Assert.True(TopicAdvisor.TopicOf.Count >= 7);
        foreach (var (category, topic) in TopicAdvisor.TopicOf)
        {
            Assert.Contains(category, MistakeAnalyzer.All);
            Assert.Equal(topic, Tutor.NormalizeTopic(topic));
            Assert.Equal(topic, GroupLogic.CleanLessonTopic(topic));            // ustoz vazifasiga sig'adi (<= 33)
            Assert.True(GroupLogic.IsValidKind(GroupLogic.LessonKind(topic)), topic);
        }
        Assert.Equal(TopicAdvisor.TopicOf.Values.Count(), TopicAdvisor.TopicOf.Values.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void Topics_without_a_grammar_subject_are_not_recommended()
    {
        foreach (var id in new[] { MistakeAnalyzer.Spelling, MistakeAnalyzer.WordChoice, MistakeAnalyzer.Other })
            Assert.DoesNotContain(id, TopicAdvisor.TopicOf.Keys);
    }

    [Fact]
    public void All_topics_keep_the_existing_picker_indexes_and_add_new_ones_at_the_end()
    {
        for (var i = 0; i < Tutor.SuggestedTopics.Length; i++) Assert.Equal(Tutor.SuggestedTopics[i], Tutor.AllTopics[i]);
        Assert.Equal(Tutor.AllTopics.Length, Tutor.AllTopics.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        foreach (var topic in TopicAdvisor.TopicOf.Values) Assert.Contains(topic, Tutor.AllTopics);
        Assert.True(Tutor.AllTopics.Length > Tutor.SuggestedTopics.Length);
    }

    // ---- Tavsiya ----

    [Fact]
    public void Recommends_the_most_frequent_recent_grammar_categories()
    {
        var facts = Many(5, "I have a apple", "I have an apple", "article")
            .Concat(Many(3, "depend of", "depend on", "preposition"))
            .Concat(Many(2, "She go", "She goes", "third person"))
            .ToArray();
        var recs = TopicAdvisor.Recommend(Report(facts));
        Assert.Equal([MistakeAnalyzer.Articles, MistakeAnalyzer.Prepositions, MistakeAnalyzer.Agreement], recs.Select(r => r.Category).ToArray());
        Assert.Equal("Articles (a/an/the)", recs[0].Topic);
        Assert.Equal(5, recs[0].Count);
        Assert.Equal(5, recs[0].Last30);
        Assert.NotNull(recs[0].Example);
    }

    [Fact]
    public void A_single_mistake_is_not_enough()
    {
        Assert.Empty(TopicAdvisor.Recommend(Report(F("a apple", "an apple", "article"))));
        Assert.Single(TopicAdvisor.Recommend(Report(Many(TopicAdvisor.MinMistakes, "a apple", "an apple", "article").ToArray())));
    }

    [Fact]
    public void Recent_mistakes_outrank_old_ones()
    {
        var facts = Many(6, "a apple", "an apple", "article", daysAgo: 50)      // ko'p, lekin eski
            .Concat(Many(3, "depend of", "depend on", "preposition", daysAgo: 2)) // kam, lekin yangi
            .ToArray();
        var recs = TopicAdvisor.Recommend(Report(facts));
        Assert.Equal(MistakeAnalyzer.Prepositions, recs[0].Category);
        Assert.Equal(MistakeAnalyzer.Articles, recs[1].Category);
        Assert.Equal(0, recs[1].Last30);
    }

    [Fact]
    public void Non_grammar_categories_never_appear_even_when_frequent()
    {
        var facts = Many(10, "recieve", "receive", "spelling").Concat(Many(4, "a apple", "an apple", "article")).ToArray();
        var recs = TopicAdvisor.Recommend(Report(facts));
        Assert.Single(recs);
        Assert.Equal(MistakeAnalyzer.Articles, recs[0].Category);
    }

    [Fact]
    public void Recently_mastered_topics_are_skipped_case_insensitively()
    {
        var facts = Many(5, "a apple", "an apple", "article").Concat(Many(3, "depend of", "depend on", "preposition")).ToArray();
        var recs = TopicAdvisor.Recommend(Report(facts), ["ARTICLES (A/AN/THE)"]);
        Assert.Equal([MistakeAnalyzer.Prepositions], recs.Select(r => r.Category).ToArray());
    }

    [Fact]
    public void The_limit_is_respected_and_empty_input_is_fine()
    {
        var facts = Many(5, "a apple", "an apple", "article")
            .Concat(Many(4, "depend of", "depend on", "preposition"))
            .Concat(Many(3, "She go", "She goes", "third person"))
            .Concat(Many(2, "more better", "better", "comparative"))
            .ToArray();
        Assert.Equal(3, TopicAdvisor.Recommend(Report(facts)).Count);
        Assert.Equal(1, TopicAdvisor.Recommend(Report(facts), null, 1).Count);
        Assert.Equal(4, TopicAdvisor.Recommend(Report(facts), null, 10).Count);
        Assert.Empty(TopicAdvisor.Recommend(Report(facts), null, 0));
        Assert.Empty(TopicAdvisor.Recommend(Report()));
    }

    // ---- O'zlashtirilganlar ----

    [Theory]
    [InlineData("""{"result":{"mastery":"strong"}}""", "strong")]
    [InlineData("""{"result":{"mastery":"weak"}}""", "weak")]
    [InlineData("""{"result":{}}""", null)]
    [InlineData("""{"mastery":"strong"}""", null)]
    [InlineData("not json", null)]
    [InlineData(null, null)]
    public void Mastery_is_read_from_the_lesson_result(string? json, string? expected) =>
        Assert.Equal(expected, LessonInsights.MasteryOf(json));

    // ---- Bot ----

    [Fact]
    public void Bot_buttons_put_recommendations_first_and_keep_the_picker()
    {
        var rows = BotTutor.TopicButtons(["Articles (a/an/the)", "English word order", "Unknown topic"]);
        Assert.Equal("🎯 Articles (a/an/the)", rows[0][0].Text);
        Assert.Equal("🎯 English word order", rows[1][0].Text);
        Assert.Equal(Tutor.SuggestedTopics.Length + 2, rows.Sum(r => r.Count));    // noma'lum mavzu tashlab yuboriladi
        foreach (var b in rows.SelectMany(r => r))
        {
            var cb = Assert.IsType<BotCallback.Tutor>(BotLogic.Decode(b.CallbackData));
            Assert.True(System.Text.Encoding.UTF8.GetByteCount(b.CallbackData!) <= 64);
            var pick = BotTutor.ParseTopicArg(cb.Arg)!.Value;
            Assert.Contains(Tutor.AllTopics[pick.Index], b.Text);
            Assert.Equal(b.Text.StartsWith("🎯"), pick.Recommended);       // tavsiya tugmasi boshqacha belgilanadi (o'lchov uchun)
        }
    }

    [Fact]
    public void Bot_callbacks_accept_every_known_topic_index_and_nothing_beyond()
    {
        for (var i = 0; i < Tutor.AllTopics.Length; i++) Assert.NotNull(BotLogic.Decode($"tu:t:{i}"));
        Assert.Null(BotLogic.Decode($"tu:t:{Tutor.AllTopics.Length}"));
    }

    [Fact]
    public void Bot_recommend_text_exists_in_all_languages()
    {
        foreach (var lang in Texts.Langs) Assert.False(string.IsNullOrWhiteSpace(Texts.Get(lang, "tutor.bot_recommend")));
    }

    [Fact]
    public void Corrections_query_still_reads_only_the_mistake_sources()
    {
        foreach (var t in new[] { "Speaking", "Writing", "MockExam", "Conversation" }) Assert.Contains($"'{t}'", MistakeQuery.CorrectionsSql);
        Assert.Equal(MistakeQuery.CorrectionsSql, SpeakingCoach.Api.Endpoints.MistakeEndpoints.CorrectionsSql);
    }
}
