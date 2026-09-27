namespace SpeakingCoach.Api.Services.Mock;

// CEFR (Multilevel, O'zbekiston) formatidagi mock savollar. Matnlar ORIGINAL.
//
// RASMIY MANBALAR (Bilim va malakalarni baholash agentligi):
// 1. "Ko'p darajali test sinovi gapirish ko'nikmasi yangi formati" (2024-yil
//    sentabrdan): 1.1 — 3 savol, har biriga 30 s, tayyorgarliksiz;
//    1.2 — rasmlarni solishtirish, 3 savol: 4-savol 45 s, 5–6-savollar 30 s;
//    2 — mavhum mavzu, 3 savol, bitta rasm (tasvirlash talab qilinmaydi),
//    1 daqiqa tayyorgarlik, 2 daqiqa javob; 3 — bahsning ikki tomonini
//    tahlil qilish, 1 savol, 1 daqiqa tayyorgarlik, 2 daqiqa javob.
// 2. "Rating scale for multilevel speaking exams" (yangi format): 1–3-savollar
//    0–5, 4–6-savollar 0–5, 7-savol 0–5, 8-savol 0–6 (jami 21).
// 3. Yozish ko'nikmasi yangi formati (aprobatsiya 2025-yil avgust, joriy —
//    oktabrdan): 1-qism: 1.1 norasmiy xat (taxminan 50 so'z), 1.2 rasmiy xat
//    (120–150 so'z); 2-qism: onlayn muhokama posti (180–200 so'z); 5 mezon:
//    topshiriq bajarilishi, grammatika, lug'at, izchillik va bog'lanish,
//    tinish belgilari va imlo.
// 4. "Baholash mezonlari" (16.03.2023): 0–75 shkala, Writing 1-qism 12 ball,
//    2-qism 24 ball, xom ball (0–36) → 75 jadvali, daraja chegaralari.
// E'LON QILINMAGAN (taxmin, natija sahifasida yozilgan): Speaking 0–21 xom
// ballni 75 ga o'tkazish (chiziqli), Writing vaqti (60 daqiqa).

public record CefrPicture(string Emoji, string Caption);

public record CefrSpeakingSet(
    string Id,
    string[] Part11,
    CefrPicture[] Pictures,
    string[] Part12,
    string Part2Topic,
    string[] Part2Questions,
    string Part3Statement,
    string[] For,
    string[] Against,
    CefrPicture Part2Picture)
{
    /// <summary>Javoblar imtihon tartibida: 1.1 (3) → 1.2 (3) → 2 (1) → 3 (1).</summary>
    public IReadOnlyList<(string Part, string Text)> Questions() =>
        Part11.Select(q => ("1.1", q))
            .Concat(Part12.Select(q => ("1.2", q)))
            .Append(("2", Part2Topic))
            .Append(("3", Part3Statement))
            .ToList();
}

public record CefrWritingSet(
    string Id,
    string Role,
    string EmailFrom,
    string Email,
    string Task11,
    string Task12,
    string Task2);

public static class CefrBank
{
    public const int Part11Seconds = 30;
    /// <summary>1.2-qism: birinchi savol (4-savol) — 45 s, qolganlari (5–6) — 30 s (rasmiy).</summary>
    public const int Part12FirstSeconds = 45;
    public const int Part12Seconds = 30;
    public const int Part2PrepSeconds = 60;
    public const int Part2SpeakSeconds = 120;
    public const int Part3PrepSeconds = 60;
    public const int Part3SpeakSeconds = 120;

    public const int WritingMinutes = 60;
    /// <summary>Rasmiy: "taxminan 50 so'z" — ±10 so'z oraliq sifatida tekshiriladi.</summary>
    public static readonly (int Min, int Max) Words11 = (40, 60);
    public static readonly (int Min, int Max) Words12 = (120, 150);
    public static readonly (int Min, int Max) Words2 = (180, 200);

