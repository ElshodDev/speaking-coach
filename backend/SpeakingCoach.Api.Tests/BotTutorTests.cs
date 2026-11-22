using System.Text;
using SpeakingCoach.Application.Telegram;

namespace SpeakingCoach.Api.Tests;

public class BotTutorTests
{
    private static TutorExercise Q(string prompt, string explanation = "why") => new(prompt, ["can", "cans", "canning"], 0, explanation);

    private static TutorState State(int practice = 3, int test = 2) => new(
        Guid.NewGuid(), "Modal <verbs> & co", "B1", "uz",
        [new("Idea", "Modals <b>express</b> ability."), new("Use", "can/must")],
        Enumerable.Range(1, practice).Select(i => Q($"She ___ swim {i}.")).ToList(),
        Enumerable.Range(1, test).Select(i => Q($"He ___ run {i}.")).ToList(),
        "Write 3 sentences.", "I can swim.", []);

    [Fact]
    public void Await_state_round_trips_and_fits_the_botstate_column()
    {
        var id = Guid.NewGuid();
        Assert.Equal((TutorAwait.Topic, (Guid?)null), BotTutor.ParseState(BotTutor.State(TutorAwait.Topic)));
        Assert.Equal((TutorAwait.Write, (Guid?)id), BotTutor.ParseState(BotTutor.State(TutorAwait.Write, id)));
        Assert.Equal((TutorAwait.Ask, (Guid?)id), BotTutor.ParseState(BotTutor.State(TutorAwait.Ask, id)));
        Assert.True(BotTutor.State(TutorAwait.Write, id).Length <= 100);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("au:ielts:reading:academic:gen")]
    [InlineData("tu:")]
    [InlineData("tu:w:not-a-guid")]
    [InlineData("tu:x:00000000000000000000000000000000")]
    public void Foreign_or_broken_state_is_not_tutor_state(string? s) => Assert.Null(BotTutor.ParseState(s));

    [Fact]
    public void Authoring_state_is_not_confused_with_tutor_state()
    {
        var authoring = BotAuthoring.State(AuthorKind.All[0], true);
        Assert.Null(BotTutor.ParseState(authoring));
        Assert.Null(BotAuthoring.ParseState(BotTutor.State(TutorAwait.Topic)));
    }

    [Fact]
    public void Callbacks_round_trip_and_stay_within_telegrams_64_bytes()
    {
        var id = Guid.NewGuid();
        BotCallback.Tutor[] all =
        [
            new(null, "t", "0"), new(null, "t", "7"), new(null, "t", "new"),
            new(id, "a", "p0:2"), new(id, "a", "t3:1"), new(id, "g"), new(id, "w"), new(id, "s"), new(id, "q"),
        ];
        foreach (var cb in all)
        {
            var data = BotLogic.Encode(cb);
            Assert.True(Encoding.UTF8.GetByteCount(data) <= 64, data);
            Assert.Equal(cb, BotLogic.Decode(data));
        }
    }

    [Theory]
    [InlineData("tu:t:99")]
    [InlineData("tu:t:-1")]
    [InlineData("tu:t:x")]
    [InlineData("tu:zzzz:g")]
    [InlineData("tu:00000000000000000000000000000000:z")]
    [InlineData("tu:00000000000000000000000000000000:a:p0")]
    [InlineData("tu:00000000000000000000000000000000:a:x0:1")]
    [InlineData("tu:00000000000000000000000000000000:a:p0:12")]
    [InlineData("tu:00000000000000000000000000000000:a:p100:1")]
    public void Broken_tutor_callbacks_decode_to_null(string data) => Assert.Null(BotLogic.Decode(data));

    [Fact]
    public void Answer_arg_parsing()
    {
        Assert.True(BotTutor.IsAnswerArg("p5:3"));
        Assert.False(BotTutor.IsAnswerArg("q5:3"));
        Assert.Equal(("practice", 5, 3), BotTutor.ParseAnswerArg("p5:3"));
        Assert.Equal(("test", 12, 0), BotTutor.ParseAnswerArg("t12:0"));
        Assert.Equal("t2:1", BotTutor.AnswerArg("test", 2, 1));
        Assert.Equal("p0:2", BotTutor.AnswerArg("practice", 0, 2));
    }

    [Fact]
    public void Next_step_walks_practice_then_offers_writing_then_test_then_done()
    {
        var s = State();
        Assert.Equal(new TutorNext("question", "practice", 0), BotTutor.NextStep(s));
        Tutor.Answer(s, "practice", 0, 0);
        Assert.Equal(new TutorNext("question", "practice", 1), BotTutor.NextStep(s));
        Tutor.Answer(s, "practice", 1, 0);
        Tutor.Answer(s, "practice", 2, 0);
        Assert.Equal("write", BotTutor.NextStep(s).Kind);

        // Yozmani o'tkazib, mini-testga: yozma taklifi qayta chiqmaydi.
        Tutor.Answer(s, "test", 0, 0);
        Assert.Equal(new TutorNext("question", "test", 1), BotTutor.NextStep(s));
        Tutor.Answer(s, "test", 1, 0);
        Assert.Equal("done", BotTutor.NextStep(s).Kind);
    }

    [Fact]
    public void Writing_done_goes_straight_to_the_test()
    {
        var s = State() with { Writing = new TutorWriting("ok", [], "") };
        foreach (var i in Enumerable.Range(0, 3)) Tutor.Answer(s, "practice", i, 0);
        Assert.Equal(new TutorNext("question", "test", 0), BotTutor.NextStep(s));
    }

