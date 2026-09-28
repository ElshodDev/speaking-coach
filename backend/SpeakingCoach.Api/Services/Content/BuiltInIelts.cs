using SpeakingCoach.Api.Services.Mock;

namespace SpeakingCoach.Api.Services.Content;

// Tayyor IELTS testlari. Original matn va savollar (haqiqiy imtihondan ko'chirilmagan),
// tuzilishi rasmiy formatga mos: Listening 4 qism × 10 savol, Reading 3 matn (13 + 13 + 14).
public static partial class BuiltInIelts
{
    public static readonly ListeningTest Listening1 = new([ListeningPart1(), ListeningPart2(), ListeningPart3(), ListeningPart4()]);

    public static readonly ReadingTest ReadingAcademic1 = new(IeltsBank.Academic, [ReadingPassage1(), ReadingPassage2(), ReadingPassage3()]);

    // ---- Yordamchilar ----

    private const string Abc = "Choose the correct letter, A, B or C.";
    private const string Abcd = "Choose the correct letter, A, B, C or D.";

    private static ScriptLine M(string speaker, string text) => new(speaker, "male", text);
    private static ScriptLine F(string speaker, string text) => new(speaker, "female", text);

    private static ObjQuestion Gap(int n, string prompt, string[] answers, string explanation) =>
        new(n, prompt, null, [.. answers], explanation);

    private static ObjQuestion Mcq(int n, string prompt, string[] options, string answer, string explanation) =>
        new(n, prompt, [.. options], [answer], explanation);

    private static ObjQuestion Stmt(int n, string statement, string answer, string explanation) =>
        new(n, statement, null, [answer], explanation);

    // =====================================================================
    // LISTENING
    // =====================================================================

    // Part 1 — velosiped ijarasi (forma to'ldirish)
    private static ListeningPart ListeningPart1() => new(1,
        "You will hear a man phoning a bicycle hire shop to book bikes for a family holiday.",
        [
            F("Laura", "Good morning, Riverside Cycle Hire, Laura speaking. How can I help you?"),
            M("Martin", "Oh, hello. I'm coming to the town on holiday with my family next month, and I'd like to hire some bikes for the week, if that's possible."),
            F("Laura", "Of course. Let me take a few details and then I'll check what we have available. Could I have your name, please?"),
            M("Martin", "Yes, it's Martin Delaney."),
            F("Laura", "Could you spell your surname for me?"),
            M("Martin", "Sure. It's D, E, L, A, N, E, Y."),
            F("Laura", "Thank you, Mr Delaney. And where will you be staying while you're here? We sometimes deliver bikes if it's close by."),
            M("Martin", "We originally booked a room at the Station Hotel, but they had a problem with the rooms, so they moved us to the Harbour Hotel instead. It's on the seafront, I think."),
            F("Laura", "Yes, I know it. It's only five minutes from us. So, how many bikes will you need?"),
            M("Martin", "Well, it was going to be three adult bikes, but my brother has decided to bring his own, so just two adult bikes. And one bike for my daughter."),
            F("Laura", "Right. For children's bikes the size depends on height rather than age. Do you know how tall she is?"),
            M("Martin", "She's nine, and she's quite tall for her age. We measured her last week, and she's 135 centimetres."),
            F("Laura", "Lovely, then we have a good bike for her. And which dates are you thinking of?"),
            M("Martin", "We arrive on the 13th of July, but that's a Sunday and we'll be tired after the journey, so we'd like to start on the 14th, the Monday, and keep the bikes for seven days."),
            F("Laura", "That's fine. Now, about the price. Our adult bikes are 12 pounds a day, but if you take one for a full week, it's 45 pounds per bike. That's quite a saving."),
            M("Martin", "That's good. And the child's bike?"),
            F("Laura", "Children's bikes are half that price for the week, so 22 pounds 50."),
            M("Martin", "Great. Is there anything else I need to pay for? Helmets, for example?"),
            F("Laura", "No, every bike comes with a helmet and a lock, so you don't need to worry about those. Lights are extra, but you probably won't need them in the summer."),
            M("Martin", "OK. And do you need a deposit?"),
            F("Laura", "Yes, we take a deposit of 50 pounds per bike. We used to accept cash, but we don't any more, so it has to be a credit card. We don't take any money from the card unless there's some damage."),
            M("Martin", "That's fine. I'll use a credit card, then."),
            F("Laura", "And would you like us to deliver the bikes to your hotel, or will you collect them yourselves?"),
            M("Martin", "We'll collect them, I think. It'll be a nice walk. Is your shop on Market Road?"),
            F("Laura", "Our main shop is, yes, but the family bikes are kept at our second branch, which is on Bridge Street, right next to the river. So please come there."),
            M("Martin", "Bridge Street, got it. And one last thing. Can you recommend a good route for a family? My daughter is a confident rider, but I don't want anything too hilly or busy."),
            F("Laura", "Lots of families enjoy the Coast Road, but it gets very busy with cars in July. I'd suggest the Old Railway path. It follows the line of a railway that closed years ago, so it's completely flat and there's no traffic at all."),
            M("Martin", "That sounds perfect. Thank you so much for your help."),
            F("Laura", "You're welcome. I'll email you a confirmation this afternoon. See you in July!"),
        ],
        [
            new("gap", "Complete the form below.\nWrite NO MORE THAN TWO WORDS AND/OR A NUMBER for each answer.", 2,
            [
                Gap(1, "Customer's surname: ___", ["Delaney"], "He spells it: \"D, E, L, A, N, E, Y\"."),
                Gap(2, "Staying at: the ___ Hotel", ["Harbour", "Harbor"], "\"They moved us to the Harbour Hotel instead\" (not the Station Hotel)."),
                Gap(3, "Number of adult bikes: ___", ["2", "two"], "\"So just two adult bikes\" — his brother is bringing his own."),
                Gap(4, "Child's height: ___ cm", ["135"], "\"She's 135 centimetres.\""),
                Gap(5, "Start date: ___ July", ["14", "14th", "fourteenth"], "\"We'd like to start on the 14th, the Monday\" (they arrive on the 13th)."),
                Gap(6, "Price per adult bike for one week: £ ___", ["45", "£45"], "\"If you take one for a full week, it's 45 pounds per bike.\""),
                Gap(7, "Included with every bike: a helmet and a ___", ["lock"], "\"Every bike comes with a helmet and a lock.\""),
                Gap(8, "Deposit: must be paid by ___", ["credit card"], "\"We don't [accept cash] any more, so it has to be a credit card.\""),
                Gap(9, "Collect the bikes from the branch on ___ Street", ["Bridge"], "\"The family bikes are kept at our second branch, which is on Bridge Street.\""),
                Gap(10, "Recommended family route: the ___ path", ["Old Railway"], "\"I'd suggest the Old Railway path\" (the Coast Road is busy)."),
            ]),
        ]);

