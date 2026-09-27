using SpeakingCoach.Api.Services.Mock;
using static SpeakingCoach.Api.Services.Mock.CefrObjective;

namespace SpeakingCoach.Api.Services.Content;

// Tayyor CEFR (Multilevel) testlari. Original; tuzilishi agentlik formatiga mos:
// Listening 6 qism (8 + 6 + 4 + 5 + 6 + 6), Reading 5 qism (6 + 8 + 6 + 9 + 6).
// Matn va savollar mualliflik ishi — haqiqiy imtihon materiallaridan ko'chirilmagan.
public static class BuiltInCefr
{
    public static readonly ListeningTest Listening1 = new([ListeningPart1(), ListeningPart2(), ListeningPart3(), ListeningPart4(), ListeningPart5(), ListeningPart6()]);

    public static readonly ReadingTest Reading1 = new("", [ReadingPart1(), ReadingPart2(), ReadingPart3(), ReadingPart4(), ReadingPart5()]);

    // ---- Yordamchilar ----

    private static ScriptLine Male(string speaker, string text) => new(speaker, "male", text);
    private static ScriptLine Female(string speaker, string text) => new(speaker, "female", text);

    /// <summary>Variantli savol: javob — harf, variantlar — harfsiz matn.</summary>
    private static ObjQuestion Choice(int n, string prompt, string answer, string why, params string[] options) =>
        new(n, prompt, [.. options], [answer], why);

    /// <summary>Gap / match / map / tfng savoli: bir yoki bir nechta qabul qilinadigan javob.</summary>
    private static ObjQuestion Item(int n, string prompt, string why, params string[] answers) =>
        new(n, prompt, null, [.. answers], why);

    // =====================================================================
    // LISTENING
    // =====================================================================

    private static ListeningPart ListeningPart1() => new(1,
        "You will hear eight short sentences. Choose the best reply to each one.",
        [
            Female("Woman at a bus stop", "Excuse me, could you tell me when the next bus to the city centre leaves?"),
            Male("Neighbour", "Would you mind turning your music down a bit? I'm trying to study."),
            Female("Receptionist", "I'm afraid the doctor is running about twenty minutes late this morning."),
            Male("Classmate", "How long have you been learning English?"),
            Female("Friend", "Congratulations on your new job! When do you actually start?"),
            Male("Tourist", "Do you happen to know whether the library is open on Sundays?"),
            Female("Colleague", "I can't believe how much they're charging for the concert tickets this year!"),
            Male("Friend", "If you're not doing anything after work, how about grabbing a bite to eat?"),
        ],
        [
            new("mcq", L1, null,
            [
                Choice(1, "Choose the correct reply.", "B", "The speaker asks \"when the next bus ... leaves\", so the reply must give a time: \"In about ten minutes\".",
                    "Yes, I often go to the city centre.", "In about ten minutes, from this stop.", "It takes nearly an hour by car."),
                Choice(2, "Choose the correct reply.", "A", "\"Would you mind turning your music down?\" is a polite complaint, so an apology fits: \"Oh, sorry\".",
                    "Oh, sorry — I didn't realise it was so loud.", "No, I don't mind studying at all.", "It's a new song by my favourite band."),
                Choice(3, "Choose the correct reply.", "C", "\"The doctor is running about twenty minutes late\" is answered by agreeing to wait: \"I'll wait here\".",
                    "He must be a very good doctor.", "I came here by taxi.", "That's all right, I'll wait here."),
                Choice(4, "Choose the correct reply.", "B", "\"How long have you been ...?\" asks about a period of time up to now: \"For about three years now\".",
                    "Every day after work.", "For about three years now.", "Until I pass my exam."),
                Choice(5, "Choose the correct reply.", "A", "The question is \"When do you actually start?\", so the reply names a day: \"next Monday\".",
                    "Thanks — next Monday, if everything goes to plan.", "I started looking for it last year.", "It wasn't easy to find, you know."),
                Choice(6, "Choose the correct reply.", "A", "\"Do you happen to know whether ...?\" asks for information; \"I'm not sure — you could check their website\" answers it.",
                    "I'm not sure — you could check their website.", "Yes, I happened to read that book.", "I usually borrow novels and magazines."),
                Choice(7, "Choose the correct reply.", "B", "\"I can't believe how much they're charging\" is a complaint about price; \"I know — I nearly didn't buy one\" agrees.",
                    "The concert starts at eight o'clock.", "I know — I nearly didn't buy one.", "They're sold at the main entrance."),
                Choice(8, "Choose the correct reply.", "B", "\"How about grabbing a bite to eat?\" is an invitation, accepted with \"Sounds great\".",
                    "I'd rather not do anything at work.", "Sounds great — did you have anywhere in mind?", "I grabbed it just before it fell."),
            ]),
        ]);

