using SpeakingCoach.Api.Services.Mock;
using static SpeakingCoach.Api.Services.Mock.CefrObjective;

namespace SpeakingCoach.Api.Services.Content;

// Tayyor CEFR (Multilevel) Reading testlari 7–11. Original matnlar; tuzilishi
// Reading1 bilan bir xil: 5 qism (6 + 8 + 6 + 9 + 6) = 35 savol.
// Matn va savollar mualliflik ishi — haqiqiy imtihon materiallaridan ko'chirilmagan.
public static partial class BuiltInCefr
{
    public static readonly ReadingTest Reading7 = new("", [Reading7Part1(), Reading7Part2(), Reading7Part3(), Reading7Part4(), Reading7Part5()]);
    public static readonly ReadingTest Reading8 = new("", [Reading8Part1(), Reading8Part2(), Reading8Part3(), Reading8Part4(), Reading8Part5()]);
    public static readonly ReadingTest Reading9 = new("", [Reading9Part1(), Reading9Part2(), Reading9Part3(), Reading9Part4(), Reading9Part5()]);
    public static readonly ReadingTest Reading10 = new("", [Reading10Part1(), Reading10Part2(), Reading10Part3(), Reading10Part4(), Reading10Part5()]);
    public static readonly ReadingTest Reading11 = new("", [Reading11Part1(), Reading11Part2(), Reading11Part3(), Reading11Part4(), Reading11Part5()]);

    // =====================================================================
    // READING 7
    // =====================================================================

    private static ReadingPassage Reading7Part1() => new("Spring Charity Run for the children's hospital",
        """
        Dear runners,

        Thank you for registering for the Spring Charity Run on Sunday 14 April. All the money we raise this year will be used to buy new equipment for the children's hospital in our city. More than two hundred people have already signed up, and together you have raised over forty million som. Maps of each route are attached to this email.

        The run starts and finishes in front of the city stadium. The short route is five kilometres long and stays inside Central Park, while the long (1) ______ follows the river and passes through the old town.

        Please collect your running number from the stadium ticket office on Friday or Saturday between 10 a.m. and 6 p.m. You will need to show this email and an identity card when you come to the ticket (2) ______.

        On the day of the run, the changing rooms under the main stand will be open from 7 a.m. You can leave your bags there, but please do not bring anything valuable. There are also toilets and showers in the changing (3) ______.

        There will be water stations every two and a half kilometres. If you need help, look for our volunteers in yellow jackets; every (4) ______ carries a phone and a small first-aid kit. We still need a few more helpers, so if a friend of yours would like to join us as a volunteer, please ask them to contact me.

        Every runner who finishes will receive a medal. There will also be (5) ______ for the three fastest men and women on the long route, including sports watches and running shoes. The prizes will be presented at 1 p.m. on the stage next to the stadium.

        Remember that this is not a race for professionals: walking is perfectly acceptable. If you can no longer take part, please reply to this (6) ______ by Wednesday so that we can give your number to someone on the waiting list.

        Good luck with your training!
        Nigora Salimova
        Charity Run organiser
        """,
        [
            new("gap", R1, 1,
            [
                Item(1, "The short route is five kilometres long and stays inside Central Park, while the long ___ follows the river and passes through the old town.", "\"The short route ... while the long route\" — \"Maps of each route are attached.\"", "route"),
                Item(2, "You will need to show this email and an identity card when you come to the ticket ___.", "\"Collect your running number from the stadium ticket office.\"", "office"),
                Item(3, "There are also toilets and showers in the changing ___.", "\"The changing rooms under the main stand will be open from 7 a.m.\"", "rooms"),
                Item(4, "If you need help, look for our volunteers in yellow jackets; every ___ carries a phone and a small first-aid kit.", "\"Look for our volunteers in yellow jackets\" — \"join us as a volunteer\".", "volunteer"),
                Item(5, "There will also be ___ for the three fastest men and women on the long route, including sports watches and running shoes.", "\"The prizes will be presented at 1 p.m.\"", "prizes"),
                Item(6, "If you can no longer take part, please reply to this ___ by Wednesday so that we can give your number to someone on the waiting list.", "\"Maps of each route are attached to this email.\"", "email"),
            ]),
        ]);

    private static ReadingPassage Reading7Part2() => new("Eight apps worth downloading",
        """
        7. PlantPal. Ever wondered what that flower by the road is called? Just point your camera at any plant and PlantPal will tell you its name, whether it is safe to touch and how to look after it. Because the whole database is stored on your phone, the app works perfectly on hiking trips high in the mountains, far from the nearest mobile signal. It is free to download and contains no adverts.

        8. WordWise. This vocabulary app teaches ten new English words a day through short games, pictures and example sentences. The basic version, with around 2,000 common words, costs nothing, but if you want the business and academic word lists, as well as pronunciation practice, you will need to buy a subscription for about 30,000 som a month. The app is suitable for learners aged twelve and over.

        9. StepUp. StepUp counts your steps, measures how far you walk and reminds you to get up if you have been sitting for too long. What makes it different is the weekly league: you can invite friends, classmates or colleagues, and the app shows who has walked the most. Many users say that the wish to beat their friends is what finally got them off the sofa.

        10. BudgetBox. BudgetBox was created by Aziz Karimov, a university student who was tired of having no money left by the middle of every month. He built the app to solve his own problem, and it now has thousands of users. You enter what you spend each day, and BudgetBox shows you where your money goes and how much you can safely spend before your next payment arrives.

        11. QuietMind. QuietMind offers short breathing exercises and calm stories read by gentle voices. The makers say it works best if you use it in bed, after you have switched off the lights: the screen turns dark automatically, and the sound slowly fades away after twenty minutes so that it does not wake you up later. All the content is free, and there are no adverts to interrupt you.

        12. CityMove. Stop waiting at the bus stop without knowing when your bus will come. CityMove uses GPS signals from buses and metro trains to show exactly where they are right now, and the arrival times on your screen change every few seconds as vehicles move through traffic. You can also plan journeys across the city and save your favourite routes. It is available for both Android phones and iPhones.

        13. RecipeRoom. Open your fridge, type in what you can see, such as two eggs, half an onion and some old bread, and RecipeRoom will suggest dishes you can make with exactly those ingredients. The idea is to use up food before it goes bad instead of throwing it into the bin. The app contains more than 5,000 recipes, including many traditional dishes from Central Asia.

        14. LinguaCall. Textbooks can only teach you so much. LinguaCall connects you through video calls with native speakers in more than eighty countries who want to learn your language. You spend half of each call speaking their language and half speaking yours, so both partners benefit. Calls last thirty minutes, and you can rate your partner afterwards. The app is not suitable for children under sixteen.
        """,
        [
            new("match", R2, null,
            [
                Item(7, "Text 7", "\"The app works perfectly ... far from the nearest mobile signal.\"", "C"),
                Item(8, "Text 8", "\"If you want the business and academic word lists ... you will need to buy a subscription.\"", "G"),
                Item(9, "Text 9", "\"You can invite friends, classmates or colleagues, and the app shows who has walked the most.\"", "J"),
                Item(10, "Text 10", "\"He built the app to solve his own problem.\"", "A"),
                Item(11, "Text 11", "\"It works best if you use it in bed, after you have switched off the lights.\"", "F"),
                Item(12, "Text 12", "\"The arrival times on your screen change every few seconds.\"", "E"),
                Item(13, "Text 13", "\"The idea is to use up food before it goes bad instead of throwing it into the bin.\"", "B"),
                Item(14, "Text 14", "\"You spend half of each call speaking their language and half speaking yours, so both partners benefit.\"", "H"),
            ],
            [
                "Its creator originally made it for himself.",
                "It helps users to waste less food.",
                "It can be used where there is no mobile phone signal.",
                "It is aimed at young children.",
                "The information it shows is updated all the time.",
                "It is designed to be used just before going to sleep.",
                "Users must pay to get all of its content.",
                "Users help each other to learn.",
                "It only works on one type of phone.",
                "It lets users compete with people they know.",
            ]),
        ]);

    private static ReadingPassage Reading7Part3() => new("The apricot village",
        """
        15. Twenty years ago, few people outside the region had heard of Sariqsoy, a village of about three thousand people in the foothills of the mountains. Its narrow streets were quiet, and many young people had left to look for work in the cities. Today, cars fill the village every June, shops in the capital sell boxes of dried fruit with the village's name printed on them, and its apricots are sold in more than a dozen countries. How did such a small place become so well known?

        16. The story begins with the land itself. Sariqsoy lies around 1,200 metres above sea level, where winters are cold enough to give the trees the rest they need, and summers are long, hot and dry. Water from melting snow reaches the orchards through channels that were dug by hand centuries ago. Apricots grown in these conditions are small but unusually sweet, and many of the trees in the village are more than fifty years old.

        17. For a long time, however, good fruit did not mean a good income. Each family sold its harvest separately to traders who arrived in lorries, and because fresh apricots spoil within days, the farmers had no choice but to accept whatever price was offered. Everything changed when a retired schoolteacher persuaded forty families to form a cooperative. By selling together, and by drying much of the fruit instead of selling it fresh, they could finally wait for a better price.

        18. The drying itself is done much as it was by their grandparents. The apricots are cut in half by hand, the stones are removed, and the fruit is laid out on wooden frames on flat roofs, where it dries in the sun for about a week. No chemicals are added, which is why Sariqsoy apricots are brown rather than bright orange. The cooperative has, however, written strict rules about cleanliness and checks every batch, because a single bad delivery could damage the reputation the village has worked so hard to build.

        19. A few years later, the villagers held their first apricot festival, expecting a few hundred guests. More than five thousand came. The festival is now a three-day event with music, cooking competitions and tours of the orchards, and many families have turned spare rooms into guest houses. For the first time in decades, young people can earn a living at home, and several who had moved to the city have returned to open cafés, work as guides or manage the cooperative's online shop.

        20. Success has not removed every risk. Apricot trees flower early in spring, and a single night of frost at the wrong moment can destroy most of the crop. This happened three years ago, when the harvest was less than a third of its usual size. Since then, the cooperative has put part of its income into a fund to support families in bad years, and farmers have started planting varieties that flower a little later. Nobody can control the weather, but the village is now better prepared for it.
        """,
        [
            new("match", R3, null,
            [
                Item(15, "Paragraph 15", "\"Twenty years ago, few people outside the region had heard of Sariqsoy ... How did such a small place become so well known?\"", "C"),
                Item(16, "Paragraph 16", "\"Winters are cold enough ... summers are long, hot and dry. Water from melting snow reaches the orchards.\"", "G"),
                Item(17, "Paragraph 17", "\"A retired schoolteacher persuaded forty families to form a cooperative. By selling together ...\"", "E"),
                Item(18, "Paragraph 18", "\"The drying itself is done much as it was by their grandparents ... strict rules about cleanliness.\"", "D"),
                Item(19, "Paragraph 19", "\"Several who had moved to the city have returned to open cafés, work as guides ...\"", "H"),
                Item(20, "Paragraph 20", "\"A fund to support families in bad years ... varieties that flower a little later.\"", "A"),
            ],
            [
                "Planning for difficult years",
                "Too many visitors for a small village",
                "From a quiet village to a famous name",
                "Traditional methods, modern standards",
                "Stronger together",
                "Selling fresh fruit abroad",
                "Ideal conditions for growing",
                "A reason to come home",
            ]),
        ]);