    // Part 2 — botanika bog'i bo'ylab ekskursiya (monolog)
    private static ListeningPart ListeningPart2() => new(2,
        "You will hear a guide talking to a group of visitors at the start of a tour of a botanical garden.",
        [
            F("Guide", "Good morning, everyone, and welcome to Greenhill Botanical Garden. My name's Helen, and I'll be your guide for the next hour or so. Before we set off, I'd like to tell you a little about the garden's history and give you some practical information."),
            F("Guide", "Many visitors assume that the garden was created by a wealthy family as their private garden, because of the grand house you passed at the entrance. In fact, the house was added much later. The garden was founded in 1820 as a teaching garden for the city's medical school, where students learnt to recognise the plants that were used to make medicines. It only opened to the general public about eighty years later."),
            F("Guide", "Now, you may have read in the local paper about changes here this year. The new café that everyone's been talking about is still being built, I'm afraid, and it won't open until next spring. And some people have asked me whether we've stopped charging for entry. Well, entry to the garden has always been free, so nothing has changed there. The really big news is that our Palm House, the huge glasshouse you can see on the hill, has finally reopened after three years of repair work, and we'll be going inside it today."),
            F("Guide", "A few requests before we start. You're very welcome to take photographs anywhere in the garden, and please feel free to sit on the lawns. The grass is there to be enjoyed. The one thing we do ask is that you don't feed the ducks on the lake. Bread is very bad for them, and they find plenty of natural food on their own."),
            F("Guide", "Normally, our tours end at the rose garden, which is lovely at this time of year. Unfortunately, it's closed this month while we replant some of the older beds, so today we'll finish our walk down by the lake instead. From there it's only a short walk back to the gift shop, if you'd like to buy anything."),
            F("Guide", "People often ask me when the best time to see the Japanese garden is. Lots of visitors come in spring for the cherry blossom, and it's certainly pretty then. But personally, I think it's at its most beautiful in the autumn, when the maple trees turn a deep red. So if you can come back then, please do."),
            F("Guide", "Right, let me tell you about some of the places we'll see. Our first stop is the Palm House. Inside, it's kept at a steady 28 degrees all year round, so you may want to take your coats off before we go in. It's home to plants from tropical rainforests, including a banana tree which actually produces fruit."),
            F("Guide", "Next door is the Desert House, which is much drier. Look out for a small cactus near the door. It looks very ordinary during the day, but it only flowers at night, and when it does, it has a wonderful smell. Our staff sometimes stay late just to see it open."),
            F("Guide", "After that we'll walk through the herb garden. Here you'll find more than two hundred plants which have been used in cooking and medicine for hundreds of years. You're allowed to touch and smell the leaves, but please don't pick them."),
            F("Guide", "If you have children with you, they'll love the Discovery Area near the lake. Children can borrow a magnifying glass from the small hut there and use it to look for insects and spiders. It's free, and they just need to bring it back afterwards."),
            F("Guide", "And finally, if you'd like to explore on your own after the tour, you can collect a free audio guide from the library beside the main gate. It's available in several languages and lasts about forty minutes. OK, if everyone's ready, let's head up the hill to the Palm House."),
        ],
        [
            new("mcq", Abc, null,
            [
                Mcq(11, "The garden was first created as", ["a private garden for a wealthy family.", "a place for training medical students.", "a public park for the city."], "B",
                    "\"The garden was founded in 1820 as a teaching garden for the city's medical school.\""),
                Mcq(12, "What is new at the garden this year?", ["A café has opened.", "Entry is now free.", "The glasshouse has reopened."], "C",
                    "\"Our Palm House, the huge glasshouse ... has finally reopened\"; the café is still being built and entry has always been free."),
                Mcq(13, "Visitors are asked not to", ["feed the ducks.", "take photographs.", "sit on the grass."], "A",
                    "\"The one thing we do ask is that you don't feed the ducks on the lake.\""),
                Mcq(14, "Where will today's tour finish?", ["at the rose garden", "by the lake", "at the gift shop"], "B",
                    "\"Today we'll finish our walk down by the lake instead.\""),
                Mcq(15, "The guide thinks the best time to visit the Japanese garden is", ["in spring.", "in summer.", "in autumn."], "C",
                    "\"I think it's at its most beautiful in the autumn, when the maple trees turn a deep red.\""),
            ]),
            new("gap", "Complete the notes below.\nWrite NO MORE THAN TWO WORDS AND/OR A NUMBER for each answer.", 2,
            [
                Gap(16, "Palm House: kept at ___ degrees all year round", ["28", "twenty-eight"], "\"It's kept at a steady 28 degrees all year round.\""),
                Gap(17, "Desert House: a cactus near the door only flowers at ___", ["night"], "\"It only flowers at night.\""),
                Gap(18, "Herb garden: plants used for centuries in ___ and medicine", ["cooking"], "\"Plants which have been used in cooking and medicine for hundreds of years.\""),
                Gap(19, "Discovery Area: children can borrow a ___ to look for insects", ["magnifying glass"], "\"Children can borrow a magnifying glass from the small hut.\""),
                Gap(20, "Free audio guides: collect from the ___ beside the main gate", ["library"], "\"Collect a free audio guide from the library beside the main gate.\""),
            ]),
        ]);

