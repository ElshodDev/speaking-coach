using System.Net;
using System.Text.Json;
namespace SpeakingCoach.Api.Tests.Integration;

/// <summary>Mock imtihonlar HTTP orqali: kirish, holat, test banki, Listening/Reading oqimi (javoblar brauzerga chiqmaydi), tarix va natija.</summary>
public class MockHttpTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Mock_endpoints_need_a_login_with_a_mock_specific_message()
    {
        var guest = factory.CreateClient();
        foreach (var path in new[] { "/api/mock/status", "/api/mock/history", "/api/mock/ielts/speaking/new", "/api/mock/ielts/reading/bank", "/api/mock/ielts/reading/new" })
        {
            var res = await guest.GetAsync(path);
            Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
            Assert.Equal(Msg.Err("mock.login"), (await res.JsonAsync()).GetProperty("error").GetString());
        }
    }

    [Fact]
    public async Task Status_shows_the_remaining_ai_mocks_and_admins_have_no_limit()
    {
        var u = await factory.CreateUserAsync();
        var status = await (await u.Client.GetAsync("/api/mock/status")).JsonAsync();
        Assert.Equal(4, status.GetProperty("perDay").GetInt32());
        Assert.Equal(4, status.GetProperty("remaining").GetInt32());
        Assert.Equal(JsonValueKind.Null, status.GetProperty("retryAtUtc").ValueKind);

        var admin = await factory.CreateUserAsync(ApiFactory.AdminEmail);
        Assert.Equal(JsonValueKind.Null, (await (await admin.Client.GetAsync("/api/mock/status")).JsonAsync()).GetProperty("remaining").ValueKind);
    }

    [Fact]
    public async Task Unknown_exams_and_modules_are_404()
    {
        var u = await factory.CreateUserAsync();
        Assert.Equal(HttpStatusCode.NotFound, (await u.Client.GetAsync("/api/mock/toefl/reading/bank")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await u.Client.GetAsync("/api/mock/ielts/cooking/bank")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await u.Client.GetAsync("/api/mock/ielts/speaking/new-nothing")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await u.Client.GetAsync($"/api/mock/{Guid.NewGuid()}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await u.Client.GetAsync("/api/mock/session/not-a-guid")).StatusCode);
    }

    [Fact]
    public async Task Speaking_and_writing_sets_come_from_the_built_in_bank_with_their_timing()
    {
        var u = await factory.CreateUserAsync();
        var speaking = await (await u.Client.GetAsync("/api/mock/ielts/speaking/new")).JsonAsync();
        Assert.True(speaking.GetProperty("set").TryGetProperty("id", out _));
        Assert.True(speaking.GetProperty("timing").GetProperty("part2PrepSeconds").GetInt32() > 0);

        var writing = await (await u.Client.GetAsync("/api/mock/ielts/writing/new?variant=general")).JsonAsync();
        Assert.Equal("general", writing.GetProperty("set").GetProperty("variant").GetString());
        Assert.True(writing.GetProperty("timing").GetProperty("minutes").GetInt32() > 0);

        var cefr = await (await u.Client.GetAsync("/api/mock/cefr/speaking/new")).JsonAsync();
        Assert.True(cefr.GetProperty("timing").TryGetProperty("part2PrepSeconds", out _));

        var bank = await (await u.Client.GetAsync("/api/mock/ielts/speaking/bank")).JsonAsync();
        Assert.True(bank.GetProperty("total").GetInt32() > 0);
        Assert.Equal(0, bank.GetProperty("done").GetInt32());
        Assert.Equal(bank.GetProperty("total").GetInt32(), bank.GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task A_reading_test_never_exposes_its_answers_and_a_submission_is_graded_saved_and_not_duplicated()
    {
        var u = await factory.CreateUserAsync();
        var next = await (await u.Client.GetAsync("/api/mock/ielts/reading/new?variant=academic")).JsonAsync();
        var test = next.GetProperty("test");
        var testId = test.GetProperty("id").GetGuid();
        Assert.Equal("bank", next.GetProperty("source").GetString());
        Assert.False(next.GetProperty("repeated").GetBoolean());
        var raw = test.GetRawText();
        Assert.DoesNotContain("\"answers\"", raw);                                                // to'g'ri javoblar brauzerga chiqmaydi
        Assert.DoesNotContain("\"explanation\"", raw);

        var bank = await (await u.Client.GetAsync("/api/mock/ielts/reading/bank?variant=academic")).JsonAsync();
        Assert.True(bank.GetProperty("total").GetInt32() >= 2);
        Assert.Equal(0, bank.GetProperty("done").GetInt32());

        var submit = await u.Client.PostAsync("/api/mock/ielts/reading", JsonExtensions.Json(new { testId, answers = new Dictionary<string, string> { ["1"] = "x", ["2"] = "y" }, secondsUsed = 600 }));
        Assert.Equal(HttpStatusCode.OK, submit.StatusCode);
        var id = (await submit.JsonAsync()).GetProperty("id").GetGuid();

        var again = await (await u.Client.PostAsync("/api/mock/ielts/reading", JsonExtensions.Json(new { testId, answers = new Dictionary<string, string> { ["1"] = "x", ["2"] = "y" }, secondsUsed = 650 }))).JsonAsync();
        Assert.Equal(id, again.GetProperty("id").GetGuid());
        Assert.True(again.GetProperty("duplicate").GetBoolean());                                // ikki marta bosish — yangi XP yo'q

        var history = (await (await u.Client.GetAsync("/api/mock/history")).JsonAsync()).EnumerateArray().Single();
        Assert.Equal(id, history.GetProperty("id").GetGuid());
        Assert.Equal("reading", history.GetProperty("module").GetString());
        Assert.Equal("ielts", history.GetProperty("exam").GetString());

        var result = await (await u.Client.GetAsync($"/api/mock/{id}")).JsonAsync();
        Assert.Equal(JsonValueKind.Object, result.GetProperty("result").ValueKind);
        Assert.Contains("answers", result.GetProperty("test").GetRawText());                      // natija sahifasida tahlil uchun testning o'zi qaytadi

        var nextAgain = await (await u.Client.GetAsync("/api/mock/ielts/reading/new?variant=academic")).JsonAsync();
        Assert.NotEqual(testId, nextAgain.GetProperty("test").GetProperty("id").GetGuid());        // bajarilgani qayta berilmaydi
        Assert.Equal(1, (await (await u.Client.GetAsync("/api/mock/ielts/reading/bank?variant=academic")).JsonAsync()).GetProperty("done").GetInt32());
    }

    [Fact]
    public async Task A_result_belongs_to_its_owner_and_a_bad_test_is_a_400()
    {
        var owner = await factory.CreateUserAsync();
        var other = await factory.CreateUserAsync();
        var testId = (await (await owner.Client.GetAsync("/api/mock/cefr/listening/new")).JsonAsync()).GetProperty("test").GetProperty("id").GetGuid();
        var id = (await (await owner.Client.PostAsync("/api/mock/cefr/listening", JsonExtensions.Json(new { testId, answers = new Dictionary<string, string>(), secondsUsed = 5 }))).JsonAsync()).GetProperty("id").GetGuid();

        Assert.Equal(HttpStatusCode.OK, (await owner.Client.GetAsync($"/api/mock/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.Client.GetAsync($"/api/mock/{id}")).StatusCode);

        var bad = await other.Client.PostAsync("/api/mock/ielts/reading", JsonExtensions.Json(new { testId = Guid.NewGuid(), answers = new Dictionary<string, string>(), secondsUsed = 5 }));
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
        Assert.Equal(Msg.Err("mock.bad_set"), (await bad.JsonAsync()).GetProperty("error").GetString());
    }
}