    private static ReadingPassage Reading7Part4() => new("More than a game: teenagers and team sports",
        """
        Ask adults what they remember most clearly about their teenage years, and many will mention a team: the football club that trained on Tuesday evenings, the volleyball team that lost every match for a whole season, the basketball coach who never seemed satisfied. For a large number of people, team sport is where some of the most important lessons of adolescence are learned. Yet in many countries, participation drops sharply between the ages of thirteen and seventeen, and the reasons for this, as well as what could be done about it, deserve closer attention.

        The physical benefits of regular exercise are well known, and they apply to any sport, whether it is played alone or in a group. What makes team sports different, researchers argue, is their effect on social and emotional development. A teenager in a team has to cooperate with people he or she did not choose, accept decisions made by a coach, and deal with both success and failure in public. Several long-term studies have found that young people who played team sports reported lower levels of anxiety and sadness in their twenties than those who did not, and the effect was stronger than for young people who only exercised on their own. Researchers are careful to point out, however, that such studies show a connection rather than proving that one thing causes the other: it is possible that confident, sociable teenagers are simply more likely to join teams in the first place.

        Team sports also seem to teach skills that are valued far beyond the sports field. Employers frequently say that they look for people who can work in a group, communicate clearly under pressure and recover quickly from mistakes. A goalkeeper who lets in a goal in the first minute and must stay focused for the rest of the match is practising exactly this kind of resilience. Some teachers also report that pupils who are members of teams are better at organising their time, perhaps because they have to fit homework around training sessions and matches.

        Why, then, do so many teenagers give up? Surveys point to several reasons. The most common one is simply that the activity stops being fun. As players get older, many clubs begin to focus heavily on winning, and those who are less talented spend more time sitting on the bench than playing. Others leave because of pressure from schoolwork, especially in the years before important exams. Cost can be a further barrier: equipment, travel to matches and club fees can add up to a significant amount, which some families cannot afford. Girls tend to leave team sports earlier and in greater numbers than boys, and many say they feel uncomfortable about how they look while playing or believe that sport is "not for them".

        A number of clubs and schools have tried to reverse this trend. One approach is to create "recreational" teams alongside competitive ones, where the aim is to play and enjoy the game rather than to win trophies. Another is to offer shorter, more flexible sessions that fit around study. Some sports have also introduced new versions of traditional games, such as three-a-side basketball, which need fewer players and less space. Early results suggest that these changes can keep teenagers involved for longer, although it is too soon to know whether the effect will last.

        Parents, too, have an important role to play, though not always the one they expect. Coaches often say that the most helpful parents are not those who shout instructions from the side of the pitch, but those who ask their children after a match whether they enjoyed it, rather than whether they won. Young people who feel that their parents' approval depends on results are, according to coaches, more likely to lose interest.

        None of this means that every teenager must join a team. Some young people are happier running, swimming or climbing on their own, and they gain a great deal from it. But for those who do enjoy playing with others, the evidence suggests that staying in the game through the difficult teenage years may bring benefits that last long after the final whistle.
        """,
        [
            new("mcq", R4Mcq, null,
            [
                Choice(21, "What does the writer say about the long-term studies mentioned in the second paragraph?", "C", "\"Such studies show a connection rather than proving that one thing causes the other.\"",
                    "They prove that team sports make young people happier.", "They found no difference between team sports and exercising alone.", "They cannot show for certain that team sports cause the benefits.", "They only included teenagers who were already confident."),
                Choice(22, "Why does the writer mention a goalkeeper?", "B", "\"Employers ... look for people who can ... recover quickly from mistakes. A goalkeeper ... is practising exactly this kind of resilience.\"",
                    "to show that some positions in a team are more stressful than others", "to give an example of a skill that is also useful at work", "to explain why employers prefer people who played football", "to suggest that mistakes early in a match are very common"),
                Choice(23, "According to surveys, what is the most common reason why teenagers give up team sports?", "A", "\"The most common one is simply that the activity stops being fun.\"",
                    "They no longer enjoy it.", "They have too much schoolwork.", "Their families cannot afford it.", "They are not good enough to win."),
                Choice(24, "What do coaches say about parents?", "D", "\"Young people who feel that their parents' approval depends on results are ... more likely to lose interest.\"",
                    "Parents should give their children instructions during matches.", "Most parents care more about winning than about enjoyment.", "Parents should choose which sport their child plays.", "Children may lose interest if they feel they must win to please their parents."),
            ]),
            new("tfng", R4Tfng, null,
            [
                Item(25, "In many countries, fewer young people take part in team sports as they move through their teenage years.", "\"Participation drops sharply between the ages of thirteen and seventeen.\"", "TRUE"),
                Item(26, "Some teachers have noticed that pupils in teams are worse at organising their time.", "\"Pupils who are members of teams are better at organising their time.\"", "FALSE"),
                Item(27, "Most girls who leave team sports take up an individual sport instead.", "The text gives reasons why girls leave but says nothing about what they do afterwards.", "NOT GIVEN"),
                Item(28, "It has been shown that recreational teams keep teenagers playing sport permanently.", "\"It is too soon to know whether the effect will last.\"", "FALSE"),
                Item(29, "The writer accepts that individual sports can also be valuable for young people.", "\"Some young people are happier running, swimming or climbing on their own, and they gain a great deal from it.\"", "TRUE"),
            ]),
        ]);

    private static ReadingPassage Reading7Part5() => new("The second life of a glass bottle",
        """
        Glass is one of the oldest materials made by humans, and one of the easiest to recycle. A glass bottle thrown into a recycling bin today can, in principle, be back on a shop shelf as a new bottle within a few weeks, and unlike many plastics, glass does not lose quality however many times it is melted down. Yet the journey from bin to bottle is more complicated than most people imagine, and a surprising amount of the glass that is collected never becomes a new container.

        To understand why recycling glass is worthwhile, it helps to know how new glass is made. The main ingredient is silica sand, which is mixed with soda ash and limestone and heated in a furnace to around 1,500°C until it becomes a thick liquid. The soda ash lowers the melting point of the sand, while the limestone makes the finished glass stronger and stops it from dissolving in water. Keeping a furnace at such a high temperature requires enormous amounts of energy, and the chemical reactions involved in melting the raw materials also release carbon dioxide.

        Recycled glass, which is crushed into small pieces known as cullet, makes this process more efficient. Cullet melts at a lower temperature than the raw materials, so every tonne that is added to the furnace reduces the amount of fuel needed. As a rough guide, the industry estimates that increasing the share of cullet by ten per cent lowers energy use by around two to three per cent. It also means that less sand needs to be dug from quarries and rivers. Some furnaces producing green bottles use a mixture that is up to ninety per cent recycled glass.

        The first stage of the process takes place at a sorting plant. Collected glass is tipped onto a moving belt, where workers remove large objects by hand. Magnets then pull out steel bottle tops, and jets of air blow away paper labels and light pieces of plastic. The most important and most difficult step is sorting by colour. Clear, green and brown glass have different chemical compositions, and even a small amount of coloured glass can spoil a batch of clear bottles. Modern plants use optical sorters, in which cameras identify each piece as it falls and fire short bursts of air to push it into the right container.

        Contamination is the greatest enemy of glass recycling. Many people assume that any glass object can go into the bottle bank, but this is not the case. Drinking glasses, window panes, mirrors and ovenproof dishes are made to different recipes and melt at different temperatures. A single piece of heat-resistant glass may fail to melt completely in the furnace and leave a weak spot in a new bottle, which could later break. Ceramics, such as pieces of a broken cup or plate, cause similar problems, which is why recycling centres ask the public to keep them out of glass collections.

        Glass that is too mixed or too dirty to become a new container is not necessarily wasted. It can be melted and spun into glass wool, a material used to insulate the walls and roofs of buildings. It can also replace some of the sand and stone in concrete or in the lower layers of roads. However, these uses save less energy than turning old bottles into new ones, and experts describe them as a second-best option.

        Finally, some specialists argue that recycling is not the most efficient solution of all. Glass is heavy, and transporting it to a sorting plant and then to a furnace uses fuel. In several countries, bottles for milk, water and soft drinks are collected, washed and refilled twenty times or more before they are finally recycled. Refillable systems work best where bottles are used and returned locally, and they depend on shops and customers being willing to take part. Where such systems exist, the humble glass bottle is one of the most sustainable forms of packaging ever invented.
        """,
        [
            new("gap", R5Gap, 2,
            [
                Item(30, "Soda ash is added to the sand because it lowers its ___.", "\"The soda ash lowers the melting point of the sand.\"", "melting point"),
                Item(31, "Pieces of crushed recycled glass, called ___, melt at a lower temperature than the raw materials.", "\"Recycled glass, which is crushed into small pieces known as cullet ... Cullet melts at a lower temperature.\"", "cullet"),
                Item(32, "At the sorting plant, steel bottle tops are removed with ___.", "\"Magnets then pull out steel bottle tops.\"", "magnets"),
                Item(33, "Glass that cannot become a new container may be turned into ___, which is used to insulate buildings.", "\"It can be melted and spun into glass wool, a material used to insulate the walls and roofs of buildings.\"", "glass wool"),
            ]),
            new("mcq", R5Mcq, null,
            [
                Choice(34, "According to the text, why is sorting glass by colour so important?", "B", "\"Even a small amount of coloured glass can spoil a batch of clear bottles.\"",
                    "Coloured glass melts at a much higher temperature.", "A little coloured glass can ruin a batch of clear glass.", "Optical sorters cannot recognise brown glass.", "Green bottles are worth more than clear ones."),
                Choice(35, "What is the writer's view of refillable bottles?", "C", "\"Where such systems exist, the humble glass bottle is one of the most sustainable forms of packaging.\"",
                    "They are cheaper for shops than recycling.", "They are only suitable for milk.", "In the right conditions, they can be better than recycling.", "They should replace all other kinds of packaging."),
            ]),
        ]);

    // =====================================================================
    // READING 8
    // =====================================================================

    private static ReadingPassage Reading8Part1() => new("Aqua Centre: main pool closed for repairs",
        """
        NOTICE TO ALL MEMBERS

        Main pool closed for repairs: 3–30 March

        Please note that the main swimming pool at the Riverside Aqua Centre will be closed from Monday 3 March to Sunday 30 March. During this time, workers will replace the old tiles on the floor and walls of the pool and repair the heating system, which has broken down twice this winter.

        We know that this closure will be inconvenient for many of you, especially those who swim every day. However, the repairs cannot wait any longer. Several (1) ______ have come loose in recent weeks, and there is a real danger that swimmers could cut their feet.

        The children's pool will stay open as usual, because it has its own (2) ______ system and its floor was repaired last year.

        The sauna and the gym on the first floor will also remain open at the normal times. During the closure, swimmers may use the (3) ______ free of charge, and our trainers will be happy to show them how to use the machines.

        All adult swimming lessons are cancelled until the end of March. Children's (4) ______ will continue in the small pool at the usual times, although some may be moved to Saturday afternoons.

        If you have a monthly card, you will not lose any money. Your (5) ______ will automatically be extended by four weeks, so you do not need to do anything. If you would prefer a refund instead, please fill in a form at reception.

        We expect the pool to reopen on Monday 31 March. For the latest information, please check our website or ask the staff at (6) ______.

        Thank you for your patience. We are sure you will enjoy the new pool!

        The Management
        """,
        [
            new("gap", R1, 1,
            [
                Item(1, "Several ___ have come loose in recent weeks, and there is a real danger that swimmers could cut their feet.", "\"Workers will replace the old tiles on the floor and walls of the pool.\"", "tiles"),
                Item(2, "The children's pool will stay open as usual, because it has its own ___ system and its floor was repaired last year.", "\"Repair the heating system, which has broken down twice this winter.\"", "heating"),
                Item(3, "During the closure, swimmers may use the ___ free of charge, and our trainers will be happy to show them how to use the machines.", "\"The sauna and the gym on the first floor\" — only the gym has trainers and machines.", "gym"),
                Item(4, "Children's ___ will continue in the small pool at the usual times, although some may be moved to Saturday afternoons.", "\"All adult swimming lessons are cancelled\" — the children's ones continue.", "lessons"),
                Item(5, "Your ___ will automatically be extended by four weeks, so you do not need to do anything.", "\"If you have a monthly card, you will not lose any money.\"", "card"),
                Item(6, "For the latest information, please check our website or ask the staff at ___.", "\"Please fill in a form at reception.\"", "reception"),
            ]),
        ]);