    // Part 3 — talaba va o'qituvchi: oshxonadagi ovqat isrofi loyihasi
    private static ListeningPart ListeningPart3() => new(3,
        "You will hear a student, Maya, talking to her tutor, Dr Evans, about her project on food waste in the university canteen.",
        [
            M("Dr Evans", "Come in, Maya. So, you wanted to talk about your project on food waste in the canteen. How's it going?"),
            F("Maya", "Quite well, I think. But there are a few things I'd like your advice on."),
            M("Dr Evans", "Of course. First, remind me why you chose this topic. Was it on the list of topics I gave the class?"),
            F("Maya", "No, it wasn't on the list, actually. I work part-time in the canteen at weekends, and I was really shocked by how much food ended up in the bins. So I decided to find out more. The canteen manager didn't ask me to do it, but she was very happy to help once I explained the idea."),
            M("Dr Evans", "Good. A personal interest usually makes for a better project. How did you measure the waste?"),
            F("Maya", "I thought about asking students to keep a food diary, but I didn't think people would be honest about what they threw away. And counting the plates with food left on them would have taken far too long. So in the end, I weighed the waste bins at the end of every lunch for four weeks."),
            M("Dr Evans", "That's a sensible, objective method. And what did you find?"),
            F("Maya", "Well, on average about 40 kilos of food was thrown away every day. I expected vegetables to be the biggest problem, because a lot of students leave them on the plate. But the result that really surprised me was bread. The free bread rolls that come with every meal made up nearly a third of all the waste."),
            M("Dr Evans", "Interesting. Now, I read the survey you attached to your email. You had two hundred responses, which is quite reasonable for a project of this size, and it wasn't too long either. My main concern is the wording. Some of your questions could be understood in two different ways, so I'm not sure how far we can trust the answers."),
            F("Maya", "Yes, I was a bit worried about that. Should I do the survey again with better questions?"),
            M("Dr Evans", "I don't think there's time for that before the deadline. And I wouldn't remove it from the report either, because it still tells us something. I'd simply explain the problem honestly in your discussion section. Examiners respect a student who can see the weaknesses in their own work."),
            F("Maya", "OK, that makes sense. The other thing is the presentation next month. I was planning to open with the figure of 40 kilos a day."),
            M("Dr Evans", "The trouble with numbers is that people find it hard to imagine them. You could tell a little story, but that can take too long. I'd show the audience a photograph of one day's waste instead. It's much more powerful than a number on a slide."),
            F("Maya", "I've got some photos, so that's easy. What else should I do before I write the final report?"),
            M("Dr Evans", "I think you need to talk to the people who actually decide how much food is prepared. Have you interviewed the head chef?"),
            F("Maya", "Not yet. I've only spoken to the manager."),
            M("Dr Evans", "Then that's your first step. Ask about how the orders are planned. Secondly, you should look at what's happening outside the university. The city council published a report on food waste last year, and it has some useful figures you could compare with yours."),
            F("Maya", "I'll find it. And for my recommendations, I was thinking the canteen could simply stop giving out free bread."),
            M("Dr Evans", "That might be rather unpopular. What about serving meals on smaller plates? Some studies suggest that people take less food when the plate is smaller, and so they waste less."),
            F("Maya", "That's a good idea. I'll add that. When would you like to see a draft?"),
            M("Dr Evans", "I'm away on Wednesday, so send it to me by Thursday, and we'll meet the following week to discuss it."),
            F("Maya", "Thursday. Great, thanks very much."),
        ],
        [
            new("mcq", Abc, null,
            [
                Mcq(21, "Why did Maya choose food waste as her topic?", ["She had seen the problem herself at work.", "It was on the tutor's list of topics.", "The canteen manager asked her to study it."], "A",
                    "\"I work part-time in the canteen ... and I was really shocked by how much food ended up in the bins.\""),
                Mcq(22, "How did Maya measure the amount of waste?", ["She asked students to keep a diary.", "She counted plates with food left on them.", "She weighed the bins after lunch."], "C",
                    "\"In the end, I weighed the waste bins at the end of every lunch for four weeks.\""),
                Mcq(23, "What surprised Maya about her results?", ["how much bread was thrown away", "how many vegetables were left on plates", "how much the waste changed from day to day"], "A",
                    "\"The result that really surprised me was bread.\""),
                Mcq(24, "What is the tutor's main concern about the survey?", ["Too few students answered it.", "It was too long.", "Some of the questions were unclear."], "C",
                    "\"Some of your questions could be understood in two different ways.\""),
                Mcq(25, "What does the tutor advise Maya to do about the survey?", ["discuss its weakness in the report", "repeat it with new questions", "leave it out of the report"], "A",
                    "\"I'd simply explain the problem honestly in your discussion section.\""),
                Mcq(26, "The tutor suggests that Maya should begin her presentation with", ["a statistic.", "a photograph.", "a short story."], "B",
                    "\"I'd show the audience a photograph of one day's waste instead.\""),
            ]),
            new("gap", "Complete the notes below.\nWrite NO MORE THAN TWO WORDS for each answer.", 2,
            [
                Gap(27, "Next steps: interview the ___ about how orders are planned", ["head chef"], "\"Have you interviewed the head chef? ... Ask about how the orders are planned.\""),
                Gap(28, "Compare results with a report published by the ___", ["city council"], "\"The city council published a report on food waste last year.\""),
                Gap(29, "Recommendation: serve meals on ___ plates", ["smaller"], "\"What about serving meals on smaller plates?\""),
                Gap(30, "Send the draft to the tutor by ___", ["Thursday"], "\"I'm away on Wednesday, so send it to me by Thursday.\""),
            ]),
        ]);

