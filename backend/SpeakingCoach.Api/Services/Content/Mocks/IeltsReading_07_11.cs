using SpeakingCoach.Api.Services.Mock;

namespace SpeakingCoach.Api.Services.Content;

// Tayyor IELTS Academic Reading testlari 7–11. Original matn va savollar (haqiqiy
// imtihondan ko'chirilmagan); tuzilishi ReadingAcademic1 bilan bir xil: 3 matn (13 + 13 + 14).
public static partial class BuiltInIelts
{
    public static readonly ReadingTest ReadingAcademic7 = new(IeltsBank.Academic, [Ra7P1(), Ra7P2(), Ra7P3()]);
    public static readonly ReadingTest ReadingAcademic8 = new(IeltsBank.Academic, [Ra8P1(), Ra8P2(), Ra8P3()]);
    public static readonly ReadingTest ReadingAcademic9 = new(IeltsBank.Academic, [Ra9P1(), Ra9P2(), Ra9P3()]);
    public static readonly ReadingTest ReadingAcademic10 = new(IeltsBank.Academic, [Ra10P1(), Ra10P2(), Ra10P3()]);
    public static readonly ReadingTest ReadingAcademic11 = new(IeltsBank.Academic, [Ra11P1(), Ra11P2(), Ra11P3()]);

    // Matn raqami ko'rsatmada aniq bo'lishi uchun (TFNG 1- yoki 2-matnda, YNNG 3-matnda).
    private static string Ra711Tfng(int passage) =>
        $"Do the following statements agree with the information given in Reading Passage {passage}?\n" +
        "TRUE — if the statement agrees with the information\nFALSE — if the statement contradicts the information\nNOT GIVEN — if there is no information on this";

    private static string Ra711Ynng(int passage) =>
        $"Do the following statements agree with the views of the writer in Reading Passage {passage}?\n" +
        "YES — if the statement agrees with the views of the writer\nNO — if the statement contradicts the views of the writer\nNOT GIVEN — if it is impossible to say what the writer thinks about this";

    // =====================================================================
    // TEST 7 — monarch kapalaklari; sovutish tarixi; ommaviy turizm (fikr)
    // =====================================================================

    private static ReadingPassage Ra7P1() => new("The Monarch's Long Journey",
        """
        Every autumn, one of the most extraordinary journeys in the animal kingdom begins in the fields and gardens of the United States and southern Canada. Millions of monarch butterflies, each weighing less than a gram, set off towards the south-west. Some will fly more than 4,000 kilometres before they reach their destination: a small number of forested mountain slopes in central Mexico, where they will spend the winter packed together in their millions. The monarch is often described as the only butterfly that makes a regular two-way migration in the way that many birds do.

        For much of the twentieth century, it was known that monarchs left the north each autumn and returned in spring, but no one could say where they spent the months in between. The Canadian zoologist Fred Urquhart devoted several decades to the problem. In the 1950s he began a tagging programme, recruiting thousands of volunteers to attach small paper labels to the wings of butterflies and to report any tagged insects they found. Slowly, the reports traced a route leading towards Mexico. The final breakthrough came in 1975, when two volunteers working with Urquhart, following local information, found a mountain forest in which the trees were covered with butterflies. The discovery was announced to the public the following year, and scientists soon identified a dozen or so similar sites in the same mountain range.

        The overwintering sites share certain features. They lie at around 3,000 metres above sea level, in forests of oyamel fir. At this altitude the air is cool enough to slow the butterflies' metabolism, so that they use up their stored fat very slowly, but it rarely becomes so cold that they freeze. The dense forest canopy acts rather like a blanket, protecting the insects from rain, snow and wind. The butterflies gather in such numbers that branches can bend under their weight, and they remain mostly inactive until the days lengthen in late winter.

        Perhaps the most surprising aspect of the migration is that no individual butterfly completes the whole cycle. Monarchs born in early summer live for only a few weeks, during which time they mate, lay eggs and die. The generation that emerges at the end of summer is different. These butterflies do not breed immediately; instead they store fat and can live for as long as eight months, long enough to fly to Mexico, survive the winter and begin the return journey. In spring they mate and lay eggs in the southern United States, and it is their children and grandchildren that continue northwards. As a result, the butterflies that arrive in Mexico each autumn are the great-grandchildren of those that left it the previous spring, and have never made the journey before.

        How, then, do they find their way? Research has shown that monarchs use the sun as a compass. Because the sun moves across the sky during the day, a compass of this kind is only useful if it is combined with a sense of time. In 2009, researchers made the unexpected discovery that the butterflies' clock is located not only in the brain but also in the antennae: insects whose antennae had been painted black, so that they could not detect light, flew in the wrong direction. Later experiments suggested that monarchs can also sense the Earth's magnetic field, which may help them keep to their course on cloudy days.

        The migration depends entirely on a group of plants known as milkweeds, the only food that monarch caterpillars can eat. Milkweed contains chemicals that are poisonous to many animals, and the caterpillars store these chemicals in their bodies. The adult butterflies therefore taste unpleasant, and their bright orange wings serve as a warning to birds that might otherwise eat them. However, the spread of intensive agriculture and the use of herbicides have removed milkweed from large areas of farmland, reducing the number of places where the butterflies can breed.

        The wintering grounds face pressures of their own. Illegal logging has thinned parts of the forest, leaving the butterflies more exposed to storms, and severe weather has killed large numbers in some years. Since 2008 the main area has been protected as a World Heritage Site, and local communities have been supported in developing tourism as an alternative source of income to cutting timber. Across North America, meanwhile, schools, farmers and gardeners have been encouraged to plant milkweed along roads and in parks. Scientists monitor the population by measuring the area of forest occupied by the butterflies each winter, since counting individual insects is impossible. The figures vary greatly from year to year, but the long-term trend has been downward, a reminder that even a journey refined over thousands of generations can be disrupted within a few decades.
        """,
        [
            new("tfng", Ra711Tfng(1), null,
            [
                Stmt(1, "Fred Urquhart's tagging programme depended on help from members of the public.", "TRUE",
                    "\"He began a tagging programme, recruiting thousands of volunteers to attach small paper labels to the wings of butterflies.\""),
                Stmt(2, "The butterflies' winter home in Mexico was discovered in the 1950s.", "FALSE",
                    "The tagging began in the 1950s, but \"the final breakthrough came in 1975\"."),
                Stmt(3, "Oyamel fir trees grow only in Mexico.", "NOT GIVEN",
                    "The passage says the sites are \"in forests of oyamel fir\" but says nothing about where else these trees grow."),
                Stmt(4, "Temperatures at the overwintering sites often fall low enough to freeze the butterflies.", "FALSE",
                    "\"It rarely becomes so cold that they freeze.\""),
                Stmt(5, "Monarchs that emerge at the end of summer live longer than those born in early summer.", "TRUE",
                    "Early-summer monarchs \"live for only a few weeks\", while the late-summer generation \"can live for as long as eight months\"."),
                Stmt(6, "Monarchs whose antennae had been painted black were still able to fly in the correct direction.", "FALSE",
                    "\"Insects whose antennae had been painted black ... flew in the wrong direction.\""),
                Stmt(7, "Birds that eat a monarch butterfly usually die as a result.", "NOT GIVEN",
                    "The passage says adult monarchs \"taste unpleasant\" and their wings are a warning, but not what happens to a bird that eats one."),
            ]),
            new("gap", "Complete the sentences below.\nChoose NO MORE THAN TWO WORDS from the passage for each answer.", 2,
            [
                Gap(8, "The forest canopy protects the wintering butterflies in the same way as a ___.", ["blanket"],
                    "\"The dense forest canopy acts rather like a blanket.\""),
                Gap(9, "The monarchs that reach Mexico each autumn are the ___ of the butterflies that left in spring.", ["great-grandchildren", "great grandchildren"],
                    "\"The butterflies that arrive in Mexico each autumn are the great-grandchildren of those that left it the previous spring.\""),
                Gap(10, "A sun compass can only work if the butterfly also has a sense of ___.", ["time"],
                    "\"A compass of this kind is only useful if it is combined with a sense of time.\""),
                Gap(11, "Because of the chemicals from milkweed, adult monarchs taste ___.", ["unpleasant"],
                    "\"The adult butterflies therefore taste unpleasant.\""),
                Gap(12, "Local communities have been helped to develop ___ so that they do not need to cut timber.", ["tourism"],
                    "\"Local communities have been supported in developing tourism as an alternative source of income to cutting timber.\""),
                Gap(13, "Scientists estimate the population by measuring the ___ of forest the butterflies occupy.", ["area"],
                    "\"Scientists monitor the population by measuring the area of forest occupied by the butterflies each winter.\""),
            ]),
        ]);

    private static ReadingPassage Ra7P2() => new("Keeping Things Cold",
        """
        For most of human history, keeping food fresh was a constant struggle. Meat, fish and milk spoil quickly in warm weather, and people relied on methods such as drying, salting, smoking and pickling to make them last. Where it was available, natural cold offered another solution. In many parts of the world, people cut ice from frozen lakes and rivers in winter and stored it in deep pits or specially built ice houses, insulated with straw or sawdust, where it could last well into the summer. In the dry regions of ancient Persia, builders went further, constructing tall domed structures in which ice made in shallow pools on winter nights could be kept for months.

        In the early nineteenth century, ice became an international business. In 1806, a young Boston merchant named Frederic Tudor sent a shipload of ice to the Caribbean island of Martinique. The venture was a financial failure: much of the cargo melted, and local people had little idea what to do with the rest. Tudor, however, persisted. He experimented with different kinds of insulation, built ice houses in the ports he supplied and, crucially, created demand by persuading bar owners to serve cold drinks. By the 1830s his ships were carrying ice as far as India, and ice cut from the lakes of New England had become a major export. Tudor became known as the "Ice King".

        Natural ice, however, had obvious limitations. Its supply depended on the weather, and a mild winter could cause severe shortages. Scientists had known for some time that cold could be produced artificially. In the mid-eighteenth century, William Cullen, a professor in Glasgow, showed that when a liquid evaporates rapidly it absorbs heat from its surroundings, and he used this effect to produce a small amount of ice. The principle is the same one that makes us feel cold when we step out of a swimming pool: as the water on our skin evaporates, it draws heat from the body. Cullen's demonstration, though, had no practical use at the time.

        The key step was to find a way of reusing the evaporating liquid rather than letting it escape. In a vapour-compression system, the liquid, known as a refrigerant, evaporates inside pipes and absorbs heat; a pump then compresses the vapour, turning it back into a liquid and releasing the heat elsewhere, and the cycle begins again. An American inventor, Jacob Perkins, patented a machine working on this principle in 1834. In the following decade, John Gorrie, a doctor in Florida, built a machine to cool the rooms of patients suffering from fever. His attempts to find investors failed, however, and he died without seeing his invention succeed.

        Commercial success came in the second half of the century, and breweries were among the first major customers. The German engineer Carl von Linde designed more efficient machines, initially for brewers who needed to keep beer cool all year round. Refrigeration also transformed international trade in food. In 1882 the sailing ship Dunedin arrived in London from New Zealand after a voyage of more than three months, carrying a cargo of frozen meat that was still in good condition. Within a few decades, refrigerated ships were carrying meat, butter and fruit across the world.

        Refrigerators for the home appeared in the early twentieth century, but they were expensive and had a serious drawback: the refrigerants they used, such as ammonia and sulphur dioxide, were toxic, and leaks could be dangerous. In the late 1920s, chemists developed a new group of compounds, chlorofluorocarbons or CFCs, which were neither poisonous nor flammable. They seemed ideal, and domestic refrigerators spread rapidly. Only in the 1970s did scientists discover that CFCs released into the atmosphere were damaging the ozone layer, which protects the Earth from harmful ultraviolet radiation. An international agreement signed in 1987 led to CFCs being phased out, although some of the gases that replaced them have since been found to contribute strongly to global warming.

        The domestic refrigerator changed daily life in ways that are easy to overlook. Families no longer needed to buy fresh food every day, and shops could sell a far wider range of products. Diets changed too, as fruit, vegetables and dairy products became available throughout the year. Today, the "cold chain" of refrigerated warehouses, lorries and shops is essential not only for food but also for medicines, including many vaccines, which must be kept within a narrow range of temperatures all the way from the factory to the patient.
        """,
        [
            new("mcq", Abcd, null,
            [
                Mcq(14, "Why was Tudor's first shipment of ice unsuccessful?",
                    ["The ship arrived too late in the season.", "Local merchants refused to buy the ice.", "Much of the ice melted and there was little demand for it.", "The ice was of poor quality."], "C",
                    "\"Much of the cargo melted, and local people had little idea what to do with the rest.\""),
                Mcq(15, "Which of the following helped Tudor's business to succeed?",
                    ["encouraging bars to serve cold drinks", "lowering the price of ice", "using faster ships", "support from the government"], "A",
                    "\"Crucially, [he] created demand by persuading bar owners to serve cold drinks.\""),
                Mcq(16, "What does the writer say about Cullen's demonstration?",
                    ["It showed a scientific principle but was not used in practice at the time.", "It produced large quantities of ice.", "It was immediately adopted by industry.", "It was carried out in a swimming pool."], "A",
                    "\"Cullen's demonstration, though, had no practical use at the time.\""),
                Mcq(17, "In a vapour-compression system, the refrigerant",
                    ["escapes into the air after use.", "is used over and over again.", "absorbs heat while it is being compressed.", "is heated before it evaporates."], "B",
                    "\"The key step was to find a way of reusing the evaporating liquid ... and the cycle begins again.\""),
                Mcq(18, "What happened to John Gorrie?",
                    ["He sold his machine to Jacob Perkins.", "He became rich from his invention.", "He was unable to find people to fund his invention.", "He built the first refrigerated ship."], "C",
                    "\"His attempts to find investors failed, however, and he died without seeing his invention succeed.\""),
                Mcq(19, "The voyage of the Dunedin was significant because it showed that",
                    ["sailing ships were faster than expected.", "New Zealand needed to import ice.", "breweries could export beer.", "frozen meat could survive a long sea journey."], "D",
                    "After more than three months at sea, it carried \"a cargo of frozen meat that was still in good condition\"."),
            ]),
            new("gap", "Complete the sentences below.\nChoose NO MORE THAN TWO WORDS from the passage for each answer.", 2,
            [
                Gap(20, "In ice houses, the ice was insulated with straw or ___.", ["sawdust"],
                    "\"Specially built ice houses, insulated with straw or sawdust.\""),
                Gap(21, "Frederic Tudor was given the nickname the ___.", ["Ice King"],
                    "\"Tudor became known as the 'Ice King'.\""),
                Gap(22, "Some of the first important customers for refrigeration machines were ___.", ["breweries"],
                    "\"Breweries were among the first major customers.\""),
                Gap(23, "The refrigerants used in early home refrigerators were ___ and could be dangerous if they leaked.", ["toxic"],
                    "\"The refrigerants they used, such as ammonia and sulphur dioxide, were toxic, and leaks could be dangerous.\""),
                Gap(24, "In the 1970s, CFCs were found to be damaging the ___.", ["ozone layer"],
                    "\"CFCs released into the atmosphere were damaging the ozone layer.\""),
                Gap(25, "With a refrigerator, families did not have to buy ___ every day.", ["fresh food"],
                    "\"Families no longer needed to buy fresh food every day.\""),
                Gap(26, "The network of refrigerated warehouses, lorries and shops is known as the ___.", ["cold chain"],
                    "\"The 'cold chain' of refrigerated warehouses, lorries and shops.\""),
            ]),
        ]);

