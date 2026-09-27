using System.Text;
using System.Text.Json;
using SpeakingCoach.Api.Data;
using SpeakingCoach.Api.Services;
using SpeakingCoach.Api.Services.Mock;
using SpeakingCoach.Api.Services.Telegram;

namespace SpeakingCoach.Api.Tests;

/// <summary>Bot orqali test qo'shish: holat, tugmalar, ruxsatlar, material qabul qilish, ko'rib chiqish matni.</summary>
public class BotAuthoringTests
{
    private static TgMessage Msg(string? text = null, TgDocument? doc = null, TgPhotoSize[]? photo = null) =>
        new(1, new TgChat(7, "private"), new TgUser(7, "T", null, "uz"), text, null, doc, photo);

    private static string Json(object o) => JsonSerializer.Serialize(o, o.GetType(), MockSets.Web);

    // ---------------- Turlar va holat ----------------

    [Fact]
    public void All_kinds_have_unique_keys_and_parse_back()
    {
        Assert.Equal(12, AuthorKind.All.Length);
        Assert.Equal(12, AuthorKind.All.Select(k => k.Key).Distinct().Count());
        Assert.Equal("Shadowing (YouTube)", AuthorKind.Parse("shadowing:youtube:")!.Label);
        Assert.Equal("Characters", AuthorKind.Parse("shadowing:character:")!.ModuleLabel);
        foreach (var k in AuthorKind.All) Assert.Equal(k, AuthorKind.Parse(k.Key));
        Assert.Null(AuthorKind.Parse("ielts:speaking:academic"));
        Assert.Null(AuthorKind.Parse(null));
        Assert.Equal("IELTS Reading (General)", AuthorKind.Parse("ielts:reading:general")!.Label);
        Assert.Equal("CEFR Listening", AuthorKind.Parse("cefr:listening:")!.Label);
    }

    [Fact]
    public void Bot_state_round_trips_for_every_kind_and_mode()
    {
        foreach (var k in AuthorKind.All)
        foreach (var gen in new[] { true, false })
        {
            var s = BotAuthoring.State(k, gen);
            Assert.True(s.Length <= 100, s);
            var parsed = BotAuthoring.ParseState(s);
            Assert.NotNull(parsed);
            Assert.Equal(k, parsed!.Value.Kind);
            Assert.Equal(gen, parsed.Value.Generate);
        }
        Assert.Null(BotAuthoring.ParseState(null));
        Assert.Null(BotAuthoring.ParseState("au:ielts:speaking::xyz"));
        Assert.Null(BotAuthoring.ParseState("au:toefl:speaking::gen"));
        Assert.Null(BotAuthoring.ParseState("other"));
    }

    // ---------------- Tugmalar (callback_data) ----------------

    [Fact]
    public void Author_callbacks_round_trip_and_fit_in_64_bytes()
    {
        var id = Guid.NewGuid();
        var all = new List<BotCallback>
        {
            new BotCallback.AuthorExam("ielts"),
            new BotCallback.AuthorExam("cefr"),
            new BotCallback.AuthorCancel(),
        };
        foreach (var k in AuthorKind.All)
        {
            all.Add(new BotCallback.AuthorKindPick(k.Key));
            all.Add(new BotCallback.AuthorModePick(k.Key, true));
            all.Add(new BotCallback.AuthorModePick(k.Key, false));
        }
        foreach (var a in Enum.GetValues<TestAction>()) all.Add(new BotCallback.TestCmd(a, id));

        foreach (var cb in all)
        {
            var data = BotLogic.Encode(cb);
            Assert.True(Encoding.UTF8.GetByteCount(data) <= 64, data);
            Assert.Equal(cb, BotLogic.Decode(data));
        }
    }

    [Theory]
    [InlineData("au:e:toefl")]
    [InlineData("au:k:ielts:speaking:academic")]
    [InlineData("au:m:ielts:reading:academic:x")]
    [InlineData("au:q:0123456789abcdef0123456789abcdef")]
    [InlineData("au:p:not-a-guid")]
    [InlineData("au")]
    public void Broken_author_callbacks_are_ignored(string data)
    {
        Assert.Null(BotLogic.Decode(data));
    }

