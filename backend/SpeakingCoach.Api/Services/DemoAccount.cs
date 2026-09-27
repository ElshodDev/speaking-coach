using System.Security.Cryptography;
using System.Text.Json;
using SpeakingCoach.Api.Data;
using SpeakingCoach.Api.Services.Mock;

namespace SpeakingCoach.Api.Services;

/// <summary>
/// Demo hisob: ro'yxatdan o'tmasdan, bir bosishda — tayyor tarix, kartalar,
/// seriya va XP bilan (portfolio'ni ko'ruvchi yoki shunchaki qiziquvchi uchun).
/// Har bir bosish — alohida vaqtinchalik hisob (boshqalar bilan aralashmaydi),
/// 24 soatdan keyin butunlay o'chiriladi. Email — ".invalid" domenida (RFC 2606):
/// hech qachon haqiqiy manzil bo'lolmaydi, xat yuborilmaydi, parol yo'q.
/// </summary>
public static class DemoAccount
{
    public const string Domain = "demo.speakingcoach.invalid";
    public static readonly TimeSpan Lifetime = TimeSpan.FromHours(24);

    public static bool IsDemo(string? email) =>
        email is not null && email.EndsWith("@" + Domain, StringComparison.OrdinalIgnoreCase);

    public static string NewEmail() =>
        $"demo-{Convert.ToHexString(RandomNumberGenerator.GetBytes(5)).ToLowerInvariant()}@{Domain}";

