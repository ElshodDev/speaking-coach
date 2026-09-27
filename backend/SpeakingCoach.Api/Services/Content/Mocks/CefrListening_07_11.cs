using SpeakingCoach.Api.Services.Mock;
using static SpeakingCoach.Api.Services.Mock.CefrObjective;

namespace SpeakingCoach.Api.Services.Content;

// CEFR (Multilevel) Listening, 7–11-testlar. Original mualliflik matnlari;
// tuzilishi BuiltInCefr.Listening1 bilan bir xil (8 + 6 + 4 + 5 + 6 + 6).
public static partial class BuiltInCefr
{
    public static readonly ListeningTest Listening7 = new([Cl07P1(), Cl07P2(), Cl07P3(), Cl07P4(), Cl07P5(), Cl07P6()]);
    public static readonly ListeningTest Listening8 = new([Cl08P1(), Cl08P2(), Cl08P3(), Cl08P4(), Cl08P5(), Cl08P6()]);
    public static readonly ListeningTest Listening9 = new([Cl09P1(), Cl09P2(), Cl09P3(), Cl09P4(), Cl09P5(), Cl09P6()]);
    public static readonly ListeningTest Listening10 = new([Cl10P1(), Cl10P2(), Cl10P3(), Cl10P4(), Cl10P5(), Cl10P6()]);
    public static readonly ListeningTest Listening11 = new([Cl11P1(), Cl11P2(), Cl11P3(), Cl11P4(), Cl11P5(), Cl11P6()]);

    // =====================================================================
    // TEST 7
    // =====================================================================

    private static ListeningPart Cl07P1() => new(1,
        "You will hear eight people speaking in everyday situations. Choose the best reply to each one.",
        [
            Female("Shop assistant", "Would you like to pay by card or in cash today?"),
            Male("Friend", "Sorry I'm late — the traffic on the ring road was absolutely terrible."),
            Female("Teacher", "Could you all hand in your essays by Friday at the latest?"),
            Male("Waiter", "Are you ready to order, or would you like a few more minutes?"),
            Female("Neighbour", "Have you heard that they're building a new sports centre near the school?"),
            Male("Colleague", "Do you think you could cover my shift on Saturday? I've got a family wedding."),
            Female("Stranger on a bus", "Excuse me, is anyone sitting here?"),
            Male("Friend", "You look exhausted — were you up late revising again?"),
        ],
        [
            new("mcq", L1, null,
            [
                Choice(1, "Choose the correct reply.", "B", "\"Card or in cash?\" offers a choice, so the reply chooses one: \"By card, please.\"",
                    "Yes, I'd like to pay.", "By card, please.", "It costs too much."),
                Choice(2, "Choose the correct reply.", "A", "\"Sorry I'm late\" is an apology; \"Don't worry\" accepts it.",
                    "Don't worry — we haven't ordered yet.", "Yes, I took the ring road too.", "I'll be there on time."),
                Choice(3, "Choose the correct reply.", "C", "\"Could you ... hand in your essays by Friday?\" is a request, agreed to with \"Of course\".",
                    "I handed it to him yesterday.", "Friday is my favourite day.", "Of course — I've nearly finished mine."),
                Choice(4, "Choose the correct reply.", "A", "\"Would you like a few more minutes?\" is answered by \"Just a couple more minutes, please.\"",
                    "Just a couple more minutes, please.", "I ordered it last week.", "Yes, it was delicious."),
                Choice(5, "Choose the correct reply.", "B", "\"Have you heard that ...?\" shares news; \"Really? That's great news\" reacts to it.",
                    "No, I built it myself.", "Really? That's great news for the kids.", "I've been there many times."),
                Choice(6, "Choose the correct reply.", "C", "\"Do you think you could cover my shift?\" asks a favour; \"I should be able to\" answers it.",
                    "The wedding was lovely, wasn't it?", "I'm not sure the shift is covered.", "I should be able to — let me just check my plans."),
                Choice(7, "Choose the correct reply.", "A", "\"Is anyone sitting here?\" — \"No, go ahead\" lets the person sit down.",
                    "No, go ahead.", "Yes, I'm standing.", "I sat there yesterday."),
                Choice(8, "Choose the correct reply.", "B", "\"Were you up late revising?\" is confirmed with \"I'm afraid so — the exam's tomorrow.\"",
                    "Yes, I'm revising the plan.", "I'm afraid so — the exam's tomorrow.", "No, I'm looking forward to it."),
            ]),
        ]);

    private static ListeningPart Cl07P2()
    {
        const string who = "Transport officer";
        return new(2,
            "You will hear an officer from the city transport department talking about a new bike-sharing scheme.",
            [
                Male(who, "Good evening, everyone, and thank you for coming. My name is Bekzod Tursunov, and I work for the city transport department. Tonight I'm going to explain how our new bike-sharing scheme will work when it starts on the first of May."),
                Male(who, "In the first stage, we're putting 350 bikes at forty stations around the city centre. If the scheme is a success, we hope to add another 200 bikes in the suburbs next spring. Most of the stations are right next to metro exits and bus stops, so it'll be easy to combine a bike ride with public transport. The bikes themselves are dark green, but you'll recognise the stations by their bright orange signs."),
                Male(who, "To use the bikes, you'll need to download our app and register. You don't need to show a passport or any other document. You simply register with your phone number, and we send you a code by text message. You will need a bank card to top up your account, though."),
                Male(who, "Now, the price. A single ride costs 3,000 som for the first thirty minutes. After that, you pay 1,000 som for every extra fifteen minutes. Students and pensioners can buy a monthly pass for 40,000 som, which gives them unlimited rides of up to forty-five minutes each."),
                Male(who, "A lot of people have asked us whether helmets will be provided with the bikes. I'm afraid the answer is no, for reasons of hygiene, so we strongly advise all riders to bring their own helmets. The bikes do have lights and a bell, and every bike is checked by our mechanics at least once a week."),
                Male(who, "The bikes are available from six in the morning until midnight. And finally, please remember to return your bike to a station. If you leave it anywhere else, for example outside your office or your home, you'll have to pay a fine of 20,000 som. The app shows you exactly where the nearest station is and how many free spaces it has."),
                Male(who, "Right, I'm sure you've got lots of questions, so let's get started."),
            ],
            [
                new("gap", L2, 2,
                [
                    Item(9, "In the first stage, there will be ___ bikes at forty stations.", "\"We're putting 350 bikes at forty stations\" — the extra 200 are only planned for next spring.", "350"),
                    Item(10, "The stations have bright ___ signs.", "\"You'll recognise the stations by their bright orange signs\" — the bikes are dark green.", "orange"),
                    Item(11, "Users register with their ___ number.", "\"You simply register with your phone number.\"", "phone"),
                    Item(12, "After the first thirty minutes, every extra ___ minutes costs 1,000 som.", "\"You pay 1,000 som for every extra fifteen minutes.\"", "fifteen", "15"),
                    Item(13, "Riders are advised to bring their own ___.", "\"We strongly advise all riders to bring their own helmets.\"", "helmets"),
                    Item(14, "Leaving a bike outside a station means paying a ___ of 20,000 som.", "\"You'll have to pay a fine of 20,000 som.\"", "fine"),
                ]),
            ]);
    }

    private static ListeningPart Cl07P3() => new(3,
        "You will hear four people talking about learning a musical instrument.",
        [
            Female("Speaker 1", "I had piano lessons from the age of seven, mainly because all the girls in my class did. By the time I was fourteen, I hated practising, so I stopped, and for nearly fifteen years I didn't touch a keyboard. Then last year I moved into a flat where the previous owners had left an old piano. One evening I tried a piece I used to play, and to my surprise my fingers remembered most of it. Now I have a lesson every fortnight. People say music helps them unwind, but for me practice is still hard work."),
            Male("Speaker 2", "I've never had a teacher. When I was sixteen, I bought a cheap second-hand guitar and taught myself using free lessons on the internet. There are thousands of them, from beginner level to really advanced techniques, and you can watch a difficult bit twenty times without anyone getting impatient. My older brother plays too, but he never had the patience to show me anything. The downside is that nobody corrects your bad habits, so my technique probably isn't perfect. Still, I can play most of the songs I like."),
            Male("Speaker 3", "The instrument I play is the dutar, the traditional two-stringed instrument. My grandfather played it at every family celebration, and as a child I used to sit next to him and watch his hands. When I was twelve, I asked him to teach me, and he was delighted. Some of my friends told me I should choose something more modern, like the electric guitar, but I've never regretted my decision. Last summer the two of us played together at my cousin's wedding, which was one of the proudest moments of my life."),
            Female("Speaker 4", "I work as a nurse, so you might think I play the violin just to relax after long shifts. In fact, it's the opposite: our rehearsals are quite demanding. I belong to an amateur orchestra with about thirty members, and we meet every Tuesday evening in a school hall. We give two concerts a year, and before a concert we rehearse on Saturdays too. What I love most is the feeling of being part of something bigger. On my own I'm an average player, but together we can sound really impressive."),
        ],
        [
            new("match", L3, null,
            [
                Item(15, "Speaker 1", "Speaker 1: \"I stopped, and for nearly fifteen years I didn't touch a keyboard ... Now I have a lesson every fortnight.\"", "E"),
                Item(16, "Speaker 2", "Speaker 2: \"I ... taught myself using free lessons on the internet.\"", "C"),
                Item(17, "Speaker 3", "Speaker 3: \"My grandfather played it ... When I was twelve, I asked him to teach me.\"", "A"),
                Item(18, "Speaker 4", "Speaker 4: \"I belong to an amateur orchestra ... we meet every Tuesday evening.\"", "F"),
            ],
            [
                "I started learning because of a family member.",
                "I wish I had chosen a different instrument.",
                "I learned mainly from online lessons.",
                "Playing helps me to relax after a stressful day.",
                "I gave up for a long time but then started again.",
                "I play regularly with other people in a group.",
            ]),
        ]);

    private static ListeningPart Cl07P4()
    {
        const string who = "Garden guide";
        var map = new MapPlan("Chinor Botanical Garden",
            [new("Main gate", 2, 4), new("Fountain", 2, 2), new("Car park", 4, 4)],
            [
                new("A", 0, 4), new("B", 1, 3), new("C", 0, 2), new("D", 1, 0),
                new("E", 3, 0), new("F", 4, 2), new("G", 3, 3), new("H", 2, 0),
            ]);
        return new(4,
            "You will hear a guide describing the Chinor Botanical Garden to visitors at the main gate.",
            [
                Female(who, "Good morning, and welcome to the Chinor Botanical Garden. We're standing at the main gate, in the middle of the southern side of the garden, so the path in front of you leads north. The car park, as you know, is in the south-east corner. Let me tell you where the main attractions are."),
                Female(who, "As you walk from the gate towards the fountain, you'll pass two small areas, one on each side of the path. The one on your left is the herb garden, where you can smell mint, basil and many plants used in traditional medicine. Opposite it, on the right of the path, is the café, which serves excellent fresh juice."),
                Female(who, "Just to the left of the gate, in the south-west corner of the garden, is the gift shop, but I'd suggest visiting it on your way out."),
                Female(who, "When you reach the fountain in the centre, turn right and follow the path to the eastern edge of the garden. There you'll find the cactus house, level with the fountain. Some of the plants inside are over a hundred years old."),
                Female(who, "If you turn left at the fountain instead and walk all the way to the western edge, you'll come to the Japanese garden, with its little stone bridge. It's the quietest place in the whole garden."),
                Female(who, "From the fountain, carry straight on north and you'll reach the tropical glasshouse, right in the middle of the northern side. It's the biggest building here, and inside it's always around thirty degrees, so you might want to take your jacket off!"),
                Female(who, "The glasshouse has a smaller garden on each side. The rose garden is immediately to its left as you face it, and to its right is the children's garden, where young visitors can plant seeds and take them home."),
                Female(who, "Please remember that the garden closes at six o'clock. Enjoy your visit!"),
            ],
            [
                new("map", L4, null,
                [
                    Item(19, "Herb garden", "\"From the gate towards the fountain ... The one on your left is the herb garden.\"", "B"),
                    Item(20, "Cactus house", "\"At the fountain, turn right ... the eastern edge ... the cactus house, level with the fountain.\"", "F"),
                    Item(21, "Japanese garden", "\"Turn left at the fountain ... all the way to the western edge ... the Japanese garden.\"", "C"),
                    Item(22, "Tropical glasshouse", "\"Carry straight on north ... right in the middle of the northern side.\"", "H"),
                    Item(23, "Children's garden", "\"The rose garden is immediately to its left ... to its right is the children's garden.\"", "E"),
                ], Map: map),
            ]);
    }