    // Part 4 — ma'ruza: shisha tarixi
    private static ListeningPart ListeningPart4() => new(4,
        "You will hear part of a university lecture about the history of glass.",
        [
            M("Lecturer", "Good morning. In today's lecture I'm going to look at the history of glass, a material we use every day without really noticing it. We'll follow its development from ancient times to the present, and think about why it has been so important for science."),
            M("Lecturer", "Let's begin with the natural world. Long before people learnt to make glass, they used a natural glass produced by volcanoes, called obsidian. Because it breaks into very sharp edges, early humans used it to make tools such as knives and arrowheads."),
            M("Lecturer", "The first man-made glass appeared in Mesopotamia, in what is now the Middle East, about four and a half thousand years ago. Archaeologists believe that the earliest glass objects were not windows or bottles but small beads, which were worn as jewellery. At that time glass was so rare that it was treated almost like a precious stone."),
            M("Lecturer", "The big change came roughly two thousand years ago, along the eastern coast of the Mediterranean, with the invention of glassblowing. By blowing air through a long tube into a lump of hot glass, a worker could shape a thin, hollow object in minutes. Suddenly glass was cheap enough for ordinary households, and simple cups became a common sight on the table. The Romans spread the technique across their empire, and they even put glass in some windows. However, Roman window glass was thick and uneven, and it was not fully transparent. It let the light in, but you couldn't see clearly through it."),
            M("Lecturer", "Let's jump forward now to medieval Venice, which became the most famous glassmaking centre in Europe. At the end of the thirteenth century, the city's glassmakers were ordered to move their workshops to the nearby island of Murano. The main reason was the danger of fire. The furnaces burned day and night, and most buildings in Venice at that time contained a great deal of wood. On Murano, the glassmakers developed a very clear glass which they called cristallo. They achieved this partly by adding a mineral called manganese, which removed the greenish colour found in most glass."),
            M("Lecturer", "Clear glass had consequences far beyond beautiful tableware. Once craftsmen could produce clear, even pieces of glass, they could grind them into lenses. The first practical use of lenses was in spectacles, which appeared in Italy towards the end of the thirteenth century and allowed older scholars to go on reading. Later, lenses made possible the telescope and the microscope, two instruments that completely changed our understanding of the universe and of living things."),
            M("Lecturer", "For centuries, however, making large flat sheets of glass for windows remained difficult and expensive. The breakthrough came in the 1950s, with what is called the float process. In this method, molten glass is poured onto a bath of liquid tin. The glass floats on the surface and spreads out into a perfectly flat, smooth sheet. Today, almost all the window glass in the world is made in this way."),
            M("Lecturer", "Finally, what about the future? One exciting area of research is so-called smart glass, which can change from clear to dark when an electric current is applied. In buildings, this helps to control heat from the sun, and so it can reduce the need for air conditioning. And there's good news for the environment, too. Glass can be recycled again and again without any loss of quality, so a bottle you throw away today could become part of a new bottle within a few weeks."),
            M("Lecturer", "In the next lecture, we'll look more closely at how glass is made in modern factories, and at some of the problems engineers still face."),
        ],
        [
            new("gap", "Complete the notes below.\nWrite ONE WORD ONLY for each answer.", 1,
            [
                Gap(31, "Obsidian: natural volcanic glass, used by early humans to make ___ such as knives", ["tools"], "\"Early humans used it to make tools such as knives and arrowheads.\""),
                Gap(32, "Mesopotamia: the earliest glass objects were probably small ___ worn as jewellery", ["beads"], "\"The earliest glass objects were not windows or bottles but small beads.\""),
                Gap(33, "Glassblowing: glass became cheap, and simple ___ were common in ordinary homes", ["cups"], "\"Simple cups became a common sight on the table.\""),
                Gap(34, "Roman window glass: thick, uneven and not fully ___", ["transparent"], "\"It was not fully transparent.\""),
                Gap(35, "Venice: glassmakers moved to Murano because of the danger of ___", ["fire"], "\"The main reason was the danger of fire.\""),
                Gap(36, "Cristallo: clear glass produced partly by adding ___", ["manganese"], "\"By adding a mineral called manganese, which removed the greenish colour.\""),
                Gap(37, "Lenses: first practically used in ___", ["spectacles"], "\"The first practical use of lenses was in spectacles.\""),
                Gap(38, "Float process (1950s): molten glass is poured onto liquid ___", ["tin"], "\"Molten glass is poured onto a bath of liquid tin.\""),
                Gap(39, "Smart glass: helps to control ___ from the sun", ["heat"], "\"This helps to control heat from the sun.\""),
                Gap(40, "Recycling: glass can be recycled without any loss of ___", ["quality"], "\"Glass can be recycled again and again without any loss of quality.\""),
            ]),
        ]);

