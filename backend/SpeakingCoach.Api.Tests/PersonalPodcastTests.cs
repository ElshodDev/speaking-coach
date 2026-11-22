using Microsoft.Extensions.Configuration;
using SpeakingCoach.Application.Telegram;

namespace SpeakingCoach.Api.Tests;

public class PersonalPodcastTests
{
    private static IConfiguration Cfg(params (string K, string V)[] kv) =>
        new ConfigurationBuilder().AddInMemoryCollection(kv.ToDictionary(x => x.K, x => (string?)x.V)).Build();

    private static PersonalShadowingService Service(params (string K, string V)[] kv)
    {
        var cfg = Cfg(kv);
        // Baza bu testlarda ishlatilmaydi (faqat kunlik limit mantiqi).
        return new PersonalShadowingService(null!, new AiDailyCaps(), new AiQuotaOptions(cfg), new AdminOptions(cfg));
    }

    private static User U(string email = "a@x.uz") => new() { Id = Guid.NewGuid(), Email = email, EmailVerifiedAtUtc = DateTime.UtcNow };

    // ---- Holat ----

    [Fact]
    public void Personal_status_fits_the_status_column_and_is_distinct()
    {
        Assert.True(MockTestStatus.Personal.Length <= 16);                    // MockTests.Status — 16 belgi (migratsiya kerak emas)
        var all = new[] { MockTestStatus.Published, MockTestStatus.Draft, MockTestStatus.Pending, MockTestStatus.Rejected, MockTestStatus.Deleted, MockTestStatus.Personal };
        Assert.Equal(all.Length, all.Distinct().Count());
    }

    [Fact]
    public void Personal_kind_is_the_youtube_shadowing_kind()
    {
        Assert.True(PersonalShadowingService.Kind.IsShadowing);
        Assert.Equal(ShadowingRules.YouTube, PersonalShadowingService.Kind.Module);
        Assert.False(BotAuthoring.NeedsMode(PersonalShadowingService.Kind));  // havola yuboriladi, rejim tanlanmaydi
    }

    // ---- Limitlar ----

    [Fact]
    public void Daily_limit_defaults_to_three_and_is_per_user()
    {
        var s = Service();
        var a = U();
        var b = U("b@x.uz");
        Assert.Equal(3, s.PerDay);
        Assert.True(s.TryTakeAttempt(a));
        Assert.True(s.TryTakeAttempt(a));
        Assert.True(s.TryTakeAttempt(a));
        Assert.False(s.TryTakeAttempt(a));
        Assert.True(s.TryTakeAttempt(b));                                      // boshqa foydalanuvchiga ta'sir qilmaydi
    }

    [Fact]
    public void A_failed_attempt_is_refunded()
    {
        var s = Service(("Ai:PodcastsPerDay", "1"));
        var a = U();
        Assert.True(s.TryTakeAttempt(a));
        Assert.False(s.TryTakeAttempt(a));
        s.RefundAttempt(a);
        Assert.True(s.TryTakeAttempt(a));
    }

    [Fact]
    public void Limit_is_configurable_and_admins_are_unlimited()
    {
        var s = Service(("Ai:PodcastsPerDay", "2"), ("Admin:Emails", "boss@x.uz"));
        Assert.Equal(2, s.PerDay);
        var admin = U("boss@x.uz");
        for (var i = 0; i < 20; i++) Assert.True(s.TryTakeAttempt(admin));
        s.RefundAttempt(admin);                                                // admin uchun qaytarish hech narsani buzmaydi
        var user = U();
        Assert.True(s.TryTakeAttempt(user));
        Assert.True(s.TryTakeAttempt(user));
        Assert.False(s.TryTakeAttempt(user));
    }

    [Fact]
    public void Stored_lessons_are_capped()
    {
        Assert.InRange(PersonalShadowingService.MaxStored, 10, 100);
    }

    [Fact]
    public void Podcast_flag_is_on_by_default_and_can_be_turned_off()
    {
        Assert.True(new FeatureFlags(Cfg()).PersonalPodcasts);
        Assert.True(new FeatureFlags(Cfg(("Features:PersonalPodcasts", "true"))).PersonalPodcasts);
        Assert.False(new FeatureFlags(Cfg(("Features:PersonalPodcasts", "false"))).PersonalPodcasts);
    }

    // ---- Bot ----

