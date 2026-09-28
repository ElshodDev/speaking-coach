using SpeakingCoach.Api.Services.Mock;
using static SpeakingCoach.Api.Services.Mock.CefrObjective;

namespace SpeakingCoach.Api.Services.Content;

// CEFR (Multilevel) Reading — Test 2–6. Original matnlar; tuzilishi Reading1 bilan bir xil:
// 5 qism (6 + 8 + 6 + 9 + 6 = 35 savol), ko'rsatmalar CefrObjective'dagi rasmiy matn.
// Matn va savollar mualliflik ishi — haqiqiy imtihon materiallaridan ko'chirilmagan.
public static partial class BuiltInCefr
{
    public static readonly ReadingTest Reading2 = new("", [Reading2Part1(), Reading2Part2(), Reading2Part3(), Reading2Part4(), Reading2Part5()]);
    public static readonly ReadingTest Reading3 = new("", [Reading3Part1(), Reading3Part2(), Reading3Part3(), Reading3Part4(), Reading3Part5()]);
    public static readonly ReadingTest Reading4 = new("", [Reading4Part1(), Reading4Part2(), Reading4Part3(), Reading4Part4(), Reading4Part5()]);
    public static readonly ReadingTest Reading5 = new("", [Reading5Part1(), Reading5Part2(), Reading5Part3(), Reading5Part4(), Reading5Part5()]);
    public static readonly ReadingTest Reading6 = new("", [Reading6Part1(), Reading6Part2(), Reading6Part3(), Reading6Part4(), Reading6Part5()]);

    // =====================================================================
    // READING — TEST 2
    // =====================================================================

    private static ReadingPassage Reading2Part1() => new("Sports day at School No. 21",
        """
        Dear parents and pupils,

        Our annual sports day will take place next Friday, 16 May, at the city stadium on Bobur Street. Every class will take part this year, and families are very welcome to watch.

        Pupils should arrive at school at 8 a.m. as usual. Buses will then take everyone to the (1) ______, and the first races will start at 9.30.

        All pupils must wear their school sports uniform and a pair of comfortable trainers. Please do not send children in brand-new (2) ______, because they can hurt the feet if they have not been worn before.

        The programme includes running races, the long jump, football and, as always, a tug-of-war between the teachers and the parents. The teachers have won for three years in a row, so any (3) ______ who would like to help change that should give their name to Mr Rakhimov by Wednesday.

        Lunch will not be provided, so please give your child a packed lunch and plenty of water. The afternoon can be very hot, and every pupil must bring a hat. Anyone who arrives without a (4) ______ will have to watch the events from the shade.

        Medals will be given to the top three runners in every race, and the class with the most points at the end of the day will receive the school cup. The winning (5) ______ will also have its photo on the school website.

        In the afternoon, pupils will return to school on the same (6) ______, arriving at about 3.30 p.m. If it rains heavily, sports day will move to the following Monday.

        Best wishes,
        Nigora Saidova
        Head teacher
        """,
        [
            new("gap", R1, 1,
            [
                Item(1, "Buses will then take everyone to the ___, and the first races will start at 9.30.", "Sports day takes place \"at the city stadium on Bobur Street\".", "stadium"),
                Item(2, "Please do not send children in brand-new ___, because they can hurt the feet if they have not been worn before.", "Pupils must wear \"a pair of comfortable trainers\" — only shoes can hurt the feet.", "trainers"),
                Item(3, "The teachers have won for three years in a row, so any ___ who would like to help change that should give their name to Mr Rakhimov by Wednesday.", "The tug-of-war is \"between the teachers and the parents\", so the parents want to beat the teachers.", "parents", "parent"),
                Item(4, "Anyone who arrives without a ___ will have to watch the events from the shade.", "\"The afternoon can be very hot, and every pupil must bring a hat.\"", "hat"),
                Item(5, "The winning ___ will also have its photo on the school website.", "\"The class with the most points at the end of the day will receive the school cup.\"", "class"),
                Item(6, "In the afternoon, pupils will return to school on the same ___, arriving at about 3.30 p.m.", "In the morning \"buses will then take everyone\" to the stadium.", "buses"),
            ]),
        ]);

    private static ReadingPassage Reading2Part2() => new("Summer jobs for students",
        """
        7. Ice-cream seller, Ecopark. We need cheerful young people to sell ice cream from our kiosks in the park from June to August. The work is simple, and the park is a lovely place to spend the summer. Shifts are four or six hours long, and you decide which days suit you: just tell the manager on Sunday evening, and she will plan the week around your timetable. Pay is weekly.

        8. Day camp helper. Sunshine Day Camp is looking for energetic helpers for its summer programme for children aged five to eight. You will help our teachers with games, arts and crafts, and trips to the swimming pool. No previous experience is needed, but you must be patient and enjoy spending time with little ones. The camp runs from Monday to Friday, 9 a.m. to 4 p.m., for six weeks.

        9. Hotel receptionist, Samarkand. A small family hotel near the Registan needs a receptionist for July and August. Most of our guests come from Europe, so you must be able to communicate confidently in English or French. Duties include checking guests in and out and answering their questions about the city. Please note that staff cannot stay at the hotel, so you will need to live nearby.

        10. Waiter or waitress, Old Town Café. Our busy café in the city centre needs extra staff for the summer season. The work is hard, especially at lunchtime, but the team is friendly and the tips are good. Staff work eight-hour shifts, and every shift includes a free hot meal from our kitchen. Please apply in person with a short CV on any weekday afternoon.

        11. Fruit pickers, Fergana Valley. An apricot and cherry farm needs pickers for June and early July. The farm is thirty kilometres from the nearest town, but you do not need a car: our minibus will collect you from the railway station when you arrive. Pickers sleep in shared rooms on the farm at no cost, and there is a shared kitchen where you can cook your own meals.

        12. Customer support assistant. An online bookshop is looking for students to answer customers' questions by email and chat. You will work from your own home, so you need a laptop and a reliable internet connection. The hours are fixed: Monday to Friday, 9 a.m. to 1 p.m. You will only need to visit our office once, on your first day, to sign your contract.

        13. Delivery rider. Deliver food to homes around the city on one of our electric bikes. You do not need a driving licence, and a bike and helmet are provided. Shifts are given out by the manager each week. Saturday and Sunday shifts pay thirty per cent more than weekday ones, because that is when we are busiest. Riders must be at least eighteen years old.

        14. Museum guide assistant. The City History Museum needs assistants to show visitors around its new exhibition. Before you start, you will spend a week with our senior guides, learning about the collection and practising your tours. Most of our visitors are local families, so foreign languages are useful but not required. The job is ideal for history students.
        """,
        [
            new("match", R2, null,
            [
                Item(7, "Text 7", "\"You decide which days suit you ... she will plan the week around your timetable.\"", "F"),
                Item(8, "Text 8", "\"Our summer programme for children aged five to eight.\"", "E"),
                Item(9, "Text 9", "\"You must be able to communicate confidently in English or French.\"", "B"),
                Item(10, "Text 10", "\"Every shift includes a free hot meal from our kitchen.\"", "D"),
                Item(11, "Text 11", "\"Pickers sleep in shared rooms on the farm at no cost.\"", "G"),
                Item(12, "Text 12", "\"You will work from your own home.\"", "I"),
                Item(13, "Text 13", "\"Saturday and Sunday shifts pay thirty per cent more than weekday ones.\"", "A"),
                Item(14, "Text 14", "\"Before you start, you will spend a week with our senior guides ... practising your tours.\"", "J"),
            ],
            [
                "Working at the weekend is better paid.",
                "You must speak at least one foreign language.",
                "You need a driving licence for this job.",
                "Food is provided while you are working.",
                "You will be working with young children.",
                "You can choose the days you work.",
                "You can stay at the workplace free of charge.",
                "Only people with experience can apply.",
                "You will do this job from home.",
                "You will be trained before you start the job.",
            ]),
        ]);

    private static ReadingPassage Reading2Part3() => new("Three generations of plov",
        """
        15. When Saodat Karimova cooked plov for her neighbours' weddings in the 1980s, she had no plans to become a businesswoman. Cooking for two hundred guests was simply something people in her mahalla did for one another. But her plov was so popular that strangers began knocking on her door to ask whether she would cook for them, too. In 1994, with her husband's help, she turned the front room of their house into a small café with four tables and a single large pot.

        16. The early years were hard. The family had no money for advertising, so the café depended entirely on its customers. Saodat's husband went to the market at five every morning to buy meat and vegetables, and the café stayed open until the last guest had gone home. A newspaper published a short article about the café in 1998, but it brought only a handful of new visitors. What really filled the tables was people telling their friends. Within six years, the Karimovs had moved to a proper building with room for sixty diners.

        17. In 2008 Saodat's son Bakhtiyor took over the business. When the first international burger restaurants opened in the neighbourhood, he feared that young people would stop coming, so he added pizza, pasta and salads to the menu. The experiment lasted less than a year. Regular customers complained that the café had lost its character, and the new dishes rarely sold. "They were telling me they came for my mother's cooking," Bakhtiyor says, "not for something they could get anywhere." The European dishes quietly disappeared.

        18. Visitors often ask for the recipe, but there isn't one, at least not on paper. Saodat never wrote anything down. Instead, she taught her son by making him stand beside her at the pot, day after day, until he could tell by the smell when the onions were ready and by the sound when the rice needed water. Bakhtiyor has trained his own cooks in the same way. "You can't learn this from a book," he says. "Your hands have to remember it."

        19. The biggest test came in 2020, when the dining room had to close for several months. For a restaurant built on the pleasure of eating together, it seemed like a disaster. It was Bakhtiyor's daughter Madina, then a business student, who suggested selling plov for delivery through an app. Her father was doubtful, but within weeks the orders were arriving faster than the kitchen could cook. Today, even with the dining room full again, deliveries make up nearly a third of the restaurant's income.

        20. Now the family is discussing the future. Madina would like to open a second restaurant on the other side of the city, and several investors have offered money. Her father is less sure. He worries that the food would not taste the same without a member of the family in the kitchen. For now, the older generation has won the argument. "I would rather have one restaurant I am proud of," Bakhtiyor says, "than five that I am not."
        """,
        [
            new("match", R3, null,
            [
                Item(15, "Paragraph 15", "\"She had no plans to become a businesswoman ... she turned the front room of their house into a small café.\"", "D"),
                Item(16, "Paragraph 16", "\"What really filled the tables was people telling their friends.\"", "G"),
                Item(17, "Paragraph 17", "\"Regular customers complained that the café had lost its character ... The European dishes quietly disappeared.\"", "A"),
                Item(18, "Paragraph 18", "\"Saodat never wrote anything down. Instead, she taught her son by making him stand beside her.\"", "E"),
                Item(19, "Paragraph 19", "\"It seemed like a disaster ... Madina ... suggested selling plov for delivery through an app.\"", "H"),
                Item(20, "Paragraph 20", "\"I would rather have one restaurant I am proud of than five that I am not.\"", "B"),
            ],
            [
                "Customers who wanted things to stay the same",
                "Why the family is not expanding yet",
                "How a newspaper made the café famous",
                "An unplanned start in business",
                "Skills passed on without writing",
                "Learning to cook at a professional school",
                "Success spread by personal recommendation",
                "A difficult period that led to a new idea",
            ]),
        ]);