    // =====================================================================
    // READING (Academic)
    // =====================================================================

    private const string TfngInstructions =
        "Do the following statements agree with the information given in Reading Passage 1?\n" +
        "TRUE — if the statement agrees with the information\nFALSE — if the statement contradicts the information\nNOT GIVEN — if there is no information on this";

    private const string YnngInstructions =
        "Do the following statements agree with the views of the writer in Reading Passage 3?\n" +
        "YES — if the statement agrees with the views of the writer\nNO — if the statement contradicts the views of the writer\nNOT GIVEN — if it is impossible to say what the writer thinks about this";

    private static ReadingPassage ReadingPassage1() => new("How Honeybees Share Directions",
        """
        For thousands of years, beekeepers noticed that when one bee discovered a good source of food, many others from the same hive soon arrived at it. It seemed natural to assume that the bees simply followed one another, or that they were guided by smell. The real explanation turned out to be far more remarkable, and it was uncovered mainly through the patient work of the Austrian scientist Karl von Frisch in the first half of the twentieth century.

        Von Frisch began by training bees to visit dishes of sugar water placed at known distances from their hive. He marked individual foragers with small dots of paint so that he could recognise them when they returned home. To observe their behaviour inside the hive, he used glass-walled hives, which allowed him to watch the bees on the vertical surface of the honeycomb. What he saw was a series of repeated movements, which he called dances, performed by the returning foragers in front of their sisters.

        Von Frisch identified two main forms. When food was close to the hive, within about fifty metres, the forager performed what he named the round dance, running in small circles, first one way and then the other. This dance appeared to tell other bees only that food was nearby, encouraging them to search the area around the hive. When the food was further away, the forager performed a very different movement: the waggle dance. In this, the bee runs forward in a straight line while shaking its body from side to side, then circles back to its starting point and repeats the straight run, alternating between a loop to the left and a loop to the right, so that the overall pattern resembles a figure of eight.

        The waggle dance, von Frisch discovered, contains two pieces of information. The first is direction. Because the dance takes place on a vertical comb in the dark, the bee uses gravity as a reference. The angle of the straight run in relation to 'straight up' on the comb corresponds to the angle between the food source and the sun, as seen from the hive entrance. If the bee dances straight upwards, the food lies directly towards the sun; if it dances straight downwards, the food is in the opposite direction. The second piece of information is distance. The longer the waggle part of the dance lasts, the further away the food is. Researchers have since estimated that each second of waggling represents roughly a kilometre, although the exact relationship varies between colonies.

        Remarkably, the bees also allow for the movement of the sun across the sky during the day. A forager that has been kept inside the hive for some time will adjust the angle of its dance when it is released, showing that bees possess an internal clock. The dancers also communicate the quality of the food. A forager that has found a rich source dances with greater energy and repeats the dance more times than one that has found only a poor source, so more recruits are sent to the best locations.

        Not everyone accepted von Frisch's conclusions at once. In the 1960s, a group of American researchers argued that bees found food mainly by smell, and that the dance itself carried little useful information. The debate continued for decades. It was finally settled by experiments using a small robotic bee, which could perform dances pointing to places where no real bee had been. When recruits flew towards these places, it became clear that the dance alone was enough to guide them. Later studies using tiny radar transmitters attached to bees' backs confirmed that recruits fly in the direction indicated by the dance, although they probably use smell to find the exact spot once they are close.

        In 1973, von Frisch shared the Nobel Prize in Physiology or Medicine with two other scientists who studied animal behaviour. His work is still regarded as one of the clearest examples of symbolic communication in any animal other than humans. Today, the dance language has found some unexpected practical uses. By decoding hundreds of dances recorded on video, scientists can build maps showing where the bees in a particular area are finding food. Such maps have suggested, for example, that bees living near towns often travel shorter distances than those in areas of intensive farming, which may mean that gardens provide a more reliable supply of flowers than large fields of a single crop.
        """,
        [
            new("tfng", TfngInstructions, null,
            [
                Stmt(1, "Before von Frisch's research, beekeepers had noticed that many bees would arrive at a food source soon after one bee had found it.", "TRUE",
                    "\"Beekeepers noticed that when one bee discovered a good source of food, many others from the same hive soon arrived at it.\""),
                Stmt(2, "Von Frisch carried out his first experiments with bees in his own garden.", "NOT GIVEN",
                    "The passage describes his methods but never says where the first experiments took place."),
                Stmt(3, "The round dance tells other bees the exact direction of nearby food.", "FALSE",
                    "\"This dance appeared to tell other bees only that food was nearby.\""),
                Stmt(4, "The overall movement of the waggle dance is similar in shape to a figure of eight.", "TRUE",
                    "\"The overall pattern resembles a figure of eight.\""),
                Stmt(5, "A bee dancing straight down the comb is showing that food lies in the direction of the sun.", "FALSE",
                    "\"If it dances straight downwards, the food is in the opposite direction.\""),
                Stmt(6, "Bees that have found a rich source of food dance more energetically.", "TRUE",
                    "\"A forager that has found a rich source dances with greater energy.\""),
                Stmt(7, "The American researchers eventually agreed that von Frisch had been right.", "NOT GIVEN",
                    "The passage says the debate was settled by experiments, but not how the American researchers reacted."),
            ]),
            new("gap", "Complete the notes below.\nChoose NO MORE THAN TWO WORDS from the passage for each answer.", 2,
            [
                Gap(8, "In the dark hive, the dancing bee uses ___ as a reference for direction.", ["gravity"],
                    "\"The bee uses gravity as a reference.\""),
                Gap(9, "How long the waggle part of the dance lasts shows the ___ to the food.", ["distance"],
                    "\"The second piece of information is distance. The longer the waggle part ... the further away the food is.\""),
                Gap(10, "Bees can adjust their dance for the sun's movement because they have an ___.", ["internal clock"],
                    "\"Showing that bees possess an internal clock.\""),
                Gap(11, "The long debate was settled by experiments with a ___.", ["robotic bee"],
                    "\"It was finally settled by experiments using a small robotic bee.\""),
                Gap(12, "When they are close to the food, recruits probably use ___ to find the exact spot.", ["smell"],
                    "\"They probably use smell to find the exact spot once they are close.\""),
                Gap(13, "By decoding filmed dances, scientists can build ___ of where bees find food.", ["maps"],
                    "\"Scientists can build maps showing where the bees in a particular area are finding food.\""),
            ]),
        ]);