    private static ReadingPassage Ra7P3() => new("Loved to Death?",
        """
        There is a particular kind of disappointment that many travellers will recognise. You arrive at a place you have dreamed of seeing for years, an ancient temple or a medieval town square, only to find it so crowded that you can barely see it at all. You shuffle forward in a queue, glimpse the famous view over a forest of raised phones, and leave with the uncomfortable feeling that you have been part of the problem. Over the past few decades, the number of international tourist trips has grown several times over, and a relatively small number of famous sites now receive a very large share of the visitors. The question of how to protect these places from the people who love them has become one of the most pressing in the heritage world.

        It is worth being clear about the damage that crowds can do. Some of it is physical. Millions of feet wear away stone steps and floors; the breath and body heat of visitors change the humidity inside painted tombs and caves, encouraging the growth of mould; and hands touch surfaces that were never meant to be touched. Some of the damage is less visible. In historic city centres, rising rents driven by short-term holiday lets can push out the residents whose daily lives gave the place its character, until what remains is a stage set populated by visitors and the people who serve them.

        Faced with these problems, it is tempting to call for simple solutions, and the simplest of all is to close the most vulnerable sites completely. In a few cases this is right. Some prehistoric painted caves have been closed to the public for decades, and visitors instead see carefully made replicas nearby. I have visited one of these replicas, and I was surprised by how moving the experience was. But closure cannot be a general answer. Heritage that nobody is allowed to see soon loses its public support, and with it the money needed to look after it. In many poorer countries, the income from tourism pays for conservation work that would otherwise not be done at all.

        A second popular solution is to raise prices sharply, on the grounds that fewer people will come if the ticket is expensive. I am unconvinced. High prices certainly reduce numbers, but they do so by excluding those with the least money, including many local people for whom the site is part of their own history. A world in which only the wealthy can visit the great monuments of human civilisation is not one we should be aiming for. If prices are to rise, there should at least be reduced or free entry for residents and students.

        The more promising approaches are less dramatic. Timed tickets, which require visitors to book a particular entry slot, have been introduced at several major sites and have made a real difference, spreading visitors more evenly through the day and ending the worst of the queues. They do require careful design, since people who cannot plan in advance, or who lack internet access, can easily be shut out. Daily limits on visitor numbers, based on scientific studies of how much wear a site can tolerate, are another sensible measure, and they are far easier to defend than limits based on guesswork.

        Just as important, in my view, is the effort to spread tourism more widely. Most countries have hundreds of historic places that receive very few visitors, and some of them are every bit as interesting as the famous sites. Tourist boards, travel writers and guidebooks all have a part to play here, by encouraging travellers to look beyond the same short list of destinations. Travelling outside the peak season helps too: a famous cathedral city in winter can be a quite different place from the same city in August.

        Finally, visitors themselves deserve both some credit and some responsibility. Most tourists are not careless vandals; they are curious people who have saved for a trip and want to experience something remarkable. When they are told why a rule exists, most of them will follow it. Too often, however, sites communicate only through signs saying "Do not touch" or "No photographs", without any explanation. A short, clear message about the damage done by flash photography or by touching ancient paint is likely to be more effective than any number of warnings.

        None of these measures will solve the problem on its own, and some famous places will remain crowded for the foreseeable future. But the aim should not be to keep people away from heritage. It should be to ensure that those who come can see it properly, that those who live there can continue to do so, and that the places themselves survive for the generations who have not yet had their turn.
        """,
        [
            new("ynng", Ra711Ynng(3), null,
            [
                Stmt(27, "Tourists who visit crowded sites may feel that they are contributing to the problem.", "YES",
                    "\"Leave with the uncomfortable feeling that you have been part of the problem.\""),
                Stmt(28, "All the damage caused by large numbers of visitors can easily be seen.", "NO",
                    "\"Some of the damage is less visible.\""),
                Stmt(29, "The replica cave the writer visited was a disappointing substitute for the original.", "NO",
                    "\"I was surprised by how moving the experience was.\""),
                Stmt(30, "In wealthy countries, tourism is the main source of money for conservation.", "NOT GIVEN",
                    "The writer mentions tourism income paying for conservation only \"in many poorer countries\"."),
                Stmt(31, "If ticket prices rise, residents and students should pay less or nothing.", "YES",
                    "\"If prices are to rise, there should at least be reduced or free entry for residents and students.\""),
                Stmt(32, "Timed tickets are more effective than daily limits on visitor numbers.", "NOT GIVEN",
                    "The writer supports both measures but never compares their effectiveness."),
            ]),
            new("mcq", Abcd, null,
            [
                Mcq(33, "What is the writer's view of closing heritage sites completely?",
                    ["It should happen at all famous sites.", "It always increases public support for heritage.", "It is suitable only in a small number of cases.", "It is cheaper than other solutions."], "C",
                    "\"In a few cases this is right ... But closure cannot be a general answer.\""),
                Mcq(34, "What is the writer's main objection to raising ticket prices sharply?",
                    ["It does not reduce visitor numbers.", "It is unfair to people with less money.", "It damages the local economy.", "It makes queues longer."], "B",
                    "\"They do so by excluding those with the least money, including many local people.\""),
                Mcq(35, "What problem with timed tickets does the writer mention?",
                    ["They may shut out people who cannot book in advance.", "They make visitors arrive too early.", "They are expensive to introduce.", "Visitors often ignore their entry times."], "A",
                    "\"People who cannot plan in advance, or who lack internet access, can easily be shut out.\""),
                Mcq(36, "What does the writer suggest about signs at heritage sites?",
                    ["There should be many more of them.", "They should explain the reasons for the rules.", "They should be replaced by guides.", "Most visitors do not read them."], "B",
                    "\"A short, clear message about the damage done ... is likely to be more effective than any number of warnings.\""),
            ]),
            new("gap", "Complete the sentences below.\nChoose NO MORE THAN TWO WORDS from the passage for each answer.", 2,
            [
                Gap(37, "Visitors' breath and body heat can encourage the growth of ___ in tombs and caves.", ["mould", "mold"],
                    "\"The breath and body heat of visitors change the humidity ... encouraging the growth of mould.\""),
                Gap(38, "In historic city centres, rising rents can force ___ to leave.", ["residents"],
                    "\"Rising rents driven by short-term holiday lets can push out the residents.\""),
                Gap(39, "Visiting outside the ___ is another way to reduce crowding.", ["peak season"],
                    "\"Travelling outside the peak season helps too.\""),
                Gap(40, "The writer believes that most tourists are curious people rather than careless ___.", ["vandals"],
                    "\"Most tourists are not careless vandals; they are curious people.\""),
            ]),
        ]);

    // =====================================================================
    // TEST 8 — hayvonlar uyqusi; mayoqlar tarixi; tibbiyotda sun'iy intellekt (fikr)
    // =====================================================================