    public static readonly CefrSpeakingSet[] Speaking =
    [
        new("c1",
            ["What do you usually do in the evenings?", "Do you prefer mornings or evenings? Why?", "What is your favourite season of the year?"],
            [new("🚌", "Picture A: people travelling to work by crowded bus"), new("🚲", "Picture B: a person cycling to work along a quiet road")],
            ["Describe what you can see in the two pictures.", "Which way of travelling to work do you prefer and why?", "How could cities make travelling to work easier?"],
            "Talk about a journey that you remember well.",
            ["Where did you go and who were you with?", "What happened during the journey?", "Why do you still remember it?"],
            "Public transport should be free for everyone.",
            ["Fewer cars and less pollution", "Helps people with low incomes", "Less traffic in city centres"],
            ["Very expensive for the government", "Buses may become overcrowded", "Less money to improve the service"],
            new("🧳", "A picture of a traveller with a suitcase at a railway station")),
        new("c2",
            ["Where do you usually study or work?", "How often do you use the internet in a day?", "Do you like reading books? Why?"],
            [new("📚", "Picture A: students studying together in a library"), new("💻", "Picture B: a student studying alone online at home")],
            ["Describe what you can see in the two pictures.", "Which way of studying suits you better and why?", "Will students study more online in the future?"],
            "Talk about a teacher who helped you a lot.",
            ["Who was this teacher and what did they teach?", "How did they help you?", "What did you learn from them apart from the subject?"],
            "Online learning is better than learning in a classroom.",
            ["Students can learn at their own pace", "It saves travel time and money", "Lessons can be watched again"],
            ["Less contact with teachers and classmates", "Harder to stay motivated", "Not everyone has a good internet connection"],
            new("🍎", "A picture of a teacher standing in front of a classroom")),
        new("c3",
            ["What kind of food do you like?", "Who usually cooks in your family?", "Do you often eat outside the home?"],
            [new("🍲", "Picture A: a family eating a home-cooked meal together"), new("🍔", "Picture B: young people eating fast food in a busy café")],
            ["Describe what you can see in the two pictures.", "Which kind of meal do you enjoy more and why?", "Why do many young people choose fast food?"],
            "Talk about a special celebration you attended.",
            ["What was the celebration and where was it?", "What did people do there?", "Why was it special for you?"],
            "Schools should ban unhealthy food and drinks.",
            ["Children develop healthier habits", "It can reduce health problems later in life", "Students may concentrate better in class"],
            ["Students will buy the same food outside school", "It limits personal choice", "Healthy food can be more expensive"],
            new("🎉", "A picture of a family celebration with a big table of food")),
        new("c4",
            ["What do you like doing at the weekend?", "Do you prefer spending time indoors or outdoors?", "What sport do you enjoy watching?"],
            [new("🏃", "Picture A: a group of people jogging in a park"), new("🏋️", "Picture B: a person exercising alone in a modern gym")],
            ["Describe what you can see in the two pictures.", "Which way of keeping fit would you choose and why?", "Why do some people find it hard to exercise regularly?"],
            "Talk about a skill you would like to learn.",
            ["What is the skill?", "How would you learn it?", "How would it be useful in your life?"],
            "Young people spend too much time on their phones.",
            ["Less time for sport and hobbies", "It can harm sleep and eyesight", "Less face-to-face communication"],
            ["Phones help them study and find information", "They keep in touch with family and friends", "Many jobs now need digital skills"],
            new("🎸", "A picture of a young person practising the guitar")),
        new("c5",
            ["Where are you from?", "What do you like most about your neighbourhood?", "Would you like to live in another country? Why?"],
            [new("🏙️", "Picture A: a busy modern city centre with tall buildings"), new("🏡", "Picture B: a quiet village surrounded by fields")],
            ["Describe what you can see in the two pictures.", "Where would you prefer to live and why?", "Why do many young people move from villages to cities?"],
            "Talk about a place you would like to visit in the future.",
            ["Where is this place?", "What would you do there?", "Why do you want to visit it?"],
            "Tourism does more harm than good to local communities.",
            ["Prices for local people go up", "Traffic, noise and rubbish increase", "Traditional culture can change"],
            ["Tourism creates jobs", "Local businesses earn more money", "People learn about other cultures"],
            new("🗺️", "A picture of a map with several places marked on it")),
        new("c6",
            ["What job would you like to have in the future?", "What do you do to relax after a busy day?", "Do you prefer shopping in shops or online?"],
            [new("🏢", "Picture A: people working together in a busy office"), new("🏠", "Picture B: a person working from home at a kitchen table")],
            ["Describe what you can see in the two pictures.", "Which working environment would you prefer and why?", "How might offices change in the next ten years?"],
            "Talk about an important decision you made.",
            ["What was the decision?", "Who helped you to decide?", "How did it change your life?"],
            "Everyone should learn at least two foreign languages at school.",
            ["More job opportunities in the future", "Better understanding of other cultures", "It develops memory and thinking skills"],
            ["There is less time for other subjects", "Not all students need foreign languages", "Schools may not have enough good teachers"],
            new("🤝", "A picture of two people shaking hands in an office")),
        new("c7",
            ["What kind of music do you like?", "Do you have a pet? Why or why not?", "How do you usually celebrate your birthday?"],
            [new("🧺", "Picture A: people buying fruit and vegetables at a busy outdoor market"), new("🛍️", "Picture B: a person shopping in a large modern shopping centre")],
            ["Describe what you can see in the two pictures.", "Where do you prefer to shop and why?", "How has shopping changed in your country in recent years?"],
            "Talk about a gift that you gave or received.",
            ["What was the gift and who was it for?", "Why was this gift chosen?", "How did people feel about it?"],
            "Children should get pocket money from their parents.",
            ["Children learn how to manage money", "They become more independent", "They understand the value of things"],
            ["They may spend it on unhealthy snacks", "Children should earn money by helping at home", "Not all families can afford it"],
            new("🎁", "A picture of a child opening a wrapped present")),
        new("c8",
            ["What is the weather like in your region today?", "How much time do you spend outdoors every day?", "Where did you go on your last holiday?"],
            [new("🏕️", "Picture A: a family camping in the mountains"), new("🏨", "Picture B: a couple relaxing by the swimming pool of a hotel")],
            ["Describe what you can see in the two pictures.", "Which kind of holiday would you enjoy more and why?", "Why do some people prefer to spend their holidays at home?"],
            "Talk about a time you spent a day in nature.",
            ["Where were you and who were you with?", "What did you do there?", "How did you feel at the end of the day?"],
            "Holidays abroad are better than holidays in your own country.",
            ["People experience a new culture", "It is a chance to practise foreign languages", "Travellers can see famous places around the world"],
            ["Travelling abroad is very expensive", "Long flights can be tiring", "Your own country has many interesting places to discover"],
            new("🏞️", "A picture of a river valley with mountains in the background")),
        new("c9",
            ["Do you have any brothers or sisters?", "Who do you spend most of your free time with?", "What do you usually do on public holidays?"],
            [new("👪", "Picture A: a large family of three generations living together in one house"), new("🏢", "Picture B: a young person living alone in a small flat in the city")],
            ["Describe what you can see in the two pictures.", "Which way of living do you think is better and why?", "Why do more young people live alone nowadays?"],
            "Talk about an older person you respect.",
            ["Who is this person?", "What have you learned from them?", "Why do you respect them?"],
            "Young people should live with their parents until they finish their studies.",
            ["They save money on rent and food", "They can help and support their parents", "Family ties stay strong"],
            ["They become independent later", "They do not learn important life skills", "Their university may be in another city"],
            new("👴", "A picture of a grandfather telling a story to his grandchildren")),
        new("c10",
            ["How do you usually keep in touch with your friends?", "Do you prefer calling or sending messages? Why?", "What was the last film you watched?"],
            [new("✉️", "Picture A: a person writing a letter by hand at a desk"), new("📱", "Picture B: a young person sending a message on a smartphone")],
            ["Describe what you can see in the two pictures.", "Which way of communicating do you use more and why?", "Do you think people will still write letters by hand in the future?"],
            "Talk about a time when you received some good news.",
            ["What was the news and who told you?", "How did you react?", "How did this news change things for you?"],
            "Social media brings people closer together.",
            ["People can stay in touch with relatives who live far away", "It is easy to find people with the same interests", "Important moments can be shared quickly"],
            ["People meet face to face less often", "Messages can cause misunderstandings", "People often compare their lives with others"],
            new("😊", "A picture of a person reading a message on a phone with a big smile")),
        new("c11",
            ["Is there a park near your home?", "Do you like growing plants or flowers?", "What do you do to stay healthy?"],
            [new("🌳", "Picture A: volunteers planting trees in a city park"), new("♻️", "Picture B: people sorting rubbish into different recycling bins")],
            ["Describe what you can see in the two pictures.", "Which of these activities do you think is more useful and why?", "What else can ordinary people do to keep their town clean?"],
            "Talk about a place in your town that you would like to improve.",
            ["What is the place and where is it?", "What problems does it have now?", "What changes would you make?"],
            "Cars should not be allowed in city centres.",
            ["The air becomes cleaner", "Streets are safer for people walking and cycling", "City centres become quieter"],
            ["It is difficult for older and disabled people", "Shops may lose customers", "Public transport may not be good enough"],
            new("🚧", "A picture of workers repairing a street in a town")),
        new("c12",
            ["Have you ever had a part-time job?", "What skill are you good at?", "Do you prefer working alone or in a team?"],
            [new("🍳", "Picture A: a young cook learning from an experienced chef in a restaurant kitchen"), new("🎓", "Picture B: students listening to a lecture at university")],
            ["Describe what you can see in the two pictures.", "Which way of preparing for a job do you think is better and why?", "Why do some young people start working before they finish their studies?"],
            "Talk about a job you would find interesting.",
            ["What is the job?", "What skills does it need?", "Why would you find it interesting?"],
            "A university degree is necessary for a successful career.",
            ["Students gain deep knowledge of a subject", "Graduates often get better-paid jobs", "Students meet useful people at university"],
            ["Many successful people do not have a degree", "Practical experience can be more useful", "Studying takes a lot of time and money"],
            new("🔬", "A picture of a young scientist working in a laboratory")),
    ];