    private static ReadingPassage Reading8Part2() => new("Where to shop at the weekend",
        """
        7. Old Town Book Market. Every Saturday morning, the square in front of the old library fills with tables of second-hand books in Uzbek, Russian, English and many other languages. Prices are low, and sellers expect you to bargain, so never pay the first price you hear. Bring enough cash with you, as none of the sellers accept cards and the nearest cash machine is a fifteen-minute walk away.

        8. Green Valley Farmers' Market. Held on Sundays in the car park of the sports stadium, this market has one simple rule: everything on sale must be grown, raised or made by the people behind the stall. You will not find traders reselling fruit from wholesale markets here. Expect seasonal vegetables, fresh cheese, honey and home-baked bread, all from farms less than fifty kilometres from the city.

        9. Lantern Market. When the sun goes down on Fridays and Saturdays, the street along the canal is closed to traffic and hundreds of lanterns are lit. Stalls stay open from 7 p.m. until midnight, selling street food from all over Central Asia, handmade jewellery and souvenirs. Live bands play on a small stage, and on warm summer nights it can be difficult to move through the crowds.

        10. Railway Flea Market. Collectors of old coins, cameras, records and radios have been coming to this market beside the old railway yard for over thirty years. Serious buyers arrive at six in the morning with torches, because by eight o'clock the most interesting items have usually been sold to dealers. Later in the day the market becomes quieter, and it is easier to get a good price.

        11. Makers' Market. This market in the courtyard of the art college brings together potters, woodcarvers, embroiderers and knife-makers. What makes it special is that most of them bring their tools with them, so you can watch a bowl being shaped or a pattern being stitched while you wait. Children can also try simple activities, such as painting a tile, for a small fee.

        12. Spring and Autumn Plant Fair. Keen gardeners should mark their calendars: this fair takes place only on the first two weekends of April and the first two weekends of October, the best times for planting. Nurseries from across the region sell fruit trees, roses, herbs and vegetable seedlings, and experts are on hand to answer questions about soil, watering and pests.

        13. Young Traders' Market. On the last Saturday of each month, children aged eight to sixteen rent a table for a symbolic price and run their own stalls. They sell old toys, homemade biscuits, drawings and bracelets, and they are responsible for setting prices and giving change. Parents may help to carry things, but they are asked to stand back and let the children do the selling.

        14. Swap Shop Sunday. Tired of clothes you never wear? At this monthly event in the community hall, you bring clean clothes, shoes or books in good condition and exchange them for things that other people have brought. Nothing is bought or sold: for every item you bring, you receive a token that you can exchange for something else. Anything left at the end is given to charity.
        """,
        [
            new("match", R2, null,
            [
                Item(7, "Text 7", "\"Bring enough cash with you, as none of the sellers accept cards.\"", "H"),
                Item(8, "Text 8", "\"Everything on sale must be grown, raised or made by the people behind the stall. You will not find traders reselling fruit.\"", "B"),
                Item(9, "Text 9", "\"Stalls stay open from 7 p.m. until midnight.\"", "J"),
                Item(10, "Text 10", "\"By eight o'clock the most interesting items have usually been sold to dealers.\"", "E"),
                Item(11, "Text 11", "\"You can watch a bowl being shaped or a pattern being stitched while you wait.\"", "I"),
                Item(12, "Text 12", "\"This fair takes place only on the first two weekends of April and the first two weekends of October.\"", "A"),
                Item(13, "Text 13", "\"Children aged eight to sixteen ... run their own stalls.\"", "F"),
                Item(14, "Text 14", "\"Nothing is bought or sold: for every item you bring, you receive a token.\"", "D"),
            ],
            [
                "It only takes place at certain times of the year.",
                "Selling goods bought from someone else is not allowed.",
                "Visitors have to pay to get in.",
                "No money is used.",
                "It is best to arrive very early.",
                "Young people run the stalls.",
                "It has recently moved to a new location.",
                "You cannot pay by card.",
                "You can watch items being made.",
                "It is open late at night.",
            ]),
        ]);

    private static ReadingPassage Reading8Part3() => new("Teaching children to code",
        """
        15. On a Saturday morning in a bright room above a bookshop, twelve children aged eight to eleven are arguing loudly about a robot. It keeps turning left when it should turn right, and they are determined to find out why. Watching them with a smile is Dilnoza Rahimova, who started the club six years ago in her own living room with just five pupils. Today her organisation runs weekend classes in three cities, and more than four hundred children attend every week.

        16. Rahimova did not plan to become a teacher. For ten years she worked as a software developer for a large bank, and she enjoyed the work. The turning point came when she watched her young nephews using a tablet. "They were brilliant at games and videos," she says, "but when I asked them how a game was made, they had no idea. They thought computers were magic boxes that other people controlled. I wanted them to be makers, not just users."

        17. Surprisingly, the children's first lessons often take place without a single computer. In one popular activity, a child has to guide a blindfolded partner across the room using only simple commands such as "two steps forward" and "turn right". In another, the group writes instructions for making a sandwich, which Rahimova then follows exactly as written, often with messy results. "Programming is really about breaking a problem into clear steps," she explains. "Children understand that much better when they can see and touch it."

        18. When the club began, almost all of the pupils were boys. Rahimova noticed that parents were more likely to sign up their sons than their daughters, and that some girls who came once never returned. She changed the posters, which had shown only boys, and started inviting women who work in technology to speak to the classes. She also introduced projects such as designing animations and building a website for an animal shelter. Today, girls make up almost half of the pupils.

        19. Perhaps the most unusual feature of her classes is the way they treat errors. Every Saturday, the children vote for the "bug of the week", the most interesting mistake anyone has made in their code, and the winner explains to the group how they found and fixed it. Some parents were confused at first, as they expected their children to be praised for getting things right. But Rahimova is convinced that learning to stay calm when something goes wrong is one of the most valuable skills her pupils develop.

        20. Her next goal is to reach villages, where many schools have few computers and even fewer teachers trained to use them. She has written a free course for teachers that relies mostly on activities that need no equipment at all. She insists that her aim has never been to turn every child into a programmer. "Most of them will become doctors, farmers or artists," she says, "but all of them will need to think logically and solve problems. That is what we are really teaching."
        """,
        [
            new("match", R3, null,
            [
                Item(15, "Paragraph 15", "\"Started the club six years ago in her own living room with just five pupils. Today ... more than four hundred children.\"", "D"),
                Item(16, "Paragraph 16", "\"I wanted them to be makers, not just users.\"", "H"),
                Item(17, "Paragraph 17", "\"The children's first lessons often take place without a single computer.\"", "C"),
                Item(18, "Paragraph 18", "\"She changed the posters ... Today, girls make up almost half of the pupils.\"", "E"),
                Item(19, "Paragraph 19", "\"The children vote for the 'bug of the week', the most interesting mistake.\"", "A"),
                Item(20, "Paragraph 20", "\"Her aim has never been to turn every child into a programmer ... all of them will need to think logically.\"", "G"),
            ],
            [
                "Learning to love mistakes",
                "Winning national competitions",
                "Lessons that begin away from the screen",
                "A small idea that grew",
                "Making the club welcoming for everyone",
                "Why she disliked her old job",
                "Thinking skills for any future",
                "Makers rather than users",
            ]),
        ]);

    private static ReadingPassage Reading8Part4() => new("Living large in a small space",
        """
        In many of the world's big cities, the average size of a new flat has been shrinking for years. In some places, developers now build so-called micro-apartments of less than thirty square metres, with a bed, a tiny kitchen and a shower room in a space not much bigger than a hotel room. For some people, this is a sign of an unfair housing market; for others, it is a sensible response to how we live today. The truth, as usual, lies somewhere in between.

        The main reason for the trend is simple: land in city centres is expensive, and it is becoming more so. A smaller flat costs less to buy or rent, and for many young professionals, the choice is not between a small flat and a large one, but between a small flat in the centre and a larger one an hour and a half away. Many choose to live small in exchange for being able to walk to work, meet friends easily and enjoy the cafés, cinemas and parks that the city offers. At the same time, the number of people living alone has risen steadily in many countries, and a single person may simply not need three rooms.

        Designers have become remarkably inventive in making small spaces work. Beds fold up into the wall during the day, tables slide out from kitchen cupboards, and stairs double as drawers. Light-coloured walls, large windows and mirrors make rooms feel bigger than they are, while shelves that reach all the way to the ceiling make use of space that is usually wasted. One architect who specialises in small homes says that the most important rule is to plan for every activity: "You need to know exactly where you will sleep, work, eat and store your winter coat before you move in, not after."

        Those who have made the move often describe unexpected benefits. A small flat takes less time to clean and costs less to heat, and electricity and water bills are usually lower. Many residents also say that living in a small space forced them to think carefully about what they really need. With no room for things that are rarely used, they buy less, and several say they feel lighter and less stressed as a result. "I used to spend my weekends shopping," one young teacher explains. "Now I spend them outdoors."

        But small living is not for everyone, and critics warn against presenting it as a lifestyle choice when for many people it is a financial necessity. Research on housing and wellbeing suggests that overcrowding can cause serious problems, particularly for families with children. When there is no quiet corner to study, sleep or be alone, arguments become more frequent and children's schoolwork may suffer. Working from home, which has become common in recent years, adds another difficulty: it is hard to concentrate when the bed is two metres from the desk.

        Some countries have responded by setting minimum sizes for new homes, so that developers cannot keep reducing the space in order to increase their profits. Others have taken a different approach, encouraging buildings where private flats are small but residents share generous common areas, such as a large kitchen, a laundry room, a workspace or a roof garden. Supporters argue that such buildings can also reduce loneliness, because they create natural opportunities for neighbours to meet. Critics reply that sharing works well for students and young adults, but less well for older people who value their privacy.

        What seems clear is that the size of a home matters less than how well it fits the people who live in it. A well-designed thirty-square-metre flat in the right location may give a single person a better life than a poorly planned flat three times the size. For a family of four, however, the same space would be a daily struggle. The challenge for cities is to offer enough choice so that a small home is a decision people make, rather than the only option they have.
        """,
        [
            new("mcq", R4Mcq, null,
            [
                Choice(21, "According to the second paragraph, why do many young professionals choose small flats?", "B", "\"The choice is ... between a small flat in the centre and a larger one an hour and a half away. Many choose to live small in exchange for being able to walk to work.\"",
                    "They cannot find large flats anywhere in the city.", "They prefer being close to the centre to having more space.", "They plan to move to a larger flat when they have children.", "They want to spend less time travelling to other cities."),
                Choice(22, "What does the architect say is the most important rule?", "C", "\"You need to know exactly where you will sleep, work, eat and store your winter coat before you move in.\"",
                    "Choose furniture that folds away.", "Paint the walls in light colours.", "Decide how every part of the space will be used before moving in.", "Buy as few things as possible."),
                Choice(23, "Why does the writer mention the young teacher?", "A", "\"I used to spend my weekends shopping ... Now I spend them outdoors.\"",
                    "to show how living in a small flat can change people's habits", "to show that small flats are cheaper to heat", "to suggest that teachers cannot afford large flats", "to show that people in small flats often feel lonely"),
                Choice(24, "What do critics say about buildings with shared spaces?", "D", "\"Sharing works well for students and young adults, but less well for older people who value their privacy.\"",
                    "They are too expensive to build.", "They make neighbours argue more often.", "They are only popular in a few countries.", "They may not suit people who want to be on their own."),
            ]),
            new("tfng", R4Tfng, null,
            [
                Item(25, "The number of people who live alone has increased in many countries.", "\"The number of people living alone has risen steadily in many countries.\"", "TRUE"),
                Item(26, "People who live in small flats usually pay more for electricity and water.", "\"Electricity and water bills are usually lower.\"", "FALSE"),
                Item(27, "Most people who move into small flats later return to larger homes.", "The text does not say what residents do later.", "NOT GIVEN"),
                Item(28, "Living in overcrowded conditions can affect how well children do at school.", "\"Children's schoolwork may suffer.\"", "TRUE"),
                Item(29, "Every country has now introduced minimum sizes for new homes.", "\"Some countries have responded by setting minimum sizes ... Others have taken a different approach.\"", "FALSE"),
            ]),
        ]);