    private static ReadingPassage Ra8P1() => new("The Many Ways of Sleeping",
        """
        Sleep is so familiar to us that it is easy to forget how strange it is. For several hours each day, an animal stops feeding, stops watching for danger and becomes largely unaware of its surroundings. From an evolutionary point of view this is a considerable risk, and if sleep did not serve some vital purpose, it is hard to imagine that natural selection would have allowed it to survive. Yet some form of sleep has been found in almost every animal that scientists have examined carefully, from mammals and birds to fish, insects and even worms.

        Defining sleep in animals very different from ourselves is not straightforward. In humans and other mammals, researchers can record the electrical activity of the brain and identify characteristic patterns. For animals such as insects, scientists rely instead on behaviour. An animal is considered to be asleep if it adopts a typical resting posture, becomes less responsive to its surroundings, and can be woken quickly by a strong enough disturbance. A further test is whether the animal makes up for lost rest: if it is kept awake, does it sleep longer or more deeply afterwards? Fruit flies deprived of sleep do exactly that, which is one reason why they have become important in sleep research. In 2017, researchers even reported sleep-like behaviour in a species of jellyfish, an animal with no brain at all.

        The amount of sleep that animals need varies enormously. Some bats spend up to twenty hours a day asleep, while large grazing animals such as horses and giraffes may manage with only a few hours, often taken in short periods. A study of wild African elephants, in which two females were fitted with devices that recorded their movements, found that they slept for only about two hours a night on average, and occasionally went without sleep for almost two days, possibly when they needed to move away from danger. In general, animals that eat plants, which provide relatively little energy, must spend long hours feeding and sleep relatively little, while hunters that can satisfy their needs with a single meal can afford to rest for much longer.

        Some of the most remarkable adaptations are found in animals that cannot easily stop moving. Dolphins and other marine mammals must come to the surface regularly to breathe, and a dolphin that became completely unconscious might drown. Their solution is to sleep with one half of the brain at a time. While one side of the brain rests, the other remains alert enough to control swimming and breathing, and the eye connected to the waking side stays open. Several species of birds can do the same. In one experiment with ducks sleeping in a row, the birds at the ends of the row kept the eye facing away from the group open much more often than the birds in the middle, apparently watching for predators.

        For a long time it was suspected that birds which spend weeks over the ocean might sleep while flying, but this was difficult to prove. In 2016 a team of scientists attached small recording devices to the heads of frigatebirds, large seabirds that can stay in the air for weeks at a time. The recordings showed that the birds did indeed sleep in flight, sometimes with one half of the brain and sometimes with both, usually while circling upwards on rising currents of warm air. However, they slept for less than an hour a day in the air, compared with more than twelve hours a day when they were on land.

        Why sleep exists at all remains one of the great unanswered questions of biology. One theory is that it saves energy, since body temperature and metabolism fall during sleep. Another is that it allows the brain to strengthen memories, sorting and storing the experiences of the day; experiments in which animals learn a task and are then either allowed or not allowed to sleep support this idea. A third, more recent theory suggests that sleep helps to clear away waste products that build up in the brain during waking hours. In experiments with mice, the spaces between brain cells were found to expand during sleep, allowing fluid to flow through and wash these substances away more efficiently.

        These theories are not necessarily in competition. Sleep may well have begun for one reason and taken on further roles over millions of years of evolution. What is clear is that it cannot simply be abandoned. Animals prevented from sleeping for long periods become seriously ill, and even species that sleep very little, like the frigatebird in flight, seem to find a way of snatching some rest. Sleep, it appears, is not an optional luxury but one of the basic requirements of animal life.
        """,
        [
            new("tfng", Ra711Tfng(1), null,
            [
                Stmt(1, "Some form of sleep has been observed in nearly every animal that scientists have studied closely.", "TRUE",
                    "\"Some form of sleep has been found in almost every animal that scientists have examined carefully.\""),
                Stmt(2, "Scientists decide whether insects are asleep by recording the activity of their brains.", "FALSE",
                    "\"For animals such as insects, scientists rely instead on behaviour.\""),
                Stmt(3, "Fruit flies that have been kept awake sleep longer or more deeply afterwards.", "TRUE",
                    "\"If it is kept awake, does it sleep longer or more deeply afterwards? Fruit flies deprived of sleep do exactly that.\""),
                Stmt(4, "The jellyfish studied in 2017 has a very small and simple brain.", "FALSE",
                    "The jellyfish is described as \"an animal with no brain at all\"."),
                Stmt(5, "Horses usually sleep for longer than giraffes.", "NOT GIVEN",
                    "Horses and giraffes are mentioned together as managing \"with only a few hours\"; they are not compared."),
                Stmt(6, "The devices fitted to the elephants recorded the electrical activity of their brains.", "FALSE",
                    "The elephants \"were fitted with devices that recorded their movements\"."),
                Stmt(7, "Plant-eating animals generally need to spend more time feeding than hunting animals do.", "TRUE",
                    "\"Animals that eat plants ... must spend long hours feeding ... while hunters ... can satisfy their needs with a single meal.\""),
                Stmt(8, "The ducks at the ends of the row were attacked by predators more often than the others.", "NOT GIVEN",
                    "The passage says these ducks kept one eye open more often, \"apparently watching for predators\", but not that they were attacked more."),
            ]),
            new("mcq", Abcd, null,
            [
                Mcq(9, "How do dolphins avoid drowning while they sleep?",
                    ["They sleep only in shallow water.", "They float on the surface with both eyes closed.", "They sleep for a few seconds at a time.", "They rest one half of the brain at a time."], "D",
                    "\"Their solution is to sleep with one half of the brain at a time.\""),
                Mcq(10, "What did the study of frigatebirds show?",
                    ["The birds never slept while flying.", "The birds slept as much in the air as on land.", "The birds slept in flight, but far less than on land.", "The birds slept in flight only with both halves of the brain."], "C",
                    "\"They slept for less than an hour a day in the air, compared with more than twelve hours a day when they were on land.\""),
                Mcq(11, "When did the frigatebirds usually sleep in the air?",
                    ["while circling upwards on warm air currents", "while flying low over the sea", "while flying in large groups", "while flying at night"], "A",
                    "\"Usually while circling upwards on rising currents of warm air.\""),
                Mcq(12, "What did the experiments with mice show?",
                    ["Their body temperature rose during sleep.", "The spaces between their brain cells grew larger during sleep.", "They remembered tasks better when kept awake.", "Their brains produced fewer waste products at night."], "B",
                    "\"The spaces between brain cells were found to expand during sleep.\""),
                Mcq(13, "What does the writer conclude about the different theories of sleep?",
                    ["The energy-saving theory is the most convincing.", "More than one of them may be correct.", "Only one of them can be true.", "None of them is supported by evidence."], "B",
                    "\"These theories are not necessarily in competition. Sleep may well have begun for one reason and taken on further roles.\""),
            ]),
        ]);

    private static ReadingPassage Ra8P2() => new("Lights Along the Coast",
        """
        For as long as people have travelled by sea, the most dangerous parts of any voyage have been its beginning and its end. On the open ocean a ship has room to manoeuvre, but near the coast it faces rocks, sandbanks and strong currents, often in darkness or poor weather. The earliest aids to navigation were probably simple fires lit on hilltops or beaches to guide boats home. Over time these fires were raised on towers so that they could be seen from further away, and the lighthouse was born.

        The most famous lighthouse of the ancient world stood on the island of Pharos, at the entrance to the harbour of Alexandria in Egypt. Completed in the third century BC, it is thought to have been more than 100 metres tall, which made it one of the tallest structures built by human hands for many centuries. Ancient writers described a fire burning at its top, and some claimed that its light could be seen from far out at sea, although such reports may have been exaggerated. The tower survived for well over a thousand years before being damaged by a series of earthquakes, and in the fifteenth century its ruins were used to build a fortress on the same site. So famous was the building that the word for "lighthouse" in several European languages, including French and Italian, comes from its name.

        The Romans built lighthouses at many of the ports of their empire. One of them, the Tower of Hercules on the north-west coast of Spain, still stands today and is the oldest lighthouse in the world that remains in use, although its outer walls were rebuilt in the eighteenth century. After the fall of the Roman Empire, however, few new lighthouses were built in Europe for several hundred years.

        Building a lighthouse on land is one thing; building one on a rock in the open sea is quite another. The story of the Eddystone rocks, which lie about fourteen kilometres off the coast of south-west England, illustrates the difficulties. The first lighthouse there, a wooden tower completed in 1698, was swept away in a great storm only five years later, taking its designer with it. A second tower, also mainly of wood, lasted for almost fifty years until it was destroyed by fire. The third, designed by the engineer John Smeaton and completed in 1759, was built of stone blocks cut so that they locked together like the pieces of a puzzle, making the structure extremely resistant to the force of the waves. Smeaton also developed a type of lime mortar that could set under water. His tower stood for more than 120 years and was replaced only because the rock beneath it had begun to wear away. The upper part was then taken down and rebuilt on land, where it can still be visited.

        For most of their history, lighthouses were lit by fires of wood or coal, or by candles, all of which produced a weak and smoky light. Improvements in the late eighteenth century, such as oil lamps with circular wicks and polished metal reflectors, helped, but the real revolution came in 1822, when the French physicist Augustin Fresnel designed a new kind of lens. Instead of a single thick piece of glass, which would have been extremely heavy, Fresnel's lens consisted of rings of glass prisms arranged around a central lens. It gathered light that would otherwise have been lost and concentrated it into a powerful beam that could be seen more than thirty kilometres away. Within a few decades, Fresnel lenses had been installed in lighthouses around the world.

        As the number of lighthouses grew, a new problem arose: sailors needed to know not only that they were near land but which light they were looking at. The solution was to give each lighthouse its own signature. Some flashed once every few seconds, others two or three times in a group, and some showed coloured lights. Charts printed for sailors listed the pattern of each light, so that a navigator could identify a light simply by timing its flashes.

        For centuries, lighthouses depended on keepers who lived in or beside them, cleaning the lenses, trimming the wicks and keeping the light burning through the night. It was often a lonely life, particularly on rock towers, where keepers might be cut off by bad weather for weeks. During the twentieth century, electric lights and automatic equipment gradually made keepers unnecessary, and in many countries the last of them left in the 1980s and 1990s. Today, satellite navigation allows ships to know their position with great accuracy, and some lighthouses have been switched off altogether. Many others, however, continue to operate as a backup in case electronic systems fail, and a number of former keepers' houses have found new lives as museums and holiday homes.
        """,
        [
            new("mcq", Abcd, null,
            [
                Mcq(14, "According to the first paragraph, ships are in greatest danger",
                    ["near the start and end of a voyage.", "in the middle of the ocean.", "when they carry heavy cargo.", "during the summer months."], "A",
                    "\"The most dangerous parts of any voyage have been its beginning and its end.\""),
                Mcq(15, "What does the writer say about ancient reports of the Pharos light?",
                    ["They described the light as weak.", "They were written by sailors.", "They were written after the tower had fallen.", "They may not have been accurate."], "D",
                    "\"Such reports may have been exaggerated.\""),
                Mcq(16, "What happened to the ruins of the Pharos?",
                    ["They were used to build a fortress.", "They were carried to Rome.", "They were destroyed by fire.", "They became a new lighthouse."], "A",
                    "\"Its ruins were used to build a fortress on the same site.\""),
                Mcq(17, "What is special about the Tower of Hercules?",
                    ["It is the tallest Roman building in Spain.", "It is the oldest lighthouse still in use.", "It was built after the fall of the Roman Empire.", "Its walls have never been rebuilt."], "B",
                    "\"The oldest lighthouse in the world that remains in use.\""),
                Mcq(18, "Why was Smeaton's tower eventually replaced?",
                    ["It was damaged by a storm.", "The rock under it was wearing away.", "It was destroyed by fire.", "Its light was too weak."], "B",
                    "\"It was replaced only because the rock beneath it had begun to wear away.\""),
            ]),
            new("gap", "Complete the notes below.\nChoose NO MORE THAN TWO WORDS from the passage for each answer.", 2,
            [
                Gap(19, "First Eddystone lighthouse (1698): a ___ tower, swept away in a storm", ["wooden"],
                    "\"The first lighthouse there, a wooden tower completed in 1698, was swept away in a great storm.\""),
                Gap(20, "Smeaton's tower: stone blocks that ___ like the pieces of a puzzle", ["locked together"],
                    "\"Stone blocks cut so that they locked together like the pieces of a puzzle.\""),
                Gap(21, "Smeaton's mortar: able to ___ under water", ["set"],
                    "\"A type of lime mortar that could set under water.\""),
                Gap(22, "Early light sources: wood, coal or ___", ["candles"],
                    "\"Lit by fires of wood or coal, or by candles.\""),
                Gap(23, "Late 18th century: oil lamps and polished metal ___", ["reflectors"],
                    "\"Oil lamps with circular wicks and polished metal reflectors.\""),
                Gap(24, "Fresnel's lens: rings of glass ___ around a central lens", ["prisms"],
                    "\"Rings of glass prisms arranged around a central lens.\""),
                Gap(25, "Identifying a light: sailors timed its ___", ["flashes"],
                    "\"A navigator could identify a light simply by timing its flashes.\""),
                Gap(26, "Today: many lighthouses operate as a ___ in case electronic systems fail", ["backup"],
                    "\"Many others ... continue to operate as a backup in case electronic systems fail.\""),
            ]),
        ]);