    private static ReadingPassage Reading2Part4() => new("Online or in the classroom?",
        """
        Not so long ago, studying online was widely seen as a second-best option, something for people who could not get to a "real" school or university. When classrooms around the world closed in 2020, that changed almost overnight. Millions of students and teachers who had never used video lessons were suddenly forced to do so, and many were surprised by what they found. Today, few people argue that online learning is simply inferior. The more interesting question is which kind of learning suits which kind of learner.

        The main attraction of online study is flexibility. A nurse working night shifts, a parent with young children or a student living in a remote village can all follow the same course at a time that suits them. Recorded lectures can be paused while a learner takes notes, and difficult explanations can be watched two or three times. In one survey of adult learners, the freedom to study at any time of day was the reason most often given for choosing an online course, well ahead of lower fees or the chance to avoid travel.

        Yet flexibility has a price. Free online courses that are open to anyone regularly attract tens of thousands of participants, but it is not unusual for fewer than one in ten of them to reach the end. Some of these learners never intended to finish; they simply wanted to sample a subject. Many others, however, give up because nothing forces them to continue. In a classroom, a teacher notices when a student is absent, and classmates ask where they have been. Online, a learner can quietly disappear, and nobody may notice for weeks.

        Supporters of traditional classes point to other advantages, too. A good teacher can see from students' faces when an explanation has not worked and try again in a different way. Questions can be asked and answered immediately, and a single comment from a classmate can start a discussion that nobody had planned. Researchers who study learning have long argued that we learn a great deal from one another, not only from the teacher. At the same time, a classroom is no guarantee of active learning. A student sitting silently at the back of a lecture hall with three hundred others may be no more involved than one watching a video at home.

        So what does the research actually say? When studies compare students who take the same course online and in person, the overall difference in results is often surprisingly small, provided that the online course has been carefully designed rather than simply recorded. Averages, however, can hide important differences. Several studies have suggested that students who are already confident and well organised do just as well online, while those who were struggling before tend to fall further behind. For these learners, the regular contact and structure of a classroom seem to matter most.

        Increasingly, schools and universities are trying to combine the strengths of both. In what is often called a "flipped" classroom, students watch short video lectures at home, at their own pace, and then use class time for discussion, problem-solving and practical work: the activities that benefit most from having a teacher and classmates in the room. Many teachers who have tried this approach say that it makes lessons livelier, although it only works if students actually watch the videos before they arrive.

        The debate between online and classroom learning, then, may be based on the wrong question. Neither is automatically better. A motivated professional who wants to learn a new software package may be perfectly served by a well-made online course, while a teenager who finds it hard to concentrate may need the routine and human contact of a classroom. What matters most is not where learning takes place, but whether the method matches the needs of the person who is doing the learning.
        """,
        [
            new("mcq", R4Mcq, null,
            [
                Choice(21, "According to the first paragraph, how did many people view online study in the past?", "C", "\"Studying online was widely seen as a second-best option.\"",
                    "as more modern than classroom learning", "as suitable only for adults", "as less valuable than traditional study", "as too expensive for most students"),
                Choice(22, "In the survey mentioned in the second paragraph, adult learners chose online courses mainly because", "B", "\"The freedom to study at any time of day was the reason most often given ... well ahead of lower fees or the chance to avoid travel.\"",
                    "they were cheaper than classroom courses.", "they could study whenever they wanted.", "they did not have to travel to lessons.", "they could watch lectures more than once."),
                Choice(23, "According to the writer, why do many learners who start free online courses give up?", "C", "\"Many others, however, give up because nothing forces them to continue.\"",
                    "The courses are too difficult for them.", "Their teachers do not answer their questions.", "There is no pressure on them to keep going.", "They find the videos too long."),
                Choice(24, "What does the research described in the fifth paragraph suggest?", "A", "\"The overall difference in results is often surprisingly small, provided that the online course has been carefully designed.\"",
                    "Well-designed online courses can produce results similar to classroom courses.", "Online students usually get better results than classroom students.", "Weaker students make more progress when they study online.", "Recorded lectures are as effective as specially designed online courses."),
            ]),
            new("tfng", R4Tfng, null,
            [
                Item(25, "Free online courses often have more than ten thousand participants.", "\"Free online courses ... regularly attract tens of thousands of participants.\"", "TRUE"),
                Item(26, "Researchers believe that students learn only from their teachers.", "\"We learn a great deal from one another, not only from the teacher.\"", "FALSE"),
                Item(27, "Students in large lecture halls usually get lower marks than students in small classes.", "The text says such a student may not be \"involved\", but it says nothing about marks.", "NOT GIVEN"),
                Item(28, "Students who were already having difficulties tend to do worse when they study online.", "\"Those who were struggling before tend to fall further behind.\"", "TRUE"),
                Item(29, "According to teachers, a flipped classroom works even if students do not watch the videos.", "\"It only works if students actually watch the videos before they arrive.\"", "FALSE"),
            ]),
        ]);

    private static ReadingPassage Reading2Part5() => new("How rivers shape the land",
        """
        Few natural forces have done more to shape the surface of the Earth than running water. Over thousands and millions of years, rivers have cut deep valleys through mountains, carried vast quantities of rock and soil towards the sea, and built new land where none existed before. To understand how they do this, geographers usually divide a river's work into three processes: erosion, transportation and deposition.

        Erosion is the wearing away of the land, and a river erodes in several ways. The sheer force of moving water, known as hydraulic action, can loosen and remove material from the bed and banks, especially where there are cracks in the rock. More important in many rivers, however, is abrasion. Here the river uses the sand, pebbles and boulders it carries, collectively called its load, rather like sandpaper, scraping and grinding the rock over which it flows. At the same time, the stones themselves are knocked against one another and gradually become smaller and rounder, a process known as attrition. Finally, some rocks, such as limestone, are slowly dissolved by the water.

        The type of landscape a river creates depends largely on where along its course we look. Near its source, in the mountains, a river flows down a steep slope. Its energy is directed mainly downwards, so it cuts into its bed and produces a narrow valley with steep sides, shaped like the letter V. Where a band of hard rock lies on top of softer rock, a waterfall may form. The softer rock beneath is worn away more quickly, leaving the hard rock above without support until it eventually collapses. In this way, a waterfall slowly moves upstream, leaving a steep-sided gorge behind it. Niagara Falls, for example, is thought to have moved back by around eleven kilometres over the past twelve thousand years.

        Further downstream, the land becomes flatter and the river begins to wind from side to side in large bends called meanders. Water flows fastest around the outside of each bend, where it erodes the bank and creates a small cliff. On the inside of the bend, where the current is slower, the river drops some of its load, forming a gently sloping beach of sand and gravel. Because one side of the bend is constantly being worn away while the other is being built up, meanders gradually change shape and move across the valley floor. Sometimes, during a flood, the river cuts straight across the narrow neck of land between two bends. The old bend is then left behind as a curved, still body of water known as an oxbow lake.

        As a river approaches the sea, it usually slows down, and deposition becomes its main activity. When rivers flood, water spreads out over the surrounding land and loses speed, dropping fine particles of mud and silt. Over centuries, these layers build up a wide, flat floodplain, whose soils are often among the most fertile in the world. The heaviest material is dropped first, close to the channel, and this can form natural raised banks, called levees, along the river's edges.

        Where a river enters a sea or lake whose water is fairly calm, it may deposit so much sediment that new land forms at its mouth. This feature is called a delta, after the Greek letter Δ, whose triangular shape the delta of the Nile resembles. Deltas are home to millions of people and support intensive farming, but they are also fragile. When dams are built upstream, much of the sediment that once reached the coast is trapped behind them, and some deltas have begun to shrink as a result.

        The work of rivers is usually too slow to see within a human lifetime, yet occasionally it happens dramatically. A single major flood can move more material in a few days than a river normally transports in years, and it can change the course of a channel overnight. Understanding these processes is therefore not only of academic interest: it helps engineers decide where it is safe to build and helps communities prepare for the power of water.
        """,
        [
            new("gap", R5Gap, 2,
            [
                Item(30, "In abrasion, a river uses its ___, such as sand and pebbles, to grind the rock over which it flows.", "\"The sand, pebbles and boulders it carries, collectively called its load.\"", "load"),
                Item(31, "Where hard rock lies on top of softer rock, a ___ may form and slowly move upstream.", "\"Where a band of hard rock lies on top of softer rock, a waterfall may form.\"", "waterfall"),
                Item(32, "If a river cuts across the neck of land between two bends during a flood, the old bend becomes an ___.", "\"The old bend is then left behind as a curved, still body of water known as an oxbow lake.\"", "oxbow lake"),
                Item(33, "Mud and silt left by floods build up a floodplain, whose soils are often very ___.", "\"A wide, flat floodplain, whose soils are often among the most fertile in the world.\"", "fertile"),
            ]),
            new("mcq", R5Mcq, null,
            [
                Choice(34, "Why do meanders gradually move across the valley floor?", "B", "\"Because one side of the bend is constantly being worn away while the other is being built up, meanders gradually change shape and move.\"",
                    "Floods regularly destroy the old bends.", "One bank is eroded while the opposite bank is built up.", "Water flows fastest on the inside of each bend.", "The river carries less load as it goes downstream."),
                Choice(35, "What problem affecting some deltas does the writer mention?", "D", "\"When dams are built upstream, much of the sediment ... is trapped behind them, and some deltas have begun to shrink.\"",
                    "Intensive farming has made their soils less fertile.", "Rough seas have washed away their sediment.", "Too many people have moved away from them.", "Dams prevent sediment from reaching them."),
            ]),
        ]);

    // =====================================================================
    // READING — TEST 3
    // =====================================================================

    private static ReadingPassage Reading3Part1() => new("Our book club's next meeting",
        """
        Hi everyone,

        I hope you all enjoyed our last meeting. It was lovely to see so many new faces!

        Our next meeting will be on Thursday, 12 June, at 6.30 p.m. The café where we usually meet is closed for repairs, so this time we will be in the small reading room on the first floor of the city library. The (1) ______ closes to the public at 8.30 p.m., so we will need to finish on time.

        For June we have chosen The Orchard at the End of the Road, so please start reading soon! The library has kindly ordered six copies for our members, but that is not enough for everyone. If you are able to buy the book yourself, please do, and leave the library's (2) ______ for members who can't.

        As always, the meeting will begin with a short introduction to the book by one member. This time it is Jasur's turn, and he has promised that his (3) ______ will last no more than ten minutes, so that everyone has time to speak.

        Several of you have asked whether you can bring friends. New members are always welcome, but please let me know by Monday how many (4) ______ you are bringing, because the room only has twenty chairs.

        We will also vote on our book for July. Please email me the title of one novel you would like to read. At the meeting, each person will have one (5) ______, and the most popular title will win.

        Finally, tea and biscuits will be provided during the break, but please don't bring any food or drinks into the reading (6) ______ itself. We will have our tea in the corridor outside.

        See you there,
        Dildora
        Book club organiser
        """,
        [
            new("gap", R1, 1,
            [
                Item(1, "The ___ closes to the public at 8.30 p.m., so we will need to finish on time.", "The meeting is \"in the small reading room on the first floor of the city library\" — the building closes.", "library"),
                Item(2, "If you are able to buy the book yourself, please do, and leave the library's ___ for members who can't.", "\"The library has kindly ordered six copies for our members.\"", "copies"),
                Item(3, "This time it is Jasur's turn, and he has promised that his ___ will last no more than ten minutes.", "\"The meeting will begin with a short introduction to the book by one member.\"", "introduction"),
                Item(4, "Please let me know by Monday how many ___ you are bringing, because the room only has twenty chairs.", "\"Several of you have asked whether you can bring friends.\"", "friends"),
                Item(5, "At the meeting, each person will have one ___, and the most popular title will win.", "\"We will also vote on our book for July.\"", "vote"),
                Item(6, "Please don't bring any food or drinks into the reading ___ itself.", "The meeting is \"in the small reading room\".", "room"),
            ]),
        ]);

