import type { GrammarLesson, L3 } from './types';

// Qoʻshimcha grammatika darslari (A2–C1): har bir bosqichga yana 4 tadan.
// Original tushuntirishlar va mashqlar. Har bir tushuntirish uch tilda: oʻzbekcha, ruscha, inglizcha.
const t = (uz: string, ru: string, en: string): L3 => ({ uz, ru, en });

export const GRAMMAR_MORE: GrammarLesson[] = [
  // ───────────────────────────── A2 ─────────────────────────────
  {
    id: 'a2-countable-some-any',
    level: 'A2',
    title: t(
      "Sanaladigan va sanalmaydigan otlar: some, any, much, many",
      "Исчисляемые и неисчисляемые существительные: some, any, much, many",
      "Countable and uncountable nouns: some, any, much, many",
    ),
    summary: t(
      "Ingliz tilida otlar sanaladigan (an apple, two apples) va sanalmaydigan (water, rice, information) turlarga boʻlinadi. Some, any, much, many yoki a lot of dan qaysi birini tanlash aynan shu farqqa bogʻliq.",
      "В английском существительные делятся на исчисляемые (an apple, two apples) и неисчисляемые (water, rice, information). От этого зависит, что выбрать: some, any, much, many или a lot of.",
      "English nouns are either countable (an apple, two apples) or uncountable (water, rice, information). This decides whether you use some, any, much, many or a lot of.",
    ),
    rules: [
      {
        text: t(
          "Sanaladigan otlarning birlik va koʻplik shakli bor, birlikda ular a/an oladi: a book, three books. Sanalmaydigan otlar (water, bread, money, advice, information, homework, news) koʻplik shakliga ega emas, a/an olmaydi va fe’l ular bilan birlikda keladi.",
          "У исчисляемых существительных есть единственное и множественное число, в единственном числе они употребляются с a/an: a book, three books. Неисчисляемые (water, bread, money, advice, information, homework, news) не имеют множественного числа, не употребляются с a/an, а глагол после них стоит в единственном числе.",
          "Countable nouns have singular and plural forms and take a/an in the singular: a book, three books. Uncountable nouns (water, bread, money, advice, information, homework, news) have no plural, never take a/an and are followed by a singular verb.",
        ),
        examples: ["I have two brothers and a sister.", "The information on this website is very useful.", "Can you give me some advice?"],
      },
      {
        text: t(
          "Some odatda tasdiq gaplarda, any esa inkor va soʻroq gaplarda ishlatiladi. Ikkalasi ham koʻplikdagi sanaladigan otlar bilan ham, sanalmaydigan otlar bilan ham keladi. Taklif va iltimosda gap soʻroq boʻlsa ham some ishlatiladi: Would you like some tea?",
          "Some обычно используется в утвердительных предложениях, any — в отрицаниях и вопросах. Оба слова сочетаются и с исчисляемыми во множественном числе, и с неисчисляемыми. В предложениях и просьбах используется some, даже если это вопрос: Would you like some tea?",
          "Use some in positive sentences and any in negatives and questions. Both go with plural countable nouns and with uncountable nouns. In offers and requests, use some even in a question: Would you like some tea?",
        ),
        examples: ["There are some eggs in the fridge.", "We don't have any milk left.", "Would you like some cake?"],
      },
      {
        text: t(
          "Many — sanaladigan otlar bilan, much — sanalmaydigan otlar bilan ishlatiladi, asosan inkor va soʻroq gaplarda. «Qancha?» soʻrogʻi ham shunga koʻra: How many…? / How much…? Tasdiq gaplarda esa ikkala tur uchun ham a lot of qulayroq.",
          "Many употребляется с исчисляемыми, much — с неисчисляемыми, в основном в отрицаниях и вопросах. Вопрос «сколько?» строится так же: How many…? / How much…? В утвердительных предложениях для обоих типов удобнее a lot of.",
          "Many goes with countable nouns and much with uncountable nouns, mostly in negatives and questions: How many…? / How much…? In positive sentences, a lot of sounds more natural with both types.",
        ),
        examples: ["How many languages do you speak?", "I don't have much time today.", "She drinks a lot of coffee."],
      },
    ],
    mistakes: [
      {
        wrong: "I need an information about the course.",
        right: "I need some information about the course.",
        note: t(
          "Information — sanalmaydigan ot: u a/an olmaydi va koʻplikda (informations) kelmaydi.",
          "Information — неисчисляемое существительное: без a/an и без формы множественного числа (informations).",
          "Information is uncountable: no a/an and no plural form.",
        ),
      },
      {
        wrong: "She gave me many advices.",
        right: "She gave me a lot of advice.",
        note: t(
          "Oʻzbek va rus tillarida «maslahat» / «совет» sanaladi, ingliz tilida advice esa sanalmaydi. Bitta maslahat — a piece of advice.",
          "По-русски «совет» можно посчитать, а английское advice — неисчисляемое. Один совет — a piece of advice.",
          "Advice is uncountable in English. For one, say a piece of advice.",
        ),
      },
      {
        wrong: "How much people came to the party?",
        right: "How many people came to the party?",
        note: t(
          "People — sanaladigan otning koʻplik shakli, shuning uchun how many ishlatiladi.",
          "People — это множественное число исчисляемого существительного, поэтому нужен how many.",
          "People is a plural countable noun, so use how many.",
        ),
      },
      {
        wrong: "The news are very good today.",
        right: "The news is very good today.",
        note: t(
          "News -s bilan tugasa ham, sanalmaydigan va birlikdagi ot hisoblanadi, shuning uchun fe’l is boʻladi.",
          "Хотя news оканчивается на -s, это неисчисляемое существительное в единственном числе, поэтому глагол — is.",
          "News ends in -s, but it is uncountable and takes a singular verb.",
        ),
      },
    ],
    quiz: [
      {
        prompt: "There isn't ___ sugar in my tea.",
        options: ["any", "some", "many"],
        answer: 0,
        why: t(
          "Inkor gap va sanalmaydigan ot (sugar) — any ishlatiladi.",
          "Отрицательное предложение и неисчисляемое существительное (sugar) — нужен any.",
          "It's a negative sentence with an uncountable noun, so we use any.",
        ),
      },
      {
        prompt: "How ___ students are there in your class?",
        options: ["much", "many", "lot"],
        answer: 1,
        why: t(
          "Students — sanaladigan ot, koʻplikda: how many.",
          "Students — исчисляемое существительное во множественном числе: how many.",
          "Students is a plural countable noun, so it's how many.",
        ),
      },
      {
        prompt: "I'd like ___ water, please.",
        options: ["a", "many", "some"],
        answer: 2,
        why: t(
          "Iltimosda some ishlatiladi; water sanalmaydi, shuning uchun a ham, many ham toʻgʻri kelmaydi.",
          "В просьбе используется some; water — неисчисляемое, поэтому ни a, ни many не подходят.",
          "We use some in requests; water is uncountable, so a and many are wrong.",
        ),
      },
      {
        prompt: "Hurry up! We don't have ___ time.",
        options: ["many", "much", "a"],
        answer: 1,
        why: t(
          "Time bu ma’noda sanalmaydi, inkor gapda esa much ishlatiladi.",
          "Time в этом значении неисчисляемое, а в отрицании используется much.",
          "Time is uncountable here, and much is used in negatives.",
        ),
      },
      {
        prompt: "Your advice ___ very helpful. Thank you!",
        options: ["were", "are", "was"],
        answer: 2,
        why: t(
          "Advice — sanalmaydigan ot, u bilan fe’l birlikda keladi: was.",
          "Advice — неисчисляемое существительное, глагол после него в единственном числе: was.",
          "Advice is uncountable, so the verb is singular: was.",
        ),
      },
      {
        prompt: "Are there ___ good cafés near here?",
        options: ["much", "a", "an", "any"],
        answer: 3,
        why: t(
          "Soʻroq gap va koʻplikdagi sanaladigan ot (cafés) — any.",
          "Вопрос и исчисляемое существительное во множественном числе (cafés) — any.",
          "It's a question with a plural countable noun, so we use any.",
        ),
      },
    ],
  },
  {
    id: 'a2-prepositions-time-place',
    level: 'A2',
    title: t(
      "In, on, at: vaqt va joy predloglari",
      "In, on, at: предлоги времени и места",
      "Prepositions of time and place: in, on, at",
    ),
    summary: t(
      "In, on va at kichik soʻzlar, lekin eng koʻp xato aynan ularda qilinadi. Oʻzbek tilida bularning oʻrnini koʻpincha bitta -da qoʻshimchasi bosadi, rus tilida esa «в» va «на», shuning uchun har bir holatni alohida eslab qolgan ma’qul.",
      "In, on и at — маленькие слова, но ошибок в них больше всего. В русском языке им соответствуют «в» и «на», и совпадают они далеко не всегда, поэтому каждый случай лучше запомнить.",
      "In, on and at are small words, but they cause a lot of mistakes. Your first language often uses one form (-da in Uzbek, в/на in Russian) where English uses three, so learn each pattern.",
    ),
    rules: [
      {
        text: t(
          "Vaqt: at — soat va aniq payt uchun (at 7 o'clock, at noon, at night); on — hafta kunlari va sanalar uchun (on Monday, on 5 May, on my birthday); in — oylar, yillar, fasllar va kunning qismlari uchun (in July, in 2024, in winter, in the morning).",
          "Время: at — с часами и точными моментами (at 7 o'clock, at noon, at night); on — с днями недели и датами (on Monday, on 5 May, on my birthday); in — с месяцами, годами, временами года и частями суток (in July, in 2024, in winter, in the morning).",
          "Time: at for clock times and exact points (at 7 o'clock, at noon, at night); on for days and dates (on Monday, on 5 May, on my birthday); in for months, years, seasons and parts of the day (in July, in 2024, in winter, in the morning).",
        ),
        examples: ["The lesson starts at 9 o'clock.", "My birthday is on 12 March.", "We usually travel in summer."],
      },
      {
        text: t(
          "This, next, last, every, tomorrow, yesterday soʻzlaridan oldin predlog qoʻyilmaydi.",
          "Перед this, next, last, every, tomorrow, yesterday предлог не ставится.",
          "Don't use a preposition before this, next, last, every, tomorrow or yesterday.",
        ),
        examples: ["I'll see you next Friday.", "She visited us last week.", "We play football every Sunday."],
      },
      {
        text: t(
          "Joy: in — biror narsaning ichida yoki shahar, mamlakat (in the box, in Tashkent, in Uzbekistan); on — sirt ustida, qavatda, avtobus, poyezd va samolyotda (on the table, on the second floor, on the bus); at — aniq nuqta yoki faoliyat joyi (at the door, at the bus stop, at school, at work, at home).",
          "Место: in — внутри чего-то, а также с городами и странами (in the box, in Tashkent, in Uzbekistan); on — на поверхности, на этаже, в автобусе, поезде и самолёте (on the table, on the second floor, on the bus); at — в точке или месте занятия (at the door, at the bus stop, at school, at work, at home).",
          "Place: in for inside a space, a city or a country (in the box, in Tashkent, in Uzbekistan); on for surfaces, floors and public transport (on the table, on the second floor, on the bus); at for a point or a place where an activity happens (at the door, at the bus stop, at school, at work, at home).",
        ),
        examples: ["My keys are on the table.", "He lives in Samarkand.", "I'll meet you at the station."],
      },
    ],
    mistakes: [
      {
        wrong: "I was born at 2005.",
        right: "I was born in 2005.",
        note: t(
          "Yillar, oylar va fasllar bilan in ishlatiladi.",
          "С годами, месяцами и временами года используется in.",
          "Use in with years, months and seasons.",
        ),
      },
      {
        wrong: "See you in Monday!",
        right: "See you on Monday!",
        note: t(
          "Hafta kunlari va sanalar bilan doim on ishlatiladi.",
          "С днями недели и датами всегда используется on.",
          "Days and dates always take on.",
        ),
      },
      {
        wrong: "I'll call you on next week.",
        right: "I'll call you next week.",
        note: t(
          "Next, last, this, every dan oldin predlog kerak emas.",
          "Перед next, last, this, every предлог не нужен.",
          "No preposition before next, last, this or every.",
        ),
      },
      {
        wrong: "He is in the bus now.",
        right: "He is on the bus now.",
        note: t(
          "Rus tilidagi «в автобусе» ta’sirida in deyiladi, lekin avtobus, poyezd, samolyot bilan on ishlatiladi. Mashina va taksi bilan esa in: in a car, in a taxi.",
          "Под влиянием «в автобусе» говорят in, но с bus, train, plane нужен on. А с машиной и такси — in: in a car, in a taxi.",
          "Use on with buses, trains and planes, but in with cars and taxis.",
        ),
      },
    ],
    quiz: [
      {
        prompt: "The film starts ___ 8 p.m.",
        options: ["in", "on", "at"],
        answer: 2,
        why: t(
          "Soat bilan at ishlatiladi.",
          "С указанием часов используется at.",
          "We use at with clock times.",
        ),
      },
      {
        prompt: "We got married ___ 2019.",
        options: ["in", "at", "on"],
        answer: 0,
        why: t(
          "Yil bilan in ishlatiladi.",
          "С годом используется in.",
          "We use in with years.",
        ),
      },
      {
        prompt: "Let's meet ___ Saturday morning.",
        options: ["in", "on", "at"],
        answer: 1,
        why: t(
          "Aniq kun tilga olinsa, «ertalab» boʻlsa ham on ishlatiladi: on Saturday morning.",
          "Если назван конкретный день, даже с «утром» используется on: on Saturday morning.",
          "When a specific day is named, use on, even with morning: on Saturday morning.",
        ),
      },
      {
        prompt: "Put the plates ___ the table, please.",
        options: ["in", "at", "on"],
        answer: 2,
        why: t(
          "Stol — sirt, likoplar uning ustida turadi: on.",
          "Стол — поверхность, тарелки стоят на нём: on.",
          "A table is a surface, so we use on.",
        ),
      },
      {
        prompt: "I'm going to visit my cousins in Bukhara ___.",
        options: ["on next week", "next week", "in next week"],
        answer: 1,
        why: t(
          "Next dan oldin predlog qoʻyilmaydi.",
          "Перед next предлог не ставится.",
          "There is no preposition before next.",
        ),
      },
      {
        prompt: "Wait for me ___ the bus stop.",
        options: ["in", "on", "at", "to"],
        answer: 2,
        why: t(
          "Bekat — aniq nuqta, shuning uchun at.",
          "Остановка — конкретная точка, поэтому at.",
          "A bus stop is a point, so we use at.",
        ),
      },
    ],
  },
  {
    id: 'a2-can-could',
    level: 'A2',
    title: t(
      "Can va could: qobiliyat, ruxsat va iltimos",
      "Can и could: умение, разрешение и просьба",
      "Can and could: ability, permission and requests",
    ),
    summary: t(
      "Can va could yordamida biror ishni qila olish-olmaslikni aytamiz, ruxsat soʻraymiz va xushmuomalalik bilan iltimos qilamiz. Bu kundalik muloqotda eng koʻp ishlatiladigan modal fe’llardan.",
      "С помощью can и could мы говорим об умениях, спрашиваем разрешения и вежливо просим о чём-то. Это одни из самых частых модальных глаголов в повседневной речи.",
      "We use can and could to talk about ability, to ask for permission and to make polite requests. They are among the most common modal verbs in everyday English.",
    ),
    rules: [
      {
        text: t(
          "Can + fe’lning boshlangʻich shakli — hozirgi qobiliyat. Can dan keyin to qoʻyilmaydi, he/she/it bilan ham -s qoʻshilmaydi. Inkor — can’t (cannot), soʻroq — Can you…?",
          "Can + начальная форма глагола — умение в настоящем. После can не ставится to, и с he/she/it не добавляется -s. Отрицание — can't (cannot), вопрос — Can you…?",
          "Use can + base verb for present ability. There is no to after can and no -s with he/she/it. The negative is can't (cannot); the question is Can you…?",
        ),
        examples: ["I can swim, but I can't dive.", "Can your brother play the piano?", "She can speak three languages."],
      },
      {
        text: t(
          "Could — oʻtmishdagi umumiy qobiliyat: bolaligimda qila olardim. Kelajakda paydo boʻladigan qobiliyat uchun will be able to ishlatiladi, chunki can ning kelasi zamon shakli yoʻq.",
          "Could — умение в прошлом вообще: «в детстве я умел». Для умения, которое появится в будущем, используется will be able to, так как у can нет формы будущего времени.",
          "Could describes general ability in the past. For an ability you will have in the future, use will be able to, because can has no future form.",
        ),
        examples: ["When I was six, I could ride a bike.", "My grandfather couldn't use a computer.", "After this course, you will be able to write a formal email."],
      },
      {
        text: t(
          "Ruxsat va iltimos: Can I…? — oddiy, samimiy; Could I…? / Could you…? — xushmuomalaroq. Bu yerda could oʻtmishni emas, odobni bildiradi. Javob: Yes, of course / Sure / Sorry, you can’t.",
          "Разрешение и просьба: Can I…? — просто и по-дружески; Could I…? / Could you…? — вежливее. Здесь could выражает не прошлое, а вежливость. Ответ: Yes, of course / Sure / Sorry, you can't.",
          "For permission and requests, Can I…? is informal and Could I…? / Could you…? is more polite. Here could shows politeness, not the past. Answer with Yes, of course / Sure / Sorry, you can't.",
        ),
        examples: ["Can I open the window?", "Could you help me with this bag, please?", "Could I speak to the manager, please?"],
      },
    ],
    mistakes: [
      {
        wrong: "I can to swim.",
        right: "I can swim.",
        note: t(
          "Modal fe’llardan keyin to qoʻyilmaydi.",
          "После модальных глаголов to не ставится.",
          "Don't use to after a modal verb.",
        ),
      },
      {
        wrong: "She cans speak English.",
        right: "She can speak English.",
        note: t(
          "Can shaxsga qarab oʻzgarmaydi: he/she/it bilan ham can.",
          "Can не изменяется по лицам: с he/she/it тоже can.",
          "Can never changes: it's can with he, she and it too.",
        ),
      },
      {
        wrong: "Do you can help me?",
        right: "Can you help me?",
        note: t(
          "Soʻroq gapda do ishlatilmaydi — can oʻzi egadan oldinga chiqadi.",
          "В вопросе do не нужен — can сам ставится перед подлежащим.",
          "Don't use do: can itself moves before the subject.",
        ),
      },
      {
        wrong: "Next year I will can drive.",
        right: "Next year I will be able to drive.",
        note: t(
          "Will va can yonma-yon kelmaydi; kelasi zamonda will be able to ishlatiladi.",
          "Will и can не ставятся рядом; в будущем времени используется will be able to.",
          "Will and can don't go together; use will be able to for the future.",
        ),
      },
    ],
    quiz: [
      {
        prompt: "My little brother ___ read yet. He's only three.",
        options: ["can't", "doesn't can", "can not to"],
        answer: 0,
        why: t(
          "Can ning inkori — can’t; do va to ishlatilmaydi.",
          "Отрицание can — can't; do и to не нужны.",
          "The negative of can is can't, without do or to.",
        ),
      },
      {
        prompt: "___ you pass me the salt, please?",
        options: ["Do", "Could", "Are"],
        answer: 1,
        why: t(
          "Xushmuomala iltimos — Could you…?",
          "Вежливая просьба — Could you…?",
          "A polite request starts with Could you…?",
        ),
      },
      {
        prompt: "When she was young, she ___ run very fast.",
        options: ["can", "cans", "could"],
        answer: 2,
        why: t(
          "Oʻtmishdagi qobiliyat — could.",
          "Умение в прошлом — could.",
          "Past ability is could.",
        ),
      },
      {
        prompt: "Excuse me, ___ I use your phone for a minute?",
        options: ["can", "do", "am", "will"],
        answer: 0,
        why: t(
          "Ruxsat soʻrash — Can I…?",
          "Просьба о разрешении — Can I…?",
          "We ask for permission with Can I…?",
        ),
      },
      {
        prompt: "After this course, you ___ speak English more confidently.",
        options: ["can will", "will be able to", "could"],
        answer: 1,
        why: t(
          "Kelajakdagi qobiliyat — will be able to.",
          "Умение в будущем — will be able to.",
          "Future ability is will be able to.",
        ),
      },
      {
        prompt: "He ___ play chess very well.",
        options: ["can to", "is can", "can"],
        answer: 2,
        why: t(
          "Can + fe’lning boshlangʻich shakli, to va is siz.",
          "Can + начальная форма глагола, без to и is.",
          "Can is followed by the base verb, with no to or is.",
        ),
      },
    ],
  },
  {
    id: 'a2-possessives-object-pronouns',
    level: 'A2',
    title: t(
      "Egalik: ’s, my/mine va obyekt olmoshlari",
      "Притяжательность: 's, my/mine и объектные местоимения",
      "Possessives ('s, my/mine) and object pronouns",
    ),
    summary: t(
      "Narsa kimga tegishli ekanini ’s, my/your yoki mine/yours bilan bildiramiz. Me, him, her, them kabi obyekt olmoshlari esa fe’l va predlogdan keyin keladi. Oʻzbek tilida «u» jinsni farqlamaydi, shuning uchun his va her ga alohida e’tibor bering.",
      "Принадлежность выражается через 's, my/your или mine/yours. Объектные местоимения (me, him, her, them) стоят после глагола и предлога.",
      "We show who owns something with 's, my/your or mine/yours. Object pronouns such as me, him, her and them come after verbs and prepositions.",
    ),
    rules: [
      {
        text: t(
          "Odamlar va hayvonlarga tegishlilik ’s bilan bildiriladi: Aziz’s car. -s bilan tugaydigan koʻplik otlarga faqat apostrof qoʻshiladi: my parents’ house. Children, people kabi notoʻgʻri koʻplikka esa ’s: children’s. Narsalar uchun koʻpincha of ishlatiladi: the end of the film.",
          "Принадлежность людям и животным выражается через 's: Aziz's car. К существительным во множественном числе на -s добавляется только апостроф: my parents' house. К неправильным формам (children, people) — 's: children's. Для предметов чаще используется of: the end of the film.",
          "Use 's for people and animals: Aziz's car. Plural nouns ending in -s just add an apostrophe: my parents' house. Irregular plurals add 's: children's. For things, we often use of: the end of the film.",
        ),
        examples: ["This is Madina's phone.", "My parents' house is near the river.", "The children's toys are everywhere."],
      },
      {
        text: t(
          "My, your, his, her, its, our, their — otdan oldin keladi. Mine, yours, his, hers, ours, theirs esa otning oʻrnini bosadi va yolgʻiz ishlatiladi. «Kimning?» — Whose…?",
          "My, your, his, her, its, our, their стоят перед существительным. Mine, yours, his, hers, ours, theirs заменяют существительное и употребляются самостоятельно. «Чей?» — Whose…?",
          "My, your, his, her, its, our, their come before a noun. Mine, yours, his, hers, ours, theirs replace the noun and stand alone. To ask about the owner, use Whose…?",
        ),
        examples: ["This is my bag.", "This bag is mine.", "Whose pen is this? — It's hers."],
      },
      {
        text: t(
          "Obyekt olmoshlari (me, you, him, her, it, us, them) fe’ldan va predlogdan keyin ishlatiladi. Ega oʻrnida esa I, he, she, we, they turadi.",
          "Объектные местоимения (me, you, him, her, it, us, them) ставятся после глагола и предлога. В роли подлежащего используются I, he, she, we, they.",
          "Object pronouns (me, you, him, her, it, us, them) come after verbs and prepositions. As the subject, use I, he, she, we, they.",
        ),
        examples: ["Call me tomorrow.", "I bought a present for her.", "We saw them at the park."],
      },
    ],
    mistakes: [
      {
        wrong: "This is the car of my father.",
        right: "This is my father's car.",
        note: t(
          "Odamga tegishlilikni ’s bilan ifodalash tabiiyroq. Of ni rus tilidagi «машина моего отца» kabi soʻzma-soʻz tarjima qilmang.",
          "Принадлежность человеку естественнее выражать через 's. Не переводите «машина моего отца» дословно через of.",
          "For people, use 's rather than of.",
        ),
      },
      {
        wrong: "This umbrella is my.",
        right: "This umbrella is mine.",
        note: t(
          "Otsiz, yolgʻiz turganda mine, yours, hers kabi shakl ishlatiladi.",
          "Без существительного используется mine, yours, hers и т. д.",
          "With no noun after it, use mine, yours, hers and so on.",
        ),
      },
      {
        wrong: "My sister lives with his husband.",
        right: "My sister lives with her husband.",
        note: t(
          "Oʻzbek tilidagi «u» va «uning» jinsni farqlamaydi, ingliz tilida esa ayolga — her, erkakka — his.",
          "Для женщины — her, для мужчины — his. Не путайте их, особенно в быстрой речи.",
          "Use her for a woman and his for a man.",
        ),
      },
      {
        wrong: "The dog is wagging it's tail.",
        right: "The dog is wagging its tail.",
        note: t(
          "Its — egalik («uning»), it’s esa it is ning qisqa shakli.",
          "Its — притяжательное («его»), а it's — сокращение от it is.",
          "Its shows possession; it's means it is.",
        ),
      },
    ],
    quiz: [
      {
        prompt: "This is ___ room. She shares it with nobody.",
        options: ["my sister's", "my sisters", "sister's my"],
        answer: 0,
        why: t(
          "Birlikdagi otga tegishlilik ’s bilan: my sister’s room.",
          "Принадлежность одному лицу выражается через 's: my sister's room.",
          "Possession for one person is shown with 's: my sister's room.",
        ),
      },
      {
        prompt: "That book isn't yours. It's ___.",
        options: ["my", "me", "mine"],
        answer: 2,
        why: t(
          "Otdan keyin emas, yolgʻiz turibdi — mine.",
          "Существительного после нет — нужно mine.",
          "There is no noun after it, so we need mine.",
        ),
      },
      {
        prompt: "Can you help ___ with my homework?",
        options: ["I", "me", "my"],
        answer: 1,
        why: t(
          "Fe’ldan keyin obyekt olmoshi keladi: help me.",
          "После глагола нужно объектное местоимение: help me.",
          "After a verb we use an object pronoun: help me.",
        ),
      },
      {
        prompt: "My aunt lives in Fergana with ___ two sons.",
        options: ["his", "its", "her"],
        answer: 2,
        why: t(
          "Aunt — ayol, shuning uchun her.",
          "Aunt — женщина, поэтому her.",
          "An aunt is a woman, so we use her.",
        ),
      },
      {
        prompt: "The cat is drinking ___ milk.",
        options: ["its", "it's", "it"],
        answer: 0,
        why: t(
          "Egalik ma’nosi — its (apostrofsiz).",
          "Значение принадлежности — its (без апострофа).",
          "Possession is its, with no apostrophe.",
        ),
      },
      {
        prompt: "___ bag is this? — It's Timur's.",
        options: ["Who", "Whose", "Who's", "Which's"],
        answer: 1,
        why: t(
          "Kimga tegishli ekanini soʻrash — Whose.",
          "Чтобы спросить о владельце, используется Whose.",
          "To ask about the owner, use Whose.",
        ),
      },
    ],
  },
  // ───────────────────────────── B1 ─────────────────────────────
  {
    id: 'b1-past-continuous-simple',
    level: 'B1',
    title: t(
      "Oʻtgan davomli va oʻtgan oddiy zamon",
      "Past Continuous и Past Simple",
      "Past continuous vs past simple",
    ),
    summary: t(
      "Oʻtgan davomli zamon (Past Continuous) oʻtmishdagi ma’lum paytda davom etayotgan ishni, oʻtgan oddiy zamon (Past Simple) esa tugallangan ishni bildiradi. Ikkalasi birga kelsa, voqeaning fonini va uni boʻlgan qisqa harakatni koʻrsatadi — hikoya qilishda juda kerakli.",
      "Past Continuous описывает действие, которое длилось в определённый момент в прошлом, а Past Simple — завершённое действие. Вместе они показывают фон событий и короткое действие, которое его прервало, — это основа любого рассказа.",
      "The past continuous describes an action in progress at a moment in the past; the past simple describes a completed action. Together they show the background of a story and the short event that interrupted it.",
    ),
    rules: [
      {
        text: t(
          "Past Continuous — was/were + fe’l-ing. Oʻtmishdagi aniq paytda davom etayotgan ish uchun ishlatiladi: at 8 o'clock yesterday, all afternoon, this time last year.",
          "Past Continuous — was/were + глагол с -ing. Используется для действия, которое длилось в конкретный момент в прошлом: at 8 o'clock yesterday, all afternoon, this time last year.",
          "The past continuous is was/were + verb-ing. Use it for an action in progress at a particular time in the past: at 8 o'clock yesterday, all afternoon, this time last year.",
        ),
        examples: ["At 8 o'clock last night I was watching the news.", "They were playing football all afternoon.", "What were you doing when I called?"],
      },
      {
        text: t(
          "Uzoq davom etgan ish (Past Continuous) qisqa ish (Past Simple) bilan boʻlinadi. When odatda qisqa harakatdan, while esa davomli harakatdan oldin keladi.",
          "Длительное действие (Past Continuous) прерывается коротким (Past Simple). When обычно стоит перед коротким действием, while — перед длительным.",
          "A longer action (past continuous) is interrupted by a shorter one (past simple). When usually introduces the short action, and while introduces the long one.",
        ),
        examples: ["I was walking home when it started to rain.", "While she was cooking, the lights went out.", "He hurt his knee while he was playing tennis."],
      },
      {
        text: t(
          "Bir vaqtda parallel davom etgan ikki ish uchun ikkala qismda ham Past Continuous ishlatiladi. Ketma-ket sodir boʻlgan tugallangan ishlar esa Past Simple bilan aytiladi.",
          "Для двух параллельных длительных действий в обеих частях используется Past Continuous. Последовательные завершённые действия передаются через Past Simple.",
          "For two actions in progress at the same time, use the past continuous in both parts. For a sequence of completed actions, use the past simple.",
        ),
        examples: ["While I was reading, my brother was playing computer games.", "She opened the door, looked around and went in."],
      },
      {
        text: t(
          "Holat fe’llari (know, want, believe, like, own) oʻtgan zamonda ham davomli shaklda ishlatilmaydi.",
          "Глаголы состояния (know, want, believe, like, own) и в прошедшем времени не употребляются в длительной форме.",
          "State verbs (know, want, believe, like, own) are not used in the continuous form in the past either.",
        ),
        examples: ["I knew the answer, but I was too nervous to say it.", "She wanted to leave early."],
      },
    ],
    mistakes: [
      {
        wrong: "When I came home, my mother cooked dinner.",
        right: "When I came home, my mother was cooking dinner.",
        note: t(
          "Ona ovqatni siz kelganingizda pishirayotgan edi — ish davom etayotgan edi, shuning uchun Past Continuous kerak. Past Simple esa «men kelganimdan keyin pishirdi» degan ma’noni beradi.",
          "По-русски в обоих случаях можно сказать «готовила», но в английском выбор важен: was cooking — готовила в тот момент, cooked — начала готовить после вашего прихода.",
          "Was cooking means she was in the middle of cooking; cooked means she started after you arrived.",
        ),
      },
      {
        wrong: "I was knowing the answer.",
        right: "I knew the answer.",
        note: t(
          "Know — holat fe’li, u -ing shaklida ishlatilmaydi.",
          "Know — глагол состояния, в форме -ing не используется.",
          "Know is a state verb, so it isn't used with -ing.",
        ),
      },
      {
        wrong: "While I watched TV, the phone was ringing.",
        right: "While I was watching TV, the phone rang.",
        note: t(
          "Uzoq davom etgan ish — Past Continuous, uni boʻlgan qisqa ish — Past Simple. Bu yerda ular teskari qoʻyilgan.",
          "Длительное действие — Past Continuous, прервавшее его короткое — Past Simple. Здесь они перепутаны.",
          "The long action takes the past continuous and the interruption takes the past simple — here they are reversed.",
        ),
      },
      {
        wrong: "They was sleeping when we arrived.",
        right: "They were sleeping when we arrived.",
        note: t(
          "You, we, they bilan were ishlatiladi.",
          "С you, we, they используется were.",
          "Use were with you, we and they.",
        ),
      },
    ],
    quiz: [
      {
        prompt: "I ___ a shower when the doorbell rang.",
        options: ["had", "was having", "were having"],
        answer: 1,
        why: t(
          "Dush qabul qilish davom etayotgan edi va qoʻngʻiroq uni boʻldi; I bilan was.",
          "Душ длился, и звонок его прервал; с I — was.",
          "The shower was in progress when the bell interrupted it, and I takes was.",
        ),
      },
      {
        prompt: "While we were driving to Chimgan, we ___ a strange noise.",
        options: ["heard", "were hearing", "hear"],
        answer: 0,
        why: t(
          "Shovqin — qisqa, davomli ishni boʻlgan harakat, shuning uchun Past Simple.",
          "Шум — короткое действие, прервавшее длительное, поэтому Past Simple.",
          "The noise is a short event that interrupted the drive, so we use the past simple.",
        ),
      },
      {
        prompt: "At 10 p.m. yesterday I ___ for my exam, so I didn't hear the phone.",
        options: ["studied", "am studying", "was studying"],
        answer: 2,
        why: t(
          "Oʻtmishdagi aniq paytda davom etayotgan ish — Past Continuous.",
          "Действие, длившееся в конкретный момент в прошлом, — Past Continuous.",
          "An action in progress at a specific past time takes the past continuous.",
        ),
      },
      {
        prompt: "She ___ her keys on the way home, so she couldn't get in.",
        options: ["was losing", "lost", "were losing"],
        answer: 1,
        why: t(
          "Kalitni yoʻqotish — bir lahzalik, tugallangan ish: Past Simple.",
          "Потерять ключи — мгновенное завершённое действие: Past Simple.",
          "Losing keys is a single completed event, so it's the past simple.",
        ),
      },
      {
        prompt: "It ___ heavily when we left the house, so we took an umbrella.",
        options: ["was raining", "rained", "is raining"],
        answer: 0,
        why: t(
          "Biz chiqqanimizda yomgʻir allaqachon yogʻayotgan edi — bu fon, shuning uchun Past Continuous.",
          "Когда мы вышли, дождь уже шёл — это фон, поэтому Past Continuous.",
          "The rain was already in progress — it's the background, so we use the past continuous.",
        ),
      },
      {
        prompt: "I ___ you at the party last night. Were you there?",
        options: ["don't see", "wasn't seeing", "not saw", "didn't see"],
        answer: 3,
        why: t(
          "Oʻtmishdagi tugallangan ish, inkor: didn’t + boshlangʻich shakl.",
          "Завершённое действие в прошлом, отрицание: didn't + начальная форма.",
          "It's a completed past event in the negative: didn't + base form.",
        ),
      },
    ],
  },
  {
    id: 'b1-used-to-would',
    level: 'B1',
    title: t(
      "Used to va would: oʻtmishdagi odatlar",
      "Used to и would: привычки в прошлом",
      "Used to and would for past habits",
    ),
    summary: t(
      "Used to oʻtmishda boʻlgan, lekin hozir yoʻq odat va holatlarni bildiradi: «ilgari … edim». Would esa oʻtmishda takrorlanib turgan harakatlarni, ayniqsa xotira va hikoyalarda ifodalaydi.",
      "Used to описывает привычки и состояния, которые были в прошлом, но теперь их нет: «раньше я…». Would передаёт повторяющиеся действия в прошлом, особенно в воспоминаниях и рассказах.",
      "Used to describes past habits and states that are no longer true. Would describes repeated past actions, especially when we tell stories or share memories.",
    ),
    rules: [
      {
        text: t(
          "Used to + fe’lning boshlangʻich shakli — oʻtmishdagi odat yoki holat, hozir esa bunday emas. U harakat fe’llari bilan ham, holat fe’llari (live, have, be, like) bilan ham ishlatiladi.",
          "Used to + начальная форма глагола — привычка или состояние в прошлом, которых сейчас нет. Используется и с глаголами действия, и с глаголами состояния (live, have, be, like).",
          "Used to + base verb describes a past habit or state that is no longer true. It works with both action verbs and state verbs (live, have, be, like).",
        ),
        examples: ["I used to live in Andijan.", "She used to have long hair.", "We used to play in the street after school."],
      },
      {
        text: t(
          "Inkor va soʻroq gaplarda did bor, shuning uchun -d tushib qoladi: didn’t use to, Did you use to…?",
          "В отрицаниях и вопросах есть did, поэтому -d пропадает: didn't use to, Did you use to…?",
          "In negatives and questions, did carries the past, so we drop the -d: didn't use to, Did you use to…?",
        ),
        examples: ["I didn't use to like vegetables.", "Did you use to walk to school?"],
      },
      {
        text: t(
          "Would + fe’l ham oʻtmishda takrorlangan harakatni bildiradi, lekin faqat harakat fe’llari bilan. Holat fe’llari (have, live, be, know) bilan would ishlatilmaydi. Odatda vaqt avval aniqlab olinadi: When I was a child…",
          "Would + глагол тоже обозначает повторяющееся действие в прошлом, но только с глаголами действия. С глаголами состояния (have, live, be, know) would не используется. Обычно время уже задано в контексте: When I was a child…",
          "Would + verb also describes repeated past actions, but only actions, not states (have, live, be, know). The past time is usually set first: When I was a child…",
        ),
        examples: ["Every summer we would visit our grandparents in the village.", "My grandmother would tell us stories before bed."],
      },
      {
        text: t(
          "Hozirgi odatlar uchun used to emas, usually + Present Simple ishlatiladi. Be used to + -ing esa boshqa ma’noga ega: «…ga oʻrganib qolgan».",
          "Для привычек в настоящем используется не used to, а usually + Present Simple. А be used to + -ing — это другое значение: «привык к…».",
          "For present habits, use usually + present simple, not used to. Be used to + -ing is different: it means 'accustomed to'.",
        ),
        examples: ["I usually get up at seven.", "I'm used to getting up early now."],
      },
    ],
    mistakes: [
      {
        wrong: "I use to go to the gym every day.",
        right: "I usually go to the gym every day.",
        note: t(
          "Used to faqat oʻtmish haqida. Hozirgi odat uchun usually ishlatiladi.",
          "Used to относится только к прошлому. Для привычки в настоящем — usually.",
          "Used to is only for the past; for present habits use usually.",
        ),
      },
      {
        wrong: "Did you used to live here?",
        right: "Did you use to live here?",
        note: t(
          "Did dan keyin fe’l boshlangʻich shaklda: use to.",
          "После did глагол стоит в начальной форме: use to.",
          "After did, use the base form: use to.",
        ),
      },
      {
        wrong: "I would have a dog when I was a child.",
        right: "I used to have a dog when I was a child.",
        note: t(
          "Have (egalik ma’nosida) — holat fe’li, u bilan would emas, used to ishlatiladi.",
          "Have (в значении «иметь») — глагол состояния, с ним используется used to, а не would.",
          "Have (possession) is a state, so use used to, not would.",
        ),
      },
      {
        wrong: "I used to living in a village.",
        right: "I used to live in a village.",
        note: t(
          "«Ilgari … edim» ma’nosida used to dan keyin fe’lning boshlangʻich shakli keladi.",
          "В значении «раньше я…» после used to идёт начальная форма глагола.",
          "When it means a past habit, used to is followed by the base verb.",
        ),
      },
    ],
    quiz: [
      {
        prompt: "When I was a child, I ___ afraid of dogs.",
        options: ["would be", "used to be", "use to be"],
        answer: 1,
        why: t(
          "Be — holat fe’li, shuning uchun would emas, used to.",
          "Be — глагол состояния, поэтому used to, а не would.",
          "Be is a state verb, so we need used to, not would.",
        ),
      },
      {
        prompt: "Did you ___ play any sports at school?",
        options: ["use to", "used to", "using to"],
        answer: 0,
        why: t(
          "Did bilan soʻroqda -d tushib qoladi: use to.",
          "В вопросе с did окончание -d пропадает: use to.",
          "In a question with did, drop the -d: use to.",
        ),
      },
      {
        prompt: "Every evening my father ___ read the newspaper in his armchair.",
        options: ["uses to", "was used to", "would"],
        answer: 2,
        why: t(
          "Oʻtmishda takrorlangan harakat — would + fe’l.",
          "Повторяющееся действие в прошлом — would + глагол.",
          "A repeated past action can be expressed with would + verb.",
        ),
      },
      {
        prompt: "I ___ like coffee, but now I drink it every day.",
        options: ["didn't use to", "didn't used to", "not used to"],
        answer: 0,
        why: t(
          "Inkor shakli — didn’t use to.",
          "Отрицательная форма — didn't use to.",
          "The negative form is didn't use to.",
        ),
      },
      {
        prompt: "There ___ a cinema on this street, but they knocked it down.",
        options: ["would be", "used to be", "was used to be"],
        answer: 1,
        why: t(
          "Oʻtmishdagi, hozir mavjud boʻlmagan holat — used to be.",
          "Состояние в прошлом, которого больше нет, — used to be.",
          "A past state that is no longer true takes used to be.",
        ),
      },
      {
        prompt: "These days I ___ go to bed before midnight.",
        options: ["use to", "used to", "usually", "would"],
        answer: 2,
        why: t(
          "These days — hozirgi odat, shuning uchun usually.",
          "These days — привычка в настоящем, поэтому usually.",
          "These days shows a present habit, so we use usually.",
        ),
      },
    ],
  },
  {
    id: 'b1-too-enough-quantifiers',
    level: 'B1',
    title: t(
      "Too, enough va miqdor soʻzlari: a few, a little, lots of",
      "Too, enough и слова количества: a few, a little, lots of",
      "Too, enough and quantifiers: a few, a little, lots of",
    ),
    summary: t(
      "Too «haddan tashqari», enough «yetarli» degan ma’noni beradi. A few, a little, lots of kabi soʻzlar esa miqdorni aniq raqamsiz ifodalaydi. Ular yordamida fikrni aniqroq va tabiiyroq aytish mumkin.",
      "Too означает «слишком», enough — «достаточно». Слова a few, a little, lots of передают количество без точных чисел. С ними речь становится точнее и естественнее.",
      "Too means 'more than necessary' and enough means 'as much as necessary'. Quantifiers like a few, a little and lots of describe amounts without exact numbers.",
    ),
    rules: [
      {
        text: t(
          "Too + sifat/ravish — kerakdan ortiq, odatda salbiy ma’noda. Otlar bilan: too many + sanaladigan, too much + sanalmaydigan. Too … to + fe’l — «shunchalik …ki, … qilib boʻlmaydi».",
          "Too + прилагательное/наречие — больше, чем нужно, обычно с отрицательным оттенком. С существительными: too many + исчисляемые, too much + неисчисляемые. Too … to + глагол — «слишком …, чтобы…».",
          "Too + adjective/adverb means more than is good or necessary. With nouns: too many + countable, too much + uncountable. Too … to + verb means it's impossible because of this.",
        ),
        examples: ["This coffee is too hot to drink.", "There are too many cars in the city centre.", "You spend too much time on your phone."],
      },
      {
        text: t(
          "Enough sifat va ravishdan keyin (old enough, fast enough), otdan esa oldin (enough money) keladi. Inkorda: not … enough. Davomi: enough to + fe’l yoki enough for + kishi.",
          "Enough ставится после прилагательного и наречия (old enough, fast enough), но перед существительным (enough money). В отрицании: not … enough. Продолжение: enough to + глагол или enough for + кто-то.",
          "Enough goes after adjectives and adverbs (old enough, fast enough) but before nouns (enough money). Negative: not … enough. Continue with enough to + verb or enough for + someone.",
        ),
        examples: ["He isn't old enough to drive.", "Do we have enough chairs for everyone?", "You didn't speak loudly enough."],
      },
      {
        text: t(
          "A few — sanaladigan, a little — sanalmaydigan otlar bilan: «bir oz, bir nechta» (ijobiy ma’no). Artiklsiz few va little esa «juda oz, deyarli yoʻq» degan salbiy ma’noni beradi.",
          "A few — с исчисляемыми, a little — с неисчисляемыми: «несколько, немного» (положительный смысл). Без артикля few и little означают «мало, почти нет» — с отрицательным оттенком.",
          "A few goes with countable nouns and a little with uncountable nouns; both mean 'some'. Without a, few and little mean 'not many / not much' and sound negative.",
        ),
        examples: ["I have a few friends in Namangan.", "Add a little salt to the soup.", "Few people came to the meeting, so it was very short."],
      },
      {
        text: t(
          "Lots of / a lot of — ikkala turdagi otlar bilan, asosan tasdiq gaplarda. Plenty of — «yetarlidan ham koʻp».",
          "Lots of / a lot of — с обоими типами существительных, в основном в утвердительных предложениях. Plenty of — «более чем достаточно».",
          "Lots of / a lot of go with both kinds of noun, mostly in positive sentences. Plenty of means 'more than enough'.",
        ),
        examples: ["We've got lots of time, don't worry.", "There's plenty of food for everyone."],
      },
    ],
    mistakes: [
      {
        wrong: "The bag is enough big.",
        right: "The bag is big enough.",
        note: t(
          "«Yetarlicha katta» — big enough: sifat bilan enough undan keyin keladi.",
          "«Достаточно большой» — big enough: с прилагательным enough ставится после него.",
          "With adjectives, enough comes after: big enough.",
        ),
      },
      {
        wrong: "This soup is too delicious!",
        right: "This soup is really delicious!",
        note: t(
          "Too «juda» emas, balki «haddan tashqari» degani va salbiy ma’noga ega. Maqtash uchun very yoki really ishlatiladi.",
          "Too — это не «очень», а «слишком», и звучит негативно. Для похвалы используйте very или really.",
          "Too means 'more than is good', not 'very'. For praise, use very or really.",
        ),
      },
      {
        wrong: "I have a little friends here.",
        right: "I have a few friends here.",
        note: t(
          "Friends sanaladi, shuning uchun a few.",
          "Friends — исчисляемое, поэтому a few.",
          "Friends is countable, so use a few.",
        ),
      },
      {
        wrong: "There are too much people here.",
        right: "There are too many people here.",
        note: t(
          "People — sanaladigan koʻplik ot: too many.",
          "People — исчисляемое во множественном числе: too many.",
          "People is plural and countable: too many.",
        ),
      },
    ],
    quiz: [
      {
        prompt: "I can't carry this box. It's ___ heavy.",
        options: ["enough", "too", "too much"],
        answer: 1,
        why: t(
          "Sifatdan oldin, «haddan tashqari» ma’nosida — too.",
          "Перед прилагательным в значении «слишком» — too.",
          "Before an adjective, meaning 'more than I can manage', we use too.",
        ),
      },
      {
        prompt: "Is he ___ to join the basketball team?",
        options: ["tall enough", "enough tall", "too tall enough"],
        answer: 0,
        why: t(
          "Enough sifatdan keyin keladi: tall enough.",
          "Enough стоит после прилагательного: tall enough.",
          "Enough comes after the adjective: tall enough.",
        ),
      },
      {
        prompt: "Could I have ___ milk in my tea, please?",
        options: ["a few", "a little", "many"],
        answer: 1,
        why: t(
          "Milk sanalmaydi: a little.",
          "Milk — неисчисляемое: a little.",
          "Milk is uncountable, so we use a little.",
        ),
      },
      {
        prompt: "Hurry up! We don't have ___ time.",
        options: ["enough", "too", "a few"],
        answer: 0,
        why: t(
          "Otdan oldin, «yetarli» ma’nosida — enough.",
          "Перед существительным в значении «достаточно» — enough.",
          "Before a noun, meaning 'as much as we need', we use enough.",
        ),
      },
      {
        prompt: "She has ___ close friends, so she never feels lonely.",
        options: ["a little", "few", "a few", "much"],
        answer: 2,
        why: t(
          "Friends sanaladi va ma’no ijobiy («yolgʻiz emas»), shuning uchun a few.",
          "Friends — исчисляемое, а смысл положительный («не одиноко»), поэтому a few.",
          "Friends is countable and the meaning is positive, so a few.",
        ),
      },
      {
        prompt: "There's ___ food, so please take as much as you want.",
        options: ["too many", "a few", "plenty of"],
        answer: 2,
        why: t(
          "«Yetarlidan ham koʻp» — plenty of; food sanalmaydi.",
          "«Более чем достаточно» — plenty of; food — неисчисляемое.",
          "Plenty of means more than enough, and it works with uncountable food.",
        ),
      },
    ],
  },
  {
    id: 'b1-phrasal-verbs',
    level: 'B1',
    title: t(
      "Keng tarqalgan frazali fe’llar",
      "Распространённые фразовые глаголы",
      "Common phrasal verbs",
    ),
    summary: t(
      "Frazali fe’l — fe’l va kichik soʻzdan (up, off, after, out…) iborat birikma, uning ma’nosi koʻpincha tarkibidagi soʻzlardan butunlay farq qiladi. Soʻzlashuv tilida ular juda koʻp ishlatiladi, shuning uchun ularni bilish tabiiy gapirish uchun muhim.",
      "Фразовый глагол — это глагол с частицей (up, off, after, out…), и его значение часто совсем не выводится из частей. В разговорной речи их очень много, поэтому без них трудно звучать естественно.",
      "A phrasal verb is a verb plus a particle (up, off, after, out…), and its meaning is often different from its parts. They are everywhere in spoken English, so you need them to sound natural.",
    ),
    rules: [
      {
        text: t(
          "Frazali fe’lning ma’nosi yangi boʻladi, uni yaxlit birlik sifatida yodlash kerak: get up — oʻrnidan turmoq, look after — qaramoq, gʻamxoʻrlik qilmoq, give up — tashlamoq, voz kechmoq, find out — bilib olmoq.",
          "У фразового глагола новое значение, и его нужно запоминать целиком: get up — вставать, look after — присматривать, заботиться, give up — бросать, отказываться, find out — узнавать.",
          "A phrasal verb has its own meaning, so learn it as one unit: get up (leave your bed), look after (take care of), give up (stop doing), find out (discover).",
        ),
        examples: ["I usually get up at half past six.", "Who looks after your children when you're at work?", "My uncle gave up smoking last year."],
      },
      {
        text: t(
          "Ba’zi frazali fe’llar toʻldiruvchi olmaydi: get up, wake up, grow up, break down, set off.",
          "Некоторые фразовые глаголы не требуют дополнения: get up, wake up, grow up, break down, set off.",
          "Some phrasal verbs have no object: get up, wake up, grow up, break down, set off.",
        ),
        examples: ["Our car broke down on the way to Samarkand.", "I grew up in a small village.", "We set off early to avoid the traffic."],
      },
      {
        text: t(
          "Ajraladigan fe’llar (turn on/off, put on, take off, pick up, fill in): ot toʻldiruvchi kichik soʻzdan oldin ham, keyin ham turishi mumkin. Olmosh (it, him, them) esa faqat oʻrtada turadi: turn it off.",
          "Разделяемые глаголы (turn on/off, put on, take off, pick up, fill in): существительное может стоять и до частицы, и после неё. А местоимение (it, him, them) — только посередине: turn it off.",
          "Separable verbs (turn on/off, put on, take off, pick up, fill in): a noun can go before or after the particle, but a pronoun (it, him, them) must go in the middle: turn it off.",
        ),
        examples: ["Put on your coat. / Put your coat on.", "The music is loud — turn it down, please.", "Can you pick me up at six?"],
      },
      {
        text: t(
          "Ajralmaydigan fe’llarda (look after, look for, get on, run out of, take after) toʻldiruvchi doim kichik soʻzdan keyin keladi.",
          "У неразделяемых глаголов (look after, look for, get on, run out of, take after) дополнение всегда стоит после частицы.",
          "With inseparable verbs (look after, look for, get on, run out of, take after), the object always comes after the particle.",
        ),
        examples: ["I'm looking for my keys.", "We've run out of bread.", "She takes after her mother."],
      },
    ],
    mistakes: [
      {
        wrong: "It's too loud. Turn off it.",
        right: "It's too loud. Turn it off.",
        note: t(
          "Olmosh ajraladigan frazali fe’lning oʻrtasida turadi.",
          "Местоимение ставится между глаголом и частицей.",
          "A pronoun goes between the verb and the particle.",
        ),
      },
      {
        wrong: "I'm looking my phone.",
        right: "I'm looking for my phone.",
        note: t(
          "«Qidirmoq» — look for; for ni tushirib qoldirmang. Oddiy look «qaramoq» degani.",
          "«Искать» — look for; без for глагол look означает «смотреть».",
          "'Search' is look for — don't drop for.",
        ),
      },
      {
        wrong: "Can you look for my cat while I'm on holiday?",
        right: "Can you look after my cat while I'm on holiday?",
        note: t(
          "Look after — qaramoq, parvarish qilmoq; look for — qidirmoq. Kichik soʻz ma’noni butunlay oʻzgartiradi.",
          "Look after — присматривать, look for — искать. Частица полностью меняет смысл.",
          "Look after means take care of; look for means search for.",
        ),
      },
    ],
    quiz: [
      {
        prompt: "I'm tired because I ___ at five this morning.",
        options: ["got up", "got on", "gave up"],
        answer: 0,
        why: t(
          "Get up — oʻrnidan turmoq.",
          "Get up — вставать с постели.",
          "Get up means leave your bed.",
        ),
      },
      {
        prompt: "My neighbour ___ our plants while we were away.",
        options: ["looked for", "looked after", "looked up"],
        answer: 1,
        why: t(
          "Oʻsimliklarga qarab turdi — looked after.",
          "Присматривал за растениями — looked after.",
          "Taking care of the plants is looked after.",
        ),
      },
      {
        prompt: "It's cold outside. Here's your jacket — ___.",
        options: ["put on it", "put it", "put it on"],
        answer: 2,
        why: t(
          "Olmosh oʻrtada turadi: put it on.",
          "Местоимение стоит посередине: put it on.",
          "The pronoun goes in the middle: put it on.",
        ),
      },
      {
        prompt: "He decided to ___ sugar to improve his health.",
        options: ["give in", "give out", "give up"],
        answer: 2,
        why: t(
          "Give up — biror narsani iste’mol qilish yoki qilishni tashlamoq.",
          "Give up — отказаться от чего-то, бросить.",
          "Give up means stop having or doing something.",
        ),
      },
      {
        prompt: "We've ___ milk. Can you buy some on your way home?",
        options: ["run out of", "run out", "run into"],
        answer: 0,
        why: t(
          "Run out of + ot — «tugab qolmoq»; of kerak.",
          "Run out of + существительное — «закончиться»; нужен of.",
          "Run out of + noun means have none left; you need of.",
        ),
      },
      {
        prompt: "Could you ___ the TV? I'm trying to sleep.",
        options: ["turn up", "turn off", "turn on", "turn in"],
        answer: 1,
        why: t(
          "Uxlash uchun televizorni oʻchirish kerak — turn off.",
          "Чтобы уснуть, телевизор нужно выключить — turn off.",
          "To sleep, you want the TV switched off: turn off.",
        ),
      },
    ],
  },
  // ───────────────────────────── B2 ─────────────────────────────
  {
    id: 'b2-present-perfect-continuous',
    level: 'B2',
    title: t(
      "Hozirgi tugallangan davomli zamon",
      "Present Perfect Continuous",
      "Present perfect continuous",
    ),
    summary: t(
      "Present Perfect Continuous oʻtmishda boshlanib, hozirgacha davom etayotgan yoki hozirgina tugab, izi koʻrinib turgan ishni bildiradi. Oʻzbek va rus tillarida bunday gaplar koʻpincha hozirgi zamonda aytiladi («uch yildan beri oʻqiyapman»), ingliz tilida esa maxsus shakl kerak.",
      "Present Perfect Continuous описывает действие, которое началось в прошлом и продолжается до сих пор или только что закончилось и оставило видимый след. По-русски в таких случаях обычно используется настоящее время («учу три года»), а в английском нужна особая форма.",
      "The present perfect continuous describes an activity that started in the past and is still going on, or has just stopped and left a visible result. Many languages use the present tense here, but English needs this special form.",
    ),
    rules: [
      {
        text: t(
          "Have/has been + fe’l-ing. Oʻtmishda boshlangan va hozir ham davom etayotgan ish uchun, ayniqsa for, since va How long…? bilan ishlatiladi.",
          "Have/has been + глагол с -ing. Используется для действия, которое началось в прошлом и продолжается сейчас, особенно с for, since и How long…?",
          "Form it with have/has been + verb-ing. Use it for an activity that began in the past and is still continuing, especially with for, since and How long…?",
        ),
        examples: ["I've been learning English for three years.", "She has been working here since 2021.", "How long have you been waiting?"],
      },
      {
        text: t(
          "Hozirgina tugagan, lekin natijasi koʻrinib turgan ish uchun ham ishlatiladi: charchagan koʻrinish, hoʻl yoʻl, iflos qoʻllar.",
          "Также используется для недавно закончившегося действия с видимым результатом: усталый вид, мокрая дорога, грязные руки.",
          "It is also used for an activity that has just stopped but has a visible result: you look tired, the road is wet, your hands are dirty.",
        ),
        examples: ["You look tired. Have you been running?", "It's been raining — the streets are wet.", "Sorry about my hands. I've been fixing the bike."],
      },
      {
        text: t(
          "Davomli shakl jarayon va uning davomiyligiga urgʻu beradi, oddiy Present Perfect esa natija va miqdorga (nechta, qancha). Miqdor aytilsa, davomli shakl ishlatilmaydi.",
          "Длительная форма подчёркивает процесс и его продолжительность, а простой Present Perfect — результат и количество (сколько штук). Если названо количество, длительная форма не используется.",
          "The continuous form focuses on the activity and how long it has lasted; the simple form focuses on the result and how many. If you give a number, use the simple form.",
        ),
        examples: ["I've been writing emails all morning.", "I've written twelve emails this morning.", "She's been reading that novel for weeks, but she hasn't finished it yet."],
      },
      {
        text: t(
          "Holat fe’llari (know, have — egalik, be, like, own) davomli shaklda ishlatilmaydi; ular bilan Present Perfect Simple keladi.",
          "Глаголы состояния (know, have в значении «иметь», be, like, own) не употребляются в длительной форме; с ними используется Present Perfect Simple.",
          "State verbs (know, have for possession, be, like, own) don't take the continuous form, so use the present perfect simple.",
        ),
        examples: ["I've known Dilnoza since school.", "We've had this car for ten years."],
      },
    ],
    mistakes: [
      {
        wrong: "I am learning English for three years.",
        right: "I have been learning English for three years.",
        note: t(
          "«Uch yildan beri oʻrganyapman» deganda ingliz tilida hozirgi zamon emas, have been + -ing kerak.",
          "«Я учу английский три года» — в английском здесь нужен не Present Continuous, а have been + -ing.",
          "With for + a period up to now, use have been + -ing, not the present continuous.",
        ),
      },
      {
        wrong: "I've been knowing him for years.",
        right: "I've known him for years.",
        note: t(
          "Know — holat fe’li, shuning uchun Present Perfect Simple.",
          "Know — глагол состояния, поэтому Present Perfect Simple.",
          "Know is a state verb, so use the present perfect simple.",
        ),
      },
      {
        wrong: "How long are you waiting here?",
        right: "How long have you been waiting here?",
        note: t(
          "How long bilan hozirgacha davom etgan ish haqida soʻralsa, Present Perfect Continuous ishlatiladi.",
          "Вопрос How long о действии, которое длится до сих пор, требует Present Perfect Continuous.",
          "How long about an activity up to now needs the present perfect continuous.",
        ),
      },
      {
        wrong: "I've been reading five books this month.",
        right: "I've read five books this month.",
        note: t(
          "Miqdor (five books) natijani koʻrsatadi, shuning uchun oddiy Present Perfect.",
          "Количество (five books) показывает результат, поэтому простой Present Perfect.",
          "A number shows a result, so use the simple form.",
        ),
      },
    ],
    quiz: [
      {
        prompt: "We ___ for the bus for forty minutes! Where is it?",
        options: ["are waiting", "have been waiting", "waited"],
        answer: 1,
        why: t(
          "Kutish oʻtmishda boshlangan va hozir ham davom etyapti: have been waiting.",
          "Ожидание началось в прошлом и продолжается сейчас: have been waiting.",
          "The waiting started in the past and is still going on: have been waiting.",
        ),
      },
      {
        prompt: "My parents ___ each other for thirty years.",
        options: ["have been knowing", "know", "have known"],
        answer: 2,
        why: t(
          "Know — holat fe’li, for bilan esa Present Perfect Simple kerak.",
          "Know — глагол состояния, а с for нужен Present Perfect Simple.",
          "Know is a state verb, and with for we need the present perfect simple.",
        ),
      },
      {
        prompt: "Your eyes are red. ___ crying?",
        options: ["Have you been", "Had you", "Do you"],
        answer: 0,
        why: t(
          "Hozirgina tugagan ish va uning koʻrinib turgan izi (qizargan koʻzlar) — Present Perfect Continuous.",
          "Недавнее действие с видимым результатом (красные глаза) — Present Perfect Continuous.",
          "A recent activity with a visible result (red eyes) takes the present perfect continuous.",
        ),
      },
      {
        prompt: "She ___ three chapters of her thesis so far.",
        options: ["has been writing", "has written", "is writing"],
        answer: 1,
        why: t(
          "Miqdor (three chapters) — natija, shuning uchun has written.",
          "Количество (three chapters) — это результат, поэтому has written.",
          "A number of chapters is a result, so we use has written.",
        ),
      },
      {
        prompt: "It ___ since this morning, and it still hasn't stopped.",
        options: ["has been snowing", "snows", "is snowing"],
        answer: 0,
        why: t(
          "Since this morning — ertalabdan hozirgacha davom etayotgan jarayon.",
          "Since this morning — процесс, который длится с утра до сих пор.",
          "Since this morning shows an activity continuing up to now.",
        ),
      },
      {
        prompt: "How long ___ Spanish?",
        options: ["are you studying", "have you been studying", "do you study", "you have studied"],
        answer: 1,
        why: t(
          "How long + hozirgacha davom etayotgan ish — have you been studying (soʻroq tartibi bilan).",
          "How long + действие, длящееся до сих пор, — have you been studying (с вопросительным порядком слов).",
          "How long about an ongoing activity takes have you been studying, with question word order.",
        ),
      },
    ],
  },
  {
    id: 'b2-modals-deduction',
    level: 'B2',
    title: t(
      "Taxmin modallari: must, might, can’t",
      "Модальные глаголы предположения: must, might, can't",
      "Modals of deduction: must, might, can't",
    ),
    summary: t(
      "Must, might, may, could va can’t yordamida dalillarga tayanib xulosa chiqaramiz: «albatta shunday boʻlsa kerak», «balki», «boʻlishi mumkin emas». Oʻtmish haqidagi taxmin uchun ularga have + uchinchi shakl qoʻshiladi.",
      "С помощью must, might, may, could и can't мы делаем выводы на основе фактов: «наверняка», «возможно», «не может быть». Для предположений о прошлом к ним добавляется have + третья форма.",
      "We use must, might, may, could and can't to make logical guesses based on evidence: 'I'm sure', 'perhaps', 'it's impossible'. For guesses about the past, add have + past participle.",
    ),
    rules: [
      {
        text: t(
          "Hozirgi zamon: must — deyarli aniq ishonch («shunday boʻlsa kerak»), can’t — deyarli aniq inkor («boʻlishi mumkin emas»), might/may/could — imkoniyat («balki»). Ulardan keyin fe’lning boshlangʻich shakli yoki be + -ing keladi.",
          "Настоящее: must — почти полная уверенность («наверняка»), can't — уверенность в обратном («не может быть»), might/may/could — возможность («возможно»). После них идёт начальная форма глагола или be + -ing.",
          "Present: must means you are almost sure something is true, can't means you are almost sure it isn't, and might/may/could mean it's possible. They are followed by the base verb or be + -ing.",
        ),
        examples: ["She's not answering. She must be busy.", "That can't be Timur — he's in London this week.", "Take an umbrella. It might rain later."],
      },
      {
        text: t(
          "Taxmin ma’nosida must ning inkori mustn’t emas, balki can’t. Mustn’t — taqiq bildiradi.",
          "В значении предположения противоположность must — не mustn't, а can't. Mustn't выражает запрет.",
          "For deduction, the opposite of must is can't, not mustn't. Mustn't expresses a prohibition.",
        ),
        examples: ["He's been working all day. He must be exhausted.", "You've just had lunch. You can't be hungry already!"],
      },
      {
        text: t(
          "Oʻtmish haqida taxmin: must have / might have / could have / can’t have (couldn’t have) + fe’lning uchinchi shakli.",
          "Предположение о прошлом: must have / might have / could have / can't have (couldn't have) + третья форма глагола.",
          "For the past, use must have / might have / could have / can't have (couldn't have) + past participle.",
        ),
        examples: ["The ground is wet. It must have rained last night.", "I can't find my wallet. I might have left it at the café.", "She can't have seen us — she didn't even wave."],
      },
      {
        text: t(
          "Davom etayotgan ish haqida taxmin: must be doing (hozir), must have been doing (oʻtmishda).",
          "Предположение о длящемся действии: must be doing (сейчас), must have been doing (в прошлом).",
          "For actions in progress: must be doing (now) and must have been doing (in the past).",
        ),
        examples: ["The lights are on. They must be watching TV.", "He looked sleepy. He must have been working late."],
      },
    ],
    mistakes: [
      {
        wrong: "He mustn't be at home — his car is gone.",
        right: "He can't be at home — his car is gone.",
        note: t(
          "«Uyda boʻlishi mumkin emas» — can’t be. Mustn’t «mumkin emas, taqiqlangan» degani.",
          "«Не может быть дома» — can't be. Mustn't означает запрет.",
          "Use can't for 'it's impossible'; mustn't means 'it's not allowed'.",
        ),
      },
      {
        wrong: "She must went home early.",
        right: "She must have gone home early.",
        note: t(
          "Modaldan keyin oʻtgan zamon shakli qoʻyilmaydi; oʻtmish uchun have + uchinchi shakl ishlatiladi.",
          "После модального глагола не ставится прошедшая форма; для прошлого — have + третья форма.",
          "A modal can't be followed by a past form; use have + past participle.",
        ),
      },
      {
        wrong: "It must to be expensive.",
        right: "It must be expensive.",
        note: t(
          "Modal fe’llardan keyin to qoʻyilmaydi.",
          "После модальных глаголов to не ставится.",
          "No to after a modal verb.",
        ),
      },
      {
        wrong: "You must have forgot to lock the door.",
        right: "You must have forgotten to lock the door.",
        note: t(
          "Have dan keyin fe’lning uchinchi shakli keladi: forget – forgot – forgotten.",
          "После have нужна третья форма: forget – forgot – forgotten.",
          "After have, use the past participle: forgotten.",
        ),
      },
    ],
    quiz: [
      {
        prompt: "Aziz has three jobs. He ___ be very busy.",
        options: ["must", "can't", "mustn't"],
        answer: 0,
        why: t(
          "Dalilga asoslangan ishonchli xulosa — must.",
          "Уверенный вывод на основе фактов — must.",
          "It's a confident conclusion from evidence, so must.",
        ),
      },
      {
        prompt: "That ___ be the right answer — it doesn't make any sense.",
        options: ["must", "might", "can't"],
        answer: 2,
        why: t(
          "Mantiqqa toʻgʻri kelmaydi — demak, «boʻlishi mumkin emas»: can’t.",
          "Это бессмысленно — значит, «не может быть»: can't.",
          "It makes no sense, so it's impossible: can't.",
        ),
      },
      {
        prompt: "I'm not sure where Lola is. She ___ be in the library.",
        options: ["must", "might", "can't"],
        answer: 1,
        why: t(
          "«Aniq bilmayman» — faqat imkoniyat: might.",
          "«Я не уверен» — только возможность: might.",
          "The speaker isn't sure, so it's only a possibility: might.",
        ),
      },
      {
        prompt: "The window is broken. Someone ___ it.",
        options: ["must break", "must have broken", "can't have broken"],
        answer: 1,
        why: t(
          "Oʻtmish haqida ishonchli taxmin: must have + uchinchi shakl.",
          "Уверенное предположение о прошлом: must have + третья форма.",
          "A confident guess about the past: must have + past participle.",
        ),
      },
      {
        prompt: "You ___ Nodira at the party — she was in Bukhara that weekend.",
        options: ["can't have seen", "must have seen", "must see"],
        answer: 0,
        why: t(
          "U boshqa shaharda edi, demak koʻrgan boʻlishingiz mumkin emas: can’t have seen.",
          "Она была в другом городе, значит, вы не могли её видеть: can't have seen.",
          "She was in another city, so it's impossible: can't have seen.",
        ),
      },
      {
        prompt: "Their lights are off and nobody's answering. They ___ out.",
        options: ["must go", "must have gone", "can't have gone", "mustn't go"],
        answer: 1,
        why: t(
          "Dalillar ular chiqib ketganini koʻrsatadi: must have gone.",
          "Факты указывают, что они ушли: must have gone.",
          "The evidence shows they left: must have gone.",
        ),
      },
    ],
  },
  {
    id: 'b2-causative-have-get',
    level: 'B2',
    title: t(
      "Birovga ish qildirish: have/get something done",
      "Каузатив: have/get something done",
      "Causative: have/get something done",
    ),
    summary: t(
      "Biror ishni oʻzimiz emas, boshqa odam (usta, sartarosh, mexanik) bajarsa, have/get + narsa + uchinchi shakl ishlatiladi. Bu oʻzbek tilidagi -tir/-dir/-t qoʻshimchasiga oʻxshaydi: sochimni oldirdim — I had my hair cut.",
      "Если работу делаем не мы сами, а кто-то другой (мастер, парикмахер, механик), используется have/get + предмет + третья форма. По-русски это часто передаётся возвратным глаголом или безличной формой: «я подстригся», «мне починили машину».",
      "When someone else (a hairdresser, a mechanic, a technician) does a job for us, we use have/get + object + past participle.",
    ),
    rules: [
      {
        text: t(
          "Have + narsa + uchinchi shakl — xizmat sifatida birov bajargan ish. Zamon have fe’li orqali ifodalanadi: had, am having, will have, have had.",
          "Have + предмет + третья форма — работа, которую для нас сделал кто-то другой. Время выражается через have: had, am having, will have, have had.",
          "Have + object + past participle describes a service someone else does for us. The tense is shown by have: had, am having, will have, have had.",
        ),
        examples: ["I had my hair cut yesterday.", "We're having our kitchen painted next week.", "You should have your eyes tested."],
      },
      {
        text: t(
          "Get + narsa + uchinchi shakl — xuddi shu ma’no, lekin soʻzlashuvga xosroq. Koʻpincha need to, must, buyruq gaplarda ishlatiladi.",
          "Get + предмет + третья форма — то же значение, но более разговорное. Часто используется с need to, must и в повелительных предложениях.",
          "Get + object + past participle means the same but is more informal. It's common with need to, must and in imperatives.",
        ),
        examples: ["I need to get my phone repaired.", "Where did you get your suit made?", "Get your car checked before the long trip."],
      },
      {
        text: t(
          "Bu tuzilma boshimizga tushgan noxush voqeani aytishda ham ishlatiladi: sumkasini oʻgʻirlatib qoʻydi.",
          "Эта конструкция используется и для неприятных событий, которые с нами произошли: у неё украли сумку.",
          "The same structure describes bad things that happened to us: something was stolen or broken.",
        ),
        examples: ["She had her bag stolen on the metro.", "He got his nose broken playing football."],
      },
      {
        text: t(
          "Kim bajarishini aytmoqchi boʻlsak: have + kishi + fe’lning boshlangʻich shakli yoki get + kishi + to + fe’l.",
          "Если нужно назвать исполнителя: have + человек + начальная форма глагола или get + человек + to + глагол.",
          "To say who does the job: have + person + base verb, or get + person + to + verb.",
        ),
        examples: ["I'll have the technician check your computer.", "I got my brother to help me move the sofa."],
      },
    ],
    mistakes: [
      {
        wrong: "I cut my hair at the hairdresser's yesterday.",
        right: "I had my hair cut at the hairdresser's yesterday.",
        note: t(
          "I cut my hair — sochimni oʻzim oldim, degani. Sartarosh olgan boʻlsa, had my hair cut.",
          "I cut my hair значит «я сам себя подстриг». Если стриг парикмахер — had my hair cut.",
          "I cut my hair means you did it yourself; use had my hair cut for a service.",
        ),
      },
      {
        wrong: "I had repaired my car last week.",
        right: "I had my car repaired last week.",
        note: t(
          "Soʻz tartibi muhim: have + narsa + uchinchi shakl. Aks holda bu Past Perfect boʻlib qoladi.",
          "Порядок слов важен: have + предмет + третья форма. Иначе получается Past Perfect.",
          "Word order matters: have + object + participle. Otherwise it becomes the past perfect.",
        ),
      },
      {
        wrong: "I got my brother help me.",
        right: "I got my brother to help me.",
        note: t(
          "Get + kishi dan keyin to + fe’l keladi.",
          "После get + человек нужен to + глагол.",
          "After get + person, use to + verb.",
        ),
      },
      {
        wrong: "We are going to have painted the house.",
        right: "We are going to have the house painted.",
        note: t(
          "Narsa (the house) have va uchinchi shakl oʻrtasida turadi.",
          "Предмет (the house) стоит между have и третьей формой.",
          "The object goes between have and the past participle.",
        ),
      },
    ],
    quiz: [
      {
        prompt: "I ___ my eyes tested last week.",
        options: ["had", "did", "made"],
        answer: 0,
        why: t(
          "Xizmat — have + narsa + uchinchi shakl; oʻtgan zamonda had.",
          "Услуга — have + предмет + третья форма; в прошедшем — had.",
          "A service uses have + object + participle; in the past it's had.",
        ),
      },
      {
        prompt: "She's going to have her wedding dress ___ by a local designer.",
        options: ["make", "made", "making"],
        answer: 1,
        why: t(
          "Have + narsa dan keyin uchinchi shakl: made.",
          "После have + предмет нужна третья форма: made.",
          "After have + object, use the past participle: made.",
        ),
      },
      {
        prompt: "The roof is leaking. We need to ___.",
        options: ["repair it by someone", "have repaired it", "have it repaired"],
        answer: 2,
        why: t(
          "Toʻgʻri tartib: have + it + repaired.",
          "Правильный порядок: have + it + repaired.",
          "The correct order is have + it + repaired.",
        ),
      },
      {
        prompt: "Where do you usually ___ your hair cut?",
        options: ["make", "get", "let"],
        answer: 1,
        why: t(
          "Get + narsa + uchinchi shakl — have ning soʻzlashuv varianti.",
          "Get + предмет + третья форма — разговорный вариант have.",
          "Get + object + participle is the informal version of have.",
        ),
      },
      {
        prompt: "He ___ his wallet stolen while he was travelling.",
        options: ["had", "made", "was"],
        answer: 0,
        why: t(
          "Noxush hodisa ham shu tuzilma bilan aytiladi: had his wallet stolen.",
          "Неприятное событие передаётся той же конструкцией: had his wallet stolen.",
          "Bad experiences use the same pattern: had his wallet stolen.",
        ),
      },
      {
        prompt: "I'll get the mechanic ___ the brakes before our trip.",
        options: ["check", "to check", "checked", "checking"],
        answer: 1,
        why: t(
          "Get + kishi + to + fe’l.",
          "Get + человек + to + глагол.",
          "The pattern is get + person + to + verb.",
        ),
      },
    ],
  },
  {
    id: 'b2-future-continuous-perfect',
    level: 'B2',
    title: t(
      "Kelasi davomli va kelasi tugallangan zamon",
      "Future Continuous и Future Perfect",
      "Future continuous and future perfect",
    ),
    summary: t(
      "Future Continuous kelajakdagi ma’lum paytda davom etayotgan ishni, Future Perfect esa kelajakdagi ma’lum paytgacha tugab boʻladigan ishni bildiradi. Ular reja tuzish, muddat haqida gapirish va xushmuomala savol berishda juda qoʻl keladi.",
      "Future Continuous описывает действие, которое будет длиться в определённый момент в будущем, а Future Perfect — действие, которое завершится к определённому моменту. Они полезны, когда мы строим планы, говорим о сроках и вежливо спрашиваем.",
      "The future continuous describes an action in progress at a certain time in the future; the future perfect describes an action that will be complete before a certain time. They are useful for plans, deadlines and polite questions.",
    ),
    rules: [
      {
        text: t(
          "Will be + fe’l-ing — kelajakdagi aniq paytda davom etib turgan ish: this time tomorrow, at 8 p.m. on Friday.",
          "Will be + глагол с -ing — действие, которое будет длиться в конкретный момент в будущем: this time tomorrow, at 8 p.m. on Friday.",
          "Will be + verb-ing describes an action in progress at a specific future time: this time tomorrow, at 8 p.m. on Friday.",
        ),
        examples: ["This time tomorrow I'll be flying to Istanbul.", "Don't call at eight — we'll be having dinner.", "At 10 a.m. on Monday she'll be taking her driving test."],
      },
      {
        text: t(
          "Future Continuous odatiy tartibda boʻladigan ishlar va boshqalarning rejalari haqida xushmuomala soʻrash uchun ham ishlatiladi: Will you be using…?",
          "Future Continuous также используется для событий, которые произойдут в обычном порядке, и для вежливых вопросов о чужих планах: Will you be using…?",
          "It is also used for things that will happen as a matter of course, and to ask politely about someone's plans: Will you be using…?",
        ),
        examples: ["Will you be using the printer this afternoon?", "I'll be passing the post office, so I can send your parcel."],
      },
      {
        text: t(
          "Will have + uchinchi shakl — kelajakdagi muayyan paytgacha tugallanadigan ish. Odatda by, by the time, before bilan keladi.",
          "Will have + третья форма — действие, которое будет завершено к определённому моменту в будущем. Обычно с by, by the time, before.",
          "Will have + past participle describes an action that will be finished before a future time, usually with by, by the time or before.",
        ),
        examples: ["By June I'll have finished my degree.", "The film will have started by the time we get there.", "Will you have read the report by Friday?"],
      },
      {
        text: t(
          "Will have been + fe’l-ing — kelajakdagi ma’lum paytgacha qancha vaqt davom etgan boʻlishini bildiradi.",
          "Will have been + глагол с -ing показывает, сколько времени действие будет длиться к определённому моменту в будущем.",
          "Will have been + verb-ing shows how long an activity will have lasted by a future point.",
        ),
        examples: ["Next month I'll have been working here for five years.", "By the time we land, we'll have been travelling for twenty hours."],
      },
    ],
    mistakes: [
      {
        wrong: "By the time you will arrive, I will have left.",
        right: "By the time you arrive, I will have left.",
        note: t(
          "By the time, when, before, after dan keyin kelasi zamon ma’nosida Present Simple ishlatiladi.",
          "После by the time, when, before, after о будущем используется Present Simple.",
          "After by the time, when, before and after, use the present simple for the future.",
        ),
      },
      {
        wrong: "At this time tomorrow I will lie on the beach.",
        right: "At this time tomorrow I will be lying on the beach.",
        note: t(
          "Kelajakdagi aniq paytda davom etib turadigan ish — will be + -ing.",
          "Действие, которое будет длиться в конкретный момент в будущем, — will be + -ing.",
          "An action in progress at a future moment needs will be + -ing.",
        ),
      },
      {
        wrong: "I will finish the project till Friday.",
        right: "I will have finished the project by Friday.",
        note: t(
          "«Jumagacha tugataman» (muddat) — by Friday. Till/until «shu paytgacha davom etadi» degani. Rus tilidagi «до пятницы» ikkala ma’noni ham beradi, shuning uchun adashish oson.",
          "«До пятницы» в смысле срока — by Friday, а till/until значит «вплоть до» (действие длится до этого момента).",
          "For a deadline, use by, not till/until, which means an action continues up to that time.",
        ),
      },
      {
        wrong: "Next year I will be living here for ten years.",
        right: "Next year I will have been living here for ten years.",
        note: t(
          "Kelajakdagi nuqtagacha boʻlgan davomiylik — will have been + -ing.",
          "Продолжительность до момента в будущем — will have been + -ing.",
          "Duration up to a future point needs will have been + -ing.",
        ),
      },
    ],
    quiz: [
      {
        prompt: "Don't phone me at 3 — I ___ an important meeting.",
        options: ["will have", "will be having", "will have had"],
        answer: 1,
        why: t(
          "Soat 3 da majlis davom etayotgan boʻladi: will be having.",
          "В 3 часа встреча будет идти: will be having.",
          "The meeting will be in progress at 3: will be having.",
        ),
      },
      {
        prompt: "By the end of this year, we ___ 50,000 kilometres in this car.",
        options: ["will drive", "will be driving", "will have driven"],
        answer: 2,
        why: t(
          "By the end of this year — shu paytgacha natija: will have driven.",
          "By the end of this year — результат к этому моменту: will have driven.",
          "By the end of the year shows a result before a future point: will have driven.",
        ),
      },
      {
        prompt: "The workers ___ the new bridge by 2028.",
        options: ["will have built", "will be building", "are building"],
        answer: 0,
        why: t(
          "By 2028 — muddatgacha tugallanadi: will have built.",
          "By 2028 — будет завершено к сроку: will have built.",
          "By 2028 is a deadline, so we use will have built.",
        ),
      },
      {
        prompt: "___ the car tomorrow? I'd like to borrow it.",
        options: ["Will you have used", "Will you be using", "Do you use"],
        answer: 1,
        why: t(
          "Birovning rejasi haqida xushmuomala soʻrash — Will you be using…?",
          "Вежливый вопрос о чужих планах — Will you be using…?",
          "A polite question about someone's plans: Will you be using…?",
        ),
      },
      {
        prompt: "By the time we ___ at the stadium, the match will have started.",
        options: ["will arrive", "arrive", "will have arrived"],
        answer: 1,
        why: t(
          "By the time dan keyin Present Simple ishlatiladi.",
          "После by the time используется Present Simple.",
          "After by the time, use the present simple.",
        ),
      },
      {
        prompt: "In June she ___ at the bank for exactly ten years.",
        options: ["will be working", "will work", "will have been working", "has worked"],
        answer: 2,
        why: t(
          "Kelajakdagi nuqtagacha davomiylik (ten years) — will have been working.",
          "Продолжительность до момента в будущем (ten years) — will have been working.",
          "Duration up to a future point needs will have been working.",
        ),
      },
    ],
  },
  // ───────────────────────────── C1 ─────────────────────────────
  {
    id: 'c1-cleft-sentences',
    level: 'C1',
    title: t(
      "Ajratuvchi gaplar: It was… that, What I need is…",
      "Расщеплённые предложения: It was… that, What I need is…",
      "Cleft sentences: It was… that / What I need is…",
    ),
    summary: t(
      "Cleft gaplar oddiy gapni ikki qismga boʻlib, bir boʻlagini alohida ta’kidlaydi. Oʻzbek tilida bu vazifani koʻpincha «aynan», «-ku» yoki soʻz tartibi, rus tilida «именно» va «вот что» bajaradi; ingliz tilida esa soʻz tartibi erkin emas, shuning uchun maxsus tuzilma kerak.",
      "Расщеплённые предложения делят простое предложение на две части, чтобы выделить один элемент. В русском это делают порядок слов, «именно» или «вот что», а в английском порядок слов жёсткий, поэтому нужна особая конструкция.",
      "Cleft sentences split a simple sentence into two parts to put the focus on one element. English word order is fixed, so instead of moving words around we use these special structures.",
    ),
    rules: [
      {
        text: t(
          "It + be + ta’kidlanayotgan boʻlak + that/who + gapning qolgani. Shu yoʻl bilan ega, toʻldiruvchi, vaqt yoki joyni ajratib koʻrsatish mumkin; odamlar uchun who ham ishlatiladi.",
          "It + be + выделяемый элемент + that/who + остальная часть. Так можно выделить подлежащее, дополнение, время или место; для людей можно использовать who.",
          "It + be + focus + that/who + the rest. You can focus on the subject, the object, a time or a place; who is possible for people.",
        ),
        examples: ["It was my sister who found the keys, not me.", "It's the noise that I can't stand.", "It was in 2019 that we first met."],
      },
      {
        text: t(
          "What bilan boshlanadigan gaplar: What + ega + fe’l + be + yangi axborot. Ta’kid gap oxiridagi qismga tushadi. Tuzilmani teskari ham qurish mumkin: A holiday is what I need.",
          "Предложения с what: What + подлежащее + глагол + be + новая информация. Акцент падает на последнюю часть. Можно построить и в обратном порядке: A holiday is what I need.",
          "What-clefts: What + subject + verb + be + new information. The focus falls on the final part. You can also reverse it: A holiday is what I need.",
        ),
        examples: ["What I need is a long holiday.", "What surprised me was his calm reaction.", "A good night's sleep is what you need."],
      },
      {
        text: t(
          "Harakatni ta’kidlash uchun: What + ega + do/did + be + (to) + fe’l.",
          "Чтобы выделить действие: What + подлежащее + do/did + be + (to) + глагол.",
          "To focus on an action: What + subject + do/did + be + (to) + verb.",
        ),
        examples: ["What we did was call the police.", "What she does is design websites for small businesses."],
      },
      {
        text: t(
          "Boshqa variantlar: All I want is…, The reason (why)… is…, The thing that… is…, The place where… is…",
          "Другие варианты: All I want is…, The reason (why)… is…, The thing that… is…, The place where… is…",
          "Other patterns: All I want is…, The reason (why)… is…, The thing that… is…, The place where… is…",
        ),
        examples: ["All I want is a bit of peace and quiet.", "The reason why I left was the salary.", "The thing that annoys me most is the traffic."],
      },
    ],
    mistakes: [
      {
        wrong: "It was Aziz who he broke the vase.",
        right: "It was Aziz who broke the vase.",
        note: t(
          "Who egani almashtiradi, shuning uchun he ni takrorlamaslik kerak.",
          "Who заменяет подлежащее, поэтому he повторять не нужно.",
          "Who replaces the subject, so don't repeat he.",
        ),
      },
      {
        wrong: "What I need it is a rest.",
        right: "What I need is a rest.",
        note: t(
          "What-qismi oʻzi ega vazifasini bajaradi — it ortiqcha.",
          "Часть с what сама является подлежащим — it лишнее.",
          "The what-clause is the subject, so it is not needed.",
        ),
      },
      {
        wrong: "The thing what I like most is the food.",
        right: "The thing that I like most is the food.",
        note: t(
          "Otdan keyin what emas, that/which keladi. Rus tilidagi «то, что» ni soʻzma-soʻz tarjima qilmang.",
          "После существительного нужен that/which, а не what. Не переводите «то, что» дословно.",
          "After a noun, use that or which, not what.",
        ),
      },
      {
        wrong: "It was in Tashkent where I was born.",
        right: "It was in Tashkent that I was born.",
        note: t(
          "It-cleft gaplarda joy yoki vaqt ta’kidlansa ham, bogʻlovchi odatda that boʻladi.",
          "В it-cleft даже при выделении места или времени обычно используется that.",
          "In it-clefts, use that even when the focus is a place or time.",
        ),
      },
    ],
    quiz: [
      {
        prompt: "It was Dilshod ___ told me the news.",
        options: ["which", "who", "what"],
        answer: 1,
        why: t(
          "Ta’kidlangan boʻlak — odam, shuning uchun who (yoki that).",
          "Выделен человек, поэтому who (или that).",
          "The focus is a person, so we use who (or that).",
        ),
      },
      {
        prompt: "___ I really need is a new laptop.",
        options: ["That", "It", "What"],
        answer: 2,
        why: t(
          "«Menga aynan kerak boʻlgan narsa…» — What-cleft.",
          "«Что мне действительно нужно…» — конструкция с What.",
          "The pattern is a what-cleft: What I need is…",
        ),
      },
      {
        prompt: "It was only last year ___ I started running.",
        options: ["that", "when it", "which"],
        answer: 0,
        why: t(
          "It was + vaqt + that — cleft gapning asosiy qolipi.",
          "It was + время + that — стандартная схема.",
          "It was + time + that is the standard cleft pattern.",
        ),
      },
      {
        prompt: "What she did ___ resign the next morning.",
        options: ["is", "was", "has"],
        answer: 1,
        why: t(
          "Did oʻtgan zamonda, shuning uchun be ham oʻtgan zamonda: was.",
          "Did — прошедшее время, поэтому и be в прошедшем: was.",
          "Did is past, so be is also past: was.",
        ),
      },
      {
        prompt: "The reason ___ I called is to invite you to dinner.",
        options: ["why", "what", "because"],
        answer: 0,
        why: t(
          "The reason why… is… — sabab ta’kidlanadi.",
          "The reason why… is… — выделяется причина.",
          "The reason why… is… puts the focus on the reason.",
        ),
      },
      {
        prompt: "All I want ___ some peace and quiet.",
        options: ["are", "is", "to be", "it is"],
        answer: 1,
        why: t(
          "All I want birlikdagi ega sifatida qaraladi: is.",
          "All I want воспринимается как подлежащее в единственном числе: is.",
          "All I want acts as a singular subject: is.",
        ),
      },
    ],
  },
  {
    id: 'c1-participle-clauses',
    level: 'C1',
    title: t(
      "Sifatdosh va ravishdosh oborotlari (participle clauses)",
      "Причастные и деепричастные обороты (participle clauses)",
      "Participle clauses",
    ),
    summary: t(
      "Participle clauses — -ing, uchinchi shakl yoki having done bilan boshlanadigan qisqa oborotlar. Ular because, when, after, which kabi soʻzlarni tushirib, matnni ixcham va rasmiy qiladi. Esse, hisobot va rasmiy nutqda juda koʻp uchraydi.",
      "Participle clauses — короткие обороты с -ing, третьей формой или having done. Они заменяют придаточные с because, when, after, which и делают текст сжатым и формальным. Особенно часто встречаются в эссе, отчётах и официальной речи.",
      "Participle clauses are short phrases starting with an -ing form, a past participle or having done. They replace clauses with because, when, after or which, making writing concise and formal.",
    ),
    rules: [
      {
        text: t(
          "-ing shakli (present participle) — faol ma’no. U vaqt, sabab yoki natija ergash gapining oʻrnini bosadi. Oborot va bosh gapning egasi bitta boʻlishi shart.",
          "Форма на -ing (present participle) — активное значение. Она заменяет придаточное времени, причины или следствия. Подлежащее оборота и главного предложения должно совпадать.",
          "An -ing participle has an active meaning and can replace a clause of time, reason or result. The participle and the main clause must share the same subject.",
        ),
        examples: ["Walking along the river, we saw an old bridge.", "Not knowing what to say, she stayed silent.", "The train was delayed, leaving hundreds of passengers stranded."],
      },
      {
        text: t(
          "Uchinchi shakl (past participle) — majhul ma’no: ega harakatni bajarmaydi, balki harakat unga nisbatan bajariladi.",
          "Третья форма (past participle) — пассивное значение: подлежащее не совершает действие, а подвергается ему.",
          "A past participle has a passive meaning: the subject receives the action rather than doing it.",
        ),
        examples: ["Built in the 15th century, the bridge is still in use today.", "Asked about the delay, the manager apologised.", "Made from local cotton, these shirts sell very well."],
      },
      {
        text: t(
          "Having + uchinchi shakl (perfect participle) — oborotdagi ish bosh gapdagi ishdan oldin tugaganini koʻrsatadi. Majhul variant: having been + uchinchi shakl.",
          "Having + третья форма (perfect participle) показывает, что действие оборота завершилось раньше действия главного предложения. Пассивный вариант: having been + третья форма.",
          "Having + past participle shows that the action finished before the main action. The passive form is having been + past participle.",
        ),
        examples: ["Having finished the report, she went home.", "Having lived abroad for years, he speaks three languages fluently.", "Having been warned about the traffic, we left early."],
      },
      {
        text: t(
          "Qisqartirilgan nisbiy gaplar: who/which + fe’l oʻrniga ot + -ing (faol) yoki ot + uchinchi shakl (majhul).",
          "Сокращённые определительные придаточные: вместо who/which + глагол — существительное + -ing (актив) или + третья форма (пассив).",
          "Reduced relative clauses: instead of who/which + verb, use noun + -ing (active) or noun + past participle (passive).",
        ),
        examples: ["The woman sitting by the window is my teacher.", "Most of the goods sold at the market are made locally.", "Anyone wishing to join should register online."],
      },
    ],
    mistakes: [
      {
        wrong: "Walking into the room, the lights suddenly went off.",
        right: "As I walked into the room, the lights suddenly went off.",
        note: t(
          "Oborot va bosh gapning egasi bir xil boʻlishi kerak. Bu gapda xonaga chiroqlar kirgandek chiqadi.",
          "Подлежащее оборота и главного предложения должно совпадать. Здесь получается, что в комнату вошёл свет.",
          "The participle must share the main clause's subject; here it sounds as if the lights walked in.",
        ),
      },
      {
        wrong: "The man stood at the door is my uncle.",
        right: "The man standing at the door is my uncle.",
        note: t(
          "Faol, davom etayotgan harakat uchun -ing shakli kerak: who is standing → standing.",
          "Для активного длящегося действия нужна форма -ing: who is standing → standing.",
          "For an active, ongoing action, use -ing: who is standing → standing.",
        ),
      },
      {
        wrong: "Having been finished the project, we celebrated.",
        right: "Having finished the project, we celebrated.",
        note: t(
          "Loyihani biz tugatdik — ma’no faol, shuning uchun been kerak emas.",
          "Проект закончили мы — значение активное, поэтому been не нужно.",
          "We finished the project, so the meaning is active and been is wrong.",
        ),
      },
    ],
    quiz: [
      {
        prompt: "___ in 1970, the stadium now needs serious repairs.",
        options: ["Building", "Built", "Having built"],
        answer: 1,
        why: t(
          "Stadionni qurishgan — majhul ma’no: Built.",
          "Стадион построили — пассивное значение: Built.",
          "The stadium was built, so the meaning is passive: Built.",
        ),
      },
      {
        prompt: "___ his homework, he went out to play football.",
        options: ["Having done", "Done", "Being done"],
        answer: 0,
        why: t(
          "Avval uy vazifasi tugadi, keyin chiqib ketdi: Having done.",
          "Сначала закончил домашнее задание, потом ушёл: Having done.",
          "The homework was finished first, then he went out: Having done.",
        ),
      },
      {
        prompt: "The people ___ next door are very friendly.",
        options: ["are living", "lived", "living"],
        answer: 2,
        why: t(
          "Who live oʻrniga qisqartirilgan faol shakl: living.",
          "Сокращённая активная форма вместо who live: living.",
          "It's a reduced form of who live, so we use living.",
        ),
      },
      {
        prompt: "___ that the shop was closed, we went home.",
        options: ["Realised", "Realising", "Being realised"],
        answer: 1,
        why: t(
          "Biz tushundik — faol ma’no va sabab: Realising.",
          "Мы поняли — активное значение и причина: Realising.",
          "We realised it, so the meaning is active: Realising.",
        ),
      },
      {
        prompt: "Most of the paintings ___ in the exhibition belong to private collectors.",
        options: ["shown", "have shown", "are shown"],
        answer: 0,
        why: t(
          "Which are shown ning qisqargan majhul shakli: shown.",
          "Сокращённая пассивная форма от which are shown: shown.",
          "It's a reduced form of which are shown: shown.",
        ),
      },
      {
        prompt: "___ twice before, she knew exactly what to expect from the interview.",
        options: ["Being interviewed", "Having been interviewed", "Interviewing", "Having interviewed"],
        answer: 1,
        why: t(
          "Uni avval suhbatdan oʻtkazishgan — ham oldin, ham majhul: Having been interviewed.",
          "Её уже дважды собеседовали — действие раньше и пассивное: Having been interviewed.",
          "She was interviewed earlier, so it's both prior and passive: Having been interviewed.",
        ),
      },
    ],
  },
  {
    id: 'c1-subjunctive-formal',
    level: 'C1',
    title: t(
      "Subjunktiv va rasmiy tuzilmalar",
      "Сослагательное наклонение (subjunctive) и формальные конструкции",
      "The subjunctive and formal structures",
    ),
    summary: t(
      "Talab, tavsiya yoki zaruratni rasmiy tarzda ifodalashda ingliz tili subjunktivdan foydalanadi: that dan keyin fe’l boshlangʻich shaklda qoladi — -s ham, oʻtgan zamon ham qoʻshilmaydi. Bu shakl rasmiy xat, hisobot va akademik matnlarda ayniqsa koʻp uchraydi.",
      "Для официального выражения требований, рекомендаций и необходимости в английском используется subjunctive: после that глагол остаётся в начальной форме — без -s и без сдвига времени. Особенно часто это встречается в деловых письмах, отчётах и академических текстах.",
      "To express demands, recommendations and necessity formally, English uses the subjunctive: after that, the verb stays in its base form, with no -s and no past tense. It is common in formal letters, reports and academic writing.",
    ),
    rules: [
      {
        text: t(
          "Suggest, recommend, insist, demand, propose, request, require + that + ega + fe’lning boshlangʻich shakli. Inkor: not + boshlangʻich shakl (that he not be…).",
          "Suggest, recommend, insist, demand, propose, request, require + that + подлежащее + начальная форма глагола. Отрицание: not + начальная форма (that he not be…).",
          "Suggest, recommend, insist, demand, propose, request, require + that + subject + base verb. The negative is not + base verb (that he not be…).",
        ),
        examples: ["The doctor recommended that he stay in bed for a week.", "She insisted that we be on time.", "The committee requested that the report not be published yet."],
      },
      {
        text: t(
          "It is essential / vital / important / crucial / necessary + that + ega + boshlangʻich shakl.",
          "It is essential / vital / important / crucial / necessary + that + подлежащее + начальная форма.",
          "It is essential / vital / important / crucial / necessary + that + subject + base verb.",
        ),
        examples: ["It is essential that every student attend the first lecture.", "It is vital that the data be checked carefully.", "It is important that she submit the form before Friday."],
      },
      {
        text: t(
          "Britaniya ingliz tilida subjunktiv oʻrniga should + fe’l ham ishlatiladi. Suggest va recommend dan keyin -ing shakli ham keladi, lekin hech qachon «kishi + to + fe’l» kelmaydi.",
          "В британском английском вместо subjunctive часто используется should + глагол. После suggest и recommend возможна форма -ing, но никогда не «человек + to + глагол».",
          "In British English, should + verb is a common alternative. Suggest and recommend can also take -ing, but never 'person + to + verb'.",
        ),
        examples: ["The committee proposed that the rules should be changed.", "I suggest (that) you take a taxi.", "He suggested going to the cinema."],
      },
      {
        text: t(
          "Subjunktiv ba’zi qotib qolgan iboralarda va noreal shart gaplarda ham saqlangan: If I were…, as it were, be that as it may, come what may.",
          "Subjunctive сохранился в некоторых устойчивых выражениях и в нереальных условиях: If I were…, as it were, be that as it may, come what may.",
          "The subjunctive also survives in fixed expressions and unreal conditions: If I were…, as it were, be that as it may, come what may.",
        ),
        examples: ["If I were you, I would apply for the job.", "Come what may, we will finish on time.", "The plan is expensive. Be that as it may, it's our best option."],
      },
    ],
    mistakes: [
      {
        wrong: "He suggested me to apply for the scholarship.",
        right: "He suggested that I apply for the scholarship.",
        note: t(
          "Suggest dan keyin «kishi + to + fe’l» ishlatilmaydi. «Menga taklif qildi» / «предложил мне» ni soʻzma-soʻz tarjima qilmang.",
          "После suggest не бывает «человек + to + глагол». Не переводите «предложил мне» дословно.",
          "Suggest is never followed by person + to + verb.",
        ),
      },
      {
        wrong: "I recommend you to book early.",
        right: "I recommend that you book early.",
        note: t(
          "Recommend ham suggest kabi: that + ega + boshlangʻich shakl yoki -ing.",
          "Recommend ведёт себя как suggest: that + подлежащее + начальная форма или -ing.",
          "Recommend works like suggest: that + subject + base verb, or -ing.",
        ),
      },
      {
        wrong: "The manager demanded that he pays the fine.",
        right: "The manager demanded that he pay the fine.",
        note: t(
          "Rasmiy uslubda demand that dan keyin fe’l -s olmaydi: he pay.",
          "В официальном стиле после demand that глагол не получает -s: he pay.",
          "In formal English, the verb after demand that has no -s: he pay.",
        ),
      },
    ],
    quiz: [
      {
        prompt: "The teacher suggested that she ___ her essay again.",
        options: ["checks", "check", "to check"],
        answer: 1,
        why: t(
          "Suggest that + ega + boshlangʻich shakl: check.",
          "Suggest that + подлежащее + начальная форма: check.",
          "Suggest that + subject + base verb: check.",
        ),
      },
      {
        prompt: "It is vital that the documents ___ signed before Friday.",
        options: ["be", "were", "being"],
        answer: 0,
        why: t(
          "It is vital that… dan keyin subjunktiv: be.",
          "После It is vital that… — subjunctive: be.",
          "After It is vital that…, use the subjunctive: be.",
        ),
      },
      {
        prompt: "He insisted ___ for dinner.",
        options: ["on paying", "to pay", "that pay"],
        answer: 0,
        why: t(
          "Insist on + -ing yoki insist that + ega + fe’l; to pay notoʻgʻri.",
          "Insist on + -ing или insist that + подлежащее + глагол; to pay неверно.",
          "It's insist on + -ing or insist that + subject + verb, never insist to.",
        ),
      },
      {
        prompt: "I recommend ___ the museum early in the morning.",
        options: ["to visit", "visiting", "you visiting"],
        answer: 1,
        why: t(
          "Recommend + -ing — kim ekani aytilmasa.",
          "Recommend + -ing, если не называется, кто именно.",
          "Recommend + -ing is used when we don't name the person.",
        ),
      },
      {
        prompt: "The board demanded that the director ___ immediately.",
        options: ["resigns", "resigned", "to resign", "resign"],
        answer: 3,
        why: t(
          "Demand that + ega + boshlangʻich shakl, -s siz: resign.",
          "Demand that + подлежащее + начальная форма, без -s: resign.",
          "Demand that + subject + base verb with no -s: resign.",
        ),
      },
      {
        prompt: "If I ___ you, I would accept the offer.",
        options: ["am", "were", "be"],
        answer: 1,
        why: t(
          "Noreal shartda barcha shaxslar uchun were ishlatiladi.",
          "В нереальном условии для всех лиц используется were.",
          "In unreal conditions, were is used for all persons.",
        ),
      },
    ],
  },
  {
    id: 'c1-passive-reporting',
    level: 'C1',
    title: t(
      "Xabar passivi: It is said that…, He is believed to…",
      "Пассивные конструкции передачи мнения: It is said that…, He is believed to…",
      "Advanced passive reporting: It is said that… / He is believed to…",
    ),
    summary: t(
      "Umumiy fikr, xabar yoki taxminni manbasini aytmasdan, xolis va rasmiy ohangda yetkazish uchun reporting passive ishlatiladi. Yangiliklar, ilmiy matnlar va IELTS esselarida juda foydali: «aytilishicha», «taxmin qilinishicha», «ma’lum boʻlishicha».",
      "Пассивные конструкции передачи мнения позволяют сообщить общее мнение, новость или предположение безлично и официально: «говорят», «считается», «сообщается». Они очень полезны в новостях, научных текстах и эссе IELTS.",
      "Reporting passives let you present opinions, reports and beliefs impersonally and formally, without naming a source. They are common in news, academic writing and IELTS essays.",
    ),
    rules: [
      {
        text: t(
          "It + passiv reporting fe’l + that-gap. Fe’llar: said, thought, believed, reported, claimed, expected, known, estimated, considered.",
          "It + пассивный глагол сообщения + придаточное с that. Глаголы: said, thought, believed, reported, claimed, expected, known, estimated, considered.",
          "It + passive reporting verb + that-clause. Common verbs: said, thought, believed, reported, claimed, expected, known, estimated, considered.",
        ),
        examples: ["It is thought that the painting is over 300 years old.", "It was reported that the fire had started in the kitchen.", "It is expected that prices will rise next year."],
      },
      {
        text: t(
          "Shaxsli tuzilma: ega + passiv fe’l + to-infinitiv. Infinitiv shakli vaqtga qarab tanlanadi: to do (bir vaqtda yoki kelajak), to be doing (davom etayotgan), to have done (oldinroq sodir boʻlgan), to have been done (oldinroq, majhul).",
          "Личная конструкция: подлежащее + пассивный глагол + инфинитив с to. Форма инфинитива зависит от времени: to do (одновременно или в будущем), to be doing (длится), to have done (произошло раньше), to have been done (раньше, пассив).",
          "Personal structure: subject + passive verb + to-infinitive. Choose the infinitive by time: to do (same time or future), to be doing (in progress), to have done (earlier), to have been done (earlier, passive).",
        ),
        examples: ["He is believed to be living abroad.", "The company is said to have lost millions last year.", "The bridge is thought to have been designed by a local engineer."],
      },
      {
        text: t(
          "There bilan: There is/are + said/thought/believed + to be / to have been. Fe’l keyingi otga qarab birlik yoki koʻplikda boʻladi.",
          "С there: There is/are + said/thought/believed + to be / to have been. Глагол согласуется со следующим существительным.",
          "With there: There is/are + said/thought/believed + to be / to have been. The verb agrees with the noun that follows.",
        ),
        examples: ["There are said to be over 300 species of birds in the park.", "There is thought to have been a village here centuries ago."],
      },
      {
        text: t(
          "Reporting fe’lning zamoni fikr qachon mavjud boʻlganini koʻrsatadi: People believed… → It was believed… / He was believed to…",
          "Время глагола сообщения показывает, когда существовало мнение: People believed… → It was believed… / He was believed to…",
          "The tense of the reporting verb shows when the belief existed: People believed… → It was believed… / He was believed to…",
        ),
        examples: ["People once believed that the Earth was flat. → The Earth was once believed to be flat.", "People say that he earns a fortune. → He is said to earn a fortune."],
      },
    ],
    mistakes: [
      {
        wrong: "He is said that he is very rich.",
        right: "He is said to be very rich.",
        note: t(
          "Ikki tuzilmani aralashtirmang: yo It is said that he is…, yo He is said to be…",
          "Не смешивайте две конструкции: либо It is said that he is…, либо He is said to be…",
          "Don't mix the two patterns: It is said that he is… or He is said to be…",
        ),
      },
      {
        wrong: "She is believed to leave the country last year.",
        right: "She is believed to have left the country last year.",
        note: t(
          "Ish fikrdan oldin sodir boʻlgan (last year), shuning uchun to have + uchinchi shakl.",
          "Действие произошло раньше (last year), поэтому to have + третья форма.",
          "The action happened earlier (last year), so use to have + past participle.",
        ),
      },
      {
        wrong: "She is considered as the best surgeon in the city.",
        right: "She is considered (to be) the best surgeon in the city.",
        note: t(
          "Consider dan keyin as qoʻyilmaydi; as bilan regard keladi: She is regarded as…",
          "После consider не ставится as; as употребляется с regard: She is regarded as…",
          "Consider doesn't take as; regard does: She is regarded as…",
        ),
      },
      {
        wrong: "There is said to be many problems with the plan.",
        right: "There are said to be many problems with the plan.",
        note: t(
          "Fe’l keyingi otga moslashadi: many problems — are.",
          "Глагол согласуется со следующим существительным: many problems — are.",
          "The verb agrees with the noun that follows: many problems — are.",
        ),
      },
    ],
    quiz: [
      {
        prompt: "It ___ that the museum will reopen next spring.",
        options: ["is said", "says", "is saying"],
        answer: 0,
        why: t(
          "It + passiv reporting fe’l: is said.",
          "It + пассивный глагол сообщения: is said.",
          "It + passive reporting verb: is said.",
        ),
      },
      {
        prompt: "The singer is reported ___ married last weekend.",
        options: ["to get", "to have got", "getting"],
        answer: 1,
        why: t(
          "Toʻy oʻtgan haftada boʻlgan — xabardan oldin: to have got.",
          "Свадьба была на прошлой неделе — раньше сообщения: to have got.",
          "The wedding happened before the report: to have got.",
        ),
      },
      {
        prompt: "The thieves are believed ___ somewhere in the city.",
        options: ["hiding", "that they are hiding", "to be hiding"],
        answer: 2,
        why: t(
          "Hozir davom etayotgan ish — to be + -ing.",
          "Действие, которое длится сейчас, — to be + -ing.",
          "An action in progress now takes to be + -ing.",
        ),
      },
      {
        prompt: "There ___ to be over two million people living in the region.",
        options: ["is thought", "are thought", "thinks"],
        answer: 1,
        why: t(
          "Fe’l keyingi otga moslashadi: two million people — are.",
          "Глагол согласуется с существительным: two million people — are.",
          "The verb agrees with two million people, so it's are.",
        ),
      },
      {
        prompt: "___ that the ancient city was destroyed by an earthquake.",
        options: ["It believes", "It is believing", "It is believed"],
        answer: 2,
        why: t(
          "Umumiy fikr passiv bilan beriladi: It is believed.",
          "Общее мнение передаётся пассивом: It is believed.",
          "A general belief is expressed with the passive: It is believed.",
        ),
      },
      {
        prompt: "The castle is said ___ in the 12th century.",
        options: ["to build", "to have been built", "to be built", "having been built"],
        answer: 1,
        why: t(
          "Qal’ani oʻtmishda qurishgan — oldinroq va majhul: to have been built.",
          "Замок построили в прошлом — раньше и в пассиве: to have been built.",
          "The castle was built long ago, so it's prior and passive: to have been built.",
        ),
      },
    ],
  },
];