    private static ReadingPassage Reading8Part5() => new("How the brain learns new words",
        """
        An adult who speaks one language knows somewhere between twenty and forty thousand words, depending on how a "word" is defined and how knowledge is measured. Each of these words had to be learned at some point, usually without any conscious effort. How the brain manages this remarkable task has fascinated psychologists and neuroscientists for decades, and recent research has begun to reveal a process that is both faster and slower than we might expect.

        It is faster because the first step can happen almost instantly. Young children are able to link a new word to its meaning after hearing it only once or twice, a skill researchers call "fast mapping". In a typical experiment, a child is shown several familiar objects and one unfamiliar one, and is asked to pass "the toma". Most children immediately choose the unknown object, reasoning that a new word probably refers to something they do not already have a name for. Adults use similar strategies when they meet an unfamiliar word in a text and guess its meaning from the context.

        However, recognising a word once is very different from truly knowing it. To use a word correctly, a learner needs to know how it sounds and is spelled, what it means in different situations, which words it usually appears with and how it changes form. This fuller knowledge develops gradually, through repeated encounters. Estimates vary, but many researchers suggest that a learner may need to meet a word between ten and twenty times, in a variety of contexts, before it becomes a stable part of their vocabulary.

        Where these memories are stored also changes over time. Brain-imaging studies suggest that when a new word is first learned, a structure deep in the brain called the hippocampus plays a central role. The hippocampus is good at learning new information quickly, but it keeps each item somewhat separate from existing knowledge. Over the following days and weeks, the word gradually becomes connected to the network of words already stored in the outer layer of the brain, the cortex. Only when this has happened does a new word start to behave like an old one: for example, it begins to be activated automatically when a similar-sounding word is heard.

        Sleep appears to be important for this transfer. In several experiments, adults learned a list of invented words and were tested either on the same day or after a night's sleep. The new words only began to interact with similar existing words after the participants had slept. Researchers believe that during deep sleep, the brain replays recently learned information, strengthening the connections between new memories and older ones. This may explain why a word that seemed impossible to remember in the evening can feel more familiar the next morning.

        These findings have practical implications for language learners. First, spreading study over time is far more effective than concentrating it into a single long session, a principle known as the spacing effect. Reviewing a word after a day, then after a few days, then after a week or two, gives the brain repeated opportunities to strengthen the memory. Second, trying to recall a word from memory, for example by covering the translation and testing yourself, produces stronger learning than simply reading the word again. Third, meeting words in meaningful contexts, such as stories, conversations and articles, helps learners build the rich connections that make a word easy to find when it is needed.

        None of this means that vocabulary learning can be made effortless. But it does suggest that the traditional image of a student memorising long lists of words the night before an exam is one of the least efficient ways to learn. A little regular practice, a variety of contexts and a good night's sleep may achieve far more.
        """,
        [
            new("gap", R5Gap, 2,
            [
                Item(30, "Young children can connect a new word with its meaning after hearing it only once or twice, an ability known as ___.", "\"A skill researchers call 'fast mapping'.\"", "fast mapping"),
                Item(31, "When a word is first learned, a structure deep in the brain called the ___ plays a central role.", "\"A structure deep in the brain called the hippocampus plays a central role.\"", "hippocampus"),
                Item(32, "Over the following days and weeks, the word becomes linked to words stored in the outer layer of the brain, the ___.", "\"The network of words already stored in the outer layer of the brain, the cortex.\"", "cortex"),
                Item(33, "Reviewing words at increasing intervals rather than in one long session makes use of the ___.", "\"Spreading study over time ... a principle known as the spacing effect.\"", "spacing effect"),
            ]),
            new("mcq", R5Mcq, null,
            [
                Choice(34, "What did the experiments with invented words show?", "B", "\"The new words only began to interact with similar existing words after the participants had slept.\"",
                    "Adults remember invented words better than real ones.", "New words became connected to existing words only after sleep.", "Participants forgot most of the new words during the night.", "Deep sleep is needed to learn how a word is spelled."),
                Choice(35, "Which way of learning vocabulary does the writer consider one of the least efficient?", "D", "\"A student memorising long lists of words the night before an exam is one of the least efficient ways to learn.\"",
                    "reviewing words over several weeks", "testing yourself by covering the translation", "meeting words in stories and articles", "studying long lists of words just before an exam"),
            ]),
        ]);

    // =====================================================================
    // READING 9
    // =====================================================================

    private static ReadingPassage Reading9Part1() => new("Young Chefs Competition in the school canteen",
        """
        Dear pupils and parents,

        We are pleased to announce that our school will hold its first Young Chefs Competition on Saturday 18 May in the school canteen. The competition is open to all pupils from Year 7 to Year 11, who may take part alone or with a partner.

        This year's theme is "a healthy lunch". Each competitor will have ninety minutes to prepare a main dish and a dessert. The (1) ______ should include some fresh fruit and must not contain more than two spoons of sugar.

        Competitors must bring their own ingredients, but the school will provide cookers, pans, knives and all other equipment. If you prefer to use your own (2) ______, please mark it clearly with your name.

        The dishes will be judged by a panel of three people: our head teacher, the canteen manager and a chef from a well-known restaurant in the city. Each member of the (3) ______ will give marks for taste, appearance and how healthy the meal is.

        To register, please fill in the form on the school website and attach a copy of your recipe. Every competitor will receive a certificate, and the winning (4) ______ will be printed in the school magazine with a photo of the dish.

        Parents and friends are welcome to watch from 12 noon and to taste the food at the end. Please note that because the (5) ______ will be needed for lunch on Friday, competitors cannot come in to practise the day before.

        The deadline for registration is Friday 10 May, and we have space for only twenty competitors, so do not leave it too late. If you are entering with a partner, only one registration (6) ______ is needed for the two of you.

        We look forward to seeing you there!

        Mr Sobirov
        Head of Food Technology
        """,
        [
            new("gap", R1, 1,
            [
                Item(1, "The ___ should include some fresh fruit and must not contain more than two spoons of sugar.", "\"Prepare a main dish and a dessert\" — fruit and sugar refer to the dessert.", "dessert"),
                Item(2, "If you prefer to use your own ___, please mark it clearly with your name.", "\"The school will provide cookers, pans, knives and all other equipment.\"", "equipment"),
                Item(3, "Each member of the ___ will give marks for taste, appearance and how healthy the meal is.", "\"The dishes will be judged by a panel of three people.\"", "panel"),
                Item(4, "Every competitor will receive a certificate, and the winning ___ will be printed in the school magazine with a photo of the dish.", "\"Attach a copy of your recipe.\"", "recipe"),
                Item(5, "Please note that because the ___ will be needed for lunch on Friday, competitors cannot come in to practise the day before.", "\"The competition ... in the school canteen.\"", "canteen"),
                Item(6, "If you are entering with a partner, only one registration ___ is needed for the two of you.", "\"Please fill in the form on the school website.\"", "form"),
            ]),
        ]);

    private static ReadingPassage Reading9Part2() => new("Cafés worth a visit",
        """
        7. Page & Cup. Hidden above a bookshop in the old part of the city, Page & Cup has soft armchairs, large windows and walls covered with shelves. Any book on the shelves can be taken home, as long as you leave one of your own in its place, so the collection is always changing. The coffee is good rather than excellent, but the cheesecake is famous, and the quiet atmosphere makes it popular with students preparing for exams.

        8. Whiskers. At Whiskers, you share your tea with eleven rescued cats who live in the café and wander freely between the tables. All of them are looking for a new family, and several have already gone to live with customers who fell in love with them. Visitors are asked to wash their hands on arrival and not to wake sleeping cats. Children under ten are welcome but must be accompanied by an adult.

        9. Skyline Café. Take the glass lift to the twenty-second floor of the Trade Centre and you will find one of the most impressive places to drink coffee in the region. On a clear day, you can see across the whole city to the mountains in the east. Prices are high, and it can be difficult to get a table at sunset, so booking in advance is a good idea.

        10. Second Chance Café. This small café near the railway station is run by a charity that helps young people who have never had a job. Each year, twenty trainees learn to make coffee, serve customers and work in a team, and most of them find permanent work in hotels and restaurants within six months. The menu is simple, but the service is warm and the prices are very reasonable.

        11. Early Bird. As its name suggests, Early Bird is for people who like to start the day well. It opens at 6.30 every morning, seven days a week, and serves a wide range of breakfasts, from fresh bread with honey and cream to pancakes with berries. The kitchen stops cooking at 11.30, and the doors close at noon, so late risers will have to go elsewhere.

        12. The Clock Room. Here, the food and drink do not appear on the bill at all. Instead, every visitor takes an alarm clock at the door and pays 15,000 som for each hour they stay. Tea, coffee, biscuits and fruit are unlimited, and there are desks, sofas and plenty of sockets for laptops. It has become a popular place for freelancers who want to work somewhere other than their kitchen table.

        13. Offline. The owners of Offline have made one unusual rule: no phones or laptops. When you arrive, you are politely asked to leave your devices in a small locker by the door, and there is no wifi. Instead, there are newspapers, chess sets and long wooden tables where strangers often end up chatting. Some first-time visitors find it strange, but many return week after week.

        14. Bean School. Bean School roasts its own coffee in the back room, and the smell reaches the end of the street. As well as serving excellent coffee, the café runs two-hour courses every Saturday morning where, for a fee, you can learn to make the perfect espresso and draw patterns in milk foam. Places are limited to six people, and the price includes as much coffee as you can drink.
        """,
        [
            new("match", R2, null,
            [
                Item(7, "Text 7", "\"Any book on the shelves can be taken home, as long as you leave one of your own in its place.\"", "E"),
                Item(8, "Text 8", "\"All of them are looking for a new family, and several have already gone to live with customers.\"", "I"),
                Item(9, "Text 9", "\"It can be difficult to get a table at sunset, so booking in advance is a good idea.\"", "G"),
                Item(10, "Text 10", "\"Trainees learn to make coffee ... most of them find permanent work in hotels and restaurants.\"", "F"),
                Item(11, "Text 11", "\"The kitchen stops cooking at 11.30, and the doors close at noon.\"", "B"),
                Item(12, "Text 12", "\"Pays 15,000 som for each hour they stay.\"", "A"),
                Item(13, "Text 13", "\"You are politely asked to leave your devices in a small locker by the door.\"", "C"),
                Item(14, "Text 14", "\"For a fee, you can learn to make the perfect espresso and draw patterns in milk foam.\"", "H"),
            ],
            [
                "You pay for the time you spend there.",
                "It closes before lunchtime.",
                "Customers are asked not to use electronic devices.",
                "It is only open at weekends.",
                "You may take something away if you give something back.",
                "It prepares people for future employment.",
                "It is a good idea to reserve a table.",
                "You can pay to learn a new skill.",
                "Some of the animals you meet could go home with you.",
                "Children are not allowed inside.",
            ]),
        ]);

    private static ReadingPassage Reading9Part3() => new("A photographer above the clouds",
        """
        15. Nodir Umarov never intended to become a photographer. As a geography student, he joined a university trip to the mountains and took along a camera borrowed from his uncle, mainly to record rock formations for a project. When he looked at his pictures afterwards, he was disappointed: the peaks that had seemed so enormous looked small and flat. "I was annoyed," he says, "and that annoyance was the start of everything. I wanted to understand why the camera had failed to show what I had felt." Twenty years later, his mountain photographs appear in magazines and exhibitions around the world.

        16. Umarov's best-known images may look like lucky moments, but very few of them are. Before each trip, he studies maps to find out where the sun will rise and set in relation to a particular peak, and he checks weather forecasts for days. He then often waits in a tent for the right conditions. For one famous photograph of a frozen lake at dawn, he returned to the same spot four times over two years before the light, the clouds and the ice were exactly as he wanted them.

        17. The mountains have also taught him to respect their dangers. On one early expedition, he climbed too quickly, developed a severe headache and had to be helped down by his companions. On another, a storm arrived hours before it had been forecast. Since then, he has never travelled alone, and he has a strict rule: if conditions become dangerous, the group goes down, however close they are to the perfect photograph. "No picture is worth a life," he says. "The mountain will still be there next year."

        18. Although he is known as a landscape photographer, many of Umarov's favourite pictures include people: shepherds moving their flocks to summer pastures, children walking to a village school, a woman baking bread in a clay oven at 3,000 metres. He believes that a small human figure helps viewers understand the true scale of the landscape, and that it also tells a story. "People think mountains are empty," he explains. "But they have been home to families for thousands of years."

        19. In recent years, his work has taken on an unexpected scientific importance. Because he returns to the same places again and again, he has built up a collection of photographs showing how several glaciers have changed over two decades. In some pairs of pictures, taken from exactly the same position fifteen years apart, the ice has retreated by hundreds of metres. Researchers studying the climate of the region have asked to use his images, which provide a clear visual record that measurements alone cannot give.

        20. Umarov now spends part of each year running free workshops for young photographers from mountain villages. His first lesson always surprises them: expensive equipment, he says, matters far less than they think. Some of his students take impressive pictures with ordinary phones. What matters is learning to notice light, to be patient and to walk a little further than everyone else. "The best photographs," he tells them, "are usually taken by the person who stayed when everyone else went home."
        """,
        [
            new("match", R3, null,
            [
                Item(15, "Paragraph 15", "\"He was disappointed ... that annoyance was the start of everything.\"", "E"),
                Item(16, "Paragraph 16", "\"Very few of them are [lucky moments]. Before each trip, he studies maps ... checks weather forecasts.\"", "G"),
                Item(17, "Paragraph 17", "\"If conditions become dangerous, the group goes down ... No picture is worth a life.\"", "C"),
                Item(18, "Paragraph 18", "\"Many of Umarov's favourite pictures include people ... 'People think mountains are empty.'\"", "H"),
                Item(19, "Paragraph 19", "\"Photographs showing how several glaciers have changed over two decades.\"", "A"),
                Item(20, "Paragraph 20", "\"Running free workshops for young photographers from mountain villages.\"", "D"),
            ],
            [
                "A record of a changing landscape",
                "Selling pictures to magazines",
                "Safety comes first",
                "Passing on his skills",
                "Frustration that led to a career",
                "Why good equipment matters most",
                "Nothing is left to chance",
                "Why his landscapes are not empty",
            ]),
        ]);