    public static readonly CefrWritingSet[] Writing =
    [
        new("w1", "You are a member of a local art club. You received an email from the club secretary.", "The Club Secretary",
            "Dear Member,\n\nWe are planning to move our weekly meetings from Tuesday evenings to Sunday mornings because the hall is no longer available on weekdays. We are also thinking about increasing the monthly fee to pay for new painting materials.\n\nWe would like to hear your opinion and any suggestions.\n\nBest wishes,\nThe Club Secretary",
            "Write a letter to your friend, who is also a club member, telling them about the changes, giving your opinion and suggesting what the club should do. Write about 50 words.",
            "Write a letter to the club secretary giving your opinion about the new meeting time and the higher fee, and suggesting how the club could solve these problems. Write 120-150 words.",
            "You are taking part in an online discussion for young people. The question is: \"Should hobbies be taught at school as part of the timetable?\" Write a post giving your opinion with reasons and examples. Write 180-200 words."),
        new("w2", "You live in a block of flats. You received an email from the building manager.", "The Building Manager",
            "Dear Resident,\n\nNext month the lift in our building will be replaced. The work will take about four weeks, and during this time residents will have to use the stairs. We also plan to turn the empty room on the ground floor into a small gym.\n\nPlease let us know your views and suggestions.\n\nKind regards,\nThe Building Manager",
            "Write a letter to your friend, who lives in the same building, telling them about the plans, giving your opinion and suggesting one idea. Write about 50 words.",
            "Write a letter to the building manager giving your opinion about the lift work and the new gym, explaining how residents may be affected, and suggesting what could be done to help them. Write 120-150 words.",
            "You are taking part in an online discussion about modern life. The question is: \"Is it better to live in a flat or in a house?\" Write a post giving your opinion with reasons and examples. Write 180-200 words."),
        new("w3", "You are a student at a language school. You received an email from the school director.", "The School Director",
            "Dear Student,\n\nFrom next term, all homework will be submitted through a new mobile app instead of on paper. We also plan to reduce the number of lessons from four to three per week but make each lesson thirty minutes longer.\n\nWe would be happy to receive your comments and suggestions.\n\nYours sincerely,\nThe School Director",
            "Write a letter to your friend, who studies at the same school, telling them about the news, giving your opinion and suggesting what the school should do. Write about 50 words.",
            "Write a letter to the director giving your opinion about the homework app and the new timetable, and suggesting how the changes could work better for students. Write 120-150 words.",
            "You are taking part in an online discussion for students. The question is: \"Do students get too much homework nowadays?\" Write a post giving your opinion with reasons and examples. Write 180-200 words."),
        new("w4", "You are a regular customer at a local supermarket. You received an email from the store manager.", "The Store Manager",
            "Dear Customer,\n\nWe are planning to replace most of our cashiers with self-service checkout machines. We will also stop giving free plastic bags and start selling paper bags instead.\n\nBefore we make these changes, we would like to know what you think.\n\nKind regards,\nThe Store Manager",
            "Write a letter to your friend, who also shops there, telling them about the plans, giving your opinion and suggesting one improvement. Write about 50 words.",
            "Write a letter to the store manager giving your opinion about the self-service machines and the bag policy, and suggesting how the changes could be made easier for customers. Write 120-150 words.",
            "You are taking part in an online discussion about technology. The question is: \"Will machines replace most workers in shops and offices?\" Write a post giving your opinion with reasons and examples. Write 180-200 words."),
        new("w5", "You are a volunteer at a local animal shelter. You received an email from the shelter coordinator.", "The Shelter Coordinator",
            "Dear Volunteer,\n\nBecause more animals are arriving, we need more help at weekends. We are thinking about asking every volunteer to work at least one weekend a month. We also plan to hold an open day to find new families for our animals.\n\nWe would like to hear your views and ideas.\n\nBest regards,\nThe Shelter Coordinator",
            "Write a letter to your friend, who also volunteers at the shelter, telling them about the plans, giving your opinion and suggesting one idea for the open day. Write about 50 words.",
            "Write a letter to the coordinator giving your opinion about the weekend rule, explaining how it may affect volunteers, and suggesting how the open day could be successful. Write 120-150 words.",
            "You are taking part in an online discussion for young people. The question is: \"Should volunteering be compulsory for teenagers?\" Write a post giving your opinion with reasons and examples. Write 180-200 words."),
        new("w6", "You recently took part in a city marathon. You received an email from the event organiser.", "The Event Organiser",
            "Dear Participant,\n\nThank you for running in this year's city marathon. We would appreciate your feedback on the route, the water stations and the organisation. Next year we are thinking of starting the race two hours earlier and adding a shorter 5 km run for beginners.\n\nPlease share your opinion.\n\nKind regards,\nThe Event Organiser",
            "Write a letter to your friend, who is thinking about joining next year, describing your experience, giving your opinion about the new ideas and suggesting one improvement. Write about 50 words.",
            "Write a letter to the organiser giving feedback on the event, commenting on the planned changes, and suggesting how next year's marathon could be improved. Write 120-150 words.",
            "You are taking part in an online discussion about health. The question is: \"Should governments spend more money on sports facilities?\" Write a post giving your opinion with reasons and examples. Write 180-200 words."),
        new("w7", "You are a member of your local library. You received an email from the library manager.", "The Library Manager",
            "Dear Member,\n\nFrom next month, the library will be closed on Sundays, but it will stay open until 9 p.m. on weekdays. We are also planning to charge a small fee for borrowing newly published books so that we can buy more of them.\n\nWe would be glad to hear your opinion and suggestions.\n\nBest wishes,\nThe Library Manager",
            "Write a letter to your friend, who is also a library member, telling them about the changes, giving your opinion and suggesting what the library should do. Write about 50 words.",
            "Write a letter to the library manager giving your opinion about the new opening hours and the fee for new books, and suggesting how the library could meet the needs of all its members. Write 120-150 words.",
            "You are taking part in an online discussion about reading. The question is: \"Will printed books disappear in the future?\" Write a post giving your opinion with reasons and examples. Write 180-200 words."),
        new("w8", "You are a student at a college. You received an email from the student council.", "The Student Council",
            "Dear Student,\n\nWe are organising an end-of-year trip for all students. We need to choose between a day trip to the mountains and a two-day visit to a historical city. The trip will take place on a weekday, so students will miss one day of lessons. The cost will be shared between the college and the students.\n\nPlease tell us which trip you prefer and why.\n\nKind regards,\nThe Student Council",
            "Write a letter to your friend, who studies at the same college, telling them about the trip, giving your opinion and suggesting which option to choose. Write about 50 words.",
            "Write a letter to the student council giving your opinion about the two trip options and the date, and suggesting how the trip could be organised so that more students can take part. Write 120-150 words.",
            "You are taking part in an online discussion for students. The question is: \"Should schools organise more trips and lessons outside the classroom?\" Write a post giving your opinion with reasons and examples. Write 180-200 words."),
        new("w9", "You live in a residential area. You received an email from the local council.", "The Local Council",
            "Dear Resident,\n\nWe are planning to use the empty land behind the school on your street. We are choosing between a football pitch for young people and a garden with trees and benches for everyone. The building work will take about three months, and part of the street will be closed during this time.\n\nWe would welcome your views and ideas.\n\nYours faithfully,\nThe Local Council",
            "Write a letter to your friend, who lives in the same area, telling them about the plan, giving your opinion and suggesting which option is better. Write about 50 words.",
            "Write a letter to the local council giving your opinion about the two options, explaining how the building work may affect residents, and suggesting how these problems could be reduced. Write 120-150 words.",
            "You are taking part in an online discussion about modern life. The question is: \"Is it better for children to grow up in a city or in the countryside?\" Write a post giving your opinion with reasons and examples. Write 180-200 words."),
        new("w10", "You work part-time at a café. You received an email from the café owner.", "The Café Owner",
            "Dear Team,\n\nBecause we have more customers in the evenings, we are thinking about keeping the café open until midnight from next month. We also want all staff to attend a customer service training course on two Saturday mornings. The training will be unpaid.\n\nPlease let me know what you think and share any suggestions.\n\nBest regards,\nThe Café Owner",
            "Write a letter to your friend, who also works at the café, telling them about the plans, giving your opinion and suggesting what you could both do. Write about 50 words.",
            "Write a letter to the café owner giving your opinion about the new opening hours and the training course, explaining how staff may be affected, and suggesting a fairer solution. Write 120-150 words.",
            "You are taking part in an online discussion for young people. The question is: \"Should students have part-time jobs while they are studying?\" Write a post giving your opinion with reasons and examples. Write 180-200 words."),
        new("w11", "You have a membership card at a local cinema. You received an email from the cinema manager.", "The Cinema Manager",
            "Dear Member,\n\nWe are planning some changes to our cinema. Tickets for evening shows will cost 20% more, but morning shows will become cheaper. We would also like to show more foreign films with subtitles instead of dubbed versions.\n\nBefore we decide, we would like to hear from our members.\n\nKind regards,\nThe Cinema Manager",
            "Write a letter to your friend, who often goes to this cinema with you, telling them about the changes, giving your opinion and suggesting one idea. Write about 50 words.",
            "Write a letter to the cinema manager giving your opinion about the new ticket prices and the subtitled films, and suggesting how the cinema could attract more visitors. Write 120-150 words.",
            "You are taking part in an online discussion about entertainment. The question is: \"Is it better to watch films at the cinema or at home?\" Write a post giving your opinion with reasons and examples. Write 180-200 words."),
        new("w12", "You recently attended a weekend English-speaking camp. You received an email from the camp organiser.", "The Camp Organiser",
            "Dear Participant,\n\nThank you for joining our English-speaking camp. We would like your feedback on the activities, the accommodation and the food. Next year we are thinking of making the camp one week long and asking participants not to use their phones during the day.\n\nWe look forward to hearing from you.\n\nBest wishes,\nThe Camp Organiser",
            "Write a letter to your friend, who wants to go to the camp next year, describing your experience, giving your opinion about the new ideas and suggesting one thing to bring. Write about 50 words.",
            "Write a letter to the organiser giving feedback on the camp, commenting on the planned changes, and suggesting how next year's camp could be even better. Write 120-150 words.",
            "You are taking part in an online discussion about education. The question is: \"Is it better to learn a foreign language abroad or in your own country?\" Write a post giving your opinion with reasons and examples. Write 180-200 words."),
    ];