    private static ReadingPassage Reading3Part2() => new("Holiday homes to rent",
        """
        7. Mountain cabin, Chimgan. This cosy wooden cabin sleeps four and stands among pine trees, a short walk from the ski slopes. It is open all year round, and a wood stove keeps it warm even in January. Please note that there is no Wi-Fi and the phone signal is very weak, so this is the perfect place for a real break from screens. The nearest shop is five kilometres away.

        8. Lakeside house, Charvak. A modern single-storey house with a garden that goes right down to the water. All the rooms are on the ground floor, the doors are extra wide, and the bathroom has been specially designed for wheelchair users. The house sleeps six. A small rowing boat can be hired from our neighbours for 100,000 som a day.

        9. Guesthouse in the old town, Bukhara. Our traditional guesthouse has five rooms around a shady courtyard with a fountain. It is only a short walk from the bazaar, cafés and shops, so you won't need a car during your stay; in fact, cars cannot enter most of the narrow streets nearby. Guests are welcome to use the kitchen to make tea and coffee.

        10. Village house, Nurata. Stay with a family of carpet weavers in a quiet village at the foot of the mountains. The house has three guest rooms, and meals can be ordered from the family for a small extra charge. If you are interested, your hosts will be happy to teach you the basics of weaving on their own looms, and many guests go home with a small rug they have made themselves.

        11. Farmhouse near Tashkent. Planning a family reunion or a holiday with friends? This large farmhouse has seven bedrooms and sleeps up to fourteen people. All the bedrooms are upstairs, and there is a huge table in the kitchen where everyone can eat together. Outside there is a swimming pool and a barbecue area.

        12. City-centre flat, Tashkent. This bright two-bedroom flat is on the fifth floor of an older building without a lift, and it has fast Wi-Fi and a view over the park. Dogs and cats are very welcome at no extra charge, and you will find water bowls and a dog bed in the flat. The park opposite is ideal for morning walks with your pet.

        13. Villa near the old city, Khiva. Our villa sleeps four and has a private garden full of fruit trees. To keep our prices low, we only accept bookings of seven nights or more, so the villa is best for travellers who want to explore the region slowly. The owners live next door and are always ready to help with advice or taxis.

        14. Stone cottage, Zaamin. A charming cottage in the hills, surrounded by juniper forests. Every morning, fresh bread, eggs and honey from the owners' garden are served on the terrace, and this is included in the price. The cottage sleeps three and is a good base for walking in the national park.
        """,
        [
            new("match", R2, null,
            [
                Item(7, "Text 7", "\"There is no Wi-Fi and the phone signal is very weak.\"", "I"),
                Item(8, "Text 8", "\"All the rooms are on the ground floor ... designed for wheelchair users.\"", "E"),
                Item(9, "Text 9", "\"Only a short walk from the bazaar, cafés and shops, so you won't need a car.\"", "C"),
                Item(10, "Text 10", "\"Your hosts will be happy to teach you the basics of weaving on their own looms.\"", "D"),
                Item(11, "Text 11", "\"This large farmhouse has seven bedrooms and sleeps up to fourteen people.\"", "H"),
                Item(12, "Text 12", "\"Dogs and cats are very welcome at no extra charge.\"", "B"),
                Item(13, "Text 13", "\"We only accept bookings of seven nights or more.\"", "F"),
                Item(14, "Text 14", "\"Fresh bread, eggs and honey ... are served on the terrace, and this is included in the price.\"", "G"),
            ],
            [
                "Guests can use a boat free of charge.",
                "Guests can bring their pets.",
                "Guests do not need a car to reach the shops.",
                "Guests can learn a traditional craft.",
                "It is suitable for guests who cannot climb stairs.",
                "There is a minimum length of stay.",
                "Breakfast is included in the price.",
                "It is suitable for a large group.",
                "Guests will not be able to go online.",
                "It can only be booked in the warmer months.",
            ]),
        ]);

    private static ReadingPassage Reading3Part3() => new("The girl who made water cleaner",
        """
        15. In the village where Laylo Tursunova's grandmother lives, in the hills of Namangan region, the water comes from an old well. Every family boils it before drinking, which means burning wood or gas every day, and in summer many people buy bottled water instead, although it is expensive and leaves piles of plastic behind. Laylo spent her school holidays in the village, and at the age of fourteen she began to wonder whether there might be a simpler and cheaper way to make the water cleaner.

        16. The answer came from somewhere unexpected: her grandmother's kitchen. Like many families in the region, hers dried apricots every summer and burned the hard stones in the stove. One evening Laylo read that charcoal made from fruit stones and nutshells had been used to clean water for centuries, because its surface is full of tiny holes that trap dirt and some harmful substances. She suddenly realised that the material she needed was being burned as fuel every day.

        17. Turning the idea into a working filter took almost two years. Her first attempts were failures: some filters cracked, others became blocked after a few litres, and one made the water taste of smoke. Laylo recorded every attempt in a notebook, noting what she had changed and what had happened. "My chemistry teacher told me that each failure was information," she says. "After a while, I stopped feeling disappointed and started feeling curious." By the thirty-first version, the filter was working.

        18. Last year she entered a national competition for young scientists and finished second out of more than two hundred entries. Not everyone was convinced, however. Several adults at the competition suggested that a parent or a teacher must have done most of the work. Laylo did not argue. Instead, she brought out her notebooks and showed the judges every failed design, every measurement and every date. One judge later admitted that the notebooks had impressed him even more than the filter itself.

        19. Making one filter in a school laboratory is very different from producing hundreds. The charcoal has to be prepared in exactly the same way every time, and each filter must be tested before anyone can sell it. A small workshop in her town has agreed to produce a first batch, and scientists at a local university are helping her to check the results. Laylo has discovered that this stage involves much less chemistry and much more paperwork than she had imagined.

        20. Now sixteen, Laylo hopes to study chemical engineering. When younger students ask her how to become an inventor, her advice is always the same: do not start by trying to invent something. "Start by looking for a problem that real people have," she says, "and then be patient. The idea is the easy part." She is still improving the filter, and her next aim is to find a way to remove bacteria as well.
        """,
        [
            new("match", R3, null,
            [
                Item(15, "Paragraph 15", "\"Every family boils it before drinking ... bottled water ... is expensive and leaves piles of plastic behind.\"", "E"),
                Item(16, "Paragraph 16", "\"The answer came from somewhere unexpected: her grandmother's kitchen.\"", "B"),
                Item(17, "Paragraph 17", "\"Each failure was information ... I stopped feeling disappointed and started feeling curious.\"", "H"),
                Item(18, "Paragraph 18", "\"Adults ... suggested that a parent or a teacher must have done most of the work ... she brought out her notebooks.\"", "A"),
                Item(19, "Paragraph 19", "\"Making one filter in a school laboratory is very different from producing hundreds.\"", "G"),
                Item(20, "Paragraph 20", "\"Start by looking for a problem that real people have ... and then be patient.\"", "D"),
            ],
            [
                "Proving that the work was her own",
                "An answer found in the kitchen",
                "Winning first prize",
                "Advice for future inventors",
                "An everyday problem in a village",
                "Selling the filter abroad",
                "From one filter to many",
                "Learning from every mistake",
            ]),
        ]);

    private static ReadingPassage Reading3Part4() => new("Walking: the underrated exercise",
        """
        When people decide to get fit, they usually think of joining a gym, taking up running or buying a bicycle. Walking rarely makes the list. It seems too ordinary to count as exercise: we all do it every day, it requires no special equipment, and it costs nothing. Yet a growing body of research suggests that this most ordinary of activities may be one of the most valuable things we can do for our bodies and our minds.

        The physical benefits are well established. Regular walking strengthens the heart, helps to control weight and blood pressure, and reduces the risk of type 2 diabetes. The World Health Organization advises adults to do at least 150 minutes of moderate physical activity every week, and a brisk walk, one that makes you breathe a little faster but still allows you to talk, counts towards that total. In other words, half an hour of brisk walking on five days a week is enough to meet the basic recommendation.

        Many people now track their activity with a phone or a smartwatch, and the most popular target is ten thousand steps a day. Surprisingly, this figure did not come from medical research. It is usually traced back to a Japanese company that sold one of the first step counters in the 1960s; the name of its product meant, roughly, "ten-thousand-step meter". Since then, scientists have tested the number. Some large studies of older adults have found that the health benefits rise steadily up to around seven or eight thousand steps a day and then level off, with relatively small gains after that. For people who currently walk very little, even a few thousand extra steps can make a real difference.

        Walking also appears to be good for the mind. In one study, volunteers were asked to walk for ninety minutes either through a natural area with grass and trees or along a busy road. Afterwards, those who had walked in nature reported spending less time dwelling on negative thoughts about themselves, and brain scans showed reduced activity in an area linked to such thoughts. The group that had walked beside the traffic showed no such change. Findings like these have led some doctors to recommend walks in parks as one way of coping with stress.

        There may even be benefits for creativity. Researchers at Stanford University asked people to think of unusual uses for everyday objects, either while sitting or while walking. On average, the walkers came up with around sixty per cent more ideas. Most interestingly, the effect appeared even when participants walked on a treadmill facing a blank wall, which suggests that it is the movement itself, rather than a change of scenery, that helps the ideas to flow.

        If walking is so beneficial, why do so many people do so little of it? Part of the answer lies in the way modern cities are built. In many places, streets are designed mainly for cars, pavements are narrow or missing, and shops and workplaces are too far apart to reach on foot. Climate matters, too: in a hot summer, a midday walk can be unpleasant or even dangerous. Experts suggest small changes that fit into daily life, such as getting off the bus one stop early, taking the stairs instead of the lift, or holding "walking meetings", in which colleagues discuss their work while walking together outside.

        None of this means that walking can replace every other form of exercise. People who want to build strength or improve their performance in a sport will need more demanding training. But for most people, the biggest health gains come not from becoming athletes but from moving from doing almost nothing to doing something. Walking, the activity we so often overlook, may be the easiest way to take that first step.
        """,
        [
            new("mcq", R4Mcq, null,
            [
                Choice(21, "According to the writer, why do people rarely think of walking as exercise?", "A", "\"It seems too ordinary to count as exercise: we all do it every day.\"",
                    "It seems too much like an everyday activity.", "It takes too much time to produce results.", "It requires expensive equipment.", "Doctors rarely recommend it."),
                Choice(22, "What does the writer say about the target of ten thousand steps a day?", "C", "\"This figure did not come from medical research ... the name of its product meant ... 'ten-thousand-step meter'.\"",
                    "It was chosen after medical research in Japan.", "It is too low for most adults.", "It first appeared in the name of a product.", "It is the official advice of the World Health Organization."),
                Choice(23, "What did the study described in the fourth paragraph find?", "B", "\"Those who had walked in nature reported spending less time dwelling on negative thoughts about themselves.\"",
                    "Walking beside a busy road reduced stress as much as walking in nature.", "People who walked in nature thought less about negative things.", "Both groups showed changes in their brain activity.", "Walks of ninety minutes were too long for most volunteers."),
                Choice(24, "What does the writer find most interesting about the Stanford study?", "D", "\"The effect appeared even when participants walked on a treadmill facing a blank wall.\"",
                    "The walkers produced fewer ideas than people who sat.", "The participants preferred to walk outdoors.", "Only people who walked regularly became more creative.", "Walking helped even when there was nothing new to look at."),
            ]),
            new("tfng", R4Tfng, null,
            [
                Item(25, "Half an hour of brisk walking on five days a week meets the basic recommendation for adults.", "\"Half an hour of brisk walking on five days a week is enough to meet the basic recommendation.\"", "TRUE"),
                Item(26, "Most people who track their steps manage to reach ten thousand a day.", "The text says ten thousand is \"the most popular target\" but not how many people reach it.", "NOT GIVEN"),
                Item(27, "Studies show that walking more than eight thousand steps a day brings no extra benefit at all.", "The benefits level off, \"with relatively small gains after that\" — small gains are still gains.", "FALSE"),
                Item(28, "The volunteers who walked in nature had been suffering from stress before the study.", "Nothing is said about the volunteers' condition before the walk.", "NOT GIVEN"),
                Item(29, "The design of some cities makes walking more difficult.", "\"Streets are designed mainly for cars, pavements are narrow or missing.\"", "TRUE"),
            ]),
        ]);