    private static ReadingPassage Reading9Part4() => new("The true cost of fast fashion",
        """
        Twenty years ago, most clothing shops changed their collections a few times a year, usually in line with the seasons. Today, some of the largest fashion brands introduce new designs every week, and online retailers add thousands of new items to their websites every day. This business model, known as fast fashion, has made clothes cheaper and more varied than ever before. But a growing number of researchers, campaigners and even people within the industry are asking whether the true cost of a cheap T-shirt is much higher than the price on its label.

        The success of fast fashion is easy to understand. Brands watch what is being worn on catwalks, on social media and in the street, and can produce similar designs and deliver them to shops within a few weeks. Because the clothes are made in very large quantities, often in countries where wages are low, they can be sold at prices that almost anyone can afford. For young people in particular, this has made it possible to follow trends and express their personality through clothing in a way that was once only open to the wealthy.

        However, the speed and volume of production come at a considerable environmental price. According to one widely quoted industry report, the number of garments produced worldwide roughly doubled in the first fifteen years of this century, while the average number of times each item was worn fell sharply. Growing cotton requires large amounts of water and, in many places, pesticides. Polyester, now the most common fibre in clothing, is made from oil, and every time synthetic clothes are washed, tiny plastic fibres are released into the water, eventually reaching rivers and oceans. Estimates of the industry's total contribution to greenhouse gas emissions vary widely, but even the lowest figures suggest it is larger than many people would expect.

        There is also the question of what happens to clothes when we no longer want them. Many people assume that items given to charity shops or placed in collection bins will be worn again by someone else. Some are, but charities receive far more donations than they can sell, and a large proportion is exported, often to countries in Africa and Asia, where much of it is resold, cut up for rags or eventually thrown away. Only a very small percentage of old clothing is recycled into new clothes, largely because most garments are made from mixtures of fibres that are difficult to separate.

        The human cost has received increasing attention as well. Workers in some factories that supply fast-fashion brands earn very low wages and work long hours, sometimes in unsafe buildings. After several serious factory accidents, many brands signed agreements to improve safety inspections, and some now publish lists of the factories they use. Campaigners argue that progress remains slow, and that a system based on constantly falling prices makes it very difficult to pay workers fairly.

        What, then, can be done? Some brands have launched collections made from recycled or organic materials and offer to take back old clothes in their shops. Critics point out, however, that these initiatives are small compared with the total amount the same companies produce, and that encouraging customers to bring in old clothes in exchange for a discount on new ones may actually lead them to buy more. Some governments are considering making producers financially responsible for the clothes they sell even after they have been thrown away.

        Many experts believe that the most effective changes will come from consumers themselves. Buying fewer, better-made items, repairing clothes instead of replacing them, and choosing second-hand clothing are all becoming more popular, particularly among younger shoppers, who are often both the biggest buyers of fast fashion and its strongest critics. Clothing rental services and online platforms for selling used items have grown quickly. The most sustainable garment, as one designer put it, is the one that is already in your wardrobe.
        """,
        [
            new("mcq", R4Mcq, null,
            [
                Choice(21, "According to the second paragraph, why has fast fashion been so successful?", "B", "\"They can be sold at prices that almost anyone can afford ... possible to follow trends.\"",
                    "It offers better quality than traditional brands.", "It allows people to follow trends at a low cost.", "It only produces designs that have already sold well.", "It is mainly aimed at wealthy customers."),
                Choice(22, "What does the writer say about estimates of the industry's greenhouse gas emissions?", "D", "\"Estimates ... vary widely, but even the lowest figures suggest it is larger than many people would expect.\"",
                    "They are all much higher than people realise.", "They are based only on the production of cotton.", "They have been falling in recent years.", "They differ, but even the lowest are significant."),
                Choice(23, "According to the text, why is so little old clothing recycled into new clothes?", "C", "\"Most garments are made from mixtures of fibres that are difficult to separate.\"",
                    "Charities prefer to sell old clothes abroad.", "Most people throw clothes away instead of giving them to charity.", "Many garments contain fibres that are hard to separate.", "Recycled clothes are too expensive to sell."),
                Choice(24, "What criticism is made of schemes in which shops take back old clothes?", "A", "\"Encouraging customers to bring in old clothes in exchange for a discount on new ones may actually lead them to buy more.\"",
                    "They may encourage customers to buy even more.", "They are too expensive for most brands.", "Customers rarely use them.", "The clothes collected are never recycled."),
            ]),
            new("tfng", R4Tfng, null,
            [
                Item(25, "Some fashion brands now bring out new designs every week.", "\"Some of the largest fashion brands introduce new designs every week.\"", "TRUE"),
                Item(26, "Cotton is used more widely in clothing than polyester.", "\"Polyester, now the most common fibre in clothing.\"", "FALSE"),
                Item(27, "Charity shops are able to sell every item that people give them.", "\"Charities receive far more donations than they can sell.\"", "FALSE"),
                Item(28, "Some brands make public the names of the factories that produce their clothes.", "\"Some now publish lists of the factories they use.\"", "TRUE"),
                Item(29, "The designer quoted at the end of the text only uses recycled materials.", "The text quotes the designer but says nothing about the materials he or she uses.", "NOT GIVEN"),
            ]),
        ]);

    private static ReadingPassage Reading9Part5() => new("How solar panels turn light into electricity",
        """
        In 1839, a nineteen-year-old French scientist named Edmond Becquerel noticed that certain materials produced a small electric current when they were exposed to light. The discovery attracted little attention at the time, and more than a century passed before it was put to practical use. Today, the same phenomenon, known as the photovoltaic effect, powers everything from pocket calculators to vast solar farms covering many square kilometres, and solar energy has become one of the fastest-growing sources of electricity in the world.

        Most solar panels are made from silicon, the second most common element in the Earth's crust and the main component of ordinary sand. Silicon is a semiconductor: in its pure form, it conducts electricity poorly, but its properties can be changed by adding tiny amounts of other elements, a process called doping. In a typical solar cell, one thin layer of silicon is doped with phosphorus, which gives it extra electrons, while the layer beneath it is doped with boron, which leaves it with "holes" where electrons are missing. Where the two layers meet, an electric field forms, acting rather like a one-way barrier.

        Sunlight consists of tiny packets of energy called photons. When a photon with enough energy strikes the silicon, it can knock an electron free from its atom. The electric field at the boundary between the layers then pushes the freed electrons in one direction, towards the front of the cell. Thin metal lines on the surface collect these electrons, and if the cell is connected to a circuit, they flow through it as an electric current before returning to the back of the cell. As long as light continues to fall on the cell, the process continues, with no moving parts and no fuel.

        A single cell produces only a small voltage, so dozens of cells are connected together and sealed behind glass to form a panel. The electricity they produce is direct current (DC), which flows in one direction only. Homes and power grids, however, use alternating current (AC), so a device called an inverter is needed to convert it before it can be used by ordinary appliances or sent into the grid.

        No solar cell can convert all the energy in sunlight into electricity. Some photons do not have enough energy to free an electron, while others have more than is needed, and the extra energy is lost as heat. As a result, there is a theoretical limit of around 33 per cent for a simple cell of this type. The first practical silicon cells, produced in the United States in 1954, converted about 6 per cent of sunlight into electricity; most panels sold today achieve around 20 per cent, and the best laboratory cells, which combine several layers of different materials, have gone far beyond the limit for a single layer.

        Several factors affect how much electricity a panel produces in practice. The angle at which it faces the sun is important, which is why panels in the northern hemisphere usually face south. Dust and snow reduce the amount of light that reaches the cells. Perhaps surprisingly, heat is also a problem: solar cells work less efficiently when they are hot, so a panel may produce more electricity on a cold, bright day in spring than on a very hot day in summer.

        Solar panels are designed to last for a long time, and most manufacturers guarantee that their products will still produce at least 80 per cent of their original output after twenty-five years. Their main limitation is obvious: they produce nothing at night and much less on cloudy days. For this reason, the growth of solar energy is closely linked to improvements in batteries and other ways of storing electricity, so that the energy collected at midday can be used in the evening, when demand is often highest.
        """,
        [
            new("gap", R5Gap, 2,
            [
                Item(30, "The ability of some materials to produce an electric current when light falls on them is called the ___.", "\"The same phenomenon, known as the photovoltaic effect.\"", "photovoltaic effect"),
                Item(31, "The properties of silicon are changed by adding tiny amounts of other elements, a process known as ___.", "\"By adding tiny amounts of other elements, a process called doping.\"", "doping"),
                Item(32, "The freed electrons are collected by thin ___ on the surface of the cell.", "\"Thin metal lines on the surface collect these electrons.\"", "metal lines"),
                Item(33, "Because homes use alternating current, a device called an ___ is needed to convert the electricity.", "\"A device called an inverter is needed to convert it.\"", "inverter"),
            ]),
            new("mcq", R5Mcq, null,
            [
                Choice(34, "What does the text say about the effect of heat on solar panels?", "C", "\"Solar cells work less efficiently when they are hot.\"",
                    "Panels produce the most electricity in the hottest months.", "Heat can damage the glass that covers the cells.", "Panels work better when they are cool.", "Panels face south in order to avoid the heat."),
                Choice(35, "Why, according to the writer, is the development of storage important for solar energy?", "A", "\"They produce nothing at night ... so that the energy collected at midday can be used in the evening.\"",
                    "Panels cannot produce electricity when it is dark.", "Batteries make solar panels last longer.", "Panels produce most of their electricity in the evening.", "Storage improves the efficiency of each cell."),
            ]),
        ]);

    // =====================================================================
    // READING 10
    // =====================================================================

    private static ReadingPassage Reading10Part1() => new("School Science Fair: call for projects",
        """
        SCIENCE FAIR: CALL FOR PROJECTS

        This year's Science Fair will take place in the school hall on Thursday 12 March from 2 p.m. to 6 p.m. All pupils in Years 5 to 10 are invited to take part. Last year, more than eighty pupils presented their work, and we hope to break that record this year.

        Projects may be on any area of science, including biology, chemistry, physics and technology. You can work alone or in a group of up to three pupils. Each (1) ______ must choose a leader, who will be responsible for keeping in touch with the teachers.

        Every project must try to answer a question through an experiment, for example "Which type of soil helps beans grow fastest?" Projects that only describe a topic will not be accepted. Your results should be presented on a poster. The (2) ______ must be no larger than A1 size and should clearly show your question, your method and what you found out.

        Please give your science teacher the title of your project by Friday 14 February. Teachers will check that every experiment is safe, and projects using fire, strong chemicals or electricity from the mains will not be allowed. If you are unsure whether an (3) ______ is safe, ask before you begin.

        To help you prepare, the labs will be open after school every Tuesday and Thursday, and a teacher will be there to help you.

        On the day, each project will have its own table and a board. There are only twenty sockets in the hall, so if you need (4) ______ for a computer or a lamp, please mention this on your registration form.

        Projects will be judged by our science teachers and two guests from the university. Prizes will be awarded in each age group, and the overall winner will represent the school at the city science (5) ______ in April.

        Families are warmly invited to visit from 4 p.m. Year 11 pupils will sell drinks and cakes, and all the money raised will be spent on new equipment for the school (6) ______.

        Mr Aliev
        Head of Science
        """,
        [
            new("gap", R1, 1,
            [
                Item(1, "Each ___ must choose a leader, who will be responsible for keeping in touch with the teachers.", "\"You can work alone or in a group of up to three pupils.\"", "group"),
                Item(2, "The ___ must be no larger than A1 size and should clearly show your question, your method and what you found out.", "\"Your results should be presented on a poster.\"", "poster"),
                Item(3, "If you are unsure whether an ___ is safe, ask before you begin.", "\"Teachers will check that every experiment is safe.\"", "experiment"),
                Item(4, "There are only twenty sockets in the hall, so if you need ___ for a computer or a lamp, please mention this on your registration form.", "\"Projects using ... electricity from the mains will not be allowed.\"", "electricity"),
                Item(5, "The overall winner will represent the school at the city science ___ in April.", "\"This year's Science Fair will take place in the school hall.\"", "fair"),
                Item(6, "All the money raised will be spent on new equipment for the school ___.", "\"The labs will be open after school every Tuesday and Thursday.\"", "labs"),
            ]),
        ]);