    private static ListeningPart ListeningPart2()
    {
        const string who = "Library manager";
        return new(2,
            "You will hear the manager of a new city library talking to visitors on its opening day.",
            [
                Female(who, "Good morning, everyone, and welcome to the Central City Library on its very first day. My name is Dilnoza Karimova, and I'm the manager here. Before you start exploring, I'd like to give you some practical information."),
                Female(who, "First of all, the library isn't in the old building on Navoi Street any more. We've moved into the new cultural centre, and we occupy the whole of the third floor. The second floor belongs to the art gallery, so please don't get out of the lift there by mistake."),
                Female(who, "Membership is free for everyone who lives in the city. When you register, you'll need to show your passport. You don't need to bring a photograph, because we'll take one for your card at the desk. Once you're a member, adults can borrow up to twelve books at a time and keep them for three weeks. Children can take six. DVDs and audiobooks are very popular, so there's a limit of four of those."),
                Female(who, "We've got something for every age group. For younger visitors, there's a children's reading club. Last year it met on Sunday afternoons in the old building, but a lot of parents told us that didn't suit them, so now it takes place every Saturday morning from ten to twelve."),
                Female(who, "If you need a quiet place to work, you'll love our silent study room. It has forty desks, and every one of them has a socket for your laptop. There's also a group study room with twenty seats, where you're allowed to talk, but please keep your voices down."),
                Female(who, "Finally, I'm pleased to announce that from next month we'll be holding evening talks by local authors. The first one is on the fifth of November. It's free, but you'll need to book a seat at the information desk."),
                Female(who, "Right, if you have any questions, my colleagues in the green T-shirts will be happy to help. Enjoy your visit!"),
            ],
            [
                new("gap", L2, 2,
                [
                    Item(9, "The library is on the ___ floor of the new cultural centre.", "\"We occupy the whole of the third floor.\"", "third", "3rd"),
                    Item(10, "To register, visitors must show their ___.", "\"When you register, you'll need to show your passport.\"", "passport"),
                    Item(11, "Adults can borrow up to ___ books at a time.", "\"Adults can borrow up to twelve books at a time.\"", "twelve", "12"),
                    Item(12, "The children's reading club now meets every ___ morning.", "\"Now it takes place every Saturday morning.\"", "Saturday"),
                    Item(13, "The silent study room has ___ desks.", "\"It has forty desks.\"", "forty", "40"),
                    Item(14, "From next month there will be evening talks by local ___.", "\"We'll be holding evening talks by local authors.\"", "authors"),
                ]),
            ]);
    }

    private static ListeningPart ListeningPart3() => new(3,
        "You will hear four people talking about learning a foreign language online.",
        [
            Male("Speaker 1", "To be honest, I'd never been interested in languages at school. But two years ago my company started working with a partner in Germany, and my manager made it clear that anyone who wanted a promotion would need at least basic German. So I signed up for an online course. It wasn't cheap, but the company paid for it. I studied for half an hour every evening, usually on the metro on the way home. I'm still not fluent, but I can now read emails from our German colleagues and follow video calls without feeling completely lost."),
            Female("Speaker 2", "I started learning Korean with an app because I could study whenever I liked. For the first few weeks I was really enthusiastic. The problem was that nobody noticed whether I did my lessons or not. There was no one to check my progress and no one to tell me I was doing well. Gradually I started skipping days, then whole weeks. In the end a friend of mine agreed to study with me, and knowing that she would ask me about my lessons every Sunday made all the difference."),
            Male("Speaker 3", "I've never taken a proper course in my life. My English comes almost entirely from watching films and series and listening to music. At first I used subtitles in Uzbek, then I switched to English subtitles, and eventually I turned them off completely. I also used to write down the lyrics of songs I liked and look up every word I didn't know. People are sometimes surprised that I learned this way, but honestly, it never felt like studying, and that's probably why I kept doing it for so many years."),
            Female("Speaker 4", "When I decided to learn Spanish, I downloaded all the popular apps, but I soon realised they were teaching me words, not conversation. So I joined a language exchange website. Now I talk twice a week on video with a woman from Madrid. I help her with her English, and she corrects my Spanish. My progress has been slow, which is what I expected, because grammar is still my weak point. But I can finally hold a real conversation, and I've made a good friend too."),
        ],
        [
            new("match", L3, null,
            [
                Item(15, "Speaker 1", "Speaker 1: \"anyone who wanted a promotion would need at least basic German\".", "C"),
                Item(16, "Speaker 2", "Speaker 2: \"nobody noticed whether I did my lessons or not ... I started skipping days\".", "F"),
                Item(17, "Speaker 3", "Speaker 3: \"My English comes almost entirely from watching films and series and listening to music\".", "E"),
                Item(18, "Speaker 4", "Speaker 4: \"I talk twice a week on video with a woman from Madrid\".", "B"),
            ],
            [
                "I made faster progress than I had expected.",
                "I practised by chatting online with a native speaker.",
                "I needed the language for my work.",
                "I regret paying for an expensive course.",
                "I learned a lot from films and songs.",
                "I found it hard to stay motivated when I studied alone.",
            ]),
        ]);