    private static ReadingPassage Reading3Part5() => new("The science of rainbows",
        """
        Few natural sights have attracted as much curiosity as the rainbow. Ancient philosophers, including Aristotle, attempted to explain it, but real progress came only when scholars began to experiment. In the early fourteenth century, a German friar, Theodoric of Freiberg, studied the paths of light through large glass globes filled with water, treating each globe as a giant raindrop. About three hundred years later, René Descartes used geometry to calculate the angle at which a rainbow appears, and Isaac Newton's experiments with prisms soon showed that white sunlight is in fact a mixture of all the colours of the spectrum.

        A rainbow forms when sunlight meets a large number of falling raindrops. As a ray of light enters a drop, it slows down and changes direction, a process called refraction. Crucially, each colour is bent by a slightly different amount: violet light is bent the most and red light the least, so the colours begin to separate. The light then reflects off the back of the drop and is refracted again as it leaves, which separates the colours further. The light that emerges is concentrated at an angle of about 42 degrees from the direction of the incoming sunlight for red, and about 40 degrees for violet.

        This geometry explains where rainbows can be seen. The observer must stand with the sun behind them and rain in front. The centre of the rainbow is always the point directly opposite the sun, which is marked by the shadow of the observer's head. Every drop that lies at 42 degrees from this point sends red light towards the eye, so together these drops form a circle. Normally the ground hides the lower part of the circle, which is why we see an arch. The same geometry explains why rainbows are most common in the early morning and late afternoon: if the sun is higher than about 42 degrees above the horizon, the whole bow lies below the horizon and cannot be seen from the ground. From an aircraft, however, a complete circle is sometimes visible.

        One consequence of this is that a rainbow is not an object in a particular place. The light reaching one person's eyes comes from one set of drops, while a person standing a few metres away sees light from a different set. Strictly speaking, therefore, no two people ever see exactly the same rainbow. And because the bow is always at the same angle from the observer, it moves whenever the observer moves, which is why nobody has ever reached its end.

        On some occasions, a second, fainter bow appears outside the first. This secondary rainbow is produced by light that reflects twice inside each drop before leaving it. The extra reflection reverses the order of the colours, so red is on the inside of the secondary bow rather than the outside, and the bow appears at about 51 degrees. Some light is lost at each reflection, which is why the secondary bow is always weaker. Between the two bows the sky looks noticeably darker, because neither kind of reflection sends light towards the observer from that region. This darker area is known as Alexander's band, after the Greek philosopher Alexander of Aphrodisias, who described it around 200 AD.

        The size of the drops also matters. Large raindrops produce bright, clearly separated colours, whereas very small droplets, such as those in fog or mist, produce a broad, almost white arc known as a fogbow. Sometimes faint extra bands of pink and green can be seen just inside the main bow. These supernumerary bows could not be explained by the simple idea of light travelling in straight rays. They were finally explained in the early nineteenth century by Thomas Young, who showed that they result from light waves interfering with one another, and they became important evidence that light behaves as a wave.
        """,
        [
            new("gap", R5Gap, 2,
            [
                Item(30, "In the fourteenth century, Theodoric of Freiberg used large glass ___ filled with water as models of raindrops.", "\"Studied the paths of light through large glass globes filled with water, treating each globe as a giant raindrop.\"", "globes"),
                Item(31, "Inside a raindrop, the colours separate because each colour is ___ by a slightly different amount.", "\"Each colour is bent by a slightly different amount.\"", "bent"),
                Item(32, "A rainbow cannot be seen from the ground if the sun is more than about ___ degrees above the horizon.", "\"If the sun is higher than about 42 degrees above the horizon, the whole bow lies below the horizon.\"", "42", "forty-two"),
                Item(33, "Very small droplets, such as those in fog, produce an almost white arc called a ___.", "\"A broad, almost white arc known as a fogbow.\"", "fogbow"),
            ]),
            new("mcq", R5Mcq, null,
            [
                Choice(34, "According to the text, why has nobody ever reached the end of a rainbow?", "C", "\"It moves whenever the observer moves, which is why nobody has ever reached its end.\"",
                    "It is always much further away than it looks.", "It disappears as soon as the rain stops.", "It moves whenever the person watching it moves.", "Different people see it in different places."),
                Choice(35, "Why were supernumerary bows important in the history of science?", "A", "\"They became important evidence that light behaves as a wave.\"",
                    "They helped to show that light behaves as a wave.", "They showed that raindrops are not perfectly round.", "They proved that Descartes' calculations were wrong.", "They explained why the secondary bow is weaker."),
            ]),
        ]);

    // =====================================================================
    // READING — TEST 4
    // =====================================================================

    private static ReadingPassage Reading4Part1() => new("New bus route 47",
        """
        NEW BUS ROUTE 47: CENTRAL STATION – UNIVERSITY TOWN

        From Monday, 1 September, a new bus route will connect the city centre with University Town for the first time. Route 47 will start at Central Station and end at the main gate of the university, stopping at the Central Market, the City Hospital and Olmazor Park on the way. The whole journey will take about thirty-five minutes.

        Buses will run every ten minutes during the morning and evening rush hours and every twenty (1) ______ at other times. The first bus will leave Central Station at 6 a.m., and the last bus will leave the university at 11 p.m.

        All buses on the new route are electric, so they are much quieter and cleaner than the older (2) ______ that still run on some city routes.

        A single journey costs 2,000 som. Passengers can pay with a transport card or a bank card. Transport cards can be bought at any metro station and topped up on the city transport website. If you do not have a (3) ______, you can buy a paper ticket from the machine at each stop.

        Please note that the bus stop outside Olmazor Park has been moved 100 metres to the north. Passengers who used the old (4) ______ should look for the new blue sign.

        During the first two weeks, there may be short delays near the City Hospital because of repairs to the road. Passengers with appointments at the (5) ______ are advised to allow an extra ten minutes.

        A full timetable is displayed at every stop along the route. It can also be downloaded from the (6) ______, where you can also see a live map of the route.
        """,
        [
            new("gap", R1, 1,
            [
                Item(1, "Buses will run every ten minutes during the morning and evening rush hours and every twenty ___ at other times.", "\"The whole journey will take about thirty-five minutes.\"", "minutes"),
                Item(2, "They are much quieter and cleaner than the older ___ that still run on some city routes.", "\"All buses on the new route are electric\" — they are compared with older buses.", "buses"),
                Item(3, "If you do not have a ___, you can buy a paper ticket from the machine at each stop.", "\"Passengers can pay with a transport card or a bank card.\"", "card"),
                Item(4, "Passengers who used the old ___ should look for the new blue sign.", "\"The bus stop outside Olmazor Park has been moved 100 metres to the north.\"", "stop"),
                Item(5, "Passengers with appointments at the ___ are advised to allow an extra ten minutes.", "\"There may be short delays near the City Hospital.\"", "hospital"),
                Item(6, "It can also be downloaded from the ___, where you can also see a live map of the route.", "\"Topped up on the city transport website.\"", "website"),
            ]),
        ]);

    private static ReadingPassage Reading4Part2() => new("Evening courses this autumn",
        """
        7. Italian Cooking. Every Monday evening for eight weeks, chef Dilshod Aminov, who trained in Rome, will show you how to make fresh pasta, risotto and classic sauces. All ingredients are included in the fee. At the end of each class, you can pack the dish you have prepared into a container that we provide and take it home to share with your family.

        8. Accounting for Small Businesses. This twelve-week course on Wednesday evenings covers bookkeeping, taxes and financial planning for people who run, or plan to run, their own business. No entrance test is required. At the end of the course, students sit a written exam, and those who pass receive a nationally recognised certificate.

        9. Spanish (Intermediate). This course is for learners who already know some Spanish and want to speak more fluently. To make sure that everyone is in the right group, all new students take a short placement test before they are accepted. Classes take place on Tuesday evenings and focus on conversation, with some grammar revision.

        10. Watercolour Painting. Discover the basic techniques of watercolour on Tuesday evenings in our bright studio. Our tutor is a working artist who has exhibited across Central Asia. The fee covers teaching only: you will need to buy your own paints, brushes and paper, which cost about 200,000 som in total. A shopping list will be sent to you when you register.

        11. Astronomy for Beginners. Learn to recognise the planets, the brightest stars and the constellations. Classes meet every Friday evening in the garden of the city observatory, where telescopes are set up under the open sky. Please dress warmly, as the temperature drops quickly after dark, and bring a small torch with a red light if you have one.

        12. Yoga for Busy People. These gentle one-hour classes are designed to help you relax after a long day at work. They take place every Monday and Thursday evening, and you are expected to attend both. Mats are provided. If you have to miss a session, a recording of each class is available online so that you can practise at home.

        13. Guitar for Beginners. Always wanted to play the guitar but never had the chance? In this ten-week course on Wednesday evenings, you will learn basic chords and play simple songs. Not sure whether the guitar is for you? Come along to the first class without paying and decide afterwards. Guitars can be borrowed for the first month.

        14. Local History. Every Thursday evening, a university lecturer will introduce you to the history of our city, from ancient times to the present day. Why not bring a friend? History is always more fun when you can discuss it. The course ends with a day trip by train to Samarkand, and the cost of the trip is included in the fee.
        """,
        [
            new("match", R2, null,
            [
                Item(7, "Text 7", "\"You can pack the dish you have prepared into a container ... and take it home.\"", "D"),
                Item(8, "Text 8", "\"Those who pass receive a nationally recognised certificate.\"", "I"),
                Item(9, "Text 9", "\"All new students take a short placement test before they are accepted.\"", "J"),
                Item(10, "Text 10", "\"You will need to buy your own paints, brushes and paper.\"", "E"),
                Item(11, "Text 11", "\"Classes meet ... in the garden of the city observatory ... under the open sky.\"", "F"),
                Item(12, "Text 12", "\"They take place every Monday and Thursday evening, and you are expected to attend both.\"", "A"),
                Item(13, "Text 13", "\"Come along to the first class without paying.\"", "G"),
                Item(14, "Text 14", "\"The course ends with a day trip by train to Samarkand.\"", "B"),
            ],
            [
                "Classes are held twice a week.",
                "Participants will go on a trip.",
                "There is a discount for people who sign up with a friend.",
                "Participants take home something they have made.",
                "Students need to buy their own materials.",
                "The course is held outdoors.",
                "The first class is free.",
                "Classes are held online.",
                "The course leads to an official qualification.",
                "New students must take a test before joining.",
            ]),
        ]);

    private static ReadingPassage Reading4Part3() => new("Rishton: a town shaped by clay",
        """
        15. Walk down almost any street in Rishton, a small town in the Fergana Valley, and you will soon find a potter's workshop. Some are large studios with shelves of finished plates and bowls; others are nothing more than a wheel and a kiln in the corner of a family courtyard. Local people like to say that nearly every family in the town has someone who makes, paints or sells ceramics. This may be an exaggeration, but it is clear that one craft has shaped the life of the whole community for centuries.

        16. The reason lies, quite literally, beneath the potters' feet. The soil around Rishton contains a fine, even clay that can be used with very little preparation. In many pottery centres, clay has to be brought from far away or carefully cleaned and mixed before it can be shaped, but here, the potters say, the earth itself seems to have been made for their work. It was this local clay that first attracted craftsmen to the town, and it remains the foundation of their trade today.

        17. What makes Rishton ceramics instantly recognisable, however, is their colour: a deep, bright blue, often combined with turquoise and white. Traditionally, this comes from a glaze called ishkor, made with the ashes of plants that grow in the dry steppe. Preparing the glaze is slow, and the results are never entirely predictable. The temperature of the kiln, the thickness of the glaze and even the weather on the day of firing can change the final shade, so no two pieces are ever quite the same.

        18. In the twentieth century, much of the town's production moved into a large factory that turned out plates and cups by the thousand, following standard designs. The factory provided steady jobs, but hand-painting and the traditional glaze were used less and less, and some old techniques were kept alive only by a few elderly masters working at home. By the end of the century, there was a real danger that knowledge built up over many generations would disappear with them.

        19. Since then, family workshops have gradually returned. Young people have become apprentices to the remaining masters, and many workshops now welcome visitors. Tourists can watch a potter shape a bowl on the wheel, see designs being painted by hand and, in some studios, even try making a simple pot themselves. For the craftsmen, these visits bring not only income but also the satisfaction of seeing their work appreciated by people from all over the world.

        20. Success, however, brings its own pressures. Many visitors want cheap souvenirs, and some workshops have started using ready-made industrial glazes, which are quicker and cheaper than ishkor but lack its depth of colour. Older masters worry that the town's reputation could suffer if quality falls. Most workshops try to find a balance, selling simple, affordable pieces alongside the slow, carefully glazed work that made Rishton famous in the first place.
        """,
        [
            new("match", R3, null,
            [
                Item(15, "Paragraph 15", "\"Nearly every family in the town has someone who makes, paints or sells ceramics ... one craft has shaped the life of the whole community.\"", "C"),
                Item(16, "Paragraph 16", "\"The reason lies, quite literally, beneath the potters' feet ... a fine, even clay.\"", "G"),
                Item(17, "Paragraph 17", "\"The results are never entirely predictable ... no two pieces are ever quite the same.\"", "A"),
                Item(18, "Paragraph 18", "\"There was a real danger that knowledge built up over many generations would disappear.\"", "H"),
                Item(19, "Paragraph 19", "\"Many workshops now welcome visitors. Tourists can watch a potter shape a bowl.\"", "D"),
                Item(20, "Paragraph 20", "\"Some workshops have started using ready-made industrial glazes, which are quicker and cheaper.\"", "F"),
            ],
            [
                "A colour that cannot be fully controlled",
                "A festival that brings the world to the town",
                "A town built around one craft",
                "Opening workshop doors to visitors",
                "Studying abroad to learn new styles",
                "The temptation of quick profits",
                "The gift beneath their feet",
                "When the old methods almost disappeared",
            ]),
        ]);