    private static ReadingPassage ReadingPassage2() => new("Why Cities Run Hot",
        """
        On a still summer evening, a person travelling by train from the countryside into the centre of a large city may notice the air becoming steadily warmer, even though the sun has already set. This effect, known as the urban heat island, was first described in the early nineteenth century by Luke Howard, an English chemist who is better known today for giving clouds the names we still use. Comparing his own measurements in London with those taken outside the city, Howard found that the centre was consistently warmer, and that the difference was greatest at night. Two centuries later, his basic observation has been confirmed in cities all over the world.

        The size of the effect varies considerably. In a small town, the difference may be less than one degree Celsius. In a large, densely built city on a calm, clear night, the centre can be up to ten degrees warmer than the surrounding fields. The difference is usually smaller during the day, and it almost disappears in windy weather, when moving air mixes warm and cool air across the whole region.

        Several causes act together. The most important is the replacement of soil and plants by materials such as asphalt, concrete and brick. Dark road surfaces absorb a large proportion of the sunlight that falls on them, and these dense materials store heat during the day and release it slowly after dark, which explains why cities cool down so slowly at night. In the countryside, by contrast, much of the sun's energy is used to evaporate water from soil and leaves. This process, known as evapotranspiration, works rather like sweating: it cools the surface. Because cities have fewer plants, and because rainwater is quickly carried away by drains, far less of this natural cooling takes place.

        The shape of the city also matters. Where tall buildings line narrow streets, forming what scientists call urban canyons, heat given off by the ground and walls at night is partly absorbed by the buildings opposite instead of escaping to the sky. The buildings also act as a barrier to wind, which would otherwise carry warm air away. Finally, cities produce heat of their own. Vehicles, factories and the air conditioners that keep offices cool all release waste heat into the streets, creating a cycle in which hotter weather leads to more air conditioning, which in turn warms the air outside.

        The consequences go beyond discomfort. Higher temperatures increase the demand for electricity, and during heatwaves they can put the health of elderly people and young children at serious risk, particularly when high night temperatures prevent the body from recovering from the heat of the day. Warm city air can also speed up the chemical reactions that produce ground-level ozone, a pollutant that irritates the lungs. Not all the effects are negative, however. In cold climates, the heat island can shorten the winter and reduce the energy needed for heating homes.

        Fortunately, the heat island is one of the few climate problems that cities can tackle directly. One of the simplest measures is the 'cool roof': covering roofs with white or highly reflective materials so that they absorb less sunlight. Studies suggest that a white roof can remain many degrees cooler than a dark one on a sunny afternoon, reducing the need for air conditioning in the rooms below. A related idea is to use lighter-coloured paving for roads and car parks, although critics point out that the reflected light can cause glare for drivers and pedestrians.

        Green roofs, which are covered with a layer of soil and plants, offer additional benefits. As well as cooling the building, they hold rainwater, provide a home for insects and birds, and can last longer than conventional roofs because the plants protect the waterproof layer beneath them from sunlight. Their main drawback is cost: they are heavier and more expensive to install, and many older buildings would need their structure strengthened before they could support one.

        Perhaps the most effective tool of all is the ordinary street tree. Trees cool the air through evapotranspiration and by shading the pavements and walls around them. Several cities have now set targets for increasing their tree canopy, the proportion of the city covered by leaves when seen from above. Planners warn, however, that trees must be chosen carefully. A species that needs a lot of water may not survive in a city where summers are becoming longer and drier, so many planners are now choosing trees from warmer regions that are expected to cope better with the climate of the future.
        """,
        [
            new("mcq", Abcd, null,
            [
                Mcq(14, "What did Luke Howard discover?",
                    ["London was warmer than the area around it, especially at night.", "The heat island effect was strongest in the middle of the day.", "Clouds formed more often over London than over the countryside.", "London had become warmer during his lifetime."], "A",
                    "\"Howard found that the centre was consistently warmer, and that the difference was greatest at night.\""),
                Mcq(15, "According to the second paragraph, the heat island effect is weakest",
                    ["in the centre of large cities.", "on calm, clear nights.", "in windy weather.", "in the early evening."], "C",
                    "\"It almost disappears in windy weather.\""),
                Mcq(16, "Why do cities cool down slowly at night?",
                    ["Dark surfaces reflect most of the sunlight.", "Water evaporates more slowly after dark.", "Drains carry warm rainwater into the streets.", "Building materials release stored heat gradually."], "D",
                    "\"These dense materials store heat during the day and release it slowly after dark, which explains why cities cool down so slowly at night.\""),
                Mcq(17, "What does the writer say about air conditioners?",
                    ["They contribute to a cycle of increasing heat.", "They reduce the temperature of city streets.", "They are the main cause of the heat island effect.", "They are used less in cities with tall buildings."], "A",
                    "\"Creating a cycle in which hotter weather leads to more air conditioning, which in turn warms the air outside.\""),
                Mcq(18, "Which positive effect of the heat island is mentioned?",
                    ["lower heating needs in cold places", "cleaner air", "lower demand for electricity", "faster growth of plants"], "A",
                    "\"In cold climates, the heat island can ... reduce the energy needed for heating homes.\""),
                Mcq(19, "What concern do critics raise about lighter-coloured paving?",
                    ["It is more expensive than dark paving.", "It can dazzle people using the streets.", "It wears out quickly.", "It stops rainwater from draining away."], "B",
                    "\"The reflected light can cause glare for drivers and pedestrians.\""),
            ]),
            new("gap", "Complete the sentences below.\nChoose NO MORE THAN TWO WORDS from the passage for each answer.", 2,
            [
                Gap(20, "Luke Howard is best known today for naming ___.", ["clouds"],
                    "\"Better known today for giving clouds the names we still use.\""),
                Gap(21, "On a calm, clear night, a large city centre may be up to ___ warmer than the fields around it.", ["ten degrees", "10 degrees"],
                    "\"The centre can be up to ten degrees warmer than the surrounding fields.\""),
                Gap(22, "Cities lose natural cooling partly because rainwater is quickly removed by ___.", ["drains"],
                    "\"Rainwater is quickly carried away by drains.\""),
                Gap(23, "Narrow streets with tall buildings on both sides are known as ___.", ["urban canyons"],
                    "\"Forming what scientists call urban canyons.\""),
                Gap(24, "Hot nights are dangerous because they stop the body from ___ after a hot day.", ["recovering"],
                    "\"High night temperatures prevent the body from recovering from the heat of the day.\""),
                Gap(25, "Green roofs last longer because plants protect the ___ from sunlight.", ["waterproof layer"],
                    "\"The plants protect the waterproof layer beneath them from sunlight.\""),
                Gap(26, "Many planners now choose trees from ___ because they should cope better with future conditions.", ["warmer regions"],
                    "\"Many planners are now choosing trees from warmer regions.\""),
            ]),
        ]);

