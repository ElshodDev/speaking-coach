using SpeakingCoach.Api.Services.Mock;
using static SpeakingCoach.Api.Services.Mock.CefrObjective;

namespace SpeakingCoach.Api.Services.Content;

// CEFR (Multilevel) Listening — 2–6-testlar. Original matnlar (mualliflik ishi),
// tuzilishi Listening1 bilan bir xil: 6 qism, 8 + 6 + 4 + 5 + 6 + 6 = 35 savol.
public static partial class BuiltInCefr
{
    public static readonly ListeningTest Listening2 = new([Cl2Part1(), Cl2Part2(), Cl2Part3(), Cl2Part4(), Cl2Part5(), Cl2Part6()]);
    public static readonly ListeningTest Listening3 = new([Cl3Part1(), Cl3Part2(), Cl3Part3(), Cl3Part4(), Cl3Part5(), Cl3Part6()]);
    public static readonly ListeningTest Listening4 = new([Cl4Part1(), Cl4Part2(), Cl4Part3(), Cl4Part4(), Cl4Part5(), Cl4Part6()]);
    public static readonly ListeningTest Listening5 = new([Cl5Part1(), Cl5Part2(), Cl5Part3(), Cl5Part4(), Cl5Part5(), Cl5Part6()]);
    public static readonly ListeningTest Listening6 = new([Cl6Part1(), Cl6Part2(), Cl6Part3(), Cl6Part4(), Cl6Part5(), Cl6Part6()]);

    // =====================================================================
    // TEST 2
    // =====================================================================

    private static ListeningPart Cl2Part1() => new(1,
        "You will hear eight short sentences said in a restaurant, an office, a shop and other everyday places. Choose the best reply to each one.",
        [
            Female("Waiter", "Are you ready to order, or would you like a few more minutes?"),
            Male("Colleague", "Sorry, is anyone sitting here?"),
            Female("Shop assistant", "Would you like me to wrap it as a present?"),
            Male("Friend", "You look exhausted. Didn't you sleep well last night?"),
            Female("Teacher", "Make sure you hand in your essays by Friday at the latest."),
            Male("Caller", "I'm calling about the flat you advertised. Is it still available?"),
            Female("Neighbour", "Would you mind keeping an eye on my cat while I'm away?"),
            Male("Colleague", "To be honest, I'm not sure the new manager's plan will work."),
        ],
        [
            new("mcq", L1, null,
            [
                Choice(1, "Choose the correct reply.", "B", "\"Would you like a few more minutes?\" is answered by asking for more time: \"Could we have a couple more minutes?\"",
                    "Yes, I ordered it yesterday.", "Could we have a couple more minutes, please?", "It was delicious, thank you."),
                Choice(2, "Choose the correct reply.", "A", "\"Is anyone sitting here?\" asks if the seat is free: \"No, go ahead — it's free\".",
                    "No, go ahead — it's free.", "Yes, I'm sitting very well.", "I usually sit by the window."),
                Choice(3, "Choose the correct reply.", "C", "\"Would you like me to wrap it as a present?\" is an offer, accepted with \"Yes, please\" and a reason.",
                    "It was a present from my aunt.", "I'd rather pay by card.", "Yes, please — it's for my sister's birthday."),
                Choice(4, "Choose the correct reply.", "C", "\"Didn't you sleep well last night?\" asks about the past night; \"Not really — the neighbours were having a party\" explains why.",
                    "Yes, I look after myself well.", "I'm going to bed at nine tomorrow.", "Not really — the neighbours were having a party."),
                Choice(5, "Choose the correct reply.", "B", "\"Hand in your essays by Friday\" is an instruction; asking \"Is it all right if I email mine instead?\" responds to it.",
                    "I handed it to my friend.", "Is it all right if I email mine instead?", "Friday was a lovely day."),
                Choice(6, "Choose the correct reply.", "B", "\"Is it still available?\" asks about the flat; \"I'm afraid it was rented out this morning\" answers it.",
                    "Yes, I've been calling you all day.", "I'm afraid it was rented out this morning.", "It's available in three colours."),
                Choice(7, "Choose the correct reply.", "C", "\"Would you mind ...?\" is a request, accepted with \"Not at all\" and a practical question.",
                    "I haven't seen your cat, sorry.", "Yes, I mind cats a lot.", "Not at all — how long will you be gone?"),
                Choice(8, "Choose the correct reply.", "A", "\"I'm not sure the plan will work\" expresses doubt; \"Neither am I\" agrees with it.",
                    "Neither am I, but let's give it a chance.", "The manager works on the fifth floor.", "Honestly, I planned it all last year."),
            ]),
        ]);

    private static ListeningPart Cl2Part2()
    {
        const string who = "Museum director";
        return new(2,
            "You will hear the director of a new history museum speaking at its official opening.",
            [
                Male(who, "Good morning, ladies and gentlemen, and thank you all for coming to the opening of the Zarafshan Regional History Museum. My name is Bobur Tursunov, and I'm the director. I know you're all keen to see the galleries, so I'll keep this brief."),
                Male(who, "Some of you may be wondering about this beautiful building. Many people assume it was once a palace or a school, but in fact it was originally a railway station. It was completed in 1912 and closed about forty years ago, and since then it had been standing empty, so we're delighted to have given it a new life."),
                Male(who, "The galleries are arranged in chronological order, so you start on the ground floor with archaeology. Everyone expects the pottery to be the highlight there, and it is lovely, but the objects I'd really encourage you not to miss are the ancient coins, which were discovered by farmers digging a well in the 1960s. They tell us a great deal about the trade that passed through this valley."),
                Male(who, "Now some practical details. We're open from Tuesday to Sunday, from nine in the morning until six in the evening. We're closed on Mondays, when our staff clean the galleries and look after the collection."),
                Male(who, "Tickets for adults are thirty thousand som, and students and pensioners pay half price. Children under seven don't need a ticket at all. We did consider free entry for all school pupils, but unfortunately that wasn't possible this year."),
                Male(who, "If you'd like to know more as you walk round, pick up an audio guide from the desk by the entrance. It's included in the price of your ticket, and it's available in five languages: Uzbek, Russian, English, French and Chinese. We hope to add Korean and German next year."),
                Male(who, "And finally, when you've finished, do visit our café. It isn't on the ground floor, as in most museums, but up on the roof, where you'll have a wonderful view across the old city. Right, the doors are open. Please enjoy the museum!"),
            ],
            [
                new("gap", L2, 2,
                [
                    Item(9, "The museum building was originally a ___ station.", "\"In fact it was originally a railway station.\"", "railway"),
                    Item(10, "In the archaeology gallery, the director especially recommends the ancient ___.", "\"The objects I'd really encourage you not to miss are the ancient coins.\"", "coins"),
                    Item(11, "The museum is closed on ___.", "\"We're closed on Mondays.\"", "Mondays", "Monday"),
                    Item(12, "Children under ___ can enter free of charge.", "\"Children under seven don't need a ticket at all.\"", "seven", "7"),
                    Item(13, "The audio guide is available in ___ languages.", "\"It's available in five languages.\"", "five", "5"),
                    Item(14, "The museum café is on the ___.", "\"It isn't on the ground floor ... but up on the roof.\"", "roof"),
                ]),
            ]);
    }

    private static ListeningPart Cl2Part3() => new(3,
        "You will hear four people talking about their experience of working from home.",
        [
            Male("Speaker 1", "When my company first let us work from home, I thought it would be perfect: no more traffic and more time with the kids. The reality was rather different. I set up my laptop on the kitchen table, so every time my children came in for a snack, they'd start telling me about their day in the middle of a video call. And because the office was always there, I never really stopped. I'd find myself answering emails at ten at night while my wife watched TV alone. In the end I turned our small storeroom into an office with a door I can close."),
            Female("Speaker 2", "People often ask me whether I get more done at home. Honestly, I don't think my work is any faster or slower than before; the tasks are exactly the same. What's changed is the atmosphere. In the office there was always someone to have a quick coffee with, to laugh about a difficult client with, or just to say good morning to. Now my only conversations are on video, and they're always about work. I didn't realise how much those little chats meant to me until they disappeared."),
            Male("Speaker 3", "I asked to work from home two days a week long before it became common. My manager wasn't at all keen on the idea. She said she needed to see people at their desks to know they were actually working. In the end she agreed, but only if I sent her a detailed list every afternoon of everything I'd done that day. It felt a bit like being back at school. After about six months, though, she saw that my results hadn't changed, and she quietly stopped asking for the lists."),
            Female("Speaker 4", "For me, the biggest difference has been financial. I used to drive forty minutes each way, and between petrol and parking in the city centre, I was spending a fortune. Then there were the lunches in the café opposite the office, because there was nowhere else to eat. Now I make my own lunch, and my car stays in the garage most of the week. Some of my friends still go into their offices a couple of days a week, but our company closed its office completely last year, so that isn't an option for us."),
        ],
        [
            new("match", L3, null,
            [
                Item(15, "Speaker 1", "Speaker 1: \"because the office was always there, I never really stopped ... answering emails at ten at night\".", "C"),
                Item(16, "Speaker 2", "Speaker 2: \"I didn't realise how much those little chats meant to me until they disappeared\".", "A"),
                Item(17, "Speaker 3", "Speaker 3: \"She said she needed to see people at their desks to know they were actually working\".", "F"),
                Item(18, "Speaker 4", "Speaker 4: \"between petrol and parking ... I was spending a fortune ... Now I make my own lunch\".", "D"),
            ],
            [
                "I miss the informal contact with colleagues.",
                "I get more work done at home than in the office.",
                "I found it hard to keep work separate from family life.",
                "I spend much less money than before.",
                "I now work in the office for part of the week.",
                "My manager did not trust me at first.",
            ]),
        ]);

    private static ListeningPart Cl2Part4()
    {
        const string who = "Student guide";
        var map = new MapPlan("University campus",
            [new("Main gate", 2, 4), new("Library", 2, 2), new("Car park", 0, 4)],
            [
                new("A", 1, 3), new("B", 3, 3), new("C", 0, 1), new("D", 4, 2),
                new("E", 1, 0), new("F", 3, 0), new("G", 4, 4), new("H", 0, 2),
            ]);
        return new(4,
            "You will hear a student guide describing the university campus to new students standing at the main gate.",
            [
                Female(who, "Hi, everyone, and welcome to the campus. I'm Shahlo, and I'm a second-year student here. We're standing at the main gate, which is in the middle of the southern side of the campus, so if you look straight up the main path, you're facing north. Over to the left, in the south-west corner, is the car park, which you'll use if you drive."),
                Female(who, "Let's start with the buildings near the gate. As you walk up the main path towards the library, the first building on your right is the student centre. That's where you'll find the students' union, a canteen and the careers office. Directly opposite it, on the left-hand side of the path, is the university bookshop, which also sells stationery."),
                Female(who, "The large building in the very centre of the campus is the library. It's open until midnight during exams."),
                Female(who, "When you reach the library, if you turn left and follow the path all the way to the western edge of the campus, you'll come to the sports hall. It's right at the end of that path, level with the library. Just north of the sports hall there's a swimming pool, which students can use free of charge."),
                Female(who, "If you turn right at the library instead, that path takes you to the eastern edge of the campus, where you'll find the medical centre, again level with the library. You'll need to register there during your first week."),
                Female(who, "Behind the library, along the northern side of the campus, there are two teaching buildings. The one on the left as you face north, so north-west of the library, is the arts building. The one on the right, north-east of the library, is the science building, where those of you studying chemistry and biology will have most of your lectures."),
                Female(who, "Finally, if you lose anything or have a problem at night, go to the security office. It's in the south-east corner of the campus, at the opposite end of the southern fence from the car park. Right, any questions before we go inside?"),
            ],
            [
                new("map", L4, null,
                [
                    Item(19, "Student centre", "\"As you walk up the main path towards the library, the first building on your right is the student centre.\"", "B"),
                    Item(20, "Sports hall", "\"Turn left and follow the path all the way to the western edge ... level with the library.\"", "H"),
                    Item(21, "Medical centre", "\"If you turn right at the library ... the eastern edge ... again level with the library.\"", "D"),
                    Item(22, "Science building", "\"The one on the right, north-east of the library, is the science building.\"", "F"),
                    Item(23, "Security office", "\"It's in the south-east corner of the campus, at the opposite end of the southern fence from the car park.\"", "G"),
                ], Map: map),
            ]);
    }