    [Fact]
    public void Flow_states_cover_podcast_and_tutor_but_not_authoring()
    {
        Assert.True(BotPodcast.IsFlowState(BotPodcast.AwaitLink));
        Assert.True(BotPodcast.IsFlowState(BotTutor.State(TutorAwait.Topic)));
        Assert.True(BotPodcast.IsFlowState(BotTutor.State(TutorAwait.Write, Guid.NewGuid())));
        Assert.False(BotPodcast.IsFlowState(null));
        Assert.False(BotPodcast.IsFlowState(BotAuthoring.State(AuthorKind.All[0], true)));
        Assert.False(BotPodcast.IsFlowState("pc:other"));
        Assert.True(BotPodcast.AwaitLink.Length <= 100);
        // Holatlar bir-biriga aralashmaydi.
        Assert.Null(BotTutor.ParseState(BotPodcast.AwaitLink));
        Assert.Null(BotAuthoring.ParseState(BotPodcast.AwaitLink));
        Assert.False(BotPodcast.IsAwaitingLink(BotTutor.State(TutorAwait.Topic)));
    }

    [Fact]
    public void Delete_callback_round_trips_within_64_bytes_and_rejects_garbage()
    {
        var cb = new BotCallback.PodcastDelete(Guid.NewGuid());
        var data = BotLogic.Encode(cb);
        Assert.True(System.Text.Encoding.UTF8.GetByteCount(data) <= 64);
        Assert.Equal(cb, BotLogic.Decode(data));
        foreach (var bad in new[] { "pc:d:", "pc:d:xyz", "pc:x:00000000000000000000000000000000", "pc:d:0000000000000000000000000000000" })
            Assert.Null(BotLogic.Decode(bad));
    }

    [Fact]
    public void Practice_route_matches_the_shadowing_lesson_page_id()
    {
        var id = Guid.NewGuid();
        Assert.Equal($"shadowing/{id:N}", BotPodcast.PracticeRoute(id));
        Assert.Equal(MockSets.IdOf(id), id.ToString("N"));
    }

    [Fact]
    public void Describe_reads_title_lines_and_duration_from_the_lesson()
    {
        var lessons = new ShadowingLesson("x", ShadowingRules.YouTube, "Morning routines", "B1", "dQw4w9WgXcQ", 80, 140, [],
            [new ShadowLine("I wake up at six.", 80, 83), new ShadowLine("Then I go for a run.", 84, 88), new ShadowLine("It keeps me fit.", 89, 92)]);
        var payload = System.Text.Json.JsonSerializer.Serialize(lessons, MockSets.Web);
        var d = BotPodcast.Describe(null, payload)!.Value;
        Assert.Equal(lessons.Title, d.Title);
        Assert.Equal(lessons.Lines.Count, d.Lines);
        Assert.Equal(ShadowingRules.Seconds(lessons), d.Seconds);
        Assert.Equal("My title", BotPodcast.Describe("My title", payload)!.Value.Title);
        Assert.Null(BotPodcast.Describe("x", "not json"));
    }

    [Fact]
    public void Podcast_texts_exist_in_all_languages_and_escape_the_title()
    {
        foreach (var key in new[] { "pc.intro", "pc.mine_head", "pc.working", "pc.busy", "pc.ready", "pc.practice_btn", "pc.delete_btn", "pc.deleted", "pc.not_found", "pc.limit", "pc.full", "pc.disabled", "pc.cmd" })
            foreach (var lang in Texts.Langs) Assert.False(string.IsNullOrWhiteSpace(Texts.Get(lang, key, 3, 30, 5)), $"{key}/{lang}");
        foreach (var lang in Texts.Langs) Assert.InRange(Texts.Get(lang, "pc.cmd").Length, 3, 256);
        Assert.Contains("&lt;b&gt;", BotPodcast.ReadyText("uz", "<b>x", 10, 90));
        Assert.Contains("10", BotPodcast.ReadyText("en", "T", 10, 90));
        Assert.Contains("3", BotPodcast.IntroText("uz", 3, 30, false));
        Assert.Contains(Texts.Get("uz", "pc.mine_head"), BotPodcast.IntroText("uz", 3, 30, true));
        Assert.DoesNotContain(Texts.Get("uz", "pc.mine_head"), BotPodcast.IntroText("uz", 3, 30, false));
    }

    [Fact]
    public void Help_mentions_the_podcast_command_in_every_language()
    {
        foreach (var lang in Texts.Langs) Assert.Contains("/podcast", Texts.Get(lang, "bot.help"));
    }
}