    private static ReadingPassage Ra8P3() => new("Should We Trust the Machine?",
        """
        Few areas of modern technology have produced as many bold predictions as the use of artificial intelligence in medicine. A little over a decade ago, some commentators were confidently forecasting that computer programs would soon make certain medical specialists unnecessary, and that hospitals should stop training them. That has not happened, and in my view it is unlikely to happen in the foreseeable future. Yet it would be equally mistaken to dismiss the technology as mere hype. The more interesting question is not whether machines will replace doctors, but how the two can best work together.

        The recent progress is real. Modern AI systems learn by analysing huge numbers of examples, such as X-rays or photographs of the skin, each labelled with the correct diagnosis. After training, they can examine a new image and estimate the likelihood of disease, often in a fraction of a second. In a number of carefully designed studies, such systems have matched experienced specialists at particular tasks, such as detecting signs of eye disease in scans of the retina. For health services that face a shortage of specialists, the attraction is obvious: a system that can check thousands of scans and highlight the few that need urgent attention could shorten waiting times considerably.

        However, I believe we should be cautious about what these results actually show. A system that performs well in a research study has usually been tested on images similar to those it was trained on. When it is used in a different hospital, with different equipment and a different population of patients, its accuracy may fall, sometimes sharply. This is not a minor technical detail. A program trained largely on images of patients with light skin, for example, may perform less well for patients with darker skin, which means that a tool intended to improve care could in fact make existing inequalities worse. Before any system is widely adopted, it should be tested in the settings where it will actually be used, and its performance should be monitored continuously afterwards.

        There is also a human problem, which receives less attention than it deserves. When people work alongside an automated system that is usually right, they tend to trust it even when it is wrong, a tendency psychologists call automation bias. A doctor who has seen a program make hundreds of correct judgements may stop checking its conclusions carefully, and a rare but serious error may pass unnoticed. Paradoxically, the better a system performs, the greater this risk may become. Training doctors to question the machine, and designing software that explains how it has reached a particular conclusion, are both essential if this danger is to be reduced.

        A further difficulty concerns responsibility. If a doctor overlooks a tumour, it is clear who must answer for the mistake. If a computer program misses it and the doctor, relying on the program, misses it too, the question becomes much harder. Is the fault with the doctor, the hospital that bought the software, or the company that designed it? Until the law provides clear answers, many hospitals will be reluctant to rely on these systems for important decisions, and I do not think this caution is unreasonable.

        None of this means that the technology should be held back. On the contrary, I believe it has great potential, especially in the less glamorous parts of medicine. Much of a doctor's day is spent on paperwork, writing notes and letters that could be drafted automatically, leaving more time for patients. In regions with very few specialists, a reliable screening tool could make the difference between a disease being caught early or not at all. These benefits are real, and they do not depend on machines being cleverer than doctors.

        What worries me is the way the debate is so often framed as a contest between human and machine, as if the aim were to find out which is better. Diagnosis is not simply a matter of recognising patterns in images. It involves talking to patients, understanding their circumstances, weighing up uncertain evidence and explaining difficult news. These are things that current systems cannot do. The most successful uses of AI are likely to be those that take over narrow, repetitive tasks and leave doctors free to concentrate on the parts of their work that require human judgement. If we keep that aim in view, the technology is far more likely to improve medicine than to diminish it.
        """,
        [
            new("ynng", Ra711Ynng(3), null,
            [
                Stmt(27, "AI is likely to replace certain medical specialists in the near future.", "NO",
                    "\"That has not happened, and in my view it is unlikely to happen in the foreseeable future.\""),
                Stmt(28, "Claims about the progress of AI in diagnosis are entirely exaggerated.", "NO",
                    "\"It would be equally mistaken to dismiss the technology as mere hype ... The recent progress is real.\""),
                Stmt(29, "AI systems cost more to run than employing additional specialists.", "NOT GIVEN",
                    "The writer does not discuss the cost of AI systems compared with the cost of specialists."),
                Stmt(30, "An AI system may be less accurate in a hospital other than the one where it was tested.", "YES",
                    "\"When it is used in a different hospital ... its accuracy may fall, sometimes sharply.\""),
                Stmt(31, "Automation bias is more common among young doctors than older ones.", "NOT GIVEN",
                    "Automation bias is explained, but the writer says nothing about the age of the doctors affected."),
                Stmt(32, "The risk of automation bias may grow as AI systems become more accurate.", "YES",
                    "\"Paradoxically, the better a system performs, the greater this risk may become.\""),
                Stmt(33, "Hospitals are wrong to hesitate before relying on AI for important decisions.", "NO",
                    "\"Many hospitals will be reluctant ... and I do not think this caution is unreasonable.\""),
            ]),
            new("mcq", Abcd, null,
            [
                Mcq(34, "According to the writer, what makes AI attractive to health services with too few specialists?",
                    ["It could reduce the time that patients wait.", "It could remove the need for medical training.", "It makes treatment cheaper for patients.", "It can replace expensive equipment."], "A",
                    "\"A system that can check thousands of scans ... could shorten waiting times considerably.\""),
                Mcq(35, "The writer mentions patients with darker skin in order to show that",
                    ["skin conditions are hard to photograph.", "doctors often disagree about diagnoses.", "AI systems cannot analyse photographs.", "the data used to train a system can lead to unequal results."], "D",
                    "\"A tool intended to improve care could in fact make existing inequalities worse.\""),
                Mcq(36, "What does the writer say about responsibility for mistakes involving AI?",
                    ["The software company should always be blamed.", "The law has not yet provided clear answers.", "Doctors should always be held responsible.", "Hospitals should stop buying AI software."], "B",
                    "\"Until the law provides clear answers, many hospitals will be reluctant to rely on these systems.\""),
                Mcq(37, "In which area does the writer see particular potential for AI?",
                    ["conversations with patients", "the most complex operations", "routine tasks such as paperwork", "the training of new specialists"], "C",
                    "\"Much of a doctor's day is spent on paperwork, writing notes and letters that could be drafted automatically.\""),
            ]),
            new("gap", "Complete the sentences below.\nChoose NO MORE THAN TWO WORDS from the passage for each answer.", 2,
            [
                Gap(38, "In regions with few specialists, a reliable ___ could help diseases to be caught early.", ["screening tool"],
                    "\"A reliable screening tool could make the difference between a disease being caught early or not at all.\""),
                Gap(39, "According to the writer, diagnosis involves more than recognising ___ in images.", ["patterns"],
                    "\"Diagnosis is not simply a matter of recognising patterns in images.\""),
                Gap(40, "AI is most likely to succeed when it takes over narrow, ___ tasks.", ["repetitive"],
                    "\"Those that take over narrow, repetitive tasks.\""),
            ]),
        ]);

    // =====================================================================
    // TEST 9 — mangrov o'rmonlari; qalam tarixi; uy vazifasi foydalimi? (fikr)
    // =====================================================================

    private static ReadingPassage Ra9P1() => new("Forests Between Land and Sea",
        """
        Along many tropical and subtropical coastlines, where rivers meet the sea and the tide rises and falls twice a day, there is a type of forest unlike any other. Mangrove forests grow in a zone that is flooded by salt water at high tide and exposed at low tide, a place where most trees would quickly die. The word "mangrove" does not refer to a single species or family of plants. It describes around seventy species of trees and shrubs, from several unrelated groups, that have independently developed the ability to live in these harsh conditions.

        The difficulties facing a plant in this environment are considerable. The first is salt. Seawater contains far more salt than most plants can tolerate, and mangroves have evolved several ways of dealing with it. Some species filter out most of the salt at their roots, so that the water reaching the rest of the tree is almost fresh. Others take in salt with the water but get rid of it through special glands on their leaves; on a dry day, crystals of salt can sometimes be seen on the leaf surface. A few species store salt in older leaves, which are then shed.

        The second difficulty is a lack of oxygen. The mud in which mangroves grow is waterlogged and contains very little oxygen, so the roots cannot breathe as they would in ordinary soil. Mangroves solve this problem with unusual roots. Some species produce arching stilt roots that grow down from the trunk and branches, lifting the tree above the water and giving it stability in the soft mud. Others send up hundreds of pencil-like roots, called pneumatophores, which stick out of the mud around the tree like the teeth of a comb. These roots contain small openings through which air can enter at low tide and pass down to the parts of the root system buried in the mud.

        Reproduction presents a third challenge, since a seed that falls into the sea may simply be washed away. Many mangroves have overcome this in an unusual way: their seeds begin to grow while still attached to the parent tree. By the time it falls, the young plant, known as a propagule, may be as long as a pencil or even longer. Some propagules drop into the mud below and quickly take root; others float away on the tide and can survive for months at sea before settling on a suitable shore, which helps to explain how mangroves have spread so widely.

        For a long time, mangrove forests were regarded as unproductive swamps, good for little except firewood, and large areas were cleared for agriculture, fish and shrimp farms, and coastal development. It is now estimated that a significant proportion of the world's mangroves was lost in the second half of the twentieth century. Only relatively recently has their value been widely recognised. The tangled roots provide shelter for young fish, crabs and shrimps, and many species that are caught in open water spend the early part of their lives among mangroves. The forests also protect the coastline. Their dense roots slow down waves and trap sediment carried by rivers, reducing erosion and, in some places, helping the land to build up over time. During storms, communities behind healthy mangrove belts have often suffered less damage than those whose forests had been removed.

        Mangroves have attracted particular interest because of their ability to store carbon. Because their waterlogged soils contain so little oxygen, dead leaves and roots decompose very slowly, and carbon accumulates in the mud over hundreds or even thousands of years. Per hectare, mangrove forests can store several times as much carbon as many tropical forests on land, with most of it held not in the trees themselves but in the soil beneath them. When mangroves are cleared, much of this carbon can be released into the atmosphere.

        Efforts to restore mangroves have increased greatly in recent decades, but they have not always been successful. In many early projects, large numbers of seedlings were planted on open mudflats, where they looked impressive in photographs but soon died, because the site was flooded for too long each day. Experience has shown that the most important factor is not the number of seedlings planted but the conditions of the site. Where the natural flow of water has been restored, for example by removing old barriers built for fish ponds, mangroves are often able to return by themselves, carried in as propagules on the tide.
        """,
        [
            new("gap", "Complete the notes below.\nChoose ONE WORD ONLY from the passage for each answer.", 1,
            [
                Gap(1, "Mangroves: about seventy species from several unrelated ___", ["groups"],
                    "\"Around seventy species of trees and shrubs, from several unrelated groups.\""),
                Gap(2, "Salt: some species remove it through ___ on their leaves", ["glands"],
                    "\"Get rid of it through special glands on their leaves.\""),
                Gap(3, "Salt: a few species store it in older leaves, which are then ___", ["shed"],
                    "\"A few species store salt in older leaves, which are then shed.\""),
                Gap(4, "Stilt roots: lift the tree and give it ___ in soft mud", ["stability"],
                    "\"Lifting the tree above the water and giving it stability in the soft mud.\""),
                Gap(5, "Pneumatophores: air enters through small openings at low ___", ["tide"],
                    "\"Small openings through which air can enter at low tide.\""),
                Gap(6, "Reproduction: the young plant that grows on the parent tree is called a ___", ["propagule"],
                    "\"The young plant, known as a propagule.\""),
            ]),
            new("tfng", Ra711Tfng(1), null,
            [
                Stmt(7, "Mangrove propagules that float away can survive for only a few days at sea.", "FALSE",
                    "\"Others float away on the tide and can survive for months at sea.\""),
                Stmt(8, "Mangrove forests were once thought to be of little value.", "TRUE",
                    "\"Mangrove forests were regarded as unproductive swamps, good for little except firewood.\""),
                Stmt(9, "Shrimp farming was the main cause of mangrove loss in the twentieth century.", "NOT GIVEN",
                    "Shrimp farms are listed among several causes, but no cause is identified as the main one."),
                Stmt(10, "Most of the fish sold in tropical countries come from mangrove areas.", "NOT GIVEN",
                    "The passage says many species caught in open water spend their early lives among mangroves, but gives no figures for fish sold."),
                Stmt(11, "Mangrove roots increase erosion along the coast.", "FALSE",
                    "\"Their dense roots slow down waves and trap sediment ... reducing erosion.\""),
                Stmt(12, "Most of the carbon in a mangrove forest is stored in the trees.", "FALSE",
                    "\"With most of it held not in the trees themselves but in the soil beneath them.\""),
                Stmt(13, "Mangroves can sometimes return to an area without being planted.", "TRUE",
                    "\"Mangroves are often able to return by themselves, carried in as propagules on the tide.\""),
            ]),
        ]);