    private static ReadingPassage ReadingPassage3() => new("Is Earlier Always Better?",
        """
        Walk into almost any bookshop and you will find shelves of materials promising to teach toddlers a foreign language. Parents around the world are paying for classes for children who can barely speak their own language, and many education systems have moved the start of compulsory language lessons into the first years of primary school. Behind all this activity lies a belief that seems almost too obvious to question: when it comes to learning languages, the younger the better. In my view, this belief is not so much wrong as badly misunderstood, and the misunderstanding is costing families and schools a great deal of money and effort.

        The idea has respectable origins. In the 1960s, the neurologist Eric Lenneberg proposed that there is a 'critical period' for language, a window in childhood after which the brain loses much of its ability to acquire a language naturally. Later research on immigrants provided some support: people who moved to a new country as young children usually ended up speaking its language with a native-like accent, while those who arrived as adults rarely did, however long they stayed. These findings are genuine, and I have no wish to dismiss them.

        The problem lies in what happens when they are carried from one situation to a very different one. A child who moves to another country is surrounded by the new language for most of the day, at school, in the playground and on television, for years. A child in a typical school programme at home, by contrast, may receive two lessons of forty minutes a week, taught by a teacher who is not always confident in the language. To expect the second child to benefit in the same way as the first is, I would argue, like expecting someone who takes a bath once a week to become a strong swimmer.

        Studies of school programmes support this. A large research project in Spain compared pupils who had started English at the age of eight with others who had started at eleven, at points when both groups had received the same total number of hours of teaching. The later starters learnt faster, and on several measures, especially grammar and reading, they actually performed better. Older learners, it seems, bring advantages of their own: they can already read and write well in their first language, they can follow explanations of how a language works, and they are better at planning their own learning.

        None of this means that young children should be kept away from other languages. There are good reasons for introducing them early, but they are not the reasons usually given. Early lessons can make children curious about other cultures and less anxious about producing unfamiliar sounds, and these attitudes may matter more in the long run than any vocabulary they acquire. Pronunciation is the one area where an early start seems to offer a clear advantage, provided the children hear plenty of good models. What early lessons cannot do on their own is produce fluent speakers.

        The real danger, in my opinion, is that the belief in an early start distracts attention from the factors that make the greatest difference. The first is the sheer amount of contact with the language. A few hours a week, however early they begin, will never match regular exposure through films, books, games and conversation. The second is the quality of teaching: an enthusiastic teacher with a good command of the language is worth more than several extra years of lessons with one who lacks confidence. Schools that introduce languages at six but cannot find qualified teachers may be doing more harm than good, since children who are bored by a subject at seven are unlikely to choose it at fourteen.

        There is also a cost to adults, who have been told so often that their window has closed that many never try. This is a pity. Adults may find it harder to lose their accent, but an accent has never prevented anyone from being understood, and adults who study regularly often make faster progress than children in the early stages. I have met many people who began a new language in their forties or fifties and now use it confidently at work.

        So what should parents and schools do? Rather than asking when language learning should begin, we should be asking how much of the language learners will hear and use, and who will teach them. An early start in a well-resourced programme is a good thing; an early start in a poor one is simply a longer route to the same place. For parents, the most useful investment may not be an expensive class for a three-year-old but finding ways to make the language a normal part of family life, through songs, stories or holidays. As for adults, the message should be simple: it is never too late to start.
        """,
        [
            new("ynng", YnngInstructions, null,
            [
                Stmt(27, "The belief that young children learn languages best is completely wrong.", "NO",
                    "\"This belief is not so much wrong as badly misunderstood.\""),
                Stmt(28, "The research on people who moved to a new country as children should be taken seriously.", "YES",
                    "\"These findings are genuine, and I have no wish to dismiss them.\""),
                Stmt(29, "Most primary school language teachers have had too little training.", "NOT GIVEN",
                    "The writer says some teachers lack confidence or qualifications, but makes no claim about most teachers' training."),
                Stmt(30, "Early language lessons can have a positive effect on children's attitudes.", "YES",
                    "\"Early lessons can make children curious about other cultures and less anxious ... these attitudes may matter more in the long run.\""),
                Stmt(31, "Starting young gives learners no advantage in pronunciation.", "NO",
                    "\"Pronunciation is the one area where an early start seems to offer a clear advantage.\""),
                Stmt(32, "Governments should spend more money on language courses for adults.", "NOT GIVEN",
                    "The writer encourages adults to start but says nothing about government spending on adult courses."),
            ]),
            new("mcq", Abcd, null,
            [
                Mcq(33, "The writer mentions someone who takes a bath once a week in order to show that",
                    ["learners need a regular routine.", "children should learn to swim and to speak a language together.", "immigrant children learn faster than adults.", "limited contact with a language brings limited results."], "D",
                    "It compares a child with \"two lessons of forty minutes a week\" to a child \"surrounded by the new language for most of the day\"."),
                Mcq(34, "What did the research project in Spain find?",
                    ["Children who started at eight were better at grammar.", "After the same number of hours, later starters did as well or better.", "Both groups needed more hours of teaching than planned.", "Older pupils found reading more difficult."], "B",
                    "\"The later starters learnt faster, and on several measures, especially grammar and reading, they actually performed better.\""),
                Mcq(35, "According to the writer, early lessons with poorly qualified teachers may be harmful because",
                    ["children may learn incorrect pronunciation.", "parents may stop supporting the school.", "the lessons take time away from other subjects.", "children may lose interest in the subject."], "D",
                    "\"Children who are bored by a subject at seven are unlikely to choose it at fourteen.\""),
                Mcq(36, "What is the writer's main purpose in the passage?",
                    ["to argue that language teaching should begin at secondary school", "to explain the history of the critical period idea", "to move attention from starting age to the amount and quality of learning", "to persuade parents to pay for classes for very young children"], "C",
                    "\"Rather than asking when language learning should begin, we should be asking how much of the language learners will hear and use, and who will teach them.\""),
            ]),
            new("gap", "Complete the sentences below.\nChoose NO MORE THAN TWO WORDS from the passage for each answer.", 2,
            [
                Gap(37, "Many adults never try to learn a language because they have been told that their ___ has closed.", ["window"],
                    "\"Adults, who have been told so often that their window has closed that many never try.\""),
                Gap(38, "Adults may find it harder to lose their ___, but this does not stop them from being understood.", ["accent"],
                    "\"Adults may find it harder to lose their accent, but an accent has never prevented anyone from being understood.\""),
                Gap(39, "Adults who study regularly often progress faster than children in the ___.", ["early stages"],
                    "\"Adults who study regularly often make faster progress than children in the early stages.\""),
                Gap(40, "Instead of paying for expensive classes, parents could make the language a normal part of ___.", ["family life"],
                    "\"Finding ways to make the language a normal part of family life, through songs, stories or holidays.\""),
            ]),
        ]);
}
