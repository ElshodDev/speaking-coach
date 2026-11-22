using System.Net;
using System.Text.Json;
namespace SpeakingCoach.Api.Tests.Integration;

/// <summary>Lug'at, takrorlash kartalari va Diktant: HTTP orqali (status kodlar, xato matnlari, JSON shakllari).</summary>
public class StudyHttpTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static async Task<string> ErrorOf(HttpResponseMessage r) => (await r.JsonAsync()).GetProperty("error").GetString()!;

    [Fact]
    public async Task Study_endpoints_need_a_login()
    {
        var guest = factory.CreateClient();
        foreach (var path in new[] { "/api/vocab", "/api/review/due", "/api/review/cards", "/api/review/stats" })
            Assert.Equal(HttpStatusCode.Unauthorized, (await guest.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await guest.PostAsync("/api/vocab", JsonExtensions.Json(new { word = "a", translation = "b" }))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await guest.PostAsync("/api/review/cards", JsonExtensions.Json(new { front = "a", back = "b" }))).StatusCode);
    }

    [Fact]
    public async Task A_word_is_added_to_the_vocabulary_and_becomes_a_due_review_card()
    {
        var u = await factory.CreateUserAsync();
        var add = await u.Client.PostAsync("/api/vocab", JsonExtensions.Json(new { word = "  reluctant ", translation = "istamaydigan", partOfSpeech = "adj", example = "He was reluctant to go." }));
        Assert.Equal(HttpStatusCode.OK, add.StatusCode);
        Assert.Equal(1, (await add.JsonAsync()).GetProperty("added").GetInt32());

        var list = await (await u.Client.GetAsync("/api/vocab")).JsonAsync();
        var word = list.GetProperty("words").EnumerateArray().Single();
        Assert.Equal("reluctant", word.GetProperty("word").GetString());
        Assert.Equal("istamaydigan", word.GetProperty("meaning").GetString());
        Assert.Equal("Word", word.GetProperty("kind").GetString());
        Assert.True(word.GetProperty("due").GetBoolean());

        var due = await (await u.Client.GetAsync("/api/review/due?scope=vocab")).JsonAsync();
        Assert.Equal("reluctant", due.EnumerateArray().Single().GetProperty("front").GetString());
        Assert.Equal(1, (await (await u.Client.GetAsync("/api/review/stats")).JsonAsync()).GetProperty("due").GetInt32());
    }

    [Fact]
    public async Task Vocabulary_errors_have_the_right_status_codes()
    {
        var u = await factory.CreateUserAsync();
        var missing = await u.Client.PostAsync("/api/vocab", JsonExtensions.Json(new { word = "  ", translation = "x" }));
        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
        Assert.Equal(Msg.Err("vocab.word_required"), await ErrorOf(missing));

        await u.Client.PostAsync("/api/vocab", JsonExtensions.Json(new { word = "tough", translation = "qiyin" }));
        var dup = await u.Client.PostAsync("/api/vocab", JsonExtensions.Json(new { word = "tough", translation = "qiyin" }));
        Assert.Equal(HttpStatusCode.Conflict, dup.StatusCode);                                   // ikki marta bosish — 409
        Assert.Equal(Msg.Err("vocab.exists"), await ErrorOf(dup));
    }

    [Fact]
    public async Task Manual_cards_are_validated_graded_and_deleted()
    {
        var u = await factory.CreateUserAsync();
        var both = await u.Client.PostAsync("/api/review/cards", JsonExtensions.Json(new { front = "only front", back = " " }));
        Assert.Equal(HttpStatusCode.BadRequest, both.StatusCode);
        Assert.Equal(Msg.Err("review.both_sides"), await ErrorOf(both));

        var ok = await u.Client.PostAsync("/api/review/cards", JsonExtensions.Json(new { front = "break the ice", back = "suhbatni boshlash", note = "idiom" }));
        Assert.Equal(1, (await ok.JsonAsync()).GetProperty("added").GetInt32());
        var dup = await u.Client.PostAsync("/api/review/cards", JsonExtensions.Json(new { front = "break the ice", back = "boshqa" }));
        Assert.Equal(HttpStatusCode.BadRequest, dup.StatusCode);
        Assert.Equal(Msg.Err("review.phrase_exists"), await ErrorOf(dup));

        var cards = await (await u.Client.GetAsync("/api/review/cards")).JsonAsync();
        var card = cards.EnumerateArray().Single();
        Assert.Equal("Manual", card.GetProperty("kind").GetString());
        var id = card.GetProperty("id").GetGuid();

        var badGrade = await u.Client.PostAsync($"/api/review/cards/{id}/grade", JsonExtensions.Json(new { grade = 99 }));
        Assert.Equal(Msg.Err("review.bad_grade"), await ErrorOf(badGrade));
        var graded = await (await u.Client.PostAsync($"/api/review/cards/{id}/grade", JsonExtensions.Json(new { grade = 3 }))).JsonAsync();
        Assert.True(graded.TryGetProperty("dueAtUtc", out _) && graded.TryGetProperty("intervalDays", out _));
        Assert.Equal(HttpStatusCode.NotFound, (await u.Client.PostAsync($"/api/review/cards/{Guid.NewGuid()}/grade", JsonExtensions.Json(new { grade = 3 }))).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await u.Client.DeleteAsync($"/api/review/cards/{id}")).StatusCode);
        Assert.Empty((await (await u.Client.GetAsync("/api/review/cards")).JsonAsync()).EnumerateArray());
    }

    [Fact]
    public async Task Cards_are_private_to_their_owner()
    {
        var owner = await factory.CreateUserAsync();
        var other = await factory.CreateUserAsync();
        await owner.Client.PostAsync("/api/review/cards", JsonExtensions.Json(new { front = "secret", back = "maxfiy" }));
        var id = (await (await owner.Client.GetAsync("/api/review/cards")).JsonAsync()).EnumerateArray().Single().GetProperty("id").GetGuid();

        Assert.Empty((await (await other.Client.GetAsync("/api/review/cards")).JsonAsync()).EnumerateArray());
        Assert.Equal(HttpStatusCode.NotFound, (await other.Client.PostAsync($"/api/review/cards/{id}/grade", JsonExtensions.Json(new { grade = 3 }))).StatusCode);
    }

    // ---- Diktant ----

    [Fact]
    public async Task Dictation_results_are_validated_saved_for_members_only_and_capped_per_day()
    {
        var guest = await (await factory.CreateClient().PostAsync("/api/dictation/result", JsonExtensions.Json(new { level = "B1", sentences = 5, correctWords = 20, totalWords = 25 }))).JsonAsync();
        Assert.False(guest.GetProperty("saved").GetBoolean());                                    // mehmon — saqlanmaydi

        var u = await factory.CreateUserAsync();
        var bad = await u.Client.PostAsync("/api/dictation/result", JsonExtensions.Json(new { level = "B1", sentences = 5, correctWords = 30, totalWords = 25 }));
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
        Assert.Equal(Msg.Err("dictation.bad"), await ErrorOf(bad));

        for (var i = 0; i < XpGuard.DictationsPerDay; i++)
        {
            var saved = await (await u.Client.PostAsync("/api/dictation/result", JsonExtensions.Json(new { level = "B1", sentences = 5, correctWords = 20, totalWords = 25 }))).JsonAsync();
            Assert.True(saved.GetProperty("saved").GetBoolean());
        }
        var capped = await (await u.Client.PostAsync("/api/dictation/result", JsonExtensions.Json(new { level = "B1", sentences = 5, correctWords = 20, totalWords = 25 }))).JsonAsync();
        Assert.False(capped.GetProperty("saved").GetBoolean());
        Assert.True(capped.GetProperty("capped").GetBoolean());                                   // XP yig'ishga qarshi
    }
}
