using System.Net;
using System.Text.Json;
namespace SpeakingCoach.Api.Tests.Integration;

/// <summary>AI suhbat, O'qish/Tinglash bankdan mashqlari va Part 2 kartochkalari — HTTP orqali.</summary>
public class TalkHttpTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static MultipartFormDataContent Text(string text) => new() { { new StringContent(text), "text" } };
    private static async Task<string> ErrorOf(HttpResponseMessage r) => (await r.JsonAsync()).GetProperty("error").GetString()!;

    private async Task<(TestUser User, Guid TalkId)> StartAsync()
    {
        var u = await factory.CreateUserAsync();
        var start = await (await u.Client.PostAsync("/api/talk/start", JsonExtensions.Json(new { scenario = "cafe", level = "A2" }))).JsonAsync();
        return (u, start.GetProperty("talkId").GetGuid());
    }

    [Fact]
    public async Task Talk_needs_a_login_and_a_known_scenario()
    {
        var guest = await guestStart();
        Assert.Equal(HttpStatusCode.Unauthorized, guest.StatusCode);
        Assert.Equal(Msg.Err("talk.login"), await ErrorOf(guest));

        var u = await factory.CreateUserAsync();
        var bad = await u.Client.PostAsync("/api/talk/start", JsonExtensions.Json(new { scenario = "nope" }));
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
        Assert.Equal(Msg.Err("talk.not_found"), await ErrorOf(bad));

        Task<HttpResponseMessage> guestStart() => factory.CreateClient().PostAsync("/api/talk/start", JsonExtensions.Json(new { scenario = "cafe" }));
    }

    [Fact]
    public async Task A_conversation_goes_start_turn_finish_and_produces_cards_and_history()
    {
        var u = await factory.CreateUserAsync();
        var start = await (await u.Client.PostAsync("/api/talk/start", JsonExtensions.Json(new { scenario = "cafe", level = "A2" }))).JsonAsync();
        var id = start.GetProperty("talkId").GetGuid();
        Assert.Equal("cafe", start.GetProperty("scenario").GetString());
        Assert.StartsWith("Hi there!", start.GetProperty("opening").GetString());
        Assert.Equal(Talk.MaxTurns, start.GetProperty("maxTurns").GetInt32());

        var noTurns = await u.Client.PostAsync($"/api/talk/{id}/finish", null);
        Assert.Equal(HttpStatusCode.BadRequest, noTurns.StatusCode);
        Assert.Equal(Msg.Err("talk.no_turns"), await ErrorOf(noTurns));                          // hali bitta ham javob yo'q

        var turn = await (await u.Client.PostAsync($"/api/talk/{id}/turn", Text("I want a coffee"))).JsonAsync();
        Assert.Equal("I want a coffee", turn.GetProperty("heard").GetString());
        Assert.Equal("Sure, anything else?", turn.GetProperty("reply").GetString());
        Assert.Equal(1, turn.GetProperty("turn").GetInt32());
        Assert.Equal(JsonValueKind.Object, turn.GetProperty("tip").ValueKind);

        var done = await u.Client.PostAsync($"/api/talk/{id}/finish", null);
        Assert.Equal(HttpStatusCode.OK, done.StatusCode);
        var json = await done.JsonAsync();
        Assert.Equal("Nice chat", json.GetProperty("result").GetProperty("summary").GetString());
        Assert.Equal(1, json.GetProperty("turns").GetInt32());
        Assert.True(json.GetProperty("newCards").GetInt32() >= 1);                                // xulosa va maslahatlardan kartalar

        Assert.Equal(HttpStatusCode.NotFound, (await u.Client.PostAsync($"/api/talk/{id}/finish", null)).StatusCode);   // yakunlangan suhbat yo'q
        Assert.Equal(HttpStatusCode.NotFound, (await u.Client.PostAsync($"/api/talk/{id}/turn", Text("hello"))).StatusCode);
    }

    [Fact]
    public async Task Turn_validation_ownership_and_the_turn_limit()
    {
        var (u, id) = await StartAsync();
        var empty = await u.Client.PostAsync($"/api/talk/{id}/turn", new MultipartFormDataContent { { new StringContent(""), "text" } });
        Assert.Equal(HttpStatusCode.BadRequest, empty.StatusCode);
        Assert.Equal(Msg.Err("talk.empty"), await ErrorOf(empty));
        var notMultipart = await u.Client.PostAsync($"/api/talk/{id}/turn", JsonExtensions.Json(new { text = "hi" }));
        Assert.Equal(Msg.Err("speaking.multipart"), await ErrorOf(notMultipart));

        var stranger = await factory.CreateUserAsync();
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.Client.PostAsync($"/api/talk/{id}/turn", Text("hi"))).StatusCode);   // begona suhbat
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.Client.PostAsync($"/api/talk/{id}/finish", null)).StatusCode);

        for (var i = 0; i < Talk.MaxTurns; i++) Assert.Equal(HttpStatusCode.OK, (await u.Client.PostAsync($"/api/talk/{id}/turn", Text("answer " + i))).StatusCode);
        var over = await u.Client.PostAsync($"/api/talk/{id}/turn", Text("one more"));
        Assert.Equal(HttpStatusCode.BadRequest, over.StatusCode);
        Assert.Equal(Msg.Err("talk.limit", Talk.MaxTurns), await ErrorOf(over));
    }

    [Fact]
    public async Task Nothing_heard_is_422_and_an_ai_failure_is_502_without_losing_the_conversation()
    {
        var (u, id) = await StartAsync();
        factory.Ai.TalkHeard = "";
        try
        {
            var res = await u.Client.PostAsync($"/api/talk/{id}/turn", Text("..."));
            Assert.Equal(HttpStatusCode.UnprocessableEntity, res.StatusCode);
            Assert.Equal(Msg.Err("talk.not_heard"), await ErrorOf(res));
        }
        finally
        {
            factory.Ai.TalkHeard = null;
        }

        factory.Ai.TalkFailure = new HttpRequestException("down");
        try
        {
            Assert.Equal(HttpStatusCode.BadGateway, (await u.Client.PostAsync($"/api/talk/{id}/turn", Text("hello"))).StatusCode);
        }
        finally
        {
            factory.Ai.TalkFailure = null;
        }
        Assert.Equal(1, (await (await u.Client.PostAsync($"/api/talk/{id}/turn", Text("hello again"))).JsonAsync()).GetProperty("turn").GetInt32());   // suhbat saqlanib qoldi
    }
}