    public static CefrSpeakingSet? FindSpeaking(string? id) => Speaking.FirstOrDefault(s => s.Id == id);
    public static CefrWritingSet? FindWriting(string? id) => Writing.FirstOrDefault(w => w.Id == id);
}

/// <summary>
/// CEFR 0–75 shkala — RASMIY: "Chet tilini bilish darajasini baholash ko'p
/// darajali test formati uchun baholash mezonlari" (Bilim va malakalarni
/// baholash agentligi ilmiy-metodik kengashi, 16.03.2023; uzbmb.uz,
/// Multilevel-bm.pdf):
/// - Writing: ekspertlar ballarining o'rta arifmetigi (0–36) quyidagi
///   jadval bo'yicha 0–75 ga aylantiriladi; 1-qism — 33% (12 ball),
///   2-qism — 67% (24 ball);
/// - Speaking (2024-yildan yangi format): 5 + 5 + 5 + 6 = 21 ball (rasmiy
///   shkala), 75 ga o'tkazish e'lon qilinmagan — chiziqli;
/// - daraja: C1 65–75, B2 51–64, B1 38–50, B1 dan past 0–37.
/// </summary>
public static class CefrScale
{
    public const int Max = 75;
    public const int RawMax = 36;
    public const int WritingTask1Max = 12;
    public const int WritingTask2Max = 24;