    [Fact]
    public void Kind_buttons_list_only_the_chosen_exam()
    {
        var ielts = BotAuthoring.KindButtons("ielts").SelectMany(r => r).ToList();
        var cefr = BotAuthoring.KindButtons("cefr").SelectMany(r => r).ToList();
        Assert.Equal(6, ielts.Count);
        Assert.Equal(4, cefr.Count);
        Assert.All(ielts.Concat(cefr), b => Assert.IsType<BotCallback.AuthorKindPick>(BotLogic.Decode(b.CallbackData)));
        Assert.Contains(ielts, b => b.Text.Contains("Reading (Academic)"));
        Assert.All(cefr, b => Assert.DoesNotContain("(", b.Text));
    }

    [Fact]
    public void Draft_buttons_differ_for_admin_and_teacher()
    {
        var id = Guid.NewGuid();
        var admin = BotAuthoring.DraftButtons("uz", id, isAdmin: true).SelectMany(r => r).Select(b => BotLogic.Decode(b.CallbackData)).ToArray();
        var teacher = BotAuthoring.DraftButtons("uz", id, isAdmin: false).SelectMany(r => r).Select(b => BotLogic.Decode(b.CallbackData)).ToArray();
        Assert.Equal(new BotCallback?[] { new BotCallback.TestCmd(TestAction.Publish, id), new BotCallback.TestCmd(TestAction.Delete, id) }, admin);
        Assert.Equal(new BotCallback?[] { new BotCallback.TestCmd(TestAction.Submit, id), new BotCallback.TestCmd(TestAction.Delete, id) }, teacher);
        var review = BotAuthoring.ReviewButtons("ru", id).SelectMany(r => r).Select(b => BotLogic.Decode(b.CallbackData)).ToArray();
        Assert.Equal(new BotCallback?[] { new BotCallback.TestCmd(TestAction.Publish, id), new BotCallback.TestCmd(TestAction.Reject, id) }, review);
    }

    // ---------------- Kim nima qila oladi ----------------

    [Theory]
    // Admin: qoralama yoki tekshiruvdagini chop etadi
    [InlineData(MockTestStatus.Draft, TestAction.Publish, true, true, MockTestStatus.Published)]
    [InlineData(MockTestStatus.Pending, TestAction.Publish, true, false, MockTestStatus.Published)]
    [InlineData(MockTestStatus.Rejected, TestAction.Publish, true, false, null)]
    [InlineData(MockTestStatus.Published, TestAction.Publish, true, false, null)]
    // O'qituvchi o'zi chop eta olmaydi — faqat adminga yuboradi
    [InlineData(MockTestStatus.Draft, TestAction.Publish, false, true, null)]
    [InlineData(MockTestStatus.Draft, TestAction.Submit, false, true, MockTestStatus.Pending)]
    [InlineData(MockTestStatus.Draft, TestAction.Submit, false, false, null)]
    [InlineData(MockTestStatus.Pending, TestAction.Submit, false, true, null)]
    [InlineData(MockTestStatus.Draft, TestAction.Submit, true, true, null)]
    // Rad etish — faqat admin, faqat tekshiruvdagini
    [InlineData(MockTestStatus.Pending, TestAction.Reject, true, false, MockTestStatus.Rejected)]
    [InlineData(MockTestStatus.Pending, TestAction.Reject, false, true, null)]
    [InlineData(MockTestStatus.Draft, TestAction.Reject, true, false, null)]
    // O'chirish: muallif — chop etilmaganini; admin — istalganini; begona — yo'q
    [InlineData(MockTestStatus.Draft, TestAction.Delete, false, true, MockTestStatus.Deleted)]
    [InlineData(MockTestStatus.Rejected, TestAction.Delete, false, true, MockTestStatus.Deleted)]
    [InlineData(MockTestStatus.Published, TestAction.Delete, false, true, null)]
    [InlineData(MockTestStatus.Published, TestAction.Delete, true, false, MockTestStatus.Deleted)]
    [InlineData(MockTestStatus.Draft, TestAction.Delete, false, false, null)]
    [InlineData(MockTestStatus.Deleted, TestAction.Delete, true, true, null)]
    // Ko'rish: muallif yoki admin
    [InlineData(MockTestStatus.Pending, TestAction.View, true, false, MockTestStatus.Pending)]
    [InlineData(MockTestStatus.Draft, TestAction.View, false, true, MockTestStatus.Draft)]
    [InlineData(MockTestStatus.Draft, TestAction.View, false, false, null)]
    [InlineData(MockTestStatus.Deleted, TestAction.View, true, true, null)]
    public void Transitions_follow_the_review_rules(string status, TestAction action, bool isAdmin, bool isOwner, string? expected)
    {
        Assert.Equal(expected, BotAuthoring.Transition(status, action, isAdmin, isOwner));
    }