    private static ListeningPart Cl2Part5() => new(5,
        "You will hear three extracts about food and healthy eating.",
        [
            Male("Narrator", "Extract One."),
            Male("Jasur", "That looks good, Nigora. Have you stopped eating in the canteen?"),
            Female("Nigora", "I have. I cook everything on Sunday afternoon and bring lunch in a box every day. Everyone thinks I'm doing it to save money, and yes, that's a nice bonus, but that's not really why. After those heavy canteen lunches I used to feel so sleepy that I could hardly keep my eyes open in afternoon meetings. Since I started bringing salads and soup, I've had much more energy."),
            Male("Jasur", "I've thought about doing the same. Finding the time on a Sunday wouldn't be the issue. But eating the same meal five days in a row? I'd be sick of it by Wednesday."),
            Female("Nigora", "You don't have to, though. I make two or three different dishes and swap them around. Why don't you try it for a week?"),
            Male("Jasur", "Maybe. But I'm not giving up the canteen's plov on Fridays!"),
            Male("Narrator", "Extract Two."),
            Female("Presenter", "Dr Aliev, you've said that many families are eating more sugar than they realise. How can that be?"),
            Male("Dr Aliev", "Most people know that sweets and cakes contain sugar, so they're careful with those. The problem is the things that look healthy. A glass of fruit juice from the supermarket can contain as much sugar as a fizzy drink, and many breakfast cereals and yoghurts aimed at children are full of it. Parents buy them with the best intentions."),
            Female("Presenter", "So should families stop buying juice altogether?"),
            Male("Dr Aliev", "I'd never tell people that, because sudden bans rarely work, especially with children. What I suggest is mixing juice with water: half and half at first, then a little more water each week. After a month or two, most children don't even notice the difference. And of course, eating a whole piece of fruit is always better than drinking it."),
            Male("Narrator", "Extract Three."),
            Male("Student", "When I moved to Tashkent to start university, I'd never cooked anything more complicated than an egg. For the whole first term I lived on burgers, instant noodles and whatever the kiosk near our building was selling. I knew it wasn't healthy, of course, but that's not what made me change. At the end of the term I sat down and added up what I'd spent on food, and I couldn't believe the number. It was nearly twice what my parents had given me for food. So I phoned my mother and asked her to teach me a few simple dishes over video calls. Now the three of us who share the flat take turns to cook, and on Friday evenings we cook a big meal together and invite friends round. Honestly, those evenings have become the best part of my week. I'm not a great cook yet, but I've discovered it's much more fun when you're not doing it alone."),
        ],
        [
            new("mcq", L5, null,
            [
                Choice(24, "Why did Nigora start bringing her own lunch?", "B", "\"After those heavy canteen lunches I used to feel so sleepy ... that's not really why\" she saves money.",
                    "to spend less money", "because canteen meals made her feel tired", "because a colleague recommended it"),
                Choice(25, "What is Jasur's main doubt about Nigora's idea?", "C", "\"Finding the time ... wouldn't be the issue. But eating the same meal five days in a row? I'd be sick of it.\"",
                    "He does not have time to cook at the weekend.", "He thinks home-cooked food is less healthy.", "He would get bored of eating the same food."),
                Choice(26, "What is Dr Aliev's main point?", "B", "\"The problem is the things that look healthy. A glass of fruit juice ... can contain as much sugar as a fizzy drink.\"",
                    "Parents should not give children any sweets.", "Some foods that seem healthy contain a lot of sugar.", "Fizzy drinks are more harmful than fruit juice."),
                Choice(27, "What does Dr Aliev recommend?", "A", "\"Mixing juice with water: half and half at first, then a little more water each week.\"",
                    "reducing the amount of juice step by step", "buying juice only from supermarkets", "stopping juice completely at once"),
                Choice(28, "What made the speaker change his eating habits?", "B", "\"That's not what made me change ... I added up what I'd spent on food, and I couldn't believe the number.\"",
                    "He started to feel unwell.", "He realised how much he was spending.", "His mother visited and was worried."),
                Choice(29, "How does the speaker feel about cooking now?", "A", "\"Those evenings have become the best part of my week ... much more fun when you're not doing it alone.\"",
                    "He enjoys it most as a shared activity.", "He finds it relaxing to cook alone.", "He still sees it as a boring duty."),
            ]),
        ]);

    private static ListeningPart Cl2Part6()
    {
        const string who = "Lecturer";
        return new(6,
            "You will hear part of a university lecture about caravanserais on the Silk Road.",
            [
                Male(who, "Good morning, everyone. In our last session we looked at the goods that were traded along the Silk Road. Today I want to turn to the places where the traders stopped to rest: the caravanserais. The word itself comes from Persian. A caravan is a group of travellers moving together, and a serai is a palace or a large building. So, quite literally, a caravanserai was a palace for caravans, although, as you'll see, most of them were far from luxurious."),
                Male(who, "Travelling across Central Asia a thousand years ago was slow and dangerous. A caravan of camels could cover perhaps thirty to forty kilometres a day, and for that reason caravanserais along the main routes were usually built about a day's journey apart. At the end of each day, travellers knew there would be a safe place to sleep, water for their animals and protection from bandits."),
                Male(who, "The design of a typical caravanserai was remarkably similar from Anatolia to China. It was a square or rectangular building with thick outer walls and very few windows. There was usually only one large gateway, high enough for a loaded camel to pass through, and it was locked at sunset, which made the building much easier to defend. Inside, the rooms were arranged around a central open courtyard. The animals were kept in stables on the ground floor, while the merchants slept and stored their most valuable goods in the rooms above or beside them."),
                Male(who, "Water, of course, was essential, and in the dry steppe it was not always easy to find. Some caravanserais were built next to rivers or wells, but others depended on stored water. A good example from Uzbekistan is Rabati Malik, built in the eleventh century on the road between Samarkand and Bukhara, near the modern city of Navoi. Only the grand entrance of the caravanserai survives today, but nearby there is a sardoba, a large underground reservoir covered by a brick dome. The dome protected the water from the sun and kept it cool, even in the hottest months of the year."),
                Male(who, "Who paid for these buildings? In many cases they were built by rulers or wealthy citizens, partly to encourage trade and partly as an act of charity. In some caravanserais, travellers could stay for a few days without paying anything at all."),
                Male(who, "It would be a mistake, however, to see caravanserais simply as hotels. They were also places where people from very different cultures met, often for the first time. Merchants from China, Persia, India and Europe spent their evenings together in the same courtyard. They exchanged not only goods but also ideas: news from distant cities, new techniques, medical knowledge, stories and songs. Many historians believe that caravanserais helped to spread inventions such as papermaking across Asia."),
                Male(who, "So why did they decline? From the sixteenth century onwards, European ships began to carry more and more goods between Asia and Europe. Trade by sea was cheaper and could carry much larger loads, and gradually the overland routes lost their importance. Many caravanserais fell into ruin, although some have recently been restored and are now museums, restaurants or, fittingly, hotels. Next week we'll look at the cities that grew up along these trade routes."),
            ],
            [
                new("gap", L6, 1,
                [
                    Item(30, "Caravanserais on the main routes were usually one day's ___ apart.", "\"Caravanserais along the main routes were usually built about a day's journey apart.\"", "journey"),
                    Item(31, "Most caravanserais had only one large ___, which was locked at sunset.", "\"There was usually only one large gateway ... and it was locked at sunset.\"", "gateway", "gate"),
                    Item(32, "Inside, the rooms were arranged around a central open ___.", "\"The rooms were arranged around a central open courtyard.\"", "courtyard"),
                    Item(33, "The dome of the sardoba kept the water ___ in the hottest months.", "\"The dome protected the water from the sun and kept it cool.\"", "cool"),
                    Item(34, "In caravanserais, merchants exchanged ___ as well as goods.", "\"They exchanged not only goods but also ideas.\"", "ideas"),
                    Item(35, "Caravanserais declined when more goods were transported by ___.", "\"Trade by sea was cheaper and could carry much larger loads.\"", "sea", "ship"),
                ]),
            ]);
    }

    // =====================================================================
    // TEST 3
    // =====================================================================

    private static ListeningPart Cl3Part1() => new(1,
        "You will hear eight short sentences said at a pharmacy, a hotel, a school and other everyday places. Choose the best reply to each one.",
        [
            Female("Pharmacist", "Take one of these tablets three times a day, after meals."),
            Male("Friend", "What do you say we go to the cinema this weekend?"),
            Male("Hotel receptionist", "I'm sorry, but we don't have any single rooms left for tonight."),
            Female("Classmate", "Did you manage to finish the maths homework?"),
            Male("Man in the street", "Excuse me, is there a cash machine anywhere near here?"),
            Female("Manager", "Could you let me have the report before lunch tomorrow?"),
            Male("Friend", "I've just failed my driving test for the second time."),
            Female("Guest", "Thank you so much for dinner. Everything was delicious."),
        ],
        [
            new("mcq", L1, null,
            [
                Choice(1, "Choose the correct reply.", "B", "The pharmacist gives instructions; a natural reply thanks her and asks a related question.",
                    "I'd like a bigger bag, please.", "Thank you. Is there anything I should avoid while I'm taking them?", "I took them last week."),
                Choice(2, "Choose the correct reply.", "A", "\"What do you say we go ...?\" is a suggestion, accepted with \"Good idea — what's on?\"",
                    "Good idea — what's on?", "I didn't say anything.", "I went there with my brother."),
                Choice(3, "Choose the correct reply.", "C", "No single rooms are left, so the guest asks for another kind of room: \"Do you have a double room instead?\"",
                    "That's fine, I'm single.", "Tonight I'm leaving early.", "Oh no. Do you have a double room instead?"),
                Choice(4, "Choose the correct reply.", "B", "\"Did you manage to finish ...?\" asks about success; \"Only just\" answers it.",
                    "Maths is my favourite subject.", "Only just — the last question took me ages.", "I'll manage the shop tomorrow."),
                Choice(5, "Choose the correct reply.", "B", "The man asks where a cash machine is; the reply gives a place: \"inside the supermarket on the corner\".",
                    "I haven't got any change, sorry.", "There's one inside the supermarket on the corner.", "It's very near my home."),
                Choice(6, "Choose the correct reply.", "C", "The manager wants the report \"before lunch tomorrow\"; \"I'll send it first thing in the morning\" agrees.",
                    "I had lunch at one.", "I let it go yesterday.", "Sure, I'll send it first thing in the morning."),
                Choice(7, "Choose the correct reply.", "A", "Failing a test is bad news, so sympathy fits: \"Oh, bad luck!\"",
                    "Oh, bad luck! Third time lucky, maybe?", "Congratulations, you must be so pleased!", "I drive to work every day."),
                Choice(8, "Choose the correct reply.", "B", "\"Thank you so much for dinner\" is answered with \"You're welcome\".",
                    "Dinner is at seven.", "You're welcome. I'm so glad you enjoyed it.", "Yes, I thank you too for cooking."),
            ]),
        ]);