    private static ListeningPart Cl07P5() => new(5,
        "You will hear three extracts about money and shopping.",
        [
            Male("Narrator", "Extract One."),
            Male("Dilshod", "Nigora, is that a new phone? I thought you said you couldn't afford one."),
            Female("Nigora", "It isn't new, actually. I bought it second-hand from a shop that repairs old phones and sells them with a one-year guarantee. It cost less than half the price of a new one, and honestly, I can't see any difference. Every year the companies bring out a new model with a slightly better camera, and everyone rushes to buy it. I just don't think that's a sensible way to spend money."),
            Male("Dilshod", "I agree with you in principle. But be careful with the battery. Old batteries often run down much faster. Before the guarantee runs out, ask the shop to check it, and if there's a problem, get them to replace it for free."),
            Female("Nigora", "That's a good idea. I'll put a reminder in my calendar."),
            Male("Narrator", "Extract Two."),
            Female("Presenter", "Farrukh, you've run a small grocery shop in the same neighbourhood for twenty years. What's the biggest change you've seen?"),
            Male("Farrukh", "Without doubt, the way people pay. Ten years ago almost everyone paid in cash, and I spent every evening counting notes. Now around eighty per cent of my customers pay by scanning a code with their phone. Even the older people in the area have got used to it."),
            Female("Presenter", "Has that been good for business?"),
            Male("Farrukh", "In many ways, yes. I don't have to go to the bank so often, and there are no mistakes with change. But I've noticed something I didn't expect. When people paid in cash, they'd stop and chat while I counted the money. Now it's all over in two seconds. The shop is more efficient, but it's become a bit less friendly, and I miss that side of it."),
            Male("Narrator", "Extract Three."),
            Male("Speaker", "Last February I decided to try what's called a no-spend month. The rule was simple: apart from rent, bills, public transport and basic food, I wouldn't buy anything at all. No takeaway coffee, no new clothes, no online shopping. I expected the hardest part to be giving up coffee, but in fact that was easy. What I really struggled with was saying no to friends. Almost every invitation involved spending money, a meal out or a trip to the cinema, and I felt awkward explaining why I couldn't come. By the end of the month I'd saved more than I normally save in three months. But the most valuable lesson wasn't about the money. I realised that a lot of my shopping had nothing to do with need. I bought things when I was bored or tired. Now, before I buy anything, I wait twenty-four hours, and surprisingly often I decide I don't want it after all."),
        ],
        [
            new("mcq", L5, null,
            [
                Choice(24, "Why did Nigora buy a second-hand phone?", "B", "\"Everyone rushes to buy it. I just don't think that's a sensible way to spend money.\"",
                    "Her old phone could not be repaired.", "She thinks buying every new model is not sensible.", "She wanted a phone with a better camera."),
                Choice(25, "What does Dilshod advise Nigora to do?", "C", "\"Before the guarantee runs out, ask the shop to check it.\"",
                    "take the phone back to the shop immediately", "buy a spare battery", "have the battery checked while the guarantee is valid"),
                Choice(26, "What is the biggest change Farrukh has noticed?", "A", "\"Now around eighty per cent of my customers pay by scanning a code with their phone.\"",
                    "More customers pay without cash.", "Fewer older people visit his shop.", "He has to go to the bank more often."),
                Choice(27, "How does Farrukh feel about the change?", "B", "\"The shop is more efficient, but it's become a bit less friendly, and I miss that side of it.\"",
                    "He thinks it has made his shop less profitable.", "He sees advantages but misses chatting with customers.", "He wishes customers would go back to paying in cash."),
                Choice(28, "What did the speaker find most difficult?", "B", "\"What I really struggled with was saying no to friends.\"",
                    "giving up takeaway coffee", "refusing invitations from friends", "paying only for basic food"),
                Choice(29, "What did the speaker learn from the experience?", "A", "\"A lot of my shopping had nothing to do with need. I bought things when I was bored or tired.\"",
                    "He often bought things because of how he felt.", "He needed to find a better-paid job.", "He should stop meeting friends in cafés."),
            ]),
        ]);

    private static ListeningPart Cl07P6()
    {
        const string who = "Lecturer";
        return new(6,
            "You will hear part of a university lecture about bees and pollination.",
            [
                Female(who, "Good morning. Today's lecture is about one of the most important relationships in nature: the one between flowering plants and the insects that pollinate them, above all bees."),
                Female(who, "Let's start with the basics. For most flowering plants to produce seeds and fruit, pollen has to be carried from the male part of one flower to the female part of another flower of the same species. Some plants rely on the wind to do this, but around three quarters of the world's leading food crops benefit, at least to some degree, from animal pollinators. And the most important of these pollinators are bees."),
                Female(who, "Why bees? Unlike many other insects, bees feed almost entirely on flowers. Adult bees drink nectar, a sugary liquid that gives them energy, and they collect pollen, which is rich in protein, to feed their young. As a bee moves from flower to flower, pollen sticks to the fine hairs on its body and is transferred without the bee even noticing. The plant, in other words, pays the bee with food, and the bee delivers the plant's pollen in return."),
                Female(who, "When people hear the word bee, they usually think of the honeybee, which lives in large colonies and produces honey. But in fact there are around twenty thousand species of bee in the world, and most of them live alone rather than in colonies. Many of these solitary bees are excellent pollinators, sometimes more efficient than honeybees."),
                Female(who, "Honeybees do have one remarkable skill, however. When a worker finds a good source of food, she returns to the hive and performs what is known as the waggle dance. The direction of the dance tells other bees which way to fly in relation to the sun, and the length of the dance tells them how far away the flowers are."),
                Female(who, "Flowers, for their part, have developed many ways of attracting bees. Bees can't see the colour red very well, but they can see ultraviolet light, which is invisible to us. Many flowers have ultraviolet patterns on their petals which act like landing lights, guiding the bee towards the nectar."),
                Female(who, "Some plants demand a special technique. Tomatoes, for example, hold their pollen tightly inside the flower, and it's only released when the flower is shaken in a particular way. Bumblebees can do this by holding on to the flower and vibrating their flight muscles very rapidly, which is why they are often used in greenhouses where tomatoes are grown. This is known as buzz pollination."),
                Female(who, "Unfortunately, many bee populations are in decline, and scientists point to several causes working together. The loss of wild flowers, as land is turned into large farms or cities, means that bees have less food. Certain pesticides can harm bees' ability to find their way back to the nest, even in small doses. And honeybees in many countries suffer from a parasite, the varroa mite, which weakens colonies and spreads disease."),
                Female(who, "The good news is that some solutions are quite simple. Farmers can leave strips of wild flowers along the edges of their fields, and city councils can plant flowering trees and cut the grass in parks less often. Even a few pots of flowers on a balcony can provide food for bees. Next week we'll look at other pollinators, including butterflies and bats."),
            ],
            [
                new("gap", L6, 1,
                [
                    Item(30, "Adult bees drink ___, which gives them energy.", "\"Adult bees drink nectar, a sugary liquid that gives them energy.\"", "nectar"),
                    Item(31, "Pollen sticks to the fine ___ on a bee's body.", "\"Pollen sticks to the fine hairs on its body.\"", "hairs"),
                    Item(32, "Most of the world's bee species live ___ rather than in colonies.", "\"Most of them live alone rather than in colonies.\"", "alone"),
                    Item(33, "The waggle dance shows other bees which way to fly in relation to the ___.", "\"Which way to fly in relation to the sun.\"", "sun"),
                    Item(34, "Many flowers have ___ patterns that guide bees to the nectar.", "\"Many flowers have ultraviolet patterns on their petals which act like landing lights.\"", "ultraviolet"),
                    Item(35, "Some ___ can damage bees' ability to find their way home.", "\"Certain pesticides can harm bees' ability to find their way back to the nest.\"", "pesticides"),
                ]),
            ]);
    }

    // =====================================================================
    // TEST 8
    // =====================================================================

    private static ListeningPart Cl08P1() => new(1,
        "You will hear eight short remarks and questions from daily life. Choose the most suitable reply to each one.",
        [
            Female("Pharmacist", "Take one of these tablets twice a day, after meals."),
            Male("Brother", "Can I borrow your laptop for an hour? Mine's stopped working."),
            Female("Hotel receptionist", "I'm sorry, but check-in isn't until two o'clock."),
            Male("Football coach", "Why weren't you at training on Tuesday?"),
            Female("Friend", "Shall we get a taxi or just walk? It's only about fifteen minutes."),
            Male("Customer", "This jacket's a bit tight. Have you got it in a larger size?"),
            Female("Interviewer", "So, what made you apply for this position?"),
            Male("Old friend", "I haven't seen you for ages! What have you been up to?"),
        ],
        [
            new("mcq", L1, null,
            [
                Choice(1, "Choose the correct reply.", "A", "The pharmacist gives instructions; asking a follow-up question about the tablets is natural.",
                    "Thanks — should I avoid anything while I'm taking them?", "I've already had lunch, thank you.", "I took two of them yesterday."),
                Choice(2, "Choose the correct reply.", "B", "\"Can I borrow your laptop?\" asks permission; \"Sure\" gives it.",
                    "It was quite expensive, actually.", "Sure, just don't forget to plug it in.", "Yes, I borrowed one last week."),
                Choice(3, "Choose the correct reply.", "C", "\"Check-in isn't until two o'clock\" — the guest accepts and asks to leave the bags.",
                    "I checked it twice already.", "Two o'clock is too early for lunch.", "That's fine — can I leave my bags here until then?"),
                Choice(4, "Choose the correct reply.", "B", "\"Why weren't you at training?\" asks for a reason: \"I had a dentist appointment.\"",
                    "I'll be there next Tuesday too.", "Sorry, I had a dentist appointment.", "Training is twice a week."),
                Choice(5, "Choose the correct reply.", "C", "\"Taxi or just walk?\" is a suggestion with a choice; \"let's walk\" makes the choice.",
                    "About fifteen minutes ago.", "I got a taxi yesterday.", "It's a lovely evening — let's walk."),
                Choice(6, "Choose the correct reply.", "A", "\"Have you got it in a larger size?\" is answered by the assistant: \"We have, but only in black.\"",
                    "We have, but only in black, I'm afraid.", "Yes, it's a bit tight on me as well.", "It really suits the colour of your eyes."),
                Choice(7, "Choose the correct reply.", "C", "\"What made you apply?\" asks for a reason: \"I've always wanted to work with children.\"",
                    "I applied last Monday.", "The position is on the third floor.", "I've always wanted to work with children."),
                Choice(8, "Choose the correct reply.", "A", "\"What have you been up to?\" asks what someone has been doing: \"mostly working\".",
                    "Oh, not much — mostly working. How about you?", "I'll be up in a minute.", "About two years, I think."),
            ]),
        ]);

