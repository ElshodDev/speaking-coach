namespace SpeakingCoach.Api.Services.Mock;

// IELTS formatidagi mock savollar to'plami. Savollar ORIGINAL — haqiqiy
// imtihon savollari ko'chirilmagan; faqat tuzilishi rasmiy formatga mos
// (ielts.org: Speaking 3 qism, 11–14 daqiqa; Part 2 — 1 daqiqa tayyorgarlik
// va 2 daqiqagacha gapirish. Writing 60 daqiqa; Task 1 ≥150 so'z,
// Task 2 ≥250 so'z, Task 2 ning ulushi Task 1 dan ikki barobar).
//
// Nega AI yaratmaydi: tayyor to'plam — bepul (Gemini kvotasi faqat
// baholashga ketadi), bir zumda ochiladi va sifati oldindan tekshirilgan.

public record CueCard(string Topic, string[] Points, string Explain);

public record SpeakingSet(string Id, string Part1Topic, string[] Part1, CueCard Part2, string[] Part3)
{
    /// <summary>Barcha savollar imtihon tartibida: Part 1 → Part 2 (bitta) → Part 3.</summary>
    public IReadOnlyList<(int Part, string Text)> Questions() =>
        Part1.Select(q => (1, q))
            .Append((2, Part2.Topic))
            .Concat(Part3.Select(q => (3, q)))
            .ToList();
}

/// <summary>Task 1 (Academic) diagrammasi uchun ma'lumot: toifalar × seriyalar.</summary>
public record ChartSeries(string Name, double[] Values);
public record ChartData(string Kind, string Title, string Unit, string[] Categories, ChartSeries[] Series);

public record WritingTask1(string Prompt, ChartData? Chart);
public record WritingSet(string Id, string Variant, WritingTask1 Task1, string Task2);

public static class IeltsBank
{
    public const string Academic = "academic";
    public const string General = "general";

    // Vaqtlar (soniya). Rasmiy: Part 1 va 3 — 4–5 daqiqadan; bitta javobga
    // aniq chegara yo'q — imtihonchi to'xtatadi. Biz Part 1 uchun 45 s,
    // Part 3 uchun 75 s chegara qo'yamiz: 4×45 + 60 + 120 + 4×75 ≈ 11 daqiqa.
    public const int Part1AnswerSeconds = 45;
    public const int Part2PrepSeconds = 60;
    public const int Part2SpeakSeconds = 120;
    public const int Part3AnswerSeconds = 75;
    public const int WritingMinutes = 60;
    public const int Task1MinWords = 150;
    public const int Task2MinWords = 250;

    public static readonly SpeakingSet[] Speaking =
    [
        new("s1", "Your home town",
            [
                "Where is your home town?",
                "What do you like most about living there?",
                "Has your home town changed much in recent years?",
                "Would you like to live somewhere else in the future? Why?",
            ],
            new("Describe a place in your city where you like to spend time.",
                ["where it is", "how often you go there", "what you do there"],
                "and explain why you enjoy spending time in this place."),
            [
                "Why do people in cities need public places such as parks?",
                "How have public places in your country changed over the last twenty years?",
                "Should local governments spend more money on public spaces or on housing?",
                "Do you think cities will have more or fewer green areas in the future?",
            ]),
        new("s2", "Studying",
            [
                "Are you working or studying at the moment?",
                "What subject did you enjoy most at school?",
                "Do you prefer studying alone or with other people?",
                "Is there anything new you would like to learn?",
            ],
            new("Describe a teacher who has influenced you.",
                ["who this teacher was", "what subject they taught", "what they were like"],
                "and explain how this teacher influenced you."),
            [
                "What qualities make a good teacher?",
                "Is it better to learn from a teacher or from the internet?",
                "How has technology changed the way young people study?",
                "Should education be free at every level, including university?",
            ]),
        new("s3", "Food and cooking",
            [
                "What kind of food do you like to eat?",
                "Do you often cook at home?",
                "Is there any food you disliked as a child but like now?",
                "How often do you eat out in restaurants?",
            ],
            new("Describe a special meal you have had.",
                ["where you had it", "who you were with", "what you ate"],
                "and explain why this meal was special for you."),
            [
                "Why do some families no longer eat together?",
                "How are the eating habits of young people different from those of older people?",
                "Should governments do more to encourage healthy eating?",
                "Do you think traditional dishes will become less popular in the future?",
            ]),
        new("s4", "Technology",
            [
                "How often do you use your phone each day?",
                "Which app do you use most? Why?",
                "Did you use computers when you were a child?",
                "Is there any piece of technology you would like to buy?",
            ],
            new("Describe a useful piece of technology you own.",
                ["what it is", "when you got it", "how often you use it"],
                "and explain why it is useful for you."),
            [
                "Do older people find it hard to use new technology? Why?",
                "What are the disadvantages of relying too much on technology?",
                "Should children be allowed to have their own smartphones?",
                "How might artificial intelligence change people's jobs?",
            ]),
        new("s5", "Travel",
            [
                "Do you like travelling?",
                "What kind of places do you like to visit?",
                "Do you prefer travelling alone or with friends?",
                "Where did you go on your last holiday?",
            ],
            new("Describe a trip you would like to take in the future.",
                ["where you would like to go", "who you would go with", "what you would do there"],
                "and explain why you would like to take this trip."),
            [
                "Why do people like to travel to other countries?",
                "What are the effects of tourism on local communities?",
                "Is it better to travel by plane or by train? Why?",
                "Will people travel more or less in the future?",
            ]),
        new("s6", "Free time",
            [
                "What do you usually do in your free time?",
                "Do you have more free time now than when you were a child?",
                "Do you prefer spending free time indoors or outdoors?",
                "Is there a hobby you would like to try?",
            ],
            new("Describe a hobby that you enjoy.",
                ["what the hobby is", "when you started it", "how much time you spend on it"],
                "and explain why you enjoy this hobby."),
            [
                "Why is it important for people to have hobbies?",
                "Do men and women usually have different hobbies?",
                "How do hobbies today differ from hobbies in the past?",
                "Should schools teach hobbies such as music or art?",
            ]),
    ];