    private static ReadingPassage Reading10Part2() => new("Bicycles for sale",
        """
        7. Mountain bike for sale, two years old, in very good condition. It has twenty-one gears, strong brakes and front suspension, so it is perfect for rough paths in the hills. There are a few small scratches on the frame, but nothing serious. I am only selling it because I am moving to Canada for my studies next month and cannot take it with me. Price: 1,800,000 som.

        8. Small bicycle for a child aged four to seven, with training wheels that can be removed later. My daughter learned to ride on it last summer and has already grown too big for it. The tyres are new and the bell still works! It would make a perfect first bike for a boy or girl who is just starting out. Price: 350,000 som. The buyer must collect it from our flat.

        9. Lightweight road bike with a carbon frame, weighing just eight kilograms. I have ridden it in several local competitions, including the city 100-kilometre race, where I finished in the top ten. It is very fast but not comfortable for everyday use, and it is not suitable for beginners. Fits riders between 175 and 185 cm tall. Price: 6,500,000 som, firm.

        10. Classic city bicycle made in the 1970s, which belonged to my grandfather. It has a beautiful original frame and a leather seat, but it has not been ridden for many years. The chain is rusty, the tyres are flat and the brakes need replacing. A great project for someone who enjoys repairing old things. Price: 400,000 som.

        11. Folding bike, ideal for people who travel to work by bus or metro. In less than twenty seconds, it folds down to the size of a small suitcase, so you can carry it onto public transport or keep it under your desk at the office. It has six gears and mudguards to keep your clothes clean on wet days. Price: 2,100,000 som.

        12. Electric bicycle with a battery that lasts for about fifty kilometres on a single charge. It makes hills easy and is great for longer journeys to work. The charger and a spare battery are included. I am asking 7,000,000 som, but I am happy to discuss the price with serious buyers.

        13. Tandem bicycle for two riders, in good working order. My wife and I used it for weekend rides along the river for several years, and it attracted a lot of friendly comments from other cyclists. Both seats can be adjusted, so it suits riders of different heights. Great fun for couples, friends or a parent and teenager. Price: 3,000,000 som.

        14. Cargo bike with a large wooden box at the front, strong enough to carry up to eighty kilograms. We used it to take our two children to kindergarten and to bring home the weekly shopping from the market. It has a wide stand, so it does not fall over while you load it. Price: 4,200,000 som, firm.
        """,
        [
            new("match", R2, null,
            [
                Item(7, "Text 7", "\"I am moving to Canada for my studies next month and cannot take it with me.\"", "D"),
                Item(8, "Text 8", "\"A perfect first bike for a boy or girl who is just starting out.\"", "J"),
                Item(9, "Text 9", "\"I have ridden it in several local competitions.\"", "C"),
                Item(10, "Text 10", "\"The chain is rusty, the tyres are flat and the brakes need replacing.\"", "H"),
                Item(11, "Text 11", "\"It folds down to the size of a small suitcase.\"", "F"),
                Item(12, "Text 12", "\"I am happy to discuss the price with serious buyers.\"", "B"),
                Item(13, "Text 13", "\"Tandem bicycle for two riders.\"", "A"),
                Item(14, "Text 14", "\"Strong enough to carry up to eighty kilograms.\"", "G"),
            ],
            [
                "Two people can ride it at the same time.",
                "The price is not fixed.",
                "It has been used in races.",
                "The seller is leaving the country.",
                "It has never been used.",
                "It can be made smaller for carrying.",
                "It is designed to carry heavy loads.",
                "It needs repairs before it can be ridden.",
                "The seller will deliver it to the buyer.",
                "It is suitable for someone learning to ride.",
            ]),
        ]);

    private static ReadingPassage Reading10Part3() => new("Four generations at the loom",
        """
        15. In a quiet courtyard in the south of the country, the steady sound of a small metal comb being beaten against wool can be heard from early morning. Inside, four women from the same family are sitting side by side in front of a wooden loom. The oldest, Oysha, is seventy-eight and has been weaving for more than sixty years. The youngest, her great-granddaughter Zarina, is twelve and still learning. Between them, four generations share the same room, the same patterns and the same patience.

        16. A hand-knotted carpet grows remarkably slowly. Each knot is tied separately around two vertical threads, cut with a small knife and pressed down with the comb. A carpet measuring two metres by three, made to the family's usual standard, contains close to a million knots, and it takes two experienced weavers working together between six and eight months to complete. "People sometimes ask why our carpets cost so much," says Oysha's daughter, Gulnora. "I tell them to sit at the loom for one day, and then ask me again."

        17. Almost everything the family uses comes from the land around them. The wool is bought from shepherds in nearby villages, washed in the river and spun by hand. The colours come from plants: deep red from the roots of the madder plant, yellow from pomegranate skins, brown from walnut shells and blue from indigo. Natural dyes produce slightly uneven colours, and they fade gently over the years rather than becoming dull. Many collectors consider this softening one of the main attractions of an old carpet.

        18. None of the family's designs are drawn on paper. Oysha carries dozens of patterns in her head, each one learned from her own mother and grandmother, and she teaches them to the younger women row by row. Some patterns represent objects such as rams' horns, water or trees; others have names whose meaning has been forgotten. Occasionally, a weaver adds a small, deliberate change, such as the first letter of a new baby's name, so that every carpet also records a moment in the family's life.

        19. There was a time when it seemed the tradition would end. In the 1990s, cheap machine-made carpets filled the markets, and few buyers were willing to pay for months of handwork. The family's looms stood empty for several years, Gulnora worked in a shop, and her husband found a job as a driver. "We thought my mother would be the last weaver in the family," she remembers. "It was very painful for her to see the looms covered with dust."

        20. The situation began to change when a small group of foreign visitors, travelling with a local guide, knocked on their door and asked to see how carpets were made. Word spread, and today tourists, collectors and interior designers from many countries place orders, often through the family's page on social media. One of their carpets is now on display in a museum abroad. The family has even started teaching girls from neighbouring houses, and Zarina, who once found weaving boring, now says she wants to have her own loom one day.
        """,
        [
            new("match", R3, null,
            [
                Item(15, "Paragraph 15", "\"Four women from the same family ... four generations share the same room.\"", "D"),
                Item(16, "Paragraph 16", "\"Close to a million knots ... between six and eight months to complete.\"", "H"),
                Item(17, "Paragraph 17", "\"The colours come from plants: deep red from the roots of the madder plant ...\"", "A"),
                Item(18, "Paragraph 18", "\"None of the family's designs are drawn on paper. Oysha carries dozens of patterns in her head.\"", "B"),
                Item(19, "Paragraph 19", "\"The family's looms stood empty for several years.\"", "E"),
                Item(20, "Paragraph 20", "\"A small group of foreign visitors ... knocked on their door ... Word spread.\"", "F"),
            ],
            [
                "Colours from nature",
                "Patterns kept in memory",
                "The dangers of chemical dyes",
                "Four generations in one room",
                "When the looms fell silent",
                "Visitors who changed everything",
                "Why machine-made carpets are better",
                "Months of work in every carpet",
            ]),
        ]);

    private static ReadingPassage Reading10Part4() => new("Can our cities become quieter?",
        """
        Close your eyes on a street in almost any large city and listen. Cars and buses, motorbikes, building sites, air conditioners, sirens, music from shops and cafés: together they create a background noise that many residents hardly notice any more. Yet a growing body of research suggests that this constant sound is far from harmless, and that noise deserves to be taken as seriously as other forms of pollution.

        Noise is measured in decibels, a scale that works in an unusual way. Because it is logarithmic, an increase of ten decibels represents ten times as much sound energy, and most listeners hear it as roughly twice as loud. A quiet library might measure around 40 decibels, normal conversation about 60, and heavy traffic close by 80 or more. The World Health Organization has published guidelines recommending limits for average traffic noise, and in many city centres these limits are regularly exceeded, especially at night.

        The most obvious effect of noise is on sleep. Even when people do not wake up, sudden sounds such as a passing motorbike can make the heart beat faster and disturb the deeper stages of sleep. Over months and years, poor sleep is linked to tiredness, difficulty concentrating and a weaker immune system. Studies of people living near busy roads and airports have also found higher rates of high blood pressure and heart disease, although researchers stress that it is difficult to separate the effects of noise from those of air pollution, which often occurs in the same places.

        Children may be particularly affected. Several studies have compared pupils at schools under the flight paths of busy airports with pupils at similar schools in quieter areas. Those exposed to aircraft noise tended to perform less well on reading tests and on tasks requiring long-term memory. When one city's old airport closed and a new one opened elsewhere, the difference among children near the old airport gradually disappeared, while children near the new airport began to show similar problems. This suggests that the effect can be reversed.

        Why, then, is noise so often ignored? Part of the answer is that people adapt to it. After a few weeks in a new flat, the sound of traffic becomes part of the background, and many residents insist that they no longer hear it. However, research suggests that the body continues to react, even when the mind has stopped paying attention. Another reason is that noise, unlike smoke or rubbish, leaves no visible trace, so it is easy for politicians and planners to give it a low priority.

        Nevertheless, a number of cities have shown that it is possible to reduce noise. Traffic is the main source in most places, and lowering speed limits from 50 to 30 kilometres per hour can reduce noise noticeably, as well as making streets safer. Special road surfaces that absorb sound, quieter tyres and the gradual replacement of petrol and diesel vehicles with electric ones can also help, although electric vehicles are only much quieter at low speeds, because at higher speeds most of the noise comes from the tyres rather than the engine. Some cities have also introduced noise cameras that identify vehicles with illegally loud exhausts.

        Planners are also rethinking how buildings are designed. Placing bedrooms on the quiet side of a building, away from the street, and fitting thicker windows can make a significant difference to residents' sleep. Trees and green walls have a smaller effect on noise levels than people often imagine, but they can make an area feel calmer, and studies suggest that natural sounds, such as birdsong or running water, make people less bothered by traffic noise.

        Perhaps the most important change is one of attitude. Many experts argue that cities should protect "quiet areas", such as parks and courtyards, in the same way that they protect historic buildings. In a noisy world, the chance to hear nothing but birds and the wind may turn out to be one of the most valuable things a city can offer.
        """,
        [
            new("mcq", R4Mcq, null,
            [
                Choice(21, "What does the writer say about the decibel scale?", "A", "\"An increase of ten decibels ... most listeners hear it as roughly twice as loud.\"",
                    "An increase of ten decibels sounds about twice as loud.", "It measures how loud people think a sound is.", "Normal conversation is almost as loud as heavy traffic.", "It was developed by the World Health Organization."),
                Choice(22, "Why is it difficult for researchers to measure the health effects of noise?", "C", "\"It is difficult to separate the effects of noise from those of air pollution, which often occurs in the same places.\"",
                    "Most people do not wake up when they hear noise.", "Heart disease takes many years to develop.", "People near busy roads usually breathe polluted air as well.", "People living near airports refuse to take part in studies."),
                Choice(23, "What does the example of the airport that closed show?", "B", "\"The difference among children near the old airport gradually disappeared ... the effect can be reversed.\"",
                    "Aircraft noise affects adults more than children.", "The effects of noise on children may not be permanent.", "Schools near airports have fewer pupils.", "Children quickly get used to aircraft noise."),
                Choice(24, "What does the writer say about electric vehicles?", "D", "\"Electric vehicles are only much quieter at low speeds, because at higher speeds most of the noise comes from the tyres.\"",
                    "They will soon solve the problem of traffic noise.", "They are noisier than petrol cars at high speeds.", "They are detected by noise cameras.", "Their advantage becomes smaller at higher speeds."),
            ]),
            new("tfng", R4Tfng, null,
            [
                Item(25, "Many people who live in cities no longer notice the noise around them.", "\"A background noise that many residents hardly notice any more.\"", "TRUE"),
                Item(26, "Sudden noises only affect sleep if they wake people up.", "\"Even when people do not wake up, sudden sounds ... disturb the deeper stages of sleep.\"", "FALSE"),
                Item(27, "Most people who move to noisy areas move away again within a year.", "The text says people adapt to noise but gives no information about moving away.", "NOT GIVEN"),
                Item(28, "Research shows that the body stops reacting to noise once people get used to it.", "\"Research suggests that the body continues to react, even when the mind has stopped paying attention.\"", "FALSE"),
                Item(29, "Natural sounds can make traffic noise seem less annoying.", "\"Natural sounds, such as birdsong or running water, make people less bothered by traffic noise.\"", "TRUE"),
            ]),
        ]);