    private static ReadingPassage Ra9P2() => new("The Humble Pencil",
        """
        Few objects are as ordinary as the pencil. It is cheap, simple to use and found in almost every home, school and office. Yet the familiar wooden pencil is the product of several centuries of discovery, accident and invention, and its story involves shepherds, wars and a good deal of confusion about what it is actually made of.

        The story begins in the sixteenth century in Borrowdale, a valley in the north-west of England. According to local tradition, a storm uprooted a tree and exposed a deposit of a black, shiny substance beneath it. Local shepherds soon found that it left a dark mark and began using it to mark their sheep. The substance was graphite, a form of pure carbon, but at the time nobody knew this. Because it looked similar to lead and left a similar grey mark, it was called "black lead", and the confusion has never entirely disappeared: we still speak of the "lead" in a pencil, although pencils have never contained any lead at all.

        The Borrowdale deposit was unusual. Most graphite is found in small grains mixed with other minerals, but here it was solid and remarkably pure, so it could be sawn into sticks and used directly. It soon became clear that it was valuable, not only for writing but also for lining the moulds in which cannonballs were made. The mine came under the control of the Crown, armed guards were posted, and at times the mine was deliberately flooded so that graphite could not be removed when it was not needed. Theft and smuggling were nevertheless common, and in the eighteenth century the British parliament made stealing graphite a serious crime.

        The earliest pencils were simply sticks of graphite wrapped in string or sheepskin to keep the user's fingers clean. Later, craftsmen began to cut a groove in a strip of wood, insert a thin stick of graphite and glue a second strip on top. This basic design has remained almost unchanged ever since. In Germany, the city of Nuremberg became an important centre of pencil making, although German makers, lacking access to pure graphite, had to mix graphite powder with other substances such as sulphur, producing pencils of poorer quality.

        The decisive breakthrough came from France. In the 1790s, France was at war with Britain and could no longer obtain English graphite. The French government asked Nicolas-Jacques Conté, an inventor and army officer, to find a solution. Conté discovered that powdered graphite could be mixed with clay and water, shaped into thin rods and then baked in a kiln. The result was not only a good pencil but a better one than before, because the proportions could be varied. The more clay in the mixture, the harder the pencil and the lighter its mark; the more graphite, the softer and darker the line. Conté's method meant that pencils could be made from ordinary, impure graphite, and it is essentially the method still used today.

        In the nineteenth century, pencil making became an industry. Machines were developed to cut and shape the wood, and producers began to grade their pencils by hardness. In the system now most widely used, H stands for hard and B for black, with HB in the middle; a 6B pencil is very soft and dark, and is favoured by artists, while a 4H pencil gives a fine, pale line suitable for technical drawing. Manufacturers also experimented with the shape of the pencil. Round pencils rolled off desks easily, and the hexagonal shape that is now so common was partly a solution to this problem, as well as being more economical to produce, since it wasted less wood.

        Even the colour of the pencil has a history. Many pencils today are painted yellow, and a popular explanation is that one manufacturer in the late nineteenth century chose the colour for its best pencils to suggest a connection with China, where yellow was associated with royalty, and that other companies soon copied it. Whether or not the story is true, yellow pencils remain particularly common in North America.

        Attaching an eraser to the end of a pencil seems an obvious idea, and it was patented in the United States in 1858. The patent was later declared invalid by the country's highest court, on the grounds that the invention simply combined two existing objects, each of which continued to do exactly what it had done before. Today, billions of pencils are produced every year. Despite the arrival of computers and tablets, the pencil survives, valued for its simplicity and reliability, and for the fact that, unlike many pens, it will write upside down and its marks can be rubbed out.
        """,
        [
            new("tfng", Ra711Tfng(2), null,
            [
                Stmt(14, "Shepherds were among the first people to make use of the Borrowdale graphite.", "TRUE",
                    "\"Local shepherds soon found that it left a dark mark and began using it to mark their sheep.\""),
                Stmt(15, "Pencils once contained small amounts of lead.", "FALSE",
                    "\"Pencils have never contained any lead at all.\""),
                Stmt(16, "Most graphite in the world is as pure as the graphite found at Borrowdale.", "FALSE",
                    "\"Most graphite is found in small grains mixed with other minerals, but here it was solid and remarkably pure.\""),
                Stmt(17, "Graphite was used in the production of cannonballs.", "TRUE",
                    "It was valuable \"for lining the moulds in which cannonballs were made\"."),
                Stmt(18, "The guards at the Borrowdale mine were often involved in smuggling.", "NOT GIVEN",
                    "The passage says theft and smuggling were common, but does not say who was involved."),
                Stmt(19, "The basic design of the wooden pencil has changed very little since it was introduced.", "TRUE",
                    "\"This basic design has remained almost unchanged ever since.\""),
                Stmt(20, "Pencil makers in Nuremberg had easy access to pure graphite.", "FALSE",
                    "\"German makers, lacking access to pure graphite, had to mix graphite powder with other substances.\""),
            ]),
            new("mcq", Abcd, null,
            [
                Mcq(21, "Why did the French government ask Conté to find a solution?",
                    ["French graphite was of poor quality.", "The Borrowdale mine had closed.", "German pencils had become too expensive.", "War with Britain had cut off the supply of English graphite."], "D",
                    "\"France was at war with Britain and could no longer obtain English graphite.\""),
                Mcq(22, "What was the main advantage of Conté's method?",
                    ["It did not require any graphite.", "It did not need any heat.", "The hardness of the pencil could be controlled.", "It produced pencils without wood."], "C",
                    "\"A better one than before, because the proportions could be varied. The more clay ... the harder the pencil.\""),
                Mcq(23, "Which pencil would be most suitable for technical drawing?",
                    ["6B", "HB", "4H", "any soft pencil"], "C",
                    "\"A 4H pencil gives a fine, pale line suitable for technical drawing.\""),
                Mcq(24, "One reason for the hexagonal shape of pencils was that",
                    ["it was easier to paint.", "it used less wood.", "it made the graphite stronger.", "artists preferred it."], "B",
                    "\"More economical to produce, since it wasted less wood.\""),
                Mcq(25, "What does the writer say about the story of yellow pencils?",
                    ["It has been proved to be true.", "It explains why yellow pencils are rare in North America.", "It concerns a Chinese manufacturer.", "It may not be true."], "D",
                    "\"Whether or not the story is true, yellow pencils remain particularly common in North America.\""),
                Mcq(26, "Why was the 1858 patent declared invalid?",
                    ["Someone else had registered the idea earlier.", "The eraser did not work well.", "The inventor had not paid the necessary fees.", "It combined two existing objects without changing what they did."], "D",
                    "\"The invention simply combined two existing objects, each of which continued to do exactly what it had done before.\""),
            ]),
        ]);

    private static ReadingPassage Ra9P3() => new("Homework: Time to Think Again?",
        """
        Few aspects of school life provoke as much disagreement as homework. For some parents, a steady supply of homework is a sign that a school takes learning seriously; for others, it is a source of nightly arguments that eats into family life and leaves children exhausted. Teachers themselves are divided, and many set homework less because they believe in it than because it is expected of them. Having looked at the evidence, and having spent many evenings at the kitchen table with my own children, I have come to a conclusion that will satisfy neither side entirely: homework can be valuable, but much of what is currently set is not.

        Let us begin with what research can tell us. The American psychologist Harris Cooper, who reviewed dozens of studies over several decades, found that the link between homework and achievement depends strongly on age. For students in the later years of secondary school, those who did more homework generally did better, up to a point, beyond which additional time brought little extra benefit. For younger students, and especially in primary school, the connection was weak or almost non-existent. Cooper has suggested a rough guideline, sometimes called the ten-minute rule, in which homework increases by about ten minutes a night for each year of schooling. By that measure, a seven-year-old would do perhaps twenty minutes, and a student in the final year of school a couple of hours.

        We should be careful, of course, about what such studies show. A link between homework and results does not prove that homework causes the results. Students who are already motivated and well organised may simply be more likely to complete their homework. Nevertheless, the pattern is consistent enough to take seriously, and it matches what many teachers observe: older students can use homework to practise skills, consolidate what they have learnt and prepare for examinations, while younger children often lack the ability to work independently for long periods.

        If the benefits for young children are small, the costs are not. Homework that a child cannot do alone quickly becomes homework for the parents, and not all parents are equally able to help. A child whose parents work in the evenings, who do not speak the language of the school, or who simply had difficult experiences of school themselves is at a clear disadvantage compared with a classmate whose parents have both the time and the confidence to sit beside them. In this way homework, which is meant to support learning, can widen the gap between children from different backgrounds. Some schools have tried to reduce this problem by running homework clubs, where pupils can complete their work at school with a teacher available, and I think this idea deserves to be much more widely adopted.

        The quality of homework matters at least as much as the quantity. Too often, homework consists of finishing work that was not completed in class, or of tasks set simply because a timetable requires them. Such work teaches children little except that homework is a chore. By contrast, a short task with a clear purpose, such as practising a skill that was introduced that day or preparing questions to discuss in the next lesson, can be genuinely useful. Teachers also need time to look at what their students have done; homework that is never checked or discussed sends a clear message that it does not really matter.

        For primary school children, I would go further. The single most useful thing a young child can do at home, in my view, is to read, or to be read to, for pleasure. Reading builds vocabulary and general knowledge in a way that few worksheets can match, and it is an activity that families of all backgrounds can share. Beyond that, children need time to play, to be outdoors, to pursue their own interests and to rest. These are not distractions from learning; they are a large part of how children learn.

        Some schools have responded to these arguments by abolishing homework altogether. I understand the impulse, but I believe it goes too far, particularly for older students. The ability to study independently, to plan one's time and to work without a teacher present is essential for university and for many jobs, and it is difficult to develop without practice. The aim, then, should not be to eliminate homework but to set less of it in the early years, to make every task count, and to ensure that no child is held back because of the circumstances in which they happen to be doing it.
        """,
        [
            new("ynng", Ra711Ynng(3), null,
            [
                Stmt(27, "Some teachers set homework mainly because it is expected of them.", "YES",
                    "\"Many set homework less because they believe in it than because it is expected of them.\""),
                Stmt(28, "All the homework that is currently set is of little value.", "NO",
                    "\"Homework can be valuable, but much of what is currently set is not.\" (much, not all)"),
                Stmt(29, "Research proves that doing homework causes students to achieve better results.", "NO",
                    "\"A link between homework and results does not prove that homework causes the results.\""),
                Stmt(30, "More schools should run homework clubs.", "YES",
                    "\"I think this idea deserves to be much more widely adopted.\""),
                Stmt(31, "Parents who help with homework often confuse children by teaching different methods.", "NOT GIVEN",
                    "The writer discusses whether parents are able to help, not the methods they use."),
                Stmt(32, "Results have fallen in schools that have abolished homework.", "NOT GIVEN",
                    "The writer disagrees with abolishing homework but gives no information about results in those schools."),
            ]),
            new("gap", "Complete the sentences below.\nChoose NO MORE THAN TWO WORDS from the passage for each answer.", 2,
            [
                Gap(33, "Cooper's guideline for how much homework to set is sometimes called the ___.", ["ten-minute rule", "10-minute rule"],
                    "\"A rough guideline, sometimes called the ten-minute rule.\""),
                Gap(34, "Students who are already ___ and well organised may be more likely to do their homework.", ["motivated"],
                    "\"Students who are already motivated and well organised may simply be more likely to complete their homework.\""),
                Gap(35, "Homework that a young child cannot do alone becomes homework for the ___.", ["parents"],
                    "\"Homework that a child cannot do alone quickly becomes homework for the parents.\""),
                Gap(36, "Homework that is never checked suggests to students that it does not really ___.", ["matter"],
                    "\"Homework that is never checked or discussed sends a clear message that it does not really matter.\""),
            ]),
            new("mcq", Abcd, null,
            [
                Mcq(37, "According to the writer, what is the most useful thing a young child can do at home?",
                    ["complete worksheets", "practise skills learnt in class", "prepare questions for the next lesson", "read or be read to for pleasure"], "D",
                    "\"The single most useful thing a young child can do at home ... is to read, or to be read to, for pleasure.\""),
                Mcq(38, "How does the writer regard play and rest for young children?",
                    ["as a reward for finishing homework", "as an important part of learning", "as a distraction from learning", "as something schools should organise"], "B",
                    "\"These are not distractions from learning; they are a large part of how children learn.\""),
                Mcq(39, "What is the writer's opinion of abolishing homework completely?",
                    ["It is understandable but goes too far.", "It is the best solution for all ages.", "It has been shown to improve results.", "It should happen only in secondary schools."], "A",
                    "\"I understand the impulse, but I believe it goes too far, particularly for older students.\""),
                Mcq(40, "Which of the following best summarises the writer's overall position?",
                    ["Homework should be increased for all students.", "Young children should get less homework, and all homework should have a clear purpose.", "Homework should be left entirely to parents.", "Homework should be abolished in primary and secondary schools."], "B",
                    "\"To set less of it in the early years, to make every task count.\""),
            ]),
        ]);

    // =====================================================================
    // TEST 10 — bambuk qurilish materiali; yozuv tizimlari; olomon xulqi (fikr)
    // =====================================================================

