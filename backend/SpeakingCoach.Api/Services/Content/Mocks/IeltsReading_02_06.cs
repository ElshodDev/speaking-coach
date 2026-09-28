using SpeakingCoach.Api.Services.Mock;

namespace SpeakingCoach.Api.Services.Content;

// Tayyor IELTS Academic Reading testlari 2–6. Original matn va savollar (haqiqiy imtihondan
// ko'chirilmagan), tuzilishi ReadingAcademic1 bilan bir xil: 3 matn (13 + 13 + 14 savol).
public static partial class BuiltInIelts
{
    public static readonly ReadingTest ReadingAcademic2 = new(IeltsBank.Academic, [Ra02P1(), Ra02P2(), Ra02P3()]);
    public static readonly ReadingTest ReadingAcademic3 = new(IeltsBank.Academic, [Ra03P1(), Ra03P2(), Ra03P3()]);
    public static readonly ReadingTest ReadingAcademic4 = new(IeltsBank.Academic, [Ra04P1(), Ra04P2(), Ra04P3()]);
    public static readonly ReadingTest ReadingAcademic5 = new(IeltsBank.Academic, [Ra05P1(), Ra05P2(), Ra05P3()]);
    public static readonly ReadingTest ReadingAcademic6 = new(IeltsBank.Academic, [Ra06P1(), Ra06P2(), Ra06P3()]);

    private static string Ra26Tfng(int passage) =>
        $"Do the following statements agree with the information given in Reading Passage {passage}?\n" +
        "TRUE — if the statement agrees with the information\nFALSE — if the statement contradicts the information\nNOT GIVEN — if there is no information on this";

    private const string Ra26Notes1 = "Complete the notes below.\nChoose ONE WORD ONLY from the passage for each answer.";
    private const string Ra26Notes2 = "Complete the notes below.\nChoose NO MORE THAN TWO WORDS from the passage for each answer.";
    private const string Ra26Sent1 = "Complete the sentences below.\nChoose ONE WORD ONLY from the passage for each answer.";
    private const string Ra26Sent2 = "Complete the sentences below.\nChoose NO MORE THAN TWO WORDS from the passage for each answer.";

    // =====================================================================
    // TEST 2
    // =====================================================================

    private static ReadingPassage Ra02P1() => new("A Promise on Paper",
        """
        Few objects are as ordinary, or as strange, as a banknote. A piece of printed paper or plastic has almost no value in itself, yet people everywhere accept it in exchange for food, clothes and houses. The story of how this came about stretches back more than a thousand years, and it begins not in the banks of Europe but in the markets of China.

        Under the Tang dynasty, in the ninth century, merchants who travelled long distances to trade faced a practical problem. The copper coins of the time were heavy, and carrying large quantities of them along the roads was both tiring and dangerous. A system therefore developed in which a merchant could deposit coins in one city and receive a certificate that could be exchanged for coins in another. Because the paper seemed to travel without effort, these certificates became known as 'flying money'. They were not true currency, however, since they could not be spent in ordinary shops; they were closer to what we would now call a money order.

        The first genuine paper money appeared about two centuries later in the province of Sichuan, during the Song dynasty. Sichuan used coins made of iron rather than copper, and iron coins were worth so little that buying even a modest quantity of silk could require a load of coins too heavy for one person to carry. Merchants in Chengdu, the main city of the province, began to issue paper receipts in exchange for deposits of iron coins, and these receipts, called jiaozi, were soon passed from hand to hand as payment. When some merchants issued more receipts than they could honour, disputes followed, and in the early eleventh century the government took control of the system, creating the world's first paper money issued by a state.

        Later Chinese rulers expanded the use of paper currency. When the Venetian traveller Marco Polo visited the court of Kublai Khan in the late thirteenth century, he was astonished to find that money was made from the inner bark of the mulberry tree, stamped with the seal of the ruler, and that anyone who refused to accept it could be punished. Yet the history of paper money in China also offered an early warning. Because printing notes was so much easier than mining metal, governments were tempted to print more whenever they needed to pay for wars or great building projects. The result was inflation: prices rose, the notes lost their value and public trust collapsed. By the middle of the fifteenth century, the Ming government had largely given up on paper money, and silver became the main form of currency.

        In Europe, paper money arrived much later. In seventeenth-century London, wealthy people often left their gold with goldsmiths for safekeeping and received a written receipt in return. Over time these receipts began to be used for payments themselves, since it was far easier to hand over a receipt than to collect the gold. The first European banknotes in the modern sense, however, were issued in Sweden in 1661 by the Stockholm Banco. Sweden's currency at the time consisted largely of copper plates, some of which weighed more than ten kilograms. Like the merchants of Sichuan, the bank soon issued more notes than it could support with metal, and it collapsed within a few years.

        The Bank of England, founded in 1694, issued notes that were partly handwritten, with the amount and the name of the person being paid filled in by a clerk. For most of the following two centuries, a banknote was essentially a promise: the holder could take it to the bank and exchange it for a fixed amount of gold or silver. This arrangement, known as the gold standard, gave people confidence that paper was 'as good as gold'. But it also limited how much money a government could create, and during the economic crises of the twentieth century country after country abandoned it. Britain left the gold standard in 1931, and in 1971 the United States ended the right of foreign governments to exchange dollars for gold.

        Since then, almost all the world's money has been what economists call fiat money: it has value not because it can be exchanged for metal, but because governments declare it to be legal tender and people trust that others will accept it. This trust depends heavily on preventing forgery. Modern notes include watermarks, metallic threads and holograms that are difficult to copy, and in 1988 Australia became the first country to issue a banknote printed on plastic polymer rather than paper. Polymer notes last several times longer than paper ones and are harder to counterfeit, and many countries have since followed Australia's example.

        Today, as more and more payments are made by card or phone, some observers predict that banknotes will disappear altogether. Whether or not they do, the basic idea behind them, that a promise written on paper can be trusted, has shaped economic life for more than a thousand years.
        """,
        [
            new("tfng", TfngInstructions, null,
            [
                Stmt(1, "'Flying money' could be used to buy goods in ordinary shops.", "FALSE",
                    "\"They were not true currency, however, since they could not be spent in ordinary shops.\""),
                Stmt(2, "The iron coins used in Sichuan had a low value.", "TRUE",
                    "\"Iron coins were worth so little that buying even a modest quantity of silk could require a load of coins too heavy for one person to carry.\""),
                Stmt(3, "The jiaozi were first issued by the government of the Song dynasty.", "FALSE",
                    "They were first issued by merchants in Chengdu; the government only \"took control of the system\" later, in the early eleventh century."),
                Stmt(4, "Marco Polo brought some Chinese banknotes back with him to Venice.", "NOT GIVEN",
                    "The passage describes what Marco Polo saw at Kublai Khan's court but says nothing about him taking notes home."),
                Stmt(5, "Chinese governments sometimes printed extra money to pay for wars.", "TRUE",
                    "\"Governments were tempted to print more whenever they needed to pay for wars or great building projects.\""),
                Stmt(6, "London goldsmiths charged their customers a fee for storing gold.", "NOT GIVEN",
                    "The passage says people left gold with goldsmiths and received receipts, but it does not mention any fee."),
                Stmt(7, "The Stockholm Banco remained in business for several decades.", "FALSE",
                    "\"The bank soon issued more notes than it could support with metal, and it collapsed within a few years.\""),
            ]),
            new("gap", Ra26Notes2, 2,
            [
                Gap(8, "Paper money seen by Marco Polo: made from the inner bark of the ___ tree", ["mulberry"],
                    "\"Money was made from the inner bark of the mulberry tree.\""),
                Gap(9, "Mid-15th century: the Ming government turned to ___ as the main currency", ["silver"],
                    "\"The Ming government had largely given up on paper money, and silver became the main form of currency.\""),
                Gap(10, "Sweden, 1661: currency consisted largely of heavy copper ___", ["plates"],
                    "\"Sweden's currency at the time consisted largely of copper plates, some of which weighed more than ten kilograms.\""),
                Gap(11, "Early Bank of England notes: amount and name ___ by a clerk", ["filled in"],
                    "\"With the amount and the name of the person being paid filled in by a clerk.\""),
                Gap(12, "Since 1971: most of the world's money is ___ money", ["fiat"],
                    "\"Almost all the world's money has been what economists call fiat money.\""),
                Gap(13, "1988: Australia issues the first banknote printed on ___ instead of paper", ["polymer", "plastic polymer", "plastic"],
                    "\"Australia became the first country to issue a banknote printed on plastic polymer rather than paper.\""),
            ]),
        ]);

    private static ReadingPassage Ra02P2() => new("Farming the City",
        """
        In many cities, food is now grown in places that would have seemed unlikely only a generation ago: on the roofs of supermarkets, in empty warehouses, along railway embankments and even in disused underground tunnels. Supporters of urban farming see it as a way to make cities greener, healthier and less dependent on distant farms. Critics reply that it is a charming hobby which can never feed more than a tiny fraction of a city's population. The truth, as is often the case, lies somewhere between the two.

        Growing food in cities is not a new idea. Before refrigeration and fast transport, most towns were surrounded by market gardens that supplied fresh vegetables, and many households kept chickens or a pig in the back yard. During the Second World War, governments in Britain and the United States encouraged citizens to turn lawns and parks into vegetable plots, and at their peak these 'victory gardens' produced a significant share of the fresh vegetables eaten in both countries. After the war, as cheap food arrived from large, mechanised farms, most of these plots disappeared.

        Modern urban farming takes several forms. The simplest is the community garden, where residents share a plot of land and grow food for their own use. More ambitious are rooftop farms, which may be run as businesses selling produce to local restaurants and shops. At the high-technology end are so-called vertical farms: indoor buildings in which plants are grown in stacked layers under artificial light, with their roots in water containing nutrients rather than in soil. This method, known as hydroponics, allows growers to control temperature, humidity and light precisely, so crops can be harvested all year round and pests are rarely a problem.

        The advantages claimed for vertical farms are impressive. Because the plants are grown indoors and the water is recycled, they can use a small fraction of the water needed by crops in fields. They require no pesticides, and since they can be built close to the people who will eat the food, transport distances are short and the produce can be sold within hours of being picked. Leafy vegetables such as lettuce, together with herbs like basil, grow especially well in these conditions.

        The main difficulty is energy. In a field, sunlight is free; in a vertical farm, every hour of light must be provided by electric lamps. Although modern LED lights are far more efficient than older types, electricity remains the largest cost for most operators, and several well-funded companies have gone out of business in recent years after energy prices rose. For the same reason, vertical farms have so far concentrated on crops that grow quickly and sell for a high price. Staple crops such as wheat and rice, which provide most of the world's calories, need so much light and space that growing them indoors would be hopelessly expensive.

        Outdoor urban farms face different problems. Land in cities is expensive, and a plot used for vegetables may be worth far more to its owner as a site for housing or offices. Many community gardens therefore exist on temporary agreements and can be closed at short notice. Soil quality is another concern. Land that was once used for industry, or that lies close to busy roads, may contain lead and other harmful substances, so growers are often advised to test the soil before planting, or to grow food in raised beds filled with clean soil brought in from elsewhere.

        Given these limits, what can urban farming realistically achieve? Most researchers agree that cities will continue to depend on the countryside for the bulk of their food, particularly grains and other staples. Yet measuring urban farms only by the number of calories they produce may miss much of their value. Community gardens bring together neighbours who might otherwise never meet, and studies have linked gardening to lower levels of stress. School gardens teach children where food comes from, and some teachers report that pupils who grow vegetables themselves become more willing to eat them. Roofs used for farming can also help to keep buildings cool in summer and reduce the amount of rainwater flowing into drains during storms.

        Perhaps the most promising future lies in combining approaches. Some planners now include space for growing food in new housing developments, while supermarkets have experimented with small hydroponic units inside their stores, where herbs are grown and sold on the same spot. Urban farming will not replace traditional agriculture, but it may once again become a normal part of city life, as it was for most of human history.
        """,
        [
            new("mcq", Abcd, null,
            [
                Mcq(14, "What is the writer's view of urban farming in the first paragraph?",
                    ["It will soon replace traditional farming.", "Its critics have been proved right.", "It is mainly found in underground tunnels.", "Its real value lies between the claims of supporters and critics."], "D",
                    "\"The truth, as is often the case, lies somewhere between the two.\""),
                Mcq(15, "What happened to most victory gardens after the Second World War?",
                    ["They were turned into market gardens.", "They were taken over by large farms.", "They disappeared as cheap food became available.", "They continued to supply most fresh vegetables."], "C",
                    "\"After the war, as cheap food arrived from large, mechanised farms, most of these plots disappeared.\""),
                Mcq(16, "According to the passage, hydroponics allows growers to",
                    ["grow plants in rich soil.", "use natural sunlight more efficiently.", "grow staple crops cheaply.", "control growing conditions precisely."], "D",
                    "\"This method, known as hydroponics, allows growers to control temperature, humidity and light precisely.\""),
                Mcq(17, "What is the largest cost for most vertical farm operators?",
                    ["land", "electricity", "water", "staff"], "B",
                    "\"Electricity remains the largest cost for most operators.\""),
            ]),
            new("tfng", Ra26Tfng(2), null,
            [
                Stmt(18, "Vertical farms use much less water than crops grown in fields.", "TRUE",
                    "\"They can use a small fraction of the water needed by crops in fields.\""),
                Stmt(19, "LED lights have become cheaper to buy in recent years.", "NOT GIVEN",
                    "The passage says LED lights are \"far more efficient than older types\", but says nothing about their price."),
                Stmt(20, "Wheat could be grown cheaply in vertical farms.", "FALSE",
                    "\"Staple crops such as wheat and rice ... need so much light and space that growing them indoors would be hopelessly expensive.\""),
                Stmt(21, "Most community gardens are owned by local councils.", "NOT GIVEN",
                    "The passage says many community gardens exist on temporary agreements, but not who owns the land."),
                Stmt(22, "Gardening has been associated with reduced stress.", "TRUE",
                    "\"Studies have linked gardening to lower levels of stress.\""),
            ]),
            new("gap", Ra26Sent2, 2,
            [
                Gap(23, "Instead of testing the soil, growers may plant food in ___ filled with clean soil.", ["raised beds"],
                    "\"Or to grow food in raised beds filled with clean soil brought in from elsewhere.\""),
                Gap(24, "Some teachers say that pupils who grow vegetables become more willing to ___ them.", ["eat"],
                    "\"Pupils who grow vegetables themselves become more willing to eat them.\""),
                Gap(25, "Farming on roofs can reduce the amount of ___ that flows into drains during storms.", ["rainwater"],
                    "\"Reduce the amount of rainwater flowing into drains during storms.\""),
                Gap(26, "Some supermarkets have tried growing herbs in small ___ units inside their stores.", ["hydroponic"],
                    "\"Supermarkets have experimented with small hydroponic units inside their stores.\""),
            ]),
        ]);