    public static bool Enabled(IConfiguration config) =>
        !string.Equals(config["Demo:Enabled"], "false", StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Demo hisoblar sonini cheklaydi (xotirada): bitta IP'dan kuniga 5 ta,
/// butun serverga kuniga 300 ta — bazani demo hisoblar bilan to'ldirib bo'lmasin.
/// </summary>
public sealed class DemoGate(TimeProvider? clock = null)
{
    public const int PerIpPerDay = 5;
    public const int PerDay = 300;

    private readonly TimeProvider _clock = clock ?? TimeProvider.System;
    private readonly object _lock = new();
    private readonly Dictionary<string, int> _byIp = new();
    private DateOnly _day;
    private int _total;

    public bool TryTake(string ip)
    {
        var today = DateOnly.FromDateTime(_clock.GetUtcNow().UtcDateTime);
        lock (_lock)
        {
            if (today != _day)
            {
                _day = today;
                _total = 0;
                _byIp.Clear();
            }
            var mine = _byIp.GetValueOrDefault(ip);
            if (mine >= PerIpPerDay || _total >= PerDay) return false;
            _byIp[ip] = mine + 1;
            _total++;
            return true;
        }
    }
}

public record DemoData(List<Activity> Activities, List<ReviewCard> Cards, List<ReviewLog> Logs);

/// <summary>
/// Demo hisobning boshlang'ich ma'lumotlari — sof funksiya (bazaga tegmaydi,
/// testlanadi). Natijalar haqiqiy endpoint'lar saqlaydigan shaklda: xuddi
/// shu record'lar va xuddi shu karta yasovchi (ReviewCardFactory), shuning
/// uchun tarix, kartalar, XP va seriya saytda odatdagidek ko'rinadi.
/// Oxirgi 6 kunda har kuni nimadir qilingan (seriya 6 kun), bugun esa 3 ta
/// karta takrorlangan — "Bugungi reja" qisman bajarilgan holda ochiladi.
/// </summary>
public static class DemoSeed
{
    public const int StreakDays = 6;
    public const int ReviewedToday = 3;

    public static DemoData Build(Guid userId, DateTime nowUtc, int tzOffsetMinutes)
    {
        var activities = new List<Activity>();
        var cards = new List<ReviewCard>();
        var logs = new List<ReviewLog>();
        var today = ReviewScheduler.ToLocalDate(nowUtc, tzOffsetMinutes);

        // Mahalliy kun, soat 19:00 → UTC (getTimezoneOffset: UTC − mahalliy).
        DateTime At(int daysAgo, int hour = 19) =>
            DateTime.SpecifyKind(today.AddDays(-daysAgo).ToDateTime(new TimeOnly(hour, 0)).AddMinutes(tzOffsetMinutes), DateTimeKind.Utc);

        void Add(ActivityType type, int daysAgo, object prompt, object response)
        {
            activities.Add(new Activity
            {
                Id = Guid.NewGuid(),
                Type = type,
                UserId = userId,
                CreatedAtUtc = At(daysAgo),
                PromptData = prompt as string ?? JsonSerializer.Serialize(prompt),
                ResponseData = JsonSerializer.Serialize(response),
            });
        }

        void Cards(IEnumerable<NewCard> newCards, ActivityType? source, int daysAgo)
        {
            foreach (var c in newCards)
            {
                cards.Add(new ReviewCard
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Kind = c.Kind,
                    Source = source,
                    Front = c.Front,
                    Back = c.Back,
                    Note = c.Note,
                    CreatedAtUtc = At(daysAgo, 19).AddMinutes(1),
                    DueAtUtc = At(daysAgo, 19).AddMinutes(1),
                    Ease = ReviewDefaults.StartingEase,
                });
            }
        }

        // ---- Speaking: uchta urinish, ball asta-sekin o'sadi ----
        var speaking = new[]
        {
            (Days: 5, Topic: "Describe your favorite city and why you like it.",
                Transcript: "My favorite city is Samarkand. I like it because it have many old buildings and the people are very friendly. I am living there since 2015, and every evening I walk in Registan square. It is very much beautiful at night.",
                Scores: (62, 58, 60),
                Fixes: new[]
                {
                    new CorrectionItem("it have many old buildings", "it has many old buildings", "\"It\" is third person singular, so the verb is \"has\"."),
                    new CorrectionItem("I am living there since 2015", "I have lived there since 2015", "With \"since\" + a starting point, use the present perfect."),
                    new CorrectionItem("It is very much beautiful", "It is very beautiful", "\"Very much\" goes with verbs; adjectives take \"very\"."),
                }),
            (Days: 3, Topic: "Talk about a skill you would like to learn.",
                Transcript: "I want learn programming, because it is more easy to find a good job. I already watched some videos, but I need a teacher who can explain me the difficult parts.",
                Scores: (68, 64, 66),
                Fixes: new[]
                {
                    new CorrectionItem("I want learn programming", "I want to learn programming", "\"Want\" is followed by \"to\" + infinitive."),
                    new CorrectionItem("it is more easy", "it is easier", "Short adjectives form the comparative with -er."),
                    new CorrectionItem("explain me the difficult parts", "explain the difficult parts to me", "\"Explain\" does not take an indirect object without \"to\"."),
                }),
            (Days: 1, Topic: "Describe your ideal job.",
                Transcript: "My ideal job is a data analyst. I would like to work in a big international company, where I can make a research and use English every day with my colleagues.",
                Scores: (72, 70, 69),
                Fixes: new[]
                {
                    new CorrectionItem("make a research", "do research", "\"Research\" is uncountable and collocates with \"do\"."),
                    new CorrectionItem("work in a big international company", "work for a big international company", "We usually work *for* an employer."),
                }),
        };
        foreach (var s in speaking)
        {
            var result = new SpeakingEvaluationResult(
                s.Transcript,
                new ScoreWithReasoning(s.Scores.Item1, "You speak with few long pauses, but some sentences restart in the middle."),
                new ScoreWithReasoning(s.Scores.Item2, "Mostly accurate simple structures; verb forms after \"since\" and \"want\" need work."),
                new ScoreWithReasoning(s.Scores.Item3, "Good everyday vocabulary; try more precise words instead of \"very\"."),
                s.Fixes.ToList(),
                "Clear answer with a personal example — nice!",
                "Use the present perfect with \"since\" and \"for\".");
            Add(ActivityType.Speaking, s.Days, new { topic = s.Topic }, result);
            Cards(ReviewCardFactory.FromCorrections(s.Fixes), ActivityType.Speaking, s.Days);
        }

        // ---- Writing: ikkita insho ----
        var writing = new[]
        {
            (Days: 4, Topic: "Some people prefer online courses, others classroom lessons. Discuss both views and give your opinion.",
                Text: "Nowadays many people is learning online. It is cheap and you can study at any time. In the other hand, students can not ask questions directly to teacher, and it is hard to stay motivated. In my opinion, the best way is to combine both: online lessons for theory and classroom lessons for speaking practice.",
                Scores: (60, 62, 55, 63),
                Fixes: new[]
                {
                    new CorrectionItem("many people is learning online", "many people are learning online", "\"People\" is plural, so use \"are\"."),
                    new CorrectionItem("In the other hand", "On the other hand", "The fixed phrase is \"on the other hand\"."),
                    new CorrectionItem("ask questions directly to teacher", "ask the teacher questions directly", "Use the article and the natural word order: ask + person + questions."),
                }),
            (Days: 2, Topic: "Should cities ban cars from their centres? Give reasons for your answer.",
                Text: "Banning cars from city centres has clear advantages. First, the air becomes cleaner, which is crucial for children and older people. Second, streets become safer and more pleasant for walking. However, shop owners worry that fewer customers will come. Overall, I believe the benefits outweigh the drawbacks if public transport is cheap and reliable.",
                Scores: (68, 70, 66, 67),
                Fixes: new[]
                {
                    new CorrectionItem("fewer customers will come", "fewer customers will visit", "\"Visit\" is more precise in formal writing."),
                    new CorrectionItem("has clear advantages", "has several clear advantages", "A quantifier makes the claim more measured."),
                }),
        };
        foreach (var w in writing)
        {
            var result = new WritingEvaluationResult(
                new ScoreWithReasoning(w.Scores.Item1, "You answer the question and give an opinion; develop each idea with an example."),
                new ScoreWithReasoning(w.Scores.Item2, "Clear paragraphing and linking words."),
                new ScoreWithReasoning(w.Scores.Item3, "Some agreement and article mistakes."),
                new ScoreWithReasoning(w.Scores.Item4, "Appropriate topic vocabulary."),
                w.Fixes.ToList(),
                "Your position is easy to follow.",
                "Add one concrete example to every body paragraph.");
            Add(ActivityType.Writing, w.Days, new { topic = w.Topic, text = w.Text }, result);
            Cards(ReviewCardFactory.FromCorrections(w.Fixes), ActivityType.Writing, w.Days);
        }

        // ---- Reading va Listening: server tekshiradigan savollar ----
        var reading = new ComprehensionExercise(
            "The four-day week",
            "Several companies in Europe have tried a four-day working week without cutting salaries. In most trials, productivity stayed the same or even rose, while staff reported less stress and fewer sick days. Critics argue that the model suits office work better than hospitals or factories, where someone has to be present every day. Supporters reply that shifts can be reorganised, and that rested workers make fewer costly mistakes.",
            [
                new("What happened to productivity in most trials?", ["It fell sharply", "It stayed the same or rose", "It was not measured", "It depended on salaries"], 1, "The text says productivity \"stayed the same or even rose\"."),
                new("What did staff report?", ["Longer hours", "More sick days", "Less stress", "Lower salaries"], 2, "Staff reported less stress and fewer sick days."),
                new("Why do critics doubt the model?", ["It is too expensive", "Some jobs need people every day", "Workers dislike it", "It is illegal"], 1, "Hospitals and factories need someone present every day."),
                new("What do supporters say about mistakes?", ["Rested workers make fewer of them", "They are unavoidable", "They increase", "They are not important"], 0, "\"Rested workers make fewer costly mistakes.\""),
            ]);
        var listening = new ComprehensionExercise(
            "Booking a hotel room",
            "Receptionist: Good afternoon, Riverside Hotel. Guest: Hi, I'd like to book a double room for two nights, from the twelfth of May. Receptionist: Certainly. With breakfast it's ninety-five pounds a night. Guest: Is parking included? Receptionist: Parking is ten pounds extra per day, but the airport shuttle is free.",
            [
                new("How many nights does the guest want?", ["One", "Two", "Three", "Four"], 1, "\"For two nights\"."),
                new("When does the stay start?", ["2 May", "12 May", "20 May", "12 March"], 1, "\"From the twelfth of May\"."),
                new("How much is parking?", ["Free", "£5 a day", "£10 a day", "£95 a night"], 2, "\"Ten pounds extra per day\"."),
                new("What is free?", ["Breakfast", "Parking", "The airport shuttle", "A late checkout"], 2, "\"The airport shuttle is free\"."),
            ]);
        foreach (var (type, exercise, answers, days) in new[]
                 {
                     (ActivityType.Reading, reading, new[] { 1, 2, 0, 0 }, 6),
                     (ActivityType.Listening, listening, new[] { 1, 1, 0, 2 }, 2),
                 })
        {
            var result = GeminiComprehensionService.Grade(exercise, answers);
            Add(type, days, JsonSerializer.Serialize(exercise), result);
            Cards(ReviewCardFactory.FromWrongAnswers(exercise, result), type, days);
        }

        // ---- Shadowing: bitta tugallangan dars ----
        var lesson = ShadowingBank.Lessons[0];
        Add(ActivityType.Shadowing, 1,
            new { lessonId = lesson.Id, title = lesson.Title, level = lesson.Level, kind = lesson.Kind },
            new { lines = lesson.Lines.Count, total = lesson.Lines.Count, average = 84 });

        // ---- Lug'at: foydalanuvchi saqlagan so'zlar ----
        var words = new[]
        {
            ("resilient", "chidamli, bardoshli", "adjective", "able to recover quickly from difficulties", "Children are often more resilient than adults."),
            ("commute", "ishga qatnash (yoʻl)", "noun", "the regular journey between home and work", "My commute takes forty minutes by metro."),
            ("reluctant", "istamaydigan, ikkilanuvchi", "adjective", "not willing to do something", "He was reluctant to speak in front of the class."),
            ("thrive", "gullab-yashnamoq, rivojlanmoq", "verb", "to grow or develop successfully", "Small businesses thrive in this part of the city."),
            ("crucial", "hal qiluvchi, juda muhim", "adjective", "extremely important", "Sleep is crucial for learning."),
            ("outcome", "natija, oqibat", "noun", "the result of an action or process", "Nobody could predict the outcome of the match."),
        };
        Cards(words.Select(w => new NewCard(ReviewCardKind.Word, w.Item1, w.Item2, Vocabulary.ComposeNote(w.Item3, w.Item4, w.Item5))), null, 3);

        // ---- Takrorlash tarixi: har kuni bir nechta karta; ba'zilari hozir navbatda ----
        for (var i = 0; i < cards.Count; i++)
        {
            var card = cards[i];
            var daysAgo = today.DayNumber - ReviewScheduler.ToLocalDate(card.CreatedAtUtc, tzOffsetMinutes).DayNumber;
            // Har ikkinchi karta (va kecha qo'shilganlar) — hali takrorlanmagan, hozir navbatda.
            if (i % 2 == 1 || daysAgo < 2)
            {
                card.DueAtUtc = nowUtc.AddMinutes(-5 - i);
                continue;
            }
            // Qolganlari: ertasiga, keyin yana ikki kundan so'ng takrorlangan.
            var reviewDays = new[] { daysAgo - 1, daysAgo - 3 }.Where(d => d >= 1).ToList();
            foreach (var d in reviewDays)
            {
                var at = At(d, 20).AddMinutes(i);
                logs.Add(new ReviewLog { Id = Guid.NewGuid(), UserId = userId, CardId = card.Id, Grade = 3, ReviewedAtUtc = at });
                card.LastReviewedAtUtc = at;
            }
            card.Repetitions = reviewDays.Count;
            card.IntervalDays = reviewDays.Count == 1 ? 1 : 4;
            card.DueAtUtc = card.LastReviewedAtUtc!.Value.AddDays(card.IntervalDays + 2);
        }

        // Seriya: oxirgi 6 kunning har birida kamida bitta takrorlash bor.
        for (var d = 1; d <= StreakDays; d++)
        {
            if (logs.Any(l => ReviewScheduler.ToLocalDate(l.ReviewedAtUtc, tzOffsetMinutes) == today.AddDays(-d))) continue;
            var card = cards[d % cards.Count];
            logs.Add(new ReviewLog { Id = Guid.NewGuid(), UserId = userId, CardId = card.Id, Grade = 3, ReviewedAtUtc = At(d, 21) });
        }

        // Bugun: 3 ta karta takrorlangan (kunlik maqsaddan kam — reja ochiq qoladi).
        var todayCards = cards.Where(c => c.DueAtUtc > nowUtc).Take(ReviewedToday).ToList();
        for (var i = 0; i < todayCards.Count; i++)
        {
            var at = nowUtc.AddSeconds(-30 - i * 20);
            logs.Add(new ReviewLog { Id = Guid.NewGuid(), UserId = userId, CardId = todayCards[i].Id, Grade = 3, ReviewedAtUtc = at });
            todayCards[i].LastReviewedAtUtc = at;
        }

        return new DemoData(activities, cards, logs);
    }
}