    // ---------------- Material qabul qilish ----------------

    [Fact]
    public void Topic_mode_accepts_short_text_only()
    {
        var (input, error, _) = BotAuthoring.ReadInput(Msg("  Environment  "), generate: true);
        Assert.Null(error);
        Assert.Equal("Environment", input!.Topic);

        Assert.Equal("bot.author_topic_bad", BotAuthoring.ReadInput(Msg("ab"), true).Error);
        Assert.Equal("bot.author_topic_bad", BotAuthoring.ReadInput(Msg(new string('x', MockAuthoringRules.MaxTopicChars + 1)), true).Error);
        Assert.Equal("bot.author_topic_bad", BotAuthoring.ReadInput(Msg(doc: new TgDocument("f", "a.pdf", "application/pdf", 100)), true).Error);
    }

    [Fact]
    public void Source_mode_accepts_pdf_text_and_the_largest_photo()
    {
        var pdf = BotAuthoring.ReadInput(Msg(doc: new TgDocument("pdf1", "Test 3.PDF", null, 2_000_000)), false);
        Assert.Equal(new AuthorInput(null, "pdf1", "application/pdf", null), pdf.Input);

        var txt = BotAuthoring.ReadInput(Msg(doc: new TgDocument("t1", "script.txt", "text/plain", 3000)), false);
        Assert.Equal("text/plain", txt.Input!.Mime);

        var photo = BotAuthoring.ReadInput(Msg(photo: [new("small", 90, 120, 2000), new("big", 1280, 960, 300_000), new("mid", 320, 240, 20_000)]), false);
        Assert.Equal("big", photo.Input!.FileId);
        Assert.Equal("image/jpeg", photo.Input.Mime);

        var text = new string('a', BotAuthoring.MinSourceChars);
        Assert.Equal(text, BotAuthoring.ReadInput(Msg(text), false).Input!.Text);
    }

    [Fact]
    public void Source_mode_rejects_wrong_types_big_files_and_short_text()
    {
        Assert.Equal("bot.author_bad_file", BotAuthoring.ReadInput(Msg(doc: new TgDocument("a", "talk.mp3", "audio/mpeg", 100)), false).Error);
        Assert.Equal("bot.author_bad_file", BotAuthoring.ReadInput(Msg(doc: new TgDocument("a", "test.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", 100)), false).Error);
        var big = BotAuthoring.ReadInput(Msg(doc: new TgDocument("a", "a.pdf", "application/pdf", MockAuthoringRules.MaxFileBytes + 1)), false);
        Assert.Equal("bot.author_too_big", big.Error);
        Assert.Equal((object)5, big.Args[0]);
        Assert.Equal("bot.author_too_short", BotAuthoring.ReadInput(Msg("Reading test please"), false).Error);
        Assert.Equal("bot.author_bad_file", BotAuthoring.ReadInput(Msg(), false).Error);
    }