    private static ReadingPassage Ra02P3() => new("Thinking Fast When It Matters",
        """
        We tend to imagine good decisions as the product of calm reflection. The ideal decision-maker, in this picture, sits at a desk, lists every option, weighs the advantages and disadvantages of each and finally chooses the best. Pressure, whether it comes from lack of time, high stakes or an audience, is seen as the enemy of this process, something that makes people panic and act foolishly. I want to suggest that this picture, while not entirely false, gives a misleading account of how people actually decide when it matters most, and that it has led to some unhelpful advice.

        Consider the research of the psychologist Gary Klein, who in the 1980s set out to study how fire commanders make decisions at the scene of a fire. He expected to find them comparing a small number of options quickly. Instead, many of the commanders insisted that they did not make decisions at all; they simply knew what to do. On closer examination, Klein concluded that experienced commanders recognised the situation in front of them as similar to ones they had met before, and this recognition immediately suggested a suitable course of action. Rather than comparing options, they imagined how the first plan that came to mind would work out, and only if they spotted a problem did they consider another.

        This finding matters because it reverses the usual relationship between thinking and speed. For an expert, the quick response is not a careless shortcut but the product of years of accumulated experience. Forcing such a person to write out and compare every option would not improve the decision; it would simply slow it down, and in a burning building delay can be fatal. I would go further: in many fields, the ability to act well without extended deliberation is exactly what we mean by expertise.

        None of this means that pressure is harmless. Psychologists have long observed that performance tends to improve as arousal rises, but only up to a point, after which it falls sharply. A moderate level of stress sharpens attention; a high level narrows it so much that people fail to notice important information. Research on sport has identified another danger, commonly called choking. When skilled performers become anxious, they sometimes start to pay conscious attention to movements that normally happen automatically, such as the swing of a golf club, and the result is a clumsy performance of something they have done perfectly thousands of times.

        What, then, separates those who cope well with pressure from those who do not? The evidence suggests that the key factor is not personality but preparation. Pilots, surgeons and emergency workers practise in simulators and exercises designed to reproduce the stress of real incidents, so that when a genuine crisis arrives, it feels familiar rather than overwhelming. I find this far more convincing than the popular notion that some people are simply born to stay calm. Composure under pressure is, in my view, largely a skill, and like other skills it can be learnt.

        This has implications for how organisations treat people who are new to their jobs. Beginners have no store of experience to draw on, so the rapid, recognition-based decision-making that serves experts so well is not available to them. For a novice, a checklist or a clear written procedure is not an insult but a support. It is a mistake, therefore, to take the example of experts and conclude that everyone should simply trust their instincts. Instincts are only as good as the experience behind them, and a newcomer's instincts may be little better than guesses.

        Equally, experts are not immune to error. Recognition works well when the present situation really does resemble past ones, but it can be dangerous when something new is happening that only looks familiar. The most skilled decision-makers seem to combine rapid recognition with a habit of asking, even briefly, what might be different this time. Some aviation and medical teams now encourage junior members to speak up when they notice something unusual, precisely because a person who is less attached to the familiar pattern may be quicker to see that it does not fit.

        The lesson, I believe, is not that we should try to remove pressure from important decisions, which is usually impossible, nor that we should always slow down. It is that we should invest in the kind of experience that makes good decisions possible when time is short: realistic training, honest reviews of what went wrong and what went right, and working cultures in which people feel able to question a plan. Calm reflection has its place, but the decisions that matter most are often made in seconds, and it is in the years beforehand that they are really made well.
        """,
        [
            new("ynng", YnngInstructions, null,
            [
                Stmt(27, "The traditional picture of careful decision-making is completely wrong.", "NO",
                    "\"This picture, while not entirely false, gives a misleading account.\""),
                Stmt(28, "Klein's research methods were criticised by other psychologists.", "NOT GIVEN",
                    "The writer describes Klein's research and conclusions but says nothing about how other psychologists judged his methods."),
                Stmt(29, "Being able to act well without long deliberation is an important part of expertise.", "YES",
                    "\"The ability to act well without extended deliberation is exactly what we mean by expertise.\""),
                Stmt(30, "The ability to stay calm under pressure is mainly something people are born with.", "NO",
                    "\"I find this far more convincing than the popular notion that some people are simply born to stay calm. Composure under pressure is ... largely a skill.\""),
                Stmt(31, "People who are new to a job benefit from following clear written procedures.", "YES",
                    "\"For a novice, a checklist or a clear written procedure is not an insult but a support.\""),
                Stmt(32, "Hospitals use checklists less often than airlines do.", "NOT GIVEN",
                    "The writer mentions aviation and medical teams encouraging junior members to speak up, but does not compare how often they use checklists."),
            ]),
            new("mcq", Abcd, null,
            [
                Mcq(33, "What did Klein expect to find when he began studying fire commanders?",
                    ["They relied mainly on written procedures.", "They made decisions without thinking.", "They compared a few options quickly.", "They asked junior members for advice."], "C",
                    "\"He expected to find them comparing a small number of options quickly.\""),
                Mcq(34, "According to the passage, choking happens when skilled performers",
                    ["consciously focus on movements that are usually automatic.", "stop paying attention to what they are doing.", "practise too little before an important event.", "try a technique they have never used before."], "A",
                    "\"They sometimes start to pay conscious attention to movements that normally happen automatically.\""),
                Mcq(35, "Why do some teams encourage junior members to speak up?",
                    ["Someone less attached to a familiar pattern may notice when it does not fit.", "Junior members usually have more up-to-date training.", "Experts are too busy to notice every detail.", "It helps junior members to build confidence."], "A",
                    "\"A person who is less attached to the familiar pattern may be quicker to see that it does not fit.\""),
                Mcq(36, "What is the writer's main purpose in the passage?",
                    ["to show that pressure always leads to poor decisions", "to describe the history of research into firefighting", "to argue that good decisions under pressure depend on experience and preparation", "to recommend that all decisions should be made slowly"], "C",
                    "\"We should invest in the kind of experience that makes good decisions possible when time is short.\""),
            ]),
            new("gap", Ra26Sent1, 1,
            [
                Gap(37, "Performance tends to improve as ___ rises, but only up to a point.", ["arousal"],
                    "\"Performance tends to improve as arousal rises, but only up to a point.\""),
                Gap(38, "A high level of stress ___ attention so much that important information is missed.", ["narrows"],
                    "\"A high level narrows it so much that people fail to notice important information.\""),
                Gap(39, "Pilots and surgeons practise in ___ that reproduce the stress of real incidents.", ["simulators"],
                    "\"Pilots, surgeons and emergency workers practise in simulators and exercises designed to reproduce the stress of real incidents.\""),
                Gap(40, "The writer recommends realistic training, honest ___ and cultures in which people can question a plan.", ["reviews"],
                    "\"Realistic training, honest reviews of what went wrong and what went right, and working cultures in which people feel able to question a plan.\""),
            ]),
        ]);

    // =====================================================================
    // TEST 3
    // =====================================================================

    private static ReadingPassage Ra03P1() => new("Archives in the Ice",
        """
        Every winter, snow falls on the great ice sheets of Greenland and Antarctica, and in the coldest places much of it never melts. Year after year, new layers bury the old ones, and under the growing weight the snow is slowly pressed into a grainy material called firn and eventually into solid ice. The process can take decades or even centuries, but the result is extraordinary: a frozen record of the atmosphere stretching back hundreds of thousands of years. Reading that record has become one of the most important tasks in the study of the Earth's climate.

        Scientists obtain the record by drilling ice cores, long cylinders of ice roughly ten centimetres across. A drill hanging from a cable cuts a few metres at a time, and each section is lifted to the surface, labelled and packed in insulated boxes. The work is slow and physically demanding. Drilling camps operate only in the brief polar summer, and a single deep core, reaching bedrock more than three kilometres below the surface, may take several seasons to complete. Once collected, the cores must be kept frozen at every stage of their journey to the laboratory, since even partial melting would destroy much of the information they contain.

        One of the most valuable kinds of information is trapped in tiny bubbles. As firn turns into ice, the spaces between the grains close up, sealing in small quantities of air. These bubbles are, in effect, samples of the ancient atmosphere, and by releasing and analysing the gas, scientists can measure how much carbon dioxide and methane the air contained at the time. Because the bubbles are sealed only when the firn becomes ice, the air inside is always somewhat younger than the ice around it, a difference that researchers must correct for.

        The ice itself holds evidence of past temperatures. Water molecules contain oxygen atoms of slightly different weights, known as isotopes. As moist air travels towards the poles, water containing the heavier form of oxygen tends to fall out as rain or snow first, and this effect is stronger when the climate is cold. So snow that fell during cold periods contains a lower proportion of heavy oxygen than snow from warm periods. By measuring this proportion along the length of a core, scientists can reconstruct how temperatures changed over time.

        Other clues are scattered through the ice. Layers of volcanic ash and sulphur-rich acid mark major eruptions, and because some of these can be matched to eruptions whose dates are known from historical documents, they help researchers to date the ice. Dust blown from distant deserts tends to be more plentiful during cold, dry periods, when winds were stronger and less of the land was covered by plants. Even human history appears: cores from Greenland contain traces of lead released by metalworking in Europe about two thousand years ago, when the Roman Empire was producing large quantities of silver.

        In places where snow falls heavily, such as parts of Greenland, individual years can be seen as separate layers, much like the rings of a tree, and can be counted one by one. In central Antarctica, where snowfall is so light that the region is technically a desert, the layers are too thin to count, and ages must instead be estimated using models of how the ice flows. It is in these dry regions, however, that the oldest ice is found. A core drilled at a site called Dome C in eastern Antarctica, completed in the early years of this century, contains ice around 800,000 years old.

        The results have transformed our understanding of the climate. The Antarctic cores show that during this long period the Earth swung repeatedly between cold ice ages and shorter warm periods, in a rhythm of roughly 100,000 years. Throughout these cycles, carbon dioxide and temperature rose and fell closely together. The cores also show that the concentration of carbon dioxide in today's atmosphere is far higher than at any point in the entire record.

        Ice cores are not only collected at the poles. Glaciers high in mountain ranges, including some in the tropics, contain records of regional climate, such as changes in the rainfall that feeds the great seasonal monsoons. Many of these glaciers are now shrinking rapidly, and as their surfaces melt, water seeps downwards and blurs the layers beneath. For this reason, an international project has begun collecting cores from threatened mountain glaciers in order to store them in a specially built snow cave in Antarctica, so that scientists in the future, perhaps using techniques not yet invented, will still be able to study them.
        """,
        [
            new("gap", Ra26Notes1, 1,
            [
                Gap(1, "Formation of ice: snow is pressed first into ___ and then into solid ice", ["firn"],
                    "\"The snow is slowly pressed into a grainy material called firn and eventually into solid ice.\""),
                Gap(2, "Drilling: camps work only during the polar ___", ["summer"],
                    "\"Drilling camps operate only in the brief polar summer.\""),
                Gap(3, "Transport: cores must be kept ___ at every stage", ["frozen"],
                    "\"The cores must be kept frozen at every stage of their journey to the laboratory.\""),
                Gap(4, "Air bubbles: show levels of carbon dioxide and ___", ["methane"],
                    "\"Scientists can measure how much carbon dioxide and methane the air contained at the time.\""),
                Gap(5, "Temperature: measured using oxygen atoms of different weights, called ___", ["isotopes"],
                    "\"Water molecules contain oxygen atoms of slightly different weights, known as isotopes.\""),
                Gap(6, "Dust: more common in cold, dry periods, when ___ were stronger", ["winds"],
                    "\"More plentiful during cold, dry periods, when winds were stronger.\""),
            ]),
            new("tfng", TfngInstructions, null,
            [
                Stmt(7, "Drilling a single deep ice core can take more than one season.", "TRUE",
                    "\"A single deep core ... may take several seasons to complete.\""),
                Stmt(8, "The air in the bubbles is the same age as the ice that surrounds it.", "FALSE",
                    "\"The air inside is always somewhat younger than the ice around it.\""),
                Stmt(9, "Some eruptions recorded in the ice are also described in historical documents.", "TRUE",
                    "\"Some of these can be matched to eruptions whose dates are known from historical documents.\""),
                Stmt(10, "Most of the lead found in Greenland ice came from Roman mines in Spain.", "NOT GIVEN",
                    "The passage mentions lead from \"metalworking in Europe\" but does not say where the mines were."),
                Stmt(11, "Annual layers can easily be counted in ice from central Antarctica.", "FALSE",
                    "\"In central Antarctica ... the layers are too thin to count.\""),
                Stmt(12, "The Dome C core was drilled by scientists from a single country.", "NOT GIVEN",
                    "The passage gives the location and age of the Dome C core but says nothing about who drilled it."),
                Stmt(13, "Carbon dioxide levels today are higher than at any time shown in the Antarctic record.", "TRUE",
                    "\"The concentration of carbon dioxide in today's atmosphere is far higher than at any point in the entire record.\""),
            ]),
        ]);