    private static ListeningPart ListeningPart4()
    {
        const string who = "Park guide";
        var map = new MapPlan("Navruz Park",
            [new("Entrance", 2, 4), new("Car park", 0, 4), new("Lake", 2, 2)],
            [
                new("A", 1, 4), new("B", 4, 4), new("C", 0, 2), new("D", 1, 1),
                new("E", 3, 2), new("F", 2, 0), new("G", 4, 0), new("H", 3, 3),
            ]);
        return new(4,
            "You will hear a guide describing the new Navruz Park to a group of visitors standing at the main entrance.",
            [
                Male(who, "Hello, everyone, and welcome to Navruz Park. We're standing at the main entrance, which is in the middle of the south side of the park, so when you look straight ahead, you're looking north. Let me explain where everything is, and then you're free to explore."),
                Male(who, "Just on your left as you come through the gate, between the entrance and the car park, you'll see the picnic area, with plenty of tables and benches."),
                Male(who, "If you walk straight ahead along the main path towards the lake, the first building you'll come to is the café. It's on your right, halfway between the entrance and the lake, so it's a good place to stop for a drink before you go any further."),
                Male(who, "The lake itself is right in the centre of the park. Families with small children usually head for the playground. To get there, walk up to the lake, then turn left and follow the path all the way to the western edge of the park. The playground is there, next to the fence, level with the lake. On the other side of the lake, on the eastern shore, is the boat hire. Boats are available from ten o'clock."),
                Male(who, "For those of you who like sport, the tennis courts are in the far north-east corner of the park, which is the corner furthest from the car park. You can book a court online."),
                Male(who, "Now, one of the most beautiful parts of the park is the rose garden. Just keep going straight along the main path, past the lake, and you'll find it at the far end, in the middle of the northern side. Please don't pick the flowers!"),
                Male(who, "Between the playground and the rose garden, a little to the north-west of the lake, there's a bicycle hire point."),
                Male(who, "Finally, if you need a paper map or have any questions, go to the information centre. From the entrance, turn right and walk along the southern fence until you reach the corner. It's the building in the south-east corner of the park. Enjoy your afternoon!"),
            ],
            [
                new("map", L4, null,
                [
                    Item(19, "Café", "\"It's on your right, halfway between the entrance and the lake.\"", "H"),
                    Item(20, "Playground", "\"Turn left and follow the path all the way to the western edge ... level with the lake.\"", "C"),
                    Item(21, "Tennis courts", "\"In the far north-east corner of the park ... furthest from the car park.\"", "G"),
                    Item(22, "Rose garden", "\"Past the lake ... at the far end, in the middle of the northern side.\"", "F"),
                    Item(23, "Information centre", "\"It's the building in the south-east corner of the park.\"", "B"),
                ], Map: map),
            ]);
    }

    private static ListeningPart ListeningPart5() => new(5,
        "You will hear three extracts about technology in everyday life.",
        [
            Male("Narrator", "Extract One."),
            Male("Aziz", "Is that your new smartwatch, Malika? How are you finding it?"),
            Female("Malika", "Honestly? I'm not sure it's good for me. It counts my steps, my sleep, my heart rate, and I've started checking it all the time. If I don't reach ten thousand steps, I feel as if I've failed the whole day. Last night I woke up at three, and the first thing I did was look at my sleep score!"),
            Male("Aziz", "That sounds exhausting. But the watch isn't really the problem. It's all those messages it keeps sending you. Why don't you switch off the notifications and just look at the numbers once a week? That's what I do with mine."),
            Female("Malika", "Once a week... that might actually work. I don't really want to stop wearing it anyway. It was a birthday present from my brother."),
            Male("Narrator", "Extract Two."),
            Female("Presenter", "Rustam, your family has made ceramic bowls in Rishton for three generations. Why did you decide to start selling online?"),
            Male("Rustam", "People assume it was about money, but actually our local sales were fine. The real reason was that tourists kept writing to us after they'd gone home, asking how they could order more. We were turning away customers in other countries simply because we had no way of sending them anything."),
            Female("Presenter", "And how has social media changed things for you?"),
            Male("Rustam", "It's been essential. Most of our orders now come from people who've seen short videos of us painting. But I won't pretend I enjoy it. Filming, editing, answering messages late at night... some weeks I spend more time on my phone than at the potter's wheel, and that does worry me."),
            Male("Narrator", "Extract Three."),
            Male("Speaker", "Last month I did something I'd been putting off for years: I spent a whole weekend in the mountains without my phone. I'd expected to feel anxious, but what surprised me most was how much time I suddenly had. Without constantly checking messages, a single afternoon felt twice as long. I read a whole novel, I chatted with the family who ran the guesthouse, and I actually looked at the scenery instead of photographing it. I'm not going to pretend I've given up my phone since then. I need it for work, after all. But I've made one rule at home: no screens during meals. If I can manage a whole weekend, I can certainly manage half an hour at dinner."),
        ],
        [
            new("mcq", L5, null,
            [
                Choice(24, "How does Malika feel about her smartwatch?", "A", "\"I've started checking it all the time ... I feel as if I've failed the whole day.\"",
                    "It makes her worry too much about her daily numbers.", "It has helped her to sleep much better.", "It was a disappointing present."),
                Choice(25, "What does Aziz suggest?", "B", "\"Why don't you switch off the notifications and just look at the numbers once a week?\"",
                    "returning the watch to the shop", "changing the settings on the watch", "buying a different model"),
                Choice(26, "Why did Rustam start selling online?", "B", "\"Tourists kept writing to us after they'd gone home, asking how they could order more.\"",
                    "His sales in the local area had fallen.", "Customers who had visited wanted to buy from abroad.", "A tourist offered to build a website for him."),
                Choice(27, "What is Rustam's attitude to social media?", "C", "\"It's been essential ... some weeks I spend more time on my phone than at the potter's wheel.\"",
                    "He finds it enjoyable and relaxing.", "He thinks it has had little effect on his business.", "He values it but feels it takes time away from his craft."),
                Choice(28, "What surprised the speaker most about his weekend?", "B", "\"What surprised me most was how much time I suddenly had.\"",
                    "how anxious he felt without his phone", "how much free time he seemed to have", "how friendly the local people were"),
                Choice(29, "What has the speaker done since the weekend?", "C", "\"I've made one rule at home: no screens during meals.\"",
                    "He has stopped using his phone for work.", "He has booked another trip to the mountains.", "He has made a small change to his daily routine."),
            ]),
        ]);