    [Theory]
    [InlineData("application/pdf", null, "application/pdf")]
    [InlineData(null, "scan.PDF", "application/pdf")]
    [InlineData("image/jpg", null, "image/jpeg")]
    [InlineData(null, "p.png", "image/png")]
    [InlineData("image/webp", null, "image/webp")]
    [InlineData("text/markdown", null, "text/plain")]
    [InlineData(null, "notes.md", "text/plain")]
    [InlineData("image/gif", "a.gif", null)]
    [InlineData("application/zip", "a.zip", null)]
    public void Mime_types_are_normalized(string? mime, string? name, string? expected)
    {
        Assert.Equal(expected, MockAuthoringRules.NormalizeMime(mime, name));
    }

    [Fact]
    public void Source_parts_send_files_inline_and_text_as_text()
    {
        var pdf = MockAuthoringRules.SourceParts(new AuthorSource(null, [1, 2, 3], "application/pdf", null));
        Assert.Contains("\"mime_type\":\"application/pdf\"", JsonSerializer.Serialize(pdf[0]));
        Assert.Contains("AQID", JsonSerializer.Serialize(pdf[0]));

        var txtFile = MockAuthoringRules.SourceParts(new AuthorSource(null, Encoding.UTF8.GetBytes("Passage one"), "text/plain", null));
        Assert.Contains("SOURCE MATERIAL:\\nPassage one", JsonSerializer.Serialize(txtFile[0]));

        var text = MockAuthoringRules.SourceParts(new AuthorSource(new string('z', MockAuthoringRules.MaxTextChars + 50), null, null, null));
        Assert.Contains(new string('z', 10), JsonSerializer.Serialize(text[0]));
        Assert.True(JsonSerializer.Serialize(text[0]).Length < MockAuthoringRules.MaxTextChars + 100);

        Assert.Empty(MockAuthoringRules.SourceParts(new AuthorSource(null, null, null, "Topic")));
    }

    [Fact]
    public void Prompts_switch_between_topic_and_source_modes()
    {
        var topic = MockAuthoringRules.IeltsSpeakingPrompt(new AuthorSource(null, null, null, "Travel"));
        Assert.Contains("ORIGINAL test on this topic: Travel", topic);
        Assert.DoesNotContain("SOURCE MATERIAL", topic);
        var src = MockAuthoringRules.CefrWritingPrompt(new AuthorSource("text", null, null, null));
        Assert.Contains("SOURCE MATERIAL", src);

        var gt = MockAuthoringRules.IeltsWritingPrompt(IeltsBank.General, new AuthorSource(null, null, null, "Work"));
        Assert.Contains("\"chart\": null", gt);
        Assert.Contains("General Training", gt);
        var ac = MockAuthoringRules.IeltsWritingPrompt(IeltsBank.Academic, new AuthorSource(null, null, null, "Work"));
        Assert.Contains("\"kind\": \"bar\"", ac);
        // Ichki JSON qavslari to'g'ri chiqqan (interpolyatsiya buzilmagan)
        Assert.DoesNotContain("{{", ac);
    }

    // ---------------- Tekshiruvlar ----------------

    [Fact]
    public void Built_in_sets_pass_the_authoring_validators()
    {
        foreach (var s in IeltsBank.Speaking) MockAuthoringRules.ValidateIeltsSpeaking(s);
        foreach (var w in IeltsBank.Writing) MockAuthoringRules.ValidateIeltsWriting(w, w.Variant);
        foreach (var s in CefrBank.Speaking) MockAuthoringRules.ValidateCefrSpeaking(s);
        foreach (var w in CefrBank.Writing) MockAuthoringRules.ValidateCefrWriting(w);
    }