    private static ListeningPart Cl3Part2()
    {
        const string who = "Membership manager";
        return new(2,
            "You will hear the membership manager of a sports centre talking to people who are interested in joining.",
            [
                Female(who, "Good evening, and thanks for coming along to our open evening here at the Riverside Sports Centre. I'm Gulnora, and I look after memberships, so I'll quickly go through the main things you need to know."),
                Female(who, "Let's start with opening hours. On weekdays we open at six in the morning, which is popular with people who like to train before work, and we close at ten at night. At weekends we open a little later, at eight."),
                Female(who, "There are two types of membership. Standard membership gives you the gym and all our fitness classes, and Premium adds the swimming pool and the sauna. You can pay monthly, but if you pay for a whole year in advance, you get two months free, which is a very good deal."),
                Female(who, "Students get twenty per cent off any membership. You don't need a letter from your university; just show a valid student card when you join."),
                Female(who, "Every new member is also entitled to one free session with a personal trainer. They'll check your fitness level and design a programme that suits your goals. After that, extra sessions are available, but you'll have to pay for them."),
                Female(who, "A quick word about the pool. It used to close on Monday mornings for cleaning, but that's changed. From this month, it's closed every Wednesday morning until twelve, so please plan your swims around that."),
                Female(who, "And finally, one thing that surprises some people: to reduce our laundry and water use, we no longer provide towels, so please remember to bring your own. Lockers are free, but you'll need a coin. Right, if you'd like to sign up tonight, the forms are on the table by the door."),
            ],
            [
                new("gap", L2, 2,
                [
                    Item(9, "On weekdays the centre opens at ___ a.m.", "\"On weekdays we open at six in the morning.\"", "six", "6"),
                    Item(10, "Members who pay for a whole ___ in advance get two months free.", "\"If you pay for a whole year in advance, you get two months free.\"", "year"),
                    Item(11, "To get the student discount, you need to show a valid ___.", "\"Just show a valid student card when you join.\"", "student card", "card"),
                    Item(12, "New members get one free session with a personal ___.", "\"One free session with a personal trainer.\"", "trainer"),
                    Item(13, "The pool is now closed every ___ morning.", "\"From this month, it's closed every Wednesday morning.\"", "Wednesday"),
                    Item(14, "Members must bring their own ___.", "\"We no longer provide towels, so please remember to bring your own.\"", "towels", "towel"),
                ]),
            ]);
    }

    private static ListeningPart Cl3Part3() => new(3,
        "You will hear four people talking about their experience of volunteering.",
        [
            Male("Speaker 1", "I joined an environmental charity last spring because I wanted to do something practical for nature. I imagined myself planting trees and cleaning up rivers every weekend. Instead, for the first two months, they had me sitting in their small office, entering data into spreadsheets and answering the phone. I nearly gave up, to be honest. Then in the summer they finally sent me out with a team to plant trees along a motorway, and it was everything I'd hoped for. Now I understand that the office work matters too, but it wasn't a great start."),
            Female("Speaker 2", "I've always been quite shy. At university I hardly ever spoke in seminars. A friend suggested that I volunteer at the city library, reading stories to young children on Saturday mornings. I actually wanted to help at the animal shelter, but I'm allergic to cats, so the library it was. The first time, my voice was shaking, but children are such an honest audience: if they're bored, they tell you! Gradually I learned to use different voices and ask them questions. Now I'm the one who puts her hand up first in seminars."),
            Male("Speaker 3", "I'm hoping to train as a nurse next year, and everyone told me that universities like to see some experience of caring for people. So I started volunteering at a care home for elderly people two evenings a week. I help to serve dinner, and sometimes I just sit and chat or play chess with the residents. I'll admit that at first I was mainly thinking about my application, but I've grown really fond of some of them. One old gentleman has beaten me at chess every single week."),
            Female("Speaker 4", "My grandfather has been a volunteer coach at our local football club for over thirty years. When I was little, I used to go along with him every Saturday and watch the training sessions. As soon as I was old enough, I started helping him with the youngest teams, and now I run a girls' team myself. My sister does her volunteering online; she translates documents for a charity from her bedroom. But I need to be outside with people. I suppose it runs in the family."),
        ],
        [
            new("match", L3, null,
            [
                Item(15, "Speaker 1", "Speaker 1: \"Instead, for the first two months, they had me ... entering data into spreadsheets ... it wasn't a great start\".", "E"),
                Item(16, "Speaker 2", "Speaker 2: \"I've always been quite shy ... Now I'm the one who puts her hand up first\".", "C"),
                Item(17, "Speaker 3", "Speaker 3: \"I'm hoping to train as a nurse ... universities like to see some experience of caring\".", "A"),
                Item(18, "Speaker 4", "Speaker 4: \"My grandfather has been a volunteer coach ... I used to go along with him\".", "B"),
            ],
            [
                "I wanted experience that would help my future career.",
                "I followed the example of a relative.",
                "Volunteering made me more confident.",
                "I work with animals.",
                "My first tasks were not what I had hoped for.",
                "I can volunteer without leaving my home.",
            ]),
        ]);

    private static ListeningPart Cl3Part4()
    {
        const string who = "Zoo guide";
        var map = new MapPlan("Chinor City Zoo",
            [new("Entrance", 2, 4), new("Gift shop", 1, 4), new("Fountain", 2, 2)],
            [
                new("A", 3, 4), new("B", 3, 3), new("C", 0, 0), new("D", 2, 0),
                new("E", 4, 2), new("F", 0, 2), new("G", 1, 1), new("H", 4, 0),
            ]);
        return new(4,
            "You will hear a guide describing the layout of a city zoo to visitors at the entrance.",
            [
                Male(who, "Good morning, and welcome to Chinor City Zoo. We're standing just inside the main entrance, which is in the middle of the southern fence. The path in front of you leads straight up to the fountain in the centre of the zoo, so you're facing north. Just to your left is the gift shop, if you'd like to buy a souvenir."),
                Male(who, "Immediately to your right, along the southern fence, is the penguin pool. Feeding time there is at eleven, and it's very popular, so get there early."),
                Male(who, "As you walk up the main path towards the fountain, you'll see the butterfly house on your right, just before you reach the fountain. It's warm and humid inside, so you might want to take your coat off."),
                Male(who, "When you get to the fountain, turn left and follow the path all the way to the western fence. The reptile house is right there, level with the fountain. It's home to our snakes, lizards and tortoises."),
                Male(who, "Walking back from the reptile house, you'll notice Monkey Island, which sits between the fountain and the north-west corner of the zoo. The monkeys are fed at two o'clock. Our elephants live in the largest enclosure of all, which fills the north-west corner itself."),
                Male(who, "If you carry straight on past the fountain to the very end of the main path, you'll reach the lions, in the middle of the northern side. Please don't bang on the glass; they sleep for most of the day."),
                Male(who, "Now, if you turn right at the fountain instead, the path takes you to the eastern fence, and that's where you'll find the giraffes, directly level with the fountain. There's a platform where you can see them at eye level."),
                Male(who, "And finally, for younger children, there's the children's farm, where they can feed goats and rabbits. It's in the north-east corner, just north of the giraffes. Enjoy your day!"),
            ],
            [
                new("map", L4, null,
                [
                    Item(19, "Butterfly house", "\"You'll see the butterfly house on your right, just before you reach the fountain.\"", "B"),
                    Item(20, "Reptile house", "\"Turn left and follow the path all the way to the western fence ... level with the fountain.\"", "F"),
                    Item(21, "Elephants", "\"The largest enclosure of all, which fills the north-west corner itself.\"", "C"),
                    Item(22, "Giraffes", "\"Turn right at the fountain ... the eastern fence ... directly level with the fountain.\"", "E"),
                    Item(23, "Children's farm", "\"It's in the north-east corner, just north of the giraffes.\"", "H"),
                ], Map: map),
            ]);
    }

    private static ListeningPart Cl3Part5() => new(5,
        "You will hear three extracts about travel experiences.",
        [
            Male("Narrator", "Extract One."),
            Male("Otabek", "Welcome back, Sevara! How was Istanbul?"),
            Female("Sevara", "Wonderful in the end, but the start was a disaster. The flight itself was fine, on time actually, but when I got to the baggage hall, my suitcase just never appeared. The airline found it two days later, in a completely different city."),
            Male("Otabek", "Oh no. What did you do for clothes?"),
            Female("Sevara", "I had to buy everything, even a toothbrush. The airline paid me back in the end, so I can't really complain about that. But I've learned my lesson. From now on I'm always going to pack a change of clothes and anything important in my hand luggage."),
            Male("Otabek", "That's what my mother always does. I used to laugh at her."),
            Female("Sevara", "Well, she's right. Anyway, once my suitcase arrived, I had the best week."),
            Male("Narrator", "Extract Two."),
            Male("Presenter", "Dilfuza, you've visited over forty countries, but you say you now travel to fewer places. Why is that?"),
            Female("Dilfuza", "When I started, I used to rush from one city to the next, trying to see as much as possible. I came home with thousands of photos, but I'd hardly spoken to anyone who actually lived there. Now I'll spend a whole month in one town. I rent a flat, shop at the local market, and take a cooking class or a language course. It isn't really about saving money, although it is often cheaper. It's about understanding how people live."),
            Male("Presenter", "And what about all those beautiful travel photos we see online?"),
            Female("Dilfuza", "I post them too, so I can't criticise too much! But I do worry that they give people a false picture. You see a quiet beach at sunset, but you don't see the three hundred tourists standing just behind the photographer. People arrive and feel disappointed."),
            Male("Narrator", "Extract Three."),
            Male("Hiker", "Two summers ago, a friend and I decided to climb one of the peaks near Chimgan. We'd done plenty of walks in the area before, and we'd even told the owner of our guesthouse exactly which route we were taking and when we expected to be back. We had good boots, plenty of water and a map. What we didn't do was look at the weather forecast. By midday the sky had turned dark, the temperature dropped by about fifteen degrees, and we were caught in a hailstorm near the top, wearing T-shirts. We got down safely, but we were frozen and very frightened. I haven't stopped going to the mountains. In fact, I go more often than ever. But these days I check the forecast the night before and again in the morning, and there's always a warm jacket in my rucksack, even in August."),
        ],
        [
            new("mcq", L5, null,
            [
                Choice(24, "What problem did Sevara have on her trip?", "B", "\"The flight itself was fine ... my suitcase just never appeared.\"",
                    "Her flight was delayed.", "Her luggage did not arrive with her.", "Her hotel had no record of her booking."),
                Choice(25, "What does Sevara say about the experience?", "C", "\"I've learned my lesson ... pack a change of clothes and anything important in my hand luggage.\"",
                    "She will ask the airline for more money.", "She will not fly with that airline again.", "It taught her how to pack in future."),
                Choice(26, "Why does Dilfuza now stay longer in one place?", "B", "\"It isn't really about saving money ... It's about understanding how people live.\"",
                    "to spend less money on travel", "to learn about local people's lives", "to take better photographs"),
                Choice(27, "What is Dilfuza's opinion of travel photos online?", "A", "\"I do worry that they give people a false picture ... People arrive and feel disappointed.\"",
                    "They can create unrealistic expectations.", "They are a good way to plan a trip.", "Travel writers should stop posting them."),
                Choice(28, "What was the speaker's main mistake?", "C", "\"What we didn't do was look at the weather forecast.\"",
                    "He did not tell anyone about his plans.", "He chose a route that was too difficult.", "He did not check what the weather would be like."),
                Choice(29, "How has the experience affected him?", "A", "\"These days I check the forecast the night before and again in the morning, and there's always a warm jacket in my rucksack.\"",
                    "He now prepares more carefully for his walks.", "He goes to the mountains less often than before.", "He now prefers to walk alone."),
            ]),
        ]);