    private static ListeningPart ListeningPart6()
    {
        const string who = "Lecturer";
        return new(6,
            "You will hear part of a university lecture about how sleep affects learning.",
            [
                Female(who, "Good afternoon. Today I'd like to look at a question that matters to every student in this room: what actually happens to the things we learn when we go to sleep? For a long time, sleep was seen as a kind of pause, a period when the brain simply switched off. We now know that this picture is completely wrong."),
                Female(who, "While we sleep, the brain is extremely busy. One of its most important jobs is to process the information we took in during the day. New facts are first stored in a small area of the brain called the hippocampus, which works rather like a temporary notebook. During deep sleep, this information is replayed and gradually transferred to the cortex, where it becomes part of our long-term memory. In other words, learning isn't really finished when you close your book. Part of it happens at night."),
                Female(who, "The evidence for this is quite strong. In one well-known type of experiment, two groups of students learn a list of new words. One group learns them in the morning and is tested in the evening, after a normal day awake. The other group learns them in the evening and is tested the next morning, after a night's sleep. Although the time between learning and testing is exactly the same, the students who have slept typically remember around forty per cent more."),
                Female(who, "Sleep doesn't only help us remember; it also helps us see connections. In some studies, people were given a mathematical problem that contained a hidden shortcut. Those who slept before trying again were far more likely to discover the shortcut than those who stayed awake. This suggests that sleep helps the brain to reorganise information, which may be why we sometimes wake up with the solution to a problem that seemed impossible the night before."),
                Female(who, "So why do so many young people get too little sleep? Part of the answer is biological. During the teenage years the body clock shifts, so teenagers naturally start to feel sleepy later in the evening, often not until eleven o'clock or even midnight. Unfortunately, school and university timetables don't change, so the alarm still goes off at the same time."),
                Female(who, "Technology makes the situation worse. The blue light produced by screens tells the brain that it is still daytime, and this delays the release of melatonin, the hormone that makes us feel ready for sleep. Scrolling through your phone in bed, therefore, is one of the worst things you can do the night before an exam."),
                Female(who, "What about naps? Research suggests that a short nap of around twenty minutes can improve alertness and concentration in the afternoon. Longer naps, however, can take us into deep sleep, and waking up from that often leaves us feeling worse, not better."),
                Female(who, "To finish, then, what practical advice can I give you? Many students believe the answer is simply to sleep longer at weekends. In fact, the single most useful habit is a regular routine: going to bed and getting up at roughly the same time every day, even on Saturdays. It sounds boring, I know, but your memory will thank you for it. Next week we'll look at the effects of diet on concentration."),
            ],
            [
                new("gap", L6, 1,
                [
                    Item(30, "The hippocampus works rather like a temporary ___.", "\"The hippocampus, which works rather like a temporary notebook.\"", "notebook"),
                    Item(31, "In deep sleep, information moves to the cortex and becomes part of long-term ___.", "\"Transferred to the cortex, where it becomes part of our long-term memory.\"", "memory"),
                    Item(32, "Students who sleep after learning remember about ___ per cent more words.", "\"The students who have slept typically remember around forty per cent more.\"", "forty", "40"),
                    Item(33, "People who slept were more likely to find a hidden ___ in a maths problem.", "\"Those who slept ... were far more likely to discover the shortcut.\"", "shortcut"),
                    Item(34, "Blue light from ___ makes the brain think it is still daytime.", "\"The blue light produced by screens tells the brain that it is still daytime.\"", "screens"),
                    Item(35, "The most useful habit for good sleep is a regular ___.", "\"The single most useful habit is a regular routine.\"", "routine"),
                ]),
            ]);
    }