    private static ListeningPart Cl08P2()
    {
        const string who = "Education officer";
        return new(2,
            "You will hear an education officer talking about a new science museum for children.",
            [
                Female(who, "Hello, everyone, and thank you for joining this online information session about Discovery House, the city's new science museum for children, which opens next month. I'm Shahlo Ergasheva, and I'm responsible for the museum's education programme."),
                Female(who, "First, a little about the building. Many people think we're in a brand-new building, but in fact the museum is in a former textile factory by the river. The old machines have been removed, but we've kept the high ceilings and the huge windows, which make the galleries wonderfully bright."),
                Female(who, "Now, tickets. Adults pay 30,000 som, and children aged seven to sixteen pay half price. Children under seven get in free, but they must be with an adult at all times."),
                Female(who, "The museum has six galleries, covering everything from light and sound to the human body. When we tested the museum with local schools, we expected the space gallery to be the favourite. In fact, the most popular area by far was the robot workshop, where children program small robots to find their way through a maze."),
                Female(who, "At weekends we also run a practical workshop for families. This month, children will design and build their own bridges using only paper and tape, and then test how much weight they can carry. Next month the theme will be boats."),
                Female(who, "Teachers, please note that school groups are very welcome on weekdays, but you must book at least two weeks in advance, because places are limited. There's no charge for teachers who come with a group."),
                Female(who, "Finally, a practical point. There's a picnic area on the roof, with a lovely view of the river, but for safety reasons you're not allowed to take food into the galleries. Drinks in bottles with lids are fine. If you have any questions, please type them in the chat box now."),
            ],
            [
                new("gap", L2, 2,
                [
                    Item(9, "The museum is in a former ___ factory.", "\"The museum is in a former textile factory by the river.\"", "textile"),
                    Item(10, "Children under ___ get in free.", "\"Children under seven get in free.\"", "seven", "7"),
                    Item(11, "The most popular area is the ___ workshop.", "\"The most popular area by far was the robot workshop\" — not the space gallery.", "robot"),
                    Item(12, "In this month's family workshop, children will build ___.", "\"This month, children will design and build their own bridges\" — boats are next month.", "bridges"),
                    Item(13, "School groups must book at least ___ weeks in advance.", "\"You must book at least two weeks in advance.\"", "two", "2"),
                    Item(14, "Visitors may not take ___ into the galleries.", "\"You're not allowed to take food into the galleries. Drinks ... are fine.\"", "food"),
                ]),
            ]);
    }

    private static ListeningPart Cl08P3() => new(3,
        "You will hear four students talking about their part-time jobs.",
        [
            Male("Speaker 1", "I work three afternoons a week in a café that belongs to my uncle. He needed someone reliable, and I needed some money, so it suited us both. I didn't even have an interview! The work is fine: I make coffee, clear tables and sometimes help in the kitchen. My parents hoped it would give me some ideas about what to do after university, but to be honest, it hasn't. I'm studying engineering, and I'm fairly sure I don't want to spend my life serving coffee."),
            Female("Speaker 2", "I translate documents from Russian into English for a small agency. They send me the texts by email, and I do them at my kitchen table, usually late in the evening. I only go into their office about once a month, to discuss new projects. The pay isn't very high. In fact, some of my friends who work in shops earn more than I do. But I can choose my own hours, which fits perfectly around my lectures."),
            Male("Speaker 3", "Last year I took a job stacking shelves in a supermarket. The money was good, especially for evening shifts, so I kept taking more and more of them. By the spring I was working four nights a week. I was always tired in lectures, I missed several deadlines, and my exam results were the worst I've ever had. This year I've cut down to one shift a week. I'm poorer, but I've learned my lesson."),
            Female("Speaker 4", "I'm studying biology, and for the past year I've worked on Saturdays as an assistant in a vet's clinic. I mostly answer the phone and calm down nervous pet owners, which I find easy, as I've always been quite a chatty person. But I also watch the vets at work, and that's changed everything for me. I used to think I'd go into research after my degree. Now I'm planning to apply to study veterinary medicine."),
        ],
        [
            new("match", L3, null,
            [
                Item(15, "Speaker 1", "Speaker 1: \"a café that belongs to my uncle ... I didn't even have an interview!\"", "D"),
                Item(16, "Speaker 2", "Speaker 2: \"They send me the texts by email, and I do them at my kitchen table.\"", "F"),
                Item(17, "Speaker 3", "Speaker 3: \"I missed several deadlines, and my exam results were the worst I've ever had.\"", "C"),
                Item(18, "Speaker 4", "Speaker 4: \"I used to think I'd go into research ... Now I'm planning to apply to study veterinary medicine.\"", "A"),
            ],
            [
                "The job has helped me to decide on my future career.",
                "I earn more than most of my friends.",
                "My studies have suffered because of the job.",
                "I got the job through someone I know.",
                "The job has made me more confident with people.",
                "I do most of my work from home.",
            ]),
        ]);

    private static ListeningPart Cl08P4()
    {
        const string who = "Tour guide";
        var map = new MapPlan("Lola Square",
            [new("Bus stop", 2, 4), new("Clock tower", 2, 2), new("Metro entrance", 0, 0)],
            [
                new("A", 0, 4), new("B", 3, 4), new("C", 0, 2), new("D", 4, 0),
                new("E", 1, 1), new("F", 4, 2), new("G", 2, 0), new("H", 3, 1),
            ]);
        return new(4,
            "You will hear a tour guide describing Lola Square, in the city centre, to a group of tourists.",
            [
                Male(who, "Welcome to Lola Square, the heart of our city centre. We've just got off the bus, and we're standing at the bus stop in the middle of the southern side of the square. In front of us, right in the centre of the square, is the clock tower, which was built more than a hundred years ago."),
                Male(who, "Let's start with the southern side. Immediately to the right of the bus stop is a pharmacy, which is open twenty-four hours a day. If you look to your left, you'll see the flower market in the south-western corner of the square. It's at its busiest early in the morning."),
                Male(who, "On the western side of the square, level with the clock tower, is the city theatre, a beautiful building with white columns. Tickets for this evening's concert are still available."),
                Male(who, "Just behind the clock tower, a little to the left, there's a small café with tables outside. It's between the tower and the metro entrance, which is in the north-western corner."),
                Male(who, "Directly across the square from the theatre, on the eastern side, is a bank, where you can change money if you need to. The large building in the north-eastern corner is the town hall."),
                Male(who, "Between the clock tower and the town hall, there's a bookshop that sells maps and guidebooks in several languages. It's a good place to buy postcards, too."),
                Male(who, "And finally, straight ahead of us, behind the clock tower, in the middle of the northern side of the square, is the history museum, which is where we'll begin our tour. Please follow me."),
            ],
            [
                new("map", L4, null,
                [
                    Item(19, "Pharmacy", "\"Immediately to the right of the bus stop is a pharmacy.\"", "B"),
                    Item(20, "Theatre", "\"On the western side of the square, level with the clock tower, is the city theatre.\"", "C"),
                    Item(21, "Bank", "\"Directly across the square from the theatre, on the eastern side, is a bank.\"", "F"),
                    Item(22, "Bookshop", "\"Between the clock tower and the town hall, there's a bookshop.\"", "H"),
                    Item(23, "History museum", "\"Straight ahead ... behind the clock tower, in the middle of the northern side.\"", "G"),
                ], Map: map),
            ]);
    }

    private static ListeningPart Cl08P5() => new(5,
        "You will hear three extracts about health.",
        [
            Male("Narrator", "Extract One."),
            Female("Kamila", "Anvar, you keep rubbing your back. Are you all right?"),
            Male("Anvar", "Not really. Since we moved to this new office, I've had back pain almost every day. I'm sure it's the chair. I sit in it for nine hours without moving, except to get coffee."),
            Female("Kamila", "It might be the chair, but it sounds more like the sitting. I had the same problem last year. A new chair didn't help at all. What made the difference was getting up every half hour, even just to walk to the window and back. I set a timer on my phone."),
            Male("Anvar", "Every half hour? I'd never get anything done."),
            Female("Kamila", "That's what I thought, but you'll probably concentrate better, not worse. Try it for a week."),
            Male("Narrator", "Extract Two."),
            Male("Presenter", "Doctor Yusupova, we're always told to drink eight glasses of water a day. Is that really necessary?"),
            Female("Doctor", "It's a very popular idea, but there's surprisingly little scientific evidence for that exact number. How much water you need depends on your size, how active you are and, of course, the weather. A farmer working outside in July needs far more than an office worker in winter. People also forget that tea, soup and fruit all contain water too."),
            Male("Presenter", "So how do we know if we're drinking enough?"),
            Female("Doctor", "For most healthy adults, the simplest guide is thirst. Drink when you're thirsty. The exceptions are elderly people, who don't always feel thirsty, and athletes during long training sessions. My main message is this: don't force yourself to drink a fixed amount. Listen to your body instead."),
            Male("Narrator", "Extract Three."),
            Female("Speaker", "For years I was a night owl. I'd scroll through my phone until one or two in the morning and then drag myself out of bed at seven. I always felt tired, but I thought that was just normal adult life. What finally made me change wasn't a doctor or an article, but my little nephew. He stayed with us for a week, and he asked me why I yawned all the time. I couldn't answer him! So I started going to bed at eleven and leaving my phone in the kitchen. The first week was hard. But after about a month, the change I noticed most wasn't that I had more energy, though I did. It was my mood. I became much more patient, with my colleagues and with myself."),
        ],
        [
            new("mcq", L5, null,
            [
                Choice(24, "What does Anvar believe is causing his back pain?", "A", "\"I'm sure it's the chair.\"",
                    "the chair he uses at work", "carrying heavy bags", "the long walk to the new office"),
                Choice(25, "What does Kamila recommend?", "B", "\"What made the difference was getting up every half hour.\"",
                    "buying a better chair", "taking short breaks regularly", "working fewer hours"),
                Choice(26, "What does the doctor say about the idea of eight glasses a day?", "C", "\"How much water you need depends on your size, how active you are and ... the weather.\"",
                    "It is supported by a lot of research.", "It is only suitable for people who work outdoors.", "The amount people need varies from person to person."),
                Choice(27, "What is the doctor's main advice?", "B", "\"The simplest guide is thirst ... Listen to your body instead.\"",
                    "Drink water rather than tea or soup.", "Let thirst guide how much you drink.", "Drink more in winter than in summer."),
                Choice(28, "What made the speaker change her habits?", "C", "\"What finally made me change wasn't a doctor or an article, but my little nephew.\"",
                    "advice from her doctor", "an article she read", "a question from a child"),
                Choice(29, "What was the biggest change she noticed?", "A", "\"The change I noticed most wasn't that I had more energy ... It was my mood.\"",
                    "She was in a better mood.", "She had more energy at work.", "She used her phone less."),
            ]),
        ]);