public class PracticeBankHttpTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task A_ready_made_reading_exercise_hides_its_answers_and_is_consumed_by_its_submission()
    {
        var u = await factory.CreateUserAsync();
        var bank = await (await u.Client.GetAsync("/api/reading/bank?level=B1")).JsonAsync();
        Assert.Equal("B1", bank.GetProperty("level").GetString());
        Assert.True(bank.GetProperty("total").GetInt32() > 0);
        Assert.Equal(0, bank.GetProperty("done").GetInt32());

        var ex = await (await u.Client.PostAsync("/api/reading/generate", JsonExtensions.Json(new { level = "B1", source = "bank" }))).JsonAsync();
        Assert.Equal("bank", ex.GetProperty("source").GetString());
        var questions = ex.GetProperty("questions");
        Assert.True(questions.GetArrayLength() > 0);
        Assert.DoesNotContain("correctIndex", ex.GetRawText());                                   // to'g'ri javob serverda qoladi
        Assert.DoesNotContain("explanation", ex.GetRawText());

        var exerciseId = ex.GetProperty("exerciseId").GetGuid();
        var answers = Enumerable.Repeat(0, questions.GetArrayLength()).ToArray();
        var res = await u.Client.PostAsync("/api/reading/submit", JsonExtensions.Json(new { exerciseId, answers }));
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var result = await res.JsonAsync();
        Assert.Equal(questions.GetArrayLength(), result.GetProperty("total").GetInt32());
        Assert.True(result.GetProperty("saved").GetBoolean());
        Assert.False(string.IsNullOrEmpty(result.GetProperty("passage").GetString()));

        var again = await u.Client.PostAsync("/api/reading/submit", JsonExtensions.Json(new { exerciseId, answers }));
        Assert.Equal(HttpStatusCode.NotFound, again.StatusCode);                                  // mashq bir marta ishlatiladi
        Assert.Single((await (await u.Client.GetAsync("/api/reading/history")).JsonAsync()).EnumerateArray());
        Assert.Equal(1, (await (await u.Client.GetAsync("/api/reading/bank?level=B1")).JsonAsync()).GetProperty("done").GetInt32());
    }

    [Fact]
    public async Task Guests_can_practise_but_nothing_is_saved_and_a_wrong_answer_count_is_a_400()
    {
        var guest = factory.CreateClient();
        var ex = await (await guest.PostAsync("/api/listening/generate", JsonExtensions.Json(new { level = "A2", source = "bank" }))).JsonAsync();
        var id = ex.GetProperty("exerciseId").GetGuid();
        var count = ex.GetProperty("questions").GetArrayLength();

        var bad = await guest.PostAsync("/api/listening/submit", JsonExtensions.Json(new { exerciseId = id, answers = new[] { 0 } }));
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);

        var ok = await (await guest.PostAsync("/api/listening/submit", JsonExtensions.Json(new { exerciseId = id, answers = new int[count] }))).JsonAsync();
        Assert.False(ok.GetProperty("saved").GetBoolean());
        Assert.Empty((await (await guest.GetAsync("/api/listening/history")).JsonAsync()).EnumerateArray());   // mehmonning tarixi yo'q
        Assert.Equal(HttpStatusCode.NotFound, (await guest.PostAsync("/api/listening/submit", JsonExtensions.Json(new { exerciseId = Guid.NewGuid(), answers = new int[count] }))).StatusCode);
    }

    [Fact]
    public async Task Part2_cards_are_listed_and_the_ones_the_user_did_are_marked()
    {
        var u = await factory.CreateUserAsync();
        var list = await (await u.Client.GetAsync("/api/part2/cards")).JsonAsync();
        Assert.True(list.GetProperty("prepSeconds").GetInt32() > 0);
        var cards = list.GetProperty("cards");
        Assert.True(cards.GetArrayLength() > 0);
        Assert.All(cards.EnumerateArray(), c => Assert.False(c.GetProperty("done").GetBoolean()));

        var cardId = cards[0].GetProperty("id").GetString()!;
        await factory.WithDbAsync(async db =>
        {
            db.Activities.Add(new Activity { Id = Guid.NewGuid(), Type = ActivityType.Speaking, UserId = u.Id, CreatedAtUtc = DateTime.UtcNow, PromptData = JsonSerializer.Serialize(new { topic = "t", part2 = true, cardId }), ResponseData = "{}" });
            await db.SaveChangesAsync();
        });
        var after = (await (await u.Client.GetAsync("/api/part2/cards")).JsonAsync()).GetProperty("cards");
        Assert.True(after.EnumerateArray().Single(c => c.GetProperty("id").GetString() == cardId).GetProperty("done").GetBoolean());
    }
}