    private static ReadingPassage Reading10Part5() => new("The long journey of the white stork",
        """
        For centuries, the arrival of white storks in spring has been celebrated across Europe and parts of Asia as a sign that winter is over. These large black-and-white birds, with their long red legs and red beaks, often build their enormous nests on rooftops, chimneys and electricity poles, and many communities regard them with great affection. Yet for much of the year, the storks are not there at all. Each autumn, they set off on one of the most remarkable journeys in the animal kingdom, travelling thousands of kilometres to spend the winter in Africa.

        The white stork is a heavy bird, usually weighing between two and a half and four and a half kilograms, and flapping its wings continuously over long distances would require a huge amount of energy. Instead, storks are experts at soaring. On sunny days, the ground heats the air above it, creating rising columns of warm air known as thermals. Storks circle upwards inside these thermals, sometimes to heights of more than a thousand metres, and then glide gently downwards towards the next one, often covering long distances with hardly a single beat of their wings.

        This dependence on thermals explains the routes the birds take. Thermals form over land but not over large areas of water, which heat up far more slowly. Storks therefore avoid crossing the Mediterranean Sea. Birds from western Europe fly south through Spain and cross into Africa at the Strait of Gibraltar, where the sea is only about fourteen kilometres wide. Those from central and eastern Europe take an eastern route over the Bosphorus in Turkey, then continue through the Middle East and down the Nile valley. At these narrow crossing points, tens of thousands of storks can sometimes be seen in a single day.

        Much of what we know about these journeys comes from tracking. For many years, scientists relied on fitting birds with metal rings on their legs, which could only provide information when a ringed bird was found again. Since the 1990s, however, small satellite and GPS transmitters attached to the birds' backs have allowed researchers to follow individual storks day by day. These studies have shown that storks can travel two hundred kilometres or more in a single day in good weather, and that some birds from eastern Europe fly as far as South Africa, a journey of around ten thousand kilometres in each direction.

        Tracking has also revealed some surprising behaviour. Young storks, making the journey for the first time, do not travel with their parents. Instead, they rely on an inherited sense of direction and on joining other storks they meet along the way. Researchers have also discovered that the migration of some populations is changing. Growing numbers of storks from western Europe now spend the winter in Spain and Portugal instead of crossing into Africa, feeding at rubbish dumps, where food is available all year round. Their journeys have become much shorter, and some birds no longer migrate at all.

        This change brings both benefits and risks. Birds that stay closer to their breeding grounds avoid the dangers of the long journey to Africa, such as exhaustion, hunting and collisions with power lines, and they can return early in spring to claim the best nesting sites. On the other hand, a diet of rubbish may contain plastic and other harmful materials, and scientists are concerned about what will happen when waste sites are closed, as many European countries are planning.

        The story of the white stork is, in many ways, a success. In the second half of the twentieth century, numbers fell sharply in several European countries because of the loss of wetlands and the use of pesticides. Protection programmes, the restoration of wetlands and the provision of nesting platforms have since helped the population to recover strongly in many areas. Few birds show so clearly how human activity can both threaten wildlife and help it to return.
        """,
        [
            new("gap", R5Gap, 2,
            [
                Item(30, "Storks save energy by circling upwards in rising columns of warm air called ___.", "\"Rising columns of warm air known as thermals.\"", "thermals"),
                Item(31, "Birds from western Europe cross into Africa at a point where the sea is only about ___ kilometres wide.", "\"The Strait of Gibraltar, where the sea is only about fourteen kilometres wide.\"", "fourteen", "14"),
                Item(32, "In the past, scientists studied migration by fitting metal ___ to the birds' legs.", "\"Scientists relied on fitting birds with metal rings on their legs.\"", "rings"),
                Item(33, "Many storks from western Europe now spend the winter in Spain and Portugal, feeding at ___.", "\"Feeding at rubbish dumps, where food is available all year round.\"", "rubbish dumps"),
            ]),
            new("mcq", R5Mcq, null,
            [
                Choice(34, "What does the text say about young storks on their first migration?", "A", "\"They rely on an inherited sense of direction and on joining other storks they meet along the way.\"",
                    "They partly rely on instinct to find their way.", "They follow their parents all the way to Africa.", "They spend their first winter in Europe.", "They always leave before the adult birds."),
                Choice(35, "What is the main point of the final paragraph?", "D", "\"Protection programmes ... have since helped the population to recover strongly.\"",
                    "Stork numbers are still falling across Europe.", "Pesticides are no longer used anywhere in Europe.", "Nesting platforms are the only reason for the recovery.", "Human action has helped storks to recover after a decline."),
            ]),
        ]);

    // =====================================================================
    // READING 11
    // =====================================================================

    private static ReadingPassage Reading11Part1() => new("We're moving house!",
        """
        Hi Aziza,

        I hope you're well and that your new job is going nicely! I'm writing with some big news: we're finally moving house. After two years of searching, we have bought a flat on the other side of the city, not far from the botanical garden. We move on Saturday 5 October.

        The new flat is on the fourth floor of a quiet building, and it has three bedrooms instead of two, so Lola and Jasur will each have their own (1) ______ at last. They have argued about sharing for years! The biggest bedroom, which will be ours, even has a small balcony.

        The best thing, though, is the kitchen. It is twice as big as our old one, with a window looking onto the garden, and there is enough space for a table where the whole family can eat together. I'm sure I'll spend most of my time in the (2) ______!

        The only problem is that the building has no lift, and carrying our furniture up four floors won't be easy. We've booked a removal company, but they can only send two men. Could you and Farrukh possibly come and help us on the day? We would really appreciate the extra (3) ______.

        The flat is also further from Lola's school, so she will have to take a bus every morning. Luckily, the (4) ______ stop is just outside our building, and the journey only takes fifteen minutes.

        Once everything is unpacked, we're planning a small party, probably on Saturday 19 October. It won't be anything special, just some food and music, but we would love to see you both at the (5) ______.

        Our new address is 14 Garden Street, flat 32. Our phone numbers will stay the same, but please remember to write down the new (6) ______ so that your birthday cards don't go to the old flat!

        Hope to see you soon.

        Love,
        Malika
        """,
        [
            new("gap", R1, 1,
            [
                Item(1, "The new flat ... has three bedrooms instead of two, so Lola and Jasur will each have their own ___ at last.", "\"The biggest bedroom, which will be ours, even has a small balcony.\"", "bedroom"),
                Item(2, "I'm sure I'll spend most of my time in the ___!", "\"The best thing, though, is the kitchen.\"", "kitchen"),
                Item(3, "Could you and Farrukh possibly come and help us on the day? We would really appreciate the extra ___.", "\"Could you and Farrukh possibly come and help us on the day?\"", "help"),
                Item(4, "Luckily, the ___ stop is just outside our building, and the journey only takes fifteen minutes.", "\"She will have to take a bus every morning.\"", "bus"),
                Item(5, "It won't be anything special, just some food and music, but we would love to see you both at the ___.", "\"We're planning a small party.\"", "party"),
                Item(6, "Our phone numbers will stay the same, but please remember to write down the new ___ so that your birthday cards don't go to the old flat!", "\"Our new address is 14 Garden Street, flat 32.\"", "address"),
            ]),
        ]);

    private static ReadingPassage Reading11Part2() => new("Board games for every table",
        """
        7. Word Tower. Players take turns to build a tower of wooden blocks, each with a letter on it, and must make a word every time they add a block. If the tower falls, the player who knocked it over loses a point. It is a favourite with English teachers, because players pick up new words from one another while they are laughing. Suitable for two to six players aged ten and over.

        8. Silk Road Merchants. In this strategy game, each player runs a trading company, buying and selling silk, spices and paper as caravans travel between cities. Prices change according to supply and demand, so careful planning is needed. The rules are not difficult, but be warned: a full game with four players usually lasts three to four hours, so it is best kept for a free weekend afternoon.

        9. Sketch & Guess. One player draws a secret word while the others try to guess it before the sand timer runs out. Players are divided into teams, and the game can be played by anything from four to twenty people, which makes it ideal for birthday parties, classrooms and family gatherings. No artistic talent is needed: the worse the drawings, the funnier the game usually becomes.

        10. The Lighthouse Mystery. A storm, a missing lighthouse keeper and a box full of clues: letters, maps, photographs and a diary. Your task is to work out what happened by reading each document carefully. The game was designed for a single player, although some people enjoy solving it with a partner. Everything is printed on paper, so no app or screen is needed. Once the mystery is solved, however, the game cannot really be played again.

        11. Forest Fire. In most games, players compete against each other, but in Forest Fire everybody is on the same side. Together, you are firefighters trying to stop a fire from spreading through a national park. Every decision must be discussed as a group, and if the fire reaches the village, everyone loses. It is an excellent choice for families whose games usually end in arguments.

        12. Pocket Chess Plus. This clever version of chess comes in a flat case no bigger than a phone. The pieces are magnetic, so they stay in place on a bumpy bus or plane, and you can close the case in the middle of a game and continue later. It also includes a small book of chess puzzles. It is perfect for long train rides and waiting rooms.

        13. Animal Pairs. Designed for children from the age of three, this memory game has forty large cards with bright pictures of animals. Players turn over two cards at a time and try to find matching pairs. Because there are no words anywhere on the cards, children can play it long before they start school. The game was tested with hundreds of families before it went on sale.

        14. Stones and Holes. This beautiful wooden board with two rows of small holes is a modern version of one of the oldest board games in the world, similar to games that have been played in Africa and Asia for many centuries. Players move stones around the board and try to capture as many of their opponent's stones as possible. The rules take two minutes to learn, but mastering the strategy can take years.
        """,
        [
            new("match", R2, null,
            [
                Item(7, "Text 7", "\"It is a favourite with English teachers, because players pick up new words from one another.\"", "H"),
                Item(8, "Text 8", "\"A full game with four players usually lasts three to four hours.\"", "D"),
                Item(9, "Text 9", "\"The game can be played by anything from four to twenty people.\"", "I"),
                Item(10, "Text 10", "\"The game was designed for a single player.\"", "C"),
                Item(11, "Text 11", "\"Everybody is on the same side ... if the fire reaches the village, everyone loses.\"", "B"),
                Item(12, "Text 12", "\"The pieces are magnetic, so they stay in place on a bumpy bus or plane ... perfect for long train rides.\"", "A"),
                Item(13, "Text 13", "\"Designed for children from the age of three ... children can play it long before they start school.\"", "F"),
                Item(14, "Text 14", "\"A modern version of one of the oldest board games in the world.\"", "G"),
            ],
            [
                "It is designed to be taken on journeys.",
                "Players win or lose together.",
                "You can play it on your own.",
                "A single game can take a long time.",
                "It needs a phone or tablet.",
                "Very young children can play it.",
                "It is based on a game with a long history.",
                "It is popular with language teachers.",
                "It works well with a large number of players.",
                "It was invented by a child.",
            ]),
        ]);