    private static ReadingPassage Ra10P1() => new("Building with Bamboo",
        """
        In many parts of Asia, Latin America and Africa, bamboo has been used for building for thousands of years. Houses, bridges and scaffolding made from it are a common sight, and in some cities tall buildings are still constructed with the help of bamboo scaffolding tied together by hand. Yet in much of the industrialised world, bamboo has long been regarded as a material for the poor, suitable for temporary shelters but not for serious architecture. That view is now changing, as engineers and architects look for building materials that are strong, cheap and less harmful to the environment than steel and concrete.

        Although it can grow as tall as a tree, bamboo is in fact a type of grass. Some species are among the fastest-growing plants on Earth: under ideal conditions, a single stem can grow almost a metre in a day. More importantly for builders, bamboo reaches its full strength quickly. A stem, or culm, can be harvested for construction after three to five years, whereas trees grown for timber may take decades to mature. And because bamboo spreads through an underground system of roots, harvesting the culms does not kill the plant; new shoots continue to appear each year, so a well-managed plantation can provide material indefinitely without replanting.

        The structure of the culm explains much of bamboo's strength. It is a hollow tube, divided at intervals by solid walls at the joints, or nodes, which prevent it from bending too easily. The walls of the tube contain long, strong fibres, which are packed more densely towards the outside, exactly where they are most needed to resist bending. As a result, bamboo is remarkably strong for its weight. Its ability to resist being pulled apart, known as tensile strength, compares favourably with that of many types of timber and, when its low weight is taken into account, even with some steels. Bamboo is also flexible, and buildings made from it can sway during an earthquake rather than collapsing. In regions where earthquakes are common, well-built bamboo houses have often survived tremors that destroyed heavier buildings of brick and concrete.

        Bamboo does, however, have serious weaknesses. Its stems contain starch, which attracts insects, particularly a type of beetle that can reduce a culm to powder. It is also vulnerable to fungi, and untreated bamboo left exposed to rain and sun may last only a few years. The traditional solution was to harvest bamboo in the dry season, when starch levels are lower, and to soak it in water for weeks to wash out some of the starch. Today, bamboo is often treated with a solution of boron salts, which protect it against insects and fungi and are relatively harmless to people. Good design is equally important. Builders in bamboo-growing regions have a saying that a bamboo building needs "good boots and a good hat": it should stand on a raised foundation, so that the stems do not touch the wet ground, and it should have a wide roof that keeps off the rain.

        A further problem is that no two culms are the same. Each stem varies in diameter, wall thickness and straightness, and even a single stem is thicker at the bottom than at the top. For engineers used to working with steel beams of precisely known dimensions, this makes calculations difficult. Joining culms is also a challenge: because they are round and hollow, they cannot simply be nailed together like planks, as nails tend to split them along their length. Builders have traditionally used lashings of rope or strips of bamboo. Some modern architects have developed joints in which the hollow ends of the culms are filled with concrete and connected by steel bolts, allowing much larger structures to be built.

        Another approach avoids the problem of irregular culms altogether. In engineered bamboo, the stems are cut into thin strips, which are then treated, dried and glued together under pressure to form boards and beams of standard sizes. These products look and behave rather like timber and can be used in the same way, although the glue and the energy needed for processing reduce some of bamboo's environmental advantages. Engineered bamboo is already widely used for flooring and furniture, and it is increasingly being tested in structural uses.

        The introduction of international standards for the design of bamboo structures has given engineers greater confidence in the material. Nevertheless, barriers remain. In many countries, building regulations make no mention of bamboo, which makes it difficult to obtain permission to use it, and many people still associate it with poverty. Supporters of bamboo argue that these obstacles will fall as more high-quality buildings are completed, showing that a material once seen as humble can also be beautiful, durable and modern.
        """,
        [
            new("tfng", Ra711Tfng(1), null,
            [
                Stmt(1, "Bamboo scaffolding is still used in the construction of some tall buildings.", "TRUE",
                    "\"In some cities tall buildings are still constructed with the help of bamboo scaffolding.\""),
                Stmt(2, "Bamboo can be harvested for building sooner than timber trees.", "TRUE",
                    "\"A stem ... can be harvested for construction after three to five years, whereas trees grown for timber may take decades.\""),
                Stmt(3, "A bamboo plantation has to be replanted after the stems are harvested.", "FALSE",
                    "\"A well-managed plantation can provide material indefinitely without replanting.\""),
                Stmt(4, "Bamboo houses are cheaper to insure in areas where earthquakes are common.", "NOT GIVEN",
                    "The passage says bamboo houses have often survived earthquakes, but says nothing about insurance."),
                Stmt(5, "Bamboo cut in the dry season contains less starch.", "TRUE",
                    "\"Harvest bamboo in the dry season, when starch levels are lower.\""),
                Stmt(6, "Boron salts are highly dangerous to people.", "FALSE",
                    "Boron salts \"are relatively harmless to people\"."),
            ]),
            new("mcq", Abcd, null,
            [
                Mcq(7, "What does the saying \"good boots and a good hat\" refer to?",
                    ["protective clothing for builders", "a raised foundation and a wide roof", "choosing the right season for harvesting", "treating bamboo with chemicals"], "B",
                    "\"It should stand on a raised foundation ... and it should have a wide roof that keeps off the rain.\""),
                Mcq(8, "Why do engineers find bamboo difficult to work with?",
                    ["Every culm has different dimensions.", "It is too heavy to lift easily.", "It bends too easily under load.", "It cannot be cut with ordinary tools."], "A",
                    "\"No two culms are the same ... this makes calculations difficult.\""),
                Mcq(9, "Why are nails unsuitable for joining bamboo?",
                    ["They rust in wet weather.", "They tend to split the culms.", "They are too expensive.", "They damage the nodes."], "B",
                    "\"Nails tend to split them along their length.\""),
            ]),
            new("gap", "Complete the sentences below.\nChoose NO MORE THAN TWO WORDS from the passage for each answer.", 2,
            [
                Gap(10, "Traditionally, builders joined culms with ___ of rope or strips of bamboo.", ["lashings"],
                    "\"Builders have traditionally used lashings of rope or strips of bamboo.\""),
                Gap(11, "Some modern joints are made by filling the ends of culms with ___ and connecting them with steel bolts.", ["concrete"],
                    "\"The hollow ends of the culms are filled with concrete and connected by steel bolts.\""),
                Gap(12, "Engineered bamboo is made into boards and beams of ___.", ["standard sizes"],
                    "\"Glued together under pressure to form boards and beams of standard sizes.\""),
                Gap(13, "Engineered bamboo is already widely used for flooring and ___.", ["furniture"],
                    "\"Engineered bamboo is already widely used for flooring and furniture.\""),
            ]),
        ]);

    private static ReadingPassage Ra10P2() => new("From Tokens to Alphabets",
        """
        Writing is so central to modern life that it is hard to imagine a world without it. Yet for most of the time that modern humans have existed, there was no writing at all. People told stories, passed on knowledge and organised their communities entirely through speech and memory. Writing, when it finally appeared, was invented independently only a handful of times in human history, and most of the scripts used around the world today can be traced back to one or other of these few beginnings.

        The earliest known writing developed in Mesopotamia, in what is now Iraq, a little over five thousand years ago. Interestingly, it seems to have grown not out of a desire to record stories or laws, but out of bookkeeping. For thousands of years before writing appeared, people in the region had used small clay tokens of different shapes to keep track of goods: a cone might represent a measure of grain, a cylinder an animal. According to one influential theory, these tokens were sometimes sealed inside hollow clay balls to record an agreement, and marks were pressed into the outside to show what was inside. In time, people realised that the marks alone were enough, and the tokens themselves became unnecessary.

        The earliest Mesopotamian tablets are almost all administrative records: lists of rations, workers, animals and fields. The signs were at first simple pictures, drawn with a pointed tool in soft clay. But drawing curves in clay is slow, and scribes gradually began to press the end of a cut reed into the surface instead, producing marks shaped like small wedges. This script is known today as cuneiform, from the Latin word for "wedge". Over the centuries, the signs became increasingly abstract, until most no longer looked like the objects they had once represented.

        A system consisting only of pictures has serious limitations. It is easy to draw a fish or a bird, but much harder to represent a person's name, an abstract idea or a grammatical ending. The solution, discovered independently in several places, was the rebus principle: using a sign for the sound of the word it represents rather than for its meaning. In English, for example, a picture of a bee followed by a picture of a leaf could be used to write "belief". Once scribes began to use signs in this way, writing could record any word in the spoken language and was no longer limited to lists.

        Writing appeared in Egypt at about the same time as in Mesopotamia, and scholars still debate whether the idea travelled from one to the other or developed separately. It was certainly invented independently in China, where the earliest surviving texts, carved on animal bones and shells used in predicting the future, are more than three thousand years old, and in what is now Mexico and Central America, where the Maya developed a sophisticated script. These systems all used a large number of signs, often several hundred or even thousands, which meant that learning to read and write took many years. In most early societies, literacy was restricted to a small group of professional scribes.

        The alphabet, in which a small number of signs each represent a single sound, appeared much later. The earliest examples, from around four thousand years ago, were apparently created by speakers of a Semitic language who were familiar with Egyptian hieroglyphs. They took a few Egyptian pictures and used each one to stand for the first sound of the word for that object in their own language. Their script recorded only consonants; readers were expected to supply the vowels from their knowledge of the language. The idea was developed by the Phoenicians, a seafaring trading people of the eastern Mediterranean, whose alphabet of twenty-two letters spread along their trade routes.

        When the Greeks adopted the Phoenician alphabet, around the eighth century BC, they made a crucial change. Greek, unlike the Phoenicians' language, could not easily be read without vowels, and some Phoenician letters represented sounds that did not exist in Greek. The Greeks used these spare letters for vowels, creating what many scholars regard as the first full alphabet. From Greek, the alphabet passed to the Etruscans in Italy and then to the Romans, whose Latin script is now used, with various additions, for hundreds of languages across the world.

        Because an alphabet needs only a few dozen letters, it is far easier to learn than a script with hundreds of signs, and it is often said that it helped to make reading and writing available to ordinary people. Yet alphabets have not replaced all other systems. Chinese characters, for example, have remained in use for thousands of years, partly because they can be understood by speakers of different Chinese languages who would not understand each other's speech. Each writing system, it seems, represents a different solution to the same problem: how to make spoken language visible and lasting.
        """,
        [
            new("gap", "Complete the sentences below.\nChoose ONE WORD ONLY from the passage for each answer.", 1,
            [
                Gap(14, "The earliest writing in Mesopotamia seems to have developed from ___ rather than from stories or laws.", ["bookkeeping"],
                    "\"It seems to have grown not out of a desire to record stories or laws, but out of bookkeeping.\""),
                Gap(15, "Before writing, people used small clay ___ of different shapes to keep track of goods.", ["tokens"],
                    "\"People in the region had used small clay tokens of different shapes to keep track of goods.\""),
                Gap(16, "Scribes later pressed the end of a cut ___ into the clay.", ["reed"],
                    "\"Scribes gradually began to press the end of a cut reed into the surface.\""),
                Gap(17, "The name of the script, cuneiform, comes from the Latin word for \"___\".", ["wedge"],
                    "\"Known today as cuneiform, from the Latin word for 'wedge'.\""),
                Gap(18, "Over time, the signs became more and more ___.", ["abstract"],
                    "\"Over the centuries, the signs became increasingly abstract.\""),
                Gap(19, "Using a sign for a sound instead of a meaning is known as the ___ principle.", ["rebus"],
                    "\"The solution ... was the rebus principle: using a sign for the sound of the word.\""),
            ]),
            new("mcq", Abcd, null,
            [
                Mcq(20, "What does the writer say about the origins of Egyptian writing?",
                    ["It was clearly copied from Mesopotamia.", "It appeared long after Mesopotamian writing.", "It was the first writing system in the world.", "Scholars disagree about whether it developed independently."], "D",
                    "\"Scholars still debate whether the idea travelled from one to the other or developed separately.\""),
                Mcq(21, "The earliest surviving Chinese texts were written on",
                    ["bones and shells.", "clay tablets.", "strips of bamboo.", "stone walls."], "A",
                    "\"The earliest surviving texts, carved on animal bones and shells.\""),
                Mcq(22, "Why was literacy limited to a small group in many early societies?",
                    ["Writing materials were very expensive.", "Rulers did not allow ordinary people to write.", "There were no schools.", "The scripts contained a very large number of signs."], "D",
                    "\"These systems all used a large number of signs ... which meant that learning to read and write took many years.\""),
                Mcq(23, "The earliest alphabet",
                    ["recorded only consonants.", "was invented by Egyptian scribes.", "included signs for vowels.", "contained several hundred letters."], "A",
                    "\"Their script recorded only consonants.\""),
                Mcq(24, "How did the Phoenician alphabet spread?",
                    ["through military conquest", "through religious texts", "through Greek schools", "along trade routes"], "D",
                    "\"Whose alphabet of twenty-two letters spread along their trade routes.\""),
                Mcq(25, "What change did the Greeks make to the alphabet?",
                    ["They reduced the number of letters.", "They changed the direction of writing.", "They used some letters to represent vowels.", "They added pictures of objects."], "C",
                    "\"The Greeks used these spare letters for vowels.\""),
                Mcq(26, "According to the writer, one reason why Chinese characters are still used is that",
                    ["they are easier to learn than an alphabet.", "they need only a few dozen signs.", "speakers of different Chinese languages can understand them.", "they were adopted by the Romans."], "C",
                    "\"They can be understood by speakers of different Chinese languages who would not understand each other's speech.\""),
            ]),
        ]);