    [Fact]
    public void Texts_escape_html_so_telegram_does_not_reject_them()
    {
        var s = State();
        var explain = BotTutor.ExplainText("uz", s);
        Assert.Contains("Modal &lt;verbs&gt; &amp; co", explain);
        Assert.Contains("&lt;b&gt;express&lt;/b&gt;", explain);
        Assert.DoesNotContain("<verbs>", explain);
        Assert.True(explain.Length < 4096);
    }

    [Fact]
    public void Question_and_answered_texts()
    {
        var s = State();
        var q = BotTutor.QuestionText("uz", s, "practice", 1);
        Assert.Contains("Mashq 2/3", q);
        Assert.Contains("She ___ swim 2.", q);
        Assert.DoesNotContain("why", q);

        Tutor.Answer(s, "practice", 0, 1);                       // xato (to'g'ri — 0-variant matn "can" emas, indeks)
        var wrongIdx = s.Picked["p0"];
        var text = BotTutor.AnsweredText("uz", s, "practice", 0);
        var wrongOpt = s.Practice[0].Options[wrongIdx];
        Assert.Contains(wrongIdx == s.Practice[0].AnswerIndex ? "Toʻgʻri" : "Notoʻgʻri", text);
        Assert.Contains("why", text);
        if (wrongIdx != s.Practice[0].AnswerIndex) Assert.Contains($"<s>{wrongOpt}</s>", text);
        Assert.Contains("savol 1/2", BotTutor.QuestionText("uz", s, "test", 0));
        Assert.Contains("Exercise 1/3", BotTutor.QuestionText("en", s, "practice", 0));
    }

    [Fact]
    public void Option_buttons_one_per_row_with_valid_callbacks()
    {
        var id = Guid.NewGuid();
        var rows = BotTutor.OptionButtons(id, "test", 1, Q("x ___"));
        Assert.Equal(3, rows.Count);
        Assert.All(rows, r => Assert.Single(r));
        Assert.Equal(new BotCallback.Tutor(id, "a", "t1:2"), BotLogic.Decode(rows[2][0].CallbackData));
        Assert.Equal("canning", rows[2][0].Text);
    }

    [Fact]
    public void Topic_buttons_cover_all_suggestions_two_per_row()
    {
        var rows = BotTutor.TopicButtons();
        Assert.Equal(Tutor.SuggestedTopics.Length, rows.Sum(r => r.Count));
        Assert.All(rows, r => Assert.True(r.Count <= 2));
        for (var i = 0; i < Tutor.SuggestedTopics.Length; i++)
            Assert.Contains(rows.SelectMany(r => r), b => b.Text == Tutor.SuggestedTopics[i] && BotLogic.Decode(b.CallbackData) == new BotCallback.Tutor(null, "t", i.ToString()));
    }

    [Fact]
    public void Writing_and_result_texts()
    {
        var w = new TutorWriting("Good <try>", [new CorrectionItem("I must to go", "I must go", "no to")], "I must go.");
        var wt = BotTutor.WritingText("uz", w);
        Assert.Contains("Good &lt;try&gt;", wt);
        Assert.Contains("<s>I must to go</s> → <b>I must go</b>", wt);
        Assert.Contains("I must go.", wt);

        var s = State();
        foreach (var i in Enumerable.Range(0, 3)) Tutor.Answer(s, "practice", i, (s.Practice[i].AnswerIndex + 1) % 3);
        foreach (var i in Enumerable.Range(0, 2)) Tutor.Answer(s, "test", i, s.Test[i].AnswerIndex);
        var result = Tutor.Summarize(s);
        var text = BotTutor.ResultText("en", new TutorFinished(Guid.NewGuid(), s, result, 3));
        Assert.Contains("<b>0/3</b>", text);
        Assert.Contains("<b>2/2</b>", text);
        Assert.Contains("Great!", text);
        Assert.Contains("3 cards added", text);
        Assert.True(text.Split("•").Length - 1 <= 5);
        Assert.DoesNotContain("cards added", BotTutor.ResultText("en", new TutorFinished(Guid.NewGuid(), s, result, 0)));
    }

    [Fact]
    public void Every_bot_tutor_text_exists_in_all_languages()
    {
        var keys = new[]
        {
            "tutor.bot_ask_topic", "tutor.bot_preparing", "tutor.bot_checking", "tutor.bot_to_practice", "tutor.bot_ask_btn", "tutor.bot_next",
            "tutor.bot_continue", "tutor.bot_step_practice", "tutor.bot_step_test", "tutor.bot_correct", "tutor.bot_wrong", "tutor.bot_write_prompt",
            "tutor.bot_write_btn", "tutor.bot_skip_btn", "tutor.bot_corrections", "tutor.bot_improved", "tutor.bot_ask_prompt", "tutor.bot_asks_left",
            "tutor.bot_result", "tutor.bot_mastery_strong", "tutor.bot_mastery_ok", "tutor.bot_mastery_weak", "tutor.bot_mistakes", "tutor.bot_cards",
            "tutor.bot_review_btn", "tutor.bot_new_btn", "tutor.bot_cancelled", "tutor.bot_cmd",
        };
        foreach (var k in keys)
            foreach (var lang in Texts.Langs)
                Assert.False(string.IsNullOrWhiteSpace(Texts.Get(lang, k, "A", "B", "C", "D", "E")), $"{k}/{lang}");
        // Telegram buyruq tavsifi — 3..256 belgi.
        foreach (var lang in Texts.Langs) Assert.InRange(Texts.Get(lang, "tutor.bot_cmd").Length, 3, 256);
    }
}