    // =====================================================================
    // READING
    // =====================================================================

    private static ReadingPassage ReadingPart1() => new("Clean-up day in Navruz Park",
        """
        Dear volunteers,

        Thank you for signing up for our spring clean-up day in Navruz Park this Saturday. More than sixty people have joined us this year, which is the largest number we have ever had. We are sure the park will look wonderful by the evening.

        Please arrive at the main gate at 9 a.m. When you arrive, you will be put into a small team with five or six other people. Each (1) ______ will have its own leader and will be responsible for one area of the park, such as the playground, the paths or the area around the lake.

        We will provide rubbish bags, rakes and other tools, but unfortunately we do not have enough gloves for everyone. If you have your own (2) ______, please bring them with you.

        There is nowhere to buy drinks inside the park, so bring a large bottle of water. Remember to drink plenty of (3) ______ during the day, especially if it is sunny.

        The weather forecast says it may rain in the afternoon. If the (4) ______ is heavy, we will finish at 1 p.m. instead of 3 p.m. and send everyone home early.

        Children are very welcome, but they must stay with an adult at all times. Please make sure they do not go near the (5) ______, as the water there is quite deep.

        At the end of the day, we will all meet again at the main (6) ______ for a photo together and a picnic lunch.

        If you can no longer come, please reply to this email by Thursday so that we can plan the teams again.

        Best wishes,
        Kamola Yusupova
        Event coordinator
        """,
        [
            new("gap", R1, 1,
            [
                Item(1, "Each ___ will have its own leader and will be responsible for one area of the park, such as the playground, the paths or the area around the lake.", "\"You will be put into a small team\" — each team has \"its own leader\".", "team"),
                Item(2, "If you have your own ___, please bring them with you.", "\"We do not have enough gloves for everyone.\"", "gloves"),
                Item(3, "Remember to drink plenty of ___ during the day, especially if it is sunny.", "\"Bring a large bottle of water.\"", "water"),
                Item(4, "If the ___ is heavy, we will finish at 1 p.m. instead of 3 p.m. and send everyone home early.", "\"The weather forecast says it may rain in the afternoon.\"", "rain"),
                Item(5, "Please make sure they do not go near the ___, as the water there is quite deep.", "Only \"the area around the lake\" has deep water.", "lake"),
                Item(6, "At the end of the day, we will all meet again at the main ___ for a photo together and a picnic lunch.", "\"Please arrive at the main gate at 9 a.m.\" — they meet there \"again\".", "gate"),
            ]),
        ]);