    [Fact]
    public void Validators_reject_incomplete_sets()
    {
        var s = IeltsBank.Speaking[0];
        Assert.Throws<InvalidOperationException>(() => MockAuthoringRules.ValidateIeltsSpeaking(s with { Part1 = s.Part1.Take(2).ToArray() }));
        Assert.Throws<InvalidOperationException>(() => MockAuthoringRules.ValidateIeltsSpeaking(s with { Part2 = s.Part2 with { Points = [] } }));

        var academic = IeltsBank.Writing.First(w => w.Variant == IeltsBank.Academic);
        Assert.Throws<InvalidOperationException>(() => MockAuthoringRules.ValidateIeltsWriting(academic with { Task1 = academic.Task1 with { Chart = null } }, IeltsBank.Academic));
        Assert.Throws<InvalidOperationException>(() => MockAuthoringRules.ValidateIeltsWriting(academic, IeltsBank.General));
        var chart = academic.Task1.Chart!;
        var broken = chart with { Series = [chart.Series[0] with { Values = [1] }] };
        Assert.Throws<InvalidOperationException>(() => MockAuthoringRules.ValidateIeltsWriting(academic with { Task1 = academic.Task1 with { Chart = broken } }, IeltsBank.Academic));

        var general = IeltsBank.Writing.First(w => w.Variant == IeltsBank.General);
        Assert.Throws<InvalidOperationException>(() => MockAuthoringRules.ValidateIeltsWriting(general with { Task1 = general.Task1 with { Chart = chart } }, IeltsBank.General));

        var cs = CefrBank.Speaking[0];
        Assert.Throws<InvalidOperationException>(() => MockAuthoringRules.ValidateCefrSpeaking(cs with { Pictures = cs.Pictures.Take(1).ToArray() }));
        var cw = CefrBank.Writing[0];
        Assert.Throws<InvalidOperationException>(() => MockAuthoringRules.ValidateCefrWriting(cw with { Email = "Hi, too short." }));
        Assert.Throws<InvalidOperationException>(() => MockAuthoringRules.ValidateCefrWriting(cw with { Task2 = "Write an essay." }));
    }

    // ---------------- Ko'rib chiqish matni ----------------

    [Fact]
    public void Full_text_summary_and_title_cover_speaking_and_writing()
    {
        var sp = IeltsBank.Speaking[0];
        var kind = AuthorKind.Parse("ielts:speaking:")!;
        var payload = Json(sp);
        var full = MockAuthoringRules.FullText(kind, payload);
        Assert.StartsWith("IELTS Speaking", full);
        Assert.Contains(sp.Part1[0], full);
        Assert.Contains(sp.Part3[^1], full);
        Assert.Contains(sp.Part2.Topic, MockAuthoringRules.Summary(kind, payload));
        Assert.True(MockAuthoringRules.TitleOf(kind, payload).Length is > 0 and <= 80);

        var w = IeltsBank.Writing.First(x => x.Variant == IeltsBank.Academic);
        var wk = AuthorKind.Parse("ielts:writing:academic")!;
        var wfull = MockAuthoringRules.FullText(wk, Json(w));
        Assert.Contains("TASK 1", wfull);
        Assert.Contains("[Chart:", wfull);
        Assert.Contains("📊", MockAuthoringRules.Summary(wk, Json(w)));

        var cw = CefrBank.Writing[0];
        var ck = AuthorKind.Parse("cefr:writing:")!;
        Assert.Contains(cw.EmailFrom, MockAuthoringRules.FullText(ck, Json(cw)));

        var cs = CefrBank.Speaking[0];
        var csk = AuthorKind.Parse("cefr:speaking:")!;
        var csFull = MockAuthoringRules.FullText(csk, Json(cs));
        Assert.Contains("AGAINST:", csFull);
        Assert.Contains(cs.Part3Statement, MockAuthoringRules.Summary(csk, Json(cs)));
    }