    private static ListeningPart Cl3Part6()
    {
        const string who = "Lecturer";
        return new(6,
            "You will hear part of a university lecture about how electric cars work.",
            [
                Female(who, "Good afternoon, everyone. Electric cars are now a common sight on our roads, but many people who drive them have only a vague idea of what's happening under the bonnet. So today I'd like to explain, in fairly simple terms, how an electric car actually works, and why it is so different from a car with a petrol engine."),
                Female(who, "Let's begin with the heart of the vehicle: the battery. In most modern electric cars this is a large, flat pack that sits under the floor, between the wheels. Placing it low down gives the car a low centre of gravity, which makes it more stable on corners. The pack isn't one single battery, however. It's made up of thousands of small cells, very similar to the ones in your phone or laptop, connected together. Most of them are lithium-ion cells, which can store a lot of energy for their weight."),
                Female(who, "Now, the battery stores electricity as direct current, or DC, but most electric car motors run on alternating current, or AC. So between the battery and the motor there's a device called an inverter, which converts one into the other. The inverter also controls how much power reaches the motor. When you press the accelerator, you aren't letting more fuel into an engine, as in a petrol car. You're simply telling the inverter to send more current."),
                Female(who, "The motor itself is surprisingly simple. It uses magnetism to turn a shaft, which then turns the wheels. And it's extremely efficient. An electric motor converts about ninety per cent of the energy it receives into movement, whereas a petrol engine typically manages only around a quarter to a third; the rest is lost, mostly as heat. That's one reason why electric cars are cheaper to run."),
                Female(who, "Another big difference is the number of moving parts. A petrol engine has hundreds of them. An electric motor has very few, and because it can produce strong power at almost any speed, most electric cars don't need a gearbox at all. There's no oil to change, and far less to go wrong."),
                Female(who, "One of the cleverest features is what happens when you slow down. In a normal car, the brakes turn the car's movement into heat, which is simply wasted. In an electric car, when you lift your foot off the accelerator, the motor starts to work in reverse, as a generator. It slows the car down and, at the same time, sends electricity back into the battery. This is called regenerative braking, and in city driving it can noticeably increase the car's range."),
                Female(who, "Finally, a word about looking after the battery. Batteries don't like extremes. Very cold weather reduces their range, and too much heat, for example from repeated fast charging, can shorten their life. That's why modern electric cars have their own heating and cooling systems for the battery, which try to keep it at a moderate temperature. In that sense, batteries are rather like people: they're happiest when they're neither too hot nor too cold. Next time, we'll look at charging networks and what they mean for our cities."),
            ],
            [
                new("gap", L6, 1,
                [
                    Item(30, "The battery pack is made up of thousands of small ___.", "\"It's made up of thousands of small cells.\"", "cells"),
                    Item(31, "A device called an ___ changes direct current into alternating current.", "\"There's a device called an inverter, which converts one into the other.\"", "inverter"),
                    Item(32, "An electric motor turns about ___ per cent of its energy into movement.", "\"An electric motor converts about ninety per cent of the energy it receives into movement.\"", "ninety", "90"),
                    Item(33, "Most electric cars do not need a ___.", "\"Most electric cars don't need a gearbox at all.\"", "gearbox"),
                    Item(34, "When the driver slows down, the motor works as a ___.", "\"The motor starts to work in reverse, as a generator.\"", "generator"),
                    Item(35, "Heating and cooling systems keep the battery at a moderate ___.", "\"Which try to keep it at a moderate temperature.\"", "temperature"),
                ]),
            ]);
    }

    // =====================================================================
    // TEST 4
    // =====================================================================

    private static ListeningPart Cl4Part1() => new(1,
        "You will hear eight short sentences said at a bus stop, a library, a restaurant and other everyday places. Choose the best reply to each one.",
        [
            Male("Bus driver", "I'm afraid this bus doesn't go to the airport."),
            Female("Friend", "Have you heard that Kamola is moving to Canada?"),
            Female("Librarian", "This book is overdue. It should have been returned last week."),
            Male("Colleague", "Shall I open the window? It's rather stuffy in here."),
            Male("Waiter", "I'm afraid we've run out of the fish today."),
            Female("Friend", "I've been offered places at two universities, and I can't decide."),
            Male("Stranger in a café", "Do you mind if I take this chair?"),
            Female("Teacher", "Your essay was excellent, but you need to work on your spelling."),
        ],
        [
            new("mcq", L1, null,
            [
                Choice(1, "Choose the correct reply.", "A", "The bus doesn't go to the airport, so the passenger asks which bus to take instead.",
                    "Oh — which one should I take, then?", "I've never been to the airport.", "The airport is very big."),
                Choice(2, "Choose the correct reply.", "C", "\"Have you heard ...?\" introduces news; \"Really? No — when did she decide that?\" reacts to it.",
                    "Yes, I moved last year.", "Canada is a cold country.", "Really? No — when did she decide that?"),
                Choice(3, "Choose the correct reply.", "B", "An overdue book usually means a fine, so the reader apologises and asks \"how much do I owe?\"",
                    "I'll read it again next week.", "I'm so sorry — how much do I owe?", "Yes, it's a really good book."),
                Choice(4, "Choose the correct reply.", "A", "\"Shall I open the window?\" is an offer, accepted with \"Please do\".",
                    "Please do — I was thinking the same.", "Yes, I'll have some more.", "No, it's full of papers."),
                Choice(5, "Choose the correct reply.", "C", "\"We've run out of the fish\" means it isn't available, so the customer asks for another suggestion.",
                    "Don't run, the floor is wet.", "I ate fish yesterday too.", "Oh, that's a pity. What would you recommend instead?"),
                Choice(6, "Choose the correct reply.", "A", "The friend \"can't decide\" between two universities; asking which course suits her helps her decide.",
                    "Which one has the better course for you?", "I've decided to eat out tonight.", "I've never been to university."),
                Choice(7, "Choose the correct reply.", "B", "\"Do you mind if I take this chair?\" is answered with \"Not at all\", meaning yes, you can.",
                    "I bought this chair last year.", "Not at all — nobody's using it.", "Yes, take it seriously."),
                Choice(8, "Choose the correct reply.", "B", "The teacher praises the essay and gives advice about spelling; \"Thanks — I'll be more careful\" responds to both.",
                    "I wrote it in the library.", "Thanks — I'll be more careful with it.", "I've never spelled an essay."),
            ]),
        ]);

    private static ListeningPart Cl4Part2()
    {
        const string who = "Radio presenter";
        return new(2,
            "You will hear a radio presenter announcing a food festival in the city.",
            [
                Male(who, "And now some news for everyone who loves good food. The city's annual food festival is back, and this year it's bigger than ever. Last year it ran for three days, but this time the organisers have added an extra day, so the festival will last four days, from Thursday to Sunday next week."),
                Male(who, "For the past few years the festival has been held in Central Park, but because of building work there, it's moving to the square in front of the city theatre. The organisers say there's actually more space there, with room for over a hundred stalls."),
                Male(who, "As always, there'll be food from every region of Uzbekistan, as well as stalls from neighbouring countries. The highlight, of course, will be the giant plov. Each evening at seven o'clock, a team of chefs will serve plov cooked in an enormous pot big enough to feed a thousand people. Get there early, because last year it was all gone within an hour."),
                Male(who, "There's something new this year, too: a cooking competition for teenagers. Young people aged thirteen to seventeen can enter in teams of three, and the winning team will get a week's training in a professional restaurant kitchen. You can register on the festival website until Monday."),
                Male(who, "The organisers are also trying to reduce waste. There won't be any plastic bottles on sale, and visitors are asked to bring their own cup. Anyone who does will get ten per cent off every drink."),
                Male(who, "Finally, getting there. Parking near the theatre will be very limited, and several streets will be closed to traffic, so the organisers strongly recommend coming by metro. The nearest station is only two minutes' walk from the square. Entry to the festival is free, and it's open from midday until eleven at night."),
            ],
            [
                new("gap", L2, 2,
                [
                    Item(9, "This year the festival will last ___ days.", "\"The festival will last four days.\"", "four", "4"),
                    Item(10, "The festival will be held in the square in front of the city ___.", "\"It's moving to the square in front of the city theatre.\"", "theatre"),
                    Item(11, "Plov will be served every evening at ___ o'clock.", "\"Each evening at seven o'clock, a team of chefs will serve plov.\"", "seven", "7"),
                    Item(12, "There will be a new cooking competition for ___.", "\"A cooking competition for teenagers.\"", "teenagers"),
                    Item(13, "Visitors who bring their own ___ will get a discount on drinks.", "\"Visitors are asked to bring their own cup. Anyone who does will get ten per cent off.\"", "cup", "cups"),
                    Item(14, "The organisers recommend travelling to the festival by ___.", "\"The organisers strongly recommend coming by metro.\"", "metro"),
                ]),
            ]);
    }

    private static ListeningPart Cl4Part3() => new(3,
        "You will hear four people talking about their reading habits.",
        [
            Female("Speaker 1", "I spend nearly two hours a day on buses, and I used to try to read on them, but I always felt sick. My husband reads everything on his tablet, and he tried to persuade me to use one, but staring at a screen on a moving bus was even worse. Then a colleague introduced me to audiobooks. Now I just close my eyes and let someone read to me. I've got through more novels this year than in the previous five, and a good narrator makes a story come alive in a way my own reading never did."),
            Male("Speaker 2", "At school I hated reading. We had to read long classic novels and then write essays about them, and it felt like a punishment. I didn't open a book for pleasure until I was about twenty-five, when I broke my leg and was stuck at home for six weeks. A friend brought me a detective story, and I couldn't put it down. Since then I've read almost every crime novel I can find. I sometimes think it's a pity nobody at school gave me books like that."),
            Female("Speaker 3", "I love bookshops, and I buy far more books than I could ever read. My problem is that I get excited by a new book, read the first fifty pages, and then something else catches my attention. At the moment there are about eight books on my bedside table with bookmarks somewhere in the middle. People say reading in bed helps them relax and drop off, but it doesn't work like that for me: if a book's good, I'm awake till two. Maybe one day I'll go back and finish them all."),
            Male("Speaker 4", "Once a month, six of us meet in a café to talk about a book we've all read. We take turns choosing, which means I often end up reading things I'd never have picked myself, like poetry or science books. Sometimes the conversation gets quite heated, because we rarely agree. What I like is that it makes me read more carefully. I actually take notes now, so that I've got something intelligent to say. I still read on paper, by the way; I can't get used to e-books."),
        ],
        [
            new("match", L3, null,
            [
                Item(15, "Speaker 1", "Speaker 1: \"a colleague introduced me to audiobooks. Now I just close my eyes and let someone read to me\".", "B"),
                Item(16, "Speaker 2", "Speaker 2: \"At school I hated reading ... I didn't open a book for pleasure until I was about twenty-five\".", "C"),
                Item(17, "Speaker 3", "Speaker 3: \"read the first fifty pages, and then something else catches my attention\".", "F"),
                Item(18, "Speaker 4", "Speaker 4: \"Once a month, six of us meet in a café to talk about a book we've all read\".", "D"),
            ],
            [
                "I now do most of my reading on a screen.",
                "I prefer listening to books to reading them.",
                "I did not enjoy reading until I was an adult.",
                "I discuss books regularly with a group of people.",
                "Reading helps me to fall asleep.",
                "I often don't finish the books I start.",
            ]),
        ]);