    private static ReadingPassage Ra03P2() => new("The Spyglass That Changed the Sky",
        """
        In the autumn of 1608, a spectacle maker named Hans Lipperhey, who lived in the Dutch town of Middelburg, travelled to The Hague to ask the government for a patent on a new device. It consisted of a tube with a lens at each end, and it made distant objects appear about three times closer. Lipperhey's application is the earliest surviving document that clearly describes a telescope, and for this reason he is often called its inventor. The truth is less certain. Within weeks, at least one other Dutchman had claimed to have made the same instrument, and the government refused to grant a patent, partly on the grounds that the device was already known and too easy to copy.

        It is not surprising that the telescope emerged in the Netherlands at this time. Spectacles had been in use in Europe for more than three centuries, and Dutch craftsmen had become skilled at grinding lenses. Once lenses of reasonable quality were widely available, it was probably only a matter of time before someone held two of them in line and noticed the effect. A popular story claims that the discovery was made by children playing in a spectacle maker's workshop, but there is no reliable evidence for this.

        News of the device spread quickly across Europe, and by the following spring small telescopes were being sold in Paris. Their first users saw them mainly as tools for war and trade: a general could watch an enemy army, and a merchant could identify an approaching ship long before it reached the harbour. It was an Italian professor of mathematics, Galileo Galilei, who turned the instrument towards the sky, with results that changed science.

        Galileo heard about the Dutch invention in 1609 and, without having seen one, worked out how it must function and built his own. He then set about improving it, grinding his own lenses, and within a few months had produced an instrument that magnified around twenty times. With it he saw that the Moon was not a perfectly smooth sphere, as many scholars believed, but had mountains and valleys like the Earth. He found that the Milky Way was made up of countless individual stars, and in January 1610 he observed four small bodies moving around the planet Jupiter. He published these discoveries in a short book in March 1610, and it made him famous almost overnight.

        Galileo's telescopes had serious limitations. Their field of view was so narrow that only a small part of the Moon could be seen at once, and finding a particular object required great patience. In 1611, the German astronomer Johannes Kepler suggested a different arrangement using two convex lenses. This design produced an upside-down image, which mattered little to astronomers, but it offered a much wider field of view, and it eventually became the standard form of the astronomical telescope.

        A more stubborn problem was colour. A simple lens bends light of different colours by slightly different amounts, so the image of a bright star is surrounded by coloured fringes. Astronomers found that the effect was reduced if the lens was only gently curved, but such a lens brings light to a focus a long way behind it. The result was a period of remarkably long telescopes, some of them more than forty metres in length, which had to be hung from tall masts and were extremely difficult to point at the sky.

        The English scientist Isaac Newton took a different approach. Believing, wrongly as it turned out, that the colour problem could never be solved with lenses, he built a telescope in 1668 that used a curved mirror instead, since a mirror reflects all colours in the same way. His first reflecting telescope was only about fifteen centimetres long, yet it performed as well as a much longer instrument using lenses. Newton's mirror was made of a polished metal alloy, which tarnished quickly, and reflectors did not become truly practical until later craftsmen learnt to make larger and more reliable mirrors.

        The colour problem in lenses was eventually solved in the eighteenth century by combining two lenses made of different types of glass, whose errors cancel each other out. Such 'achromatic' lenses made smaller, sharper telescopes possible. Yet it is the principle of Newton's instrument that dominates astronomy today. All of the largest modern telescopes use mirrors, some of them several metres across and made from many separate segments, because a large mirror can be supported from behind, whereas a large lens can only be held at its edges and tends to sag under its own weight.
        """,
        [
            new("tfng", Ra26Tfng(2), null,
            [
                Stmt(14, "Lipperhey's patent application is the oldest known document that clearly describes a telescope.", "TRUE",
                    "\"Lipperhey's application is the earliest surviving document that clearly describes a telescope.\""),
                Stmt(15, "The Dutch government granted Lipperhey a patent for his device.", "FALSE",
                    "\"The government refused to grant a patent.\""),
                Stmt(16, "There is strong evidence that the telescope was discovered by children.", "FALSE",
                    "\"A popular story claims that the discovery was made by children ... but there is no reliable evidence for this.\""),
                Stmt(17, "The first telescopes sold in Paris were very expensive.", "NOT GIVEN",
                    "The passage says telescopes were being sold in Paris, but nothing about their price."),
                Stmt(18, "Galileo examined a Dutch telescope before building his own.", "FALSE",
                    "\"Without having seen one, [he] worked out how it must function and built his own.\""),
                Stmt(19, "Galileo's book about his discoveries was quickly translated into several languages.", "NOT GIVEN",
                    "The passage says the book made him famous, but does not mention translations."),
            ]),
            new("mcq", Abcd, null,
            [
                Mcq(20, "According to the passage, the first users of telescopes saw them mainly as",
                    ["tools for military and commercial purposes.", "toys for children.", "aids for people with poor eyesight.", "instruments for studying the stars."], "A",
                    "\"Their first users saw them mainly as tools for war and trade.\""),
                Mcq(21, "What was the main advantage of Kepler's design?",
                    ["It produced an image the right way up.", "It gave a much wider field of view.", "It was cheaper to build.", "It removed coloured fringes."], "B",
                    "\"It offered a much wider field of view.\""),
                Mcq(22, "Why were some telescopes made extremely long?",
                    ["Longer tubes made telescopes easier to point at the sky.", "Astronomers wanted to magnify objects more than twenty times.", "Gently curved lenses, which reduced colour problems, focused light far behind them.", "Mirrors had to be placed far from the eye."], "C",
                    "\"The effect was reduced if the lens was only gently curved, but such a lens brings light to a focus a long way behind it.\""),
                Mcq(23, "Why did Newton use a mirror in his telescope?",
                    ["He could not find lenses of good quality.", "Mirrors were cheaper to make than lenses.", "He believed lenses could never solve the colour problem.", "He wanted a telescope with a wider field of view."], "C",
                    "\"Believing, wrongly as it turned out, that the colour problem could never be solved with lenses, he built a telescope ... that used a curved mirror instead.\""),
            ]),
            new("gap", Ra26Sent2, 2,
            [
                Gap(24, "Newton's mirror was made of a polished ___, which tarnished quickly.", ["metal alloy"],
                    "\"Newton's mirror was made of a polished metal alloy, which tarnished quickly.\""),
                Gap(25, "The colour problem was solved by combining two lenses made of different types of ___.", ["glass"],
                    "\"Combining two lenses made of different types of glass, whose errors cancel each other out.\""),
                Gap(26, "A large lens can only be held at its edges and tends to ___ under its own weight.", ["sag"],
                    "\"A large lens can only be held at its edges and tends to sag under its own weight.\""),
            ]),
        ]);

    private static ReadingPassage Ra03P3() => new("Can Creativity Be Taught?",
        """
        Few educational ideas enjoy such universal approval as creativity. Business leaders say they need creative employees, education ministers promise schools that will prepare children for jobs that do not yet exist, and a lecture arguing that schools destroy children's natural creativity has been watched by many millions of people online. It is hard to find anyone who is against creativity. Yet when we ask what it would actually mean for schools to teach it, the agreement quickly disappears. My own view is that schools can and should develop creativity, but that much of the current enthusiasm is directed at the wrong target.

        Part of the difficulty is that the word itself is used loosely. For some people it refers mainly to the arts: painting, music, drama and creative writing. For others it means a general ability to produce original ideas in any field, from engineering to cooking. Researchers usually define a creative idea as one that is both new and useful, or appropriate to the task. This second condition is often forgotten. A child who answers a mathematics problem with a random number has produced something original, but not something creative.

        The claim that schools destroy creativity rests largely on tests of so-called divergent thinking, in which people are asked, for example, to list as many uses as they can for an everyday object such as a brick or a paper clip. Young children often produce more unusual answers than older ones, and this has been taken as evidence that schooling makes children more conventional. I find this argument weak. Such tests measure a narrow kind of playful fluency, and it is far from clear that they predict real creative achievement. Older children may give fewer odd answers simply because they have learnt what bricks are actually good for.

        A more serious objection to teaching creativity comes from those who argue that it cannot be separated from knowledge. Studies of people who have made important contributions in science, music or literature consistently show that they spent many years mastering their field before producing their best work. A composer cannot break the rules of harmony in an interesting way without first understanding them. On this view, the best way for schools to encourage creativity is simply to teach subjects well.

        I agree with the first half of this argument, but not with its conclusion. Knowledge is necessary for creativity, but it is not sufficient. Many students learn a great deal of chemistry or history without ever being asked to use it to solve an unfamiliar problem, design an experiment or construct an argument of their own. The habits involved in creative work, such as tolerating uncertainty, generating several possible approaches before choosing one and revising a draft in response to criticism, need practice, and in many classrooms there is little opportunity for it.

        What I do not support is the idea of teaching creativity as a separate subject, with its own place in the timetable and its own set of exercises. There is little evidence that general thinking skills practised in isolation transfer to other areas. A lesson on brainstorming techniques will not make a pupil a better scientist unless it takes place within science. Creativity should instead be built into existing subjects, through tasks that do not have a single correct answer: designing a fair test in biology, writing a speech from the point of view of a historical figure, or finding more than one way to solve a mathematical problem.

        Assessment is the greatest practical obstacle. Examinations reward answers that can be marked quickly and consistently, and creative work is, by its nature, difficult to mark in this way. Teachers under pressure to raise exam results understandably spend their time on what will be tested. Some education systems have experimented with assessing extended projects, and although these are more expensive to mark, they give students a reason to work on problems over weeks rather than minutes. I believe this cost is worth paying, at least for part of a student's final grade.

        Finally, creativity requires a certain kind of classroom atmosphere. Pupils who fear being laughed at or penalised for a wrong answer are unlikely to take intellectual risks. This does not mean that every idea should be praised; honest feedback is essential, and a teacher who calls every suggestion wonderful teaches pupils nothing about the difference between a good idea and a poor one. But mistakes need to be treated as a normal part of learning rather than as failures.

        Schools, then, do not need to choose between knowledge and creativity. The real question is not whether children should learn facts or learn to think creatively, but whether they are given the chance to do something with what they know.
        """,
        [
            new("mcq", Abcd, null,
            [
                Mcq(27, "What point does the writer make in the first paragraph?",
                    ["Business leaders do not really value creativity.", "People agree creativity is good but disagree about how schools should teach it.", "Most people have stopped supporting creativity in schools.", "Schools have already found effective ways of teaching creativity."], "B",
                    "\"It is hard to find anyone who is against creativity. Yet when we ask what it would actually mean for schools to teach it, the agreement quickly disappears.\""),
                Mcq(28, "The example of a child answering with a random number shows that",
                    ["children often find mathematics difficult.", "an original answer is not necessarily a creative one.", "mathematics leaves little room for creativity.", "teachers should reward unusual answers."], "B",
                    "A creative idea must be \"both new and useful\"; the random number is \"something original, but not something creative\"."),
                Mcq(29, "What does the writer think of divergent thinking tests?",
                    ["They prove that schools make children conventional.", "They are the best way to measure creative achievement.", "They measure only a limited kind of ability.", "They are too difficult for young children."], "C",
                    "\"Such tests measure a narrow kind of playful fluency, and it is far from clear that they predict real creative achievement.\""),
                Mcq(30, "According to the writer, older children may give fewer unusual answers because they",
                    ["know more about how objects are really used.", "are less imaginative than younger children.", "are afraid of being laughed at.", "have spent less time playing."], "A",
                    "\"Older children may give fewer odd answers simply because they have learnt what bricks are actually good for.\""),
            ]),
            new("ynng", YnngInstructions, null,
            [
                Stmt(31, "Knowledge of a subject is necessary for creative work in it.", "YES",
                    "\"Knowledge is necessary for creativity, but it is not sufficient.\""),
                Stmt(32, "Science teachers are better at encouraging creativity than history teachers.", "NOT GIVEN",
                    "The writer mentions chemistry and history students but does not compare teachers of different subjects."),
                Stmt(33, "Creativity should be taught as a separate school subject.", "NO",
                    "\"What I do not support is the idea of teaching creativity as a separate subject.\""),
                Stmt(34, "Most pupils enjoy lessons on brainstorming techniques.", "NOT GIVEN",
                    "The writer says brainstorming lessons will not make pupils better scientists, but says nothing about whether pupils enjoy them."),
                Stmt(35, "The extra cost of assessing extended projects is justified.", "YES",
                    "\"I believe this cost is worth paying, at least for part of a student's final grade.\""),
                Stmt(36, "Teachers should praise every idea that pupils suggest.", "NO",
                    "\"This does not mean that every idea should be praised; honest feedback is essential.\""),
            ]),
            new("gap", Ra26Sent1, 1,
            [
                Gap(37, "People who made important contributions spent many years ___ their field before producing their best work.", ["mastering"],
                    "\"They spent many years mastering their field before producing their best work.\""),
                Gap(38, "A composer must understand the rules of ___ before breaking them in an interesting way.", ["harmony"],
                    "\"A composer cannot break the rules of harmony in an interesting way without first understanding them.\""),
                Gap(39, "Creative habits include tolerating ___ and revising a draft after criticism.", ["uncertainty"],
                    "\"Such as tolerating uncertainty, generating several possible approaches ... and revising a draft in response to criticism.\""),
                Gap(40, "There is little evidence that thinking skills practised in isolation ___ to other areas.", ["transfer"],
                    "\"There is little evidence that general thinking skills practised in isolation transfer to other areas.\""),
            ]),
        ]);