    public static string Level(int score) => score switch
    {
        >= 65 => "C1",
        >= 51 => "B2",
        >= 38 => "B1",
        _ => "below B1",
    };

    // Rasmiy jadvalning 27.0 dan yuqori qismi (undan pastda — har 0.5 xom ball
    // uchun +1: 0.1–0.5 → 10, …, 26.6–27.0 → 63).
    private static readonly (decimal UpTo, int Score)[] Top =
    [
        (28.0m, 64), (29.0m, 65), (30.0m, 66), (30.5m, 67), (31.0m, 68), (31.5m, 69),
        (32.0m, 70), (32.5m, 71), (33.0m, 72), (34.0m, 73), (35.0m, 74), (36.0m, 75),
    ];

    /// <summary>Xom ball (0–36, o'nli bo'lishi mumkin) → 0–75 (rasmiy jadval).</summary>
    public static int Convert(decimal raw)
    {
        raw = Math.Clamp(raw, 0m, RawMax);
        if (raw == 0m) return 0;
        if (raw <= 27.0m) return 9 + (int)Math.Ceiling(raw / 0.5m);
        foreach (var (upTo, score) in Top)
        {
            if (raw <= upTo) return score;
        }
        return Max;
    }

    /// <summary>Speaking yangi format: 5 + 5 + 5 + 6 = 21 (rasmiy shkala).</summary>
    public static readonly int[] SpeakingMax = [5, 5, 5, 6];
    public const int SpeakingRawMax = 21;

    /// <summary>
    /// 0–21 → 0–75. O'tkazish jadvali e'lon qilinmagan — chiziqli olamiz.
    /// Rasmiy tavsiflarga mos: "Higher B2" (5+5+4+4=18) → 64 = B2,
    /// "Lower B1" darajasidagi javoblar (≈11) → 39 = B1.
    /// </summary>
    public static int SpeakingScore(IReadOnlyList<int> groups) =>
        (int)Math.Round(groups.Select((g, i) => Math.Clamp(g, 0, SpeakingMax[i])).Sum() * (decimal)Max / SpeakingRawMax, MidpointRounding.AwayFromZero);

    public static int WritingScore(int task1Raw12, int task2Raw24) =>
        Convert(Math.Clamp(task1Raw12, 0, WritingTask1Max) + Math.Clamp(task2Raw24, 0, WritingTask2Max));
}
