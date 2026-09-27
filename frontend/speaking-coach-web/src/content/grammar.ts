import type { GrammarLesson, L3 } from './types';

// Grammatika darslari (A2–C1). Original tushuntirishlar va mashqlar.
// Har bir tushuntirish uch tilda: oʻzbekcha, ruscha, inglizcha.
const t = (uz: string, ru: string, en: string): L3 => ({ uz, ru, en });

export const GRAMMAR: GrammarLesson[] = [
  // ───────────────────────────── A2 ─────────────────────────────
  {
    id: 'a2-present-simple-continuous',
    level: 'A2',
    title: t(
      "Hozirgi oddiy va hozirgi davomli zamon",
      "Настоящее простое и настоящее длительное время",
      "Present simple vs present continuous",
    ),
    summary: t(
      "Hozirgi oddiy zamon (Present Simple) odat, doimiy holat va umumiy haqiqatlarni, hozirgi davomli zamon (Present Continuous) esa aynan hozir yoki shu kunlarda davom etayotgan ish-harakatni ifodalaydi.",
      "Present Simple выражает привычки, постоянные состояния и общеизвестные факты, а Present Continuous — действия, которые происходят прямо сейчас или в текущий период.",
      "The present simple describes habits, permanent situations and facts; the present continuous describes actions happening now or around now.",
    ),
    rules: [
      {
        text: t(
          "Present Simple — odatlar, jadvallar va umumiy haqiqatlar uchun. He, she, it bilan fe’lga -s yoki -es qoʻshiladi, inkor va soʻroq gaplarda esa do/does yordamchi fe’li ishlatiladi.",
          "Present Simple — для привычек, расписаний и фактов. С he, she, it к глаголу добавляется -s или -es, а в отрицаниях и вопросах используется вспомогательный глагол do/does.",
          "Use the present simple for habits, timetables and facts. Add -s or -es after he, she, it, and use do/does in negatives and questions.",
        ),
        examples: ["She walks to school every day.", "Water boils at 100 degrees.", "Does your brother work in Tashkent?"],
      },
      {
        text: t(
          "Present Continuous — am/is/are + fe’l-ing. Hozir sodir boʻlayotgan yoki vaqtincha davom etayotgan ishlar uchun ishlatiladi: now, right now, at the moment, these days, this week.",
          "Present Continuous — am/is/are + глагол с окончанием -ing. Используется для действий, которые происходят сейчас или временно: now, right now, at the moment, these days, this week.",
          "The present continuous is am/is/are + verb-ing. Use it for actions happening now or for temporary situations: now, at the moment, these days, this week.",
        ),
        examples: ["I'm cooking dinner right now.", "They are staying with their grandparents this week.", "Look! It's snowing."],
      },
      {
        text: t(
          "Holat fe’llari (know, like, want, need, believe, understand, belong) odatda davomli shaklda ishlatilmaydi, chunki ular harakatni emas, holatni bildiradi.",
          "Глаголы состояния (know, like, want, need, believe, understand, belong) обычно не употребляются в длительной форме: они обозначают состояние, а не действие.",
          "State verbs (know, like, want, need, believe, understand, belong) are not usually used in the continuous form because they describe states, not actions.",
        ),
        examples: ["I know the answer.", "She wants a new phone.", "This bag belongs to Aziz."],
      },
    ],
    mistakes: [
      {
        wrong: "He work in a bank.",
        right: "He works in a bank.",
        note: t(
          "Uchinchi shaxs birlikda (he, she, it) fe’lga -s qoʻshiladi.",
          "В третьем лице единственного числа (he, she, it) к глаголу добавляется -s.",
          "With he, she and it, the verb needs -s.",
        ),
      },
      {
        wrong: "I am agree with you.",
        right: "I agree with you.",
        note: t(
          "Agree — fe’l, shuning uchun oldidan am qoʻyilmaydi. «Roziman» soʻzini aynan tarjima qilmang.",
          "Agree — это глагол, поэтому am перед ним не нужен. Не переводите «я согласен» дословно.",
          "Agree is a verb, so it doesn't need am in front of it.",
        ),
      },
      {
        wrong: "I am understanding you now.",
        right: "I understand you now.",
        note: t(
          "Understand — holat fe’li, u odatda -ing shaklida ishlatilmaydi.",
          "Understand — глагол состояния, в длительной форме он обычно не используется.",
          "Understand is a state verb, so we don't normally use it with -ing.",
        ),
      },
      {
        wrong: "What you are doing?",
        right: "What are you doing?",
        note: t(
          "Soʻroq gapda yordamchi fe’l (am/is/are) egadan oldin keladi.",
          "В вопросе вспомогательный глагол (am/is/are) стоит перед подлежащим.",
          "In questions, the auxiliary (am/is/are) comes before the subject.",
        ),
      },
    ],
    quiz: [
      {
        prompt: "My father ___ tea every morning.",
        options: ["drinks", "is drinking", "drink"],
        answer: 0,
        why: t(
          "Every morning — odat, demak Present Simple; he bilan fe’lga -s qoʻshiladi.",
          "Every morning — привычка, значит Present Simple; с he добавляется -s.",
          "Every morning shows a habit, so we use the present simple with -s.",
        ),
      },
      {
        prompt: "Be quiet, please! The baby ___.",
        options: ["sleeps", "is sleeping", "sleep"],
        answer: 1,
        why: t(
          "Chaqaloq aynan hozir uxlayapti, shuning uchun Present Continuous kerak.",
          "Ребёнок спит прямо сейчас, поэтому нужен Present Continuous.",
          "The baby is sleeping right now, so we need the present continuous.",
        ),
      },
      {
        prompt: "I ___ what you mean.",
        options: ["am understanding", "understands", "understand"],
        answer: 2,
        why: t(
          "Understand — holat fe’li; I bilan -s ham qoʻshilmaydi.",
          "Understand — глагол состояния, а с I окончание -s не нужно.",
          "Understand is a state verb, and there is no -s after I.",
        ),
      },
      {
        prompt: "___ your sister speak French?",
        options: ["Is", "Does", "Do"],
        answer: 1,
        why: t(
          "Your sister — uchinchi shaxs birlik, shuning uchun soʻroqda Does ishlatiladi.",
          "Your sister — третье лицо единственного числа, поэтому в вопросе нужен Does.",
          "Your sister is a she, so the question needs Does.",
        ),
      },
      {
        prompt: "This month we ___ a new project at work.",
        options: ["are doing", "do", "does"],
        answer: 0,
        why: t(
          "This month vaqtinchalik holatni koʻrsatadi, shuning uchun Present Continuous.",
          "This month указывает на временную ситуацию, поэтому Present Continuous.",
          "This month shows a temporary situation, so we use the present continuous.",
        ),
      },
      {
        prompt: "The train ___ at 7:15 tomorrow morning.",
        options: ["is leave", "leaves", "leaving"],
        answer: 1,
        why: t(
          "Jadval boʻyicha boʻladigan ishlar (poyezd, dars, film) uchun kelasi zamon ma’nosida ham Present Simple ishlatiladi.",
          "Для расписаний (поезда, уроки, сеансы) Present Simple используется даже о будущем.",
          "We use the present simple for timetables, even when we talk about the future.",
        ),
      },
    ],
  },
  {
    id: 'a2-past-simple',
    level: 'A2',
    title: t(
      "Oʻtgan oddiy zamon: toʻgʻri va notoʻgʻri fe’llar",
      "Прошедшее простое время: правильные и неправильные глаголы",
      "Past simple: regular and irregular verbs",
    ),
    summary: t(
      "Oʻtgan oddiy zamon (Past Simple) oʻtmishda, aniq vaqtda boshlanib tugagan ish-harakatlar haqida gapirish uchun kerak: yesterday, last week, in 2020, two days ago.",
      "Past Simple нужен, чтобы рассказать о действиях, которые начались и закончились в определённый момент в прошлом: yesterday, last week, in 2020, two days ago.",
      "Use the past simple for finished actions at a definite time in the past: yesterday, last week, in 2020, two days ago.",
    ),
    rules: [
      {
        text: t(
          "Toʻgʻri fe’llarga -ed qoʻshiladi. Imloga e’tibor bering: live → lived, stop → stopped, study → studied.",
          "К правильным глаголам добавляется -ed. Обратите внимание на правописание: live → lived, stop → stopped, study → studied.",
          "Regular verbs add -ed. Watch the spelling: live → lived, stop → stopped, study → studied.",
        ),
        examples: ["We watched a film last night.", "The bus stopped near the park.", "She studied in Namangan for two years."],
      },
      {
        text: t(
          "Notoʻgʻri fe’llarning oʻtgan zamon shakli yodlab olinadi: go → went, buy → bought, see → saw, have → had, make → made.",
          "Формы прошедшего времени неправильных глаголов нужно запомнить: go → went, buy → bought, see → saw, have → had, make → made.",
          "Irregular verbs have special past forms that you need to learn: go → went, buy → bought, see → saw, have → had, make → made.",
        ),
        examples: ["I went to the bazaar on Saturday.", "My uncle bought a new car.", "We had a great time at the wedding."],
      },
      {
        text: t(
          "Inkor va soʻroq gaplarda did / didn’t ishlatiladi, asosiy fe’l esa boshlangʻich shaklga qaytadi.",
          "В отрицаниях и вопросах используется did / didn't, а основной глагол возвращается в начальную форму.",
          "In negatives and questions use did / didn't, and the main verb goes back to its base form.",
        ),
        examples: ["I didn't see him yesterday.", "Did you enjoy the concert?", "Where did they go last summer?"],
      },
      {
        text: t(
          "«…oldin» ma’nosida ago ishlatiladi va u vaqt ifodasidan keyin turadi: three years ago.",
          "«…назад» переводится как ago, и оно стоит после обозначения времени: three years ago.",
          "To say how long before now, use ago after the time period: three years ago.",
        ),
        examples: ["I started learning English two years ago.", "They moved to Bukhara a long time ago."],
      },
    ],
    mistakes: [
      {
        wrong: "I didn't went to school yesterday.",
        right: "I didn't go to school yesterday.",
        note: t(
          "Did / didn’t dan keyin fe’l boshlangʻich shaklda boʻladi — oʻtgan zamon belgisini did oʻzi koʻrsatadi.",
          "После did / didn't глагол стоит в начальной форме — прошедшее время уже показывает did.",
          "After did / didn't, use the base form — did already shows the past.",
        ),
      },
      {
        wrong: "Did you saw the new film?",
        right: "Did you see the new film?",
        note: t(
          "Soʻroq gapda ham did dan keyin fe’lning birinchi shakli ishlatiladi.",
          "В вопросе после did тоже нужна начальная форма глагола.",
          "In questions, the verb after did is in the base form too.",
        ),
      },
      {
        wrong: "I came to Tashkent before three years.",
        right: "I came to Tashkent three years ago.",
        note: t(
          "«Uch yil oldin» — three years ago. Before bu ma’noda vaqt miqdoridan oldin qoʻyilmaydi.",
          "«Три года назад» — three years ago, а не before three years.",
          "To say 'three years before now', use three years ago, not before three years.",
        ),
      },
      {
        wrong: "Yesterday I go to the market.",
        right: "Yesterday I went to the market.",
        note: t(
          "Yesterday oʻtgan zamonni bildiradi, demak fe’l ham oʻtgan zamon shaklida boʻlishi kerak.",
          "Yesterday указывает на прошлое, значит и глагол должен стоять в прошедшем времени.",
          "Yesterday refers to the past, so the verb must be in the past too.",
        ),
      },
    ],
    quiz: [
      {
        prompt: "We ___ a great film last night.",
        options: ["see", "saw", "seen", "seed"],
        answer: 1,
        why: t(
          "See — notoʻgʻri fe’l, uning oʻtgan zamon shakli saw.",
          "See — неправильный глагол, его форма прошедшего времени — saw.",
          "See is irregular; its past simple form is saw.",
        ),
      },
      {
        prompt: "She ___ to the party because she was ill.",
        options: ["didn't come", "didn't came", "not came"],
        answer: 0,
        why: t(
          "Inkor gapda didn’t + fe’lning boshlangʻich shakli ishlatiladi.",
          "В отрицании используется didn't + начальная форма глагола.",
          "Negatives use didn't + the base form.",
        ),
      },
      {
        prompt: "When ___ you arrive in London?",
        options: ["do", "were", "did"],
        answer: 2,
        why: t(
          "Oʻtmishdagi ish haqida soʻralmoqda, shuning uchun yordamchi fe’l did.",
          "Вопрос о прошлом, поэтому вспомогательный глагол — did.",
          "The question is about the past, so the auxiliary is did.",
        ),
      },
      {
        prompt: "I moved to Samarkand five years ___.",
        options: ["before", "last", "ago"],
        answer: 2,
        why: t(
          "«Besh yil oldin» — five years ago; ago vaqt ifodasidan keyin keladi.",
          "«Пять лет назад» — five years ago; ago стоит после периода времени.",
          "Ago comes after the time period: five years ago.",
        ),
      },
      {
        prompt: "He ___ English at university in 2015.",
        options: ["studyed", "studied", "studys"],
        answer: 1,
        why: t(
          "Undosh + y bilan tugagan fe’llarda y harfi i ga almashadi: study → studied.",
          "Если глагол оканчивается на согласную + y, y меняется на i: study → studied.",
          "When a verb ends in consonant + y, change y to i: study → studied.",
        ),
      },
      {
        prompt: "They ___ a new flat last month.",
        options: ["buyed", "buy", "brought", "bought"],
        answer: 3,
        why: t(
          "Buy — notoʻgʻri fe’l: buy → bought. Brought esa bring fe’lining shakli.",
          "Buy — неправильный глагол: buy → bought. Brought — это форма глагола bring.",
          "Buy is irregular: buy → bought. Brought is the past of bring.",
        ),
      },
    ],
  },
  {
    id: 'a2-articles',
    level: 'A2',
    title: t("Artikllar: a/an va the", "Артикли a/an и the", "Articles: a/an and the"),
    summary: t(
      "Oʻzbek va rus tillarida artikl yoʻq, shuning uchun ular koʻpincha tushib qoladi. A/an — «bitta, qandaydir», the — «oʻsha, ma’lum», umumiy ma’noda esa koʻpincha artikl ishlatilmaydi.",
      "В узбекском и русском языках нет артиклей, поэтому их часто пропускают. A/an — «один, какой-то», the — «тот самый, известный», а в общем значении артикль часто не нужен.",
      "Uzbek and Russian have no articles, so learners often leave them out. A/an means 'one, any', the means 'the specific one', and general ideas often need no article.",
    ),
    rules: [
      {
        text: t(
          "A/an birlikdagi sanaladigan otlar bilan ishlatiladi: narsa birinchi marta tilga olinganda yoki kasbni aytganda. Undosh tovushdan oldin a, unli tovushdan oldin an: a university, an hour.",
          "A/an используется с исчисляемыми существительными в единственном числе: при первом упоминании и при названии профессии. Перед согласным звуком — a, перед гласным звуком — an: a university, an hour.",
          "Use a/an with singular countable nouns: when you mention something for the first time and with jobs. A goes before a consonant sound, an before a vowel sound: a university, an hour.",
        ),
        examples: ["She is a doctor.", "I ate an apple and a banana.", "The lesson takes an hour."],
      },
      {
        text: t(
          "The — soʻzlovchi ham, tinglovchi ham qaysi narsa haqida gap ketayotganini bilganda: ikkinchi marta tilga olinganda, yagona narsalar (the sun, the internet) va orttirma daraja bilan.",
          "The — когда и говорящий, и слушающий знают, о чём речь: при повторном упоминании, с единственными в своём роде предметами (the sun, the internet) и с превосходной степенью.",
          "Use the when both speakers know which thing you mean: for something mentioned before, for unique things (the sun, the internet) and with superlatives.",
        ),
        examples: ["I bought a shirt and a hat. The shirt is blue.", "Can you close the door, please?", "The moon is very bright tonight."],
      },
      {
        text: t(
          "Umumiy ma’noda koʻplikdagi va sanalmaydigan otlar, tillar, ovqatlanish vaqtlari va koʻpchilik davlat hamda shahar nomlari artiklsiz ishlatiladi.",
          "Без артикля употребляются исчисляемые во множественном числе и неисчисляемые существительные в общем значении, языки, приёмы пищи, а также большинство названий стран и городов.",
          "Use no article with plural and uncountable nouns in a general sense, languages, meals, and most names of countries and cities.",
        ),
        examples: ["Cats like milk.", "I usually have breakfast at eight.", "She speaks Russian and lives in Uzbekistan."],
      },
    ],
    mistakes: [
      {
        wrong: "My brother is engineer.",
        right: "My brother is an engineer.",
        note: t(
          "Kasbni aytganda birlikdagi ot oldidan a/an qoʻyiladi.",
          "При названии профессии перед существительным в единственном числе нужен a/an.",
          "Jobs need a/an before a singular noun.",
        ),
      },
      {
        wrong: "The life is beautiful.",
        right: "Life is beautiful.",
        note: t(
          "Umumiy ma’nodagi mavhum otlar (life, love, music) oldidan the qoʻyilmaydi.",
          "Абстрактные существительные в общем значении (life, love, music) употребляются без the.",
          "Abstract nouns in a general sense (life, love, music) take no article.",
        ),
      },
      {
        wrong: "I have a good news for you.",
        right: "I have good news for you.",
        note: t(
          "News sanalmaydigan ot, shuning uchun a/an bilan ishlatilmaydi.",
          "News — неисчисляемое существительное, поэтому с a/an не употребляется.",
          "News is uncountable, so it can't take a/an.",
        ),
      },
      {
        wrong: "She is a best student in our group.",
        right: "She is the best student in our group.",
        note: t(
          "Orttirma daraja (best, tallest, most interesting) oldidan the qoʻyiladi.",
          "Перед превосходной степенью (best, tallest, most interesting) ставится the.",
          "Superlatives (best, tallest, most interesting) take the.",
        ),
      },
    ],
    quiz: [
      {
        prompt: "I saw ___ elephant at the zoo.",
        options: ["a", "an", "the"],
        answer: 1,
        why: t(
          "Elephant unli tovush bilan boshlanadi va birinchi marta tilga olinmoqda — an.",
          "Elephant начинается с гласного звука и упоминается впервые — an.",
          "Elephant starts with a vowel sound and is mentioned for the first time, so an.",
        ),
      },
      {
        prompt: "___ sun rises in the east.",
        options: ["The", "A", "— (no article)"],
        answer: 0,
        why: t(
          "Quyosh — yagona narsa, shuning uchun the.",
          "Солнце — единственное в своём роде, поэтому the.",
          "There is only one sun, so we use the.",
        ),
      },
      {
        prompt: "My mother is ___ teacher.",
        options: ["the", "an", "a"],
        answer: 2,
        why: t(
          "Kasb aytilmoqda, teacher undosh tovush bilan boshlanadi — a.",
          "Называется профессия, teacher начинается с согласного звука — a.",
          "It's a job, and teacher starts with a consonant sound, so a.",
        ),
      },
      {
        prompt: "I don't drink ___ coffee in the evening.",
        options: ["a", "the", "— (no article)"],
        answer: 2,
        why: t(
          "Coffee bu yerda umumiy ma’noda, sanalmaydigan ot — artikl kerak emas.",
          "Coffee здесь в общем значении и неисчисляемо — артикль не нужен.",
          "Coffee is uncountable and used in a general sense, so no article.",
        ),
      },
      {
        prompt: "It was ___ best day of my life.",
        options: ["a", "the", "an"],
        answer: 1,
        why: t(
          "Best — orttirma daraja, undan oldin doim the keladi.",
          "Best — превосходная степень, перед ней всегда the.",
          "Best is a superlative, so it takes the.",
        ),
      },
      {
        prompt: "My cousin studies at ___ university in Bukhara.",
        options: ["an", "a", "— (no article)"],
        answer: 1,
        why: t(
          "University «yu» tovushi bilan boshlanadi (undosh tovush), shuning uchun a.",
          "University начинается со звука «ю» (согласный звук [j]), поэтому a.",
          "University begins with a 'you' sound (a consonant sound), so we say a university.",
        ),
      },
    ],
  },
  {
    id: 'a2-comparatives',
    level: 'A2',
    title: t(
      "Sifatlarning qiyosiy va orttirma darajasi",
      "Сравнительная и превосходная степень прилагательных",
      "Comparatives and superlatives",
    ),
    summary: t(
      "Qiyosiy daraja ikki narsani solishtiradi (bigger than), orttirma daraja esa guruhdagi eng yuqori belgini koʻrsatadi (the biggest).",
      "Сравнительная степень сравнивает два предмета (bigger than), а превосходная указывает на самый высокий признак в группе (the biggest).",
      "Comparatives compare two things (bigger than); superlatives show the top of a group (the biggest).",
    ),
    rules: [
      {
        text: t(
          "Qisqa (bir boʻgʻinli) sifatlarga -er / -est qoʻshiladi. Imlo: big → bigger, nice → nicer, happy → happier. Solishtirishda than ishlatiladi.",
          "К коротким (односложным) прилагательным добавляется -er / -est. Правописание: big → bigger, nice → nicer, happy → happier. При сравнении используется than.",
          "Short (one-syllable) adjectives add -er / -est. Spelling: big → bigger, nice → nicer, happy → happier. Use than to compare.",
        ),
        examples: ["Tashkent is bigger than Namangan.", "Today is hotter than yesterday.", "This is the cheapest café in town."],
      },
      {
        text: t(
          "Uzun sifatlar (koʻpchilik ikki va undan ortiq boʻgʻinli sifatlar) oldidan more / the most qoʻyiladi.",
          "Перед длинными прилагательными (большинство двусложных и более) ставится more / the most.",
          "Long adjectives (most with two or more syllables) use more / the most.",
        ),
        examples: ["This book is more interesting than the film.", "It's the most expensive hotel in the city."],
      },
      {
        text: t(
          "Ba’zi sifatlar qoidaga boʻysunmaydi: good → better → the best, bad → worse → the worst, far → further → the furthest.",
          "Некоторые прилагательные образуют степени не по правилу: good → better → the best, bad → worse → the worst, far → further → the furthest.",
          "Some adjectives are irregular: good → better → the best, bad → worse → the worst, far → further → the furthest.",
        ),
        examples: ["My English is better now.", "That was the worst meal I've ever had."],
      },
      {
        text: t(
          "As … as — «…dek, …chalik» (tenglik), not as … as — «…chalik emas». Bu qurilishda sifat oddiy shaklda qoladi.",
          "As … as — «такой же … как», not as … as — «не такой … как». В этой конструкции прилагательное стоит в обычной форме.",
          "As … as means 'equal'; not as … as means 'less'. The adjective stays in its normal form.",
        ),
        examples: ["My phone is as old as yours.", "The bus isn't as fast as the metro."],
      },
    ],
    mistakes: [
      {
        wrong: "He is more taller than me.",
        right: "He is taller than me.",
        note: t(
          "-er va more birga ishlatilmaydi — ulardan faqat bittasini tanlang.",
          "-er и more вместе не используются — выберите что-то одно.",
          "Never use -er and more together — choose one.",
        ),
      },
      {
        wrong: "My room is bigger that yours.",
        right: "My room is bigger than yours.",
        note: t(
          "Solishtirishda than ishlatiladi; that va then — boshqa soʻzlar.",
          "При сравнении нужно than; that и then — другие слова.",
          "Comparisons use than; that and then are different words.",
        ),
      },
      {
        wrong: "It is the goodest pizza in town.",
        right: "It is the best pizza in town.",
        note: t(
          "Good — qoidaga boʻysunmaydigan sifat: good → better → the best.",
          "Good — исключение: good → better → the best.",
          "Good is irregular: good → better → the best.",
        ),
      },
    ],
    quiz: [
      {
        prompt: "An elephant is ___ than a horse.",
        options: ["heavier", "heavyer", "more heavy"],
        answer: 0,
        why: t(
          "Heavy — qisqa sifat, y harfi i ga almashadi: heavier.",
          "Heavy — короткое прилагательное, y меняется на i: heavier.",
          "Heavy is short, and y changes to i: heavier.",
        ),
      },
      {
        prompt: "This is the ___ film I have ever seen.",
        options: ["more boring", "most boring", "boringest"],
        answer: 1,
        why: t(
          "Boring — uzun sifat, orttirma darajasi the most boring.",
          "Boring — длинное прилагательное, превосходная степень — the most boring.",
          "Boring is a long adjective, so the superlative is the most boring.",
        ),
      },
      {
        prompt: "My English is ___ than last year.",
        options: ["gooder", "more good", "better"],
        answer: 2,
        why: t(
          "Good ning qiyosiy darajasi — better.",
          "Сравнительная степень от good — better.",
          "The comparative of good is better.",
        ),
      },
      {
        prompt: "Moscow is colder ___ Tashkent in winter.",
        options: ["that", "than", "then"],
        answer: 1,
        why: t(
          "Qiyosiy darajadan keyin than keladi.",
          "После сравнительной степени идёт than.",
          "A comparative is followed by than.",
        ),
      },
      {
        prompt: "The bus is not as ___ as the metro.",
        options: ["faster", "fastest", "fast"],
        answer: 2,
        why: t(
          "As … as qurilishida sifat oddiy shaklda boʻladi.",
          "В конструкции as … as прилагательное стоит в обычной форме.",
          "In as … as, the adjective stays in its normal form.",
        ),
      },
      {
        prompt: "Monday was the ___ day of my week.",
        options: ["worst", "baddest", "worse", "most bad"],
        answer: 0,
        why: t(
          "Bad → worse → the worst; the dan keyin orttirma daraja kerak.",
          "Bad → worse → the worst; после the нужна превосходная степень.",
          "Bad → worse → the worst; after the we need the superlative.",
        ),
      },
    ],
  },
  // ───────────────────────────── B1 ─────────────────────────────
  {
    id: 'b1-present-perfect-past-simple',
    level: 'B1',
    title: t(
      "Hozirgi tugallangan zamon va oʻtgan oddiy zamon",
      "Present Perfect и Past Simple",
      "Present perfect vs past simple",
    ),
    summary: t(
      "Present Perfect oʻtmishni hozirgi payt bilan bogʻlaydi (tajriba, natija, hali tugamagan davr), Past Simple esa tugagan, aniq vaqtdagi voqealar uchun ishlatiladi.",
      "Present Perfect связывает прошлое с настоящим (опыт, результат, незавершённый период), а Past Simple описывает завершённые события в определённое время.",
      "The present perfect links the past to now (experience, result, unfinished time); the past simple is for finished events at a definite time.",
    ),
    rules: [
      {
        text: t(
          "Present Perfect: have/has + fe’lning uchinchi shakli (V3). Hayotiy tajriba (ever, never) va hozir koʻrinib turgan natija haqida gapirganda ishlatiladi. Vaqt aniq aytilmaydi.",
          "Present Perfect: have/has + третья форма глагола (V3). Используется для жизненного опыта (ever, never) и результата, важного сейчас. Точное время не называется.",
          "Present perfect is have/has + past participle. Use it for life experience (ever, never) and results that matter now, without saying exactly when.",
        ),
        examples: ["I have visited Istanbul twice.", "Have you ever eaten sushi?", "She has lost her keys, so she can't get in."],
      },
      {
        text: t(
          "Past Simple tugagan vaqt ifodalari bilan ishlatiladi: yesterday, last year, in 2021, three days ago, when I was a child. Bunday soʻzlar bilan Present Perfect ishlatilmaydi.",
          "Past Simple употребляется с указаниями на законченное время: yesterday, last year, in 2021, three days ago, when I was a child. С ними Present Perfect не используется.",
          "Use the past simple with finished time expressions: yesterday, last year, in 2021, three days ago, when I was a child. Don't use the present perfect with them.",
        ),
        examples: ["I visited Istanbul in 2022.", "We met three years ago.", "When did you start your new job?"],
      },
      {
        text: t(
          "Hozirgacha davom etayotgan holat uchun Present Perfect + for (davomiylik: for five years) yoki since (boshlangʻich nuqta: since 2019) ishlatiladi. Oʻzbek va rus tillarida bu yerda hozirgi zamon qoʻllanadi, ingliz tilida esa yoʻq.",
          "Для ситуации, которая продолжается до сих пор, используется Present Perfect + for (период: for five years) или since (начальная точка: since 2019). В русском здесь настоящее время, а в английском — нет.",
          "For situations that started in the past and continue now, use the present perfect with for (a period: for five years) or since (a starting point: since 2019).",
        ),
        examples: ["I have lived here for five years.", "She has worked at the bank since 2019.", "How long have you known each other?"],
      },
      {
        text: t(
          "Just, already va yet koʻpincha Present Perfect bilan keladi: just — «hozirgina», already — «allaqachon», yet — inkor va soʻroqda «hali».",
          "Just, already и yet часто используются с Present Perfect: just — «только что», already — «уже», yet — «ещё» / «уже» в отрицаниях и вопросах.",
          "Just, already and yet often go with the present perfect: just = a moment ago, already = sooner than expected, yet = in negatives and questions.",
        ),
        examples: ["I've just finished my homework.", "They've already left.", "Have you replied to the email yet?"],
      },
    ],
    mistakes: [
      {
        wrong: "I have seen him yesterday.",
        right: "I saw him yesterday.",
        note: t(
          "Yesterday — tugagan vaqt, shuning uchun faqat Past Simple.",
          "Yesterday — законченное время, поэтому только Past Simple.",
          "Yesterday is finished time, so only the past simple is possible.",
        ),
      },
      {
        wrong: "I live here since 2018.",
        right: "I have lived here since 2018.",
        note: t(
          "«2018-yildan beri yashayman» ingliz tilida Present Perfect bilan aytiladi, hozirgi oddiy zamon bilan emas.",
          "«Живу здесь с 2018 года» по-английски требует Present Perfect, а не Present Simple.",
          "For an ongoing situation with since, English uses the present perfect, not the present simple.",
        ),
      },
      {
        wrong: "I have studied English since three years.",
        right: "I have studied English for three years.",
        note: t(
          "Davomiylik (three years, a month) bilan for, boshlangʻich nuqta (2020, Monday) bilan since ishlatiladi.",
          "С периодом времени (three years, a month) — for, с начальной точкой (2020, Monday) — since.",
          "Use for with a period (three years) and since with a starting point (2020).",
        ),
      },
      {
        wrong: "Did you ever been to London?",
        right: "Have you ever been to London?",
        note: t(
          "Hayotiy tajriba haqida soʻralganda Have you ever + V3 ishlatiladi.",
          "Когда спрашивают о жизненном опыте, используется Have you ever + V3.",
          "Questions about life experience use Have you ever + past participle.",
        ),
      },
    ],
    quiz: [
      {
        prompt: "She ___ in this company since 2020.",
        options: ["works", "has worked", "worked"],
        answer: 1,
        why: t(
          "Since + hozirgacha davom etayotgan holat → Present Perfect.",
          "Since + ситуация, которая продолжается до сих пор → Present Perfect.",
          "Since plus a situation that continues now needs the present perfect.",
        ),
      },
      {
        prompt: "We ___ to Samarkand last summer.",
        options: ["have gone", "have been", "went"],
        answer: 2,
        why: t(
          "Last summer — tugagan vaqt, demak Past Simple.",
          "Last summer — законченное время, значит Past Simple.",
          "Last summer is finished time, so we use the past simple.",
        ),
      },
      {
        prompt: "I have known Dilnoza ___ ten years.",
        options: ["since", "for", "from", "during"],
        answer: 1,
        why: t(
          "Ten years — davomiylik, shuning uchun for.",
          "Ten years — период времени, поэтому for.",
          "Ten years is a period, so we use for.",
        ),
      },
      {
        prompt: "___ you finished your report yet?",
        options: ["Have", "Did", "Are"],
        answer: 0,
        why: t(
          "Yet bilan soʻroq gapda Present Perfect ishlatiladi: Have you finished…?",
          "С yet в вопросе используется Present Perfect: Have you finished…?",
          "Questions with yet use the present perfect: Have you finished…?",
        ),
      },
      {
        prompt: "This is the first time I ___ a horse.",
        options: ["ride", "rode", "have ridden"],
        answer: 2,
        why: t(
          "This is the first time … qurilishidan keyin Present Perfect keladi.",
          "После This is the first time … используется Present Perfect.",
          "This is the first time … is followed by the present perfect.",
        ),
      },
      {
        prompt: "When ___ you start learning English?",
        options: ["have", "did", "has"],
        answer: 1,
        why: t(
          "When aniq vaqtni soʻraydi, shuning uchun Past Simple: When did…?",
          "When спрашивает о конкретном времени, поэтому Past Simple: When did…?",
          "When asks about a definite time, so we use the past simple.",
        ),
      },
    ],
  },
  {
    id: 'b1-future-forms',
    level: 'B1',
    title: t(
      "Kelasi zamon shakllari: will, be going to va Present Continuous",
      "Способы выражения будущего: will, be going to и Present Continuous",
      "Future forms: will, going to and present continuous",
    ),
    summary: t(
      "Ingliz tilida kelajak haqida bir necha usulda gapiriladi. Tanlov qaror qachon qabul qilinganiga va reja qanchalik aniq ekaniga bogʻliq.",
      "В английском о будущем можно говорить несколькими способами. Выбор зависит от того, когда принято решение и насколько план определён.",
      "English has several ways to talk about the future. The choice depends on when you made the decision and how fixed the plan is.",
    ),
    rules: [
      {
        text: t(
          "Will — gapirayotgan paytda qabul qilingan qaror, va’da, yordam taklifi hamda shaxsiy fikrga asoslangan bashorat (I think…, probably).",
          "Will — решение, принятое в момент речи, обещание, предложение помощи и прогноз, основанный на мнении (I think…, probably).",
          "Will is for decisions made at the moment of speaking, promises, offers and predictions based on opinion (I think…, probably).",
        ),
        examples: ["It's cold in here — I'll close the window.", "Don't worry, I'll help you with your bags.", "I think it will be a great match."],
      },
      {
        text: t(
          "Be going to — oldindan oʻylab qoʻyilgan niyat yoki hozirgi belgilarga asoslangan bashorat.",
          "Be going to — заранее обдуманное намерение или прогноз, основанный на том, что мы видим сейчас.",
          "Be going to is for intentions decided earlier and for predictions based on present evidence.",
        ),
        examples: ["I'm going to study medicine next year.", "Look at those clouds — it's going to rain."],
      },
      {
        text: t(
          "Present Continuous — vaqti va joyi kelishib olingan aniq rejalar (uchrashuv, safar) uchun.",
          "Present Continuous — для договорённостей с конкретным временем и местом (встречи, поездки).",
          "The present continuous is for fixed arrangements with a time and place (meetings, trips).",
        ),
        examples: ["I'm meeting Sardor at six tonight.", "We're flying to Dubai on Friday."],
      },
      {
        text: t(
          "When, as soon as, before, after, until va if dan keyin kelasi zamon ma’nosida will emas, Present Simple ishlatiladi.",
          "После when, as soon as, before, after, until и if в значении будущего используется Present Simple, а не will.",
          "After when, as soon as, before, after, until and if, use the present simple for the future, not will.",
        ),
        examples: ["I'll call you when I get home.", "We'll start as soon as everyone arrives."],
      },
    ],
    mistakes: [
      {
        wrong: "I will call you when I will arrive.",
        right: "I'll call you when I arrive.",
        note: t(
          "When dan keyingi qismda will ishlatilmaydi — Present Simple kerak.",
          "В придаточном после when will не используется — нужен Present Simple.",
          "Don't use will after when in a time clause — use the present simple.",
        ),
      },
      {
        wrong: "Tomorrow I go to the dentist.",
        right: "Tomorrow I'm going to the dentist.",
        note: t(
          "Kelishilgan reja uchun Present Continuous ishlatiladi. «Ertaga boraman» ni hozirgi oddiy zamonda tarjima qilmang.",
          "Для договорённости используется Present Continuous. Не переводите «завтра я иду» через Present Simple.",
          "Use the present continuous for arrangements, not the present simple.",
        ),
      },
      {
        wrong: "I going to buy a car.",
        right: "I'm going to buy a car.",
        note: t(
          "Going to dan oldin am/is/are albatta boʻlishi kerak.",
          "Перед going to обязательно нужен am/is/are.",
          "Going to always needs am/is/are before it.",
        ),
      },
      {
        wrong: "Wait, I help you!",
        right: "Wait, I'll help you!",
        note: t(
          "Shu zahoti qabul qilingan qaror yoki yordam taklifi will bilan ifodalanadi.",
          "Спонтанное решение или предложение помощи выражается через will.",
          "Instant decisions and offers of help use will.",
        ),
      },
    ],
    quiz: [
      {
        prompt: "The phone is ringing. — OK, I ___ it.",
        options: ["answer", "'ll answer", "am going to answer"],
        answer: 1,
        why: t(
          "Qaror aynan hozir qabul qilinmoqda, shuning uchun will.",
          "Решение принимается прямо сейчас, поэтому will.",
          "The decision is made at the moment of speaking, so will.",
        ),
      },
      {
        prompt: "Look at that driver! He ___ crash!",
        options: ["will", "is going to", "is"],
        answer: 1,
        why: t(
          "Bashorat koʻrinib turgan belgiga asoslangan, shuning uchun be going to.",
          "Прогноз основан на том, что мы видим, поэтому be going to.",
          "The prediction is based on what we can see, so be going to.",
        ),
      },
      {
        prompt: "I ___ my grandmother on Sunday — we've already arranged it.",
        options: ["visit", "will visit", "am visiting"],
        answer: 2,
        why: t(
          "Kelishib olingan reja — Present Continuous.",
          "Это договорённость — Present Continuous.",
          "It's an arrangement, so we use the present continuous.",
        ),
      },
      {
        prompt: "Please text me as soon as you ___ home.",
        options: ["get", "will get", "are getting"],
        answer: 0,
        why: t(
          "As soon as dan keyin kelasi zamon ma’nosida Present Simple ishlatiladi.",
          "После as soon as будущее выражается через Present Simple.",
          "After as soon as, we use the present simple for the future.",
        ),
      },
      {
        prompt: "I think people ___ on Mars one day.",
        options: ["are living", "will live", "live"],
        answer: 1,
        why: t(
          "I think bilan shaxsiy fikrga asoslangan bashorat — will.",
          "Прогноз-мнение с I think — will.",
          "A prediction based on opinion (I think) uses will.",
        ),
      },
      {
        prompt: "She has saved some money. She ___ a new laptop.",
        options: ["is going to buy", "buys", "will buying"],
        answer: 0,
        why: t(
          "Bu oldindan oʻylangan niyat, shuning uchun be going to.",
          "Это заранее обдуманное намерение, поэтому be going to.",
          "It's a plan she decided earlier, so be going to.",
        ),
      },
    ],
  },
  {
    id: 'b1-conditionals-1-2',
    level: 'B1',
    title: t(
      "Birinchi va ikkinchi tur shart gaplar",
      "Условные предложения первого и второго типа",
      "First and second conditionals",
    ),
    summary: t(
      "Birinchi tur shart gap real, boʻlishi mumkin boʻlgan vaziyatlar haqida, ikkinchi tur esa xayoliy yoki ehtimoli past vaziyatlar haqida gapiradi.",
      "Условные предложения первого типа описывают реальные, возможные ситуации, а второго типа — воображаемые или маловероятные.",
      "The first conditional talks about real, possible situations; the second conditional talks about imaginary or unlikely ones.",
    ),
    rules: [
      {
        text: t(
          "Birinchi tur: If + Present Simple, will + fe’l. Kelajakda haqiqatan sodir boʻlishi mumkin boʻlgan vaziyat va uning natijasi.",
          "Первый тип: If + Present Simple, will + глагол. Реальная ситуация в будущем и её результат.",
          "First conditional: If + present simple, will + verb. A real possibility in the future and its result.",
        ),
        examples: ["If it rains, we'll stay at home.", "If you practise every day, you will pass the exam.", "She'll be angry if we're late."],
      },
      {
        text: t(
          "Ikkinchi tur: If + Past Simple, would + fe’l. Hozirgi yoki kelajakdagi xayoliy vaziyat. Past Simple bu yerda oʻtmishni emas, «haqiqiy emas» degan ma’noni bildiradi.",
          "Второй тип: If + Past Simple, would + глагол. Воображаемая ситуация в настоящем или будущем. Past Simple здесь означает не прошлое, а нереальность.",
          "Second conditional: If + past simple, would + verb. An imaginary situation now or in the future. The past form here means 'not real', not 'past'.",
        ),
        examples: ["If I had more time, I would learn Japanese.", "If I won the lottery, I'd travel around the world.", "What would you do if you lost your passport?"],
      },
      {
        text: t(
          "Maslahat berishda If I were you… ishlatiladi (barcha shaxslarda were). Unless «agar … boʻlmasa» ma’nosini beradi.",
          "Для совета используется If I were you… (were для всех лиц). Unless означает «если не».",
          "Use If I were you… to give advice (were for all persons). Unless means 'if not'.",
        ),
        examples: ["If I were you, I'd apologise to her.", "You won't improve unless you practise."],
      },
    ],
    mistakes: [
      {
        wrong: "If it will rain, we will stay home.",
        right: "If it rains, we will stay home.",
        note: t(
          "If dan keyingi qismda will ishlatilmaydi — Present Simple kerak.",
          "В части с if will не используется — нужен Present Simple.",
          "Don't use will in the if-clause; use the present simple.",
        ),
      },
      {
        wrong: "If I would have money, I would buy a house.",
        right: "If I had money, I would buy a house.",
        note: t(
          "Would faqat natija qismida boʻladi; if qismida Past Simple ishlatiladi.",
          "Would бывает только в главной части; в части с if — Past Simple.",
          "Would goes only in the result clause; the if-clause uses the past simple.",
        ),
      },
      {
        wrong: "If I am you, I would call her.",
        right: "If I were you, I would call her.",
        note: t(
          "Xayoliy vaziyatda If I were you qoʻllanadi — bu tayyor ibora.",
          "В нереальном условии говорят If I were you — это устойчивая конструкция.",
          "For imaginary advice, the fixed phrase is If I were you.",
        ),
      },
    ],
    quiz: [
      {
        prompt: "If she ___ early, she will catch the bus.",
        options: ["leaves", "will leave", "left"],
        answer: 0,
        why: t(
          "Birinchi tur shart gap: if qismida Present Simple.",
          "Первый тип: в части с if — Present Simple.",
          "First conditional: present simple in the if-clause.",
        ),
      },
      {
        prompt: "If I ___ a car, I would drive to work.",
        options: ["have", "had", "would have"],
        answer: 1,
        why: t(
          "Ikkinchi tur: If + Past Simple, would + fe’l.",
          "Второй тип: If + Past Simple, would + глагол.",
          "Second conditional: If + past simple, would + verb.",
        ),
      },
      {
        prompt: "What would you do if you ___ a million dollars?",
        options: ["find", "will find", "would find", "found"],
        answer: 3,
        why: t(
          "Xayoliy vaziyat, would bor — demak if qismida Past Simple.",
          "Воображаемая ситуация с would — значит, в части с if Past Simple.",
          "It's imaginary and the result has would, so the if-clause needs the past simple.",
        ),
      },
      {
        prompt: "If I were you, I ___ that job.",
        options: ["will take", "would take", "take"],
        answer: 1,
        why: t(
          "If I were you dan keyin maslahat would bilan beriladi.",
          "После If I were you совет даётся с would.",
          "After If I were you, the advice uses would.",
        ),
      },
      {
        prompt: "We won't go out ___ the rain stops.",
        options: ["if", "because", "unless"],
        answer: 2,
        why: t(
          "Unless = if not: «yomgʻir toʻxtamasa, chiqmaymiz».",
          "Unless = if not: «не выйдем, если дождь не прекратится».",
          "Unless means 'if not': we won't go out if the rain doesn't stop.",
        ),
      },
      {
        prompt: "If he ___ harder, he'd get better marks.",
        options: ["studied", "studies", "will study"],
        answer: 0,
        why: t(
          "He’d = he would, demak ikkinchi tur: if qismida Past Simple.",
          "He'd = he would, значит второй тип: в части с if — Past Simple.",
          "He'd means he would, so it's the second conditional with the past simple.",
        ),
      },
    ],
  },
  {
    id: 'b1-modals-obligation',
    level: 'B1',
    title: t(
      "Majburiyat va maslahat: must, have to, should",
      "Модальные глаголы долженствования и совета: must, have to, should",
      "Obligation and advice: must, have to, should",
    ),
    summary: t(
      "Bu modal fe’llar nima majburiy, nima taqiqlangan, nima shart emas va nima tavsiya etilishini aytish uchun kerak.",
      "Эти модальные глаголы помогают сказать, что обязательно, что запрещено, что необязательно и что рекомендуется.",
      "These modal verbs help you say what is necessary, forbidden, not necessary or advisable.",
    ),
    rules: [
      {
        text: t(
          "Must — kuchli majburiyat, koʻpincha soʻzlovchining oʻz fikri yoki rasmiy qoida. Must dan keyin to qoʻyilmaydi.",
          "Must — сильная обязанность, часто по мнению самого говорящего, или официальное правило. После must частица to не ставится.",
          "Must is strong obligation, often the speaker's own opinion or an official rule. There is no to after must.",
        ),
        examples: ["I must call my mum tonight.", "Passengers must wear seat belts."],
      },
      {
        text: t(
          "Have to — tashqi sharoit yoki qoidadan kelib chiqadigan majburiyat. Oʻtgan va kelasi zamonda faqat have to ishlatiladi: had to, will have to.",
          "Have to — обязанность, вызванная внешними обстоятельствами или правилами. В прошедшем и будущем времени используется только have to: had to, will have to.",
          "Have to is obligation from outside rules or circumstances. For the past and future use have to: had to, will have to.",
        ),
        examples: ["I have to wear a uniform at work.", "We had to wait for two hours.", "You'll have to show your passport."],
      },
      {
        text: t(
          "Mustn’t — «mumkin emas» (taqiq), don’t have to — «shart emas» (zarurat yoʻq). Ma’nolari butunlay farq qiladi!",
          "Mustn't — «нельзя» (запрет), don't have to — «не обязательно». Значения совершенно разные!",
          "Mustn't means it's forbidden; don't have to means it isn't necessary. They are very different!",
        ),
        examples: ["You mustn't smoke in the hospital.", "You don't have to come if you're busy."],
      },
      {
        text: t(
          "Should / shouldn’t — maslahat va tavsiya: «…ish kerak», «…masligi kerak».",
          "Should / shouldn't — совет и рекомендация: «следует», «не следует».",
          "Should / shouldn't give advice and recommendations.",
        ),
        examples: ["You should see a doctor.", "You shouldn't drink coffee so late."],
      },
    ],
    mistakes: [
      {
        wrong: "You must to finish it today.",
        right: "You must finish it today.",
        note: t(
          "Must, should, can kabi modal fe’llardan keyin to qoʻyilmaydi.",
          "После модальных глаголов must, should, can частица to не нужна.",
          "No to after modal verbs like must, should and can.",
        ),
      },
      {
        wrong: "Yesterday I must stay at work late.",
        right: "Yesterday I had to stay at work late.",
        note: t(
          "Must ning oʻtgan zamon shakli yoʻq — had to ishlatiladi.",
          "У must нет формы прошедшего времени — используется had to.",
          "Must has no past form — use had to.",
        ),
      },
      {
        wrong: "You mustn't pay — the museum is free.",
        right: "You don't have to pay — the museum is free.",
        note: t(
          "Toʻlash taqiqlanmagan, shunchaki shart emas — don’t have to.",
          "Платить не запрещено, просто не обязательно — don't have to.",
          "Paying isn't forbidden, it just isn't necessary — don't have to.",
        ),
      },
      {
        wrong: "She have to go now.",
        right: "She has to go now.",
        note: t(
          "He, she, it bilan has to ishlatiladi.",
          "С he, she, it используется has to.",
          "With he, she and it, use has to.",
        ),
      },
    ],
    quiz: [
      {
        prompt: "You ___ use your phone during the exam. It's forbidden.",
        options: ["don't have to", "mustn't", "shouldn't to"],
        answer: 1,
        why: t(
          "Taqiq — mustn’t.",
          "Запрет — mustn't.",
          "It's forbidden, so mustn't.",
        ),
      },
      {
        prompt: "Tomorrow is Sunday, so I ___ get up early.",
        options: ["don't have to", "mustn't", "must"],
        answer: 0,
        why: t(
          "Yakshanba — erta turish shart emas: don’t have to.",
          "В воскресенье рано вставать не обязательно: don't have to.",
          "It isn't necessary on Sunday, so don't have to.",
        ),
      },
      {
        prompt: "Last week he ___ work on Saturday.",
        options: ["must", "had to", "has to"],
        answer: 1,
        why: t(
          "Oʻtgan zamondagi majburiyat — had to.",
          "Обязанность в прошлом — had to.",
          "Past obligation is had to.",
        ),
      },
      {
        prompt: "You look tired. You ___ go to bed earlier.",
        options: ["must to", "have", "should"],
        answer: 2,
        why: t(
          "Bu maslahat, shuning uchun should.",
          "Это совет, поэтому should.",
          "This is advice, so should.",
        ),
      },
      {
        prompt: "My sister ___ wear a uniform at school.",
        options: ["have to", "has to", "must to"],
        answer: 1,
        why: t(
          "My sister = she, shuning uchun has to.",
          "My sister = she, поэтому has to.",
          "My sister is she, so has to.",
        ),
      },
      {
        prompt: "Do you think I ___ apply for this job?",
        options: ["should", "must to", "have"],
        answer: 0,
        why: t(
          "Fikr va maslahat soʻralmoqda — should.",
          "Спрашивают мнение и совет — should.",
          "The speaker is asking for advice, so should.",
        ),
      },
    ],
  },
  // ───────────────────────────── B2 ─────────────────────────────
  {
    id: 'b2-passive-voice',
    level: 'B2',
    title: t("Majhul nisbat", "Страдательный залог", "The passive voice"),
    summary: t(
      "Majhul nisbatda diqqat ish-harakatni kim bajarganiga emas, harakatning oʻziga yoki natijasiga qaratiladi. Yangiliklar, ilmiy matnlar va rasmiy yozuvlarda juda koʻp uchraydi.",
      "Страдательный залог переносит внимание с того, кто совершил действие, на само действие или его результат. Он очень част в новостях, научных и официальных текстах.",
      "The passive puts the focus on the action or result, not on who did it. It is very common in news, science and formal writing.",
    ),
    rules: [
      {
        text: t(
          "Yasalishi: be (kerakli zamonda) + fe’lning uchinchi shakli (V3). Zamonni be fe’li koʻrsatadi.",
          "Образование: be (в нужном времени) + третья форма глагола (V3). Время показывает глагол be.",
          "Form: be (in the right tense) + past participle. The verb be shows the tense.",
        ),
        examples: ["English is spoken all over the world.", "The bridge was built in 1965.", "The results will be announced tomorrow."],
      },
      {
        text: t(
          "Bajaruvchi noma’lum, ravshan yoki muhim boʻlmaganda majhul nisbat qoʻllanadi. Bajaruvchini aytish kerak boʻlsa, by ishlatiladi.",
          "Пассив используется, когда исполнитель неизвестен, очевиден или неважен. Если исполнителя нужно назвать, используется by.",
          "Use the passive when the doer is unknown, obvious or unimportant. If you need to name the doer, use by.",
        ),
        examples: ["My bike has been stolen!", "Guernica was painted by Picasso.", "The suspect was arrested last night."],
      },
      {
        text: t(
          "Modal fe’llar bilan: modal + be + V3. Davomli zamonlarda: is/was being + V3.",
          "С модальными глаголами: модальный глагол + be + V3. В длительных временах: is/was being + V3.",
          "With modals: modal + be + past participle. In continuous tenses: is/was being + past participle.",
        ),
        examples: ["The form must be signed by a parent.", "The road is being repaired this week.", "This problem can't be solved quickly."],
      },
      {
        text: t(
          "Rasmiy uslubda fikr yoki mish-mishni yetkazish uchun: It is said that… / He is believed to… Bu «aytilishicha», «taxmin qilinishicha» ma’nosini beradi.",
          "В официальном стиле для передачи мнений и слухов: It is said that… / He is believed to… — «говорят, что», «считается, что».",
          "In formal style, report opinions with It is said that… or He is believed to…",
        ),
        examples: ["It is said that the city was founded 2,500 years ago.", "She is believed to be the best candidate."],
      },
    ],
    mistakes: [
      {
        wrong: "The house was build in 1990.",
        right: "The house was built in 1990.",
        note: t(
          "Majhul nisbatda fe’lning uchinchi shakli (V3) kerak: build → built.",
          "В пассиве нужна третья форма глагола: build → built.",
          "The passive needs the past participle: build → built.",
        ),
      },
      {
        wrong: "This novel was written from Tolstoy.",
        right: "This novel was written by Tolstoy.",
        note: t(
          "«Tolstoy tomonidan» — by Tolstoy. From bu ma’noda ishlatilmaydi.",
          "«Написан Толстым» — by Tolstoy, а не from.",
          "The doer is introduced with by, not from.",
        ),
      },
      {
        wrong: "The accident was happened at night.",
        right: "The accident happened at night.",
        note: t(
          "Happen, arrive, die kabi toʻldiruvchi olmaydigan fe’llar majhul nisbatda ishlatilmaydi.",
          "Непереходные глаголы (happen, arrive, die) не образуют страдательный залог.",
          "Verbs with no object (happen, arrive, die) can't be passive.",
        ),
      },
      {
        wrong: "The letter sent yesterday.",
        right: "The letter was sent yesterday.",
        note: t(
          "Majhul nisbatda be yordamchi fe’li tushib qolmasligi kerak.",
          "В пассиве нельзя пропускать вспомогательный глагол be.",
          "Don't leave out be in the passive.",
        ),
      },
    ],
    quiz: [
      {
        prompt: "This mosque ___ in the 15th century.",
        options: ["built", "was built", "was building"],
        answer: 1,
        why: t(
          "Masjidni kimdir qurgan, masjid oʻzi qurmagan — was + V3.",
          "Мечеть построили, она не строила сама — was + V3.",
          "The mosque didn't build anything; it was built — was + past participle.",
        ),
      },
      {
        prompt: "The winners ___ next week.",
        options: ["will be announced", "will announce", "are announcing"],
        answer: 0,
        why: t(
          "Kelasi zamondagi majhul nisbat: will be + V3.",
          "Пассив в будущем времени: will be + V3.",
          "Future passive: will be + past participle.",
        ),
      },
      {
        prompt: "You can't use the lift — it ___ at the moment.",
        options: ["is repaired", "repairs", "is being repaired"],
        answer: 2,
        why: t(
          "At the moment — hozir davom etayotgan jarayon: is being + V3.",
          "At the moment — процесс идёт сейчас: is being + V3.",
          "At the moment shows an action in progress: is being + past participle.",
        ),
      },
      {
        prompt: "The accident ___ at about 9 p.m.",
        options: ["was happened", "happened", "has been happened"],
        answer: 1,
        why: t(
          "Happen majhul nisbatda ishlatilmaydi.",
          "Глагол happen не употребляется в пассиве.",
          "Happen can't be used in the passive.",
        ),
      },
      {
        prompt: "All documents must ___ before Friday.",
        options: ["be signed", "signed", "be sign"],
        answer: 0,
        why: t(
          "Modal fe’l bilan majhul nisbat: must be + V3.",
          "Пассив с модальным глаголом: must be + V3.",
          "Passive with a modal: must be + past participle.",
        ),
      },
      {
        prompt: "Hamlet was written ___ Shakespeare.",
        options: ["from", "by", "with"],
        answer: 1,
        why: t(
          "Bajaruvchi by bilan kiritiladi.",
          "Исполнитель действия вводится предлогом by.",
          "The doer is introduced with by.",
        ),
      },
    ],
  },
  {
    id: 'b2-reported-speech',
    level: 'B2',
    title: t("Oʻzlashtirma gap", "Косвенная речь", "Reported speech"),
    summary: t(
      "Oʻzlashtirma gap boshqa odamning soʻzlarini qayta aytib berish uchun ishlatiladi. Koʻpincha zamon bir pogʻona orqaga suriladi, olmoshlar va vaqt soʻzlari oʻzgaradi.",
      "Косвенная речь нужна, чтобы передать чужие слова. Обычно время сдвигается на шаг в прошлое, а местоимения и слова времени меняются.",
      "Reported speech is used to tell someone what another person said. Tenses usually move one step back, and pronouns and time words change.",
    ),
    rules: [
      {
        text: t(
          "Kirish fe’li oʻtgan zamonda boʻlsa (said, told), zamon orqaga suriladi: Present → Past, Past / Present Perfect → Past Perfect, will → would, can → could.",
          "Если вводящий глагол в прошедшем времени (said, told), время сдвигается: Present → Past, Past / Present Perfect → Past Perfect, will → would, can → could.",
          "When the reporting verb is past (said, told), tenses move back: present → past, past / present perfect → past perfect, will → would, can → could.",
        ),
        examples: ["'I'm tired.' → She said she was tired.", "'I will call you.' → He said he would call me.", "'We have finished.' → They said they had finished."],
      },
      {
        text: t(
          "Tell dan keyin albatta kishi keladi (tell me, told her), say dan keyin esa kishi shart emas yoki to bilan keladi (said to me).",
          "После tell обязательно идёт человек (tell me, told her), а после say человек не нужен или вводится через to (said to me).",
          "Tell needs a person after it (told me); say doesn't, or uses to (said to me).",
        ),
        examples: ["He told me he was busy.", "He said that he was busy.", "My teacher told us to read more."],
      },
      {
        text: t(
          "Oʻzlashtirma soʻroqda soʻz tartibi darak gapdagidek boʻladi, do/does/did ishlatilmaydi. Ha/yoʻq savollari uchun if yoki whether qoʻshiladi.",
          "В косвенном вопросе порядок слов как в утвердительном предложении, do/does/did не используются. Для общих вопросов добавляется if или whether.",
          "Reported questions use statement word order with no do/does/did. For yes/no questions add if or whether.",
        ),
        examples: ["She asked me where I lived.", "He asked if I liked football.", "They wanted to know whether we had tickets."],
      },
      {
        text: t(
          "Vaqt va joy soʻzlari ham oʻzgaradi: now → then, today → that day, tomorrow → the next day, yesterday → the day before, here → there.",
          "Слова времени и места тоже меняются: now → then, today → that day, tomorrow → the next day, yesterday → the day before, here → there.",
          "Time and place words also change: now → then, today → that day, tomorrow → the next day, yesterday → the day before, here → there.",
        ),
        examples: ["'I'll do it tomorrow.' → He said he would do it the next day.", "'I was here yesterday.' → She said she had been there the day before."],
      },
    ],
    mistakes: [
      {
        wrong: "He said me that he was late.",
        right: "He told me that he was late.",
        note: t(
          "Kishini aytsangiz, tell ishlating: told me. Said me — xato.",
          "Если называете человека, используйте tell: told me. Said me — ошибка.",
          "If you mention the listener, use tell: told me, not said me.",
        ),
      },
      {
        wrong: "She asked me where do I live.",
        right: "She asked me where I lived.",
        note: t(
          "Oʻzlashtirma soʻroqda soʻroq tartibi va do ishlatilmaydi.",
          "В косвенном вопросе нет вопросительного порядка слов и do.",
          "Reported questions have no question word order and no do.",
        ),
      },
      {
        wrong: "He asked me do I like tea.",
        right: "He asked me if I liked tea.",
        note: t(
          "Ha/yoʻq savollari if yoki whether bilan yetkaziladi.",
          "Общие вопросы передаются через if или whether.",
          "Yes/no questions are reported with if or whether.",
        ),
      },
    ],
    quiz: [
      {
        prompt: "'I am hungry,' said Ali. → Ali said that he ___ hungry.",
        options: ["is", "was", "has been"],
        answer: 1,
        why: t(
          "Said dan keyin Present Simple Past Simple ga oʻtadi: am → was.",
          "После said Present Simple сдвигается в Past Simple: am → was.",
          "After said, the present moves back to the past: am → was.",
        ),
      },
      {
        prompt: "My teacher ___ me to read more books.",
        options: ["told", "said", "spoke"],
        answer: 0,
        why: t(
          "Kishi + to + fe’l qurilishi tell bilan ishlatiladi: told me to…",
          "Конструкция человек + to + глагол используется с tell: told me to…",
          "Tell + person + to-infinitive is used for instructions: told me to…",
        ),
      },
      {
        prompt: "She asked me where ___.",
        options: ["I worked", "did I work", "do I work"],
        answer: 0,
        why: t(
          "Oʻzlashtirma soʻroqda darak gap tartibi: where I worked.",
          "В косвенном вопросе прямой порядок слов: where I worked.",
          "Reported questions use statement order: where I worked.",
        ),
      },
      {
        prompt: "'Can you help me?' → He asked if I ___ help him.",
        options: ["can", "will", "could"],
        answer: 2,
        why: t(
          "Can oʻzlashtirma gapda could ga aylanadi.",
          "В косвенной речи can превращается в could.",
          "Can becomes could in reported speech.",
        ),
      },
      {
        prompt: "'We will finish it tomorrow.' → They said they ___ it the next day.",
        options: ["will finish", "would finish", "finish"],
        answer: 1,
        why: t(
          "Will oʻzlashtirma gapda would ga aylanadi.",
          "Will в косвенной речи становится would.",
          "Will becomes would in reported speech.",
        ),
      },
      {
        prompt: "He asked me ___ I had ever been to Japan.",
        options: ["that", "whether", "what"],
        answer: 1,
        why: t(
          "Ha/yoʻq savoli whether (yoki if) bilan yetkaziladi.",
          "Общий вопрос передаётся через whether (или if).",
          "A yes/no question is reported with whether (or if).",
        ),
      },
    ],
  },
  {
    id: 'b2-relative-clauses',
    level: 'B2',
    title: t(
      "Aniqlovchi ergash gaplar: who, which, that, whose, where",
      "Определительные придаточные предложения: who, which, that, whose, where",
      "Relative clauses: defining and non-defining",
    ),
    summary: t(
      "Aniqlovchi ergash gaplar odam yoki narsa haqida qoʻshimcha ma’lumot beradi va ikki qisqa gapni bitta silliq gapga birlashtirishga yordam beradi.",
      "Определительные придаточные дают дополнительную информацию о человеке или предмете и помогают объединить два коротких предложения в одно.",
      "Relative clauses add information about a person or thing and let you join two short sentences into one smooth one.",
    ),
    rules: [
      {
        text: t(
          "Who — odamlar uchun, which — narsa va hayvonlar uchun, whose — egalik («kimning»), where — joy, when — vaqt. That aniqlovchi gaplarda who va which oʻrnida kela oladi.",
          "Who — для людей, which — для предметов и животных, whose — принадлежность («чей»), where — место, when — время. That может заменять who и which в ограничительных придаточных.",
          "Who is for people, which for things and animals, whose for possession, where for places, when for times. That can replace who or which in defining clauses.",
        ),
        examples: ["The woman who lives next door is a nurse.", "That's the man whose car was stolen.", "This is the café where we first met."],
      },
      {
        text: t(
          "Cheklovchi (defining) ergash gap qaysi odam yoki narsa nazarda tutilganini aniqlaydi, vergul qoʻyilmaydi. Nisbiy olmosh toʻldiruvchi boʻlsa, uni tushirib qoldirish mumkin.",
          "Ограничительное (defining) придаточное уточняет, о ком или о чём речь; запятые не ставятся. Если относительное местоимение — дополнение, его можно опустить.",
          "A defining clause tells us which person or thing we mean; no commas. If the pronoun is the object, you can leave it out.",
        ),
        examples: ["The film (that) we watched was boring.", "People who exercise regularly usually sleep better."],
      },
      {
        text: t(
          "Cheklamaydigan (non-defining) ergash gap qoʻshimcha ma’lumot beradi va vergul bilan ajratiladi. Unda that ishlatilmaydi va olmoshni tushirib boʻlmaydi.",
          "Распространительное (non-defining) придаточное даёт дополнительную информацию и выделяется запятыми. В нём нельзя использовать that и опускать местоимение.",
          "A non-defining clause adds extra information and has commas. You can't use that, and you can't leave the pronoun out.",
        ),
        examples: ["My brother, who lives in Seoul, is a programmer.", "Bukhara, which is over 2,000 years old, attracts many tourists."],
      },
      {
        text: t(
          "Which butun oldingi gapga ham ishora qila oladi — bunda u vergul bilan ajratiladi va «bu esa» ma’nosini beradi.",
          "Which может относиться ко всему предыдущему предложению — тогда оно отделяется запятой и значит «что».",
          "Which can refer to a whole previous clause — then it follows a comma and means 'and this'.",
        ),
        examples: ["He passed the exam, which surprised everyone.", "The train was late, which meant we missed the concert."],
      },
    ],
    mistakes: [
      {
        wrong: "The man which called you is my boss.",
        right: "The man who called you is my boss.",
        note: t(
          "Odamlar uchun who (yoki that) ishlatiladi, which emas.",
          "Для людей используется who (или that), а не which.",
          "Use who (or that) for people, not which.",
        ),
      },
      {
        wrong: "My father, that is a doctor, works a lot.",
        right: "My father, who is a doctor, works a lot.",
        note: t(
          "Vergul bilan ajratilgan qoʻshimcha ma’lumotda that ishlatilmaydi.",
          "В придаточном с запятыми that не используется.",
          "That is not used in non-defining clauses with commas.",
        ),
      },
      {
        wrong: "This is the house which I was born in it.",
        right: "This is the house where I was born.",
        note: t(
          "Nisbiy olmoshdan keyin yana it, him kabi olmoshni takrorlamang.",
          "После относительного местоимения не повторяйте it, him и т. п.",
          "Don't repeat a pronoun (it, him) after the relative pronoun.",
        ),
      },
      {
        wrong: "I met a girl who her father is a pilot.",
        right: "I met a girl whose father is a pilot.",
        note: t(
          "Egalik ma’nosi («otasi uchuvchi boʻlgan qiz») whose bilan ifodalanadi.",
          "Принадлежность («девушка, чей отец — пилот») выражается через whose.",
          "Possession is expressed with whose.",
        ),
      },
    ],
    quiz: [
      {
        prompt: "The student ___ won the prize is from Fergana.",
        options: ["which", "who", "whose"],
        answer: 1,
        why: t(
          "Student — odam, shuning uchun who.",
          "Student — человек, поэтому who.",
          "A student is a person, so who.",
        ),
      },
      {
        prompt: "Samarkand, ___ is famous for the Registan, is my favourite city.",
        options: ["which", "that", "where"],
        answer: 0,
        why: t(
          "Vergulli qoʻshimcha ma’lumot, narsa (shahar) haqida — which; that bu yerda mumkin emas.",
          "Распространительное придаточное о городе — which; that здесь невозможно.",
          "It's a non-defining clause about a thing, so which; that isn't possible with commas.",
        ),
      },
      {
        prompt: "This is the café ___ we first met.",
        options: ["which", "where", "who"],
        answer: 1,
        why: t(
          "Joy haqida gap ketyapti va keyin toʻliq gap keladi — where.",
          "Речь о месте, и дальше идёт полное предложение — where.",
          "It's a place, and a full clause follows, so where.",
        ),
      },
      {
        prompt: "I have a friend ___ father is a pilot.",
        options: ["who", "who's", "whose"],
        answer: 2,
        why: t(
          "«Otasi uchuvchi boʻlgan» — egalik, demak whose.",
          "«Чей отец — пилот» — принадлежность, значит whose.",
          "'Her father' shows possession, so whose.",
        ),
      },
      {
        prompt: "My grandmother, ___ is 85, still works in her garden.",
        options: ["who", "that", "which"],
        answer: 0,
        why: t(
          "Odam haqida vergulli qoʻshimcha ma’lumot — faqat who.",
          "Дополнительная информация о человеке с запятыми — только who.",
          "Extra information about a person, with commas: only who.",
        ),
      },
      {
        prompt: "He forgot my birthday, ___ really upset me.",
        options: ["what", "that", "which"],
        answer: 2,
        why: t(
          "Which butun oldingi gapga ishora qiladi (tugʻilgan kunni unutgani).",
          "Which относится ко всему предыдущему предложению.",
          "Which refers to the whole previous clause.",
        ),
      },
    ],
  },
  {
    id: 'b2-gerunds-infinitives',
    level: 'B2',
    title: t("Gerundiy va infinitiv", "Герундий и инфинитив", "Gerunds vs infinitives"),
    summary: t(
      "Ba’zi fe’llardan keyin -ing shakli (gerundiy), ba’zilaridan keyin esa to + fe’l (infinitiv) keladi. Ayrim fe’llarda tanlov gapning ma’nosini oʻzgartiradi.",
      "После одних глаголов идёт форма на -ing (герундий), после других — to + глагол (инфинитив). У некоторых глаголов выбор меняет смысл.",
      "Some verbs are followed by -ing (gerund), others by to + verb (infinitive). With a few verbs, the choice changes the meaning.",
    ),
    rules: [
      {
        text: t(
          "Gerundiy enjoy, avoid, finish, mind, suggest, consider, keep, practise fe’llaridan keyin, barcha predloglardan keyin va gapning egasi vazifasida ishlatiladi.",
          "Герундий используется после enjoy, avoid, finish, mind, suggest, consider, keep, practise, после любых предлогов и в роли подлежащего.",
          "Use the gerund after enjoy, avoid, finish, mind, suggest, consider, keep, practise, after all prepositions, and as the subject of a sentence.",
        ),
        examples: ["I enjoy reading detective stories.", "She's interested in learning Korean.", "Swimming is good for your back."],
      },
      {
        text: t(
          "To-infinitiv want, decide, hope, plan, agree, refuse, promise, afford fe’llaridan keyin, sifatlardan keyin va maqsadni bildirganda («…ish uchun») ishlatiladi.",
          "Инфинитив с to используется после want, decide, hope, plan, agree, refuse, promise, afford, после прилагательных и для выражения цели («чтобы»).",
          "Use to + infinitive after want, decide, hope, plan, agree, refuse, promise, afford, after adjectives, and to express purpose.",
        ),
        examples: ["We decided to leave early.", "It's difficult to understand him.", "I came to London to study."],
      },
      {
        text: t(
          "Stop, remember, forget, try fe’llarida ma’no oʻzgaradi: stop doing — «…ishni tashlamoq», stop to do — «…ish uchun toʻxtamoq»; remember doing — «…ganini eslamoq», remember to do — «…ishni unutmaslik».",
          "У stop, remember, forget, try значение меняется: stop doing — «перестать делать», stop to do — «остановиться, чтобы сделать»; remember doing — «помнить, что сделал», remember to do — «не забыть сделать».",
          "With stop, remember, forget and try, the meaning changes: stop doing = quit; stop to do = pause in order to do; remember doing = recall the past; remember to do = not forget a task.",
        ),
        examples: ["He stopped smoking last year.", "We stopped to buy some water.", "Remember to lock the door!", "I remember meeting her in 2015."],
      },
      {
        text: t(
          "Make va let dan keyin, shuningdek modal fe’llardan keyin to siz infinitiv keladi.",
          "После make и let, а также после модальных глаголов используется инфинитив без to.",
          "After make, let and modal verbs, use the infinitive without to.",
        ),
        examples: ["My parents let me stay out late.", "The film made me cry."],
      },
    ],
    mistakes: [
      {
        wrong: "I look forward to hear from you.",
        right: "I look forward to hearing from you.",
        note: t(
          "Look forward to iborasidagi to — predlog, undan keyin -ing shakli keladi.",
          "В look forward to частица to — это предлог, после него нужна форма на -ing.",
          "In look forward to, to is a preposition, so it's followed by -ing.",
        ),
      },
      {
        wrong: "I suggest you to take a taxi.",
        right: "I suggest taking a taxi.",
        note: t(
          "Suggest dan keyin kishi + to + fe’l qoʻllanmaydi: suggest doing yoki suggest (that) you do.",
          "После suggest не бывает конструкции «человек + to + глагол»: suggest doing или suggest (that) you do.",
          "Suggest is never followed by person + to-infinitive: say suggest doing or suggest (that) you do.",
        ),
      },
      {
        wrong: "She made me to wait for an hour.",
        right: "She made me wait for an hour.",
        note: t(
          "Make + kishi + to siz fe’l.",
          "Make + человек + глагол без to.",
          "Make + person + verb without to.",
        ),
      },
      {
        wrong: "I'm used to get up early.",
        right: "I'm used to getting up early.",
        note: t(
          "Be used to («…ga odatlanmoq») dagi to — predlog, shuning uchun -ing kerak.",
          "В be used to («привыкнуть к») to — предлог, поэтому нужен -ing.",
          "In be used to, to is a preposition, so use -ing.",
        ),
      },
    ],
    quiz: [
      {
        prompt: "Would you mind ___ the window?",
        options: ["to open", "opening", "open"],
        answer: 1,
        why: t(
          "Mind fe’lidan keyin gerundiy keladi.",
          "После mind используется герундий.",
          "Mind is followed by the gerund.",
        ),
      },
      {
        prompt: "We can't afford ___ a new car this year.",
        options: ["buying", "buy", "to buy"],
        answer: 2,
        why: t(
          "Afford fe’lidan keyin to-infinitiv keladi.",
          "После afford используется инфинитив с to.",
          "Afford is followed by to + infinitive.",
        ),
      },
      {
        prompt: "I was thirsty, so I stopped ___ some water.",
        options: ["to buy", "buying", "buy"],
        answer: 0,
        why: t(
          "Maqsad — suv sotib olish uchun toʻxtadi: stop to do.",
          "Цель — остановился, чтобы купить воды: stop to do.",
          "The purpose of stopping was to buy water: stop to do.",
        ),
      },
      {
        prompt: "She is good at ___ problems.",
        options: ["solve", "to solve", "solving"],
        answer: 2,
        why: t(
          "Predlogdan (at) keyin doim -ing shakli keladi.",
          "После предлога (at) всегда форма на -ing.",
          "After a preposition (at), always use -ing.",
        ),
      },
      {
        prompt: "My boss made me ___ the report again.",
        options: ["rewrite", "to rewrite", "rewriting"],
        answer: 0,
        why: t(
          "Make + kishi + to siz infinitiv.",
          "Make + человек + инфинитив без to.",
          "Make + person + infinitive without to.",
        ),
      },
      {
        prompt: "Don't forget ___ your grandmother on her birthday.",
        options: ["calling", "to call", "call"],
        answer: 1,
        why: t(
          "Kelajakda bajarilishi kerak boʻlgan ish — forget to do.",
          "Действие, которое нужно выполнить, — forget to do.",
          "It's a task to do in the future, so forget to do.",
        ),
      },
    ],
  },
  // ───────────────────────────── C1 ─────────────────────────────
  {
    id: 'c1-third-mixed-conditionals',
    level: 'C1',
    title: t(
      "Uchinchi tur va aralash shart gaplar",
      "Условные предложения третьего типа и смешанные",
      "Third and mixed conditionals",
    ),
    summary: t(
      "Uchinchi tur shart gap oʻtmishdagi amalga oshmagan vaziyatni tasavvur qiladi (afsus, tanqid), aralash shart gaplar esa oʻtmish va hozirgi zamonni bir gapda bogʻlaydi.",
      "Условные третьего типа описывают нереальное прошлое (сожаление, упрёк), а смешанные связывают прошлое и настоящее в одном предложении.",
      "The third conditional imagines a different past (regrets, criticism); mixed conditionals link the past and the present in one sentence.",
    ),
    rules: [
      {
        text: t(
          "Uchinchi tur: If + Past Perfect, would have + V3. Oʻtmishda sodir boʻlmagan voqea va uning xayoliy natijasi.",
          "Третий тип: If + Past Perfect, would have + V3. Нереальное условие в прошлом и его воображаемый результат.",
          "Third conditional: If + past perfect, would have + past participle. An unreal past condition and its imaginary result.",
        ),
        examples: ["If I had left earlier, I wouldn't have missed the train.", "She would have passed if she had revised more."],
      },
      {
        text: t(
          "Aralash shart gap (oʻtmish → hozir): If + Past Perfect, would + fe’l. Oʻtmishdagi holat hozirgi natijaga ta’sir qiladi.",
          "Смешанный тип (прошлое → настоящее): If + Past Perfect, would + глагол. Условие в прошлом влияет на настоящее.",
          "Mixed (past → present): If + past perfect, would + verb. A past condition affects the present.",
        ),
        examples: ["If I had taken that job, I would be rich now.", "If he hadn't broken his leg, he would be playing today."],
      },
      {
        text: t(
          "Aralash shart gap (doimiy holat → oʻtmish): If + Past Simple, would have + V3. Doimiy xususiyat oʻtmishdagi natijani belgilagan.",
          "Смешанный тип (постоянное состояние → прошлое): If + Past Simple, would have + V3. Постоянная черта повлияла на прошлый результат.",
          "Mixed (general state → past): If + past simple, would have + past participle. A permanent situation caused a past result.",
        ),
        examples: ["If I spoke Chinese, I would have understood the guide.", "If she weren't so shy, she would have asked a question."],
      },
      {
        text: t(
          "Would oʻrniga could have yoki might have ishlatilishi mumkin — ehtimollik yoki imkoniyatni bildiradi.",
          "Вместо would можно использовать could have или might have — они выражают возможность или вероятность.",
          "Could have and might have can replace would have to show possibility.",
        ),
        examples: ["If you had asked me, I could have helped.", "If it hadn't rained, we might have won."],
      },
    ],
    mistakes: [
      {
        wrong: "If I would have known, I would have helped.",
        right: "If I had known, I would have helped.",
        note: t(
          "If qismida would have emas, Past Perfect (had + V3) ishlatiladi.",
          "В части с if используется Past Perfect (had + V3), а не would have.",
          "The if-clause takes the past perfect, not would have.",
        ),
      },
      {
        wrong: "If I had studied medicine, I would have been a doctor now.",
        right: "If I had studied medicine, I would be a doctor now.",
        note: t(
          "Now hozirgi natijani koʻrsatadi, shuning uchun aralash tur: would + fe’l.",
          "Now указывает на результат в настоящем, поэтому смешанный тип: would + глагол.",
          "Now shows a present result, so use the mixed form: would + verb.",
        ),
      },
      {
        wrong: "If you had told me, I would help you.",
        right: "If you had told me, I would have helped you.",
        note: t(
          "Natija ham oʻtmishda boʻlsa, would have + V3 kerak.",
          "Если результат тоже в прошлом, нужно would have + V3.",
          "If the result is also in the past, use would have + past participle.",
        ),
      },
    ],
    quiz: [
      {
        prompt: "If we ___ the map, we wouldn't have got lost.",
        options: ["had checked", "checked", "would have checked"],
        answer: 0,
        why: t(
          "Uchinchi tur: if qismida Past Perfect.",
          "Третий тип: в части с if — Past Perfect.",
          "Third conditional: the if-clause needs the past perfect.",
        ),
      },
      {
        prompt: "If she had accepted the offer, she ___ in London now.",
        options: ["would have been", "would be", "had been"],
        answer: 1,
        why: t(
          "Now — hozirgi natija, demak aralash tur: would + fe’l.",
          "Now — результат в настоящем, значит смешанный тип: would + глагол.",
          "Now shows a present result, so we use would + verb.",
        ),
      },
      {
        prompt: "I ___ you if I had seen you.",
        options: ["would greet", "had greeted", "would have greeted"],
        answer: 2,
        why: t(
          "Shart ham, natija ham oʻtmishda: would have + V3.",
          "И условие, и результат в прошлом: would have + V3.",
          "Both the condition and the result are in the past: would have + past participle.",
        ),
      },
      {
        prompt: "If he weren't so stubborn, he ___ our advice last year.",
        options: ["would take", "would have taken", "took"],
        answer: 1,
        why: t(
          "Doimiy xususiyat (hozir ham qaysar) oʻtmishdagi natijaga ta’sir qilgan: would have + V3.",
          "Постоянная черта (он и сейчас упрям) повлияла на прошлое: would have + V3.",
          "A permanent trait affected a past result: would have + past participle.",
        ),
      },
      {
        prompt: "If it hadn't rained, we ___ the match.",
        options: ["might have won", "might win", "had won"],
        answer: 0,
        why: t(
          "Oʻtmishdagi ehtimoliy natija — might have + V3.",
          "Возможный результат в прошлом — might have + V3.",
          "A possible past result is might have + past participle.",
        ),
      },
      {
        prompt: "If I ___ more confident, I would have spoken at the meeting.",
        options: ["had", "am", "were"],
        answer: 2,
        why: t(
          "Doimiy holat (umuman ishonchsizman) — if qismida Past Simple: were.",
          "Постоянное состояние (я вообще неуверен) — в части с if Past Simple: were.",
          "A general state (I'm not a confident person) uses the past simple: were.",
        ),
      },
    ],
  },
  {
    id: 'c1-inversion',
    level: 'C1',
    title: t("Ta’kid uchun inversiya", "Инверсия для усиления", "Inversion for emphasis"),
    summary: t(
      "Inkor yoki cheklov ma’nosidagi ravish gap boshiga chiqsa, ega va yordamchi fe’l oʻrin almashadi. Bu usul nutqni ta’sirchan va rasmiy qiladi — esse, nutq va rasmiy xatlarda qoʻl keladi.",
      "Если в начало предложения выносится отрицательное или ограничительное наречие, подлежащее и вспомогательный глагол меняются местами. Это делает речь выразительной и официальной — полезно в эссе, выступлениях и деловых письмах.",
      "When a negative or limiting adverbial starts a sentence, the subject and auxiliary swap places. It makes your language emphatic and formal — useful in essays, speeches and formal letters.",
    ),
    rules: [
      {
        text: t(
          "Never, rarely, seldom, hardly ever, not only, under no circumstances, at no time bilan gap boshlansa: ravish + yordamchi fe’l + ega + asosiy fe’l. Yordamchi fe’l boʻlmasa, do/does/did qoʻshiladi.",
          "Если предложение начинается с never, rarely, seldom, hardly ever, not only, under no circumstances, at no time: наречие + вспомогательный глагол + подлежащее + основной глагол. Если вспомогательного глагола нет, добавляется do/does/did.",
          "After never, rarely, seldom, hardly ever, not only, under no circumstances, at no time: adverbial + auxiliary + subject + main verb. If there is no auxiliary, add do/does/did.",
        ),
        examples: ["Never have I seen such a beautiful sunset.", "Not only did she win, but she also broke the record.", "Under no circumstances should you share your password."],
      },
      {
        text: t(
          "Hardly / Scarcely … when va No sooner … than — «…gan zahoti». Odatda Past Perfect bilan ishlatiladi.",
          "Hardly / Scarcely … when и No sooner … than — «едва… как». Обычно употребляются с Past Perfect.",
          "Hardly / Scarcely … when and No sooner … than mean 'just after'. They usually take the past perfect.",
        ),
        examples: ["Hardly had we arrived when it started to rain.", "No sooner had I sat down than the phone rang."],
      },
      {
        text: t(
          "Only when, only after, only then, not until bilan boshlangan gapda inversiya bosh gapda boʻladi.",
          "В предложениях с only when, only after, only then, not until инверсия происходит в главной части.",
          "With only when, only after, only then and not until, the inversion happens in the main clause.",
        ),
        examples: ["Only when I moved abroad did I appreciate my mother's cooking.", "Not until the end of the film did we understand the plot."],
      },
      {
        text: t(
          "Rasmiy shart gaplarda if tushib qolishi mumkin: Had I known… , Should you need… , Were I in your position…",
          "В официальных условных предложениях if можно опустить: Had I known… , Should you need… , Were I in your position…",
          "In formal conditionals, if can be dropped: Had I known… , Should you need… , Were I in your position…",
        ),
        examples: ["Had I known about the problem, I would have called you.", "Should you need any help, please contact us.", "Were I in your position, I would accept the offer."],
      },
    ],
    mistakes: [
      {
        wrong: "Never I have seen such chaos.",
        right: "Never have I seen such chaos.",
        note: t(
          "Never gap boshida kelsa, yordamchi fe’l egadan oldin turadi.",
          "Если never стоит в начале, вспомогательный глагол ставится перед подлежащим.",
          "When never starts the sentence, the auxiliary comes before the subject.",
        ),
      },
      {
        wrong: "Not only she speaks French, but also German.",
        right: "Not only does she speak French, but she also speaks German.",
        note: t(
          "Not only dan keyin do/does/did qoʻshiladi va soʻroq tartibi ishlatiladi.",
          "После not only добавляется do/does/did и используется вопросительный порядок слов.",
          "After not only, add do/does/did and use question word order.",
        ),
      },
      {
        wrong: "Only when I left home I understood it.",
        right: "Only when I left home did I understand it.",
        note: t(
          "Only when dan keyin inversiya ikkinchi — bosh gapda boʻladi.",
          "После only when инверсия происходит во второй, главной части.",
          "After only when, the inversion is in the main clause.",
        ),
      },
      {
        wrong: "No sooner had he arrived when the meeting started.",
        right: "No sooner had he arrived than the meeting started.",
        note: t(
          "No sooner … than, hardly … when — juftliklarni aralashtirmang.",
          "No sooner … than, hardly … when — не путайте пары.",
          "It's no sooner … than and hardly … when — don't mix the pairs.",
        ),
      },
    ],
    quiz: [
      {
        prompt: "Rarely ___ such a talented student.",
        options: ["I have met", "have I met", "I met"],
        answer: 1,
        why: t(
          "Rarely gap boshida — inversiya: have I met.",
          "Rarely в начале — инверсия: have I met.",
          "Rarely at the start triggers inversion: have I met.",
        ),
      },
      {
        prompt: "Not only ___ late, but he also forgot the tickets.",
        options: ["was he", "he was", "he has"],
        answer: 0,
        why: t(
          "Not only dan keyin yordamchi fe’l egadan oldin keladi: was he.",
          "После not only вспомогательный глагол стоит перед подлежащим: was he.",
          "After not only, the auxiliary goes before the subject: was he.",
        ),
      },
      {
        prompt: "No sooner had the film started ___ the lights went out.",
        options: ["when", "that", "than"],
        answer: 2,
        why: t(
          "No sooner doim than bilan juftlashadi.",
          "No sooner всегда сочетается с than.",
          "No sooner always pairs with than.",
        ),
      },
      {
        prompt: "___ you have any questions, please do not hesitate to contact us.",
        options: ["Should", "Would", "Had"],
        answer: 0,
        why: t(
          "Should you… = If you… — rasmiy shart gap.",
          "Should you… = If you… — официальное условие.",
          "Should you… means If you… in formal style.",
        ),
      },
      {
        prompt: "Only after the meeting ___ the truth.",
        options: ["we learned", "did we learn", "we did learn"],
        answer: 1,
        why: t(
          "Only after … dan keyin bosh gapda inversiya: did we learn.",
          "После only after … в главной части инверсия: did we learn.",
          "After only after …, the main clause is inverted: did we learn.",
        ),
      },
      {
        prompt: "Hardly ___ the door when the phone rang.",
        options: ["I had opened", "did I open", "had I opened"],
        answer: 2,
        why: t(
          "Hardly … when qurilishida Past Perfect va inversiya: had I opened.",
          "В конструкции hardly … when — Past Perfect и инверсия: had I opened.",
          "Hardly … when takes the past perfect with inversion: had I opened.",
        ),
      },
    ],
  },
  {
    id: 'c1-linking-cohesion',
    level: 'C1',
    title: t(
      "Murakkab bogʻlovchi vositalar va matn izchilligi",
      "Связующие средства и связность текста",
      "Advanced linking and cohesion",
    ),
    summary: t(
      "Bogʻlovchi soʻzlar fikrlar orasidagi mantiqiy aloqani — zidlik, shart, sabab-natija va qoʻshimchani koʻrsatadi. Ularni toʻgʻri tanlash IELTS va CEFR imtihonlarida yuqori ball olishning muhim sharti.",
      "Связующие слова показывают логические отношения между идеями — противопоставление, условие, причину и следствие, добавление. Умение правильно их выбирать — важное условие высокого балла на IELTS и CEFR.",
      "Linking words show how ideas relate — contrast, condition, cause and result, addition. Using them correctly is key to a high score in IELTS and CEFR exams.",
    ),
    rules: [
      {
        text: t(
          "Zidlik: whereas / while ikki gapni solishtiradi; although / even though + gap; despite / in spite of + ot yoki -ing; however / nevertheless yangi gap boshida, vergul bilan.",
          "Противопоставление: whereas / while сравнивают две части; although / even though + предложение; despite / in spite of + существительное или -ing; however / nevertheless — в начале нового предложения, с запятой.",
          "Contrast: whereas / while compare two clauses; although / even though + clause; despite / in spite of + noun or -ing; however / nevertheless start a new sentence, followed by a comma.",
        ),
        examples: ["Some people love big cities, whereas others prefer villages.", "Despite feeling ill, she went to work.", "The plan was risky. Nevertheless, we decided to try it."],
      },
      {
        text: t(
          "Shart: provided (that), providing, as long as, on condition that — «…sharti bilan». In case — «ehtimol … boʻlsa» (ehtiyot chorasi), u if bilan bir xil emas.",
          "Условие: provided (that), providing, as long as, on condition that — «при условии, что». In case — «на случай, если» (мера предосторожности), это не то же самое, что if.",
          "Condition: provided (that), providing, as long as, on condition that mean 'only if'. In case means 'as a precaution' — it is not the same as if.",
        ),
        examples: ["You can borrow my car provided that you return it by six.", "Take an umbrella in case it rains."],
      },
      {
        text: t(
          "Sabab va natija: due to / owing to + ot; consequently, therefore, as a result — natija. Qoʻshimcha: moreover, furthermore, in addition.",
          "Причина и следствие: due to / owing to + существительное; consequently, therefore, as a result — следствие. Добавление: moreover, furthermore, in addition.",
          "Cause and result: due to / owing to + noun; consequently, therefore, as a result for results. Addition: moreover, furthermore, in addition.",
        ),
        examples: ["The flight was cancelled due to fog.", "Prices rose sharply; consequently, sales fell.", "The course is cheap. Moreover, it's completely online."],
      },
      {
        text: t(
          "Izchillik uchun takrordan qoching: this / these + ot, the former / the latter, do so, such kabi vositalar oldingi fikrga ishora qiladi.",
          "Для связности избегайте повторов: this / these + существительное, the former / the latter, do so, such отсылают к уже сказанному.",
          "For cohesion, avoid repetition: this / these + noun, the former / the latter, do so and such refer back to earlier ideas.",
        ),
        examples: ["Many students work part-time. This affects their grades.", "Tea and coffee are both popular; the latter contains more caffeine."],
      },
    ],
    mistakes: [
      {
        wrong: "Although he was tired, but he continued.",
        right: "Although he was tired, he continued.",
        note: t(
          "Oʻzbekcha «garchi…, lekin» yoki ruscha «хотя…, но» kabi ikki bogʻlovchini birga ishlatmang: although yoki but — faqat bittasi.",
          "Не копируйте русское «хотя…, но»: в английском нужен либо although, либо but.",
          "Use either although or but — never both in one sentence.",
        ),
      },
      {
        wrong: "Despite it was raining, we went out.",
        right: "Despite the rain, we went out.",
        note: t(
          "Despite dan keyin toʻliq gap emas, ot yoki -ing keladi. Gap uchun although ishlating.",
          "После despite идёт существительное или -ing, а не полное предложение. Для предложения используйте although.",
          "Despite is followed by a noun or -ing, not a full clause. Use although for a clause.",
        ),
      },
      {
        wrong: "However the price is high, many people buy it.",
        right: "Although the price is high, many people buy it.",
        note: t(
          "However ikki gapni bogʻlamaydi — u yangi gap boshida keladi. Bitta gap ichida although kerak.",
          "However не соединяет две части в одном предложении — оно начинает новое. Внутри предложения нужно although.",
          "However doesn't join two clauses; it starts a new sentence. Use although within one sentence.",
        ),
      },
      {
        wrong: "In case it rains, the match will be cancelled.",
        right: "If it rains, the match will be cancelled.",
        note: t(
          "In case — ehtiyot chorasi uchun («har ehtimolga qarshi»), if esa shart uchun.",
          "In case — «на всякий случай», а для условия нужно if.",
          "In case is for precautions; use if for a condition.",
        ),
      },
    ],
    quiz: [
      {
        prompt: "___ the heavy traffic, we arrived on time.",
        options: ["Although", "Despite", "However"],
        answer: 1,
        why: t(
          "Keyin ot birikmasi (the heavy traffic) keladi, shuning uchun despite.",
          "Дальше идёт существительное (the heavy traffic), поэтому despite.",
          "A noun phrase follows, so despite.",
        ),
      },
      {
        prompt: "You can join the team ___ you attend every training session.",
        options: ["provided that", "in case", "whereas"],
        answer: 0,
        why: t(
          "«…sharti bilan» — provided that.",
          "«При условии, что» — provided that.",
          "The meaning is 'only if', so provided that.",
        ),
      },
      {
        prompt: "My brother is very outgoing, ___ I'm rather shy.",
        options: ["therefore", "despite", "whereas"],
        answer: 2,
        why: t(
          "Ikki odam solishtirilmoqda — whereas.",
          "Сравниваются два человека — whereas.",
          "Two people are being contrasted, so whereas.",
        ),
      },
      {
        prompt: "The evidence was weak. ___, the jury found him guilty.",
        options: ["Although", "Nevertheless", "Despite"],
        answer: 1,
        why: t(
          "Yangi gap boshida, vergul bilan zidlik — nevertheless.",
          "Противопоставление в начале нового предложения с запятой — nevertheless.",
          "Contrast at the start of a new sentence, with a comma: nevertheless.",
        ),
      },
      {
        prompt: "Take some cash ___ the card machine isn't working.",
        options: ["in case", "provided", "unless"],
        answer: 0,
        why: t(
          "Ehtiyot chorasi — in case.",
          "Мера предосторожности — in case.",
          "It's a precaution, so in case.",
        ),
      },
      {
        prompt: "The factory closed ___ a lack of funding.",
        options: ["because", "consequently", "owing to"],
        answer: 2,
        why: t(
          "Sabab ot bilan berilgan (a lack of funding) — owing to.",
          "Причина выражена существительным (a lack of funding) — owing to.",
          "The reason is a noun phrase, so owing to.",
        ),
      },
    ],
  },
  {
    id: 'c1-wish-if-only',
    level: 'C1',
    title: t(
      "Afsus va istak: wish, if only, would rather",
      "Сожаление и желание: wish, if only, would rather",
      "Wish, if only and would rather",
    ),
    summary: t(
      "Bu qurilmalar hozirgi holatdan norozilik, oʻtmishdagi xatolar uchun afsus va afzal koʻrishni ifodalaydi. Ulardan keyingi zamon doim bir pogʻona «orqada» turadi.",
      "Эти конструкции выражают недовольство настоящим, сожаление о прошлом и предпочтения. Время после них всегда сдвинуто на шаг назад.",
      "These structures express wishes about the present, regrets about the past and preferences. The verb after them is always one step 'back' in time.",
    ),
    rules: [
      {
        text: t(
          "Wish / if only + Past Simple — hozirgi holat boshqacha boʻlishini istash. To be fe’li koʻpincha barcha shaxslarda were shaklida boʻladi.",
          "Wish / if only + Past Simple — желание, чтобы настоящее было другим. Глагол to be часто имеет форму were для всех лиц.",
          "Wish / if only + past simple: you want the present to be different. Be often becomes were for all persons.",
        ),
        examples: ["I wish I lived closer to the sea.", "If only I knew her number!", "I wish I were taller."],
      },
      {
        text: t(
          "Wish / if only + Past Perfect — oʻtmishdagi ish uchun afsus.",
          "Wish / if only + Past Perfect — сожаление о прошлом.",
          "Wish / if only + past perfect: regret about the past.",
        ),
        examples: ["I wish I had studied harder at school.", "If only we hadn't sold the house."],
      },
      {
        text: t(
          "Wish + would — boshqa odamning xatti-harakatidan norozilik yoki biror narsa oʻzgarishini xohlash. I wish I would… deyilmaydi.",
          "Wish + would — раздражение из-за чужого поведения или желание, чтобы что-то изменилось. I wish I would… не говорят.",
          "Wish + would: annoyance at someone's behaviour or a wish for change. We don't say I wish I would…",
        ),
        examples: ["I wish you would stop interrupting me.", "I wish it would stop raining."],
      },
      {
        text: t(
          "Would rather + fe’l — oʻz afzalligi; would rather + boshqa shaxs + Past Simple — boshqa odamdan istak. It’s (high) time + Past Simple — «…vaqti keldi».",
          "Would rather + глагол — собственное предпочтение; would rather + другое лицо + Past Simple — желание в отношении другого человека. It's (high) time + Past Simple — «давно пора».",
          "Would rather + verb for your own preference; would rather + another person + past simple for others. It's (high) time + past simple means 'it should happen now'.",
        ),
        examples: ["I'd rather stay at home tonight.", "I'd rather you didn't smoke here.", "It's time we went home."],
      },
    ],
    mistakes: [
      {
        wrong: "I wish I have more free time.",
        right: "I wish I had more free time.",
        note: t(
          "Hozirgi istak uchun wish dan keyin Past Simple ishlatiladi.",
          "Для желания о настоящем после wish используется Past Simple.",
          "For present wishes, use the past simple after wish.",
        ),
      },
      {
        wrong: "I wish I would be rich.",
        right: "I wish I were rich.",
        note: t(
          "Oʻzingiz haqida wish + would ishlatilmaydi; holat uchun were keladi.",
          "О себе wish + would не говорят; для состояния используется were.",
          "Don't use wish + would about yourself; use were for states.",
        ),
      },
      {
        wrong: "I'd rather you don't tell anyone.",
        right: "I'd rather you didn't tell anyone.",
        note: t(
          "Would rather + boshqa shaxs dan keyin Past Simple keladi.",
          "После would rather + другое лицо нужен Past Simple.",
          "After would rather + another person, use the past simple.",
        ),
      },
      {
        wrong: "I wish I didn't eat so much yesterday.",
        right: "I wish I hadn't eaten so much yesterday.",
        note: t(
          "Oʻtmish uchun afsus — Past Perfect.",
          "Сожаление о прошлом — Past Perfect.",
          "Regret about the past needs the past perfect.",
        ),
      },
    ],
    quiz: [
      {
        prompt: "I wish I ___ how to play the guitar.",
        options: ["know", "knew", "would know"],
        answer: 1,
        why: t(
          "Hozirgi holat haqida istak — wish + Past Simple.",
          "Желание о настоящем — wish + Past Simple.",
          "A wish about the present uses the past simple.",
        ),
      },
      {
        prompt: "If only I ___ to his advice last year!",
        options: ["had listened", "listened", "would listen"],
        answer: 0,
        why: t(
          "Last year — oʻtmish uchun afsus, shuning uchun Past Perfect.",
          "Last year — сожаление о прошлом, поэтому Past Perfect.",
          "Last year shows regret about the past, so the past perfect.",
        ),
      },
      {
        prompt: "I wish it ___ raining — we want to go for a walk.",
        options: ["would stop", "stops", "will stop"],
        answer: 0,
        why: t(
          "Vaziyat oʻzgarishini xohlash — wish + would.",
          "Желание, чтобы ситуация изменилась, — wish + would.",
          "Wanting a situation to change uses wish + would.",
        ),
      },
      {
        prompt: "I'd rather you ___ my phone without asking.",
        options: ["don't use", "didn't use", "not use"],
        answer: 1,
        why: t(
          "Would rather + boshqa shaxs + Past Simple.",
          "Would rather + другое лицо + Past Simple.",
          "Would rather + another person + past simple.",
        ),
      },
      {
        prompt: "It's high time we ___ a decision.",
        options: ["will make", "make", "made"],
        answer: 2,
        why: t(
          "It’s high time dan keyin Past Simple ishlatiladi.",
          "После It's high time используется Past Simple.",
          "It's high time is followed by the past simple.",
        ),
      },
      {
        prompt: "She wishes she ___ that expensive dress — now she has no money.",
        options: ["didn't buy", "wouldn't buy", "hadn't bought"],
        answer: 2,
        why: t(
          "Koʻylak allaqachon sotib olingan — oʻtmish uchun afsus: Past Perfect.",
          "Платье уже куплено — сожаление о прошлом: Past Perfect.",
          "She already bought it, so regret about the past: past perfect.",
        ),
      },
    ],
  },
];