    public static readonly WritingSet[] Writing =
    [
        new("a1", Academic,
            new("The chart below shows the percentage of households with internet access in three countries between 2005 and 2020.\n\nSummarise the information by selecting and reporting the main features, and make comparisons where relevant.",
                new("bar", "Households with internet access", "%",
                    ["2005", "2010", "2015", "2020"],
                    [
                        new("Country A", [42, 61, 78, 90]),
                        new("Country B", [18, 35, 57, 74]),
                        new("Country C", [55, 68, 72, 81]),
                    ])),
            "Some people believe that university education should be free for all students, while others think students should pay for their own studies.\n\nDiscuss both these views and give your own opinion."),
        new("a2", Academic,
            new("The chart below shows the average number of hours per week that people in one city spent on four leisure activities in 2000 and 2020.\n\nSummarise the information by selecting and reporting the main features, and make comparisons where relevant.",
                new("bar", "Weekly hours spent on leisure activities", "hours",
                    ["Watching TV", "Using the internet", "Sport", "Reading"],
                    [
                        new("2000", [14, 3, 4, 5]),
                        new("2020", [8, 16, 3, 2]),
                    ])),
            "In many countries, people are living longer than ever before.\n\nWhat problems does this cause for society? What solutions can you suggest?"),
        new("a3", Academic,
            new("The chart below shows how electricity was produced in one country in 1990 and 2020.\n\nSummarise the information by selecting and reporting the main features, and make comparisons where relevant.",
                new("bar", "Sources of electricity", "% of total",
                    ["Coal", "Gas", "Nuclear", "Hydro", "Solar and wind"],
                    [
                        new("1990", [52, 20, 15, 11, 2]),
                        new("2020", [21, 34, 12, 10, 23]),
                    ])),
            "Some people think that children should start learning a foreign language at primary school rather than at secondary school.\n\nDo the advantages of this outweigh the disadvantages?"),
        new("g1", General,
            new("You recently bought a product online, but when it arrived it was damaged.\n\nWrite a letter to the company. In your letter:\n• describe what you bought\n• explain what was wrong with it\n• say what you would like the company to do\n\nBegin your letter as follows: Dear Sir or Madam,", null),
            "Many people today work from home instead of travelling to an office.\n\nDo the advantages of working from home outweigh the disadvantages?"),
        new("g2", General,
            new("A friend of yours is planning to visit your city for the first time next month.\n\nWrite a letter to your friend. In your letter:\n• suggest the best time to visit\n• recommend some places to see\n• offer to help with something during the visit\n\nBegin your letter as follows: Dear ...,", null),
            "Some people say that the best way to improve public health is to increase the number of sports facilities. Others believe this would have little effect.\n\nDiscuss both these views and give your own opinion."),
        new("g3", General,
            new("You are unable to attend an important course that you have already paid for.\n\nWrite a letter to the course organiser. In your letter:\n• explain which course you registered for\n• say why you cannot attend\n• ask what can be done about your payment\n\nBegin your letter as follows: Dear Sir or Madam,", null),
            "Advertising encourages people to buy things they do not really need.\n\nTo what extent do you agree or disagree?"),
    ];

    public static SpeakingSet? FindSpeaking(string? id) => Speaking.FirstOrDefault(s => s.Id == id);
    public static WritingSet? FindWriting(string? id) => Writing.FirstOrDefault(w => w.Id == id);

    /// <summary>
    /// Yaqinda ishlanmagan to'plamni tanlaydi: avval hech ishlanmaganlardan,
    /// hammasi ishlangan bo'lsa — eng uzoq vaqt oldin ishlanganidan.
    /// </summary>
    public static T PickFresh<T>(IReadOnlyList<T> sets, Func<T, string> id, IReadOnlyList<string> recentIdsNewestFirst, Random rng)
    {
        var unused = sets.Where(s => !recentIdsNewestFirst.Contains(id(s))).ToList();
        if (unused.Count > 0) return unused[rng.Next(unused.Count)];
        return sets.OrderByDescending(s => recentIdsNewestFirst.ToList().IndexOf(id(s))).First();
    }
}