    private static ReadingPassage ReadingPart2() => new("Summer at the Youth Centre",
        """
        7. Photography for Beginners. Discover how to take better pictures in this eight-week course, held every Tuesday evening. You will learn about light, composition and editing, and each week we will visit a different part of the city to practise. Please note that the centre cannot lend cameras, so you will need to have your own; a simple digital camera or a good phone is fine. There is no exam or certificate: the aim is simply to enjoy photography.

        8. Pottery Workshop. Learn to make your own bowls and cups on a real potter's wheel with ceramic artist Nodira Alieva, who trained in Rishton. Clay, paints and aprons are all included in the price. Because our studio has only five wheels, we can accept just ten students per course, working in pairs, so early booking is strongly recommended. Classes run on Thursday evenings from 6 to 8 p.m.

        9. Sunrise Running Club. Want to start your day with energy? Join our friendly running group, which meets at the park gates at 6 a.m. every Monday, Wednesday and Friday. We run for about forty minutes at an easy pace, so everyone can keep up, and we always finish in good time for breakfast and the working day. Runners of all levels are welcome, from people who have never run before to experienced marathon runners.

        10. Coding Club Online. This club introduces teenagers to programming through fun projects such as simple games and websites. All sessions are held live on the internet on Tuesday and Thursday evenings, so members join from their own bedrooms. If you do not have a laptop, you can borrow one from the centre for the whole summer. Our tutors are university students who still remember how difficult their first lines of code were.

        11. Chess Club. Whether you are a beginner or a strong player, the chess club is a great place to train your mind and make friends. We meet in the library reading room every weekday from 3 to 5 p.m. Anyone with a valid school or university card plays without paying; other adults pay a small monthly fee of 20,000 som. Boards and clocks are provided, and there are always plenty of them.

        12. Young Actors' Workshop. Over six Friday afternoons, young people aged 12 to 16 will work with a professional director to create a short play. Participants help to write the script, design the costumes and build the set. In the final week, the group will perform the play on the main stage in front of families and friends. No acting experience is needed, just enthusiasm and the courage to try something new.

        13. Cooking Around the World. Every Saturday and Sunday this summer, our chef will teach you to prepare dishes from a different country, from Italian pasta to Japanese rice dishes. There are no classes during the week, as the kitchen is used by the centre's café from Monday to Friday. All ingredients are provided, and at the end of each session you can eat what you have cooked or take it home to your family.

        14. English Conversation Café. Improve your speaking in a relaxed atmosphere over a cup of tea. Every Wednesday evening, participants discuss a different topic, from travel to technology, with the help of a volunteer host. Please note that this is not for people starting from zero: you should already be able to hold a simple conversation in English. Part of each evening is spent talking in pairs, so feel free to bring a friend.
        """,
        [
            new("match", R2, null,
            [
                Item(7, "Text 7", "\"The centre cannot lend cameras, so you will need to have your own.\"", "A"),
                Item(8, "Text 8", "\"We can accept just ten students per course.\"", "H"),
                Item(9, "Text 9", "\"Meets at the park gates at 6 a.m. ... in good time for breakfast and the working day.\"", "J"),
                Item(10, "Text 10", "\"All sessions are held live on the internet ... members join from their own bedrooms.\"", "E"),
                Item(11, "Text 11", "\"Anyone with a valid school or university card plays without paying.\"", "B"),
                Item(12, "Text 12", "\"The group will perform the play on the main stage in front of families and friends.\"", "G"),
                Item(13, "Text 13", "\"Every Saturday and Sunday ... There are no classes during the week.\"", "C"),
                Item(14, "Text 14", "\"This is not for people starting from zero.\"", "D"),
            ],
            [
                "Participants must bring their own equipment.",
                "This activity is free for students.",
                "Sessions take place only at weekends.",
                "Complete beginners cannot join.",
                "Participants can take part without leaving home.",
                "Part of the fee is given to charity.",
                "Participants will show their work to an audience at the end.",
                "Only a small number of people can join.",
                "Participants receive a certificate at the end.",
                "The activity starts before most people go to work.",
            ]),
        ]);

    private static ReadingPassage ReadingPart3() => new("Old crafts, new designs",
        """
        15. Only fifteen years ago, many people believed that traditional crafts such as hand-painted ceramics, silk weaving and embroidery had little future. Workshops that had been run by the same families for generations were closing, and young people were leaving for jobs in the cities. Few would have predicted what happened next. Today, some of the most exciting products in design shops, from lamps and cushions to phone cases, are made using techniques that are hundreds of years old, and many of them are created by designers still in their twenties.

        16. Part of the explanation lies in the way people shop. After years of buying the same mass-produced items as everyone else, many customers have begun to look for objects that feel personal. A bowl that was shaped and painted by hand is never exactly like any other, and small irregularities, once seen as faults, are now valued as proof of human work. Buyers also want to know where their things come from: who made them, in which village, and with what materials. A handmade object, in other words, comes with a story.

        17. The most successful projects are usually partnerships. A young designer may suggest new shapes, lighter colours or a modern use for an old pattern, while an experienced craftsman provides the technical skill that can only be gained through decades of practice. Both sides say they benefit from the relationship. "I thought I was coming to teach them about modern taste," one designer explained, "but I have learned more about materials in one year with the masters than I did in four years at university." The masters, in turn, discover markets they had never imagined.

        18. However, the revival faces a serious difficulty. A hand-embroidered wall hanging can take a skilled worker several months to complete, and a set of painted plates may pass through a dozen stages before it is ready. Such work simply cannot be hurried, and its price has to reflect the hours involved. When a factory can print a similar pattern on fabric in seconds, many shoppers find it hard to understand why the handmade version costs twenty times as much.

        19. The internet has done much to ease this problem. In the past, a workshop in a small town depended on local buyers and on the few visitors who happened to pass by. Now a family of weavers can open an online shop and sell directly to customers in Tokyo or Toronto. Short videos showing the making process are especially popular, because they help buyers see the hours of work behind each item. As a result, for many workshops, tourists now account for only a small share of sales.

        20. Whether this success will last depends largely on young people. Mastering a craft takes years, and it will only attract new learners if it offers a reasonable income. Encouragingly, several workshops have started paid apprenticeships, and some art colleges now invite master craftsmen to teach short courses. If these efforts continue, skills that almost disappeared a generation ago may not only survive but develop in ways that earlier masters could never have imagined.
        """,
        [
            new("match", R3, null,
            [
                Item(15, "Paragraph 15", "\"Few would have predicted what happened next\" — old techniques are back in design shops.", "C"),
                Item(16, "Paragraph 16", "\"Buyers also want to know where their things come from ... comes with a story.\"", "G"),
                Item(17, "Paragraph 17", "\"Both sides say they benefit from the relationship.\"", "E"),
                Item(18, "Paragraph 18", "\"Such work simply cannot be hurried, and its price has to reflect the hours involved.\"", "A"),
                Item(19, "Paragraph 19", "\"A family of weavers can open an online shop and sell directly to customers in Tokyo or Toronto.\"", "H"),
                Item(20, "Paragraph 20", "\"Several workshops have started paid apprenticeships ... skills ... may not only survive.\"", "D"),
            ],
            [
                "Why handmade goods cannot be cheap",
                "Tourists as the main customers",
                "An unexpected comeback",
                "Keeping skills alive for the next generation",
                "Knowledge that flows in both directions",
                "Protecting traditional designs from copying",
                "Customers who want a story",
                "Selling to the world from a small workshop",
            ]),
        ]);