    private static ReadingPassage Reading4Part4() => new("Children and screens: what does the evidence say?",
        """
        Few topics cause as much worry among parents as screen time. Many mothers and fathers feel guilty whenever their child picks up a tablet, while others see digital devices as an essential part of modern childhood. Headlines do not help: one week a newspaper warns that screens are damaging a whole generation, and the next it reports that video games improve problem-solving skills. So what does the evidence actually tell us?

        For the youngest children, the advice is fairly clear. In 2019 the World Health Organization recommended that children under the age of two should have no sedentary screen time at all, and that those aged two to four should have no more than one hour a day, with less being better. The main reason is not that screens themselves are poisonous, but that very young children learn best through physical play and face-to-face interaction with adults. Every hour in front of a screen is an hour not spent crawling, building, talking and being talked to.

        This idea, which researchers call "displacement", is central to much of the debate. The question is often not what a screen does to a child, but what it replaces. A teenager who spends two hours on a phone after finishing homework, playing football and eating dinner with the family may be perfectly fine. A teenager whose phone use cuts into sleep is a different case. Numerous studies have found that using devices in bed is linked to later bedtimes and poorer sleep, and lack of sleep has well-known effects on mood, concentration and school performance.

        For older children and teenagers, the picture is much less certain than headlines suggest. One widely discussed analysis examined data on more than 350,000 adolescents and found that the link between digital technology use and well-being, although negative, was extremely small: comparable in size to the link between well-being and regularly eating potatoes. Critics of this study argue that averages can hide serious problems among a minority of heavy users, and that some kinds of use, such as constantly comparing oneself with others on social media, may be more harmful than others. Both sides agree, however, that most studies show only that two things occur together, not that one causes the other.

        What children watch, and how they watch it, also matters. Educational programmes designed for young children, especially those that encourage them to respond, have been shown to support early reading and number skills. Video calls with grandparents, in which a child talks to and reacts to a real person, are very different from watching videos alone. Researchers also stress the value of "co-viewing": when a parent watches alongside a child and talks about what they see, the child tends to understand and remember more.

        Perhaps the most uncomfortable finding for parents concerns their own behaviour. Children copy what they see, and several studies suggest that when parents are frequently absorbed in their own phones, conversations at home become shorter and less frequent. A parent who tells a child to put down a tablet while checking messages at the dinner table is sending a mixed message.

        Rather than counting minutes, many experts now encourage families to ask a few simple questions. Is screen use getting in the way of sleep, physical activity, homework or time together? Is the content suitable? Does the child seem happy and in control? If the answers are reassuring, the exact number of hours may matter less than parents fear. If they are not, it may be time to change the family's habits, starting with the adults.
        """,
        [
            new("mcq", R4Mcq, null,
            [
                Choice(21, "According to the first paragraph, why do parents find it hard to know what to think about screen time?", "A", "\"Headlines do not help: one week a newspaper warns that screens are damaging a whole generation, and the next it reports that video games improve problem-solving skills.\"",
                    "The media give them conflicting messages.", "Doctors refuse to give them any advice.", "Their children hide how they use screens.", "Technology changes too quickly to study."),
                Choice(22, "What is the main reason for the WHO's advice about very young children?", "B", "\"Very young children learn best through physical play and face-to-face interaction ... Every hour in front of a screen is an hour not spent crawling, building, talking.\"",
                    "Screens can damage young children's eyesight.", "Screen time takes the place of activities through which children learn.", "Programmes for young children are usually of poor quality.", "Screens make it difficult for young children to sleep."),
                Choice(23, "The writer mentions the analysis of more than 350,000 adolescents to show that", "D", "\"The link between digital technology use and well-being, although negative, was extremely small.\"",
                    "technology is seriously harming most teenagers.", "heavy users of social media are in the majority.", "eating potatoes affects teenagers' well-being.", "the negative effect found was much smaller than many people think."),
                Choice(24, "What does the writer say about \"co-viewing\"?", "C", "\"When a parent watches alongside a child and talks about what they see, the child tends to understand and remember more.\"",
                    "It is only useful for educational programmes.", "It works best during video calls with grandparents.", "It helps children to understand and remember more.", "It encourages children to watch for longer."),
            ]),
            new("tfng", R4Tfng, null,
            [
                Item(25, "According to the WHO, a three-year-old should have no more than one hour of sedentary screen time a day.", "\"Those aged two to four should have no more than one hour a day.\"", "TRUE"),
                Item(26, "The writer believes that any teenager who uses a phone for two hours a day will have problems.", "Such a teenager \"may be perfectly fine\".", "FALSE"),
                Item(27, "Teenagers sleep less during the school week than at weekends.", "The text links devices in bed to poorer sleep but does not compare school days and weekends.", "NOT GIVEN"),
                Item(28, "Critics of the large analysis think it may hide serious problems among some heavy users.", "\"Critics of this study argue that averages can hide serious problems among a minority of heavy users.\"", "TRUE"),
                Item(29, "Most studies have proved that screen use causes unhappiness.", "\"Most studies show only that two things occur together, not that one causes the other.\"", "FALSE"),
            ]),
        ]);

    private static ReadingPassage Reading4Part5() => new("From flower to jar: how bees make honey",
        """
        Honey has been part of the human diet for thousands of years. A rock painting in eastern Spain, thought to be around eight thousand years old, appears to show a person collecting honey from a wild bees' nest, and beekeeping was well established in ancient Egypt. Yet the process by which bees turn the watery liquid of flowers into thick, long-lasting honey has been properly understood only relatively recently. It is a remarkable example of cooperation, chemistry and careful control of the environment inside the hive.

        The raw material of honey is nectar, a sugary liquid produced by flowers to attract insects. Nectar is mostly water: depending on the plant, it is often more than 70 per cent water, and its main sugar is usually sucrose. Foraging bees, which are older female workers, suck up nectar through a long, tube-like tongue and store it in a special organ called the honey stomach, which is separate from the part of the gut they use to digest their own food. On a single trip, a forager may visit dozens of flowers, sometimes more than a hundred, before returning to the hive.

        The transformation begins even before the bee reaches home. Glands in the forager's head add enzymes to the nectar, the most important of which is invertase. This enzyme splits sucrose into two simpler sugars, glucose and fructose, which are the main sugars in finished honey. Back at the hive, the forager passes the nectar mouth to mouth to younger worker bees, who continue to process it, adding more enzymes as they do so. Another enzyme, glucose oxidase, produces an acid and small amounts of hydrogen peroxide, both of which help to protect the honey against microbes.

        The next challenge is to remove most of the water. The younger bees spread the nectar in thin layers across the wax cells of the honeycomb, which increases the surface from which water can evaporate. Thousands of bees then fan their wings to move warm, dry air through the hive, which is kept at around 35 degrees Celsius. Gradually the water content falls, and when it reaches about 18 per cent, the honey is ready. The bees then seal each cell with a thin cap of wax, which keeps out moisture from the air.

        The wax itself is costly to produce. It is made by young worker bees, which release tiny flakes of it from glands on the underside of their bodies, and making it requires a great deal of energy, which comes from honey. Beekeepers usually estimate that bees use several kilograms of honey to produce a single kilogram of wax. This is one reason why many modern beekeepers return the empty honeycombs to the hive after taking out the honey, saving the bees a great deal of work.

        The chemistry of finished honey explains why it keeps so well. With so little water and so much sugar, honey draws moisture out of any bacteria or fungi that land in it, and they cannot survive. Its acidity and the hydrogen peroxide produced by glucose oxidase add further protection. Stored in a closed container, honey can remain edible for years, and there are reports of honey found in ancient tombs that was still in good condition.

        Honey is not a single, uniform product. Its colour, flavour and texture depend on the flowers the bees have visited: honey from cotton or sunflowers, for instance, tastes quite different from honey made from mountain herbs. Many honeys eventually crystallise, becoming thick and cloudy. This is often mistaken for a sign that the honey has gone bad or has been mixed with sugar, but it is a natural process caused by glucose forming crystals, and it happens faster in honeys that contain more glucose. Gently warming the jar in water returns the honey to its liquid form.
        """,
        [
            new("gap", R5Gap, 2,
            [
                Item(30, "Foraging bees carry nectar back to the hive in a special organ known as the ___.", "\"Store it in a special organ called the honey stomach.\"", "honey stomach"),
                Item(31, "An enzyme called ___ splits sucrose into glucose and fructose.", "\"The most important of which is invertase. This enzyme splits sucrose into two simpler sugars.\"", "invertase"),
                Item(32, "Bees fan their wings until the water content falls to about ___ per cent.", "\"When it reaches about 18 per cent, the honey is ready.\"", "18", "eighteen"),
                Item(33, "Finally, each cell is sealed with a thin cap of ___ to keep out moisture.", "\"The bees then seal each cell with a thin cap of wax.\"", "wax"),
            ]),
            new("mcq", R5Mcq, null,
            [
                Choice(34, "According to the text, why does honey keep for so long?", "B", "\"Honey draws moisture out of any bacteria or fungi that land in it, and they cannot survive.\"",
                    "Bees cover it with a layer of wax.", "Microbes lose water in it and cannot survive.", "It is stored at the temperature of the hive.", "Beekeepers heat it before selling it."),
                Choice(35, "What does the writer say about honey that has crystallised?", "D", "\"This is often mistaken for a sign that the honey has gone bad ... but it is a natural process caused by glucose forming crystals.\"",
                    "It shows that the honey has gone bad.", "It proves that sugar has been added.", "It only happens to honey made from cotton.", "It is the result of a natural process."),
            ]),
        ]);

    // =====================================================================
    // READING — TEST 5
    // =====================================================================

    private static ReadingPassage Reading5Part1() => new("Picnic in Lakeside Park",
        """
        Hi everyone,

        Now that exams are finally over, I think we all deserve a day off! I'd like to invite you to a picnic in Lakeside Park this Sunday, 8 June. The forecast says it will be sunny all day.

        The easiest way to get there is by (1) ______: the number 12 stops right outside the north entrance of the park, and the journey from the university only takes about twenty minutes. If you miss the bus, just give me a call.

        Let's meet at the north entrance at 11 a.m. If you're late, just follow the path towards the water, and you'll find us on the grass next to the old wooden (2) ______. After lunch, we could walk across the bridge to the little island, where there's a great ice-cream café.

        To keep things simple, let's all bring something to share. I'll make a big pot of my mum's plov, and Timur has promised to bring bread and fruit. Aziza, could you bring a (3) ______? Everyone loved your tomato and cucumber one last time. If anyone else wants to make a salad too, go ahead!

        We'll also need something to sit on. I've got one large blanket, but that won't be enough for eight people, so please bring a (4) ______ if you have one.

        There's a big field next to the lake which is perfect for football. Timur, please don't forget to bring a (5) ______ this time!

        My sister is coming too, and she has promised to take some photos of all of us by the water. I'd love to have lots of (6) ______ of this summer to look back on later.

        Please reply by Friday so that I know how much food to make.

        See you on Sunday!
        Kamila
        """,
        [
            new("gap", R1, 1,
            [
                Item(1, "The easiest way to get there is by ___: the number 12 stops right outside the north entrance of the park.", "\"If you miss the bus, just give me a call.\"", "bus"),
                Item(2, "You'll find us on the grass next to the old wooden ___.", "\"After lunch, we could walk across the bridge to the little island.\"", "bridge"),
                Item(3, "Aziza, could you bring a ___? Everyone loved your tomato and cucumber one last time.", "\"If anyone else wants to make a salad too, go ahead!\"", "salad"),
                Item(4, "I've got one large blanket, but that won't be enough for eight people, so please bring a ___ if you have one.", "\"I've got one large blanket, but that won't be enough.\"", "blanket"),
                Item(5, "Timur, please don't forget to bring a ___ this time!", "\"There's a big field next to the lake which is perfect for football.\"", "football"),
                Item(6, "I'd love to have lots of ___ of this summer to look back on later.", "\"She has promised to take some photos of all of us by the water.\"", "photos"),
            ]),
        ]);