    private static ListeningPart Cl08P6()
    {
        const string who = "Lecturer";
        return new(6,
            "You will hear part of a university lecture about how deserts form.",
            [
                Male(who, "Good afternoon. In today's lecture we're going to look at deserts, and in particular at the question of why they form where they do. Deserts cover roughly a third of the land surface of the Earth, so this is not a minor topic."),
                Male(who, "First, let's be clear about what a desert actually is. Many people imagine sand dunes and extreme heat, but geographers define a desert by rainfall, not temperature. The usual definition is an area that receives less than 250 millimetres of rain or snow a year. By this definition, the largest desert in the world isn't the Sahara at all, but Antarctica, which receives very little precipitation and is covered in ice rather than sand. In fact, only about twenty per cent of the world's deserts are covered in sand; the rest are mostly rock and gravel."),
                Male(who, "So why does so little rain fall in these places? There are four main causes."),
                Male(who, "The first and most important is the global circulation of air. Near the equator, the sun heats the air strongly, so it rises. As it rises, it cools and drops its moisture as heavy rain, which is why we find rainforests there. This air, now dry, moves away from the equator high in the atmosphere and eventually sinks back towards the ground at around thirty degrees north and south. As it sinks, it becomes warmer and even drier, and it prevents clouds from forming. That's why many of the world's great deserts, including the Sahara and the deserts of Arabia and Australia, lie close to these latitudes."),
                Male(who, "The second cause is mountains. When moist air from the ocean meets a mountain range, it's forced upwards, cools, and loses most of its moisture as rain on that side. By the time it crosses the mountains and descends on the other side, it's dry. The dry area behind the mountains is known as a rain shadow."),
                Male(who, "The third cause is simply distance from the sea. Air picks up moisture over the oceans, and the further it travels inland, the less moisture it contains. Deserts such as the Gobi are dry largely because they're thousands of kilometres from the nearest coast. The Kyzylkum, here in our own region, is also partly a result of this."),
                Male(who, "The fourth cause is perhaps the most surprising: cold ocean currents. Along some coasts, cold water flows close to the shore. It cools the air above it, and cool air holds less moisture, so fog forms but rain rarely falls. The Atacama Desert in Chile, one of the driest places on Earth, is a well-known example."),
                Male(who, "Finally, I should mention that deserts aren't only created by nature. On the edges of deserts, poor land use, such as overgrazing by too many animals or cutting down trees for fuel, can destroy the plants that hold the soil together. This process, called desertification, can turn productive land into desert within a few decades. We'll look at it in detail next week."),
            ],
            [
                new("gap", L6, 1,
                [
                    Item(30, "A desert is an area that receives less than ___ millimetres of rain or snow a year.", "\"An area that receives less than 250 millimetres of rain or snow a year.\"", "250"),
                    Item(31, "By this definition, the largest desert in the world is ___.", "\"The largest desert in the world isn't the Sahara at all, but Antarctica.\"", "Antarctica"),
                    Item(32, "Heavy rain near the equator explains why there are ___ there.", "\"It ... drops its moisture as heavy rain, which is why we find rainforests there.\"", "rainforests"),
                    Item(33, "The dry area on the far side of a mountain range is called a rain ___.", "\"The dry area behind the mountains is known as a rain shadow.\"", "shadow"),
                    Item(34, "Cold ocean ___ cool the air, so fog forms but rain rarely falls.", "\"Cold ocean currents ... fog forms but rain rarely falls.\"", "currents"),
                    Item(35, "Poor land use, such as ___, can lead to desertification.", "\"Poor land use, such as overgrazing by too many animals ... called desertification.\"", "overgrazing"),
                ]),
            ]);
    }

    // =====================================================================
    // TEST 9
    // =====================================================================

    private static ListeningPart Cl09P1() => new(1,
        "You will hear eight short sentences said at home, at school, at work and while travelling. Choose the right reply to each one.",
        [
            Female("Librarian", "I'm afraid this book is overdue — it should have been returned last week."),
            Male("Friend", "Do you fancy coming to the football match with us on Sunday?"),
            Female("Mother", "Have you remembered to feed the cat?"),
            Male("Taxi driver", "Good evening. Where would you like to go?"),
            Female("Colleague", "Would you like me to send you the report by email?"),
            Male("Tour guide", "Please make sure you're all back on the coach by half past four."),
            Female("Classmate", "I don't suppose you've got a spare pen I could borrow?"),
            Male("Manager", "I'm afraid we'll have to postpone the meeting until next week."),
        ],
        [
            new("mcq", L1, null,
            [
                Choice(1, "Choose the correct reply.", "A", "\"This book is overdue\" — the reader apologises and asks about a fine.",
                    "Sorry, I completely forgot. Is there a fine?", "I'll read it next week.", "It was due last week, I think."),
                Choice(2, "Choose the correct reply.", "B", "\"Do you fancy coming ...?\" is an invitation, accepted with \"I'd love to\".",
                    "I've never fancied football players.", "I'd love to — what time does it start?", "We won the match on Sunday."),
                Choice(3, "Choose the correct reply.", "C", "\"Have you remembered to feed the cat?\" — \"Oops — I'll do it right now\" admits forgetting.",
                    "Yes, the cat's very hungry.", "It remembers everything.", "Oops — I'll do it right now."),
                Choice(4, "Choose the correct reply.", "A", "\"Where would you like to go?\" is answered with a destination: \"To the airport.\"",
                    "To the airport, please.", "I'd like to go by taxi.", "I've been there before."),
                Choice(5, "Choose the correct reply.", "B", "\"Would you like me to send ...?\" is an offer, accepted with \"Yes, please\".",
                    "I sent it yesterday.", "Yes, please — that would be really helpful.", "I'm reporting it now."),
                Choice(6, "Choose the correct reply.", "C", "\"Make sure you're back ... by half past four\" — \"we won't be late\" promises to do so.",
                    "It's half past four already.", "Does the coach go faster?", "Don't worry, we won't be late."),
                Choice(7, "Choose the correct reply.", "A", "\"Have you got a spare pen I could borrow?\" — \"Here you are\" gives one.",
                    "Here you are — you can keep it.", "I don't suppose so either.", "My pen is blue."),
                Choice(8, "Choose the correct reply.", "B", "\"We'll have to postpone the meeting\" — \"just let me know the new date\" accepts the change.",
                    "The meeting was very useful.", "No problem — just let me know the new date.", "I'm afraid I've never met him."),
            ]),
        ]);

    private static ListeningPart Cl09P2()
    {
        const string who = "Metro spokesman";
        return new(2,
            "You will hear a spokesman for the city metro talking about a new metro line.",
            [
                Male(who, "Good morning. I'm Jasur Nazarov from the city metro company, and I'd like to tell you about the new Green Line, which opens to passengers on the fifteenth of September."),
                Male(who, "When the project was first announced, fourteen stations were planned. In the end, two of them were combined, so the line now has twelve stations, running from north to south for about eighteen kilometres. Most importantly, for the first time, the metro will link the central railway station and the airport. At the moment that trip can take up to an hour by bus in heavy traffic. On the Green Line, a journey from one end of the line to the other will take just twenty-five minutes. Trains will run every four minutes in the rush hour and every eight minutes at other times."),
                Male(who, "Many of you will have seen photos of the stations already. Our metro is famous for its beautiful stations, and we wanted to continue that tradition. Each of the new stations is decorated with mosaics designed by local artists, and each one tells the story of a different part of the city."),
                Male(who, "We've also thought carefully about accessibility. Every station has lifts for wheelchair users and parents with pushchairs, and there are escalators at all entrances for passengers with heavy luggage, which is especially important for people travelling to and from the airport."),
                Male(who, "Tickets can be paid for in the usual way, with a transport card, a bank card or a QR code on your phone."),
                Male(who, "Finally, some good news. Some newspapers reported that travel on the line would be free for the first week. In fact, it'll be free for the whole of the first month, so that everyone has a chance to try it. Thank you."),
            ],
            [
                new("gap", L2, 2,
                [
                    Item(9, "The Green Line will have ___ stations.", "\"The line now has twelve stations\" — fourteen were planned at first.", "twelve", "12"),
                    Item(10, "For the first time, the metro will link the railway station and the ___.", "\"The metro will link the central railway station and the airport.\"", "airport"),
                    Item(11, "A journey from one end of the line to the other will take ___ minutes.", "\"A journey from one end of the line to the other will take just twenty-five minutes.\"", "twenty-five", "25"),
                    Item(12, "Each station is decorated with ___ designed by local artists.", "\"Each of the new stations is decorated with mosaics designed by local artists.\"", "mosaics"),
                    Item(13, "There are ___ at all entrances for passengers with heavy luggage.", "\"There are escalators at all entrances for passengers with heavy luggage.\"", "escalators"),
                    Item(14, "Travel on the line will be free for the first ___.", "\"It'll be free for the whole of the first month\" — not the first week.", "month"),
                ]),
            ]);
    }

    private static ListeningPart Cl09P3() => new(3,
        "You will hear four people talking about their hobbies.",
        [
            Male("Speaker 1", "Every Sunday morning my daughter and I get up before sunrise and go birdwatching by the lake outside the city. She's eleven, and she can already recognise more birds than I can. We take a flask of tea, a pair of binoculars and a little notebook where we write down everything we see. It isn't a very sociable hobby. We go early precisely because there's nobody around to frighten the birds. But those quiet mornings together are the best part of my week."),
            Female("Speaker 2", "I started embroidering cushion covers just for my own flat. Then a friend asked if she could buy one, and then her sister wanted two. Last year I opened a small online shop, and now I sell about ten covers a month. It doesn't pay the rent, of course, but it covers the cost of materials and gives me a bit of extra pocket money. People sometimes assume I picked it up quickly, but it took me years of practice before my stitches looked professional."),
            Male("Speaker 3", "Two years ago I broke my leg playing football and had to stay at home for almost two months. I was bored out of my mind, so I bought a sketchbook and some pencils. I'd never drawn anything before, apart from at school. At first I just drew things in my room, but gradually I started doing portraits of my family. I've kept going since my leg healed. I once sold a drawing to a neighbour, but I'd hardly call that a business."),
            Female("Speaker 4", "When I moved to Tashkent for work, I didn't know anyone. A colleague told me about a board game café where people meet every Thursday evening to play strategy games. I was nervous the first time, but everyone was very welcoming. Now, a year later, most of my closest friends are people I met around those tables. People think it must take up all my free time, but it's really just one evening a week, and sometimes a Saturday afternoon."),
        ],
        [
            new("match", L3, null,
            [
                Item(15, "Speaker 1", "Speaker 1: \"Every Sunday morning my daughter and I ... go birdwatching.\"", "B"),
                Item(16, "Speaker 2", "Speaker 2: \"Now I sell about ten covers a month ... gives me a bit of extra pocket money.\"", "A"),
                Item(17, "Speaker 3", "Speaker 3: \"I broke my leg ... had to stay at home for almost two months ... so I bought a sketchbook.\"", "C"),
                Item(18, "Speaker 4", "Speaker 4: \"Most of my closest friends are people I met around those tables.\"", "F"),
            ],
            [
                "My hobby has become a small source of income.",
                "I share my hobby with a family member.",
                "I took up my hobby while recovering from an injury.",
                "My hobby takes up more time than I would like.",
                "I was surprised how quickly I improved.",
                "My hobby has helped me to make new friends.",
            ]),
        ]);