    private static ListeningPart Cl4Part4()
    {
        const string who = "Information assistant";
        var map = new MapPlan("Oasis Shopping Centre",
            [new("Main entrance", 2, 4), new("Escalators", 2, 2), new("Cinema", 4, 0)],
            [
                new("A", 0, 4), new("B", 1, 3), new("C", 0, 2), new("D", 4, 2),
                new("E", 0, 0), new("F", 2, 0), new("G", 3, 1), new("H", 3, 3),
            ]);
        return new(4,
            "You will hear an assistant describing the ground floor of a new shopping centre to a group of visitors.",
            [
                Female(who, "Hello, and welcome to the Oasis Shopping Centre. We're standing at the main entrance, which is in the middle of the south wall of the building, so as you look into the centre, you're facing north. I'll just describe the ground floor for you."),
                Female(who, "If you turn left as soon as you come in and walk along the south wall, you'll reach the pharmacy, which is in the south-west corner. It's open until ten every night, even at weekends. Just inside the entrance, also on your left but a little further in, there's a phone shop, where you can buy SIM cards or get your phone repaired."),
                Female(who, "Straight ahead of you, in the very middle of the building, are the escalators up to the first floor. Just before you reach them, on the right-hand side, opposite the phone shop, there's a café. It's a nice place to meet friends."),
                Female(who, "The supermarket takes up most of the western side of the building. Its doors are against the west wall, level with the escalators. On the other side of the escalators, against the east wall and level with them, there's a large sports shop."),
                Female(who, "Families with young children will want the toy shop. It's in the north-west corner, at the far end of the building on the left."),
                Female(who, "If you walk past the escalators and keep going straight to the north wall, you'll come to the food court, with fifteen different restaurants. It's directly behind the escalators."),
                Female(who, "Finally, the cinema is in the north-east corner, and between the escalators and the cinema there's a shoe shop, which is having a sale this week. Enjoy your shopping!"),
            ],
            [
                new("map", L4, null,
                [
                    Item(19, "Pharmacy", "\"Turn left as soon as you come in and walk along the south wall ... in the south-west corner.\"", "A"),
                    Item(20, "Café", "\"Just before you reach them, on the right-hand side, opposite the phone shop, there's a café.\"", "H"),
                    Item(21, "Supermarket", "\"Its doors are against the west wall, level with the escalators.\"", "C"),
                    Item(22, "Toy shop", "\"It's in the north-west corner, at the far end of the building on the left.\"", "E"),
                    Item(23, "Food court", "\"Keep going straight to the north wall ... It's directly behind the escalators.\"", "F"),
                ], Map: map),
            ]);
    }

    private static ListeningPart Cl4Part5() => new(5,
        "You will hear three extracts about the environment.",
        [
            Male("Narrator", "Extract One."),
            Female("Madina", "Have you seen the new recycling bins behind our building, Dilshod?"),
            Male("Dilshod", "I have, and I think it's a great idea: separate bins for paper, plastic and glass. The problem is that half the people in the building don't seem to understand them. Yesterday I found food waste in the paper bin and bottles in with the plastic. If it's all mixed up like that, the whole lot probably ends up with the ordinary rubbish anyway."),
            Female("Madina", "I'm not surprised. The only instructions are those tiny symbols on the lids. My mother can't even see them without her glasses."),
            Male("Dilshod", "Exactly. So what can we do? We can't stand next to the bins all day."),
            Female("Madina", "Why don't we make a big, clear poster with pictures showing what goes where, and put one by the bins and one in each entrance? I'm sure the building committee would pay for the printing."),
            Male("Dilshod", "Good idea. I'll design it tonight if you speak to the committee."),
            Male("Narrator", "Extract Two."),
            Male("Presenter", "Dr Nazarova, you've spent the last five years working on the dried bed of the Aral Sea. What exactly are you doing there?"),
            Female("Dr Nazarova", "We're planting saxaul, a small desert tree that survives with very little water. When the sea shrank, it left behind a huge area of sand mixed with salt. Strong winds pick up this dust and carry it hundreds of kilometres, which damages farmland and is bad for people's health. The roots of the saxaul hold the sand in place, so the main aim is to stop those storms. People sometimes ask whether this will bring the sea back. It won't, unfortunately; that's a much bigger question."),
            Male("Presenter", "And is it working?"),
            Female("Dr Nazarova", "In some areas, yes. When we started, a lot of the young trees died in their first year. With better planting methods, many more survive now. I'm encouraged, but I'm also realistic. The area is enormous, and it will take decades of work."),
            Male("Narrator", "Extract Three."),
            Male("Volunteer", "A few years ago my washing machine stopped working. The shop told me that repairing it would cost almost as much as a new one, so I should just throw it away. I'm an engineer, so I opened it up myself and found that the only problem was a small part that cost less than a cup of coffee. That made me angry, to be honest, when I thought about how many machines like mine end up in landfill. So with a few friends I started a repair café. Once a month, people bring broken lamps, toasters, bicycles, even clothes, and our volunteers help them to fix them for free. What I love is that we don't just repair things for people; we show them how to do it. An old lady who came with a broken radio last year now helps other visitors with their electrical problems. That's the real point: skills, not just repairs."),
        ],
        [
            new("mcq", L5, null,
            [
                Choice(24, "What is Dilshod's complaint about the recycling bins?", "A", "\"Half the people in the building don't seem to understand them ... food waste in the paper bin.\"",
                    "People are putting the wrong things in them.", "There are not enough of them.", "They are emptied too rarely."),
                Choice(25, "What do Madina and Dilshod decide to do?", "C", "\"Why don't we make a big, clear poster with pictures showing what goes where?\" — \"Good idea.\"",
                    "ask for bigger symbols on the lids", "take turns to watch the bins", "put up clear instructions for residents"),
                Choice(26, "What is the main purpose of planting saxaul?", "B", "\"The roots of the saxaul hold the sand in place, so the main aim is to stop those storms.\"",
                    "to bring the sea back", "to stop dust storms", "to provide wood for local people"),
                Choice(27, "How does Dr Nazarova feel about the project's progress?", "A", "\"I'm encouraged, but I'm also realistic ... it will take decades of work.\"",
                    "positive, but aware that much work remains", "disappointed that so many trees have died", "certain that it will be finished soon"),
                Choice(28, "Why did the speaker start the repair café?", "C", "\"That made me angry ... how many machines like mine end up in landfill. So ... I started a repair café.\"",
                    "He wanted to earn some extra money.", "A shop asked him to help its customers.", "He was upset by how much is thrown away unnecessarily."),
                Choice(29, "What does the speaker value most about the repair café?", "A", "\"We don't just repair things for people; we show them how to do it ... skills, not just repairs.\"",
                    "Visitors learn to repair things themselves.", "It attracts many elderly people.", "Repairs are done very quickly."),
            ]),
        ]);

    private static ListeningPart Cl4Part6()
    {
        const string who = "Lecturer";
        return new(6,
            "You will hear part of a university lecture about saving water in agriculture.",
            [
                Male(who, "Good morning. Today's topic is one that matters enormously in Central Asia, and increasingly in the rest of the world: how farmers can use less water. Let me start with a figure that often surprises people. Worldwide, agriculture accounts for about seventy per cent of all the fresh water that we take from rivers, lakes and underground sources. Industry and households share the rest. So if we want to save water on a large scale, the fields are the place to start."),
                Male(who, "For thousands of years, the most common method of irrigation has been furrow irrigation. Water is released into small channels between the rows of crops and simply allowed to flow across the field. It's cheap and simple, but very wasteful. A large part of the water never reaches the plants at all. Some of it runs off the end of the field, some sinks too deep into the ground, and in a hot, dry climate a great deal is lost through evaporation before the crop can use it."),
                Male(who, "The most effective alternative is drip irrigation. Here, water travels through plastic pipes and comes out through tiny holes, drop by drop, right next to each plant. Because the water goes directly to the roots, very little is lost, and farmers who switch to drip systems can often cut their water use by a third or more, while their harvests stay the same or even increase. The main disadvantage is the cost of installation, which is why many governments now help farmers to buy the equipment."),
                Male(who, "But water can also be saved with much simpler changes. The first is timing. If farmers irrigate at night or very early in the morning, when temperatures are lower and the wind is usually calmer, far less water is lost to the air. It costs nothing, and yet it can make a real difference."),
                Male(who, "A second low-cost technique is to cover the soil around the plants with a layer of material such as straw, dry leaves or plastic sheeting. This layer is known as mulch. It shades the soil, keeps moisture in, and has the added benefit of stopping weeds from growing."),
                Male(who, "Why does all this matter so much? Using too much water doesn't only waste a precious resource; it can actually damage the land. When fields are over-irrigated, the level of underground water rises. As this water reaches the surface and dries, it leaves salt behind, and over the years the soil can become so salty that crops will no longer grow. This has been a serious problem in parts of Central Asia."),
                Male(who, "Finally, technology is helping farmers to make smarter decisions. Cheap sensors placed in the soil can measure moisture and send the information to a farmer's phone, so that crops are watered only when they actually need it, instead of according to a fixed timetable. In the next session, we'll look at crops that need less water in the first place."),
            ],
            [
                new("gap", L6, 1,
                [
                    Item(30, "Agriculture uses about ___ per cent of the fresh water taken worldwide.", "\"Agriculture accounts for about seventy per cent of all the fresh water.\"", "seventy", "70"),
                    Item(31, "In furrow irrigation in a hot climate, much water is lost through ___.", "\"A great deal is lost through evaporation before the crop can use it.\"", "evaporation"),
                    Item(32, "Drip irrigation takes water directly to the plants' ___.", "\"Because the water goes directly to the roots, very little is lost.\"", "roots"),
                    Item(33, "Farmers lose less water if they irrigate at ___ or very early in the morning.", "\"If farmers irrigate at night or very early in the morning ... far less water is lost.\"", "night"),
                    Item(34, "A layer of straw or plastic that keeps moisture in the soil is called ___.", "\"This layer is known as mulch.\"", "mulch"),
                    Item(35, "Over-irrigation can leave ___ in the soil, so that crops no longer grow.", "\"It leaves salt behind, and over the years the soil can become so salty that crops will no longer grow.\"", "salt"),
                ]),
            ]);
    }

    // =====================================================================
    // TEST 5
    // =====================================================================

