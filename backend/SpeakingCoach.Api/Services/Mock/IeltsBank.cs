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
        new("s7", "Weather",
            [
                "What is the weather usually like where you live?",
                "Which season do you like best? Why?",
                "Do you check the weather forecast every day?",
                "Does the weather ever change your mood?",
            ],
            new("Describe a time when the weather changed your plans.",
                ["when and where it happened", "what you had planned to do", "what the weather was like"],
                "and explain how you felt about the change of plans."),
            [
                "Why do people often talk about the weather?",
                "How does the weather affect the kind of work people do?",
                "Are weather forecasts more reliable now than in the past?",
                "Do you think extreme weather will become more common in the future?",
            ]),
        new("s8", "Shopping",
            [
                "Do you enjoy going shopping?",
                "Where do you usually buy your clothes?",
                "Do you prefer shopping online or in shops?",
                "Have you ever bought something you did not really need?",
            ],
            new("Describe a market or shop that you like to visit.",
                ["where it is", "what it sells", "how often you go there"],
                "and explain why you like visiting this place."),
            [
                "Why do some people still prefer traditional markets to supermarkets?",
                "How has online shopping changed the way people buy things?",
                "Do you think people buy too many things nowadays?",
                "What will shopping be like in twenty years' time?",
            ]),
        new("s9", "Music",
            [
                "What kind of music do you listen to?",
                "When do you usually listen to music?",
                "Did you learn to play a musical instrument as a child?",
                "Do you prefer listening to music alone or with other people?",
            ],
            new("Describe a song or piece of music that is special to you.",
                ["what it is", "when you first heard it", "how often you listen to it"],
                "and explain why this music is special to you."),
            [
                "Why is music important in people's lives?",
                "How has technology changed the way people listen to music?",
                "Should every child learn to play a musical instrument at school?",
                "Is traditional music still popular with young people in your country?",
            ]),
        new("s10", "Friends",
            [
                "How often do you meet your friends?",
                "What do you usually do together?",
                "Do you prefer to have a few close friends or many friends?",
                "Is it easy for you to make new friends?",
            ],
            new("Describe a friend you have known for a long time.",
                ["how you met", "what this person is like", "what you usually do together"],
                "and explain why your friendship has lasted so long."),
            [
                "What qualities make someone a good friend?",
                "Is it possible to have real friends that you only know online?",
                "How do friendships change as people get older?",
                "Why do some friendships end?",
            ]),
        new("s11", "Sport",
            [
                "Do you play any sports?",
                "What sports were popular at your school?",
                "Do you prefer watching sport on TV or at a stadium?",
                "Is there a sport you would like to try in the future?",
            ],
            new("Describe a sports event that you watched or took part in.",
                ["what the event was", "where and when it took place", "who you were with"],
                "and explain why you remember this event."),
            [
                "Why do people enjoy watching sports events?",
                "Do professional athletes earn too much money?",
                "How can young people be encouraged to do more sport?",
                "Is it important for a country to host international sports competitions?",
            ]),
        new("s12", "Reading",
            [
                "Do you like reading?",
                "What kind of things do you read every day?",
                "Did your parents read to you when you were a child?",
                "Do you prefer printed books or reading on a screen?",
            ],
            new("Describe a book that you found useful.",
                ["what the book was", "when you read it", "what it was about"],
                "and explain why you found this book useful."),
            [
                "Why do some people stop reading books when they finish school?",
                "What can parents do to encourage children to read?",
                "Are libraries still important in the age of the internet?",
                "Do you think printed books will disappear in the future?",
            ]),
        new("s13", "Neighbours",
            [
                "Do you know your neighbours well?",
                "How often do you talk to your neighbours?",
                "Have your neighbours ever helped you?",
                "What makes a good neighbour?",
            ],
            new("Describe a time when you helped someone.",
                ["who you helped", "what the situation was", "how you helped them"],
                "and explain how you felt after helping this person."),
            [
                "Why do some people enjoy helping others?",
                "Are people less helpful to strangers than they were in the past?",
                "Should schools teach children to do volunteer work?",
                "What kind of help do elderly people need most in your community?",
            ]),
        new("s14", "Photographs",
            [
                "Do you like taking photos?",
                "What do you usually take photos of?",
                "Do you prefer taking photos with a phone or a camera?",
                "Do you keep printed photos at home?",
            ],
            new("Describe a photograph that you like.",
                ["when and where it was taken", "who or what is in the photo", "where you keep it"],
                "and explain why you like this photograph."),
            [
                "Why do people like to keep photos of important events?",
                "How has the way people share photos changed in recent years?",
                "Do people take too many photos nowadays?",
                "Is photography a form of art in the same way as painting?",
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
        new("a4", Academic,
            new("The chart below shows how people in one city travelled to work in 2000, 2010 and 2020.\n\nSummarise the information by selecting and reporting the main features, and make comparisons where relevant.",
                new("bar", "Ways of travelling to work", "% of workers",
                    ["Car", "Bus", "Metro", "Bicycle", "Walking"],
                    [
                        new("2000", [30, 38, 12, 5, 15]),
                        new("2010", [38, 30, 16, 4, 12]),
                        new("2020", [34, 22, 26, 8, 10]),
                    ])),
            "In many countries, young people are moving from villages to large cities to find work.\n\nWhy is this happening? What effects does this have on the villages they leave behind?"),
        new("a5", Academic,
            new("The chart below shows the percentage of men and women in different age groups who did regular exercise in one country in 2022.\n\nSummarise the information by selecting and reporting the main features, and make comparisons where relevant.",
                new("bar", "People who exercise regularly, by age group", "%",
                    ["16–24", "25–34", "35–44", "45–54", "55–64", "65+"],
                    [
                        new("Men", [62, 54, 45, 40, 35, 28]),
                        new("Women", [48, 46, 44, 41, 38, 30]),
                    ])),
            "Some people think that famous sports players are paid far too much compared with people in important jobs such as nurses and teachers.\n\nTo what extent do you agree or disagree?"),
        new("a6", Academic,
            new("The chart below shows the number of printed books, e-books and audiobooks sold in one country between 2012 and 2024.\n\nSummarise the information by selecting and reporting the main features, and make comparisons where relevant.",
                new("bar", "Book sales by format", "million copies",
                    ["2012", "2015", "2018", "2021", "2024"],
                    [
                        new("Printed books", [52, 47, 44, 40, 38]),
                        new("E-books", [6, 14, 17, 18, 17]),
                        new("Audiobooks", [1, 2, 5, 9, 14]),
                    ])),
            "Nowadays more and more people read the news on websites and social media rather than in newspapers.\n\nIs this a positive or negative development?"),
        new("a7", Academic,
            new("The chart below shows the percentage of university graduates in one country who found full-time work within six months of graduating, by subject, in 2015 and 2023.\n\nSummarise the information by selecting and reporting the main features, and make comparisons where relevant.",
                new("bar", "Graduates in full-time work six months after graduating", "%",
                    ["Medicine", "Engineering", "Computing", "Business", "Arts"],
                    [
                        new("2015", [91, 78, 74, 65, 48]),
                        new("2023", [93, 82, 86, 63, 44]),
                    ])),
            "Some people believe that schools should focus mainly on academic subjects such as mathematics and science. Others think that practical skills, such as cooking and managing money, are just as important.\n\nDiscuss both these views and give your own opinion."),
        new("a8", Academic,
            new("The chart below shows how household waste was dealt with in one city in 2005, 2015 and 2025.\n\nSummarise the information by selecting and reporting the main features, and make comparisons where relevant.",
                new("bar", "Treatment of household waste", "% of total",
                    ["Landfill", "Recycling", "Composting", "Incineration"],
                    [
                        new("2005", [68, 15, 5, 12]),
                        new("2015", [47, 28, 10, 15]),
                        new("2025", [26, 39, 17, 18]),
                    ])),
            "Some people think that individuals can do very little to protect the environment and that only governments and large companies can make a real difference.\n\nTo what extent do you agree or disagree?"),
        new("a9", Academic,
            new("The chart below shows the average number of hours per day that children in three age groups in one country spent using screens on school days and at weekends in 2023.\n\nSummarise the information by selecting and reporting the main features, and make comparisons where relevant.",
                new("bar", "Children's daily screen time, by age group", "hours per day",
                    ["Aged 6–9", "Aged 10–13", "Aged 14–17"],
                    [
                        new("School days", [2.1, 3.4, 4.6]),
                        new("Weekends", [3.5, 5.0, 6.2]),
                    ])),
            "In the future, many jobs that are done by people today may be done by computers and robots.\n\nIs this a positive or negative development?"),
        new("a10", Academic,
            new("The chart below shows the number of overseas visitors to one country, by main reason for visiting, in 2014, 2019 and 2024.\n\nSummarise the information by selecting and reporting the main features, and make comparisons where relevant.",
                new("bar", "Overseas visitors by main reason for visit", "million visitors",
                    ["Holiday", "Business", "Visiting friends or family", "Study"],
                    [
                        new("2014", [8.2, 3.1, 2.4, 0.6]),
                        new("2019", [11.5, 3.6, 3.0, 0.9]),
                        new("2024", [12.8, 2.2, 3.9, 1.3]),
                    ])),
            "Some people believe that museums and art galleries should be free for everyone to visit. Others think that visitors should pay an entrance fee.\n\nDiscuss both these views and give your own opinion."),
        new("a11", Academic,
            new("The chart below shows the average age at which men and women in one country married for the first time in 1980, 1995, 2010 and 2025.\n\nSummarise the information by selecting and reporting the main features, and make comparisons where relevant.",
                new("bar", "Average age at first marriage", "years",
                    ["1980", "1995", "2010", "2025"],
                    [
                        new("Men", [25.8, 27.1, 29.4, 31.2]),
                        new("Women", [23.4, 25.0, 27.6, 29.8]),
                    ])),
            "In many cities, cars are allowed to drive into the city centre, which causes traffic jams and air pollution. Some people think that private cars should be banned from city centres completely.\n\nTo what extent do you agree or disagree?"),
        new("g1", General,
            new("You recently bought a product online, but when it arrived it was damaged.\n\nWrite a letter to the company. In your letter:\n• describe what you bought\n• explain what was wrong with it\n• say what you would like the company to do\n\nBegin your letter as follows: Dear Sir or Madam,", null),
            "Many people today work from home instead of travelling to an office.\n\nDo the advantages of working from home outweigh the disadvantages?"),
        new("g2", General,
            new("A friend of yours is planning to visit your city for the first time next month.\n\nWrite a letter to your friend. In your letter:\n• suggest the best time to visit\n• recommend some places to see\n• offer to help with something during the visit\n\nBegin your letter as follows: Dear ...,", null),
            "Some people say that the best way to improve public health is to increase the number of sports facilities. Others believe this would have little effect.\n\nDiscuss both these views and give your own opinion."),
        new("g3", General,
            new("You are unable to attend an important course that you have already paid for.\n\nWrite a letter to the course organiser. In your letter:\n• explain which course you registered for\n• say why you cannot attend\n• ask what can be done about your payment\n\nBegin your letter as follows: Dear Sir or Madam,", null),
            "Advertising encourages people to buy things they do not really need.\n\nTo what extent do you agree or disagree?"),
        new("g4", General,
            new("Your neighbour has recently started making a lot of noise late in the evening.\n\nWrite a letter to your neighbour. In your letter:\n• describe the noise and when it happens\n• explain how it is affecting you\n• suggest a solution to the problem\n\nBegin your letter as follows: Dear ...,", null),
            "Some people think parents should give children pocket money so that they learn how to manage money. Others think this is unnecessary.\n\nDiscuss both these views and give your own opinion."),
        new("g5", General,
            new("You would like to change your working hours at your job.\n\nWrite a letter to your manager. In your letter:\n• explain why you would like to change your hours\n• say which hours you would prefer to work\n• explain how this change will not cause problems for your team\n\nBegin your letter as follows: Dear ...,", null),
            "Many people now prefer to shop in large shopping centres rather than in small local shops.\n\nWhy is this? Is this a positive or negative development?"),
        new("g6", General,
            new("You stayed at a hotel last week and left something valuable in your room.\n\nWrite a letter to the hotel manager. In your letter:\n• give details of your stay\n• describe the item you left behind\n• explain how you would like it to be returned to you\n\nBegin your letter as follows: Dear Sir or Madam,", null),
            "Some people believe that governments should spend money on repairing old buildings rather than building new ones.\n\nTo what extent do you agree or disagree?"),
        new("g7", General,
            new("You started a new job a few weeks ago, and one of your colleagues has helped you a lot since you arrived.\n\nWrite a letter to your colleague. In your letter:\n• describe how your colleague has helped you\n• explain what difference this help has made to you\n• invite your colleague to do something with you as a thank-you\n\nBegin your letter as follows: Dear ...,", null),
            "In many families today, grandparents look after young children while both parents are at work.\n\nDo the advantages of this outweigh the disadvantages?"),
        new("g8", General,
            new("You would like to take an evening course at a local college, but you need more information before you register.\n\nWrite a letter to the college. In your letter:\n• say which course you are interested in\n• explain why you want to take this course\n• ask for information about the timetable and the fees\n\nBegin your letter as follows: Dear Sir or Madam,", null),
            "Some people think that governments should make unhealthy food and sugary drinks more expensive by adding higher taxes to them.\n\nTo what extent do you agree or disagree?"),
        new("g9", General,
            new("The public park near your home is in poor condition.\n\nWrite a letter to your local council. In your letter:\n• describe the park and how local people use it\n• explain what the problems are\n• suggest what the council could do to improve it\n\nBegin your letter as follows: Dear Sir or Madam,", null),
            "Some people believe that the number of flights people take should be limited in order to protect the environment. Others believe that people should be free to travel by air as often as they wish.\n\nDiscuss both these views and give your own opinion."),
        new("g10", General,
            new("You are going to travel abroad for three weeks and you need someone to look after your home while you are away.\n\nWrite a letter to a friend. In your letter:\n• explain where you are going and why\n• describe what needs to be done in your home\n• say how you would like to thank your friend\n\nBegin your letter as follows: Dear ...,", null),
            "In many countries, traditional crafts and customs are slowly disappearing.\n\nWhy is this happening? What can be done to protect them?"),
        new("g11", General,
            new("You are a member of a sports centre, and you are unhappy about some changes that were made there recently.\n\nWrite a letter to the manager of the sports centre. In your letter:\n• explain how long you have been a member\n• describe the changes that were made\n• say what you would like the manager to do\n\nBegin your letter as follows: Dear Sir or Madam,", null),
            "Some people think that young people should take a year off between finishing school and starting university in order to work or travel.\n\nDo the advantages of this outweigh the disadvantages?"),
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