    private static ReadingPassage Ra10P3() => new("The Myth of the Mindless Crowd",
        """
        When something goes wrong in a large crowd, the explanation offered in news reports is often the same: people panicked. The image is a familiar one. Faced with danger, a crowd supposedly turns into a mindless mass, each individual thinking only of their own survival, pushing and trampling others in a desperate rush for safety. It is a powerful picture, and it has shaped the way many authorities think about crowds. In my view, it is also largely wrong, and the belief in it may itself be putting people at risk.

        The idea has a long history. At the end of the nineteenth century, the French writer Gustave Le Bon argued that people in a crowd lose their individual personalities and their capacity for rational thought, becoming as easily led as children. His book was widely read, and its central idea, that crowds are by nature irrational and dangerous, has proved remarkably durable. It appealed, perhaps, to those who feared the growing power of ordinary people in the rapidly expanding industrial cities of the time.

        Modern research paints a very different picture. Psychologists who have interviewed survivors of emergencies, from building fires to transport accidents, have repeatedly found that panic, in the sense of selfish and uncontrolled flight, is rare. Far more common is cooperation. People help strangers, wait for others and share information; they often behave in an orderly way even when they are frightened. Indeed, one of the problems emergency planners face is not that people rush to escape too quickly, but that they are too slow to move, waiting to collect belongings or to see what others are doing before they react.

        Some researchers have suggested an explanation for this behaviour. When people find themselves facing a common danger, they begin to see themselves as members of a group with a common fate, even if they were strangers a few minutes earlier. This sense of shared identity encourages them to look after one another. Far from destroying individual reason, the crowd can create new bonds of solidarity.

        Why, then, do people sometimes die in crowds? The answer, in most cases, lies not in psychology but in physics. When too many people are packed into too small a space, individuals lose the ability to control their own movements. Above a certain density, a crowd begins to behave rather like a fluid: pressure travels through it in waves, and people can be pushed off their feet by forces that no one in the crowd intended or could resist. Those who fall may be unable to get up again. People at the back, unable to see what is happening at the front, may continue to move forward, not out of selfishness but simply because they do not know that anything is wrong. To describe such events as the result of panic is to blame the victims for a situation that was created by overcrowding.

        This matters because the way we understand crowds affects the way we manage them. If officials believe that crowds are liable to panic, they may be reluctant to give clear information in an emergency for fear of causing alarm. Yet the evidence suggests that people respond sensibly to clear, honest information, and that it is uncertainty and delay that are dangerous. Similarly, if crowds are seen as a threat to be controlled, the emphasis will be on barriers and policing rather than on the design of spaces and the planning of events so that dangerous densities never develop in the first place.

        I would not wish to suggest that crowds are never dangerous or that people never behave badly in them. Of course they sometimes do. But the lessons of research are clear enough to guide practice. Venues should be designed with enough space for the number of people expected, with routes in and out that do not force large flows of people through narrow openings. Organisers should monitor crowd density continuously, using cameras and trained staff, and be ready to slow the flow of arrivals before it becomes a problem. Above all, people in crowds should be treated as partners in their own safety, capable of acting sensibly if they are given the information they need.

        Crowds, after all, are neither unusual nor threatening. Every day, millions of people travel together on trains, fill stadiums and concert halls, and gather to celebrate festivals. The overwhelming majority of these events pass without incident, a fact that says a good deal about the ordinary good sense of people in large numbers. It is time we stopped fearing the crowd and started designing for it.
        """,
        [
            new("ynng", Ra711Ynng(3), null,
            [
                Stmt(27, "The usual explanation that crowd disasters are caused by panic is broadly accurate.", "NO",
                    "\"In my view, it is also largely wrong.\""),
                Stmt(28, "Le Bon's ideas about crowds have had a lasting influence.", "YES",
                    "\"Its central idea ... has proved remarkably durable.\""),
                Stmt(29, "Le Bon based his ideas on his own experience of dangerous crowds.", "NOT GIVEN",
                    "The writer describes Le Bon's argument but not what it was based on."),
                Stmt(30, "In emergencies, people often move too slowly rather than too quickly.", "YES",
                    "\"Not that people rush to escape too quickly, but that they are too slow to move.\""),
                Stmt(31, "Officials should avoid giving detailed information to a crowd during an emergency.", "NO",
                    "\"People respond sensibly to clear, honest information, and ... it is uncertainty and delay that are dangerous.\""),
                Stmt(32, "Barriers are of no use at all in managing crowds.", "NOT GIVEN",
                    "The writer criticises an emphasis on barriers rather than design, but does not say whether barriers are useful."),
            ]),
            new("gap", "Complete the sentences below.\nChoose NO MORE THAN TWO WORDS from the passage for each answer.", 2,
            [
                Gap(33, "People facing a common danger may develop a sense of ___ that makes them help one another.", ["shared identity"],
                    "\"This sense of shared identity encourages them to look after one another.\""),
                Gap(34, "Above a certain density, a crowd starts to behave like a ___.", ["fluid"],
                    "\"A crowd begins to behave rather like a fluid.\""),
                Gap(35, "In a very dense crowd, pressure moves through the crowd in ___.", ["waves"],
                    "\"Pressure travels through it in waves.\""),
                Gap(36, "People at the back keep moving forward because they cannot see what is happening at the ___.", ["front"],
                    "\"People at the back, unable to see what is happening at the front, may continue to move forward.\""),
            ]),
            new("mcq", Abcd, null,
            [
                Mcq(37, "According to the writer, most deaths in crowds are caused by",
                    ["overcrowding.", "selfish behaviour.", "people stopping to collect belongings.", "poor lighting."], "A",
                    "\"The answer, in most cases, lies not in psychology but in physics ... a situation that was created by overcrowding.\""),
                Mcq(38, "What does the writer think about describing crowd disasters as the result of panic?",
                    ["It helps organisers to plan better.", "It is accurate in most cases.", "It makes people more careful.", "It unfairly blames the victims."], "D",
                    "\"To describe such events as the result of panic is to blame the victims.\""),
                Mcq(39, "Which of the following does the writer recommend?",
                    ["holding fewer large events", "increasing the number of police", "keeping information from crowds", "monitoring crowd density continuously"], "D",
                    "\"Organisers should monitor crowd density continuously, using cameras and trained staff.\""),
                Mcq(40, "What is the writer's main purpose in the passage?",
                    ["to argue that people should avoid large crowds", "to describe the history of stadium design", "to challenge a common view of crowds and suggest a better approach", "to explain why Le Bon's book was popular"], "C",
                    "\"It is time we stopped fearing the crowd and started designing for it.\""),
            ]),
        ]);

    // =====================================================================
    // TEST 11 — qushlar yo'l topishi; soyabon tarixi; reklama va e'tibor iqtisodi (fikr)
    // =====================================================================

    private static ReadingPassage Ra11P1() => new("Finding the Way",
        """
        Each year, billions of birds travel between their breeding grounds and the places where they spend the winter. Some of these journeys are astonishing. The Arctic tern, which breeds in the far north and spends the northern winter near Antarctica, may travel some 70,000 kilometres in a single year. The bar-tailed godwit, a wading bird, has been tracked flying more than 11,000 kilometres from Alaska to New Zealand without stopping to eat, drink or rest. Even small songbirds weighing only a few grams regularly cross deserts and seas. Just as remarkable as the distances is the accuracy: many birds return year after year not just to the same region but to the same garden or even the same nest.

        How they achieve this has puzzled scientists for centuries, and it is now clear that there is no single answer. Birds appear to use several different sources of information and to switch between them depending on the conditions. Researchers usually distinguish between a compass, which tells a bird which direction it is facing, and a "map", which tells it where it is in relation to its goal. A compass alone is not enough: a person with a compass who has been taken to an unknown place knows where north is, but not which way to go.

        One of the first compasses to be identified was the sun. In the 1950s, experiments with caged starlings showed that during the migration season the birds tried to move in a particular direction, and that when the apparent position of the sun was shifted with mirrors, the direction they chose shifted by a similar amount. Because the sun moves across the sky, a bird using it must allow for the time of day, and experiments in which birds' internal clocks were altered by keeping them under artificial lighting confirmed that they do exactly this.

        Many songbirds, however, migrate at night. For them, the stars provide another compass. In a series of experiments in the 1960s, the American scientist Stephen Emlen placed young indigo buntings in a planetarium, where he could control the pattern of stars on the ceiling. The birds oriented themselves using the stars near the Pole Star, around which the night sky appears to turn. Interestingly, the young birds were not born knowing particular star patterns; they learnt during their first months which part of the sky remained still. When Emlen raised birds under an artificial sky that turned around a different star, they treated that star as north.

        Birds can also sense the Earth's magnetic field. This was first shown convincingly in the 1960s and 1970s by experiments with European robins, which changed the direction in which they tried to fly when placed inside an artificial magnetic field. The birds' magnetic compass turned out to work in an unexpected way: rather than detecting whether the field points north or south, it detects the angle at which the field lines slope towards the ground, which differs between the equator and the poles. How birds detect the field is still being investigated. One leading theory suggests that it depends on a light-sensitive molecule in the eye, which may allow birds to "see" the magnetic field as a pattern laid over their normal vision.

        Finding a direction, however, is not the same as finding a destination. A classic experiment in the 1950s showed the difference clearly. Thousands of starlings caught in the Netherlands during their autumn migration were taken to Switzerland and released. Adult birds, which had made the journey before, corrected their course and headed towards their usual wintering grounds. Young birds on their first migration, however, continued to fly in the same direction as before, and ended up in Spain and southern France, far from their normal destination. This suggests that young birds inherit a basic programme telling them to fly in a certain direction for a certain time, while experienced birds develop a genuine map.

        What that map consists of is still a matter of debate. Some scientists believe that birds can use variations in the strength of the magnetic field to judge their position. Others point to smell: seabirds such as shearwaters appear to use odours carried by the wind to find their way across the open ocean, and homing pigeons whose sense of smell has been blocked often have difficulty finding their way home from unfamiliar places. Close to home, birds also use landmarks such as coastlines, rivers and mountains.

        Human activity may be making navigation more difficult. Artificial light at night can attract and confuse migrating birds, especially in cloudy weather, and some studies suggest that electromagnetic noise from electrical equipment may disrupt their magnetic sense. Understanding how birds find their way is therefore not only a fascinating scientific puzzle but also a practical matter, relevant to the protection of species whose journeys have become a symbol of the natural world's extraordinary abilities.
        """,
        [
            new("tfng", Ra711Tfng(1), null,
            [
                Stmt(1, "The bar-tailed godwit completes its flight from Alaska to New Zealand without stopping to feed.", "TRUE",
                    "\"Flying more than 11,000 kilometres from Alaska to New Zealand without stopping to eat, drink or rest.\""),
                Stmt(2, "Scientists now believe that birds depend on a single method of navigation.", "FALSE",
                    "\"It is now clear that there is no single answer. Birds appear to use several different sources of information.\""),
                Stmt(3, "A compass on its own is enough to allow a bird to reach its goal.", "FALSE",
                    "\"A compass alone is not enough.\""),
                Stmt(4, "The mirror experiments with starlings were repeated in several different countries.", "NOT GIVEN",
                    "The passage describes the experiments but does not say where they were carried out or repeated."),
                Stmt(5, "Indigo buntings are born knowing the patterns of the stars.", "FALSE",
                    "\"The young birds were not born knowing particular star patterns; they learnt during their first months.\""),
                Stmt(6, "Experiments with European robins gave early convincing evidence that birds can sense the magnetic field.", "TRUE",
                    "\"This was first shown convincingly in the 1960s and 1970s by experiments with European robins.\""),
            ]),
            new("gap", "Complete the sentences below.\nChoose ONE WORD ONLY from the passage for each answer.", 1,
            [
                Gap(7, "A bird's magnetic compass detects the ___ at which the field lines slope towards the ground.", ["angle"],
                    "\"It detects the angle at which the field lines slope towards the ground.\""),
                Gap(8, "One theory suggests that a light-sensitive ___ in the eye allows birds to sense the magnetic field.", ["molecule"],
                    "\"It depends on a light-sensitive molecule in the eye.\""),
                Gap(9, "Shearwaters appear to use ___ carried by the wind to find their way over the ocean.", ["odours", "odors"],
                    "\"Seabirds such as shearwaters appear to use odours carried by the wind.\""),
                Gap(10, "Close to home, birds use ___ such as coastlines, rivers and mountains.", ["landmarks"],
                    "\"Close to home, birds also use landmarks such as coastlines, rivers and mountains.\""),
            ]),
            new("mcq", Abcd, null,
            [
                Mcq(11, "What happened to the young starlings that were released in Switzerland?",
                    ["They returned to the Netherlands.", "They corrected their course and reached their usual wintering grounds.", "They followed the adult birds.", "They kept flying in their original direction and ended up in the wrong place."], "D",
                    "\"Young birds ... continued to fly in the same direction as before, and ended up in Spain and southern France.\""),
                Mcq(12, "What do the results of the starling experiment suggest about experienced birds?",
                    ["They rely only on the sun.", "They cannot change direction once they have started.", "They have developed a real map.", "They migrate only at night."], "C",
                    "\"Experienced birds develop a genuine map.\""),
                Mcq(13, "According to the final paragraph, artificial light at night",
                    ["helps birds to navigate in cloudy weather.", "affects only seabirds.", "can attract and confuse migrating birds.", "has been proved to damage birds' eyes."], "C",
                    "\"Artificial light at night can attract and confuse migrating birds.\""),
            ]),
        ]);