    // =====================================================================
    // TEST 4
    // =====================================================================

    private static ReadingPassage Ra04P1() => new("The Thread from the Mulberry Tree",
        """
        According to a Chinese legend, silk was discovered almost five thousand years ago by an empress named Leizu, who was drinking tea beneath a mulberry tree when a cocoon fell into her cup. As she lifted it out, she noticed that the hot water had loosened a fine, shining thread, which unwound as she pulled. The story cannot be tested, but archaeology confirms that silk production in China is extremely old: fragments of silk fabric, and a cocoon that appears to have been deliberately cut, have been found at sites dating back around five thousand years.

        The insect at the heart of the industry, the domestic silkworm, is the caterpillar of a moth that no longer exists in the wild. Over thousands of years of breeding, it has become entirely dependent on humans. The adult moths cannot fly, and the caterpillars show little interest in escaping; they eat almost nothing but the leaves of the white mulberry tree, and they eat them in enormous quantities. In the course of about a month, a silkworm increases its weight roughly ten thousand times before it stops feeding and begins to spin its cocoon.

        The cocoon is made from a single continuous filament, produced by two glands in the caterpillar's head and coated with a sticky protein that holds the structure together. Unwound, the filament from one cocoon can be around a kilometre long. To obtain it in one piece, producers must prevent the moth from emerging, since the adult would break through the cocoon and cut the thread into short lengths. The insects inside are therefore killed with heat, usually by steaming or drying the cocoons. The cocoons are then softened in hot water, the ends of the filaments are found, and several filaments are wound together to make a single thread strong enough to weave. Several thousand cocoons may be needed for a single silk garment, which helps to explain why silk has always been a luxury.

        For many centuries, China kept the process secret, and revealing it to foreigners was reportedly punishable by death. The finished cloth, however, travelled widely. From about the second century BCE, silk was carried westwards along the network of trade routes that linked China with Central Asia, Persia and eventually the Mediterranean. These routes carried many other goods, as well as ideas and inventions, but it was silk that gave them the name by which they are known today, a term invented by a German geographer only in the nineteenth century. In Rome, silk became so fashionable among the wealthy that some writers complained about the amount of gold leaving the empire to pay for it.

        The secret could not be kept forever. Knowledge of sericulture, as silk farming is called, spread to Korea and later to Japan and India. A well-known story tells of a Chinese princess who, sent to marry a king in Central Asia, hid silkworm eggs in her headdress so that she could continue to wear silk in her new home. In the West, the historian Procopius recorded that in the sixth century two monks smuggled silkworm eggs to Constantinople, the capital of the Byzantine Empire, hidden inside hollow walking sticks. Whatever the truth of these stories, the Byzantines soon developed their own silk industry, which remained under strict state control.

        From the Byzantine world, silk production spread gradually across southern Europe. By the later Middle Ages, Italian cities such as Lucca and Venice had become famous for their silk fabrics, and in the following centuries the French city of Lyon became the leading centre of silk weaving in Europe. The climate of southern France and northern Italy suited the mulberry tree, and in some regions almost every farming family raised silkworms in spring to earn extra income.

        In the middle of the nineteenth century, this prosperous industry was almost destroyed. A disease known as pébrine spread through the silkworm farms of Europe, killing the caterpillars in vast numbers. The French government asked the chemist Louis Pasteur to investigate. Although he had never studied silkworms before, Pasteur showed that the disease was caused by a tiny parasite passed from the female moth to her eggs, and he developed a simple method of prevention: each female was examined under a microscope after laying her eggs, and only the eggs of healthy moths were kept. The method is still used, in modified form, today.

        The twentieth century brought a different kind of challenge. In the 1930s chemists developed nylon, one of the first fully synthetic fibres, and it quickly replaced silk in many products, most famously women's stockings. European silk production declined, and today the great majority of the world's silk is produced in Asia, with China and India together accounting for most of the total. Silk has not lost its appeal, however. As well as its traditional role in clothing, researchers are investigating its use in medicine, where its strength and its compatibility with the human body make it a promising material for stitches and for supports that help damaged tissue to repair itself.
        """,
        [
            new("mcq", Abcd, null,
            [
                Mcq(1, "What does the writer say about the legend of Leizu?",
                    ["It is supported by archaeological evidence.", "It cannot be checked, but silk production in China is very old.", "It was invented in the nineteenth century.", "It explains how silk reached Europe."], "B",
                    "\"The story cannot be tested, but archaeology confirms that silk production in China is extremely old.\""),
                Mcq(2, "What is true of the domestic silkworm?",
                    ["The adult moths are strong fliers.", "It is still common in the wild.", "It feeds on many kinds of leaves.", "It depends entirely on humans."], "D",
                    "\"Over thousands of years of breeding, it has become entirely dependent on humans.\""),
                Mcq(3, "Why are the insects inside the cocoons killed?",
                    ["to stop them eating the mulberry leaves", "to soften the sticky protein", "to make the thread stronger", "to prevent the moth from breaking the thread"], "D",
                    "\"Producers must prevent the moth from emerging, since the adult would break through the cocoon and cut the thread into short lengths.\""),
            ]),
            new("tfng", TfngInstructions, null,
            [
                Stmt(4, "Silkworm caterpillars will readily eat many different kinds of leaves.", "FALSE",
                    "\"They eat almost nothing but the leaves of the white mulberry tree.\""),
                Stmt(5, "Silk has always been expensive partly because so many cocoons are needed for one garment.", "TRUE",
                    "\"Several thousand cocoons may be needed for a single silk garment, which helps to explain why silk has always been a luxury.\""),
                Stmt(6, "Roman law eventually banned the wearing of silk.", "NOT GIVEN",
                    "The passage says some Roman writers complained about the cost of silk, but does not mention any law."),
                Stmt(7, "The name used today for the trade routes was invented in the nineteenth century.", "TRUE",
                    "\"A term invented by a German geographer only in the nineteenth century.\""),
                Stmt(8, "The Byzantine silk industry was free from government control.", "FALSE",
                    "\"The Byzantines soon developed their own silk industry, which remained under strict state control.\""),
            ]),
            new("gap", Ra26Notes1, 1,
            [
                Gap(9, "Silk farming is known as ___", ["sericulture"],
                    "\"Knowledge of sericulture, as silk farming is called, spread to Korea.\""),
                Gap(10, "Later Middle Ages: Italian cities such as Lucca and ___ famous for silk", ["Venice"],
                    "\"Italian cities such as Lucca and Venice had become famous for their silk fabrics.\""),
                Gap(11, "Later: the French city of ___ became Europe's leading centre of silk weaving", ["Lyon", "Lyons"],
                    "\"The French city of Lyon became the leading centre of silk weaving in Europe.\""),
                Gap(12, "Pébrine: caused by a tiny ___ passed from the female moth to her eggs", ["parasite"],
                    "\"The disease was caused by a tiny parasite passed from the female moth to her eggs.\""),
                Gap(13, "Pasteur's method: each female examined under a ___", ["microscope"],
                    "\"Each female was examined under a microscope after laying her eggs.\""),
            ]),
        ]);