    private static ListeningPart Cl5Part1() => new(1,
        "You will hear eight short sentences said on a train, in a clothes shop, at home and in other everyday places. Choose the best reply to each one.",
        [
            Female("Friend", "Do you fancy coming round for tea later?"),
            Female("Shop assistant", "Would you like to try it on? The changing rooms are over there."),
            Male("Colleague", "The meeting has been moved to Thursday afternoon."),
            Male("Passenger", "Excuse me, does this train stop at Samarkand?"),
            Female("Mother", "Have you tidied your room yet?"),
            Female("Receptionist", "Can I take your name, please?"),
            Male("Friend", "I'm thinking of getting a dog."),
            Male("Stranger", "Sorry to bother you, but you've dropped your glove."),
        ],
        [
            new("mcq", L1, null,
            [
                Choice(1, "Choose the correct reply.", "B", "\"Do you fancy coming round ...?\" is an invitation, accepted with \"I'd love to — what time?\"",
                    "I fancy a new car.", "I'd love to — what time?", "Tea is grown in the mountains."),
                Choice(2, "Choose the correct reply.", "A", "\"Would you like to try it on?\" is an offer; accepting it and asking about sizes fits.",
                    "Yes, please. Do you have it in a larger size too?", "I tried very hard.", "The room has changed a lot."),
                Choice(3, "Choose the correct reply.", "C", "The colleague gives new information, so the reply thanks him: \"thanks for letting me know\".",
                    "I moved house on Thursday.", "It was a very long meeting.", "Oh, thanks for letting me know."),
                Choice(4, "Choose the correct reply.", "A", "\"Does this train stop at Samarkand?\" is a yes/no question: \"Yes, it's the next stop but one.\"",
                    "Yes, it's the next stop but one.", "I've never been on a train.", "The train was built last year."),
                Choice(5, "Choose the correct reply.", "B", "\"Have you tidied your room yet?\" is answered with \"Not yet, but I'll do it after dinner.\"",
                    "My room is on the second floor.", "Not yet, but I'll do it after dinner.", "Yes, it's a very big room."),
                Choice(6, "Choose the correct reply.", "C", "\"Can I take your name?\" means \"What is your name?\", so the reply gives it.",
                    "No, you can't take it.", "I took it yesterday.", "Sure, it's Aziz Rahimov."),
                Choice(7, "Choose the correct reply.", "A", "The friend shares a plan; asking whether he has time to walk the dog responds to it.",
                    "Really? Will you have time to walk it every day?", "I thought so too, last night.", "I'd rather not get wet."),
                Choice(8, "Choose the correct reply.", "C", "The stranger points out a dropped glove, so the natural reply is thanks: \"I didn't notice.\"",
                    "It doesn't bother me at all.", "I bought them last winter.", "Oh, thank you — I didn't notice."),
            ]),
        ]);

    private static ListeningPart Cl5Part2()
    {
        const string who = "Camp director";
        return new(2,
            "You will hear the director of a summer camp giving information to parents.",
            [
                Female(who, "Good evening, everyone, and thank you for coming. I'm Nilufar Saidova, the director of Green Valley Summer Camp, and I'd like to tell you what your children can expect this summer and answer some of the questions that parents often ask."),
                Female(who, "The camp is in the mountains, about two hours' drive from the city, right on the shore of a lake. The water is clean and shallow near the edge, and children swim only when a lifeguard is on duty."),
                Female(who, "When the children arrive, we divide them into groups by age. In the past we had groups of twelve, but parents and staff both felt that was too many, so this year each group will have ten children. Every group has two leaders who stay with it all day, from breakfast until lights out. All our leaders are university students who have completed first-aid training."),
                Female(who, "Now, what should your children bring? We provide all the bedding, so there's no need to pack sheets or a sleeping bag. However, there are no lights on the paths between the cabins, so every child will need a torch. Please also pack a sun hat and a water bottle."),
                Female(who, "Many of you have asked about mobile phones. We collect phones on the first day and keep them safe, but children can call home every evening between seven and eight. And of course, you're welcome to visit. Our brochure said visiting day would be on the first weekend, but we've changed that, because children settle in better if they have a week first. So visiting day is now the second Sunday of the camp."),
                Female(who, "On the last evening, the children put on a talent show for everyone. They sing, dance and tell jokes, and some groups even write their own short plays. It's always the highlight of the summer. If you have any other questions, I'll be at the back of the hall with my colleagues."),
            ],
            [
                new("gap", L2, 2,
                [
                    Item(9, "The camp is on the shore of a ___.", "\"Right on the shore of a lake.\"", "lake"),
                    Item(10, "This year each group will have ___ children.", "\"This year each group will have ten children.\"", "ten", "10"),
                    Item(11, "Each group has two ___ who stay with it all day.", "\"Every group has two leaders who stay with it all day.\"", "leaders"),
                    Item(12, "Because the paths have no lights, every child needs a ___.", "\"There are no lights on the paths ... so every child will need a torch.\"", "torch"),
                    Item(13, "Visiting day is on the second ___ of the camp.", "\"Visiting day is now the second Sunday of the camp.\"", "Sunday"),
                    Item(14, "On the last evening there is a ___ show.", "\"On the last evening, the children put on a talent show.\"", "talent"),
                ]),
            ]);
    }

    private static ListeningPart Cl5Part3() => new(3,
        "You will hear four people talking about the city parks they use.",
        [
            Female("Speaker 1", "Every Saturday I take my two boys to the old park near the railway station. I grew up just across the road from it, and my grandmother used to take me there on summer evenings to buy ice cream and feed the ducks. The ducks are still there, and so is the old carousel, although it's been repainted. Whenever I sit on the bench where she used to sit, I feel as if I'm eight years old again. My boys think it's boring compared to the new park, but I hope they'll love it too one day."),
            Male("Speaker 2", "I've looked after the flower beds in the city's main park for eleven years. I start at six in the morning, before the visitors arrive, and every spring we plant more than twenty thousand tulips. People often stop to ask me the way to the lake, though I rarely have time for a proper chat. What I really love is watching something I planted in autumn come up in spring. There aren't many jobs where you can see the results of your work so clearly."),
            Female("Speaker 3", "I've lived next to Riverside Park for twenty years, and it used to be my quiet place. Since they built the new food court and the fountain show, though, it's a different world. At weekends there are so many people that you can hardly walk along the paths, and the music from the fountain goes on until midnight. Some people say they should put more lamps along the river, but honestly, the lighting's fine. What we need is fewer events, not more. These days I only go early on weekday mornings, when it's still peaceful."),
            Male("Speaker 4", "I go to the park near my office every morning before work. I run three laps around the lake, which is about five kilometres, and then I use the outdoor gym equipment near the tennis courts for twenty minutes. It's free, which is great, because I gave up my gym membership last year. There's a group of runners who meet there on Sundays and go for coffee afterwards, and they've invited me a few times, but I prefer to run alone with my headphones. It's my time to think."),
        ],
        [
            new("match", L3, null,
            [
                Item(15, "Speaker 1", "Speaker 1: \"my grandmother used to take me there ... I feel as if I'm eight years old again\".", "F"),
                Item(16, "Speaker 2", "Speaker 2: \"I've looked after the flower beds in the city's main park for eleven years ... There aren't many jobs where ...\".", "E"),
                Item(17, "Speaker 3", "Speaker 3: \"At weekends there are so many people that you can hardly walk along the paths\".", "D"),
                Item(18, "Speaker 4", "Speaker 4: \"I run three laps around the lake ... then I use the outdoor gym equipment\".", "A"),
            ],
            [
                "I use the park mainly for exercise.",
                "I think the park should be better lit at night.",
                "I have made new friends in the park.",
                "I feel the park has become too busy.",
                "The park is my place of work.",
                "The park reminds me of my childhood.",
            ]),
        ]);

    private static ListeningPart Cl5Part4()
    {
        const string who = "Hotel manager";
        var map = new MapPlan("Silk Garden Hotel",
            [new("Main gate", 4, 4), new("Hotel", 2, 2), new("Pond", 1, 1)],
            [
                new("A", 0, 2), new("B", 0, 0), new("C", 2, 4), new("D", 3, 3),
                new("E", 4, 0), new("F", 4, 2), new("G", 2, 0), new("H", 0, 4),
            ]);
        return new(4,
            "You will hear a hotel manager describing the hotel grounds to new guests at the main gate.",
            [
                Male(who, "Good afternoon, and welcome to the Silk Garden Hotel. Before you go up to your rooms, let me show you around the grounds on this plan. We're at the main gate, which is in the south-east corner of the grounds. The hotel building itself is right in the middle, so as you face north, it's ahead of you and to the left."),
                Male(who, "Just inside the gate, between the gate and the hotel, is the car park. If you've come by car, you can leave it there free of charge."),
                Male(who, "If you walk west from the gate along the southern wall, you'll find the gift shop halfway along, directly in front of the hotel. It sells local silk scarves and ceramics."),
                Male(who, "Walking north from the gate along the eastern fence, you'll come to the tennis courts, which are level with the hotel. Rackets can be borrowed from reception."),
                Male(who, "On the far side of the hotel, the western side, there's the swimming pool, right next to the western fence. It's open from seven in the morning until nine in the evening. If you have children with you, they'll probably enjoy the playground, which is in the south-west corner of the grounds, south of the pool."),
                Male(who, "Our restaurant is in a separate building directly behind the hotel, against the northern fence. Breakfast is served there from seven."),
                Male(who, "Between the hotel and the north-west corner there's a small pond with a bridge, and beyond it, in the north-west corner itself, is the spa, where you can book a massage."),
                Male(who, "And finally, if you're here for the conference, the conference centre is in the north-east corner, directly north of the tennis courts. Enjoy your stay!"),
            ],
            [
                new("map", L4, null,
                [
                    Item(19, "Gift shop", "\"Walk west from the gate along the southern wall ... halfway along, directly in front of the hotel.\"", "C"),
                    Item(20, "Swimming pool", "\"On the far side of the hotel, the western side ... right next to the western fence.\"", "A"),
                    Item(21, "Restaurant", "\"Directly behind the hotel, against the northern fence.\"", "G"),
                    Item(22, "Spa", "\"Beyond it, in the north-west corner itself, is the spa.\"", "B"),
                    Item(23, "Conference centre", "\"In the north-east corner, directly north of the tennis courts.\"", "E"),
                ], Map: map),
            ]);
    }