    private static ReadingPassage Reading11Part3() => new("A chess champion at fourteen",
        """
        15. When fourteen-year-old Madina Ergasheva sat down for the final round of the national youth championship last spring, few people expected her to win. She was the youngest player in her section and had been ranked only ninth before the tournament began. Six hours later, after a long and tense endgame, her opponent offered his hand in resignation, and Madina had become national champion. The victory earned her a place at the world youth championship later this year.

        16. Madina first learned chess at the age of five from her grandfather, a retired engineer who played every evening with his neighbours in the courtyard. He taught her how the pieces move on an old wooden board, and at first he let her win. By the time she was seven, he no longer needed to. "When she beat me properly for the first time, I was the happiest loser in the city," he laughs. It was he who took her to a chess club and persuaded a coach to look at her games.

        17. Today, Madina trains for about four hours a day, six days a week. Much of this time is spent studying the games of great players and solving puzzles, and she also uses computer programs to check her own games for mistakes. But her coach insists that physical fitness is just as important. Madina runs three times a week and swims at weekends. "A game can last five or six hours," he explains. "In the final hour, the player who is less tired usually makes fewer mistakes."

        18. Unlike some young players, Madina has not left ordinary school. Her teachers allow her to miss lessons when she travels to tournaments, and in return she catches up on the work while she is away, often doing homework on trains and in hotel rooms. Her favourite subject is biology. She admits that combining the two is not always easy. "Sometimes I come home from training and I still have maths homework," she says. "But school keeps me normal. My friends there do not care about my rating."

        19. Her path has not been free of disappointment. At twelve, she lost several important games in a row at a regional tournament and cried all the way home, telling her parents she would never play again. Her coach did not argue. Instead, a week later, he asked her to go through the lost games with him, move by move. "We found that I was not losing because I was weak," Madina says. "I was losing because I played too fast when I was nervous. That week taught me more than any victory."

        20. Madina's ambition is to become a grandmaster, the highest title in chess, but she is also thinking beyond the board. She would like to study medicine and says she does not want her whole life to depend on results. She is also keen to encourage more girls to take up the game, pointing out that at many tournaments boys still greatly outnumber girls. "Chess is not about strength," she says. "It is about patience and thinking clearly. Girls can do that as well as anyone."
        """,
        [
            new("match", R3, null,
            [
                Item(15, "Paragraph 15", "\"Few people expected her to win ... ranked only ninth ... Madina had become national champion.\"", "C"),
                Item(16, "Paragraph 16", "\"Madina first learned chess at the age of five from her grandfather.\"", "G"),
                Item(17, "Paragraph 17", "\"Her coach insists that physical fitness is just as important.\"", "H"),
                Item(18, "Paragraph 18", "\"Madina has not left ordinary school ... 'school keeps me normal.'\"", "E"),
                Item(19, "Paragraph 19", "\"She lost several important games in a row ... That week taught me more than any victory.\"", "B"),
                Item(20, "Paragraph 20", "\"Her ambition is to become a grandmaster ... She would like to study medicine.\"", "A"),
            ],
            [
                "Hopes on and off the board",
                "A painful but useful lesson",
                "A surprise champion",
                "Why computers are replacing coaches",
                "School as well as chess",
                "Travelling the world for tournaments",
                "A grandfather's gift",
                "Training for body and mind",
            ]),
        ]);

    private static ReadingPassage Reading11Part4() => new("Is it ever too late to learn a language?",
        """
        "You should have started when you were a child." Many adults who decide to learn a new language hear some version of this advice, and many believe it themselves. The idea that children are naturally better language learners is so widespread that it is often treated as a simple fact. The research, however, tells a more complicated and, for adult learners, more encouraging story.

        It is true that age affects some aspects of language learning. The strongest evidence concerns pronunciation. People who begin learning a language in early childhood and grow up surrounded by it usually end up sounding like native speakers, while those who start as adults rarely do, however many years they spend in the country. This has led some researchers to propose a "critical period", a window in childhood during which the brain is especially sensitive to the sounds of language. One large online study involving hundreds of thousands of participants suggested that the ability to learn grammar to a native-like level remains strong until around the age of seventeen or eighteen and then gradually declines.

        But native-like perfection is not the goal of most adult learners, and in other respects adults have important advantages. Studies comparing children and adults over the first months of learning have often found that adults actually progress faster. They can understand grammar rules when they are explained, use strategies such as keeping vocabulary notebooks, and draw on their knowledge of their first language and of the world. A young child may have an advantage in the long run, but an adult who studies seriously for a year will usually be able to do far more with a language than a child who has studied for the same period.

        The idea that children learn "effortlessly" is also misleading. A young child who moves to a new country typically spends many hours every day hearing and using the language, at school, in the playground and with friends, and is rarely embarrassed about making mistakes. Few adults have that kind of exposure. They learn in a weekly class, or for half an hour a day with an app, while working and running a household. When researchers take the amount of practice into account, much of the difference between children and adults becomes smaller.

        Motivation and attitude also matter enormously. Adults who learn a language for a clear purpose, such as communicating with a partner's family, working abroad or reading literature in the original, often continue through difficulties that would discourage others. On the other hand, adults are often more anxious about speaking and more afraid of looking foolish, and this anxiety can prevent them from getting the practice they need. Teachers who work with adult learners frequently say that helping students relax is as important as teaching them grammar.

        There may even be benefits beyond the language itself. A growing number of studies have looked at whether speaking more than one language affects the brain as we get older. Some have suggested that bilingual people tend to show the symptoms of certain memory-related illnesses several years later than people who speak only one language, although other studies have not found this effect, and the question remains under debate. What is less controversial is that learning a language is a demanding mental activity that involves memory, attention and social interaction, all of which are generally considered good for the ageing brain.

        So what should adults do if they want to succeed? The research points to a few practical conclusions. Regular practice matters more than long, occasional sessions. Listening to and speaking with real people, even imperfectly, is essential. Accepting a foreign accent, rather than trying to get rid of it, frees learners to concentrate on communicating clearly. Above all, it helps to remember that the question "Am I too old?" is usually less important than the question "How much time am I willing to give it?"
        """,
        [
            new("mcq", R4Mcq, null,
            [
                Choice(21, "What does the writer say about pronunciation?", "B", "\"The strongest evidence concerns pronunciation.\"",
                    "Adults can gain a native accent if they live abroad long enough.", "It is the area where the effect of age is clearest.", "Children learn it more slowly than grammar.", "It is the most important skill for adult learners."),
                Choice(22, "What did the large online study suggest?", "A", "\"The ability to learn grammar to a native-like level remains strong until around the age of seventeen or eighteen and then gradually declines.\"",
                    "The ability to learn grammar starts to fall in the late teens.", "Adults learn grammar better than teenagers.", "Children under ten learn grammar most easily.", "Grammar is harder to learn than pronunciation."),
                Choice(23, "According to the fourth paragraph, why do children seem to learn languages more easily?", "D", "\"A young child ... typically spends many hours every day hearing and using the language ... Few adults have that kind of exposure.\"",
                    "Their brains are better at remembering words.", "They are taught by better teachers at school.", "They learn grammar rules more quickly.", "They usually get much more practice than adults."),
                Choice(24, "What does the writer say about research on bilingualism and the ageing brain?", "C", "\"Other studies have not found this effect, and the question remains under debate.\"",
                    "It has proved that bilingual people never develop memory problems.", "It shows that only children benefit from being bilingual.", "Its results do not all agree.", "It has been ignored by most scientists."),
            ]),
            new("tfng", R4Tfng, null,
            [
                Item(25, "Many adults believe that children are better at learning languages.", "\"Many adults ... hear some version of this advice, and many believe it themselves.\"", "TRUE"),
                Item(26, "In the first months of study, children usually make faster progress than adults.", "\"Studies ... over the first months of learning have often found that adults actually progress faster.\"", "FALSE"),
                Item(27, "Adults who learn with an app make more progress than those who attend classes.", "Apps and weekly classes are both mentioned, but they are not compared.", "NOT GIVEN"),
                Item(28, "Teachers of adults believe that reducing learners' anxiety is important.", "\"Helping students relax is as important as teaching them grammar.\"", "TRUE"),
                Item(29, "The writer advises adult learners to try hard to lose their foreign accent.", "\"Accepting a foreign accent, rather than trying to get rid of it, frees learners to concentrate on communicating.\"", "FALSE"),
            ]),
        ]);

    private static ReadingPassage Reading11Part5() => new("How earthquakes are measured",
        """
        Every year, instruments around the world record hundreds of thousands of earthquakes. The vast majority are too small for anyone to feel, but a few are powerful enough to destroy entire cities. Comparing these events, and quickly understanding how dangerous a new one might be, depends on the ability to measure them accurately. This has proved to be a more difficult task than it might first appear.

        The basic instrument for recording earthquakes is the seismometer. Early designs relied on a heavy weight hanging from a frame: when the ground shook, the frame moved with it, but the weight, because of its inertia, tended to stay still, and a pen attached to it drew a line on a rotating drum of paper. Modern seismometers work on the same principle but record the movement electronically, and they are so sensitive that they can detect a large earthquake on the other side of the planet. The record they produce is called a seismogram.

        An earthquake sends out several kinds of waves. The fastest, called primary waves or P-waves, travel through the Earth by squeezing and stretching the rock, much as sound travels through air. They are followed by slower secondary waves, or S-waves, which shake the rock from side to side. Because the two types travel at different speeds, the time gap between their arrival at a recording station shows how far away the earthquake was: the longer the gap, the greater the distance. By combining information from at least three stations, scientists can calculate the location of the earthquake, a method known as triangulation.

        The first widely used way of describing the size of an earthquake was developed in 1935 by the American seismologist Charles Richter. His scale was based on the largest movement recorded on a particular type of seismometer, adjusted for the distance to the earthquake. Richter's scale is logarithmic: each step of one whole number represents a tenfold increase in the size of the ground movement recorded, and roughly thirty-two times more energy released. An earthquake of magnitude 6 therefore releases about a thousand times more energy than one of magnitude 4.

        Richter's method worked well for the moderate earthquakes in southern California for which it was designed, but it had limitations. It was less reliable at great distances, and for very large earthquakes it tended to give figures that were too low, because the instruments of the time could not properly record the longest waves. For this reason, in the late 1970s seismologists introduced the moment magnitude scale, which is now the standard for large earthquakes. Instead of relying on the height of waves on a seismogram, it is based on the physical size of the event: the area of the fault that moved, the distance the rock slipped and the strength of the rock. The largest earthquake ever recorded, in Chile in 1960, had a moment magnitude of 9.5.

        Magnitude, however, tells only part of the story. A second way of describing earthquakes, called intensity, measures their effects at a particular place: how strongly people felt the shaking and how much damage was caused. The most commonly used intensity scale, the Modified Mercalli scale, uses Roman numerals from I, which means the shaking is not felt, to XII, which means almost total destruction. While an earthquake has only one magnitude, its intensity varies from place to place, depending on the distance from the source, the type of ground and the quality of buildings.

        This difference explains why two earthquakes of similar magnitude can have very different consequences. A powerful earthquake deep underground in a remote area may cause little damage, while a smaller, shallow one beneath a city built on soft soil can be devastating. For emergency services, therefore, rapid estimates of intensity are often more useful than magnitude alone, and many countries now publish maps of expected shaking within minutes of an earthquake.
        """,
        [
            new("gap", R5Gap, 2,
            [
                Item(30, "The record of ground movement produced by a seismometer is called a ___.", "\"The record they produce is called a seismogram.\"", "seismogram"),
                Item(31, "The distance to an earthquake can be calculated from the ___ between the arrival of P-waves and S-waves.", "\"The time gap between their arrival at a recording station shows how far away the earthquake was.\"", "time gap"),
                Item(32, "Using information from at least three stations to find where an earthquake happened is known as ___.", "\"A method known as triangulation.\"", "triangulation"),
                Item(33, "The moment magnitude scale is based on the area of the fault, the distance the rock slipped and the ___ of the rock.", "\"The area of the fault that moved, the distance the rock slipped and the strength of the rock.\"", "strength"),
            ]),
            new("mcq", R5Mcq, null,
            [
                Choice(34, "Why was the moment magnitude scale introduced?", "C", "\"For very large earthquakes it tended to give figures that were too low.\"",
                    "Richter's scale was too complicated to use.", "Richter's scale could only be used in California.", "Richter's scale underestimated the size of very large earthquakes.", "Scientists wanted a scale based on Roman numerals."),
                Choice(35, "According to the text, what does intensity measure?", "A", "\"Intensity measures their effects at a particular place.\"",
                    "the effects of an earthquake at a particular place", "the total energy released at the source", "the depth at which an earthquake begins", "the length of the fault that moved"),
            ]),
        ]);
}