    private static ReadingPassage Ra04P2() => new("The Noisy Deep",
        """
        For most of human history, the ocean was imagined as a place of silence. The French explorer Jacques Cousteau even called one of his books The Silent World. In fact, the sea is full of sound. Whales sing, dolphins click and whistle, fish grunt and drum, and snapping shrimps produce such a constant crackle that, near some coral reefs, it resembles the sound of food frying in a pan. Natural events add to the noise: breaking waves, rain on the surface, earthquakes beneath the sea floor and the cracking of ice.

        Sound matters so much in the ocean because light does not travel far through water. Below a few hundred metres, the sea is completely dark, and even near the surface visibility is often limited to a few metres. Sound, by contrast, travels more than four times faster in water than in air and can cover enormous distances with little loss of energy, particularly at low frequencies. Many marine animals have therefore evolved to rely on hearing rather than sight. Whales use calls to keep in contact with one another across great distances, dolphins find their prey by echolocation, and many fish use sound to attract mates. Even the young of some reef fish, which spend their early lives drifting in open water, appear to find their way back to a reef partly by listening for the noise it makes.

        Over the past century, human activity has added a great deal of sound to this environment. The largest single source is shipping. Most of the noise from a large ship comes from its propeller, where tiny bubbles form and collapse as the blades turn, a process known as cavitation. Because the number of ships has grown enormously and modern vessels are larger and more powerful, background noise at low frequencies has risen considerably along major shipping routes over several decades. Other sources are louder but less continuous. Surveys searching for oil and gas beneath the sea floor use devices called airguns, which release powerful bursts of compressed air every few seconds, often for weeks at a time. The construction of offshore wind farms involves driving huge steel piles into the sea bed with hammers, producing intense pulses of sound. Military sonar, used to detect submarines, can also be extremely loud.

        The effects on marine life vary with the type of sound. Very loud sounds close to their source can damage animals' hearing, either temporarily or permanently. Several mass strandings of beaked whales, deep-diving species that are rarely seen at sea, have occurred shortly after naval exercises using sonar, and although the exact mechanism is still debated, many scientists believe that the whales were frightened into surfacing too quickly from deep dives.

        The more widespread problem, however, is not sudden injury but constant background noise. Just as people at a noisy party must raise their voices to be heard, whales in busy waters have been recorded calling more loudly, repeating their calls or waiting for a quieter moment. This effect, known as masking, reduces the distance over which animals can communicate and may make it harder for them to find food, avoid predators or locate mates. Striking evidence came from the Bay of Fundy in Canada, where researchers happened to be collecting samples from right whales during a period when shipping traffic was unusually light. They found that levels of stress-related hormones in the whales' droppings fell as the noise decreased, suggesting that the everyday sound of ships causes the animals long-term stress.

        Unlike many forms of pollution, noise has one great advantage from the point of view of those trying to reduce it: it disappears as soon as its source is switched off. It leaves nothing behind in the water, and the benefits of reducing it are felt immediately. Several practical solutions already exist. For ships, one of the most effective is simply to travel more slowly, since a propeller turning at lower speed produces far less cavitation. In one programme on the Pacific coast of Canada, ships are asked to reduce speed voluntarily while passing through waters used by an endangered population of killer whales. Slower speeds also reduce fuel consumption, so the measure can save money as well as noise, although it makes journeys longer.

        Engineers are also designing quieter propellers and hulls, and some shipping companies now advertise the low noise of their vessels. During pile driving, construction companies can surround the work site with a bubble curtain, a ring of air bubbles released from a pipe on the sea floor, which absorbs and scatters much of the sound before it spreads. Seismic surveys can be timed to avoid the seasons when whales migrate or breed.

        International rules, however, remain limited. The International Maritime Organization, which regulates global shipping, has issued guidelines on reducing underwater noise, but they are voluntary. Some scientists argue that noise should be treated like other pollutants, with legal limits and regular monitoring. Others point out that far more research is needed, since the hearing of most marine species has never been studied. What is clear is that the ocean is not the silent world it was once thought to be, and that the sounds we add to it matter to the creatures that live there.
        """,
        [
            new("tfng", Ra26Tfng(2), null,
            [
                Stmt(14, "Near some coral reefs, snapping shrimps produce an almost continuous noise.", "TRUE",
                    "\"Snapping shrimps produce such a constant crackle that, near some coral reefs, it resembles the sound of food frying.\""),
                Stmt(15, "In water, low-frequency sounds lose energy more quickly than other sounds.", "FALSE",
                    "Sound can cover great distances \"with little loss of energy, particularly at low frequencies\"."),
                Stmt(16, "Young reef fish may use sound to help them find a reef.", "TRUE",
                    "\"The young of some reef fish ... appear to find their way back to a reef partly by listening for the noise it makes.\""),
                Stmt(17, "Most of the noise made by a large ship comes from its engines.", "FALSE",
                    "\"Most of the noise from a large ship comes from its propeller.\""),
                Stmt(18, "Airguns are used more often in the Pacific than in the Atlantic.", "NOT GIVEN",
                    "The passage explains what airguns do but does not say where they are used most."),
            ]),
            new("mcq", Abcd, null,
            [
                Mcq(19, "What does the writer say about mass strandings of beaked whales?",
                    ["They are caused by damage to the whales' hearing.", "They have happened soon after naval exercises using sonar.", "Their cause is now fully understood.", "They happen mainly near offshore wind farms."], "B",
                    "\"Several mass strandings of beaked whales ... have occurred shortly after naval exercises using sonar, and although the exact mechanism is still debated...\""),
                Mcq(20, "How have whales in busy waters been recorded responding to noise?",
                    ["by swimming to deeper water", "by calling less often", "by leaving the area permanently", "by calling more loudly"], "D",
                    "\"Whales in busy waters have been recorded calling more loudly, repeating their calls or waiting for a quieter moment.\""),
                Mcq(21, "What did the study in the Bay of Fundy suggest?",
                    ["Right whales are not affected by ship noise.", "Everyday ship noise causes whales long-term stress.", "Whales avoid areas with heavy shipping.", "Shipping traffic in the bay was increasing."], "B",
                    "\"Suggesting that the everyday sound of ships causes the animals long-term stress.\""),
                Mcq(22, "According to the writer, what advantage does noise have compared with many other forms of pollution?",
                    ["It affects fewer species.", "It is cheaper to measure.", "It is already controlled by international law.", "It stops as soon as its source is switched off."], "D",
                    "\"It disappears as soon as its source is switched off. It leaves nothing behind in the water.\""),
            ]),
            new("gap", Ra26Sent2, 2,
            [
                Gap(23, "Travelling more slowly also allows ships to use less ___.", ["fuel"],
                    "\"Slower speeds also reduce fuel consumption.\""),
                Gap(24, "During pile driving, a ___ can absorb and scatter much of the sound.", ["bubble curtain"],
                    "\"Construction companies can surround the work site with a bubble curtain ... which absorbs and scatters much of the sound.\""),
                Gap(25, "Seismic surveys can be timed to avoid the seasons when whales ___ or breed.", ["migrate"],
                    "\"Seismic surveys can be timed to avoid the seasons when whales migrate or breed.\""),
                Gap(26, "The International Maritime Organization's guidelines on underwater noise are ___.", ["voluntary"],
                    "\"Has issued guidelines on reducing underwater noise, but they are voluntary.\""),
            ]),
        ]);

    private static ReadingPassage Ra04P3() => new("Hands or Heads?",
        """
        'Tell me and I forget; teach me and I may remember; involve me and I learn.' This saying, usually attributed to Benjamin Franklin although it does not appear in any of his known writings, is displayed on the walls of classrooms and training centres around the world. It expresses a view that has become something close to common sense: that we learn far more from doing things ourselves than from reading about them or listening to explanations. Books and lectures, on this view, are a second-rate substitute for experience. I think this view contains an important truth, but in its popular form it is at least as misleading as the old-fashioned belief it replaced.

        The case for learning by doing is strong where practical skills are concerned. Nobody has ever learnt to swim, ride a bicycle or play the violin by reading a book, however good. Skills of this kind depend on thousands of small adjustments that cannot be put into words, and they develop only through repeated practice with feedback. For most of history, trades from carpentry to medicine were passed on through apprenticeship, in which a young person worked alongside an experienced master, first watching, then helping and finally working alone. The educational thinker John Dewey, writing in the early twentieth century, argued that schools should similarly be places where children learn through active experience rather than passive listening.

        The difficulty arises when this principle is applied to everything, and especially to beginners. In some schools and training programmes, it has become fashionable to give learners a problem and ask them to discover the relevant ideas for themselves, with the teacher acting only as a guide. For learners who already know a good deal about a subject, this can work well. For novices, however, the research is far less encouraging. A beginner faced with an open problem has no way of knowing which features matter, and may spend a great deal of mental effort searching blindly, with little left over for actually learning. Studies comparing the two approaches have repeatedly found that beginners learn more from studying worked examples, in which an expert's solution is shown step by step, than from trying to solve the same problems unaided.

        It is worth remembering, too, that books are not simply a poor substitute for experience; in many respects they are a better one. No individual could discover through personal experience what a book on geology or economics contains, since that knowledge is the product of many lifetimes of observation and argument. A well-written text also offers something that experience rarely does: an organised account, in which the important is separated from the unimportant and the connections between ideas are made clear. Experience, by contrast, is often confusing, and people can repeat the same activity for years without improving, because they never understand why their results are good or bad.

        This last point suggests that the real contrast is not between doing and reading but between thoughtful and thoughtless activity. Research on expert performance has found that simply accumulating hours of practice is not enough; what distinguishes those who improve is practice that is focused on specific weaknesses and accompanied by feedback. Such practice is often guided by knowledge obtained from teachers or books. The violinist who improves fastest is not the one who plays the most, but the one who understands what she is trying to change.

        Nor should the value of explanation be dismissed in practical fields. A trainee surgeon must study anatomy in detail before being allowed near a patient, and a pilot spends many hours learning theory before flying. In these cases nobody suggests that learners should discover the principles for themselves, because the cost of mistakes is too high. Yet the same logic applies, less dramatically, in ordinary classrooms: when pupils are left to discover something that could have been explained in five minutes, the time lost is time that could have been spent practising and applying it.

        None of this is an argument for returning to classrooms in which pupils sit silently copying notes. Knowledge that is never used tends to be forgotten, and students who have only read about an idea often fail to recognise it when it appears in an unfamiliar form. Practical work, projects and experiments have an important role, particularly in helping learners to apply what they know and to remain motivated. The question is one of timing and balance. In my view, the most effective sequence for most learners is clear explanation first, followed by guided practice, and then gradually more independent work as their competence grows.

        The popular saying, then, needs revising. Being involved does help us to learn, but only when we have enough knowledge to make sense of what we are doing. Books and teachers provide that knowledge far more efficiently than trial and error. The choice between learning by doing and learning from books is a false one: the best learners, and the best teachers, make use of both.
        """,
        [
            new("ynng", YnngInstructions, null,
            [
                Stmt(27, "There is clear evidence that Benjamin Franklin wrote the saying quoted at the start.", "NO",
                    "\"Usually attributed to Benjamin Franklin although it does not appear in any of his known writings.\""),
                Stmt(28, "Skills such as swimming can only be developed through practice.", "YES",
                    "\"Nobody has ever learnt to swim ... by reading a book ... they develop only through repeated practice with feedback.\""),
                Stmt(29, "Apprenticeships are becoming more popular again today.", "NOT GIVEN",
                    "The writer describes apprenticeship in the past but says nothing about its popularity today."),
                Stmt(30, "Asking learners to discover ideas for themselves can suit those who already know a lot about a subject.", "YES",
                    "\"For learners who already know a good deal about a subject, this can work well.\""),
                Stmt(31, "Repeating an activity for many years always leads to improvement.", "NO",
                    "\"People can repeat the same activity for years without improving.\""),
            ]),
            new("mcq", Abcd, null,
            [
                Mcq(32, "According to the writer, why do beginners struggle when given an open problem?",
                    ["They are not interested in the subject.", "Their teachers give them too much help.", "They do not know which features are important.", "The problems are usually too easy."], "C",
                    "\"A beginner faced with an open problem has no way of knowing which features matter.\""),
                Mcq(33, "What advantage of books over experience does the writer mention?",
                    ["Books are cheaper than practical training.", "Books give an organised account that shows how ideas connect.", "Books are easier to remember.", "Books are written by people with more experience."], "B",
                    "\"A well-written text also offers ... an organised account, in which ... the connections between ideas are made clear.\""),
                Mcq(34, "The example of the violinist is used to show that",
                    ["talent matters more than practice.", "musicians should read more books.", "understanding what to change matters more than the amount of practice.", "practice without a teacher is useless."], "C",
                    "\"The violinist who improves fastest is not the one who plays the most, but the one who understands what she is trying to change.\""),
                Mcq(35, "Why does the writer mention trainee surgeons and pilots?",
                    ["to show that mistakes are too costly to leave learners to discover principles alone", "to argue that practical fields need less theory", "to show that experience is more important than study", "to compare the length of different training programmes"], "A",
                    "\"Nobody suggests that learners should discover the principles for themselves, because the cost of mistakes is too high.\""),
            ]),
            new("gap", Ra26Sent1, 1,
            [
                Gap(36, "Beginners learn more from studying ___ examples than from solving problems unaided.", ["worked"],
                    "\"Beginners learn more from studying worked examples ... than from trying to solve the same problems unaided.\""),
                Gap(37, "Effective practice is focused on specific ___.", ["weaknesses"],
                    "\"Practice that is focused on specific weaknesses and accompanied by feedback.\""),
                Gap(38, "Effective practice is also accompanied by ___.", ["feedback"],
                    "\"Practice that is focused on specific weaknesses and accompanied by feedback.\""),
                Gap(39, "Knowledge that is never used tends to be ___.", ["forgotten"],
                    "\"Knowledge that is never used tends to be forgotten.\""),
                Gap(40, "The writer recommends clear explanation, then guided ___, and finally more independent work.", ["practice"],
                    "\"Clear explanation first, followed by guided practice, and then gradually more independent work.\""),
            ]),
        ]);

    // =====================================================================
    // TEST 5
    // =====================================================================