    [Fact]
    public void Full_text_of_objective_tests_shows_answers()
    {
        var q = new ObjQuestion(1, "Meet at the ___", null, ["main gate"], "Speaker says so");
        var mcq = new ObjQuestion(2, "Why?", ["cost", "time", "place"], ["B"], "time is mentioned");
        var reading = new ReadingTest("academic", [new ReadingPassage("Bridges", "Some text here.", [new QuestionGroup("gap", "ONE WORD", 1, [q]), new QuestionGroup("mcq", "Choose", null, [mcq])])]);
        var rk = AuthorKind.Parse("ielts:reading:academic")!;
        var full = MockAuthoringRules.FullText(rk, Json(reading));
        Assert.Contains("PASSAGE 1: Bridges", full);
        Assert.Contains("✔ main gate — Speaker says so", full);
        Assert.Contains("B) time", full);
        Assert.Equal("Bridges", MockAuthoringRules.TitleOf(rk, Json(reading)));
        Assert.Contains("2 q.", MockAuthoringRules.Summary(rk, Json(reading)));

        var listening = new ListeningTest([new ListeningPart(1, "A call to a hotel", [new ScriptLine("Anna", "female", "Hello.")], [new QuestionGroup("gap", "ONE WORD", 1, [q])])]);
        var lk = AuthorKind.Parse("cefr:listening:")!;
        var lfull = MockAuthoringRules.FullText(lk, Json(listening));
        Assert.Contains("Anna (female): Hello.", lfull);
        Assert.Equal("A call to a hotel", MockAuthoringRules.TitleOf(lk, Json(listening)));
    }

    [Fact]
    public void Summary_is_capped_for_telegram()
    {
        var passages = Enumerable.Range(1, 200).Select(i => new ReadingPassage($"Passage number {i} with a long title", "x", [])).ToList();
        var s = MockAuthoringRules.Summary(AuthorKind.Parse("cefr:reading:")!, Json(new ReadingTest("", passages)));
        Assert.True(s.Length <= 1501);
        Assert.True(s.EndsWith('…'));
    }

    [Fact]
    public void Bot_messages_escape_user_content()
    {
        var kind = AuthorKind.Parse("ielts:speaking:")!;
        var html = BotAuthoring.DraftMessage("uz", kind, "<b>x</b> & y", "1 < 2", isAdmin: false);
        Assert.Contains("&lt;b&gt;x&lt;/b&gt; &amp; y", html);
        Assert.Contains("1 &lt; 2", html);
        Assert.Contains("Adminga yuborish", html);
        Assert.Contains("Chop etish", BotAuthoring.DraftMessage("uz", kind, "t", "s", isAdmin: true));

        var review = BotAuthoring.ReviewMessage("en", "a<b@x.uz", kind, "T", "S");
        Assert.Contains("a&lt;b@x.uz", review);
    }

    [Fact]
    public void Listening_instructions_ask_for_a_script()
    {
        var l = AuthorKind.Parse("ielts:listening:")!;
        Assert.Contains("skript", BotAuthoring.Instructions("uz", l, generate: false));
        Assert.DoesNotContain("skript", BotAuthoring.Instructions("uz", AuthorKind.Parse("cefr:reading:")!, generate: false));
        Assert.Contains("Mavzuni", BotAuthoring.Instructions("uz", l, generate: true));
    }

    [Fact]
    public void Bank_texts_count_by_kind_and_list_own_tests()
    {
        var counts = new Dictionary<string, (int, int)> { ["ielts:reading:academic"] = (4, 1), ["cefr:writing:"] = (2, 0) };
        var bank = BotAuthoring.BankText("uz", counts);
        Assert.Contains("IELTS Reading (Academic): <b>4</b> (1)", bank);
        Assert.Contains("CEFR Writing: <b>2</b>", bank);
        Assert.Contains("CEFR Listening: <b>0</b>", bank);
        Assert.Contains("Tekshiruvni kutmoqda: 1", bank);

        Assert.Contains("/add", BotAuthoring.MineText("uz", []));
        var mine = BotAuthoring.MineText("ru", [(AuthorKind.Parse("cefr:speaking:")!, "Holidays", MockTestStatus.Pending)]);
        Assert.Contains("Holidays — <i>на проверке</i>", mine);
    }