    private static ReadingPassage ReadingPart4() => new("The home office: what have we learned?",
        """
        When offices around the world suddenly closed in 2020, millions of people discovered that much of their work could be done from a kitchen table. At the time, many experts predicted that the traditional office would soon become a thing of the past. Several years later, the picture is more complicated. Some companies have brought their staff back full-time, others have given up their offices altogether, and a large number have settled somewhere in between.

        For many employees, the most obvious benefit of working from home is the time saved on travel. In large cities, a journey of an hour or more in each direction is common, and giving it up can free up ten hours a week, the equivalent of an extra working day. Surveys consistently show that workers value this more than almost any other advantage, and some say they would accept a lower salary rather than return to commuting every day.

        Employers, too, have found unexpected advantages. A company that does not require its staff to live nearby can recruit from a much wider area, and even from other countries. Small businesses in particular have been able to hire specialists they could never have attracted before. Some firms have also reduced their costs by moving to smaller offices, although the savings are often lower than expected, since the space that remains has to be redesigned for meetings rather than for individual desks.

        Yet working from home has its critics, and not only among managers. Many younger employees, especially those in their first jobs, say they miss the chance to learn by watching more experienced colleagues. Much of what we learn at work is never formally taught: how to deal with a difficult client, how to present an idea in a meeting, even when to stay silent. These lessons are easy to pick up when you sit next to someone, but much harder to absorb through a screen. One study of software engineers found that those who worked in the same room as their team received significantly more comments on their work than those who worked remotely, although the difference was smallest among senior staff.

        There are also concerns about wellbeing. For some people, the boundary between work and private life has almost disappeared. When the office is also the living room, it can be difficult to stop checking emails in the evening, and some workers feel they must prove they are busy by replying instantly. Loneliness is another problem, particularly for people who live alone. Having said that, others report the opposite experience: fewer interruptions, more time with their families and a greater sense of control over their day.

        Faced with such mixed evidence, most organisations have chosen what is known as a hybrid model, in which employees spend part of the week in the office and part at home. In theory, this offers the best of both worlds. In practice, it only works well when it is carefully planned. If team members choose their office days independently, a person may travel for an hour only to spend the day on video calls with colleagues who stayed at home. For this reason, many companies now ask whole teams to come in on the same days, and use that time for activities that genuinely benefit from being together, such as training, planning and solving problems as a group.

        What seems clear is that there is no single answer that suits every job or every person. A designer who needs long periods of concentration may be more productive at home, while a new sales assistant may learn far more in the office. The real lesson of recent years may be that the important question is not where people work, but how well the work, and the people doing it, are organised.
        """,
        [
            new("mcq", R4Mcq, null,
            [
                Choice(21, "According to the second paragraph, what do surveys show?", "B", "\"Workers value this more than almost any other advantage\" — \"this\" is the time saved on travel.",
                    "Most workers would like to work an extra day each week.", "Workers see not having to travel as one of the biggest benefits.", "Workers usually earn more when they work from home.", "Workers spend the time they save with their families."),
                Choice(22, "What does the writer say about companies that moved to smaller offices?", "C", "\"The savings are often lower than expected, since the space that remains has to be redesigned.\"",
                    "They have saved more money than they had predicted.", "They no longer need any space for meetings.", "Their savings have been reduced by the need to change the space.", "They have found it harder to recruit specialists."),
                Choice(23, "The study of software engineers is mentioned to show that", "B", "\"Those who worked in the same room as their team received significantly more comments on their work.\"",
                    "senior staff prefer to work in the office.", "working remotely can reduce the guidance people receive.", "remote workers are more productive than office workers.", "engineers need less feedback than other employees."),
                Choice(24, "What is the writer's main point about the hybrid model?", "D", "\"In practice, it only works well when it is carefully planned.\"",
                    "It is the best solution for every kind of job.", "It has been rejected by most organisations.", "It makes employees spend too long travelling.", "It succeeds only if office days are organised carefully."),
            ]),
            new("tfng", R4Tfng, null,
            [
                Item(25, "In 2020, many experts expected the traditional office to continue without much change.", "\"Many experts predicted that the traditional office would soon become a thing of the past.\"", "FALSE"),
                Item(26, "Small businesses have benefited from being able to employ people who live far away.", "\"Small businesses in particular have been able to hire specialists they could never have attracted before.\"", "TRUE"),
                Item(27, "Older workers feel lonely at home more often than younger workers.", "The text says loneliness affects \"people who live alone\" but compares nothing by age.", "NOT GIVEN"),
                Item(28, "Everyone who works from home finds it harder to separate work from private life.", "\"Others report the opposite experience: fewer interruptions ... a greater sense of control.\"", "FALSE"),
                Item(29, "Many companies now ask members of the same team to come to the office on the same days.", "\"Many companies now ask whole teams to come in on the same days.\"", "TRUE"),
            ]),
        ]);