    private static ReadingPassage Ra05P1() => new("When Grasshoppers Gather",
        """
        For most of the time, the desert locust is an unremarkable insect. It lives alone in the dry lands that stretch from West Africa across the Arabian Peninsula to India, avoiding other locusts and doing little harm. Every few years, however, something changes. Under the right conditions, these solitary insects can transform into dense, moving swarms containing billions of individuals, capable of destroying crops across entire regions. Understanding how and why this transformation takes place has occupied scientists for more than a century.

        The key discovery was made in 1921 by the Russian-born entomologist Boris Uvarov, working in London. Until then, the solitary green insects found in the desert and the yellow-and-black insects found in swarms had been classified as two different species. Uvarov showed that they were in fact the same species in two different forms, or phases. The solitary phase is shy and mainly active at night, while the gregarious phase is active during the day, attracted to other locusts and constantly on the move. The two phases differ not only in colour and behaviour but even in body shape.

        The change from one phase to the other is triggered by crowding. In the desert, rain is rare and vegetation patchy. After an unusually wet period, plants grow over wide areas and locusts breed rapidly. When the rain stops and the vegetation begins to dry out, the insects are forced together into the shrinking patches of green that remain. Laboratory experiments have shown that repeated touching of the hind legs, which happens constantly when locusts are crowded together, is enough to set off the change. Within a few hours, the insects begin to behave gregariously, and researchers have linked this rapid shift to a sharp increase in serotonin, a chemical that also acts as a messenger in the human brain.

        Young locusts, which have no wings and are known as hoppers, march together across the ground in groups called bands. When they become adults and develop wings, they form swarms that can travel up to 150 kilometres in a single day, carried mainly by the wind. A swarm may cover several hundred square kilometres, and each square kilometre can contain between 40 and 80 million adults. Since each locust eats roughly its own weight in fresh food every day, the damage can be enormous: according to the Food and Agriculture Organization of the United Nations, a swarm covering one square kilometre can eat as much food in a day as about 35,000 people.

        Some swarms travel extraordinary distances. In 1988, locusts from West Africa crossed the Atlantic Ocean and reached islands in the Caribbean, a journey of around 5,000 kilometres that took about ten days. During major outbreaks, known as plagues, swarms can affect around a fifth of the Earth's land surface and more than 60 countries, threatening the food supply and income of a tenth of the world's population.

        Because swarms are so difficult to stop once they have formed, modern control focuses on prevention. The aim is to detect the early stages of the change and to treat the insects before they form large swarms. This work is coordinated by a locust information service at the Food and Agriculture Organization, which collects reports from national teams in affected countries. Field officers survey remote areas and send their observations, including the location, number and phase of any locusts found, using hand-held tablets linked to satellites. Satellite images are also used to identify areas where rain has fallen and vegetation is growing, so that survey teams know where to look.

        When locusts are found in dangerous numbers, they are usually sprayed with chemical pesticides, either from vehicles or from aircraft. These chemicals work quickly, but they can harm other insects, birds and the people who handle them. An alternative is a biological pesticide based on a fungus that infects locusts and grasshoppers but is harmless to most other animals. The fungus is slower, taking a week or more to kill the insects, so it is best suited to treating hopper bands before they can fly, and to areas such as nature reserves where chemicals are unacceptable.

        Prevention depends on continuous monitoring, and this can break down when funding falls during quiet years or when remote areas become unsafe to survey. The upsurge of 2019 to 2021 showed how quickly a situation can escalate. Two cyclones in 2018 brought heavy rain to a remote desert region of the Arabian Peninsula, where locusts bred for several generations without being detected. By the time the swarms were noticed, they had spread into East Africa and South Asia, and Kenya experienced its worst outbreak in seventy years. Large-scale control efforts eventually brought the situation under control, but the episode was a reminder that the ancient threat of the locust has not disappeared. Scientists warn that changing weather patterns, including more frequent cyclones, could make such outbreaks more common in future.
        """,
        [
            new("tfng", TfngInstructions, null,
            [
                Stmt(1, "The desert locust is found in parts of both Africa and Asia.", "TRUE",
                    "\"It lives alone in the dry lands that stretch from West Africa across the Arabian Peninsula to India.\""),
                Stmt(2, "Before 1921, solitary and swarming locusts were thought to be different species.", "TRUE",
                    "\"Until then, the solitary green insects ... and the yellow-and-black insects found in swarms had been classified as two different species.\""),
                Stmt(3, "Uvarov's ideas were immediately accepted by other scientists.", "NOT GIVEN",
                    "The passage describes Uvarov's discovery but not how other scientists reacted to it."),
                Stmt(4, "Locusts in the gregarious phase are mainly active at night.", "FALSE",
                    "\"The solitary phase is shy and mainly active at night, while the gregarious phase is active during the day.\""),
                Stmt(5, "Repeated touching of the hind legs can cause locusts to change phase.", "TRUE",
                    "\"Repeated touching of the hind legs ... is enough to set off the change.\""),
                Stmt(6, "Levels of serotonin in locusts fall when they become crowded.", "FALSE",
                    "\"Researchers have linked this rapid shift to a sharp increase in serotonin.\""),
            ]),
            new("mcq", Abcd, null,
            [
                Mcq(7, "How do adult swarms mainly travel long distances?",
                    ["by marching in bands", "by following rivers", "by flying only at night", "by being carried by the wind"], "D",
                    "\"They form swarms that can travel up to 150 kilometres in a single day, carried mainly by the wind.\""),
                Mcq(8, "According to the Food and Agriculture Organization, a swarm covering one square kilometre can eat in a day",
                    ["as much food as about 35,000 people.", "its own weight in fresh food.", "the crops of 60 countries.", "the food of a tenth of the world's population."], "A",
                    "\"A swarm covering one square kilometre can eat as much food in a day as about 35,000 people.\""),
                Mcq(9, "What is said about the swarms of 1988?",
                    ["They crossed the Atlantic Ocean in about ten days.", "They travelled from the Caribbean to West Africa.", "They affected more than 60 countries.", "They were the largest ever recorded."], "A",
                    "\"Locusts from West Africa crossed the Atlantic Ocean and reached islands in the Caribbean, a journey of around 5,000 kilometres that took about ten days.\""),
            ]),
            new("gap", Ra26Sent2, 2,
            [
                Gap(10, "Field officers send their observations using hand-held ___ linked to satellites.", ["tablets"],
                    "\"Using hand-held tablets linked to satellites.\""),
                Gap(11, "One biological pesticide is based on a ___ that is harmless to most other animals.", ["fungus"],
                    "\"A biological pesticide based on a fungus that infects locusts and grasshoppers but is harmless to most other animals.\""),
                Gap(12, "Because it works slowly, the biological pesticide is best used on ___ before they can fly.", ["hopper bands"],
                    "\"It is best suited to treating hopper bands before they can fly.\""),
                Gap(13, "The upsurge of 2019 to 2021 began after two ___ brought heavy rain to the Arabian Peninsula.", ["cyclones"],
                    "\"Two cyclones in 2018 brought heavy rain to a remote desert region of the Arabian Peninsula.\""),
            ]),
        ]);

    private static ReadingPassage Ra05P2() => new("The Box That Shrank the World",
        """
        On 26 April 1956, an old tanker called the Ideal X left the port of Newark, New Jersey, bound for Houston, Texas. On its deck were fifty-eight large metal boxes, each of which had arrived at the port on the back of a lorry and had been lifted aboard by crane in one piece. The voyage attracted little attention at the time, but it is now seen as the beginning of a revolution in transport which transformed ports, shipping and ultimately the world economy.

        To understand why, it is necessary to imagine how goods were moved before. For centuries, cargo was carried in what is known as break-bulk form: sacks, barrels, boxes and crates of every size, each of which had to be handled individually. At the port, gangs of dockworkers carried or wheeled each item from a warehouse to the side of the ship, where it was lifted aboard in a net and then arranged by hand in the hold so that it would not move during the voyage. At the other end, the process was reversed. Loading a large cargo ship could take a week or more, and ships often spent as much time in port as they did at sea. The work was dangerous, and theft of cargo was common.

        The man behind the Ideal X, Malcom McLean, was not a shipowner but the owner of a trucking company. He had spent years watching his lorries wait for hours at the docks while their loads were transferred by hand, and he reasoned that it would be far more efficient to lift the whole body of the lorry, with the goods still inside, straight onto a ship. He was not the first person to have the idea; railways and some shipping lines had experimented with containers before. What made McLean different was his willingness to redesign the entire system around the container, including the ships, the cranes and the ports. The savings were dramatic. By McLean's own calculations, loading the Ideal X cost less than 16 cents per ton, compared with nearly six dollars per ton for loose cargo.

        Adoption was nonetheless slow at first. Every company used containers of different sizes, with different fittings for lifting and fastening them, so a container belonging to one company could not easily be carried on another company's ship or lorry. In the late 1960s, after years of negotiation, international standards were agreed for the size of containers and the fittings at their corners. The most common unit today is based on a container twenty feet long, and the capacity of ships and ports is still measured in 'twenty-foot equivalent units'. Standardisation meant that a container could travel from a factory to a ship, to a train and finally to a lorry without ever being opened.

        Ports changed beyond recognition. Container ships needed deep water, large open areas for stacking boxes, and space for lorries and trains, and the old docks in the centre of many cities could not provide them. In New York, activity moved from the historic piers of Manhattan and Brooklyn to new terminals across the harbour in New Jersey. In London, the docks close to the city centre closed one after another, while a small port at Felixstowe on the east coast grew into the busiest container port in Britain. Many former dock areas have since been redeveloped as offices and housing.

        The human cost was considerable. A container terminal needs only a fraction of the workers required by a traditional port, and in port cities around the world, tens of thousands of dock jobs disappeared within a generation. Communities that had been built around the docks lost their main source of employment, and the transition was often painful for workers and their families.

        For the world economy, however, the effects were enormous. Because shipping costs fell so sharply, it became economical to manufacture goods far from where they were sold, and to build them from parts made in several different countries. Some economists have argued that containerisation did more to increase international trade in the late twentieth century than all the trade agreements signed during the same period. The complex supply chains of modern manufacturing, in which a single product may cross several borders before it is finished, would be almost unthinkable without the container.

        Container ships themselves have grown steadily. The first purpose-built vessels carried a few hundred containers; the largest ships today can carry more than 24,000 twenty-foot units and are around 400 metres long. These giants are highly efficient, but they bring new problems. Only a limited number of ports are deep enough to receive them, and when one of them is delayed, or blocks a busy waterway, the effects can be felt across the global economy within days. The simple metal box has made the world far more connected, but also more dependent on a system that few people ever see.
        """,
        [
            new("tfng", Ra26Tfng(2), null,
            [
                Stmt(14, "The first voyage of the Ideal X was widely reported at the time.", "FALSE",
                    "\"The voyage attracted little attention at the time.\""),
                Stmt(15, "Under the break-bulk system, ships might spend as long in port as at sea.", "TRUE",
                    "\"Ships often spent as much time in port as they did at sea.\""),
                Stmt(16, "McLean was the first person to think of carrying goods in containers.", "FALSE",
                    "\"He was not the first person to have the idea; railways and some shipping lines had experimented with containers before.\""),
                Stmt(17, "McLean's trucking company was the largest in the United States.", "NOT GIVEN",
                    "The passage says McLean owned a trucking company but nothing about its size."),
                Stmt(18, "International standards for containers were agreed quickly.", "FALSE",
                    "\"In the late 1960s, after years of negotiation, international standards were agreed.\""),
            ]),
            new("gap", "Complete the notes below.\nWrite NO MORE THAN TWO WORDS AND/OR A NUMBER from the passage for each answer.", 2,
            [
                Gap(19, "Break-bulk cargo: each item lifted aboard in a ___", ["net"],
                    "\"Where it was lifted aboard in a net and then arranged by hand in the hold.\""),
                Gap(20, "Ideal X: loading cost less than ___ cents per ton", ["16", "sixteen"],
                    "\"Loading the Ideal X cost less than 16 cents per ton.\""),
                Gap(21, "Standards: agreed for container size and the fittings at their ___", ["corners"],
                    "\"International standards were agreed for the size of containers and the fittings at their corners.\""),
                Gap(22, "New York: activity moved to new terminals in ___", ["New Jersey"],
                    "\"Activity moved from the historic piers of Manhattan and Brooklyn to new terminals across the harbour in New Jersey.\""),
                Gap(23, "Britain: a small port at ___ became the busiest container port", ["Felixstowe"],
                    "\"A small port at Felixstowe on the east coast grew into the busiest container port in Britain.\""),
            ]),
            new("mcq", Abcd, null,
            [
                Mcq(24, "What does the writer say about the effect of containers on dock employment?",
                    ["Many dock jobs disappeared within a generation.", "Dockworkers were retrained to operate cranes.", "Container terminals needed more workers than traditional ports.", "Only ports in Britain lost jobs."], "A",
                    "\"In port cities around the world, tens of thousands of dock jobs disappeared within a generation.\""),
                Mcq(25, "According to some economists, containerisation",
                    ["was less important than trade agreements.", "reduced the number of countries involved in manufacturing.", "made supply chains simpler.", "increased trade more than trade agreements did."], "D",
                    "\"Containerisation did more to increase international trade in the late twentieth century than all the trade agreements signed during the same period.\""),
                Mcq(26, "Which problem with the largest container ships is mentioned?",
                    ["They use too much fuel.", "Only a limited number of ports can receive them.", "They are too slow.", "They can carry only a few hundred containers."], "B",
                    "\"Only a limited number of ports are deep enough to receive them.\""),
            ]),
        ]);