    private static ListeningPart Cl5Part5() => new(5,
        "You will hear three extracts about choosing a career.",
        [
            Male("Narrator", "Extract One."),
            Male("Timur", "You look worried, Laylo. Is it the university applications?"),
            Female("Laylo", "Yes. My parents have always assumed I'll study medicine, like my mother. And I'm not against it; I've got the grades, and I know it's a good career. But what I really love is graphic design. The thing is, I just don't know if I'd be good enough to earn a living from it. What if I choose design and end up without a job?"),
            Male("Timur", "Have you told your parents how you feel?"),
            Female("Laylo", "Not really. They'd support me whatever I chose, I think, but that doesn't make the decision any easier."),
            Male("Timur", "Well, why not find out what each job is really like? My cousin runs a small design studio. I'm sure he'd let you spend a week there in the summer. And your mother could probably arrange for you to spend a few days at her hospital."),
            Female("Laylo", "That's actually a really good idea."),
            Male("Narrator", "Extract Two."),
            Female("Presenter", "Mr Karimov, as a careers adviser, what's the most common mistake you see young people make?"),
            Male("Mr Karimov", "Many people imagine it's choosing a job just for the money, and that does happen. But far more often, I see students choosing a course simply because their best friends are choosing it. They don't want to be separated, which is understandable at seventeen, but it's a poor reason to spend four years studying something."),
            Female("Presenter", "And what if someone realises, a few years later, that they've made the wrong choice?"),
            Male("Mr Karimov", "Then they're in very good company! Most people today change career at least once. Nothing you learn is wasted. A lawyer who becomes a teacher still knows how to explain complicated ideas clearly. So my message is: choose carefully, but don't be paralysed by the fear of getting it wrong."),
            Male("Narrator", "Extract Three."),
            Male("Chef", "For eight years I worked as a civil engineer, designing bridges. It was a good job, well paid and respected, and I was quite good at it. But I spent most of my days in front of a computer, and I realised I was only really happy at weekends, when I was cooking for friends. So at thirty-two, I left my job and went to cooking school. Everyone thought I'd gone mad, including my parents. Now I run a small restaurant, and I work much longer hours for less money. People sometimes ask whether I regret all those years in engineering. Not at all. Engineering taught me to plan carefully, to be precise and to stay calm when things go wrong, and a busy kitchen on a Saturday night needs all three."),
        ],
        [
            new("mcq", L5, null,
            [
                Choice(24, "What is Laylo's main worry?", "B", "\"I just don't know if I'd be good enough to earn a living from it.\"",
                    "that her parents will be angry with her", "that she might not succeed in design", "that her grades are not good enough for medicine"),
                Choice(25, "What does Timur suggest?", "A", "\"Why not find out what each job is really like? ... spend a week there ... a few days at her hospital.\"",
                    "getting some work experience in both fields", "studying medicine first and design later", "asking her mother to make the decision"),
                Choice(26, "According to Mr Karimov, what is the most common mistake?", "B", "\"Far more often, I see students choosing a course simply because their best friends are choosing it.\"",
                    "choosing a job only for the salary", "following friends onto the same course", "listening too much to parents"),
                Choice(27, "What is Mr Karimov's attitude to changing career?", "C", "\"Most people today change career at least once. Nothing you learn is wasted.\"",
                    "It should be avoided if at all possible.", "It is only sensible for very young people.", "It is normal, and earlier skills remain useful."),
                Choice(28, "Why did the speaker leave engineering?", "A", "\"I realised I was only really happy at weekends, when I was cooking for friends.\"",
                    "He found more happiness in cooking.", "He was not very good at the job.", "His parents encouraged him to change."),
                Choice(29, "How does the speaker feel about his years as an engineer?", "C", "\"Engineering taught me to plan carefully, to be precise and to stay calm ... a busy kitchen ... needs all three.\"",
                    "He wishes he had left much sooner.", "He sees them as wasted time.", "They gave him skills he still uses."),
            ]),
        ]);

    private static ListeningPart Cl5Part6()
    {
        const string who = "Lecturer";
        return new(6,
            "You will hear part of a university lecture about the history of paper.",
            [
                Female(who, "Good morning, everyone. Today we're going to look at an invention that most of us use every day without a second thought: paper. It's easy to forget how revolutionary it once was."),
                Female(who, "Before paper, people wrote on all kinds of surfaces. The ancient Egyptians used papyrus, made from a plant that grew along the Nile. In China, people wrote on strips of bamboo, which were heavy, or on silk, which was very expensive. In Europe, for many centuries, the main writing material was parchment, which was made from animal skin. A single large book could require the skins of dozens or even hundreds of animals, so books were extremely valuable."),
                Female(who, "According to tradition, paper was invented in China in the year 105 by a court official called Cai Lun. In fact, archaeologists have found older fragments of paper, so he probably improved an existing technique rather than inventing it from nothing. What we do know is that he made paper from tree bark, hemp, old rags and even fishing nets. These materials were soaked, beaten into a pulp, spread thinly on a screen and left to dry."),
                Female(who, "For several hundred years, the technique stayed largely within China. It reached Samarkand in the eighth century. A popular story says that Chinese prisoners taught the craft after a battle in 751, but many historians now treat this as a legend rather than a proven fact. Whatever the truth, Samarkand soon became famous for its paper. Workshops were built along the canals, and the power of water was used to drive heavy wooden hammers that beat the fibres. The paper they produced was smooth, strong and highly valued across the Islamic world. Today, visitors can still watch paper being made in the traditional way at a workshop in the village of Konigil, near Samarkand."),
                Female(who, "From Central Asia, papermaking spread to Baghdad, then to Damascus and Cairo, and eventually to Europe. It arrived through Spain, where one of the first European paper mills was working by the twelfth century. From there it spread slowly north. When printing presses appeared in the fifteenth century, the demand for paper rose dramatically."),
                Female(who, "For most of this history, European paper was made from old linen and cotton cloth. By the nineteenth century, however, the demand for books and newspapers had grown so much that there were simply not enough rags to go round. Inventors began experimenting with other materials, and eventually they found a solution in wood. Wood pulp was cheap and plentiful, and it is still the main raw material for paper today. Next week, we'll look at how the printing press changed the way knowledge was shared."),
            ],
            [
                new("gap", L6, 1,
                [
                    Item(30, "In Europe, parchment was made from animal ___.", "\"Parchment, which was made from animal skin.\"", "skin", "skins"),
                    Item(31, "Cai Lun made paper from bark, hemp, old rags and fishing ___.", "\"He made paper from tree bark, hemp, old rags and even fishing nets.\"", "nets"),
                    Item(32, "Papermaking reached Samarkand in the ___ century.", "\"It reached Samarkand in the eighth century.\"", "eighth", "8th"),
                    Item(33, "In Samarkand, the power of ___ drove the hammers that beat the fibres.", "\"The power of water was used to drive heavy wooden hammers that beat the fibres.\"", "water"),
                    Item(34, "Papermaking arrived in Europe through ___.", "\"It arrived through Spain.\"", "Spain"),
                    Item(35, "In the nineteenth century there were not enough ___, so wood was used instead.", "\"There were simply not enough rags to go round ... they found a solution in wood.\"", "rags"),
                ]),
            ]);
    }

    // =====================================================================
    // TEST 6
    // =====================================================================

    private static ListeningPart Cl6Part1() => new(1,
        "You will hear eight short sentences said by friends, neighbours, teachers and colleagues. Choose the best reply to each one.",
        [
            Male("Colleague", "Could you give me a hand with these boxes?"),
            Female("Friend", "How did your job interview go yesterday?"),
            Male("Shop assistant", "Would you like a bag for that?"),
            Female("Neighbour", "I'm really sorry about the noise last night. It was my son's birthday."),
            Male("Tourist", "Can you recommend a good place to eat around here?"),
            Female("Teacher", "Why weren't you in class on Monday?"),
            Male("Friend", "I've lost my keys again. I'm hopeless!"),
            Female("Colleague", "Tomorrow's my last day. I'm moving to the Tashkent office."),
        ],
        [
            new("mcq", L1, null,
            [
                Choice(1, "Choose the correct reply.", "C", "\"Could you give me a hand?\" asks for help; \"Sure — where do you want them?\" agrees to help.",
                    "I gave it to him yesterday.", "They're very nice boxes.", "Sure — where do you want them?"),
                Choice(2, "Choose the correct reply.", "A", "\"How did your interview go?\" asks about the result: \"Pretty well, I think.\"",
                    "Pretty well, I think. They're calling me next week.", "I went there by taxi.", "It's tomorrow at ten."),
                Choice(3, "Choose the correct reply.", "B", "\"Would you like a bag?\" is an offer, politely refused with a reason: \"it'll fit in my pocket\".",
                    "I like bags a lot.", "No, thanks — it'll fit in my pocket.", "I'd like it in red."),
                Choice(4, "Choose the correct reply.", "A", "The neighbour apologises, so the reply accepts the apology: \"Don't worry, it wasn't too bad.\"",
                    "Don't worry, it wasn't too bad.", "Happy birthday to you!", "I was very noisy too."),
                Choice(5, "Choose the correct reply.", "C", "\"Can you recommend a good place to eat?\" is answered with a recommendation: \"Try the café by the fountain.\"",
                    "I've already eaten, thanks.", "Around here is very nice.", "Try the café by the fountain — it's cheap and very good."),
                Choice(6, "Choose the correct reply.", "B", "\"Why weren't you in class?\" asks for a reason: \"I had a doctor's appointment.\"",
                    "Class starts on Monday.", "I had a doctor's appointment. I've brought a note.", "Yes, I'll be there on Monday."),
                Choice(7, "Choose the correct reply.", "A", "The friend has lost his keys, so a helpful reply suggests where to look.",
                    "Have you checked your coat pocket?", "I lost the game last night too.", "It's the key to success."),
                Choice(8, "Choose the correct reply.", "B", "The colleague announces she is leaving; \"We'll miss you — good luck!\" is the natural reaction.",
                    "Tomorrow is Friday.", "Really? We'll miss you — good luck!", "I've never been to Tashkent."),
            ]),
        ]);

    private static ListeningPart Cl6Part2()
    {
        const string who = "Tour guide";
        return new(2,
            "You will hear a guide talking to a group at the start of a backstage tour of a theatre.",
            [
                Male(who, "Good afternoon, everyone, and welcome to the City Drama Theatre. My name's Anvar, and I'll be your guide on this backstage tour. We used to offer a short one-hour version, but we've recently added the workshops, so the tour now lasts about ninety minutes. There are quite a few stairs, so do let me know if you'd like to take the lift at any point."),
                Male(who, "Before we start, a couple of rules. You're welcome to take photos almost everywhere, but not in the costume department, because some of the designs for next season's shows are still secret. And please don't touch any of the scenery, because some of it is still wet with paint."),
                Male(who, "Our first stop will be the main stage itself. When you're sitting in the audience it looks quite small, but it's actually twenty metres wide and even deeper than that. In the middle there's a revolving section that can turn a full circle, so a whole scene can change in seconds."),
                Male(who, "From the stage, we'll look up into the fly tower. That's a huge empty space above the stage, almost as tall as the auditorium, where scenery is lifted up and stored until it's needed. You'll be amazed how many ropes there are."),
                Male(who, "After that, we'll go downstairs to see the dressing rooms and the green room, which is where actors wait and relax before they go on stage. Nobody's quite sure why it's called that; the walls in ours are actually blue!"),
                Male(who, "The tour ends in the workshops, where our team makes more than three hundred costumes every year. And finally, keep your tour ticket, because it gives you fifteen per cent off tickets for any performance this season. Right, follow me, please."),
            ],
            [
                new("gap", L2, 2,
                [
                    Item(9, "The tour now lasts about ___ minutes.", "\"The tour now lasts about ninety minutes.\"", "ninety", "90"),
                    Item(10, "Visitors may not take photos in the ___ department.", "\"You're welcome to take photos almost everywhere, but not in the costume department.\"", "costume"),
                    Item(11, "The main stage is ___ metres wide.", "\"It's actually twenty metres wide.\"", "twenty", "20"),
                    Item(12, "Scenery is stored above the stage in the ___.", "\"The fly tower ... where scenery is lifted up and stored until it's needed.\"", "fly tower"),
                    Item(13, "Before going on stage, actors wait in the ___ room.", "\"The green room, which is where actors wait and relax before they go on stage.\"", "green"),
                    Item(14, "The tour ticket gives ___ per cent off tickets for performances.", "\"It gives you fifteen per cent off tickets for any performance this season.\"", "fifteen", "15"),
                ]),
            ]);
    }