    private static ReadingPassage Reading5Part2() => new("Eight museums worth a visit",
        """
        7. The Science Discovery Centre. Unlike a traditional museum, the Discovery Centre encourages visitors to touch everything. In our laboratory zone, children can carry out real experiments, from growing crystals to building simple electric circuits, with help from our staff. School groups must book in advance, but families and individual visitors can simply turn up.

        8. The Textile Museum. Housed in what was once the city's largest silk factory, this museum tells the story of silk and cotton in Central Asia. The old weaving hall, with its high windows, now displays hundreds of robes, carpets and embroidered wall hangings. Please note that the clothes on display are very fragile and must not be touched.

        9. The Railway Museum. Climb aboard steam engines and old passenger carriages at this open-air museum next to the main railway station. Tickets are inexpensive, and on the first Sunday of every month entry costs nothing at all, so the museum is especially busy on those days. There is a small café in an old dining carriage.

        10. The Fine Arts Museum. As well as its famous collection of paintings, the museum has a restoration studio with a glass wall, where visitors can watch specialists cleaning and repairing old paintings and frames. The studio can be viewed from Tuesday to Friday, from 10 a.m. to 4 p.m. The museum shop sells high-quality prints of the best-known works.

        11. The Clock Museum. This small museum contains more than three thousand clocks and watches, all collected by a retired engineer over a period of fifty years and left to the city after his death. Every hour, the rooms fill with the sound of bells and chimes, so it is worth planning your visit to arrive just before the hour.

        12. The Museum of Modern Art. Our galleries show the work of artists from across the region, with a new exhibition every three months. On Thursdays the museum stays open until 10 p.m., and there is often live music in the entrance hall. The rooftop terrace offers wonderful views of the old city.

        13. The Archaeology Museum. See gold jewellery, coins and pottery found at ancient sites along the Silk Road. To protect the most delicate objects from light, cameras and phones may not be used for photographs in any of the rooms. Guided tours are offered in Uzbek only, but written information is available in English and Russian.

        14. The Space Museum. Sit in a full-size model of a spacecraft and see real equipment used by cosmonauts on their missions. Because the museum is small, only thirty visitors are allowed inside at any one time, and all tickets must be bought online for a particular time slot before you arrive.
        """,
        [
            new("match", R2, null,
            [
                Item(7, "Text 7", "\"Children can carry out real experiments, from growing crystals to building simple electric circuits.\"", "E"),
                Item(8, "Text 8", "\"Housed in what was once the city's largest silk factory.\"", "D"),
                Item(9, "Text 9", "\"On the first Sunday of every month entry costs nothing at all.\"", "B"),
                Item(10, "Text 10", "\"Visitors can watch specialists cleaning and repairing old paintings and frames.\"", "I"),
                Item(11, "Text 11", "\"All collected by a retired engineer over a period of fifty years.\"", "H"),
                Item(12, "Text 12", "\"On Thursdays the museum stays open until 10 p.m.\"", "F"),
                Item(13, "Text 13", "\"Cameras and phones may not be used for photographs in any of the rooms.\"", "G"),
                Item(14, "Text 14", "\"All tickets must be bought online for a particular time slot before you arrive.\"", "C"),
            ],
            [
                "Visitors can try on clothes from the past.",
                "Entry is free on certain days.",
                "Visitors must book a time in advance.",
                "The building used to have a different purpose.",
                "Children can do experiments.",
                "The museum is open late on one day a week.",
                "Visitors are not allowed to take pictures.",
                "The collection was put together by one person.",
                "Visitors can watch experts at work.",
                "Guided tours are offered in several languages.",
            ]),
        ]);

    private static ReadingPassage Reading5Part3() => new("Twenty weeks to the finish line",
        """
        15. Three years ago, Sardor Nazarov could not run for a bus without losing his breath. An office worker in his early thirties, he spent most of his day sitting at a desk and most evenings sitting in front of a screen. Then, at a routine check-up, his doctor told him that his blood pressure was far too high for his age. "She said that if I didn't change something, I'd be taking pills for the rest of my life," he remembers. He started with short, slow jogs around the park, and within a year he had set himself an ambitious target: to run a full marathon.

        16. Sardor found a twenty-week training plan online and was surprised by what it asked of him. He was to run four times a week, but on three of those days he had to go so slowly that he could hold a conversation the whole time. "At first I thought it was a waste of time," he says. "I wanted to feel that I was working hard." But the author of the plan explained that most of a marathon runner's fitness is built at an easy pace, which strengthens the heart and teaches the body to use energy efficiently, while hard sessions should be limited to about one a week.

        17. The most important session of each week was the Sunday long run, which grew gradually from twelve kilometres to thirty-two. These runs were not only about distance. They were also a chance to practise eating and drinking on the move, something that many beginners forget. The first time he tried to take an energy gel while running, Sardor felt sick for half an hour. Over the following weeks, he tried different foods and drinks until he found a combination that his stomach could handle at race pace.

        18. In the eleventh week, disaster struck. Feeling strong, Sardor added extra kilometres to his plan, and within days he felt a sharp pain on the outside of his knee. A physiotherapist told him that he had increased his distance too quickly and ordered him to stop running for two weeks. Instead, he cycled and did exercises to strengthen his hips and legs. "It was the most frustrating fortnight of the whole plan," he admits, "but I learned that the plan was there for a reason."

        19. Three weeks before the race, the plan did something that seemed strange: it cut his running dramatically. This period, known as tapering, allows the body to recover fully and store energy for race day. Sardor found it harder than any long run. "I had so much nervous energy and nowhere to put it," he says. "I kept thinking I was losing fitness." A friend who coaches runners reassured him that three quiet weeks would not undo months of work, and that arriving at the start line tired would be far more dangerous.

        20. On race day, Sardor started too fast, carried along by the crowd and the music, and he paid for it in the last ten kilometres. Even so, he crossed the finish line in four hours and twelve minutes, with colleagues from work cheering beside the road. His blood pressure is now normal, and he has already entered his second marathon. "This time," he says, "I'll run the first half as if it were one of my slow Tuesday runs."
        """,
        [
            new("match", R3, null,
            [
                Item(15, "Paragraph 15", "\"His doctor told him that his blood pressure was far too high ... within a year he had set himself an ambitious target.\"", "D"),
                Item(16, "Paragraph 16", "\"Most of a marathon runner's fitness is built at an easy pace.\"", "E"),
                Item(17, "Paragraph 17", "\"A chance to practise eating and drinking on the move.\"", "A"),
                Item(18, "Paragraph 18", "\"He had increased his distance too quickly.\"", "H"),
                Item(19, "Paragraph 19", "\"The plan ... cut his running dramatically ... I kept thinking I was losing fitness.\"", "B"),
                Item(20, "Paragraph 20", "\"He crossed the finish line ... I'll run the first half as if it were one of my slow Tuesday runs.\"", "F"),
            ],
            [
                "Learning to eat and drink on the move",
                "Resting when every instinct says train harder",
                "Choosing the right running shoes",
                "A warning that led to a goal",
                "The surprising value of running slowly",
                "A first finish and a lesson for next time",
                "The support of a running club",
                "Too much, too soon",
            ]),
        ]);

    private static ReadingPassage Reading5Part4() => new("How we shop now",
        """
        Not long ago, buying something meant going to a shop, looking at what was on the shelves and carrying your purchase home. Today, for millions of people, it means a few taps on a phone, often late at night, followed by a delivery the next day. Online shopping has grown steadily for two decades, and it has changed not only where we buy things but how we decide what to buy.

        The most obvious attraction is convenience. Online shops never close, and they offer a far wider choice than any single store. Just as important, however, is the ability to compare. Before the internet, finding the lowest price for a washing machine might have meant visiting several shops across a city; now it takes minutes. Economists expected this transparency to push prices down, and in some markets it has. Yet shoppers do not always choose the cheapest option. Many are willing to pay a little more to a seller they trust, or for faster delivery.

        Trust, in fact, has become central to online shopping, and customer reviews are one of its main foundations. Surveys regularly find that a large majority of online shoppers read reviews before buying, and many say they trust them almost as much as recommendations from friends. This has created a problem: fake reviews. Some sellers pay for positive comments about their own products, or for negative ones about their competitors. Large platforms now use software to detect suspicious patterns, but experienced shoppers have also learned to be careful, paying more attention to detailed, balanced reviews than to a long row of five-star ratings.

        Online shops are also designed to encourage spending in ways that shoppers may not notice. "One-click" buying removes the moment of hesitation that used to come at the till. Messages such as "only three left in stock" create a sense of urgency. Free delivery is often offered only above a certain amount, and many customers add items they had not planned to buy simply to reach that limit. In one sense, the "free" delivery is paid for by the extra purchases.

        Returns are another area where habits have changed. When returning goods is free and easy, customers behave differently. Some clothes shoppers now order the same item in two or three sizes, keep the one that fits and send the others back, a practice that retailers call "bracketing". For shoppers, this is a sensible response to the difficulty of buying clothes without trying them on. For retailers, it is expensive, as returned items must be transported, checked and repackaged, and some cannot be sold again. The environmental cost of all that extra transport is also considerable. As a result, a number of companies have started to charge for returns or to offer more detailed size guides.

        Physical shops, meanwhile, have not disappeared, but their role is changing. Some shoppers examine products in a store and then buy them more cheaply online, a habit known as "showrooming". Others do the opposite: they research online and then go to a shop to see the item before buying it. Many retailers have responded by combining the two, allowing customers to order online and collect in store, or turning their shops into places where people can try products, get advice and attend events, rather than simply places to pay.

        It is tempting to see online shopping as simply a faster version of the old kind, but the change runs deeper. The way we search, compare and decide has been reshaped by the tools we use. Understanding how those tools influence us may be the best way for shoppers to stay in control of their own choices.
        """,
        [
            new("mcq", R4Mcq, null,
            [
                Choice(21, "According to the second paragraph, what did economists expect online shopping to do?", "B", "\"Economists expected this transparency to push prices down.\"",
                    "make shops open for longer hours", "lower prices because comparing them became easy", "make shoppers trust sellers more", "encourage faster delivery"),
                Choice(22, "What does the writer say about experienced online shoppers?", "C", "\"Experienced shoppers ... paying more attention to detailed, balanced reviews than to a long row of five-star ratings.\"",
                    "They no longer read customer reviews.", "They use software to find fake reviews.", "They prefer detailed reviews to simple high ratings.", "They only trust recommendations from friends."),
                Choice(23, "What point does the writer make about free delivery?", "A", "\"Many customers add items they had not planned to buy simply to reach that limit.\"",
                    "It often leads customers to spend more than they intended.", "It is offered by all online shops.", "It is the main reason people shop online.", "It makes deliveries slower."),
                Choice(24, "Why have some companies started to charge for returns?", "D", "\"For retailers, it is expensive, as returned items must be transported, checked and repackaged.\"",
                    "Customers asked them to improve their size guides.", "Most returned clothes are damaged.", "Shoppers have stopped ordering several sizes.", "Dealing with returned goods costs them a lot."),
            ]),
            new("tfng", R4Tfng, null,
            [
                Item(25, "Online shoppers always choose the cheapest option.", "\"Yet shoppers do not always choose the cheapest option.\"", "FALSE"),
                Item(26, "Many shoppers trust online reviews almost as much as recommendations from friends.", "\"Many say they trust them almost as much as recommendations from friends.\"", "TRUE"),
                Item(27, "Most fake reviews are written by sellers' competitors.", "The text mentions fake reviews for and against products but gives no figures about who writes most of them.", "NOT GIVEN"),
                Item(28, "Some items that are returned cannot be sold again.", "\"Some cannot be sold again.\"", "TRUE"),
                Item(29, "\"Showrooming\" means researching products online and then buying them in a shop.", "Showrooming is examining products in a store and buying \"more cheaply online\"; the text says others \"do the opposite\".", "FALSE"),
            ]),
        ]);