    private static ReadingPassage Ra05P3() => new("Rethinking the Zoo",
        """
        Few institutions have changed as much over the past century as the zoo. The menageries of the nineteenth century were designed to entertain, displaying as many exotic animals as possible in small cages with bare floors. Today's leading zoos describe themselves quite differently, as centres for conservation, education and scientific research, and many have replaced cages with large enclosures that imitate natural habitats. Critics, however, argue that these changes are largely cosmetic and that keeping wild animals in captivity can no longer be justified. Having considered both sides, I believe that zoos can be justified, but only some zoos, and only for some animals.

        The strongest argument in favour of zoos is their role in saving species from extinction. Several animals exist today only because they were bred in captivity. The Arabian oryx, a desert antelope, had been hunted to extinction in the wild by the early 1970s, but a small number of animals held in zoos formed the basis of a breeding programme, and their descendants were later returned to the wild in several countries in the Middle East. Similarly, by 1987 every remaining California condor had been captured, when fewer than thirty of the birds survived; today, several hundred of them fly free again. Examples like these show that captive breeding, carefully managed, can make the difference between survival and extinction.

        Critics reply that such successes are rare. Most animals kept in zoos are not endangered at all, and only a small proportion of the species in zoos take part in programmes to return animals to the wild. They also point out that many zoos spend only a small share of their income directly on conservation. These criticisms are fair, and I do not think zoos can claim credit for conservation simply because a few of them have run successful programmes. But the argument cuts both ways: it suggests not that zoos should be abolished, but that they should be judged by what they actually do.

        The educational case is weaker than zoos often claim. Zoos argue that seeing a living animal creates an emotional connection that no film or book can match, and that this encourages visitors to support conservation. There is some evidence that visits increase visitors' knowledge, at least in the short term, but it is much harder to show that they lead to lasting changes in behaviour. In my experience, many visitors spend less than a minute at each enclosure, and a visit is often more of a family day out than a lesson. That is not necessarily a bad thing, but it should make us cautious about using education as the main justification for keeping animals in captivity.

        The most serious issue is animal welfare. Some animals appear to adapt well to life in captivity, breeding successfully and living longer than they would in the wild. Others clearly do not. Research has found that carnivores which travel long distances in the wild, such as polar bears, are particularly likely to show signs of distress in zoos, including repetitive pacing along the same path, and that their young die at higher rates. Elephants, which in the wild live in large family groups and walk many kilometres a day, were found in one large study to live considerably shorter lives in European zoos than in a protected area in Africa. In my view, no educational or entertainment value can justify keeping animals whose basic needs a zoo cannot meet.

        What would a justified zoo look like? First, it would keep fewer species, choosing those that thrive in captivity and those for which captive breeding makes a real contribution to conservation. Secondly, it would devote a significant and publicly reported part of its budget to protecting habitats in the wild, since breeding animals has little point if there is nowhere left to release them. Thirdly, it would share its knowledge: zoos have helped researchers learn about animal reproduction, nutrition and disease, and some of this knowledge is now used to protect wild populations.

        Some people argue that sanctuaries, which take in animals that cannot survive in the wild and do not breed them for display, offer a better model. For certain animals, such as elephants or great apes rescued from poor conditions, I think they do. But sanctuaries generally do not run the kind of coordinated breeding programmes that saved the oryx and the condor, and they depend heavily on donations. A world with only sanctuaries would probably have fewer species in it.

        The question, then, is not whether zoos as a whole are justified, but which zoos and which animals. The best modern zoos have earned their place by making genuine contributions to conservation and science. Those that keep animals mainly to attract visitors, without being able to give them a decent life, have not. It is the responsibility of zoo associations, of the authorities that license zoos and ultimately of visitors themselves, through the choices they make about where to spend their money, to tell the difference.
        """,
        [
            new("gap", Ra26Sent2, 2,
            [
                Gap(27, "Nineteenth-century menageries kept animals in small cages with ___.", ["bare floors"],
                    "\"Displaying as many exotic animals as possible in small cages with bare floors.\""),
                Gap(28, "The Arabian oryx is a kind of desert ___.", ["antelope"],
                    "\"The Arabian oryx, a desert antelope, had been hunted to extinction in the wild.\""),
                Gap(29, "By 1987, every surviving California ___ had been taken into captivity.", ["condor"],
                    "\"By 1987 every remaining California condor had been captured.\""),
                Gap(30, "Only a small proportion of the species in zoos take part in programmes to return animals to the ___.", ["wild"],
                    "\"Only a small proportion of the species in zoos take part in programmes to return animals to the wild.\""),
            ]),
            new("ynng", YnngInstructions, null,
            [
                Stmt(31, "Zoos should be judged by what they actually do.", "YES",
                    "\"It suggests not that zoos should be abolished, but that they should be judged by what they actually do.\""),
                Stmt(32, "It has been clearly shown that zoo visits change visitors' behaviour in the long term.", "NO",
                    "\"It is much harder to show that they lead to lasting changes in behaviour.\""),
                Stmt(33, "Polar bears breed more successfully in modern zoos than in older ones.", "NOT GIVEN",
                    "The writer says polar bears' young die at higher rates in zoos but does not compare modern and older zoos."),
                Stmt(34, "A justified zoo would keep fewer species than many zoos do now.", "YES",
                    "\"First, it would keep fewer species, choosing those that thrive in captivity.\""),
                Stmt(35, "Sanctuaries are a better option than zoos for every kind of animal.", "NO",
                    "\"For certain animals ... I think they do. But sanctuaries generally do not run the kind of coordinated breeding programmes ... A world with only sanctuaries would probably have fewer species in it.\""),
                Stmt(36, "Sanctuaries receive more money from donations than zoos do.", "NOT GIVEN",
                    "The writer says sanctuaries depend heavily on donations but does not compare the amount with zoos."),
            ]),
            new("mcq", Abcd, null,
            [
                Mcq(37, "Why does the writer mention the Arabian oryx and the California condor?",
                    ["to show that captive breeding can prevent extinction", "to show that most zoo animals are endangered", "to criticise zoos for keeping rare animals", "to compare desert animals with birds"], "A",
                    "\"Examples like these show that captive breeding, carefully managed, can make the difference between survival and extinction.\""),
                Mcq(38, "What is the writer's opinion of the educational argument for zoos?",
                    ["It is weaker than zoos often claim.", "It is the strongest argument for zoos.", "It has been proved wrong by research.", "It applies only to children."], "A",
                    "\"The educational case is weaker than zoos often claim.\""),
                Mcq(39, "According to research, carnivores that travel long distances in the wild",
                    ["adapt well to life in zoos.", "live longer in zoos than in the wild.", "are rarely kept in modern zoos.", "often show repetitive pacing in zoos."], "D",
                    "\"Are particularly likely to show signs of distress in zoos, including repetitive pacing along the same path.\""),
                Mcq(40, "What is the writer's overall conclusion?",
                    ["All zoos should be closed.", "Some zoos are justified, but only for some animals.", "Zoos are justified whatever animals they keep.", "Sanctuaries should replace zoos."], "B",
                    "\"The question, then, is not whether zoos as a whole are justified, but which zoos and which animals.\""),
            ]),
        ]);

    // =====================================================================
    // TEST 6
    // =====================================================================

    private static ReadingPassage Ra06P1() => new("Why We Laugh",
        """
        Laughter is one of the most familiar human sounds, yet until recently it received surprisingly little attention from scientists. Philosophers had long debated why certain things strike us as funny, but they mostly treated laughter as a response to humour, a kind of reward for understanding a joke. Research over the past few decades has shown that this view is, at best, incomplete. Laughter turns out to be older than language, far more social than we assume and only occasionally connected with anything funny.

        One of the pioneers in this field was the American psychologist Robert Provine, who decided to study laughter in the places where it naturally occurs. With a team of students, he listened in on conversations in shopping centres, on university campuses and in other public places, noting when laughter happened and what came just before it. The results were unexpected. Only a small minority of the laughs, between ten and twenty per cent, followed anything resembling a joke. Most came after perfectly ordinary remarks such as 'I'll see you later' or 'Are you sure?'. Provine also found that speakers laughed more than their listeners, which is hard to explain if laughter is simply a reaction to something amusing.

        Provine's other key finding concerned company. People, he reported, are around thirty times more likely to laugh when they are with others than when they are alone, and when people do laugh alone, it is often in response to something with a social element, such as a television programme or a message from a friend. This suggests that laughter is above all a form of communication: a signal that tells others we are relaxed, friendly or in agreement, rather than an automatic response to a clever idea.

        The origins of laughter appear to lie deep in our evolutionary past. Chimpanzees, gorillas and other great apes produce a breathy, panting sound when they are tickled or during rough play, and researchers who compared these sounds across species found that they share a common pattern with human laughter, suggesting that it evolved from the play sounds of our shared ancestors. Even rats produce high-pitched chirping sounds when they are tickled by humans, although these are too high for the human ear to detect without special equipment. In both apes and rats, the sounds seem to signal that rough behaviour is playful rather than aggressive.

        Human laughter differs from that of apes in one important respect. When chimpanzees laugh, they typically make one sound as they breathe in and another as they breathe out. Humans, by contrast, laugh only on the out-breath, chopping a single breath into a series of short bursts, the familiar 'ha-ha-ha', each lasting about a fifth of a second. Some researchers believe that this greater control over breathing, which also made speech possible, is why human laughter sounds so different. Babies usually begin to laugh at around three or four months, long before they can speak.

        Laughter is also highly contagious. Simply hearing someone else laugh makes us more likely to laugh ourselves, and brain imaging studies have shown that the sound of laughter activates areas of the brain involved in preparing facial movements, as if the listener were getting ready to join in. Television producers have taken advantage of this for decades by adding recorded laughter to comedy programmes, and studies suggest that audiences do rate jokes as funnier when they are accompanied by it. In extreme cases, laughter can spread through a whole community: in 1962, an outbreak of uncontrollable laughing began at a girls' school in what is now Tanzania and spread to nearby villages, forcing several schools to close temporarily.

        Not all laughter is the same, however. The British neuroscientist Sophie Scott and her colleagues have distinguished between spontaneous laughter, which we cannot easily control, and the deliberate, polite laughter we produce in conversation to show agreement or to ease an awkward moment. The two sound different and involve different patterns of brain activity, and listeners are generally quite good at telling them apart. Studies of couples have found that partners who laugh together while discussing a problem tend to feel less stressed and more satisfied with their relationship.

        Laughter may also bring physical benefits. In one set of experiments, the evolutionary psychologist Robin Dunbar and his colleagues found that people who had watched comedy videos in a group could tolerate more pain afterwards than those who had watched documentaries. The researchers suggested that laughter triggers the release of endorphins, chemicals produced by the body that reduce pain and create a sense of well-being. Since endorphins also appear to strengthen social bonds, Dunbar argues that laughter may have allowed early humans to maintain relationships in larger groups than would have been possible through physical contact such as grooming alone. Claims that laughter can cure illness should be treated with caution, but its value as a social tool is now well established.
        """,
        [
            new("mcq", Abcd, null,
            [
                Mcq(1, "According to the first paragraph, philosophers mainly regarded laughter as",
                    ["a form of communication.", "a sign of good health.", "a response to humour.", "an ability shared with animals."], "C",
                    "\"They mostly treated laughter as a response to humour, a kind of reward for understanding a joke.\""),
                Mcq(2, "How did Provine collect his data?",
                    ["by asking people to keep diaries", "by showing comedy films in a laboratory", "by recording television audiences", "by listening to conversations in public places"], "D",
                    "\"He listened in on conversations in shopping centres, on university campuses and in other public places.\""),
                Mcq(3, "What did Provine find about speakers and listeners?",
                    ["Listeners laughed more than speakers.", "Speakers laughed more than listeners.", "Both laughed equally often.", "Only listeners laughed at jokes."], "B",
                    "\"Provine also found that speakers laughed more than their listeners.\""),
                Mcq(4, "Provine's findings suggest that laughter is mainly",
                    ["a reaction to clever ideas.", "something people do when alone.", "a response to television.", "a way of communicating with others."], "D",
                    "\"This suggests that laughter is above all a form of communication.\""),
            ]),
            new("tfng", TfngInstructions, null,
            [
                Stmt(5, "Great apes make laugh-like sounds during rough play.", "TRUE",
                    "\"Great apes produce a breathy, panting sound when they are tickled or during rough play.\""),
                Stmt(6, "Humans can hear the chirping of tickled rats without special equipment.", "FALSE",
                    "\"These are too high for the human ear to detect without special equipment.\""),
                Stmt(7, "Chimpanzees laugh only when breathing out.", "FALSE",
                    "\"When chimpanzees laugh, they typically make one sound as they breathe in and another as they breathe out.\""),
                Stmt(8, "Babies begin to laugh before they are able to speak.", "TRUE",
                    "\"Babies usually begin to laugh at around three or four months, long before they can speak.\""),
                Stmt(9, "Recorded laughter was first used in radio programmes.", "NOT GIVEN",
                    "The passage mentions television producers adding recorded laughter, but not where it was first used."),
            ]),
            new("gap", Ra26Sent1, 1,
            [
                Gap(10, "The sound of laughter activates brain areas involved in preparing ___ movements.", ["facial"],
                    "\"Activates areas of the brain involved in preparing facial movements.\""),
                Gap(11, "In 1962, an outbreak of laughing began at a school in what is now ___.", ["Tanzania"],
                    "\"An outbreak of uncontrollable laughing began at a girls' school in what is now Tanzania.\""),
                Gap(12, "People who had watched comedy in a group could afterwards tolerate more ___.", ["pain"],
                    "\"People who had watched comedy videos in a group could tolerate more pain afterwards.\""),
                Gap(13, "Laughter may cause the body to release chemicals called ___.", ["endorphins"],
                    "\"Laughter triggers the release of endorphins, chemicals produced by the body that reduce pain.\""),
            ]),
        ]);

