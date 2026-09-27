using System.Text.Json;
using SpeakingCoach.Api.Endpoints;
using SpeakingCoach.Api.Services;
using SpeakingCoach.Api.Services.Mock;
using SpeakingCoach.Api.Services.Telegram;

namespace SpeakingCoach.Api.Tests;

/// <summary>Shadowing: YouTube havolasi, vaqtlar, dars tekshiruvi, talaffuz natijasini moslash, bot oqimi.</summary>
public class ShadowingTests
{
    private static ShadowingLesson Video(params ShadowLine[] lines) =>
        new("x", ShadowingRules.YouTube, "Clip", "B1", "dQw4w9WgXcQ", 60, 240, [], lines.ToList());

    [Theory]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ", "dQw4w9WgXcQ", 0, 180)]
    [InlineData("https://youtu.be/dQw4w9WgXcQ?t=95", "dQw4w9WgXcQ", 95, 275)]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ&t=1m20s", "dQw4w9WgXcQ", 80, 260)]
    [InlineData("youtube.com/shorts/dQw4w9WgXcQ", "dQw4w9WgXcQ", 0, 180)]
    [InlineData("https://m.youtube.com/watch?feature=share&v=dQw4w9WgXcQ 1:20-3:40 B1", "dQw4w9WgXcQ", 80, 220)]
    [InlineData("B2 https://youtu.be/dQw4w9WgXcQ 30 - 150", "dQw4w9WgXcQ", 30, 150)]
    [InlineData("https://youtu.be/a-1_b-2_c-3 1:00:05-1:02:00", "a-1_b-2_c-3", 3605, 3720)]
    public void YouTube_links_and_ranges_are_parsed(string text, string id, double from, double to)
    {
        var clip = ShadowingRules.ParseYouTube(text);
        Assert.NotNull(clip);
        Assert.Equal(id, clip!.VideoId);
        Assert.Equal(from, clip.From);
        Assert.Equal(to, clip.To);
        Assert.Equal($"https://www.youtube.com/watch?v={id}", clip.Url);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Environment")]
    [InlineData("https://vimeo.com/123456")]
    [InlineData("https://youtu.be/short")]
    [InlineData("https://youtu.be/dQw4w9WgXcQ 3:00-1:00")]
    [InlineData("https://youtu.be/dQw4w9WgXcQ 0:00-9:00")]
    [InlineData("https://youtu.be/dQw4w9WgXcQ 10-15")]
    public void Bad_links_and_ranges_are_rejected(string? text)
    {
        Assert.Null(ShadowingRules.ParseYouTube(text));
    }

    [Fact]
    public void Clock_and_level_helpers()
    {
        Assert.Equal("1:05", ShadowingRules.Clock(65.7));
        Assert.Equal("1:00:05", ShadowingRules.Clock(3605));
        Assert.Equal(80, ShadowingRules.ParseTime("1:20"));
        Assert.Null(ShadowingRules.ParseTime("1:x"));
        Assert.Equal("B2", ShadowingRules.LevelIn("b2 travel"));
        Assert.Null(ShadowingRules.LevelIn("Travel"));
    }

    [Fact]
    public void Clip_relative_times_are_shifted_to_video_time()
    {
        var rel = new List<ShadowLine> { new("Hello there.", 1.2, 2.8), new("How are you?", 3.0, 4.5) };
        var shifted = ShadowingRules.NormalizeTimes(rel, 60, 240);
        Assert.Equal(61.2, shifted[0].Start);
        Assert.Equal(64.5, shifted[1].End);

        var abs = new List<ShadowLine> { new("Hello there.", 61.2, 62.8) };
        Assert.Equal(61.2, ShadowingRules.NormalizeTimes(abs, 60, 240)[0].Start);
        // Boshidan boshlangan qism — surilmaydi
        Assert.Equal(1.2, ShadowingRules.NormalizeTimes(rel, 0, 180)[0].Start);
    }

    [Fact]
    public void Video_lessons_are_validated_strictly()
    {
        ShadowingRules.Validate(Video(new("One.", 61, 62), new("Two words.", 63, 65), new("Three is here.", 66, 70)));
        Assert.Throws<InvalidOperationException>(() => ShadowingRules.Validate(Video(new("One.", 61, 62), new("Two.", 63, 65))));
        Assert.Throws<InvalidOperationException>(() => ShadowingRules.Validate(Video(new("One.", 61, 62), new("Two.", 63, 90), new("Three.", 91, 92))));
        Assert.Throws<InvalidOperationException>(() => ShadowingRules.Validate(Video(new("One.", 10, 12), new("Two.", 13, 15), new("Three.", 16, 18))));
        Assert.Throws<InvalidOperationException>(() => ShadowingRules.Validate(Video(new("One.", 70, 72), new("Two.", 61, 62), new("Three.", 73, 75))));
        Assert.Throws<InvalidOperationException>(() => ShadowingRules.Validate(Video(new("One.", 61, 62), new("Two.", null, null), new("Three.", 73, 75))));
        var longLine = string.Join(" ", Enumerable.Repeat("word", 31));
        Assert.Throws<InvalidOperationException>(() => ShadowingRules.Validate(Video(new("One.", 61, 62), new(longLine, 63, 70), new("Three.", 73, 75))));
        Assert.Throws<InvalidOperationException>(() => ShadowingRules.Validate(Video(new("A.", 61, 62), new("B.", 63, 64), new("C.", 65, 66)) with { Level = "B3" }));
    }

    [Fact]
    public void Built_in_character_lessons_are_valid_and_unique()
    {
        Assert.True(ShadowingBank.Lessons.Length >= 6);
        Assert.Equal(ShadowingBank.Lessons.Length, ShadowingBank.Lessons.Select(l => l.Id).Distinct().Count());
        foreach (var l in ShadowingBank.Lessons)
        {
            ShadowingRules.Validate(l);
            Assert.Equal(ShadowingRules.Character, l.Kind);
            Assert.False(Guid.TryParseExact(l.Id, "N", out _));
            Assert.All(l.Lines, x => Assert.True(ShadowingRules.Words(x.Text) <= 18, x.Text));
        }
        Assert.Contains(ShadowingBank.Lessons, l => l.Characters.Length == 1);
        Assert.Equal(ShadowingBank.Lessons[0], ShadowingBank.Find("c-cafe"));
        Assert.Null(ShadowingBank.Find("nope"));
    }

    [Fact]
    public void Character_lessons_need_known_characters_and_speakers()
    {
        var l = ShadowingBank.Lessons[0];
        Assert.Throws<InvalidOperationException>(() => ShadowingRules.Validate(l with { Characters = ["dragon", "cat"] }));
        Assert.Throws<InvalidOperationException>(() => ShadowingRules.Validate(l with { Characters = ["cat"] }));
        Assert.Throws<InvalidOperationException>(() => ShadowingRules.Validate(l with { Characters = ["cat", "cat"] }));
    }

    [Fact]
    public void A_one_speaker_dialogue_keeps_one_character()
    {
        var mono = new ShadowingLesson("n", ShadowingRules.Character, "T", "B1", null, null, null, ["cat", "owl"],
            [new("Hello there, everyone.", Speaker: 1), new("Welcome to our city.", Speaker: 1), new("Let's go.", Speaker: 1)]);
        var t = ShadowingRules.TrimCharacters(mono);
        Assert.Equal(["owl"], t.Characters);
        Assert.All(t.Lines, x => Assert.Equal(0, x.Speaker));
        var duo = ShadowingBank.Lessons[0];
        Assert.Equal(duo, ShadowingRules.TrimCharacters(duo));
    }

    [Fact]
    public void Duration_is_estimated()
    {
        Assert.Equal(180, ShadowingRules.Seconds(Video(new ShadowLine("A.", 61, 62))));
        Assert.True(ShadowingRules.Seconds(ShadowingBank.Lessons[0]) is >= 20 and <= 60);
    }

    // ---------------- Talaffuz natijasi ----------------

    [Fact]
    public void Tokens_drop_punctuation_but_keep_contractions()
    {
        Assert.Equal(["Don't", "stop", "Bob", "it's", "5"], ShadowingRules.Tokens("Don't stop, Bob — it’s 5!").Select(x => x.Replace('’', '\'')).ToList());
    }

    [Fact]
    public void Ai_result_is_aligned_to_the_real_sentence()
    {
        var target = "I'm meeting a friend at ten.";
        var same = new ShadowCheck(140, "im meeting a friend at ten", [new("I'm", true), new("meeting", true), new("a", false), new("friend", true), new("at", true), new("ten", false)], "Say 'ten' clearly.");
        var r = ShadowingRules.Align(target, same);
        Assert.Equal(100, r.Score);
        Assert.Equal(["I'm", "meeting", "a", "friend", "at", "ten"], r.Words.Select(w => w.Word).ToList());
        Assert.Equal([true, true, false, true, true, false], r.Words.Select(w => w.Ok).ToList());

        // Soni mos emas — "to'g'ri" so'zlar to'plami bo'yicha
        var other = new ShadowCheck(55, "meeting friend", [new("meeting", true), new("friend", true), new("at ten", false)], "");
        var r2 = ShadowingRules.Align(target, other);
        Assert.Equal(6, r2.Words.Count);
        Assert.Equal([false, true, false, true, false, false], r2.Words.Select(w => w.Ok).ToList());
        Assert.Equal(55, r2.Score);

        var neg = ShadowingRules.Align(target, same with { Score = -5, Tip = new string('t', 500) });
        Assert.Equal(0, neg.Score);
        Assert.True(neg.Tip.Length <= 300);
    }

    [Fact]
    public void Check_prompt_names_the_sentence_and_tip_language()
    {
        var p = ShadowingRules.CheckPrompt("Here you are.", "uz");
        Assert.Contains("\"Here you are.\"", p);
        Assert.Contains("Uzbek (Latin script)", p);
        Assert.Contains("Russian", ShadowingRules.CheckPrompt("x", "ru"));
        Assert.DoesNotContain("{{", p);
        Assert.Throws<InvalidOperationException>(() => ShadowingRules.ValidateCheck(new ShadowCheck(50, "", [], "")));
    }

    [Fact]
    public void Generation_prompts_carry_the_clip_and_characters()
    {
        var clip = ShadowingRules.ParseYouTube("https://youtu.be/dQw4w9WgXcQ 1:20-3:40")!;
        var p = ShadowingRules.YouTubePrompt(clip, "B1");
        Assert.Contains("from 1:20 to 3:40", p);
        Assert.Contains("\"videoId\": \"dQw4w9WgXcQ\"", p);
        Assert.Contains("\"from\": 80", p);
        Assert.Contains("teacher suggests B1", p);
        var c = ShadowingRules.CharacterPrompt(new AuthorSource(null, null, null, "Travel"), ["cat", "owl"], "A2");
        Assert.Contains("a cat (speaker 0) and a owl (speaker 1)", c);
        Assert.Contains("Topic: Travel", c);
        Assert.Contains("\"level\": \"A2\"", c);
    }

    [Fact]
    public void Stored_lesson_payload_round_trips_and_review_text_shows_times()
    {
        var kind = AuthorKind.Parse("shadowing:youtube:")!;
        var lesson = Video(new("Hello there.", 61.2, 62.8), new("How are you?", 63, 64.5), new("Fine, thanks.", 65, 66));
        var json = JsonSerializer.Serialize(lesson, MockSets.Web);
        Assert.Equal(lesson.Lines, JsonSerializer.Deserialize<ShadowingLesson>(json, MockSets.Web)!.Lines);
        Assert.Equal("Clip", MockAuthoringRules.TitleOf(kind, json));
        var full = MockAuthoringRules.FullText(kind, json);
        Assert.Contains("[1:01.2–1:02.8] Hello there.", full);
        Assert.Contains("watch?v=dQw4w9WgXcQ&t=60s", full);
        Assert.Contains("youtu.be/dQw4w9WgXcQ 1:00–4:00", MockAuthoringRules.Summary(kind, json));

        var ck = AuthorKind.Parse("shadowing:character:")!;
        var cjson = JsonSerializer.Serialize(ShadowingBank.Lessons[0], MockSets.Web);
        Assert.Contains("cat: Good morning!", MockAuthoringRules.FullText(ck, cjson));
        Assert.Contains("A2 · 8 lines · cat + bear", MockAuthoringRules.Summary(ck, cjson));
    }

    [Fact]
    public void Summaries_hide_lines_but_keep_counts()
    {
        var s = ShadowingService.Summarize(ShadowingBank.Lessons[0]);
        Assert.Equal("c-cafe", s.Id);
        Assert.Equal(8, s.Lines);
        Assert.Equal(["cat", "bear"], s.Characters);
    }

    [Fact]
    public void Lesson_id_is_read_from_jsonb_in_any_formatting()
    {
        Assert.Equal("c-cafe", ShadowingEndpoints.LessonIdOf("{\"kind\": \"character\", \"lessonId\": \"c-cafe\"}"));
        Assert.Null(ShadowingEndpoints.LessonIdOf("{}"));
        Assert.Null(ShadowingEndpoints.LessonIdOf("not json"));
    }

    [Fact]
    public void Shadowing_counts_toward_xp()
    {
        Assert.Equal(ProgressCalculator.ShadowingXp, ProgressCalculator.XpFor(new ActivityFact(Data.ActivityType.Shadowing, DateTime.UtcNow, null)));
    }

    // ---------------- Bot ----------------

    [Fact]
    public void Bot_offers_shadowing_and_skips_the_mode_for_youtube()
    {
        var exams = BotAuthoring.ExamButtons().SelectMany(r => r).Select(b => BotLogic.Decode(b.CallbackData)).ToList();
        Assert.Contains(new BotCallback.AuthorExam("shadowing"), exams);
        var kinds = BotAuthoring.KindButtons("shadowing").SelectMany(r => r).ToList();
        Assert.Equal(["🎬 YouTube", "🐾 Characters"], kinds.Select(b => b.Text).ToList());
        Assert.False(BotAuthoring.NeedsMode(AuthorKind.Parse("shadowing:youtube:")!));
        Assert.True(BotAuthoring.NeedsMode(AuthorKind.Parse("shadowing:character:")!));
        Assert.Equal("Shadowing", BotAuthoring.ExamLabel("shadowing"));
        Assert.Contains("youtu.be", BotAuthoring.Instructions("uz", AuthorKind.Parse("shadowing:youtube:")!, false));
        Assert.Contains("qahramon", BotAuthoring.Instructions("uz", AuthorKind.Parse("shadowing:character:")!, true));
    }

    [Fact]
    public void Bot_accepts_only_youtube_links_for_video_lessons()
    {
        var kind = AuthorKind.Parse("shadowing:youtube:")!;
        TgMessage Msg(string? text) => new(1, new TgChat(7, "private"), new TgUser(7, "T", null, "uz"), text);
        var ok = BotAuthoring.ReadInput(Msg("https://youtu.be/dQw4w9WgXcQ 0:30-2:00 B1"), false, kind);
        Assert.Null(ok.Error);
        Assert.Contains("youtu.be", ok.Input!.Text);
        Assert.Equal("bot.author_bad_youtube", BotAuthoring.ReadInput(Msg("just text about travel and more words"), false, kind).Error);
        Assert.Equal("bot.author_bad_youtube", BotAuthoring.ReadInput(Msg(null), false, kind).Error);
        // Oddiy turlar — avvalgidek
        Assert.Equal("bot.author_too_short", BotAuthoring.ReadInput(Msg("short"), false, AuthorKind.Parse("cefr:reading:")).Error);
    }

    [Fact]
    public void Stuck_transcripts_are_cleaned_or_rejected()
    {
        var stuck = new List<ShadowLine>
        {
            new("I mean, I'm a huge believer in dreams with purpose.", 1, 3),
            new("I mean, I'm a huge believer in dreams with purpose.", 3.5, 5),
            new("i mean — I'm a huge believer in dreams with purpose", 5.5, 7),
            new("  ", 7, 8),
            new("I'm all about it.", 8, 9),
        };
        var clean = ShadowingRules.CleanLines(stuck);
        Assert.Equal(2, clean.Count);
        Assert.Equal(1, clean[0].Start);
        Assert.Equal("I'm all about it.", clean[1].Text);

        // Takrorlar ketma-ket bo'lmasa ham ko'p bo'lsa — rad etiladi
        var alternating = Enumerable.Range(0, 6).Select(i => new ShadowLine(i % 2 == 0 ? "Dreams with purpose." : "Something else " + i + ".", 61 + i * 2, 62 + i * 2)).ToArray();
        Assert.Throws<InvalidOperationException>(() => ShadowingRules.Validate(Video(alternating)));
    }

    [Fact]
    public void Translations_and_key_words_are_sanitized()
    {
        var l = ShadowingRules.Extras(new ShadowLine("Apparently, it's a habit.", Tr: new() { ["uz"] = "  Aytishlaricha, bu odat. ", ["ru"] = "", ["de"] = "x" },
            Keys: ["habit", "Apparently", "nonsense", "habit", " "]));
        Assert.Equal("Aytishlaricha, bu odat.", l.Tr!["uz"]);
        Assert.False(l.Tr.ContainsKey("ru"));
        Assert.False(l.Tr.ContainsKey("de"));
        Assert.Equal(["habit", "Apparently"], l.Keys);
        Assert.Null(ShadowingRules.Extras(new ShadowLine("Hi.", Tr: new() { ["de"] = "x" }, Keys: ["bye"])).Tr);
        Assert.Null(ShadowingRules.Extras(new ShadowLine("Hi.", Keys: ["bye"])).Keys);
        Assert.Equal(300, ShadowingRules.Extras(new ShadowLine("Hi.", Tr: new() { ["uz"] = new string('a', 400) })).Tr!["uz"].Length);
    }

    [Fact]
    public void Built_in_lessons_have_uzbek_and_russian_translations_and_real_key_words()
    {
        foreach (var line in ShadowingBank.Lessons.SelectMany(l => l.Lines))
        {
            Assert.True(line.Tr is { } tr && tr.ContainsKey("uz") && tr.ContainsKey("ru"), line.Text);
            Assert.DoesNotMatch(new System.Text.RegularExpressions.Regex("[oOgG]'"), line.Tr!["uz"]);
            Assert.Equal(line, ShadowingRules.Extras(line) with { Tr = line.Tr, Keys = line.Keys });
            Assert.Equal(line.Keys?.Length ?? 0, ShadowingRules.Extras(line).Keys?.Length ?? 0);
        }
    }

    [Fact]
    public void Lessons_are_numbered_per_series_videos_first()
    {
        var video = Video(new ShadowLine("A.", 61, 62)) with { Id = "v1" };
        var list = ShadowingService.Number([.. ShadowingBank.Lessons, video, video with { Id = "v2" }]);
        Assert.Equal(["v1", "v2"], list.Take(2).Select(x => x.Id).ToList());
        Assert.Equal([1, 2], list.Take(2).Select(x => x.Episode).ToList());
        Assert.Equal("c-cafe", list[2].Id);
        Assert.Equal(1, list[2].Episode);
        Assert.Equal(ShadowingBank.Lessons.Length, list.Last().Episode);
    }

    [Fact]
    public void Progress_counts_lessons_and_keeps_the_best_average()
    {
        var p = ShadowingService.ProgressOf([
            ("{\"lessonId\": \"c-cafe\"}", "{\"lines\": 8, \"average\": 70}"),
            ("{\"lessonId\": \"c-cafe\"}", "{\"lines\": 8, \"average\": 85}"),
            ("{\"lessonId\": \"c-city\"}", "{\"lines\": 5, \"average\": null}"),
            ("{}", "{}"),
        ]);
        Assert.Equal(2, p.Count);
        var cafe = p.Single(x => x.LessonId == "c-cafe");
        Assert.Equal(2, cafe.Times);
        Assert.Equal(85, cafe.Best);
        Assert.Null(p.Single(x => x.LessonId == "c-city").Best);
    }
}