    private static ReadingPassage Reading5Part5() => new("Why is the sky blue?",
        """
        It is one of the first scientific questions many children ask, and one of the hardest for adults to answer: why is the sky blue? For centuries, the colour was explained in various ways, some of them quite imaginative. The modern answer began to take shape in the nineteenth century, when the Irish physicist John Tyndall passed a beam of white light through a glass tube containing a fine mist of tiny particles. Seen from the side, the beam looked blue, while the light that passed straight through the tube took on a reddish-orange colour. Tyndall suggested that the blue of the sky was produced in a similar way.

        Shortly afterwards, in 1871, the English physicist Lord Rayleigh provided a mathematical explanation. Sunlight, which looks white, is a mixture of all the colours of the rainbow, each with a different wavelength. Red light has the longest wavelength of the visible colours, around 700 nanometres, while blue light has a much shorter one, around 450 nanometres. When light meets particles that are much smaller than its wavelength, some of it is scattered, or sent off in new directions. Rayleigh showed that the amount of scattering depends very strongly on the wavelength: shorter wavelengths are scattered far more than longer ones. It was later shown that the molecules of nitrogen and oxygen that make up most of the air are enough to produce this effect, even without any dust or water.

        The difference is dramatic. Because scattering increases so steeply as the wavelength decreases, blue light is scattered roughly five to six times more strongly than red light. As sunlight passes through the atmosphere, blue light is bounced around the sky in every direction, so whichever way we look, some of this scattered blue light reaches our eyes. The sky appears blue not because the air itself is coloured, but because it redirects blue light towards us from all parts of the sky.

        This raises an obvious question. Violet light has an even shorter wavelength than blue and should be scattered even more, so why is the sky not violet? There are several reasons. Sunlight contains less violet than blue to begin with, and some violet light is absorbed high in the atmosphere. Most importantly, our eyes are much less sensitive to violet than to blue. The mixture of scattered colours that reaches us is therefore interpreted by the brain as the pale blue we know.

        The same process explains the colours of sunrise and sunset. When the sun is low in the sky, its light must travel through a much greater thickness of atmosphere before reaching us. By the time it arrives, most of the blue light has been scattered away in other directions, leaving the longer wavelengths, orange and red, to colour the sky and the clouds. After large volcanic eruptions, extra particles in the upper atmosphere can make sunsets especially vivid around the world; after the eruption of Krakatoa in 1883, brilliant red sunsets were reported in many countries for months.

        Clouds, by contrast, are white or grey. This is because the water droplets in clouds are much larger than the wavelength of light. Particles of this size scatter all colours by roughly the same amount, so the light they send towards us remains white. Thick clouds look grey simply because less light gets through them.

        Other worlds confirm the explanation. On the Moon, which has almost no atmosphere, the sky is black even in the daytime, because there is nothing to scatter the sunlight. On Mars, where the thin air carries large amounts of fine reddish dust, photographs taken by robotic explorers show a daytime sky of a pale butterscotch colour, while sunsets there appear bluish. A simple question about the colour of the sky, it turns out, leads to some of the most fundamental ideas in physics.
        """,
        [
            new("gap", R5Gap, 2,
            [
                Item(30, "Tyndall found that white light passing through a fine ___ of tiny particles looked blue from the side.", "\"A glass tube containing a fine mist of tiny particles. Seen from the side, the beam looked blue.\"", "mist"),
                Item(31, "Rayleigh showed that the amount of scattering depends very strongly on the light's ___.", "\"The amount of scattering depends very strongly on the wavelength.\"", "wavelength"),
                Item(32, "Blue light is scattered roughly five to six times more strongly than ___ light.", "\"Blue light is scattered roughly five to six times more strongly than red light.\"", "red"),
                Item(33, "The sky does not look violet, mainly because our ___ are much less sensitive to violet than to blue.", "\"Most importantly, our eyes are much less sensitive to violet than to blue.\"", "eyes"),
            ]),
            new("mcq", R5Mcq, null,
            [
                Choice(34, "According to the text, why are sunsets orange and red?", "B", "\"Most of the blue light has been scattered away in other directions, leaving the longer wavelengths, orange and red.\"",
                    "The air contains more dust in the evening.", "Blue light is scattered away on the long path through the atmosphere.", "The sun produces more red light when it is low.", "Clouds absorb blue light at the end of the day."),
                Choice(35, "Why do clouds look white?", "C", "\"The water droplets in clouds are much larger than the wavelength of light ... scatter all colours by roughly the same amount.\"",
                    "They reflect the colour of the sun directly.", "They contain very small particles of dust.", "Their droplets scatter all colours roughly equally.", "They stop blue light from reaching the ground."),
            ]),
        ]);

    // =====================================================================
    // READING — TEST 6
    // =====================================================================

    private static ReadingPassage Reading6Part1() => new("City Library: rules for readers",
        """
        CITY LIBRARY: RULES FOR READERS

        Welcome to the City Library. To make sure that everyone can enjoy a calm and pleasant place to study, please read the following rules carefully.

        Bags and coats. Large bags, coats and umbrellas must be left in the cloakroom on the ground floor. Only small (1) ______, such as handbags and laptop cases, may be taken into the reading halls.

        Library cards. You need your library card to enter the building and to borrow books. If you lose your (2) ______, please tell a member of staff immediately. A replacement costs 10,000 som.

        Phones and noise. Phones must be switched to silent in all reading halls, and calls may only be made in the corridor or in the café on the ground floor. Reading Hall 2 is a (3) ______ hall: no talking of any kind is allowed there. If you want to work with other people, please book one of the group study rooms.

        Food and drink. Bottles of water are allowed in the reading halls, but all other drinks and all food must be consumed in the (4) ______.

        Borrowing. Readers may borrow up to five books at a time for two weeks. Reference books, newspapers and magazines cannot be borrowed and must stay in the library. If you need to keep a (5) ______ for longer than two weeks, you can renew it online, as long as no other reader has reserved it.

        Returning books. Please do not put books back on the shelves yourself. Leave them on the trolleys at the end of each row, and our (6) ______ will return them to the correct place. A fine of 1,000 som per day is charged for every item that is returned late.
        """,
        [
            new("gap", R1, 1,
            [
                Item(1, "Only small ___, such as handbags and laptop cases, may be taken into the reading halls.", "\"Large bags, coats and umbrellas must be left in the cloakroom.\"", "bags"),
                Item(2, "If you lose your ___, please tell a member of staff immediately.", "\"You need your library card to enter the building and to borrow books.\"", "card"),
                Item(3, "Reading Hall 2 is a ___ hall: no talking of any kind is allowed there.", "\"Phones must be switched to silent in all reading halls.\"", "silent"),
                Item(4, "All other drinks and all food must be consumed in the ___.", "\"Calls may only be made in the corridor or in the café on the ground floor.\"", "café", "cafe"),
                Item(5, "If you need to keep a ___ for longer than two weeks, you can renew it online.", "\"Readers may borrow up to five books at a time for two weeks.\" (\"item\" is also accepted: \"every item that is returned late\".)", "book", "item"),
                Item(6, "Leave them on the trolleys at the end of each row, and our ___ will return them to the correct place.", "\"Please tell a member of staff immediately.\"", "staff"),
            ]),
        ]);

    private static ReadingPassage Reading6Part2() => new("Find your club: student societies",
        """
        7. Debating Society. Do you enjoy a good argument? Every Tuesday evening, members prepare and deliver speeches on topics ranging from technology to education, and then the audience votes for the winning side. Our team has won the national student debating championship twice in the last five years, and we are always looking for new talent to continue that tradition.

        8. Helping Hands. Our volunteers spend Saturday mornings in nearby neighbourhoods, helping elderly residents with shopping, small repairs and learning to use their smartphones. We also collect books for village schools. Membership is free, and you can come as often or as rarely as your timetable allows.

        9. Hiking Club. Once a month we leave the city behind for a weekend in the mountains, travelling to places such as the Nuratau range and the Zaamin national park. Transport costs and tents are shared among members. Good walking boots are essential, but beginners are very welcome on our easier routes.

        10. The Campus Voice. The university newspaper is written, edited and designed entirely by students. A new issue appears online every two weeks, with a printed edition at the end of each semester. Whether you want to write, take photos or design pages, there is a place for you on the team. We meet in Room 214 on Monday afternoons.

        11. University Choir. The choir sings at graduation ceremonies and gives two concerts a year in the main hall. Singers of all voice types are welcome, but because places are limited, everyone who wants to join must sing a short piece in front of the choir's student leaders at the start of the year. Rehearsals are on Wednesday evenings.

        12. Photography Society. Whether you use an expensive camera or just your phone, you can improve your pictures with us. Once a month, a professional photographer runs a workshop on a subject such as portraits, landscapes or editing, and members can ask for advice on their own work. We hold an exhibition of members' photos every spring.

        13. Rowing Club. The club trains on the canal three times a week from 6 to 7.30 a.m., so that members can finish in time for their first lectures. Complete beginners are welcome, as experienced members will teach you everything from the start. Training in the early morning is hard at first, but most members say it gives them energy for the whole day.

        14. Chess Club. From beginners to experienced tournament players, all chess lovers are welcome. We meet on Thursday and Friday afternoons in the student centre. The membership fee of 50,000 som per semester covers entry to tournaments organised with other universities in the city. We have not won a national title yet, but we are getting closer every year!
        """,
        [
            new("match", R2, null,
            [
                Item(7, "Text 7", "\"Our team has won the national student debating championship twice in the last five years.\"", "E"),
                Item(8, "Text 8", "\"Helping elderly residents with shopping, small repairs and learning to use their smartphones.\"", "C"),
                Item(9, "Text 9", "\"Travelling to places such as the Nuratau range and the Zaamin national park.\"", "G"),
                Item(10, "Text 10", "\"A new issue appears online every two weeks, with a printed edition at the end of each semester.\"", "A"),
                Item(11, "Text 11", "\"Everyone who wants to join must sing a short piece in front of the choir's student leaders.\"", "D"),
                Item(12, "Text 12", "\"Once a month, a professional photographer runs a workshop.\"", "J"),
                Item(13, "Text 13", "\"From 6 to 7.30 a.m., so that members can finish in time for their first lectures.\"", "H"),
                Item(14, "Text 14", "\"The membership fee of 50,000 som per semester.\"", "B"),
            ],
            [
                "The club publishes something regularly.",
                "Members have to pay to join.",
                "Members help people outside the university.",
                "New members have to pass an audition.",
                "The club has won national competitions.",
                "Only people with previous experience can join.",
                "Members travel to other parts of the country.",
                "Meetings take place before classes begin.",
                "The club only meets online.",
                "Members can learn from professionals.",
            ]),
        ]);