    private static ListeningPart Cl09P4()
    {
        const string who = "Farm guide";
        var map = new MapPlan("Green Valley Eco-Farm",
            [new("Farm gate", 2, 4), new("Farmhouse", 2, 2), new("Wind turbine", 4, 0)],
            [
                new("A", 1, 4), new("B", 3, 3), new("C", 0, 4), new("D", 0, 2),
                new("E", 1, 0), new("F", 3, 1), new("G", 4, 2), new("H", 2, 0),
            ]);
        return new(4,
            "You will hear a guide describing Green Valley Eco-Farm to a group of visitors at the farm gate.",
            [
                Female(who, "Hello, and welcome to Green Valley Eco-Farm. We're at the farm gate, in the middle of the southern side of the farm, and the path in front of you leads straight up to the farmhouse in the centre. Let me explain where everything is."),
                Female(who, "Immediately on your left as you come through the gate is our farm shop. It sells our own cheese, honey and vegetables, and all the packaging is recyclable. Beyond the shop, in the south-western corner, are the vegetable plots, where visitors can pick their own tomatoes in summer."),
                Female(who, "As you walk up the path towards the farmhouse, you'll see the chicken houses on your right, about halfway between the gate and the farmhouse. The chickens spend most of the day outside."),
                Female(who, "When you get to the farmhouse, turn right and follow the track to the eastern edge of the farm. There, level with the farmhouse, is the goat barn. Children love feeding the goats, but please only give them the food we sell in the shop."),
                Female(who, "You can see our wind turbine in the north-eastern corner. It produces most of the farm's electricity. Between the turbine and the farmhouse is the solar greenhouse. It's heated entirely by the sun, so we can grow vegetables even in winter."),
                Female(who, "Directly behind the farmhouse, in the middle of the northern side, is the orchard, with apples, apricots and cherries. The beehives are just to the left of the orchard, along the northern fence. Please don't go too close to them."),
                Female(who, "Finally, if you go back to the farmhouse and turn left, you'll reach the western edge of the farm. There, level with the farmhouse, is the compost area, where all our food waste is turned into fertiliser for the fields. It's more interesting than it sounds, I promise!"),
            ],
            [
                new("map", L4, null,
                [
                    Item(19, "Farm shop", "\"Immediately on your left as you come through the gate is our farm shop.\"", "A"),
                    Item(20, "Goat barn", "\"Turn right ... to the eastern edge of the farm. There, level with the farmhouse, is the goat barn.\"", "G"),
                    Item(21, "Solar greenhouse", "\"Between the turbine and the farmhouse is the solar greenhouse.\"", "F"),
                    Item(22, "Beehives", "\"The beehives are just to the left of the orchard, along the northern fence.\"", "E"),
                    Item(23, "Compost area", "\"Turn left ... the western edge of the farm. There, level with the farmhouse, is the compost area.\"", "D"),
                ], Map: map),
            ]);
    }

    private static ListeningPart Cl09P5() => new(5,
        "You will hear three extracts about food and cooking.",
        [
            Male("Narrator", "Extract One."),
            Female("Madina", "So, what are we cooking for your parents on Saturday? I was thinking we could try that Thai curry recipe I found."),
            Male("Timur", "Hmm. It sounds delicious, but my dad's quite traditional about food. And it's the first time they're coming to our new flat."),
            Female("Madina", "I know. But if we make plov, your mother will compare it with hers, and I'll never make it as well as she does. That's what really worries me."),
            Male("Timur", "She won't mind, honestly. But how about this: we make plov as the main dish, since that's what they'll expect, and we try the curry another time, when it's just friends."),
            Female("Madina", "All right. But you're making the plov!"),
            Male("Narrator", "Extract Two."),
            Female("Interviewer", "Sardor, as a restaurant chef, what's the biggest cause of food waste in your kitchen?"),
            Male("Sardor", "People usually guess that it's food going off in the fridge, but that's actually quite rare. The biggest problem was portion size. For years we served enormous plates because customers expected them, and at the end of the evening we were throwing away bags of rice and bread that nobody had touched."),
            Female("Interviewer", "So what did you do?"),
            Male("Sardor", "We made the standard portions a bit smaller and started offering free second helpings of rice and salad. Almost nobody complains. And we now offer every customer a container to take leftovers home. Some people were embarrassed to ask for one before, but when the waiter offers, they're delighted. Frankly, I think every restaurant should be doing this."),
            Male("Narrator", "Extract Three."),
            Female("Speaker", "My grandmother was a wonderful cook, but she never used a recipe. When I asked her how much flour to put in the dough, she'd say, 'Enough.' How much salt? 'You'll know.' When she was in her eighties, I realised that if I didn't write her recipes down, they'd be lost. So every Sunday for a year, I cooked with her, and I measured everything before it went into the pot. It drove her mad at first! Sometimes she'd grab the spoon out of my hand. But gradually she became interested, and by the end she was checking my notes and correcting them. Now I've got a notebook with forty of her dishes. The funny thing is, when I cook from it, the food is good, but it never tastes quite like hers."),
        ],
        [
            new("mcq", L5, null,
            [
                Choice(24, "What is Madina worried about?", "B", "\"Your mother will compare it with hers ... That's what really worries me.\"",
                    "that the curry will be too spicy", "that Timur's mother will compare their cooking", "that the new flat is too small for guests"),
                Choice(25, "What do they decide to do?", "A", "\"We make plov as the main dish, since that's what they'll expect.\"",
                    "cook a traditional dish", "cook two different main dishes", "ask Timur's mother to cook"),
                Choice(26, "According to Sardor, what was the main cause of waste?", "B", "\"The biggest problem was portion size ... we served enormous plates.\"",
                    "Food went bad in the fridge.", "The portions were too large.", "Customers ordered too many dishes."),
                Choice(27, "What is Sardor's opinion about offering containers for leftovers?", "C", "\"Frankly, I think every restaurant should be doing this.\"",
                    "Customers find it embarrassing.", "It is too expensive for small restaurants.", "It is something all restaurants should do."),
                Choice(28, "Why was it difficult to learn from the speaker's grandmother?", "A", "\"She never used a recipe ... she'd say, 'Enough.'\"",
                    "She never measured her ingredients.", "She refused to share her recipes.", "She only cooked on Sundays."),
                Choice(29, "How does the speaker feel about the dishes she cooks from the notebook?", "B", "\"The food is good, but it never tastes quite like hers.\"",
                    "They are better than her grandmother's.", "They are not exactly the same as the original.", "They take too long to prepare."),
            ]),
        ]);

    private static ListeningPart Cl09P6()
    {
        const string who = "Lecturer";
        return new(6,
            "You will hear part of a university lecture about traditional ceramics and how they are made.",
            [
                Female(who, "Good morning, everyone. Pottery is one of the oldest crafts in the world. Archaeologists have found fired clay pots that are more than fifteen thousand years old, far older than writing. Today I'm going to take you through the main stages of making traditional ceramics, and then say a little about the tradition in our own region."),
                Female(who, "Everything starts with clay, which forms over thousands of years as rocks are slowly broken down by water and weather. Clay is useful for one simple reason: when it's wet, it can be shaped, and when it's heated to a high enough temperature, it becomes permanently hard."),
                Female(who, "The first stage is preparing the clay. Raw clay is dug from the ground, mixed with water and left to settle so that stones and roots can be removed. Then the potter has to knead it, rather like bread dough. This is called wedging, and its purpose is to remove any air bubbles. If a bubble is left inside, it can expand in the heat and cause the pot to crack or even explode."),
                Female(who, "Next comes shaping. Some pots are built by hand, from coils or flat pieces of clay, but in most traditional workshops the potter uses a wheel. The wheel spins while the potter's hands pull the clay upwards into the shape of a bowl, a plate or a jug. It looks easy when an experienced master does it, but it takes years to learn."),
                Female(who, "The shaped pot must then dry slowly, usually for several days. If it dries too quickly, especially in hot weather, it may crack. When it's completely dry, it's placed in a kiln, a special oven, and fired for the first time, usually at around nine hundred degrees. After this first firing, the pot is hard but still porous; in other words, water can pass through it."),
                Female(who, "To make the pot waterproof and to decorate it, the potter applies a glaze. A glaze is a thin layer of minerals which melts during a second firing and turns into a kind of glass on the surface. Different minerals produce different colours: cobalt, for instance, gives a deep blue, and copper gives green or turquoise."),
                Female(who, "Here in Uzbekistan, the town of Rishton in the Fergana Valley is famous for exactly these blues and greens. The local clay can be used almost straight from the ground, and potters there have traditionally made their glaze using the ash of plants that grow in the dry hills nearby. Designs often feature pomegranates, birds and geometric patterns, painted by hand with fine brushes."),
                Female(who, "Finally, a word about the main types of ceramics. Earthenware, like most Rishton pottery, is fired at relatively low temperatures. Stoneware is fired hotter and is stronger. Porcelain, which was first developed in China, is fired at the highest temperatures of all and is so fine that light can pass through it. Next week we'll look at how these different types spread along the trade routes of the Silk Road."),
            ],
            [
                new("gap", L6, 1,
                [
                    Item(30, "Wedging the clay removes any air ___.", "\"This is called wedging, and its purpose is to remove any air bubbles.\"", "bubbles"),
                    Item(31, "In most traditional workshops, pots are shaped on a ___.", "\"In most traditional workshops the potter uses a wheel.\"", "wheel"),
                    Item(32, "The dry pot is fired in a special oven called a ___.", "\"It's placed in a kiln, a special oven.\"", "kiln"),
                    Item(33, "After the first firing, the pot is still ___, so water can pass through it.", "\"The pot is hard but still porous; in other words, water can pass through it.\"", "porous"),
                    Item(34, "Rishton potters traditionally made glaze from the ___ of plants.", "\"Potters there have traditionally made their glaze using the ash of plants.\"", "ash"),
                    Item(35, "___ is fired at the highest temperatures and lets light pass through it.", "\"Porcelain ... is fired at the highest temperatures of all and is so fine that light can pass through it.\"", "Porcelain"),
                ]),
            ]);
    }

    // =====================================================================
    // TEST 10
    // =====================================================================