    [Fact]
    public void File_names_are_safe_and_short()
    {
        var id = Guid.Parse("3f2a9c1b-0000-0000-0000-000000000000");
        Assert.Equal("ielts-reading-academic-3f2a9c1b.txt", BotAuthoring.FileName(AuthorKind.Parse("ielts:reading:academic")!, id));
        Assert.Equal("cefr-speaking-3f2a9c1b.txt", BotAuthoring.FileName(AuthorKind.Parse("cefr:speaking:")!, id));
    }

    [Fact]
    public void Kind_of_a_stored_test_is_recovered()
    {
        Assert.Equal(AuthorKind.Parse("ielts:listening:"), BotAuthoring.KindOf(new MockTest { Exam = "ielts", Module = "listening", Variant = "" }));
        Assert.Null(BotAuthoring.KindOf(new MockTest { Exam = "ielts", Module = "listening", Variant = "academic" }));
    }

    [Fact]
    public void Status_labels_exist_in_all_languages()
    {
        foreach (var lang in Texts.Langs)
        foreach (var st in new[] { MockTestStatus.Published, MockTestStatus.Draft, MockTestStatus.Pending, MockTestStatus.Rejected })
            Assert.DoesNotContain("bot.status_", BotAuthoring.StatusLabel(lang, st));
    }

    [Fact]
    public void Stored_set_ids_are_32_hex_guids_and_never_clash_with_built_in_ids()
    {
        var id = Guid.NewGuid();
        Assert.Matches("^[0-9a-f]{32}$", MockSets.IdOf(id));
        var builtIn = IeltsBank.Speaking.Select(s => s.Id).Concat(IeltsBank.Writing.Select(w => w.Id))
            .Concat(CefrBank.Speaking.Select(s => s.Id)).Concat(CefrBank.Writing.Select(w => w.Id));
        Assert.All(builtIn, b => Assert.False(Guid.TryParseExact(b, "N", out _)));
    }

    [Fact]
    public void Failure_reason_is_short_and_never_leaks_the_api_key()
    {
        var gemini = new InvalidOperationException("Gemini API xatosi (TooManyRequests): {\"error\": {\"code\": 429, \"message\": \"You exceeded your current quota, please check your plan.\", \"status\": \"RESOURCE_EXHAUSTED\"}}");
        Assert.Equal("Gemini API xatosi (TooManyRequests): You exceeded your current quota, please check your plan.", MockAuthoringRules.FailureReason(gemini));

        var leak = new HttpRequestException("Failed https://generativelanguage.googleapis.com/v1beta/models/x:generateContent?key=AIzaSECRET123 timeout key=AIzaOTHER");
        var r = MockAuthoringRules.FailureReason(leak);
        Assert.DoesNotContain("SECRET", r);
        Assert.DoesNotContain("OTHER", r);
        Assert.DoesNotContain("googleapis", r);

        Assert.Equal("timeout", MockAuthoringRules.FailureReason(new TaskCanceledException("The request was canceled")));
        Assert.Equal("Matn uzunligi mos emas: 420 so'z", MockAuthoringRules.FailureReason(new AggregateException(new InvalidOperationException("Matn uzunligi mos emas: 420 so'z"))));
        Assert.True(MockAuthoringRules.FailureReason(new Exception(new string('x', 1000))).Length <= 300);
    }

    [Fact]
    public void Teacher_passages_may_differ_in_length_from_generated_ones()
    {
        var text = string.Join(" ", Enumerable.Repeat("word", 400)) + " main gate";
        var p = new ReadingPassage("T", text, [new QuestionGroup("gap", "ONE WORD", 2, [new ObjQuestion(1, "Meet at the ___", null, ["main gate"], "x")])]);
        Assert.Throws<InvalidOperationException>(() => GeminiMockGenerator.ValidatePassage(p, 1, 1));
        GeminiMockGenerator.ValidatePassage(p, 1, 1, 250, 1500);
    }
}