    private static ReadingPassage Reading6Part3() => new("The Silk Road today",
        """
        15. A thousand years ago, a merchant travelling from Tashkent to Samarkand with a caravan of camels might have spent a week or more on the road. I made the same journey on a high-speed train in a little over two hours, sitting in a comfortable seat with a cup of tea, watching cotton fields and distant mountains slide past the window. It was a fitting start to a journey along one of history's greatest trade routes: the distances are the same as they always were, but the time it takes to cross them has shrunk beyond recognition.

        16. On the edge of Samarkand, in a village beside a small river, I visited a workshop where paper is made by hand from the bark of mulberry trees. Samarkand paper was once famous across much of Asia, but the craft died out centuries ago. In the 1990s, a group of local craftsmen set out to bring it back, experimenting for years with old descriptions until they could produce sheets as smooth as those used in medieval manuscripts. Today the water-powered mill turns again, and visitors can watch every stage of the process.

        17. In Bukhara, I spent a morning wandering through the trading domes, sixteenth-century buildings whose names still recall the jewellers and money-changers who once worked beneath them. The goods on sale have changed: instead of spices and silk bound for distant markets, the stalls now offer embroidered cushions, knives and ceramics to tourists. Yet the spirit of the place felt surprisingly familiar. "My family has been selling here for four generations," one carpet seller told me. "The customers come from different countries now, but we are still traders."

        18. Wherever I went, the food told its own story of exchange. Plov, the rice dish cooked in great iron pots, was served at almost every meal, but so was lagman, a dish of hand-pulled noodles thought to have arrived from further east. Bread was baked in clay ovens just as it has been for centuries, and in the bazaars, dried fruit and nuts were piled high, much as they would have been for merchants preparing for long journeys across the desert.

        19. My journey ended in Khiva, whose old inner town, Itchan Kala, is surrounded by massive clay walls. With its minarets, palaces and madrasahs, it can feel like an open-air museum, and during the day it is crowded with tour groups. But Itchan Kala is also a place where people live. In the evening, after the tourists have left, children play football in the narrow lanes and neighbours sit talking outside their doors, reminding visitors that this is a living town, not just a collection of monuments.

        20. The Silk Road was never really a single road, and it was never only about silk. Along with goods, it carried languages, technologies and ideas between East and West. Today, long freight trains carry containers across Central Asia, and tourists, students and business people travel the old routes in greater numbers than ever before. Standing on the walls of Khiva at sunset, I found it easy to believe that the road is still doing what it has always done: bringing people together.
        """,
        [
            new("match", R3, null,
            [
                Item(15, "Paragraph 15", "\"The distances are the same as they always were, but the time it takes to cross them has shrunk beyond recognition.\"", "D"),
                Item(16, "Paragraph 16", "\"The craft died out centuries ago. In the 1990s, a group of local craftsmen set out to bring it back.\"", "G"),
                Item(17, "Paragraph 17", "\"The customers come from different countries now, but we are still traders.\"", "H"),
                Item(18, "Paragraph 18", "\"The food told its own story of exchange.\"", "A"),
                Item(19, "Paragraph 19", "\"It can feel like an open-air museum ... But Itchan Kala is also a place where people live.\"", "C"),
                Item(20, "Paragraph 20", "\"The road is still doing what it has always done: bringing people together.\"", "E"),
            ],
            [
                "A menu shaped by travel",
                "Crossing the desert by camel",
                "More than a museum",
                "Old distances, new speed",
                "A road that still connects people",
                "Finding a hotel in the old city",
                "Bringing a lost craft back to life",
                "Traders then and now",
            ]),
        ]);

    private static ReadingPassage Reading6Part4() => new("The four-day week",
        """
        For most of the twentieth century, the five-day, forty-hour working week was treated as normal in much of the world. It was itself the result of a long struggle: in the nineteenth century, factory workers often worked six days a week, for ten hours or more a day. Now a growing number of companies are asking whether the time has come for the next step: a four-day week, with no reduction in pay.

        The most widely discussed version of the idea is sometimes summarised as "100-80-100": employees receive 100 per cent of their pay for working 80 per cent of their previous hours, in return for a commitment to maintain 100 per cent of their productivity. This is different from a "compressed" week, in which people work the same total number of hours over four longer days. Supporters of the 100-80-100 model argue that much of the ordinary working week is wasted on unnecessary meetings, interruptions and tasks that add little value, and that people who know they have less time will use it more carefully.

        Several large trials have tested this claim. In Iceland, trials involving around 2,500 public-sector workers between 2015 and 2019 reduced weekly hours without cutting pay, and researchers reported that productivity stayed the same or improved in most workplaces. In the United Kingdom, a six-month trial in 2022 involved around sixty companies and nearly three thousand employees. At the end of the trial, the great majority of the companies chose to continue with the shorter week, and staff reported lower levels of stress and exhaustion, better sleep and fewer days off sick.

        Employers also point to benefits that are harder to measure. A company that offers a four-day week may find it easier to attract skilled staff, particularly in industries where good employees are scarce. Some managers report that employees who have a three-day weekend return to work more focused and more creative. There may be wider benefits, too: fewer days of commuting mean less traffic, and parents may have more time to spend with their children.

        Critics, however, urge caution. The companies that took part in the trials volunteered to do so, and they were likely to be the kind of organisations where a shorter week had a good chance of success. Results from a group of enthusiastic volunteers, sceptics argue, may not apply to the economy as a whole. Moreover, many jobs cannot simply be squeezed into fewer days. A hospital, a bus service or a shop must remain open whatever the working pattern of its staff, so reducing hours often means employing more people, which increases costs.

        There are also concerns about the quality of working life within the shorter week. Some employees in the trials reported that, in order to fit everything into four days, breaks and informal conversations with colleagues had disappeared, making work more intense. Others found that their "free" day was gradually taken over by emails and phone calls. Where the four-day week has been introduced most successfully, managers have usually taken a hard look at how work is organised, cutting meetings, setting clear priorities and agreeing rules about contact on the day off.

        The four-day week, then, is neither a magic solution nor a passing fashion. For some organisations it seems to bring real benefits for both employers and employees; for others it may be impractical or too expensive. What the debate has already achieved is to make many companies question an assumption that went unchallenged for decades: that the number of hours people spend at work is a reliable measure of how much they achieve.
        """,
        [
            new("mcq", R4Mcq, null,
            [
                Choice(21, "What does the writer say about the five-day, forty-hour week?", "B", "\"It was itself the result of a long struggle: in the nineteenth century, factory workers often worked six days a week.\"",
                    "It was first introduced in the twenty-first century.", "It replaced even longer working hours in the past.", "It was never common outside factories.", "It is now being replaced by longer working days."),
                Choice(22, "How is a \"compressed\" week different from the 100-80-100 model?", "D", "\"A 'compressed' week, in which people work the same total number of hours over four longer days.\"",
                    "Employees receive less pay.", "Employees must promise to be more productive.", "It has more meetings and interruptions.", "The total number of working hours does not change."),
                Choice(23, "What happened at the end of the UK trial?", "A", "\"The great majority of the companies chose to continue with the shorter week.\"",
                    "Most of the companies decided to keep the four-day week.", "Most companies returned to a five-day week.", "The number of days off sick increased.", "The trial was extended to more companies."),
                Choice(24, "What is the critics' main point about the trials?", "C", "\"The companies that took part in the trials volunteered ... Results from a group of enthusiastic volunteers ... may not apply to the economy as a whole.\"",
                    "They lasted too short a time to be useful.", "They included too few employees.", "Their results may not be true for all kinds of organisation.", "They ignored the views of employees."),
            ]),
            new("tfng", R4Tfng, null,
            [
                Item(25, "Workers in the Icelandic trials had to accept lower pay.", "The trials \"reduced weekly hours without cutting pay\".", "FALSE"),
                Item(26, "Employees in the UK trial said they slept better.", "\"Staff reported lower levels of stress and exhaustion, better sleep and fewer days off sick.\"", "TRUE"),
                Item(27, "Most employees in the trials used their extra day off to look after their children.", "The writer only says parents \"may have more time\"; nothing is reported about how most employees used the day.", "NOT GIVEN"),
                Item(28, "According to critics, it is easy to introduce a four-day week in hospitals and shops.", "\"Many jobs cannot simply be squeezed into fewer days. A hospital, a bus service or a shop must remain open.\"", "FALSE"),
                Item(29, "Some employees found that work messages began to fill their day off.", "\"Others found that their 'free' day was gradually taken over by emails and phone calls.\"", "TRUE"),
            ]),
        ]);

    private static ReadingPassage Reading6Part5() => new("From cowpox to vaccines: a short history",
        """
        For most of human history, smallpox was one of the most feared diseases in the world. It spread easily from person to person, killed roughly three in every ten people it infected, and left many survivors with deep scars or blindness. Yet it had one feature that had been noticed for centuries: people who survived it never caught it again. This observation lay behind the earliest attempts to protect people from the disease.

        Long before vaccines existed, people in parts of Asia and Africa practised a technique known as variolation. Material taken from the sores of a person with a mild case of smallpox was deliberately introduced into a healthy person, either through a small cut in the skin or, in China, by blowing dried, powdered scabs into the nose. The result was usually a milder illness than natural smallpox, followed by lifelong protection. The method was not without risk: perhaps one or two people in a hundred died after variolation, and those who had been treated could still pass the disease to others. Compared with the danger of catching smallpox naturally, however, the risk was often considered worth taking. In the early eighteenth century, the practice was introduced to Britain by Lady Mary Wortley Montagu, who had seen it used in the Ottoman Empire.

        The next major step came from an English country doctor, Edward Jenner. Jenner knew of a common belief among farmers that milkmaids who had caught cowpox, a mild disease of cattle that could spread to humans, did not catch smallpox. In 1796 he decided to test the idea. He took material from a cowpox sore on the hand of a milkmaid named Sarah Nelmes and introduced it into small cuts on the arm of James Phipps, the eight-year-old son of his gardener. Some weeks later, Jenner exposed the boy to smallpox material, an experiment that would never be permitted today. The boy did not become ill. Jenner repeated the procedure with other patients and published his results in 1798.

        Jenner's method was far safer than variolation, because cowpox was a much milder disease and a person treated with it could not pass smallpox on to others. The practice spread rapidly across Europe and beyond. The word "vaccine" itself comes from vacca, the Latin word for cow, and "vaccination" originally referred only to Jenner's technique. Several decades later, the French scientist Louis Pasteur proposed that the terms should be extended to all such preparations, in honour of Jenner.

        Pasteur's own work transformed the field. Jenner had been fortunate: nature had provided a mild relative of smallpox. For most diseases, no such relative existed. Around 1879, while studying chicken cholera, Pasteur's team made an accidental discovery. A culture of the bacteria had been left standing for several weeks, and when it was given to chickens, they became only mildly ill. Later, when the same chickens were exposed to fresh, dangerous bacteria, they survived. Pasteur realised that germs could be deliberately weakened in the laboratory and then used to create protection. Using this principle, he went on to develop vaccines against anthrax in animals and, in 1885, against rabies, which he first used on a nine-year-old boy, Joseph Meister, who had been bitten by an infected dog.

        In the twentieth century, vaccines were developed against many more diseases, including diphtheria, tetanus, measles and polio. The first widely used polio vaccine, developed by Jonas Salk, was tested in 1954 in one of the largest medical trials ever carried out, involving more than a million children. Meanwhile, the original target of vaccination, smallpox, remained. In 1967 the World Health Organization launched an intensified programme to eradicate it. A key technical advance was a freeze-dried vaccine that remained effective for long periods without refrigeration, which made it possible to use in hot, remote regions. Combined with a strategy of quickly finding new cases and vaccinating the people around them, this approach succeeded. The last natural case occurred in 1977, and in 1980 smallpox was officially declared eradicated, the first human disease to be eliminated worldwide.
        """,
        [
            new("gap", R5Gap, 2,
            [
                Item(30, "Before vaccines, a technique called ___ involved deliberately giving healthy people material from mild smallpox cases.", "\"People in parts of Asia and Africa practised a technique known as variolation.\"", "variolation"),
                Item(31, "Jenner tested a belief that ___ who had caught cowpox did not catch smallpox.", "\"A common belief among farmers that milkmaids who had caught cowpox ... did not catch smallpox.\"", "milkmaids"),
                Item(32, "The word \"vaccine\" comes from the Latin word for ___.", "\"The word 'vaccine' itself comes from vacca, the Latin word for cow.\"", "cow"),
                Item(33, "Pasteur's team found that an old ___ of chicken cholera bacteria made chickens only mildly ill.", "\"A culture of the bacteria had been left standing for several weeks ... they became only mildly ill.\"", "culture"),
            ]),
            new("mcq", R5Mcq, null,
            [
                Choice(34, "Why was Pasteur's discovery so important?", "A", "\"For most diseases, no such relative existed ... germs could be deliberately weakened in the laboratory and then used to create protection.\"",
                    "It meant vaccines could be made for diseases that had no mild natural relative.", "It proved that Jenner's method was dangerous.", "It showed that chickens could not catch cholera.", "It made vaccines unnecessary for animals."),
                Choice(35, "According to the text, what helped to make the eradication of smallpox possible?", "D", "\"A freeze-dried vaccine that remained effective for long periods without refrigeration ... made it possible to use in hot, remote regions.\"",
                    "Vaccinating every person in the world at the same time.", "A return to the older method of variolation.", "A new vaccine that could be taken by mouth.", "A vaccine that did not need to be kept cold."),
            ]),
        ]);
}
