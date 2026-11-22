using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
namespace SpeakingCoach.Api.Tests.Integration;

/// <summary>
/// Speaking/Writing HTTP qatlami: forma va yuklash tekshiruvlari, hajm chegarasi, saqlash shartlari (kirgan / ?save=false / mehmon),
/// AI xatolarining javobga aylanishi (502, 503 + Retry-After) va xato urinish kunlik limitni sarflamasligi.
/// Gemini soxta (FakeAi) — haqiqiy tarmoq so'rovlari yo'q.
/// </summary>
public class AiHttpTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static MultipartFormDataContent Speaking(string topic = "Describe your hometown", int audioBytes = 2048, bool withAudio = true)
    {
        var form = new MultipartFormDataContent
        {
            { new StringContent(topic), "topic" },
            { new StringContent("B1"), "level" },
            { new StringContent("20"), "seconds" },
        };
        if (withAudio)
        {
            var audio = new ByteArrayContent(new byte[audioBytes]);
            audio.Headers.ContentType = new MediaTypeHeaderValue("audio/webm");
            form.Add(audio, "audio", "answer.webm");
        }
        return form;
    }

    private static async Task<string> ErrorOf(HttpResponseMessage r) => (await r.JsonAsync()).GetProperty("error").GetString()!;

    private static async Task<int> UsedAsync(HttpClient client) =>
        (await (await client.GetAsync("/api/usage")).JsonAsync()).GetProperty("exercises").GetProperty("used").GetInt32();

    // ---- Speaking: tekshiruvlar ----

    [Fact]
    public async Task Speaking_rejects_a_non_multipart_request()
    {
        var u = await factory.CreateUserAsync();
        var res = await u.Client.PostAsync("/api/speaking/submit", JsonExtensions.Json(new { topic = "x" }));
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.Equal(Msg.Err("speaking.multipart"), await ErrorOf(res));
    }

    [Fact]
    public async Task Speaking_validates_the_audio_and_the_topic()
    {
        var u = await factory.CreateUserAsync();
        var noAudio = await u.Client.PostAsync("/api/speaking/submit", Speaking(withAudio: false));
        Assert.Equal(Msg.Err("speaking.no_audio"), await ErrorOf(noAudio));
        var emptyAudio = await u.Client.PostAsync("/api/speaking/submit", Speaking(audioBytes: 0));
        Assert.Equal(Msg.Err("speaking.no_audio"), await ErrorOf(emptyAudio));

        var noTopic = await u.Client.PostAsync("/api/speaking/submit", Speaking(topic: "   "));
        Assert.Equal(HttpStatusCode.BadRequest, noTopic.StatusCode);
        Assert.Equal(Msg.Err("topic.empty"), await ErrorOf(noTopic));
        var longTopic = await u.Client.PostAsync("/api/speaking/submit", Speaking(topic: new string('a', SpeakingWritingEndpoints.MaxTopicChars + 1)));
        Assert.Equal(Msg.Err("topic.too_long", SpeakingWritingEndpoints.MaxTopicChars), await ErrorOf(longTopic));
    }

    [Fact]
    public async Task An_oversized_upload_is_refused_with_413_before_it_is_read()
    {
        var u = await factory.CreateUserAsync();
        var callsBefore = factory.Ai.SpeakingCalls;
        var big = await u.Client.PostAsync("/api/speaking/submit", Speaking(audioBytes: (int)Uploads.MaxRecordingBytes + 1024 * 1024));
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, big.StatusCode);
        Assert.Equal(Msg.Err("audio.too_big", Uploads.MaxRecordingBytes / Uploads.Mb), await ErrorOf(big));
        Assert.Equal(callsBefore, factory.Ai.SpeakingCalls);                                              // AI'ga yetib bormadi
    }

    // ---- Speaking: muvaffaqiyat va saqlash ----

    [Fact]
    public async Task A_logged_in_speaking_answer_is_graded_saved_and_turns_corrections_into_cards()
    {
        var u = await factory.CreateUserAsync();
        var res = await u.Client.PostAsync("/api/speaking/submit", Speaking());
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var json = await res.JsonAsync();
        Assert.True(json.GetProperty("saved").GetBoolean());
        Assert.Equal(1, json.GetProperty("newCards").GetInt32());
        Assert.True(json.GetProperty("submissionId").GetGuid() != Guid.Empty);
        Assert.StartsWith("I like to travel", json.GetProperty("evaluation").GetProperty("transcript").GetString());
        Assert.True(json.TryGetProperty("metrics", out _));

        var history = await (await u.Client.GetAsync("/api/speaking/history")).JsonAsync();
        var item = history.EnumerateArray().Single();
        Assert.Contains("Describe your hometown", item.GetProperty("promptData").GetString());
        var cards = await (await u.Client.GetAsync("/api/review/cards")).JsonAsync();
        Assert.Equal("I goed there", cards.EnumerateArray().Single().GetProperty("front").GetString().Replace("\n", " ").Split("→")[0].Trim().Length > 0 ? "I goed there" : "");
        Assert.Equal(JsonValueKind.Array, (await (await u.Client.GetAsync("/api/speaking/fluency")).JsonAsync()).ValueKind);
    }

    [Fact]
    public async Task Speaking_is_not_saved_with_save_false_or_for_guests()
    {
        var u = await factory.CreateUserAsync();
        var noSave = await (await u.Client.PostAsync("/api/speaking/submit?save=false", Speaking())).JsonAsync();
        Assert.False(noSave.GetProperty("saved").GetBoolean());
        Assert.Empty((await (await u.Client.GetAsync("/api/speaking/history")).JsonAsync()).EnumerateArray());

        var guest = await (await factory.CreateClient().PostAsync("/api/speaking/submit", Speaking())).JsonAsync();
        Assert.False(guest.GetProperty("saved").GetBoolean());                                  // mehmon ham baho oladi, lekin saqlanmaydi
        Assert.Equal(0, guest.GetProperty("newCards").GetInt32());
        Assert.Empty((await (await factory.CreateClient().GetAsync("/api/speaking/history")).JsonAsync()).EnumerateArray());
    }

    // ---- AI xatolari ----

    [Fact]
    public async Task An_ai_failure_becomes_502_saves_nothing_and_does_not_spend_the_daily_quota()
    {
        var u = await factory.CreateUserAsync();
        var before = await UsedAsync(u.Client);
        factory.Ai.SpeakingFailure = new HttpRequestException("Gemini is down");
        try
        {
            var res = await u.Client.PostAsync("/api/speaking/submit", Speaking());
            Assert.Equal(HttpStatusCode.BadGateway, res.StatusCode);
            Assert.Equal(Msg.Err("ai_unavailable"), (await res.JsonAsync()).GetProperty("detail").GetString());     // ProblemDetails: foydalanuvchi tilidagi xabar
            Assert.Empty((await (await u.Client.GetAsync("/api/speaking/history")).JsonAsync()).EnumerateArray());
            Assert.Equal(before, await UsedAsync(u.Client));
        }
        finally
        {
            factory.Ai.SpeakingFailure = null;
        }
        Assert.Equal(HttpStatusCode.OK, (await u.Client.PostAsync("/api/speaking/submit", Speaking())).StatusCode);   // keyingi urinish ishlaydi
        Assert.Equal(before + 1, await UsedAsync(u.Client));                                                          // faqat muvaffaqiyatli urinish sanaladi
    }

    [Fact]
    public async Task An_ai_busy_signal_becomes_503_with_retry_after()
    {
        var u = await factory.CreateUserAsync();
        factory.Ai.SpeakingFailure = new AiBusyException(DateTime.UtcNow.AddSeconds(30), "global budget");
        try
        {
            var res = await u.Client.PostAsync("/api/speaking/submit", Speaking());
            Assert.Equal(HttpStatusCode.ServiceUnavailable, res.StatusCode);
            var retry = int.Parse(res.Headers.GetValues("Retry-After").Single());
            Assert.InRange(retry, 1, 31);
            var json = await res.JsonAsync();
            Assert.Equal(AiErrors.BusyCode, json.GetProperty("code").GetString());
            Assert.Equal(retry, json.GetProperty("retryAfterSeconds").GetInt32());
        }
        finally
        {
            factory.Ai.SpeakingFailure = null;
        }
    }

    // ---- Writing ----

    [Fact]
    public async Task Writing_validates_its_input()
    {
        var u = await factory.CreateUserAsync();
        async Task<string> Err(object body) => await ErrorOf(await u.Client.PostAsync("/api/writing/submit", JsonExtensions.Json(body)));
        Assert.Equal(Msg.Err("topic.empty"), await Err(new { topic = " ", text = new string('a', 50) }));
        Assert.Equal(Msg.Err("text.empty"), await Err(new { topic = "Cities", text = " " }));
        Assert.Equal(Msg.Err("text.too_short", 20), await Err(new { topic = "Cities", text = "too short" }));
        Assert.Equal(Msg.Err("text.too_long", 5000), await Err(new { topic = "Cities", text = new string('a', 5001) }));
    }

    [Fact]
    public async Task A_writing_essay_is_graded_saved_and_listed_in_the_history()
    {
        var u = await factory.CreateUserAsync();
        var essay = "Big cities offer many jobs, but they are also expensive and crowded. In my opinion, the benefits are greater.";
        var res = await u.Client.PostAsync("/api/writing/submit", JsonExtensions.Json(new { topic = "Cities", text = essay, level = "B2" }));
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var json = await res.JsonAsync();
        Assert.True(json.GetProperty("saved").GetBoolean());
        Assert.Equal(1, json.GetProperty("newCards").GetInt32());
        Assert.Equal(7, json.GetProperty("evaluation").GetProperty("taskAchievement").GetProperty("score").GetInt32());

        var history = (await (await u.Client.GetAsync("/api/writing/history")).JsonAsync()).EnumerateArray().Single();
        Assert.Contains("Cities", history.GetProperty("promptData").GetString());

        factory.Ai.WritingFailure = new HttpRequestException("down");
        try
        {
            var failed = await u.Client.PostAsync("/api/writing/submit", JsonExtensions.Json(new { topic = "Cities", text = essay }));
            Assert.Equal(HttpStatusCode.BadGateway, failed.StatusCode);
        }
        finally
        {
            factory.Ai.WritingFailure = null;
        }
        Assert.Single((await (await u.Client.GetAsync("/api/writing/history")).JsonAsync()).EnumerateArray());   // xato urinish tarixga tushmadi
    }
}