    private static ListeningPart Cl10P1() => new(1,
        "You will hear eight sentences spoken by friends, colleagues and people in public places. Choose the best reply to each one.",
        [
            Female("Friend", "Would you like to come round for dinner on Friday?"),
            Male("Passenger", "Excuse me, does this train stop at the airport?"),
            Female("Receptionist", "Could you spell your surname for me, please?"),
            Male("Colleague", "Would you mind if I opened the window? It's really stuffy in here."),
            Female("Cousin", "Guess what? I've just passed my driving test!"),
            Male("Shop assistant", "I'm sorry, we've run out of that size. Shall I order it for you?"),
            Female("Flatmate", "What's the weather supposed to be like tomorrow?"),
            Male("Teacher", "Whose bag is this? Someone's left it under the desk."),
        ],
        [
            new("mcq", L1, null,
            [
                Choice(1, "Choose the correct reply.", "A", "\"Would you like to come round for dinner?\" is an invitation, accepted with \"I'd love to\".",
                    "Thanks, I'd love to. Shall I bring anything?", "I came round on Friday.", "Dinner was delicious, thank you."),
                Choice(2, "Choose the correct reply.", "B", "\"Does this train stop at the airport?\" is answered: \"Yes, it's the last stop on the line.\"",
                    "No, I'm not stopping.", "Yes, it's the last stop on the line.", "The airport is very modern."),
                Choice(3, "Choose the correct reply.", "C", "\"Could you spell your surname?\" is answered by spelling it letter by letter.",
                    "My surname is quite common.", "I spelled it yesterday.", "Of course — it's R-A-H-I-M-O-V."),
                Choice(4, "Choose the correct reply.", "A", "\"Would you mind if I opened the window?\" — \"Not at all\" means it is fine.",
                    "Not at all, go ahead.", "Yes, it's a lovely window.", "I opened it last week."),
                Choice(5, "Choose the correct reply.", "B", "\"I've just passed my driving test!\" is good news, so the reply congratulates.",
                    "I've passed it many times.", "Well done! You must be delighted.", "When is your test?"),
                Choice(6, "Choose the correct reply.", "C", "\"Shall I order it for you?\" is an offer, accepted with \"Yes, please\".",
                    "I've run out of money too.", "No, I ordered it last month.", "Yes, please. How long will it take?"),
                Choice(7, "Choose the correct reply.", "B", "\"What's the weather supposed to be like tomorrow?\" asks about the forecast: \"Sunny, apparently.\"",
                    "I suppose it was.", "Sunny, apparently, but quite windy.", "It was cold yesterday."),
                Choice(8, "Choose the correct reply.", "A", "\"Whose bag is this?\" — \"Oh, it's mine\" answers the question.",
                    "Oh, it's mine — thank you!", "I left under the desk.", "It's a very nice bag."),
            ]),
        ]);

    private static ListeningPart Cl10P2()
    {
        const string who = "Fair organiser";
        return new(2,
            "You will hear one of the organisers giving visitors information about a book fair.",
            [
                Female(who, "Good morning, and welcome to this year's City Book Fair. I'm Gulnora Saidova, one of the organisers, and I'd like to give you a quick guide to what's on over the next four days."),
                Female(who, "First, the location. Last year we were in Hall 2, but the fair has grown so much that this year we've moved into Hall 4, the largest hall in the Exhibition Centre. You can't miss it: just follow the yellow signs from the main entrance."),
                Female(who, "This year we have over 120 publishers taking part, from Uzbekistan and from abroad, and you'll find books in Uzbek, Russian, English and several other languages. Many publishers are offering special fair prices, so it's a good chance to build up your home library."),
                Female(who, "A day ticket costs 20,000 som at the door, but if you buy it online, it's cheaper, only 15,000. Students and teachers can get in free on the last day."),
                Female(who, "One of the highlights of the fair is the chance to meet authors. Book signings take place every afternoon in the garden next to the café. A full timetable is printed on the back of your map. The queues can be long for the most popular writers, so come early."),
                Female(who, "We've also introduced something new this year: a book swap stand. Bring along books you've already read and swap them for someone else's. To give everyone a fair chance, there's a limit of five books per person per day."),
                Female(who, "And finally, don't miss the closing ceremony on Sunday evening, when we'll announce the winners of our poetry competition for young writers aged twelve to eighteen. The winning poems will be published in a small book next year. Enjoy the fair!"),
            ],
            [
                new("gap", L2, 2,
                [
                    Item(9, "This year the fair is in Hall ___.", "\"This year we've moved into Hall 4\" — last year it was Hall 2.", "4", "four"),
                    Item(10, "Over ___ publishers are taking part.", "\"This year we have over 120 publishers taking part.\"", "120"),
                    Item(11, "Tickets are cheaper if you buy them ___.", "\"If you buy it online, it's cheaper, only 15,000.\"", "online"),
                    Item(12, "Book signings take place in the ___ next to the café.", "\"Book signings take place every afternoon in the garden next to the café.\"", "garden"),
                    Item(13, "At the book swap stand, the limit is ___ books per person per day.", "\"There's a limit of five books per person per day.\"", "five", "5"),
                    Item(14, "The winners of the ___ competition will be announced on Sunday.", "\"We'll announce the winners of our poetry competition.\"", "poetry"),
                ]),
            ]);
    }

    private static ListeningPart Cl10P3() => new(3,
        "You will hear four people talking about friendships they have made online.",
        [
            Female("Speaker 1", "I got to know Aisha on a forum for amateur photographers. People always assume we met playing games, but I've never played a computer game in my life! We chatted almost every day for five years, but she lives in Malaysia, so we'd never actually seen each other except on video. Then this spring she came to Samarkand on holiday, and I went there to meet her. I was nervous that it would feel strange, but within five minutes it was as if we'd known each other forever."),
            Male("Speaker 2", "I've got quite a few online friends, mostly people from a group for fans of old films. We discuss what we've watched and sometimes send each other articles. But I have some rules. I never tell anyone my address, where I study or where I'll be at a certain time, and I don't post photos of my family. Some people think that's unfriendly, but you can never be completely sure who is on the other side of the screen."),
            Female("Speaker 3", "For about two years I've had a video call every Sunday with Lucia, a student from Italy. She's learning Russian, and I'm learning Italian, so we spend half an hour speaking each language. My pronunciation has improved enormously, and I've picked up lots of everyday expressions you don't find in textbooks. We've never met in person, and to be honest, I'm not sure we ever will, but she's become a real friend."),
            Male("Speaker 4", "When I moved to a new city for my first job, I didn't know a single person, and the evenings were very long. I joined an online group for runners, mainly to find good routes, but soon I was chatting with the members every night. They kept me going through those first difficult months. Some people say online friends aren't real friends, but I completely disagree. For me, they were just as important as anyone I'd met face to face."),
        ],
        [
            new("match", L3, null,
            [
                Item(15, "Speaker 1", "Speaker 1: \"This spring she came to Samarkand on holiday, and I went there to meet her.\"", "A"),
                Item(16, "Speaker 2", "Speaker 2: \"I never tell anyone my address, where I study or where I'll be at a certain time.\"", "C"),
                Item(17, "Speaker 3", "Speaker 3: \"She's learning Russian, and I'm learning Italian ... My pronunciation has improved enormously.\"", "F"),
                Item(18, "Speaker 4", "Speaker 4: \"I didn't know a single person ... They kept me going through those first difficult months.\"", "B"),
            ],
            [
                "I finally met my online friend face to face.",
                "An online friendship helped me through a lonely time.",
                "I am careful about how much I share with online friends.",
                "I met my online friend through computer games.",
                "I don't think online friends can replace real-life ones.",
                "My online friend has helped me improve a foreign language.",
            ]),
        ]);

    private static ListeningPart Cl10P4()
    {
        const string who = "Sports centre manager";
        var map = new MapPlan("Yoshlik Sports Complex",
            [new("Main entrance", 2, 4), new("Football pitch", 2, 1), new("Car park", 0, 4)],
            [
                new("A", 1, 3), new("B", 4, 4), new("C", 0, 2), new("D", 0, 0),
                new("E", 4, 0), new("F", 4, 2), new("G", 3, 3), new("H", 2, 0),
            ]);
        return new(4,
            "You will hear the manager of a sports complex describing it to new members at the main entrance.",
            [
                Male(who, "Good afternoon, and welcome to the Yoshlik Sports Complex. We're standing at the main entrance, in the middle of the southern side of the complex. The car park, where most of you left your cars, is in the south-western corner, to your left. Straight ahead of you, in the northern half of the complex, you can see the football pitch."),
                Male(who, "Just inside the entrance on the left, between the entrance and the football pitch, is the café. It's a popular meeting place, and it serves healthy snacks as well as hot meals. On the other side of the path, opposite the café, is the first-aid room. And if you came by bike, the bike racks are in the south-eastern corner, to the right of the entrance."),
                Male(who, "The swimming pool is on the eastern side of the complex, level with the path that runs across the complex just in front of the football pitch. It has an Olympic-sized pool and a smaller pool for children. On the western side, directly opposite the swimming pool, is the gym, which opens at six in the morning."),
                Male(who, "If you're playing football today, the changing rooms are directly behind the pitch, at the far end, in the middle of the northern side. You'll need to walk around the pitch to get there."),
                Male(who, "The tennis courts are in the north-western corner of the complex. There are six courts, and you can book them at reception or online."),
                Male(who, "And finally, in the opposite corner, the north-eastern one, is the climbing wall. Beginners are very welcome, but you'll need to do a short safety course first. Right, let's start the tour."),
            ],
            [
                new("map", L4, null,
                [
                    Item(19, "Café", "\"Just inside the entrance on the left, between the entrance and the football pitch, is the café.\"", "A"),
                    Item(20, "Swimming pool", "\"On the eastern side of the complex, level with the path ... just in front of the football pitch.\"", "F"),
                    Item(21, "Changing rooms", "\"Directly behind the pitch, at the far end, in the middle of the northern side.\"", "H"),
                    Item(22, "Tennis courts", "\"The tennis courts are in the north-western corner of the complex.\"", "D"),
                    Item(23, "Climbing wall", "\"In the opposite corner, the north-eastern one, is the climbing wall.\"", "E"),
                ], Map: map),
            ]);
    }