    private static ReadingPassage Ra11P2() => new("Under Cover: A History of the Umbrella",
        """
        Few everyday objects have a longer or more varied history than the umbrella. Today we think of it mainly as protection against rain, but for most of its existence it served a different purpose altogether. The word itself offers a clue: "umbrella" comes from the Latin umbra, meaning "shade", and the earliest umbrellas were designed to keep off the sun rather than the rain.

        Carvings and paintings from ancient Egypt, Assyria and India show servants holding sunshades over the heads of kings and nobles. In these societies the parasol was far more than a practical object; it was a symbol of rank and power, and in some places only the ruler and those he favoured were permitted to use one. The number of parasols carried in a procession, or the number of tiers on a single parasol, could indicate a person's importance. Similar customs continued for centuries in parts of Asia and Africa.

        It was probably in China that umbrellas were first made to keep out the rain. Chinese craftsmen made umbrellas of paper or silk stretched over frames of bamboo, and they discovered that coating the paper with oil or wax made it waterproof. Some of these umbrellas could be opened and closed, and the basic design, with ribs spreading out from a central stick, is essentially the one we still use. Oiled-paper umbrellas are still made by hand in some parts of China today, often decorated with paintings of flowers and landscapes.

        In ancient Greece and Rome, sunshades were used mainly by women, and they appear in paintings on Greek pottery. After the end of the Roman Empire, however, umbrellas largely disappeared from everyday life in Europe for many centuries. They returned in the sixteenth and seventeenth centuries, partly through contact with Asia, first as sunshades for the wealthy in Italy and France. Only gradually did people in northern Europe begin to use them against the rain.

        In Britain, the umbrella was slow to gain acceptance. For much of the eighteenth century it was considered a feminine accessory, and a man who carried one risked being laughed at; gentlemen who could afford it were expected to travel by carriage when it rained. The person usually credited with changing this attitude is Jonas Hanway, a traveller and writer who had seen umbrellas in use on his journeys abroad. From around the 1750s, he carried one regularly in the streets of London, ignoring the insults he received. The drivers of hired coaches were said to be particularly hostile, since every man who kept himself dry under an umbrella was a passenger they had lost. By the time Hanway died, however, umbrellas had become a common sight, and for a time they were even known in London by his name.

        Early European umbrellas were heavy and awkward. Their ribs were usually made of whalebone or cane, and the covering was often oiled silk or cotton, which became stiff and sticky over time, so that a large umbrella could be tiring to carry for any distance. The key improvement came in 1852, when the English manufacturer Samuel Fox introduced a frame with ribs made of steel. Fox's business also produced steel supports for women's corsets, and he was looking for a new use for the material. His steel frame was lighter, stronger and cheaper than whalebone, and it quickly became standard.

        The twentieth century brought further changes. In the 1920s an Austrian inventor, Hans Haupt, developed a telescopic umbrella that could be folded down to a much smaller size, and compact folding umbrellas became popular. New materials such as nylon replaced silk and cotton for the canopy, and more recently fibreglass ribs, which bend in strong winds instead of breaking, have been widely adopted. Some modern designs include small vents in the canopy that allow gusts of wind to pass through, reducing the chance that the umbrella will be turned inside out.

        Despite these advances, the umbrella remains a surprisingly fragile object, and huge numbers are thrown away every year, many after only a short period of use. Because they combine metal, plastic and fabric, they are difficult to recycle, and some cities have introduced schemes that allow people to borrow umbrellas and return them later. Perhaps the umbrella's long history has one more stage to come: not a return to the parasols of ancient kings, but a durable object designed to be repaired rather than replaced.
        """,
        [
            new("gap", "Complete the notes below.\nChoose ONE WORD ONLY from the passage for each answer.", 1,
            [
                Gap(14, "Origin of the word: from Latin umbra, meaning \"___\"", ["shade"],
                    "\"'Umbrella' comes from the Latin umbra, meaning 'shade'.\""),
                Gap(15, "Ancient societies: the parasol was a symbol of ___ and power", ["rank"],
                    "\"It was a symbol of rank and power.\""),
                Gap(16, "The number of ___ on a parasol could show a person's importance", ["tiers"],
                    "\"The number of tiers on a single parasol, could indicate a person's importance.\""),
                Gap(17, "China: paper was coated with oil or ___ to make it waterproof", ["wax"],
                    "\"Coating the paper with oil or wax made it waterproof.\""),
                Gap(18, "Greece and Rome: sunshades shown in paintings on ___", ["pottery"],
                    "\"They appear in paintings on Greek pottery.\""),
                Gap(19, "Europe: umbrellas returned partly through contact with ___", ["Asia"],
                    "\"They returned in the sixteenth and seventeenth centuries, partly through contact with Asia.\""),
            ]),
            new("tfng", Ra711Tfng(2), null,
            [
                Stmt(20, "In eighteenth-century Britain, a man carrying an umbrella risked being laughed at.", "TRUE",
                    "\"A man who carried one risked being laughed at.\""),
                Stmt(21, "Jonas Hanway wrote a book about the history of the umbrella.", "NOT GIVEN",
                    "Hanway is described as \"a traveller and writer\", but the passage does not say what he wrote about."),
                Stmt(22, "Hanway stopped carrying an umbrella because of the insults he received.", "FALSE",
                    "\"He carried one regularly in the streets of London, ignoring the insults he received.\""),
                Stmt(23, "Coach drivers disliked umbrellas because they lost customers as a result.", "TRUE",
                    "\"Every man who kept himself dry under an umbrella was a passenger they had lost.\""),
                Stmt(24, "The coverings of early European umbrellas stayed soft and flexible for many years.", "FALSE",
                    "\"The covering ... became stiff and sticky over time.\""),
                Stmt(25, "Before making umbrella frames, Samuel Fox's business had made steel parts for women's clothing.", "TRUE",
                    "\"Fox's business also produced steel supports for women's corsets.\""),
                Stmt(26, "Hans Haupt's folding umbrella was more expensive than earlier umbrellas.", "NOT GIVEN",
                    "The passage describes Haupt's telescopic umbrella but says nothing about its price."),
            ]),
        ]);

    private static ReadingPassage Ra11P3() => new("Paying with Our Attention",
        """
        In 1971, the economist and psychologist Herbert Simon made an observation that seems more relevant with every passing year. In a world rich in information, he pointed out, what becomes scarce is whatever information consumes, and what it consumes is the attention of those who receive it. A wealth of information, in other words, creates a poverty of attention. Simon was writing long before the internet, but his insight describes our situation precisely. Attention has become one of the most valuable resources in the modern economy, and a large part of that economy is built on capturing it and selling it to advertisers.

        The basic model is not new. Newspapers, commercial radio and television have long been sold to audiences cheaply, or given away free, with advertisers paying for the privilege of reaching those audiences. What has changed is the scale and precision of the process. Online platforms can measure exactly how long each user looks at each item, and they can use this information to show each person the content most likely to keep them looking. The longer we stay, the more advertisements we see, and the more the platform earns.

        I want to be clear that I am not opposed to advertising as such. At its best, advertising performs a useful function: it tells people about products and services they might want, helps new businesses to find customers and encourages competition. It has also paid for a great deal of valuable journalism and entertainment that many people could not otherwise have afforded. Those who condemn all advertising often overlook how much of what they enjoy has been funded by it.

        My concern is with the incentives that the attention economy creates. When a service earns money according to how much time people spend on it, its designers have every reason to make it as difficult as possible to put down. Features such as endless scrolling, the automatic playing of the next video and a constant stream of notifications are not accidents; they are carefully tested to hold our attention for as long as possible. There is also some evidence that material which provokes strong emotions, particularly anger, tends to spread more widely than calmer content, and a system designed to maximise engagement may therefore reward it, whether or not this is anyone's intention.

        A common response is that people should simply exercise more self-control. I find this argument unconvincing. It is true that individuals can and should make choices about how they spend their time, but it is unreasonable to expect them to do so on equal terms with companies that employ large teams of specialists whose job is to make those choices harder. We do not usually tell people that the only protection they need against misleading advertising is their own good sense; we have rules about what advertisers may claim. There is no obvious reason why the design of attention-capturing services should be treated differently.

        What, then, should be done? One possibility is for more services to be paid for directly by their users, through subscriptions, so that the company's interests are more closely aligned with those of the people using it. This model has already been adopted successfully by some news organisations and music services. It is not a complete solution, however. Many people cannot afford to pay for everything they use, and a world in which only the better-off can enjoy services free from advertising would raise questions of fairness of its own.

        A second approach is to require greater transparency. Users should be able to see why a particular item has been shown to them and to choose, easily, a version of the service that is not designed to maximise the time they spend on it. Settings that limit notifications or turn off automatic playing already exist in some products, but they are often hidden away in menus that few people ever open. Making the less demanding option the default, rather than the exception, would be a modest but meaningful change.

        Finally, there is a role for education. Children in particular should learn not only how to use digital services but also how they are paid for, and why they are designed as they are. Understanding that a free service is being paid for with one's own time and attention does not make the service any worse, but it does allow people to make more informed decisions about it.

        Attention is, in the end, a limited resource. Each of us has only so many hours in a day, and what we attend to shapes what we know, what we care about and, to a large extent, who we become. That is precisely why it deserves more careful protection than it currently receives.
        """,
        [
            new("mcq", Abcd, null,
            [
                Mcq(27, "Why does the writer refer to Herbert Simon?",
                    ["to introduce the idea that attention is a scarce resource", "to show that the internet was predicted in 1971", "to criticise the work of economists", "to explain how advertising began"], "A",
                    "\"A wealth of information, in other words, creates a poverty of attention.\""),
                Mcq(28, "According to the writer, what is new about the modern attention economy?",
                    ["Advertisers pay to reach audiences.", "Newspapers are given away free.", "The process is far larger and more precise.", "Advertisements have become shorter."], "C",
                    "\"What has changed is the scale and precision of the process.\""),
                Mcq(29, "What does the writer say about people who condemn all advertising?",
                    ["They are usually journalists.", "They are right about its effects.", "They forget how much it has paid for.", "They prefer to pay for subscriptions."], "C",
                    "\"Those who condemn all advertising often overlook how much of what they enjoy has been funded by it.\""),
                Mcq(30, "How does the writer describe features such as endless scrolling?",
                    ["as accidental results of poor design", "as deliberately designed to hold attention", "as features that most users dislike", "as features that advertisers demand"], "B",
                    "\"They are not accidents; they are carefully tested to hold our attention for as long as possible.\""),
            ]),
            new("ynng", Ra711Ynng(3), null,
            [
                Stmt(31, "Systems designed to maximise engagement may reward content that makes people angry.", "YES",
                    "\"A system designed to maximise engagement may therefore reward it.\""),
                Stmt(32, "Individuals have no responsibility for how they spend their time online.", "NO",
                    "\"It is true that individuals can and should make choices about how they spend their time.\""),
                Stmt(33, "The design of attention-capturing services should be subject to rules, as advertising claims are.", "YES",
                    "\"We have rules about what advertisers may claim. There is no obvious reason why the design of attention-capturing services should be treated differently.\""),
                Stmt(34, "Subscription services generally offer better content than free services.", "NOT GIVEN",
                    "The writer discusses subscriptions as a way to align interests but does not compare the quality of content."),
                Stmt(35, "Paying for services directly would solve the problem completely.", "NO",
                    "\"It is not a complete solution, however.\""),
                Stmt(36, "Technology companies now earn more from subscriptions than from advertising.", "NOT GIVEN",
                    "The passage gives no information about how companies' income is divided."),
            ]),
            new("gap", "Complete the sentences below.\nChoose NO MORE THAN TWO WORDS from the passage for each answer.", 2,
            [
                Gap(37, "Settings that limit notifications are often hidden in ___ that few people open.", ["menus"],
                    "\"They are often hidden away in menus that few people ever open.\""),
                Gap(38, "The writer suggests that the less demanding option should become the ___.", ["default"],
                    "\"Making the less demanding option the default, rather than the exception.\""),
                Gap(39, "Children should learn how digital services are ___ and why they are designed as they are.", ["paid for"],
                    "\"Children ... should learn not only how to use digital services but also how they are paid for.\""),
                Gap(40, "According to the writer, what we pay attention to shapes who we ___.", ["become"],
                    "\"What we attend to shapes what we know, what we care about and, to a large extent, who we become.\""),
            ]),
        ]);
}
