// "Ona tili tuzoqlari": rus va o'zbek tilida so'zlashuvchilarning tipik xatolari — soxta do'stlar
// (o'xshash, lekin ma'nosi boshqa so'zlar) va kalkalar (ona tilidan so'zma-so'z ko'chirilgan iboralar).
// Ilovaning o'z materiali; AI ishlatilmaydi. options[0] — to'g'ri javob (ko'rsatishda aralashtiriladi).
// cat — "Mening xatolarim" bo'limidagi xato turi (MistakeAnalyzer.cs bilan bir xil ID'lar).

export type TrapCategory = 'articles' | 'prepositions' | 'agreement' | 'tense' | 'plural' | 'word_choice';

export interface TrapItem {
  id: string;
  /** Gap: "___" o'rniga variantlardan biri qo'yiladi. */
  q: string;
  /** options[0] — to'g'ri javob. */
  options: string[];
  cat: TrapCategory;
  /** Ona tilidagi "aybdor" so'z yoki ibora (izohda ko'rsatiladi). */
  l1: string;
  explain: { uz: string; ru: string; en: string };
}

export interface TrapSet {
  id: 'false-friends' | 'prepositions' | 'grammar';
  emoji: string;
  items: TrapItem[];
}

export const TRAP_SETS: TrapSet[] = [
  {
    id: 'false-friends',
    emoji: '🎭',
    items: [
      { id: 'ff-magazine', q: 'I bought bread and milk at the ___ near my house.', options: ['shop', 'magazine'], cat: 'word_choice', l1: 'RU магазин · UZ magazin',
        explain: { uz: '“Magazine” — jurnal. Doʻkon — “shop” yoki “store”.', ru: '«Magazine» — это журнал. Магазин — «shop» или «store».', en: '“Magazine” means a journal you read. A place to buy things is a “shop” or “store”.' } },
      { id: 'ff-actual', q: 'Climate change is a very ___ topic today.', options: ['relevant', 'actual'], cat: 'word_choice', l1: 'RU актуальный · UZ aktual',
        explain: { uz: '“Actual” — haqiqiy, real. “Dolzarb” maʼnosida — “relevant” yoki “topical”.', ru: '«Actual» — настоящий, реальный. «Актуальный» — «relevant» или «topical».', en: '“Actual” means real. For “important right now” use “relevant” or “topical”.' } },
      { id: 'ff-sympathetic', q: 'Our new neighbour is very ___ — tall, with a kind smile.', options: ['good-looking', 'sympathetic'], cat: 'word_choice', l1: 'RU симпатичный · UZ simpatik',
        explain: { uz: '“Sympathetic” — hamdard, tushunadigan. Chiroyli koʻrinish — “good-looking”, “nice-looking”.', ru: '«Sympathetic» — сочувствующий. Симпатичный — «good-looking», «nice-looking».', en: '“Sympathetic” means caring about someone’s problems. For appearance use “good-looking”.' } },
      { id: 'ff-fabric', q: 'My father works at a car ___ outside the city.', options: ['factory', 'fabric'], cat: 'word_choice', l1: 'RU фабрика · UZ fabrika',
        explain: { uz: '“Fabric” — mato, gazlama. Zavod, fabrika — “factory”.', ru: '«Fabric» — ткань. Фабрика, завод — «factory».', en: '“Fabric” is cloth. A place where things are made is a “factory”.' } },
      { id: 'ff-accurate', q: 'Her room is always very ___: everything is in its place.', options: ['tidy', 'accurate'], cat: 'word_choice', l1: 'RU аккуратный · UZ akkurat',
        explain: { uz: '“Accurate” — aniq, xatosiz (accurate numbers). Ozoda, tartibli — “tidy”, “neat”.', ru: '«Accurate» — точный. Аккуратный — «tidy», «neat».', en: '“Accurate” means correct and exact. For a clean, ordered room use “tidy” or “neat”.' } },
      { id: 'ff-artist', q: 'He is a famous theatre ___ who has played Hamlet many times.', options: ['actor', 'artist'], cat: 'word_choice', l1: 'RU артист · UZ artist',
        explain: { uz: 'Inglizcha “artist” koʻpincha rassom. Sahnada oʻynaydigan — “actor”, qoʻshiqchi — “singer”, umuman — “performer”.', ru: '«Artist» чаще всего художник. Артист театра — «actor», певец — «singer», в целом — «performer».', en: 'An “artist” is usually a painter. On stage: “actor”, “singer” or “performer”.' } },
      { id: 'ff-decade', q: 'A ___ is ten years — for example, from 2010 to 2020.', options: ['decade', 'fortnight'], cat: 'word_choice', l1: 'RU декада (10 дней)',
        explain: { uz: '“Decade” — oʻn yil (rus tilidagi “декада” — oʻn kun).', ru: '«Decade» — десять лет, а не десять дней, как «декада».', en: 'A “decade” is ten years, not ten days as Russian «декада».' } },
      { id: 'ff-data', q: 'What’s the ___ today? — It’s the 20th of November.', options: ['date', 'data'], cat: 'word_choice', l1: 'RU дата · UZ sana',
        explain: { uz: '“Data” — maʼlumotlar. Sana — “date”.', ru: '«Data» — данные. Дата — «date».', en: '“Data” means information. A day of the month is a “date”.' } },
      { id: 'ff-family', q: 'What’s your ___? — Karimov.', options: ['surname', 'family'], cat: 'word_choice', l1: 'RU фамилия · UZ familiya',
        explain: { uz: '“Family” — oila. Familiya — “surname” yoki “last name”.', ru: '«Family» — семья. Фамилия — «surname» или «last name».', en: '“Family” means your relatives. Your last name is your “surname”.' } },
      { id: 'ff-complexion', q: 'He has a strong ___ — broad shoulders and big arms.', options: ['build', 'complexion'], cat: 'word_choice', l1: 'RU комплекция',
        explain: { uz: '“Complexion” — yuz rangi (fair complexion). Gavda, qomat — “build”.', ru: '«Complexion» — цвет лица. Комплекция — «build».', en: '“Complexion” is the colour of your face. Body size and shape is your “build”.' } },
      { id: 'ff-prospect', q: 'We live on a wide ___ in the city centre.', options: ['avenue', 'prospect'], cat: 'word_choice', l1: 'RU проспект · UZ prospekt',
        explain: { uz: '“Prospect” — istiqbol, imkoniyat (job prospects). Keng koʻcha — “avenue”.', ru: '«Prospect» — перспектива. Проспект — «avenue».', en: '“Prospect” means a chance in the future. A wide city street is an “avenue”.' } },
      { id: 'ff-cabinet', q: 'The director is in his ___ on the third floor.', options: ['office', 'cabinet'], cat: 'word_choice', l1: 'RU кабинет · UZ kabinet',
        explain: { uz: '“Cabinet” — shkaf yoki hukumat (vazirlar mahkamasi). Ish xonasi — “office”.', ru: '«Cabinet» — шкаф или кабинет министров. Рабочий кабинет — «office».', en: 'A “cabinet” is a cupboard or a group of ministers. A room you work in is an “office”.' } },
      { id: 'ff-replica', q: 'During the meeting she made a short ___ about the budget.', options: ['remark', 'replica'], cat: 'word_choice', l1: 'RU реплика · UZ replika',
        explain: { uz: '“Replica” — nusxa, kopiya (a replica of a ship). Qisqa gap, fikr — “remark”, “comment”.', ru: '«Replica» — копия. Реплика — «remark» или «comment».', en: 'A “replica” is a copy. A short thing you say is a “remark” or “comment”.' } },
      { id: 'ff-lunatic', q: 'My brother walks at night in his sleep — he is a ___.', options: ['sleepwalker', 'lunatic'], cat: 'word_choice', l1: 'RU лунатик',
        explain: { uz: '“Lunatic” — telba, aqldan ozgan (haqoratli soʻz). Uyqusida yuradigan — “sleepwalker”.', ru: '«Lunatic» — сумасшедший (грубо). Лунатик — «sleepwalker».', en: '“Lunatic” is a rude word for a mad person. Someone who walks in their sleep is a “sleepwalker”.' } },
      { id: 'ff-aspirant', q: 'She is a ___ at the university and is writing her dissertation.', options: ['PhD student', 'aspirant'], cat: 'word_choice', l1: 'RU аспирант · UZ aspirant',
        explain: { uz: '“Aspirant” — biror lavozimga daʼvogar (kam ishlatiladi). Aspirant — “PhD student” yoki “postgraduate”.', ru: '«Aspirant» — претендент (редкое слово). Аспирант — «PhD student» или «postgraduate».', en: '“Aspirant” means someone hoping for a position (rare). A doctoral student is a “PhD student”.' } },
    ],
  },
  {
    id: 'prepositions',
    emoji: '📍',
    items: [
      { id: 'pr-explain', q: 'Can you explain this rule ___ again?', options: ['to me', 'me'], cat: 'prepositions', l1: 'RU объясни мне · UZ menga tushuntir',
        explain: { uz: '“Explain” dan keyin odam “to” bilan keladi: explain something to someone.', ru: 'После «explain» человек идёт с «to»: explain something to someone.', en: 'The person after “explain” needs “to”: explain something to someone.' } },
      { id: 'pr-married', q: 'She is married ___ a doctor.', options: ['to', 'with'], cat: 'prepositions', l1: 'RU замужем за · UZ bilan turmush qurgan',
        explain: { uz: '“Married to someone” — “with” emas.', ru: '«Married to someone», не «with».', en: 'We say “married to someone”, not “with”.' } },
      { id: 'pr-listen', q: 'In the evening I like listening ___ music.', options: ['to', 'with'], cat: 'prepositions', l1: 'RU слушать музыку · UZ musiqa tinglash',
        explain: { uz: '“Listen” dan keyin doim “to”: listen to music, listen to me.', ru: 'После «listen» всегда «to»: listen to music, listen to me.', en: '“Listen” always needs “to”: listen to music, listen to me.' } },
      { id: 'pr-discuss', q: 'Let’s discuss ___ the plan tomorrow.', options: ['(nothing)', 'about'], cat: 'prepositions', l1: 'RU обсудить о · UZ … haqida muhokama',
        explain: { uz: '“Discuss” predlogsiz: discuss the plan. (Lekin “talk about the plan”.)', ru: '«Discuss» без предлога: discuss the plan. (Но «talk about the plan».)', en: '“Discuss” takes no preposition: discuss the plan (but “talk about the plan”).' } },
      { id: 'pr-depend', q: 'Our trip depends ___ the weather.', options: ['on', 'from'], cat: 'prepositions', l1: 'RU зависит от · UZ …ga bogʻliq',
        explain: { uz: '“Depend on” — “from” emas.', ru: '«Depend on», а не «from» (хотя «зависит от»).', en: 'It is always “depend on”, never “depend from”.' } },
      { id: 'pr-internet', q: 'I found this information ___ the internet.', options: ['on', 'in'], cat: 'prepositions', l1: 'RU в интернете · UZ internetda',
        explain: { uz: 'Inglizchada “on the internet”, “on a website”.', ru: 'По-английски «on the internet», «on a website».', en: 'English uses “on the internet” and “on a website”.' } },
      { id: 'pr-home', q: 'I came ___ late yesterday.', options: ['home', 'to home'], cat: 'prepositions', l1: 'RU пришёл домой · UZ uyga keldim',
        explain: { uz: '“Home” harakat feʼllaridan keyin predlogsiz: come home, go home.', ru: 'С глаголами движения «home» без предлога: come home, go home.', en: 'After verbs of movement “home” has no preposition: come home, go home.' } },
      { id: 'pr-angry', q: 'Why are you angry ___ me?', options: ['with', 'on'], cat: 'prepositions', l1: 'RU злишься на меня · UZ mendan jahling chiqdimi',
        explain: { uz: 'Odamga nisbatan: angry with someone; narsaga: angry about something.', ru: 'На человека — angry with someone; из-за чего-то — angry about something.', en: 'Angry with a person, angry about a thing.' } },
      { id: 'pr-congrats', q: 'Congratulations ___ passing your exam!', options: ['on', 'with'], cat: 'prepositions', l1: 'RU поздравляю с · UZ … bilan tabriklayman',
        explain: { uz: '“Congratulations on…” — “with” emas.', ru: '«Congratulations on…», а не «with».', en: 'It is “congratulations on something”.' } },
      { id: 'pr-afraid', q: 'My little sister is afraid ___ dogs.', options: ['of', 'from'], cat: 'prepositions', l1: 'RU боится собак · UZ itlardan qoʻrqadi',
        explain: { uz: 'Oʻzbekchadagi “-dan” ga qaramang: afraid of something.', ru: 'После «afraid» — «of»: afraid of dogs.', en: '“Afraid” takes “of”: afraid of dogs.' } },
    ],
  },
  {
    id: 'grammar',
    emoji: '🧩',
    items: [
      { id: 'gr-agree', q: 'I ___ with your idea.', options: ['agree', 'am agree'], cat: 'word_choice', l1: 'RU я согласен · UZ men roziman',
        explain: { uz: '“Agree” — feʼl, “am” kerak emas: I agree.', ru: '«Agree» — глагол, «am» не нужен: I agree.', en: '“Agree” is a verb, so no “am”: I agree.' } },
      { id: 'gr-age', q: 'My brother ___ twenty years old.', options: ['is', 'has'], cat: 'word_choice', l1: 'RU ему 20 лет · UZ u 20 yoshda',
        explain: { uz: 'Yosh “be” bilan aytiladi: He is twenty (years old).', ru: 'Возраст — с «be»: He is twenty (years old).', en: 'Age uses “be”: he is twenty (years old).' } },
      { id: 'gr-photo', q: 'We ___ a lot of photos in the mountains.', options: ['took', 'made'], cat: 'word_choice', l1: 'RU сделали фото · UZ rasmga tushdik',
        explain: { uz: 'Rasm “take” bilan: take a photo.', ru: 'Фото «take», а не «make»: take a photo.', en: 'We “take” photos, not “make” them.' } },
      { id: 'gr-called', q: '___ is this called in English?', options: ['What', 'How'], cat: 'word_choice', l1: 'RU как это называется · UZ bu nima deyiladi',
        explain: { uz: '“What is it called?” — “How” emas.', ru: '«What is it called?», а не «How».', en: 'Ask “What is it called?”, not “How is it called?”.' } },
      { id: 'gr-light', q: 'It’s dark — please ___ the light.', options: ['turn on', 'open'], cat: 'word_choice', l1: 'RU открой свет (разг.) · UZ chiroqni yoq',
        explain: { uz: 'Chiroq, televizor “turn on / turn off” qilinadi.', ru: 'Свет и технику «turn on / turn off».', en: 'Lights and devices are turned on or off.' } },
      { id: 'gr-interested', q: 'I am very ___ in history.', options: ['interested', 'interesting'], cat: 'word_choice', l1: 'RU мне интересно · UZ menga qiziq',
        explain: { uz: '“Interested” — odamning his-tuygʻusi; “interesting” — narsa qiziqarli: History is interesting.', ru: '«Interested» — что чувствует человек; «interesting» — какая вещь: History is interesting.', en: '“Interested” is how you feel; “interesting” describes the thing.' } },
      { id: 'gr-told', q: 'He ___ me that he was tired.', options: ['told', 'said'], cat: 'word_choice', l1: 'RU он сказал мне · UZ u menga aytdi',
        explain: { uz: '“Tell” dan keyin odam keladi: told me; “say” dan keyin — yoʻq: said that…', ru: 'После «tell» нужен человек: told me; после «say» — нет: said that…', en: '“Tell” needs a person (told me); “say” does not (said that…).' } },
      { id: 'gr-work', q: 'Every morning I go to ___ by bus.', options: ['work', 'job'], cat: 'word_choice', l1: 'RU иду на работу · UZ ishga boraman',
        explain: { uz: 'Joy sifatida “work”: go to work. “Job” — lavozim: I have a new job.', ru: 'Как место — «work»: go to work. «Job» — должность: a new job.', en: 'As a place it is “work”: go to work. A “job” is your position.' } },
      { id: 'gr-engineer', q: 'My mother is ___ engineer.', options: ['an', '(nothing)'], cat: 'articles', l1: 'RU/UZ: artikl yoʻq',
        explain: { uz: 'Kasbdan oldin artikl kerak: She is an engineer, he is a teacher.', ru: 'Перед профессией нужен артикль: She is an engineer.', en: 'Jobs need an article: she is an engineer, he is a teacher.' } },
      { id: 'gr-since', q: 'I ___ in Tashkent since 2020.', options: ['have lived', 'live'], cat: 'tense', l1: 'RU живу с 2020 · UZ 2020-yildan beri yashayman',
        explain: { uz: '“Since / for” bilan hozirgacha davom etayotgan holat — Present Perfect: I have lived.', ru: 'С «since / for» о том, что длится до сих пор, — Present Perfect: I have lived.', en: 'With “since/for” for something still true now, use the present perfect.' } },
      { id: 'gr-news', q: 'The news ___ good today.', options: ['is', 'are'], cat: 'agreement', l1: 'RU новости хорошие · UZ yangiliklar yaxshi',
        explain: { uz: '“News” — birlikda: The news is good.', ru: '«News» — единственное число: The news is good.', en: '“News” is singular: the news is good.' } },
      { id: 'gr-information', q: 'Can you give me some ___ about the course?', options: ['information', 'informations'], cat: 'plural', l1: 'RU информации · UZ maʼlumotlar',
        explain: { uz: '“Information” sanalmaydi — koʻplik shakli yoʻq: some information, a piece of information.', ru: '«Information» неисчисляемое — без множественного числа.', en: '“Information” is uncountable: some information, a piece of information.' } },
    ],
  },
];

export const ALL_TRAPS: TrapItem[] = TRAP_SETS.flatMap((s) => s.items);