    private static ListeningPart Cl10P5() => new(5,
        "You will hear three extracts about weather and the seasons.",
        [
            Male("Narrator", "Extract One."),
            Male("Rustam", "Zarina, I've just looked at the forecast for the weekend. It says heavy rain in the mountains on Saturday, and possibly snow higher up. Maybe we should postpone the hike."),
            Female("Zarina", "Snow? In April? Well, it does happen. But it's only Wednesday, and mountain forecasts change all the time."),
            Male("Rustam", "I know, but last year we got caught in that storm, remember? I don't want to be walking along a narrow path in the rain again."),
            Female("Zarina", "Fair enough. Look, why don't we keep the booking at the guesthouse and decide on Friday evening? If it still looks bad, we can do the shorter walk along the river instead of going up to the lake."),
            Male("Rustam", "That sounds sensible."),
            Male("Narrator", "Extract Two."),
            Female("Presenter", "Doctor Karimov, many listeners say weather forecasts are often wrong. Is that fair?"),
            Male("Doctor Karimov", "I understand why people feel that way, but it isn't really true. Today, a forecast for the next three days is correct about ninety per cent of the time. That's far better than thirty years ago. Beyond a week, though, the atmosphere becomes too unpredictable, and a forecast for ten days ahead is really no more than a general indication."),
            Female("Presenter", "What about weather apps? Most people use those now."),
            Male("Doctor Karimov", "They're convenient, and many are based on good data. The problem is that they show a single symbol, a sun or a cloud, for the whole day, and people don't read the details. If the app says there's a thirty per cent chance of rain, that doesn't mean it won't rain. I'd encourage people to look beyond the symbol."),
            Male("Narrator", "Extract Three."),
            Male("Speaker", "If you ask people here which season they like best, most of them will say spring. I understand why: the blossom on the trees, the first warm days, everything turning green. But for me, nothing beats autumn. The heat of summer is finally over, but it's still warm enough to sit outside in the evenings. The markets are full of melons, grapes and pomegranates, and the colours in the parks are incredible. Summer, I have to admit, I find hard. People from colder countries imagine that sunshine every day must be wonderful, but when it's over forty degrees, you can't really do anything between noon and six. I end up spending most of July indoors, hiding from the sun, which feels like a waste of the longest days of the year."),
        ],
        [
            new("mcq", L5, null,
            [
                Choice(24, "Why does Rustam want to postpone the hike?", "A", "\"Last year we got caught in that storm, remember? I don't want to be walking ... in the rain again.\"",
                    "He had a bad experience on a previous trip.", "He has too much work at the weekend.", "The guesthouse is fully booked."),
                Choice(25, "What does Zarina suggest?", "C", "\"Why don't we keep the booking at the guesthouse and decide on Friday evening?\"",
                    "cancelling the guesthouse booking", "walking up to the lake on Friday", "waiting a few days before deciding"),
                Choice(26, "What does Doctor Karimov say about forecasts?", "B", "\"A forecast for the next three days is correct about ninety per cent of the time.\"",
                    "They are less accurate than thirty years ago.", "Short-term forecasts are usually reliable.", "Ten-day forecasts are as reliable as three-day ones."),
                Choice(27, "What is his view of weather apps?", "C", "\"People don't read the details ... I'd encourage people to look beyond the symbol.\"",
                    "They are based on poor data.", "People should stop using them.", "Users often misunderstand the information."),
                Choice(28, "Why does the speaker prefer autumn?", "B", "\"The heat of summer is finally over ... The markets are full of melons, grapes and pomegranates.\"",
                    "It is the season of many celebrations.", "The weather is pleasant and there is plenty of fruit.", "It is the best time to travel abroad."),
                Choice(29, "How does the speaker feel about summer?", "A", "\"When it's over forty degrees, you can't really do anything between noon and six.\"",
                    "The heat limits what he can do.", "He enjoys the long sunny days.", "He prefers it to spring."),
            ]),
        ]);

    private static ListeningPart Cl10P6()
    {
        const string who = "Lecturer";
        return new(6,
            "You will hear part of a university lecture about the history of space exploration.",
            [
                Male(who, "Good afternoon. Today we're going to look at the history of space exploration, from its beginnings in the 1950s to the challenges that lie ahead."),
                Male(who, "The space age began on the fourth of October 1957, when the Soviet Union launched Sputnik 1, the first artificial satellite. It was a metal ball only about fifty-eight centimetres across, and all it did was send out a simple radio signal. But it showed that an object could be placed in orbit around the Earth, and it started an intense competition between the Soviet Union and the United States."),
                Male(who, "Just four years later, in April 1961, Yuri Gagarin became the first human being in space. His flight lasted a hundred and eight minutes, during which he completed one orbit of the Earth. His rocket was launched from Baikonur, in what is now Kazakhstan, which is still one of the world's busiest launch sites today."),
                Male(who, "The next great goal was the Moon. In July 1969, the American astronauts Neil Armstrong and Buzz Aldrin became the first people to walk on its surface. Between 1969 and 1972, twelve astronauts walked on the Moon, and together they brought back nearly four hundred kilograms of rock, which scientists are still studying today."),
                Male(who, "After the Moon landings, the focus changed from short, dramatic missions to living and working in space for long periods. The International Space Station, built by several countries working together, has been continuously occupied since November 2000. Research on board has taught us a great deal about how the human body reacts to weightlessness. For example, astronauts lose bone and muscle unless they exercise for around two hours every day."),
                Male(who, "Meanwhile, robots have travelled much further than people. The two Voyager spacecraft, launched in 1977, flew past Jupiter and Saturn, and Voyager 1 is now the most distant human-made object, more than twenty billion kilometres from Earth. Robotic rovers have also driven across the surface of Mars, looking for signs that water, and perhaps life, once existed there."),
                Male(who, "In the last decade, perhaps the biggest change has been economic. For most of the space age, rockets were used only once and then fell into the sea. Today, private companies have developed rockets that can land again and be reused, which has greatly reduced the cost of launching satellites."),
                Male(who, "So what's next? Several countries are planning to send astronauts back to the Moon, this time for longer periods, and perhaps to build a permanent base there. A human mission to Mars is also being discussed, but the challenges are enormous. The journey would take around seven months in each direction, and the crew would be exposed to dangerous levels of radiation. Solving that problem is, in my view, the key to our future in space. We'll discuss it in more detail next week."),
            ],
            [
                new("gap", L6, 1,
                [
                    Item(30, "Sputnik 1 was the first artificial ___.", "\"Sputnik 1, the first artificial satellite.\"", "satellite"),
                    Item(31, "Gagarin's rocket was launched from ___, which is still a busy launch site.", "\"His rocket was launched from Baikonur ... still one of the world's busiest launch sites.\"", "Baikonur"),
                    Item(32, "Astronauts brought back nearly 400 kilograms of ___ from the Moon.", "\"They brought back nearly four hundred kilograms of rock.\"", "rock"),
                    Item(33, "Without daily exercise, astronauts lose bone and ___.", "\"Astronauts lose bone and muscle unless they exercise for around two hours every day.\"", "muscle"),
                    Item(34, "Modern rockets can land again and be ___, which has cut the cost of launches.", "\"Rockets that can land again and be reused, which has greatly reduced the cost.\"", "reused"),
                    Item(35, "A crew travelling to Mars would be exposed to dangerous levels of ___.", "\"The crew would be exposed to dangerous levels of radiation.\"", "radiation"),
                ]),
            ]);
    }

    // =====================================================================
    // TEST 11
    // =====================================================================

    private static ListeningPart Cl11P1() => new(1,
        "You will hear eight people starting short conversations. Choose the reply that fits best.",
        [
            Female("Waiter", "Would you like still or sparkling water with your meal?"),
            Male("Friend", "Guess what? I've been offered a place at university!"),
            Female("Flatmate", "Have you seen my keys anywhere? I'm going to be late."),
            Male("Customer at a post office", "How much does it cost to send this parcel to Kazakhstan?"),
            Female("Teacher", "You've made real progress this term. Well done."),
            Male("Neighbour", "Could you keep an eye on my flat while I'm away next week?"),
            Female("Receptionist", "Could you fill in this form and then take a seat, please?"),
            Male("Colleague", "I'm thinking of selling my car and cycling to work instead."),
        ],
        [
            new("mcq", L1, null,
            [
                Choice(1, "Choose the correct reply.", "C", "\"Still or sparkling?\" offers a choice; \"Still, please\" chooses.",
                    "Yes, I would.", "It's still raining.", "Still, please — no ice."),
                Choice(2, "Choose the correct reply.", "A", "\"I've been offered a place at university!\" is good news, so the reply congratulates.",
                    "That's fantastic news — congratulations!", "Which place did you guess?", "I've never been there."),
                Choice(3, "Choose the correct reply.", "B", "\"Have you seen my keys?\" — a helpful suggestion: \"Have you checked your coat pocket?\"",
                    "I'm late too.", "Have you checked your coat pocket?", "Yes, they open the door."),
                Choice(4, "Choose the correct reply.", "C", "\"How much does it cost to send this parcel?\" — the clerk must weigh it first.",
                    "It costs a lot to live there.", "I sent one last year.", "Let me weigh it first, and I'll tell you."),
                Choice(5, "Choose the correct reply.", "A", "\"You've made real progress ... Well done\" is praise, answered with thanks.",
                    "Thank you — I've been working really hard.", "Yes, the term is nearly over.", "I'll do it next term."),
                Choice(6, "Choose the correct reply.", "B", "\"Could you keep an eye on my flat?\" asks a favour; \"Of course\" agrees and offers more help.",
                    "My eyes are fine, thanks.", "Of course. Shall I water your plants as well?", "I've been away for a week."),
                Choice(7, "Choose the correct reply.", "C", "\"Could you fill in this form?\" — \"Sure\" agrees and asks a relevant question.",
                    "I'll take the seat home.", "It's already full.", "Sure. Do I need to put my address as well?"),
                Choice(8, "Choose the correct reply.", "B", "\"I'm thinking of ... cycling to work\" — a natural reaction questions the plan.",
                    "I sold it last year.", "Really? Isn't it a long way from your house?", "Cycling is a sport."),
            ]),
        ]);

    private static ListeningPart Cl11P2()
    {
        const string who = "Course tutor";
        return new(2,
            "You will hear a tutor talking about a photography course for beginners.",
            [
                Male(who, "Hello, everyone, and thanks for coming to this introductory evening. My name's Akmal Rashidov, and I'll be your tutor on the Photography for Beginners course. I'd like to tell you how the course works before you decide whether to sign up."),
                Male(who, "The course lasts ten weeks. Some of you may have seen an old advert that said eight, but we've added two extra sessions on editing because students asked for them."),
                Male(who, "We'll meet every Wednesday evening from seven to nine, here in the arts centre. There's just one exception: in week five, the session will move to Thursday, because the room is being used for a concert."),
                Male(who, "You don't need an expensive camera to join. In fact, about half of our students use a smartphone, and they often take the most creative pictures. What matters is learning to see, not the equipment. Whatever you use, please bring a charger, as some of our sessions are long."),
                Male(who, "Every few weeks we'll leave the classroom and go on a photo walk. The first one, in week three, is to the flower market early on a Saturday morning, when the light is soft and the stalls are full of colour. Later in the course, we'll photograph the old town at sunset."),
                Male(who, "The full course costs 900,000 som, but students get a fifteen per cent discount, and you can pay in two parts if that's easier."),
                Male(who, "Finally, at the end of the course, each of you will choose your three best photographs, and we'll display them in an exhibition in the entrance hall of the arts centre. Your families and friends are all welcome to come to the opening. Now, does anyone have any questions?"),
            ],
            [
                new("gap", L2, 2,
                [
                    Item(9, "The course lasts ___ weeks.", "\"The course lasts ten weeks\" — the old advert said eight.", "ten", "10"),
                    Item(10, "Classes are usually held on ___ evenings.", "\"We'll meet every Wednesday evening\" — Thursday is only in week five.", "Wednesday"),
                    Item(11, "About half of the students take pictures with a ___.", "\"About half of our students use a smartphone.\"", "smartphone"),
                    Item(12, "The first photo walk is to the ___ market.", "\"The first one, in week three, is to the flower market.\"", "flower"),
                    Item(13, "Students get a ___ per cent discount.", "\"Students get a fifteen per cent discount.\"", "fifteen", "15"),
                    Item(14, "At the end, the best photographs will be shown in an ___.", "\"We'll display them in an exhibition in the entrance hall.\"", "exhibition"),
                ]),
            ]);
    }