    private static ReadingPassage Ra06P2() => new("The Roads of Rome",
        """
        In 312 BCE, the Roman official Appius Claudius Caecus began work on a road leading south-east from Rome to the city of Capua, about 200 kilometres away. The Via Appia, which still bears his name, was not the first road in Italy, but it was the first to be built by the Roman state as a planned, durable route, and it set a pattern that would be repeated across three continents. At its greatest extent, the Roman Empire was linked by tens of thousands of kilometres of major roads, many of them paved, stretching from northern Britain to the deserts of Syria.

        The primary purpose of these roads was military. A network of reliable routes allowed armies to move quickly to any part of the empire where they were needed, whatever the season, and much of the construction was carried out by soldiers themselves between campaigns. But the roads soon served many other purposes. Officials, merchants and ordinary travellers used them, and the state established a courier service with stations at regular intervals, where messengers could change horses or find a place to spend the night. Using this system, an important message could travel perhaps 80 kilometres in a day, and considerably further in an emergency.

        The first stage in building a road was surveying. Roman roads are famous for their straightness, and over open country they often ran in straight lines for many kilometres. To achieve this, surveyors used an instrument called the groma, which consisted of a vertical pole with a horizontal cross at the top; weighted cords hanging from each arm of the cross allowed the surveyor to sight straight lines and right angles. Fires or tall poles may have been used as markers over longer distances. Straightness, however, was never pursued at any cost. Where the land was steep, engineers were quite willing to let a road bend in order to keep the slope gentle enough for loaded carts and animals.

        Once the route was marked, workers dug a trench, sometimes down to solid rock, and filled it with several layers of material. Textbooks often describe a standard structure of four layers, beginning with large stones at the bottom and ending with a surface of fitted paving slabs, based largely on the writings of the Roman architect Vitruvius. Vitruvius, however, was describing floors, not roads, and archaeologists have found that construction varied enormously from place to place, depending on the local materials and the importance of the road. Near Rome, the most important roads were surfaced with large, many-sided blocks of hard volcanic stone, carefully fitted together. In the provinces, many roads had a surface of compacted gravel, which was cheaper to build and easier to repair.

        Whatever the materials, Roman engineers paid close attention to drainage, which they understood to be the key to a road's survival. Water that soaks into a road weakens its foundations and, in cold regions, freezes and breaks up the surface. Roman roads were therefore usually built higher than the surrounding land, often on a raised bank of earth, and the surface was curved, higher in the middle than at the edges, so that rainwater ran off to either side. Ditches along the sides carried the water away. Where roads crossed rivers, the Romans built bridges of stone, some of which are still in use today.

        Along the major roads stood milestones, stone columns often around two metres tall, which recorded the distance to the nearest town or to the start of the road. The Roman mile was defined as one thousand paces, each pace being a double step, and measured roughly 1,480 metres. Milestones often also recorded the name of the emperor or official responsible for building or repairing the road, which made them a form of publicity as well as a guide for travellers. In the centre of Rome, the emperor Augustus set up a gilded column known as the Golden Milestone, from which distances to the cities of the empire were said to be measured.

        Maintaining such a network was a constant task. Local communities were often required to contribute labour or money for repairs, and inscriptions record emperors taking credit for restoring roads that had fallen into poor condition. After the western empire collapsed in the fifth century, this system of maintenance came to an end, and many roads gradually deteriorated. Their stones were frequently taken away to be used in other buildings. Yet the routes themselves often survived. Many modern roads in Europe follow the lines laid down by Roman engineers, and in a number of places travellers still drive, or walk, over the very stones that Roman workers placed there nearly two thousand years ago.
        """,
        [
            new("mcq", Abcd, null,
            [
                Mcq(14, "What made the Via Appia different from earlier roads in Italy?",
                    ["It was the longest road in the empire.", "It was the first planned, durable road built by the Roman state.", "It was the first road to reach Capua.", "It was built entirely by soldiers."], "B",
                    "\"It was the first to be built by the Roman state as a planned, durable route.\""),
                Mcq(15, "According to the passage, the main purpose of Roman roads was to",
                    ["allow armies to move quickly.", "help merchants transport goods.", "carry messages between cities.", "encourage ordinary people to travel."], "A",
                    "\"The primary purpose of these roads was military. A network of reliable routes allowed armies to move quickly.\""),
                Mcq(16, "Why did Roman engineers sometimes allow a road to bend?",
                    ["to avoid private land", "to pass through towns", "to keep the slope gentle on steep ground", "to follow the course of rivers"], "C",
                    "\"Where the land was steep, engineers were quite willing to let a road bend in order to keep the slope gentle.\""),
                Mcq(17, "What does the writer say about the common description of four layers?",
                    ["It is based on writing about floors, and real roads varied greatly.", "It is supported by most archaeological evidence.", "It applies only to roads in the provinces.", "It was invented by modern engineers."], "A",
                    "\"Vitruvius, however, was describing floors, not roads, and archaeologists have found that construction varied enormously from place to place.\""),
            ]),
            new("gap", Ra26Notes1, 1,
            [
                Gap(18, "Surveying instrument: the ___, a vertical pole with a cross at the top", ["groma"],
                    "\"Surveyors used an instrument called the groma, which consisted of a vertical pole with a horizontal cross at the top.\""),
                Gap(19, "Surface near Rome: many-sided blocks of hard ___ stone", ["volcanic"],
                    "\"Surfaced with large, many-sided blocks of hard volcanic stone.\""),
                Gap(20, "Surface in the provinces: often compacted ___", ["gravel"],
                    "\"In the provinces, many roads had a surface of compacted gravel.\""),
                Gap(21, "Curved surface: allowed ___ to run off to either side", ["rainwater"],
                    "\"The surface was curved ... so that rainwater ran off to either side.\""),
                Gap(22, "Sides of the road: ___ carried the water away", ["ditches"],
                    "\"Ditches along the sides carried the water away.\""),
            ]),
            new("tfng", Ra26Tfng(2), null,
            [
                Stmt(23, "Some bridges built by the Romans are still used today.", "TRUE",
                    "\"The Romans built bridges of stone, some of which are still in use today.\""),
                Stmt(24, "Milestones gave travellers information about distance only.", "FALSE",
                    "\"Milestones often also recorded the name of the emperor or official responsible for building or repairing the road.\""),
                Stmt(25, "Local communities were sometimes expected to help pay for road repairs.", "TRUE",
                    "\"Local communities were often required to contribute labour or money for repairs.\""),
                Stmt(26, "Roman roads in Britain were maintained for longer than those in Italy.", "NOT GIVEN",
                    "The passage says maintenance ended after the western empire collapsed, but does not compare Britain and Italy."),
            ]),
        ]);

    private static ReadingPassage Ra06P3() => new("The Pen and the Keyboard",
        """
        Not long ago, a colleague showed me a birthday card written by her teenage son. The message was only two lines long, but it had clearly cost him considerable effort, and parts of it were barely legible. He was not a weak student; he simply did almost all his writing on a keyboard, at school as well as at home. His case is increasingly typical, and it raises a question that schools in many countries are now facing: in a world of screens, should children still be taught to write by hand, and if so, how much?

        Those who say no have some strong arguments. Almost all writing in adult working life is now done on computers or phones, and typing is faster for most practised users. Typed text is easy to edit, share and search, and it is always legible. Some countries have already adjusted their curriculum; in Finland, for example, schools stopped being required to teach joined-up handwriting in 2016, and more attention was given to keyboard skills instead. For pupils with physical conditions that make handwriting slow or painful, keyboards can remove a barrier that has held such pupils back for generations.

        I accept most of these points, but I believe that abandoning handwriting, particularly in the early years of school, would be a mistake. The strongest evidence concerns young children learning to read. In a number of studies, children who practised forming letters by hand later recognised those letters more readily than children who practised by typing or tracing them. Brain imaging research has found that writing letters by hand activates regions of the brain associated with reading in a way that typing does not. The likely explanation is that producing a letter by hand requires the child to pay attention to its shape, whereas pressing a key requires only that the child find the right one.

        The evidence for older students is more mixed, and I think it has sometimes been exaggerated. A widely reported study published in 2014 found that university students who took lecture notes by hand performed better on questions requiring conceptual understanding than those who used laptops. The researchers argued that because typing is fast, laptop users tended to copy down the lecturer's words almost exactly, while those writing by hand were forced to summarise, and so processed the material more deeply. The study was influential, and some lecturers banned laptops from their classrooms as a result. However, later attempts to repeat the experiment have produced weaker and less consistent results.

        My own reading of this research is that the tool matters less than the way it is used. A student who types a thoughtful summary will probably learn as much as one who writes it by hand, and a student who copies slowly by hand gains little. What the laptop does do is make mindless copying easier, and it brings the constant temptation of messages and other distractions. The most useful lesson for students, then, is not 'always use a pen' but 'think before you write', and that can be taught whatever the device.

        What of the traditional joined-up, or cursive, style that many adults were taught at school? Here I find myself less sympathetic to the defenders of handwriting. Some argue that cursive has special benefits for the brain or for spelling, but I have not seen convincing evidence that it is superior to a clear, fluent printed style. The goal should be handwriting that is legible and fast enough to be useful, not a particular style of letter. Time spent perfecting elegant loops might be better spent on other skills.

        Typing, meanwhile, deserves to be taught properly, rather than being left for children to pick up on their own. Many pupils who have grown up with touchscreens type with two fingers and look at the keyboard constantly, which slows them down and interrupts their thinking. Systematic teaching of keyboard skills from the middle years of primary school would help pupils to write at length on computers, which is how most of their future writing, and in some countries their examinations, will be done.

        The debate between pen and keyboard is often presented as a battle between tradition and progress, but this framing does not help. Each tool has advantages for different purposes and at different stages of learning. Young children should learn to form letters by hand, because it helps them to read. Older pupils should learn to type efficiently, because that is how they will write. And everyone should be able to write a birthday card that its recipient can actually read.
        """,
        [
            new("mcq", Abcd, null,
            [
                Mcq(27, "Why does the writer mention the birthday card in the first paragraph?",
                    ["to show that teenagers rarely write cards", "to criticise the colleague's son", "to prove that typing is harmful", "to illustrate a situation that is becoming common"], "D",
                    "\"His case is increasingly typical, and it raises a question that schools in many countries are now facing.\""),
                Mcq(28, "What happened in Finland in 2016?",
                    ["Handwriting lessons were removed from all schools.", "Keyboards were banned in primary schools.", "Schools no longer had to teach joined-up handwriting.", "Pupils began taking all examinations on computers."], "C",
                    "\"In Finland ... schools stopped being required to teach joined-up handwriting in 2016.\""),
                Mcq(29, "According to the writer, why does handwriting help children to recognise letters?",
                    ["It is slower than typing.", "It is more enjoyable for young children.", "It requires attention to the shape of each letter.", "It involves tracing letters many times."], "C",
                    "\"Producing a letter by hand requires the child to pay attention to its shape, whereas pressing a key requires only that the child find the right one.\""),
            ]),
            new("ynng", YnngInstructions, null,
            [
                Stmt(30, "Keyboards can help pupils who find handwriting physically difficult.", "YES",
                    "\"For pupils with physical conditions that make handwriting slow or painful, keyboards can remove a barrier.\""),
                Stmt(31, "The findings of the 2014 study have been fully confirmed by later research.", "NO",
                    "\"Later attempts to repeat the experiment have produced weaker and less consistent results.\""),
                Stmt(32, "Students should be taught to take notes in shorthand.", "NOT GIVEN",
                    "The writer discusses note-taking by hand and by laptop but never mentions shorthand."),
                Stmt(33, "Laptops make it easier for students to copy without thinking.", "YES",
                    "\"What the laptop does do is make mindless copying easier.\""),
                Stmt(34, "Schools should spend time teaching children to produce elegant joined-up writing.", "NO",
                    "\"The goal should be handwriting that is legible and fast enough to be useful, not a particular style of letter. Time spent perfecting elegant loops might be better spent on other skills.\""),
                Stmt(35, "Using touchscreens has made children's handwriting worse.", "NOT GIVEN",
                    "The writer links touchscreens to poor typing habits (two fingers), not to handwriting quality."),
            ]),
            new("gap", Ra26Sent2, 2,
            [
                Gap(36, "In the 2014 study, students writing by hand were forced to ___, so they processed the material more deeply.", ["summarise", "summarize"],
                    "\"Those writing by hand were forced to summarise, and so processed the material more deeply.\""),
                Gap(37, "The writer's advice to students is to ___ before they write.", ["think"],
                    "\"The most useful lesson for students, then, is not 'always use a pen' but 'think before you write'.\""),
                Gap(38, "Handwriting should be legible and ___ enough to be useful.", ["fast"],
                    "\"The goal should be handwriting that is legible and fast enough to be useful.\""),
                Gap(39, "Many pupils who grew up with touchscreens type with only ___ fingers.", ["two", "2"],
                    "\"Many pupils who have grown up with touchscreens type with two fingers.\""),
                Gap(40, "Keyboard skills should be taught systematically from the ___ of primary school.", ["middle years"],
                    "\"Systematic teaching of keyboard skills from the middle years of primary school.\""),
            ]),
        ]);
}