    private static ListeningPart Cl6Part3() => new(3,
        "You will hear four people talking about how they use social media.",
        [
            Female("Speaker 1", "My son works in South Korea, and my daughter lives in Germany with her husband and my two grandchildren. Ten years ago I didn't even have a smartphone. Now I'm in three family groups, and every morning there are photos waiting for me: the little ones going to school, what my son had for dinner. We have video calls every Sunday. My friends laugh because I'm on my phone more than they are, but when your family is so far away, it's the next best thing to being together."),
            Male("Speaker 2", "About a year ago I realised I was spending nearly four hours a day scrolling, and I couldn't remember anything I'd seen. So I deleted almost everything. I kept one messaging app for work, because I have to, but the rest went. The first couple of weeks were strange; I kept reaching for my phone without thinking. Now I read more, I sleep better, and I actually phone my friends. My teenage daughter still uses all the apps, and people expect me to worry about her, but she's sensible, and it's her choice."),
            Female("Speaker 3", "I bake cakes for weddings and birthdays, and almost all my customers find me through social media. I post photos of every cake and short videos of the decorating, which people seem to love. People assume I learned to decorate from online videos, but actually my mother taught me everything in her kitchen. What I've had to learn is the business side: how to take good photos, when to post and how to answer customers quickly. Some weeks I get more orders than I can handle."),
            Male("Speaker 4", "I'm nineteen, and I think social media makes life harder for people my age. Everyone posts only their best moments: holidays, new clothes, exam results. Even though I know it isn't real, I find myself doing the same. Before I post anything, I take about twenty photos to find the perfect one, and if a post doesn't get enough likes, I sometimes delete it. I know it's silly. I've thought about deleting my accounts, but all my friends organise everything through them, so I'd feel left out."),
        ],
        [
            new("match", L3, null,
            [
                Item(15, "Speaker 1", "Speaker 1: \"when your family is so far away, it's the next best thing to being together\".", "D"),
                Item(16, "Speaker 2", "Speaker 2: \"So I deleted almost everything. I kept one messaging app for work\".", "B"),
                Item(17, "Speaker 3", "Speaker 3: \"I bake cakes for weddings and birthdays, and almost all my customers find me through social media\".", "A"),
                Item(18, "Speaker 4", "Speaker 4: \"Before I post anything, I take about twenty photos to find the perfect one\".", "F"),
            ],
            [
                "I use social media mainly for my business.",
                "I have deleted most of my accounts.",
                "I worry about how my children use social media.",
                "It helps me keep in touch with family abroad.",
                "I have learned practical skills from online videos.",
                "I feel pressure to show a perfect life online.",
            ]),
        ]);

    private static ListeningPart Cl6Part4()
    {
        const string who = "Museum guide";
        var map = new MapPlan("City Museum of Crafts",
            [new("Entrance", 2, 4), new("Main hall", 2, 2), new("Lift", 4, 4)],
            [
                new("A", 4, 2), new("B", 3, 4), new("C", 0, 1), new("D", 1, 3),
                new("E", 0, 0), new("F", 2, 0), new("G", 0, 2), new("H", 4, 0),
            ]);
        return new(4,
            "You will hear a guide describing the ground floor of a museum to visitors standing at the entrance.",
            [
                Female(who, "Welcome to the City Museum of Crafts. We're standing just inside the entrance, in the middle of the south side of the building, so as you look towards the main hall, you're facing north. Let me quickly explain the layout of the ground floor."),
                Female(who, "Immediately to your right is the museum shop, next to the lift in the south-east corner. You might like to leave it until the end of your visit. Just in front of you, on the left, before you go into the main hall, is the information desk. That's where you can pick up a free audio guide or ask about guided tours."),
                Female(who, "The main hall, straight ahead in the centre of the building, is used for concerts and events. From the main hall, if you go through the door on the left, you'll come into the pottery gallery, which runs along the west wall. It has pieces from all the famous ceramics centres of the region."),
                Female(who, "Directly north of the pottery gallery is a small coin room, and beyond that, in the north-west corner, is the café, which has a lovely courtyard garden."),
                Female(who, "Now, if you go through the door on the right side of the main hall instead, you'll enter the musical instruments gallery, against the east wall. There are headphones there, so you can listen to each instrument. From the musical instruments gallery, a corridor leads north to the temporary exhibition room in the north-east corner. At the moment it's showing an exhibition of jewellery."),
                Female(who, "And finally, if you walk straight through the main hall and out the far side, you'll reach the textiles gallery, in the middle of the north wall at the back of the building. It's our largest gallery, so leave plenty of time for it."),
            ],
            [
                new("map", L4, null,
                [
                    Item(19, "Information desk", "\"Just in front of you, on the left, before you go into the main hall, is the information desk.\"", "D"),
                    Item(20, "Pottery gallery", "\"Through the door on the left ... the pottery gallery, which runs along the west wall.\"", "G"),
                    Item(21, "Musical instruments", "\"Through the door on the right side of the main hall ... against the east wall.\"", "A"),
                    Item(22, "Temporary exhibitions", "\"A corridor leads north to the temporary exhibition room in the north-east corner.\"", "H"),
                    Item(23, "Textiles gallery", "\"Walk straight through the main hall ... in the middle of the north wall at the back of the building.\"", "F"),
                ], Map: map),
            ]);
    }

    private static ListeningPart Cl6Part5() => new(5,
        "You will hear three extracts about sport.",
        [
            Male("Narrator", "Extract One."),
            Male("Rustam", "Kamila, I hear you've joined a table tennis club. That's a bit unexpected!"),
            Female("Kamila", "I know! It's all my son's fault. He's been playing for years, and one evening he said I'd never be able to return even one of his serves. Well, I couldn't let that go, could I? So I found a club for adult beginners."),
            Male("Rustam", "And how's it going?"),
            Female("Kamila", "Much better than I expected. I thought I'd just be doing it for exercise, and I am getting fitter, but what I really love is the competition. Last month I played in my first tournament and won two matches. I was so excited I couldn't sleep that night."),
            Male("Rustam", "So have you beaten your son yet?"),
            Female("Kamila", "Not yet. But he's starting to look a bit nervous."),
            Male("Narrator", "Extract Two."),
            Female("Presenter", "Bekzod, you coach football for children under ten. How important is winning at that age?"),
            Male("Bekzod", "Honestly, hardly at all. At that age I'm much more interested in whether every child touches the ball, learns to pass and enjoys the game. If we win every match but half the team spends the whole time on the bench, I've failed. Some of the best players I've coached were terrible at eight."),
            Female("Presenter", "Do parents agree with that?"),
            Male("Bekzod", "Most do. But there are always a few who stand at the side shouting instructions, sometimes different instructions from mine, and getting angry with the referee. The children find it really stressful. So now we have a rule: parents can cheer, but only the coaches give advice."),
            Male("Narrator", "Extract Three."),
            Male("Football fan", "Like most people, I used to watch football on television. You get replays, the best camera angles and expert commentary; in many ways it's a better view of the game. But last year a friend took me to see our local second-division team, and I was hooked. There were only about two thousand people in the stadium, but when the home team scored in the last minute, strangers were hugging each other. You simply can't feel that on your sofa. Tickets cost less than a pizza, and after the match some of the players came over to talk to the children. So if you've never been to a local match, I'd really encourage you to give it a try this season. These small clubs depend on local people, and without our support, many of them won't survive."),
        ],
        [
            new("mcq", L5, null,
            [
                Choice(24, "Why did Kamila start playing table tennis?", "C", "\"He said I'd never be able to return even one of his serves. Well, I couldn't let that go.\"",
                    "Her doctor told her to take more exercise.", "She wanted to meet new people.", "Her son challenged her."),
                Choice(25, "What has Kamila enjoyed most?", "A", "\"I am getting fitter, but what I really love is the competition.\"",
                    "competing against other players", "getting fitter", "spending time with her son"),
                Choice(26, "What is Bekzod's view of winning in children's football?", "B", "\"Honestly, hardly at all ... whether every child touches the ball, learns to pass and enjoys the game.\"",
                    "It motivates children to work harder.", "It matters less than learning and enjoying the game.", "It is important for the most talented players."),
                Choice(27, "What problem does Bekzod mention with some parents?", "A", "\"A few ... stand at the side shouting instructions ... The children find it really stressful.\"",
                    "They put pressure on the children during matches.", "They rarely come to watch the matches.", "They want their children to play in every position."),
                Choice(28, "What does the speaker value most about watching a match live?", "B", "\"Strangers were hugging each other. You simply can't feel that on your sofa.\"",
                    "the better view of the game", "the shared emotions of the crowd", "the chance to meet famous players"),
                Choice(29, "What is the speaker's main purpose?", "C", "\"I'd really encourage you to give it a try ... These small clubs depend on local people.\"",
                    "to complain about the cost of tickets", "to compare different television channels", "to persuade listeners to support local clubs"),
            ]),
        ]);

    private static ListeningPart Cl6Part6()
    {
        const string who = "Lecturer";
        return new(6,
            "You will hear part of a university lecture about how memory works when we learn a language.",
            [
                Male(who, "Good morning, everyone. Every language learner knows the frustration: you learn twenty new words on Monday, and by Friday you can remember only three or four. Today I'd like to explain what's happening in your memory when this occurs, and what research tells us about how to make new vocabulary stick."),
                Male(who, "Psychologists usually distinguish between two kinds of memory. The first is working memory, which holds the information you're dealing with at this very moment, for example a phone number you're about to dial. Its capacity is surprisingly small. For many years people quoted the figure of seven items, but most researchers now believe it's closer to four. That's one reason why a teacher who presents fifteen new words in five minutes is asking the impossible."),
                Male(who, "The second kind is long-term memory, which, as far as we know, has almost unlimited capacity. The challenge for learners is getting information from the first kind of memory into the second, and keeping it there."),
                Male(who, "The problem of forgetting was first studied systematically in the 1880s by the German psychologist Hermann Ebbinghaus. He taught himself lists of nonsense syllables and tested how much he remembered over time. His results, known as the forgetting curve, showed that forgetting is fastest at the beginning: most of what we forget, we forget within the first day. After that, the curve flattens out."),
                Male(who, "The good news is that every time we review something, the curve becomes flatter, and we forget more slowly. This leads to a technique called spaced repetition: you review a word after one day, then after three days, then after a week, and so on, with longer and longer gaps. Many vocabulary apps are built on exactly this idea."),
                Male(who, "However, how you review matters as much as when. Many students review by reading their word lists again and again. It feels productive, but the research is clear: actively trying to recall a word, for example by covering the translation and testing yourself, is far more effective than rereading. The effort of retrieving information is precisely what strengthens the memory."),
                Male(who, "A third useful principle concerns what you learn. Words rarely appear alone in real language. We say 'make a decision', not 'do a decision', and 'heavy rain', not 'strong rain'. If you learn words in chunks, that is, in groups of words that naturally go together, you'll not only remember them better but also use them more accurately."),
                Male(who, "Finally, memory is not purely logical. We remember best the things that matter to us. Words connected to strong emotions or to personal experiences are much easier to recall than words from a list. So instead of memorising the word 'delighted' on its own, write a sentence about a moment when you really were delighted. In our next session we'll look at the role of motivation, but for now, try spacing out your revision this week and see what happens."),
            ],
            [
                new("gap", L6, 1,
                [
                    Item(30, "Most researchers now believe working memory can hold about ___ items.", "\"Most researchers now believe it's closer to four.\"", "four", "4"),
                    Item(31, "Ebbinghaus found that most forgetting happens within the first ___.", "\"Most of what we forget, we forget within the first day.\"", "day"),
                    Item(32, "Reviewing words with longer and longer gaps is called ___ repetition.", "\"This leads to a technique called spaced repetition.\"", "spaced"),
                    Item(33, "Testing yourself is far more effective than ___.", "\"Actively trying to recall a word ... is far more effective than rereading.\"", "rereading", "re-reading"),
                    Item(34, "It helps to learn words in ___, or groups of words that go together.", "\"If you learn words in chunks, that is, in groups of words that naturally go together.\"", "chunks"),
                    Item(35, "Words connected to strong ___ are easier to recall.", "\"Words connected to strong emotions or to personal experiences are much easier to recall.\"", "emotions", "emotion"),
                ]),
            ]);
    }
}