    private static ListeningPart Cl11P3() => new(3,
        "You will hear four people talking about how they keep fit.",
        [
            Female("Speaker 1", "I used to pay a fortune for a gym membership, and to be honest, I hardly ever went. So last year I cancelled it. Now I run in the park three mornings a week, and on the other days I do exercises at home using my own body weight: push-ups, squats, that kind of thing. I don't need any equipment at all, apart from a pair of decent trainers, which I already had. I'm actually fitter now than when I was paying for the gym."),
            Male("Speaker 2", "For about ten years I ran almost every day, and I did a couple of half-marathons. Then two years ago I hurt my knee badly, and the doctor told me that running on hard roads was out of the question for a while. I was really upset at first, but I took up swimming instead. It's much kinder to the joints, and I've grown to love it. I still miss running sometimes, but I don't think I'll ever go back to it seriously."),
            Male("Speaker 3", "I'm not really a sporty person. The reason I play football every Saturday is that it's the only time I see my old school friends. We've played together on the same pitch for fifteen years. Afterwards we always go for tea and talk for hours. My wife says I sleep better on Saturday nights, but I'm not so sure about that. If the others stopped playing, I think I'd stop too."),
            Female("Speaker 4", "I go to the gym at six in the morning, before work, when it's almost empty. I put my headphones on and follow my own programme, which I write down in an old notebook. I don't use any of those fitness apps. I did try group classes a few years ago, but I hated having an instructor shouting at me and trying to keep up with everyone else. On my own, I can go at my own pace and nobody bothers me."),
        ],
        [
            new("match", L3, null,
            [
                Item(15, "Speaker 1", "Speaker 1: \"I cancelled it ... I don't need any equipment at all, apart from a pair of decent trainers, which I already had.\"", "F"),
                Item(16, "Speaker 2", "Speaker 2: \"I hurt my knee badly ... I took up swimming instead.\"", "C"),
                Item(17, "Speaker 3", "Speaker 3: \"The reason I play football every Saturday is that it's the only time I see my old school friends.\"", "E"),
                Item(18, "Speaker 4", "Speaker 4: \"I did try group classes ... but I hated [it] ... On my own, I can go at my own pace.\"", "A"),
            ],
            [
                "I prefer exercising alone to exercising in a group.",
                "I use an app to plan my training.",
                "I changed my kind of exercise after an injury.",
                "Exercise has helped me to sleep better.",
                "I exercise mainly to spend time with friends.",
                "I keep fit without spending any money.",
            ]),
        ]);

    private static ListeningPart Cl11P4()
    {
        const string who = "Hospital volunteer";
        var map = new MapPlan("City Hospital: ground floor",
            [new("Main entrance", 2, 4), new("Reception", 2, 3), new("Lifts", 2, 1)],
            [
                new("A", 0, 4), new("B", 4, 4), new("C", 1, 2), new("D", 0, 1),
                new("E", 3, 2), new("F", 4, 1), new("G", 1, 0), new("H", 3, 0),
            ]);
        return new(4,
            "You will hear a volunteer describing the ground floor of City Hospital to new patients and visitors.",
            [
                Female(who, "Good morning, and welcome to City Hospital. I'm one of the volunteers here, and I'd like to explain where everything is on the ground floor. We're at the main entrance, in the middle of the south wall of the building, and just in front of us is the reception desk. A long corridor runs from reception straight ahead to the lifts."),
                Female(who, "On your left as you come in, in the south-west corner, is the café. It's open from seven in the morning until eight at night. In the opposite corner, the south-east corner, is the pharmacy, where you can collect any medicine your doctor prescribes."),
                Female(who, "Now, if you walk along the corridor towards the lifts, halfway there you'll find two doors facing each other. The one on the left is the blood test room. Please take a ticket from the machine and wait until your number is called. The door on the right leads to the eye clinic."),
                Female(who, "When you reach the lifts, turn right and go to the end of the corridor. The X-ray department is there, on the east wall, level with the lifts. If you turn left at the lifts instead and go to the end, you'll reach the physiotherapy department, on the west wall."),
                Female(who, "Behind the lifts, along the north wall of the building, there are two more rooms. The one on the left, as you face the lifts, is a staff room, so please don't go in there. The one on the right is the waiting area for outpatients, which is where most of you will need to go for your appointments. There's water and plenty of seating."),
                Female(who, "If you get lost, just ask anyone wearing a yellow badge."),
            ],
            [
                new("map", L4, null,
                [
                    Item(19, "Pharmacy", "\"In the opposite corner, the south-east corner, is the pharmacy.\"", "B"),
                    Item(20, "Blood test room", "\"Halfway there you'll find two doors facing each other. The one on the left is the blood test room.\"", "C"),
                    Item(21, "X-ray department", "\"Turn right and go to the end of the corridor ... on the east wall, level with the lifts.\"", "F"),
                    Item(22, "Physiotherapy department", "\"If you turn left at the lifts instead ... the physiotherapy department, on the west wall.\"", "D"),
                    Item(23, "Outpatients' waiting area", "\"Behind the lifts ... The one on the right is the waiting area for outpatients.\"", "H"),
                ], Map: map),
            ]);
    }

    private static ListeningPart Cl11P5() => new(5,
        "You will hear three extracts about transport.",
        [
            Male("Narrator", "Extract One."),
            Female("Lola", "Botir, you live in Yunusabad too, don't you? I was wondering whether you'd be interested in sharing a car to work. We could take turns driving, a week each."),
            Male("Botir", "It's a nice idea. We'd save on petrol, and parking here is a nightmare. My only worry is the timing. I often have to stay late on Thursdays for meetings, and I don't want you waiting around for me."),
            Female("Lola", "That's no problem. On Thursdays I can just take the metro home. It's only the mornings when the traffic really drives me mad."),
            Male("Botir", "In that case, let's give it a try. Shall we start next Monday?"),
            Male("Narrator", "Extract Two."),
            Female("Interviewer", "Alisher, you've been a train driver for twenty-two years. What do you enjoy most about the job?"),
            Male("Alisher", "People expect me to say the speed, but it's actually the landscape. On the line between Tashkent and Samarkand, I see the same fields every day, but they're never the same. In spring they're bright green, in summer they turn gold, and in winter there's sometimes frost on everything. I never get tired of it."),
            Female("Interviewer", "And how has the job changed over the years?"),
            Male("Alisher", "Completely. When I started, the journey took about four hours. On the new high-speed trains it's just over two. The cabs are full of computer screens now, and much of the driving is controlled electronically. Some older drivers found that hard, but personally I welcomed it. It's safer, and I'm less tired at the end of a shift."),
            Male("Narrator", "Extract Three."),
            Female("Speaker", "A couple of years ago, electric scooters suddenly appeared everywhere in our city. You could rent one with an app and leave it wherever you liked. At first I was completely against them. They were left lying across pavements, and people rode them far too fast, sometimes two people on one scooter. But I have to admit I've changed my mind, at least partly. For short journeys, say to the metro, they're quick, cheap and they don't pollute the air. The problem isn't the scooters themselves, it's the lack of rules. In my opinion, the city should create special parking areas for them and set a speed limit near schools and markets. Then everyone could benefit."),
        ],
        [
            new("mcq", L5, null,
            [
                Choice(24, "Why does Lola suggest sharing a car with Botir?", "B", "\"Botir, you live in Yunusabad too, don't you?\"",
                    "Her own car has broken down.", "They live in the same area.", "She wants to learn to drive."),
                Choice(25, "What is Botir worried about?", "C", "\"My only worry is the timing. I often have to stay late on Thursdays.\"",
                    "the cost of petrol", "finding a parking space", "sometimes finishing work late"),
                Choice(26, "What does Alisher enjoy most about his job?", "B", "\"People expect me to say the speed, but it's actually the landscape.\"",
                    "driving at high speed", "seeing the countryside change", "meeting different passengers"),
                Choice(27, "How does Alisher feel about the new technology?", "C", "\"Some older drivers found that hard, but personally I welcomed it. It's safer.\"",
                    "He finds it difficult to use.", "He thinks it has made the job less interesting.", "He is positive about it."),
                Choice(28, "How did the speaker feel when the scooters first appeared?", "A", "\"At first I was completely against them.\"",
                    "She disapproved of them.", "She was excited to try them.", "She thought they were too expensive."),
                Choice(29, "What does the speaker want the city to do?", "B", "\"The problem ... is the lack of rules ... create special parking areas ... and set a speed limit.\"",
                    "ban electric scooters completely", "introduce clear rules for scooters", "build more metro stations"),
            ]),
        ]);

    private static ListeningPart Cl11P6()
    {
        const string who = "Lecturer";
        return new(6,
            "You will hear part of a university lecture about the languages of the world.",
            [
                Female(who, "Good morning. How many languages do you think are spoken in the world today? When I ask this question, students usually guess a few hundred. The real answer is about seven thousand. Today I want to look at how these languages are distributed, how they are related to one another, and why so many of them are in danger."),
                Female(who, "The first thing to notice is how unequal the distribution is. Around half of the world's population speaks one of just twenty or so languages, such as Chinese, English, Spanish, Hindi and Arabic. At the other extreme, thousands of languages have fewer than ten thousand speakers each."),
                Female(who, "Languages are also very unevenly spread across the map. Europe, for example, has fewer than three hundred languages. The island of New Guinea, by contrast, has more than a thousand, even though its population is relatively small. One reason is geography: high mountains and thick forests kept communities apart for thousands of years, so their languages developed separately."),
                Female(who, "Linguists group languages into families, rather like a family tree. Languages in the same family descend from a common ancestor that was spoken long ago. English, Russian, Persian and Hindi, for instance, all belong to the Indo-European family, which is the largest in terms of speakers. Uzbek belongs to the Turkic family, together with Turkish, Kazakh, Azerbaijani and several others. That's why an Uzbek speaker can often understand simple phrases in Kazakh without ever having studied it. Some languages, however, have no known relatives at all. These are called isolates. The best-known example is Basque, which is spoken in parts of Spain and France."),
                Female(who, "Now to the most worrying part of the story. Linguists estimate that around forty per cent of the world's languages are endangered, and some believe that half of them could disappear by the end of this century. A language rarely disappears suddenly. The usual pattern is that parents stop passing it on to their children, often because another language seems more useful for education and work. When the last generation of fluent speakers dies, the language dies with them."),
                Female(who, "Does this matter? Many linguists argue that it does. Every language contains knowledge that may exist nowhere else, for example names for local plants and their uses, or special ways of describing the natural world. When a language is lost, this knowledge can be lost too."),
                Female(who, "The good news is that languages can be revived. Welsh is a well-known example: after many years of decline, the number of young speakers has grown, largely thanks to schools that teach entirely in Welsh. Maori in New Zealand has seen a similar revival. Technology is helping too, as communities make recordings of elderly speakers and create dictionaries and apps. Next week we'll look at how children learn more than one language at the same time."),
            ],
            [
                new("gap", L6, 1,
                [
                    Item(30, "About half of the world's population speaks one of only ___ or so languages.", "\"Around half of the world's population speaks one of just twenty or so languages.\"", "twenty", "20"),
                    Item(31, "In New Guinea, mountains and forests kept ___ apart, so their languages developed separately.", "\"High mountains and thick forests kept communities apart for thousands of years.\"", "communities"),
                    Item(32, "Uzbek belongs to the ___ family of languages.", "\"Uzbek belongs to the Turkic family.\"", "Turkic"),
                    Item(33, "Languages with no known relatives are called ___.", "\"Some languages ... have no known relatives at all. These are called isolates.\"", "isolates"),
                    Item(34, "A language usually dies when parents stop passing it on to their ___.", "\"Parents stop passing it on to their children.\"", "children"),
                    Item(35, "Welsh has gained young speakers thanks to ___ that teach entirely in Welsh.", "\"Largely thanks to schools that teach entirely in Welsh.\"", "schools"),
                ]),
            ]);
    }
}