    private static ReadingPassage ReadingPart5() => new("Cooling the city: the science of urban trees",
        """
        On a hot summer afternoon, the centre of a large city can be several degrees warmer than the countryside only a few kilometres away. This effect, known as the urban heat island, was first described in the early nineteenth century, when an amateur scientist noticed that London was consistently warmer than the villages around it. Today it is one of the most widely studied problems in urban planning, and interest in it is growing as summers become hotter.

        The causes of the urban heat island are well understood. Materials such as concrete, asphalt and brick absorb large amounts of energy from the sun during the day and release it slowly at night. As a result, cities not only reach higher temperatures in the afternoon but also cool down much more slowly after sunset. Tall buildings make the problem worse by trapping warm air in narrow streets, while air conditioners, cars and factories add further heat. In some cities, night-time temperatures in the centre can be as much as 7°C higher than in nearby rural areas.

        These differences are not merely uncomfortable. Hot nights prevent the human body from recovering from the heat of the day, and health researchers have linked long periods of high night-time temperatures to an increase in hospital admissions, particularly among elderly people and young children. Higher temperatures also raise energy consumption, as more people switch on air conditioning, which in turn releases more heat into the streets outside.

        Of all the solutions that have been proposed, trees are among the most effective. They cool the air in two main ways. First, their leaves provide shade, preventing sunlight from reaching roads, pavements and walls. Measurements show that a surface in the shade of a large tree can be 20°C cooler than the same surface in full sun. Second, trees release water vapour through their leaves, a process called transpiration. Because turning water into vapour requires energy, this process takes heat from the surrounding air, in much the same way that sweating cools the human body. A single mature tree can release several hundred litres of water on a hot day.

        However, not all trees are equally useful. Species with wide, dense crowns provide far more shade than narrow ones, and trees need enough space for their roots and enough water to stay healthy. In dry climates, choosing species that can survive with little watering is essential; otherwise, the cost of irrigation can become a serious burden for city budgets. Location matters, too. Research suggests that trees planted along streets and around car parks, where the most heat is absorbed, bring greater benefits than the same number of trees concentrated in a single large park.

        Planners must also think in terms of decades rather than years. A young tree provides little shade, and it may take fifteen to twenty years before it reaches its full cooling potential. For this reason, many experts argue that protecting existing mature trees is at least as important as planting new ones. Cutting down a large old tree to widen a road and replacing it with several young ones may look like a fair exchange on paper, but it can leave a street without proper shade for a generation.

        Trees alone cannot solve the problem of urban heat. Light-coloured roofs, fountains and better building design all have a part to play. Nevertheless, the evidence suggests that a well-planned network of trees is one of the most cost-effective ways to keep cities cool, while also providing cleaner air, homes for birds and more pleasant places for people to walk.
        """,
        [
            new("gap", R5Gap, 2,
            [
                Item(30, "The urban heat island was first noticed in the early nineteenth century by an amateur scientist who compared ___ with the villages around it.", "\"An amateur scientist noticed that London was consistently warmer than the villages around it.\"", "London"),
                Item(31, "City materials such as concrete store the sun's energy during the day and give it back slowly at ___.", "\"Absorb large amounts of energy from the sun during the day and release it slowly at night.\"", "night"),
                Item(32, "Apart from giving shade, trees cool the air by releasing water vapour, a process known as ___.", "\"Trees release water vapour through their leaves, a process called transpiration.\"", "transpiration"),
                Item(33, "Because young trees give little shade, many experts believe it is just as important to protect existing ___ trees.", "\"Protecting existing mature trees is at least as important as planting new ones.\"", "mature"),
            ]),
            new("mcq", R5Mcq, null,
            [
                Choice(34, "According to the text, why are hot nights a health concern?", "B", "\"Hot nights prevent the human body from recovering from the heat of the day.\"",
                    "They make people use less energy.", "The body cannot recover from the heat of the day.", "They mainly affect people who work outdoors.", "They increase air pollution in city centres."),
                Choice(35, "What does the writer suggest about where trees should be planted?", "C", "\"Trees planted along streets and around car parks, where the most heat is absorbed, bring greater benefits.\"",
                    "Most new trees should be planted together in one large park.", "Trees should only be planted in cities with a wet climate.", "Trees work best in places where surfaces absorb the most heat.", "Narrow trees are the best choice for busy streets."),
            ]),
        ]);
}
