// AVTOMATIK YARATILGAN — qo'lda tahrirlamang. Yangilash: npm run catalog
// Darslar ro'yxati uchun yengil katalog (to'liq matn — grammar*.ts / vocabTopics*.ts da, dars ochilganda yuklanadi).
import type { L3, Level } from './types';

export interface GrammarEntry {
  id: string;
  level: Level;
  title: L3;
  summary: L3;
  file: 'grammar' | 'grammar2';
}

export interface VocabEntry {
  id: string;
  emoji: string;
  title: L3;
  words: number;
  file: 'vocabTopics' | 'vocabTopics2';
}

export const GRAMMAR_CATALOG: GrammarEntry[] = [
  {
    "id": "a2-present-simple-continuous",
    "level": "A2",
    "title": {
      "uz": "Hozirgi oddiy va hozirgi davomli zamon",
      "ru": "Настоящее простое и настоящее длительное время",
      "en": "Present simple vs present continuous"
    },
    "summary": {
      "uz": "Hozirgi oddiy zamon (Present Simple) odat, doimiy holat va umumiy haqiqatlarni, hozirgi davomli zamon (Present Continuous) esa aynan hozir yoki shu kunlarda davom etayotgan ish-harakatni ifodalaydi.",
      "ru": "Present Simple выражает привычки, постоянные состояния и общеизвестные факты, а Present Continuous — действия, которые происходят прямо сейчас или в текущий период.",
      "en": "The present simple describes habits, permanent situations and facts; the present continuous describes actions happening now or around now."
    },
    "file": "grammar"
  },
  {
    "id": "a2-past-simple",
    "level": "A2",
    "title": {
      "uz": "Oʻtgan oddiy zamon: toʻgʻri va notoʻgʻri feʼllar",
      "ru": "Прошедшее простое время: правильные и неправильные глаголы",
      "en": "Past simple: regular and irregular verbs"
    },
    "summary": {
      "uz": "Oʻtgan oddiy zamon (Past Simple) oʻtmishda, aniq vaqtda boshlanib tugagan ish-harakatlar haqida gapirish uchun kerak: yesterday, last week, in 2020, two days ago.",
      "ru": "Past Simple нужен, чтобы рассказать о действиях, которые начались и закончились в определённый момент в прошлом: yesterday, last week, in 2020, two days ago.",
      "en": "Use the past simple for finished actions at a definite time in the past: yesterday, last week, in 2020, two days ago."
    },
    "file": "grammar"
  },
  {
    "id": "a2-articles",
    "level": "A2",
    "title": {
      "uz": "Artikllar: a/an va the",
      "ru": "Артикли a/an и the",
      "en": "Articles: a/an and the"
    },
    "summary": {
      "uz": "Oʻzbek va rus tillarida artikl yoʻq, shuning uchun ular koʻpincha tushib qoladi. A/an — «bitta, qandaydir», the — «oʻsha, maʼlum», umumiy maʼnoda esa koʻpincha artikl ishlatilmaydi.",
      "ru": "В узбекском и русском языках нет артиклей, поэтому их часто пропускают. A/an — «один, какой-то», the — «тот самый, известный», а в общем значении артикль часто не нужен.",
      "en": "Uzbek and Russian have no articles, so learners often leave them out. A/an means 'one, any', the means 'the specific one', and general ideas often need no article."
    },
    "file": "grammar"
  },
  {
    "id": "a2-comparatives",
    "level": "A2",
    "title": {
      "uz": "Sifatlarning qiyosiy va orttirma darajasi",
      "ru": "Сравнительная и превосходная степень прилагательных",
      "en": "Comparatives and superlatives"
    },
    "summary": {
      "uz": "Qiyosiy daraja ikki narsani solishtiradi (bigger than), orttirma daraja esa guruhdagi eng yuqori belgini koʻrsatadi (the biggest).",
      "ru": "Сравнительная степень сравнивает два предмета (bigger than), а превосходная указывает на самый высокий признак в группе (the biggest).",
      "en": "Comparatives compare two things (bigger than); superlatives show the top of a group (the biggest)."
    },
    "file": "grammar"
  },
  {
    "id": "a2-countable-some-any",
    "level": "A2",
    "title": {
      "uz": "Sanaladigan va sanalmaydigan otlar: some, any, much, many",
      "ru": "Исчисляемые и неисчисляемые существительные: some, any, much, many",
      "en": "Countable and uncountable nouns: some, any, much, many"
    },
    "summary": {
      "uz": "Ingliz tilida otlar sanaladigan (an apple, two apples) va sanalmaydigan (water, rice, information) turlarga boʻlinadi. Some, any, much, many yoki a lot of dan qaysi birini tanlash aynan shu farqqa bogʻliq.",
      "ru": "В английском существительные делятся на исчисляемые (an apple, two apples) и неисчисляемые (water, rice, information). От этого зависит, что выбрать: some, any, much, many или a lot of.",
      "en": "English nouns are either countable (an apple, two apples) or uncountable (water, rice, information). This decides whether you use some, any, much, many or a lot of."
    },
    "file": "grammar2"
  },
  {
    "id": "a2-prepositions-time-place",
    "level": "A2",
    "title": {
      "uz": "In, on, at: vaqt va joy predloglari",
      "ru": "In, on, at: предлоги времени и места",
      "en": "Prepositions of time and place: in, on, at"
    },
    "summary": {
      "uz": "In, on va at kichik soʻzlar, lekin eng koʻp xato aynan ularda qilinadi. Oʻzbek tilida bularning oʻrnini koʻpincha bitta -da qoʻshimchasi bosadi, rus tilida esa «в» va «на», shuning uchun har bir holatni alohida eslab qolgan maʼqul.",
      "ru": "In, on и at — маленькие слова, но ошибок в них больше всего. В русском языке им соответствуют «в» и «на», и совпадают они далеко не всегда, поэтому каждый случай лучше запомнить.",
      "en": "In, on and at are small words, but they cause a lot of mistakes. Your first language often uses one form (-da in Uzbek, в/на in Russian) where English uses three, so learn each pattern."
    },
    "file": "grammar2"
  },
  {
    "id": "a2-can-could",
    "level": "A2",
    "title": {
      "uz": "Can va could: qobiliyat, ruxsat va iltimos",
      "ru": "Can и could: умение, разрешение и просьба",
      "en": "Can and could: ability, permission and requests"
    },
    "summary": {
      "uz": "Can va could yordamida biror ishni qila olish-olmaslikni aytamiz, ruxsat soʻraymiz va xushmuomalalik bilan iltimos qilamiz. Bu kundalik muloqotda eng koʻp ishlatiladigan modal feʼllardan.",
      "ru": "С помощью can и could мы говорим об умениях, спрашиваем разрешения и вежливо просим о чём-то. Это одни из самых частых модальных глаголов в повседневной речи.",
      "en": "We use can and could to talk about ability, to ask for permission and to make polite requests. They are among the most common modal verbs in everyday English."
    },
    "file": "grammar2"
  },
  {
    "id": "a2-possessives-object-pronouns",
    "level": "A2",
    "title": {
      "uz": "Egalik: ’s, my/mine va obyekt olmoshlari",
      "ru": "Притяжательность: 's, my/mine и объектные местоимения",
      "en": "Possessives ('s, my/mine) and object pronouns"
    },
    "summary": {
      "uz": "Narsa kimga tegishli ekanini ’s, my/your yoki mine/yours bilan bildiramiz. Me, him, her, them kabi obyekt olmoshlari esa feʼl va predlogdan keyin keladi. Oʻzbek tilida «u» jinsni farqlamaydi, shuning uchun his va her ga alohida eʼtibor bering.",
      "ru": "Принадлежность выражается через 's, my/your или mine/yours. Объектные местоимения (me, him, her, them) стоят после глагола и предлога.",
      "en": "We show who owns something with 's, my/your or mine/yours. Object pronouns such as me, him, her and them come after verbs and prepositions."
    },
    "file": "grammar2"
  },
  {
    "id": "b1-present-perfect-past-simple",
    "level": "B1",
    "title": {
      "uz": "Hozirgi tugallangan zamon va oʻtgan oddiy zamon",
      "ru": "Present Perfect и Past Simple",
      "en": "Present perfect vs past simple"
    },
    "summary": {
      "uz": "Present Perfect oʻtmishni hozirgi payt bilan bogʻlaydi (tajriba, natija, hali tugamagan davr), Past Simple esa tugagan, aniq vaqtdagi voqealar uchun ishlatiladi.",
      "ru": "Present Perfect связывает прошлое с настоящим (опыт, результат, незавершённый период), а Past Simple описывает завершённые события в определённое время.",
      "en": "The present perfect links the past to now (experience, result, unfinished time); the past simple is for finished events at a definite time."
    },
    "file": "grammar"
  },
  {
    "id": "b1-future-forms",
    "level": "B1",
    "title": {
      "uz": "Kelasi zamon shakllari: will, be going to va Present Continuous",
      "ru": "Способы выражения будущего: will, be going to и Present Continuous",
      "en": "Future forms: will, going to and present continuous"
    },
    "summary": {
      "uz": "Ingliz tilida kelajak haqida bir necha usulda gapiriladi. Tanlov qaror qachon qabul qilinganiga va reja qanchalik aniq ekaniga bogʻliq.",
      "ru": "В английском о будущем можно говорить несколькими способами. Выбор зависит от того, когда принято решение и насколько план определён.",
      "en": "English has several ways to talk about the future. The choice depends on when you made the decision and how fixed the plan is."
    },
    "file": "grammar"
  },
  {
    "id": "b1-conditionals-1-2",
    "level": "B1",
    "title": {
      "uz": "Birinchi va ikkinchi tur shart gaplar",
      "ru": "Условные предложения первого и второго типа",
      "en": "First and second conditionals"
    },
    "summary": {
      "uz": "Birinchi tur shart gap real, boʻlishi mumkin boʻlgan vaziyatlar haqida, ikkinchi tur esa xayoliy yoki ehtimoli past vaziyatlar haqida gapiradi.",
      "ru": "Условные предложения первого типа описывают реальные, возможные ситуации, а второго типа — воображаемые или маловероятные.",
      "en": "The first conditional talks about real, possible situations; the second conditional talks about imaginary or unlikely ones."
    },
    "file": "grammar"
  },
  {
    "id": "b1-modals-obligation",
    "level": "B1",
    "title": {
      "uz": "Majburiyat va maslahat: must, have to, should",
      "ru": "Модальные глаголы долженствования и совета: must, have to, should",
      "en": "Obligation and advice: must, have to, should"
    },
    "summary": {
      "uz": "Bu modal feʼllar nima majburiy, nima taqiqlangan, nima shart emas va nima tavsiya etilishini aytish uchun kerak.",
      "ru": "Эти модальные глаголы помогают сказать, что обязательно, что запрещено, что необязательно и что рекомендуется.",
      "en": "These modal verbs help you say what is necessary, forbidden, not necessary or advisable."
    },
    "file": "grammar"
  },
  {
    "id": "b1-past-continuous-simple",
    "level": "B1",
    "title": {
      "uz": "Oʻtgan davomli va oʻtgan oddiy zamon",
      "ru": "Past Continuous и Past Simple",
      "en": "Past continuous vs past simple"
    },
    "summary": {
      "uz": "Oʻtgan davomli zamon (Past Continuous) oʻtmishdagi maʼlum paytda davom etayotgan ishni, oʻtgan oddiy zamon (Past Simple) esa tugallangan ishni bildiradi. Ikkalasi birga kelsa, voqeaning fonini va uni boʻlgan qisqa harakatni koʻrsatadi — hikoya qilishda juda kerakli.",
      "ru": "Past Continuous описывает действие, которое длилось в определённый момент в прошлом, а Past Simple — завершённое действие. Вместе они показывают фон событий и короткое действие, которое его прервало, — это основа любого рассказа.",
      "en": "The past continuous describes an action in progress at a moment in the past; the past simple describes a completed action. Together they show the background of a story and the short event that interrupted it."
    },
    "file": "grammar2"
  },
  {
    "id": "b1-used-to-would",
    "level": "B1",
    "title": {
      "uz": "Used to va would: oʻtmishdagi odatlar",
      "ru": "Used to и would: привычки в прошлом",
      "en": "Used to and would for past habits"
    },
    "summary": {
      "uz": "Used to oʻtmishda boʻlgan, lekin hozir yoʻq odat va holatlarni bildiradi: «ilgari … edim». Would esa oʻtmishda takrorlanib turgan harakatlarni, ayniqsa xotira va hikoyalarda ifodalaydi.",
      "ru": "Used to описывает привычки и состояния, которые были в прошлом, но теперь их нет: «раньше я…». Would передаёт повторяющиеся действия в прошлом, особенно в воспоминаниях и рассказах.",
      "en": "Used to describes past habits and states that are no longer true. Would describes repeated past actions, especially when we tell stories or share memories."
    },
    "file": "grammar2"
  },
  {
    "id": "b1-too-enough-quantifiers",
    "level": "B1",
    "title": {
      "uz": "Too, enough va miqdor soʻzlari: a few, a little, lots of",
      "ru": "Too, enough и слова количества: a few, a little, lots of",
      "en": "Too, enough and quantifiers: a few, a little, lots of"
    },
    "summary": {
      "uz": "Too «haddan tashqari», enough «yetarli» degan maʼnoni beradi. A few, a little, lots of kabi soʻzlar esa miqdorni aniq raqamsiz ifodalaydi. Ular yordamida fikrni aniqroq va tabiiyroq aytish mumkin.",
      "ru": "Too означает «слишком», enough — «достаточно». Слова a few, a little, lots of передают количество без точных чисел. С ними речь становится точнее и естественнее.",
      "en": "Too means 'more than necessary' and enough means 'as much as necessary'. Quantifiers like a few, a little and lots of describe amounts without exact numbers."
    },
    "file": "grammar2"
  },
  {
    "id": "b1-phrasal-verbs",
    "level": "B1",
    "title": {
      "uz": "Keng tarqalgan frazali feʼllar",
      "ru": "Распространённые фразовые глаголы",
      "en": "Common phrasal verbs"
    },
    "summary": {
      "uz": "Frazali feʼl — feʼl va kichik soʻzdan (up, off, after, out…) iborat birikma, uning maʼnosi koʻpincha tarkibidagi soʻzlardan butunlay farq qiladi. Soʻzlashuv tilida ular juda koʻp ishlatiladi, shuning uchun ularni bilish tabiiy gapirish uchun muhim.",
      "ru": "Фразовый глагол — это глагол с частицей (up, off, after, out…), и его значение часто совсем не выводится из частей. В разговорной речи их очень много, поэтому без них трудно звучать естественно.",
      "en": "A phrasal verb is a verb plus a particle (up, off, after, out…), and its meaning is often different from its parts. They are everywhere in spoken English, so you need them to sound natural."
    },
    "file": "grammar2"
  },
  {
    "id": "b2-passive-voice",
    "level": "B2",
    "title": {
      "uz": "Majhul nisbat",
      "ru": "Страдательный залог",
      "en": "The passive voice"
    },
    "summary": {
      "uz": "Majhul nisbatda diqqat ish-harakatni kim bajarganiga emas, harakatning oʻziga yoki natijasiga qaratiladi. Yangiliklar, ilmiy matnlar va rasmiy yozuvlarda juda koʻp uchraydi.",
      "ru": "Страдательный залог переносит внимание с того, кто совершил действие, на само действие или его результат. Он очень част в новостях, научных и официальных текстах.",
      "en": "The passive puts the focus on the action or result, not on who did it. It is very common in news, science and formal writing."
    },
    "file": "grammar"
  },
  {
    "id": "b2-reported-speech",
    "level": "B2",
    "title": {
      "uz": "Oʻzlashtirma gap",
      "ru": "Косвенная речь",
      "en": "Reported speech"
    },
    "summary": {
      "uz": "Oʻzlashtirma gap boshqa odamning soʻzlarini qayta aytib berish uchun ishlatiladi. Koʻpincha zamon bir pogʻona orqaga suriladi, olmoshlar va vaqt soʻzlari oʻzgaradi.",
      "ru": "Косвенная речь нужна, чтобы передать чужие слова. Обычно время сдвигается на шаг в прошлое, а местоимения и слова времени меняются.",
      "en": "Reported speech is used to tell someone what another person said. Tenses usually move one step back, and pronouns and time words change."
    },
    "file": "grammar"
  },
  {
    "id": "b2-relative-clauses",
    "level": "B2",
    "title": {
      "uz": "Aniqlovchi ergash gaplar: who, which, that, whose, where",
      "ru": "Определительные придаточные предложения: who, which, that, whose, where",
      "en": "Relative clauses: defining and non-defining"
    },
    "summary": {
      "uz": "Aniqlovchi ergash gaplar odam yoki narsa haqida qoʻshimcha maʼlumot beradi va ikki qisqa gapni bitta silliq gapga birlashtirishga yordam beradi.",
      "ru": "Определительные придаточные дают дополнительную информацию о человеке или предмете и помогают объединить два коротких предложения в одно.",
      "en": "Relative clauses add information about a person or thing and let you join two short sentences into one smooth one."
    },
    "file": "grammar"
  },
  {
    "id": "b2-gerunds-infinitives",
    "level": "B2",
    "title": {
      "uz": "Gerundiy va infinitiv",
      "ru": "Герундий и инфинитив",
      "en": "Gerunds vs infinitives"
    },
    "summary": {
      "uz": "Baʼzi feʼllardan keyin -ing shakli (gerundiy), baʼzilaridan keyin esa to + feʼl (infinitiv) keladi. Ayrim feʼllarda tanlov gapning maʼnosini oʻzgartiradi.",
      "ru": "После одних глаголов идёт форма на -ing (герундий), после других — to + глагол (инфинитив). У некоторых глаголов выбор меняет смысл.",
      "en": "Some verbs are followed by -ing (gerund), others by to + verb (infinitive). With a few verbs, the choice changes the meaning."
    },
    "file": "grammar"
  },
  {
    "id": "b2-present-perfect-continuous",
    "level": "B2",
    "title": {
      "uz": "Hozirgi tugallangan davomli zamon",
      "ru": "Present Perfect Continuous",
      "en": "Present perfect continuous"
    },
    "summary": {
      "uz": "Present Perfect Continuous oʻtmishda boshlanib, hozirgacha davom etayotgan yoki hozirgina tugab, izi koʻrinib turgan ishni bildiradi. Oʻzbek va rus tillarida bunday gaplar koʻpincha hozirgi zamonda aytiladi («uch yildan beri oʻqiyapman»), ingliz tilida esa maxsus shakl kerak.",
      "ru": "Present Perfect Continuous описывает действие, которое началось в прошлом и продолжается до сих пор или только что закончилось и оставило видимый след. По-русски в таких случаях обычно используется настоящее время («учу три года»), а в английском нужна особая форма.",
      "en": "The present perfect continuous describes an activity that started in the past and is still going on, or has just stopped and left a visible result. Many languages use the present tense here, but English needs this special form."
    },
    "file": "grammar2"
  },
  {
    "id": "b2-modals-deduction",
    "level": "B2",
    "title": {
      "uz": "Taxmin modallari: must, might, can’t",
      "ru": "Модальные глаголы предположения: must, might, can't",
      "en": "Modals of deduction: must, might, can't"
    },
    "summary": {
      "uz": "Must, might, may, could va can’t yordamida dalillarga tayanib xulosa chiqaramiz: «albatta shunday boʻlsa kerak», «balki», «boʻlishi mumkin emas». Oʻtmish haqidagi taxmin uchun ularga have + uchinchi shakl qoʻshiladi.",
      "ru": "С помощью must, might, may, could и can't мы делаем выводы на основе фактов: «наверняка», «возможно», «не может быть». Для предположений о прошлом к ним добавляется have + третья форма.",
      "en": "We use must, might, may, could and can't to make logical guesses based on evidence: 'I'm sure', 'perhaps', 'it's impossible'. For guesses about the past, add have + past participle."
    },
    "file": "grammar2"
  },
  {
    "id": "b2-causative-have-get",
    "level": "B2",
    "title": {
      "uz": "Birovga ish qildirish: have/get something done",
      "ru": "Каузатив: have/get something done",
      "en": "Causative: have/get something done"
    },
    "summary": {
      "uz": "Biror ishni oʻzimiz emas, boshqa odam (usta, sartarosh, mexanik) bajarsa, have/get + narsa + uchinchi shakl ishlatiladi. Bu oʻzbek tilidagi -tir/-dir/-t qoʻshimchasiga oʻxshaydi: sochimni oldirdim — I had my hair cut.",
      "ru": "Если работу делаем не мы сами, а кто-то другой (мастер, парикмахер, механик), используется have/get + предмет + третья форма. По-русски это часто передаётся возвратным глаголом или безличной формой: «я подстригся», «мне починили машину».",
      "en": "When someone else (a hairdresser, a mechanic, a technician) does a job for us, we use have/get + object + past participle."
    },
    "file": "grammar2"
  },
  {
    "id": "b2-future-continuous-perfect",
    "level": "B2",
    "title": {
      "uz": "Kelasi davomli va kelasi tugallangan zamon",
      "ru": "Future Continuous и Future Perfect",
      "en": "Future continuous and future perfect"
    },
    "summary": {
      "uz": "Future Continuous kelajakdagi maʼlum paytda davom etayotgan ishni, Future Perfect esa kelajakdagi maʼlum paytgacha tugab boʻladigan ishni bildiradi. Ular reja tuzish, muddat haqida gapirish va xushmuomala savol berishda juda qoʻl keladi.",
      "ru": "Future Continuous описывает действие, которое будет длиться в определённый момент в будущем, а Future Perfect — действие, которое завершится к определённому моменту. Они полезны, когда мы строим планы, говорим о сроках и вежливо спрашиваем.",
      "en": "The future continuous describes an action in progress at a certain time in the future; the future perfect describes an action that will be complete before a certain time. They are useful for plans, deadlines and polite questions."
    },
    "file": "grammar2"
  },
  {
    "id": "c1-third-mixed-conditionals",
    "level": "C1",
    "title": {
      "uz": "Uchinchi tur va aralash shart gaplar",
      "ru": "Условные предложения третьего типа и смешанные",
      "en": "Third and mixed conditionals"
    },
    "summary": {
      "uz": "Uchinchi tur shart gap oʻtmishdagi amalga oshmagan vaziyatni tasavvur qiladi (afsus, tanqid), aralash shart gaplar esa oʻtmish va hozirgi zamonni bir gapda bogʻlaydi.",
      "ru": "Условные третьего типа описывают нереальное прошлое (сожаление, упрёк), а смешанные связывают прошлое и настоящее в одном предложении.",
      "en": "The third conditional imagines a different past (regrets, criticism); mixed conditionals link the past and the present in one sentence."
    },
    "file": "grammar"
  },
  {
    "id": "c1-inversion",
    "level": "C1",
    "title": {
      "uz": "Taʼkid uchun inversiya",
      "ru": "Инверсия для усиления",
      "en": "Inversion for emphasis"
    },
    "summary": {
      "uz": "Inkor yoki cheklov maʼnosidagi ravish gap boshiga chiqsa, ega va yordamchi feʼl oʻrin almashadi. Bu usul nutqni taʼsirchan va rasmiy qiladi — esse, nutq va rasmiy xatlarda qoʻl keladi.",
      "ru": "Если в начало предложения выносится отрицательное или ограничительное наречие, подлежащее и вспомогательный глагол меняются местами. Это делает речь выразительной и официальной — полезно в эссе, выступлениях и деловых письмах.",
      "en": "When a negative or limiting adverbial starts a sentence, the subject and auxiliary swap places. It makes your language emphatic and formal — useful in essays, speeches and formal letters."
    },
    "file": "grammar"
  },
  {
    "id": "c1-linking-cohesion",
    "level": "C1",
    "title": {
      "uz": "Murakkab bogʻlovchi vositalar va matn izchilligi",
      "ru": "Связующие средства и связность текста",
      "en": "Advanced linking and cohesion"
    },
    "summary": {
      "uz": "Bogʻlovchi soʻzlar fikrlar orasidagi mantiqiy aloqani — zidlik, shart, sabab-natija va qoʻshimchani koʻrsatadi. Ularni toʻgʻri tanlash IELTS va CEFR imtihonlarida yuqori ball olishning muhim sharti.",
      "ru": "Связующие слова показывают логические отношения между идеями — противопоставление, условие, причину и следствие, добавление. Умение правильно их выбирать — важное условие высокого балла на IELTS и CEFR.",
      "en": "Linking words show how ideas relate — contrast, condition, cause and result, addition. Using them correctly is key to a high score in IELTS and CEFR exams."
    },
    "file": "grammar"
  },
  {
    "id": "c1-wish-if-only",
    "level": "C1",
    "title": {
      "uz": "Afsus va istak: wish, if only, would rather",
      "ru": "Сожаление и желание: wish, if only, would rather",
      "en": "Wish, if only and would rather"
    },
    "summary": {
      "uz": "Bu qurilmalar hozirgi holatdan norozilik, oʻtmishdagi xatolar uchun afsus va afzal koʻrishni ifodalaydi. Ulardan keyingi zamon doim bir pogʻona «orqada» turadi.",
      "ru": "Эти конструкции выражают недовольство настоящим, сожаление о прошлом и предпочтения. Время после них всегда сдвинуто на шаг назад.",
      "en": "These structures express wishes about the present, regrets about the past and preferences. The verb after them is always one step 'back' in time."
    },
    "file": "grammar"
  },
  {
    "id": "c1-cleft-sentences",
    "level": "C1",
    "title": {
      "uz": "Ajratuvchi gaplar: It was… that, What I need is…",
      "ru": "Расщеплённые предложения: It was… that, What I need is…",
      "en": "Cleft sentences: It was… that / What I need is…"
    },
    "summary": {
      "uz": "Cleft gaplar oddiy gapni ikki qismga boʻlib, bir boʻlagini alohida taʼkidlaydi. Oʻzbek tilida bu vazifani koʻpincha «aynan», «-ku» yoki soʻz tartibi, rus tilida «именно» va «вот что» bajaradi; ingliz tilida esa soʻz tartibi erkin emas, shuning uchun maxsus tuzilma kerak.",
      "ru": "Расщеплённые предложения делят простое предложение на две части, чтобы выделить один элемент. В русском это делают порядок слов, «именно» или «вот что», а в английском порядок слов жёсткий, поэтому нужна особая конструкция.",
      "en": "Cleft sentences split a simple sentence into two parts to put the focus on one element. English word order is fixed, so instead of moving words around we use these special structures."
    },
    "file": "grammar2"
  },
  {
    "id": "c1-participle-clauses",
    "level": "C1",
    "title": {
      "uz": "Sifatdosh va ravishdosh oborotlari (participle clauses)",
      "ru": "Причастные и деепричастные обороты (participle clauses)",
      "en": "Participle clauses"
    },
    "summary": {
      "uz": "Participle clauses — -ing, uchinchi shakl yoki having done bilan boshlanadigan qisqa oborotlar. Ular because, when, after, which kabi soʻzlarni tushirib, matnni ixcham va rasmiy qiladi. Esse, hisobot va rasmiy nutqda juda koʻp uchraydi.",
      "ru": "Participle clauses — короткие обороты с -ing, третьей формой или having done. Они заменяют придаточные с because, when, after, which и делают текст сжатым и формальным. Особенно часто встречаются в эссе, отчётах и официальной речи.",
      "en": "Participle clauses are short phrases starting with an -ing form, a past participle or having done. They replace clauses with because, when, after or which, making writing concise and formal."
    },
    "file": "grammar2"
  },
  {
    "id": "c1-subjunctive-formal",
    "level": "C1",
    "title": {
      "uz": "Subjunktiv va rasmiy tuzilmalar",
      "ru": "Сослагательное наклонение (subjunctive) и формальные конструкции",
      "en": "The subjunctive and formal structures"
    },
    "summary": {
      "uz": "Talab, tavsiya yoki zaruratni rasmiy tarzda ifodalashda ingliz tili subjunktivdan foydalanadi: that dan keyin feʼl boshlangʻich shaklda qoladi — -s ham, oʻtgan zamon ham qoʻshilmaydi. Bu shakl rasmiy xat, hisobot va akademik matnlarda ayniqsa koʻp uchraydi.",
      "ru": "Для официального выражения требований, рекомендаций и необходимости в английском используется subjunctive: после that глагол остаётся в начальной форме — без -s и без сдвига времени. Особенно часто это встречается в деловых письмах, отчётах и академических текстах.",
      "en": "To express demands, recommendations and necessity formally, English uses the subjunctive: after that, the verb stays in its base form, with no -s and no past tense. It is common in formal letters, reports and academic writing."
    },
    "file": "grammar2"
  },
  {
    "id": "c1-passive-reporting",
    "level": "C1",
    "title": {
      "uz": "Xabar passivi: It is said that…, He is believed to…",
      "ru": "Пассивные конструкции передачи мнения: It is said that…, He is believed to…",
      "en": "Advanced passive reporting: It is said that… / He is believed to…"
    },
    "summary": {
      "uz": "Umumiy fikr, xabar yoki taxminni manbasini aytmasdan, xolis va rasmiy ohangda yetkazish uchun reporting passive ishlatiladi. Yangiliklar, ilmiy matnlar va IELTS esselarida juda foydali: «aytilishicha», «taxmin qilinishicha», «maʼlum boʻlishicha».",
      "ru": "Пассивные конструкции передачи мнения позволяют сообщить общее мнение, новость или предположение безлично и официально: «говорят», «считается», «сообщается». Они очень полезны в новостях, научных текстах и эссе IELTS.",
      "en": "Reporting passives let you present opinions, reports and beliefs impersonally and formally, without naming a source. They are common in news, academic writing and IELTS essays."
    },
    "file": "grammar2"
  }
];

export const VOCAB_CATALOG: VocabEntry[] = [
  {
    "id": "home-family",
    "emoji": "🏠",
    "title": {
      "uz": "Uy va oila",
      "ru": "Дом и семья",
      "en": "Home and family"
    },
    "words": 17,
    "file": "vocabTopics"
  },
  {
    "id": "food-cooking",
    "emoji": "🍲",
    "title": {
      "uz": "Taom va pazandachilik",
      "ru": "Еда и кулинария",
      "en": "Food and cooking"
    },
    "words": 17,
    "file": "vocabTopics"
  },
  {
    "id": "travel",
    "emoji": "✈️",
    "title": {
      "uz": "Sayohat",
      "ru": "Путешествия",
      "en": "Travel"
    },
    "words": 16,
    "file": "vocabTopics"
  },
  {
    "id": "work-careers",
    "emoji": "💼",
    "title": {
      "uz": "Ish va kasb",
      "ru": "Работа и карьера",
      "en": "Work and careers"
    },
    "words": 17,
    "file": "vocabTopics"
  },
  {
    "id": "education",
    "emoji": "🎓",
    "title": {
      "uz": "Taʼlim",
      "ru": "Образование",
      "en": "Education"
    },
    "words": 16,
    "file": "vocabTopics"
  },
  {
    "id": "health",
    "emoji": "🩺",
    "title": {
      "uz": "Salomatlik",
      "ru": "Здоровье",
      "en": "Health"
    },
    "words": 17,
    "file": "vocabTopics"
  },
  {
    "id": "technology",
    "emoji": "💻",
    "title": {
      "uz": "Texnologiya",
      "ru": "Технологии",
      "en": "Technology"
    },
    "words": 16,
    "file": "vocabTopics"
  },
  {
    "id": "environment",
    "emoji": "🌍",
    "title": {
      "uz": "Atrof-muhit",
      "ru": "Окружающая среда",
      "en": "The environment"
    },
    "words": 16,
    "file": "vocabTopics"
  },
  {
    "id": "city-transport",
    "emoji": "🏙️",
    "title": {
      "uz": "Shahar va transport",
      "ru": "Город и транспорт",
      "en": "City and transport"
    },
    "words": 16,
    "file": "vocabTopics"
  },
  {
    "id": "money-shopping",
    "emoji": "💰",
    "title": {
      "uz": "Pul va xarid",
      "ru": "Деньги и покупки",
      "en": "Money and shopping"
    },
    "words": 16,
    "file": "vocabTopics"
  },
  {
    "id": "sport-leisure",
    "emoji": "⚽",
    "title": {
      "uz": "Sport va boʻsh vaqt",
      "ru": "Спорт и свободное время",
      "en": "Sport and free time"
    },
    "words": 17,
    "file": "vocabTopics"
  },
  {
    "id": "culture-media",
    "emoji": "🎭",
    "title": {
      "uz": "Madaniyat va OAV",
      "ru": "Культура и СМИ",
      "en": "Culture and media"
    },
    "words": 17,
    "file": "vocabTopics"
  },
  {
    "id": "nature-animals",
    "emoji": "🦁",
    "title": {
      "uz": "Tabiat va hayvonlar",
      "ru": "Природа и животные",
      "en": "Nature and animals"
    },
    "words": 16,
    "file": "vocabTopics2"
  },
  {
    "id": "weather-seasons",
    "emoji": "🌦️",
    "title": {
      "uz": "Ob-havo va fasllar",
      "ru": "Погода и времена года",
      "en": "Weather and seasons"
    },
    "words": 16,
    "file": "vocabTopics2"
  },
  {
    "id": "feelings-personality",
    "emoji": "😊",
    "title": {
      "uz": "Hislar va xarakter",
      "ru": "Чувства и характер",
      "en": "Feelings and personality"
    },
    "words": 16,
    "file": "vocabTopics2"
  },
  {
    "id": "science-research",
    "emoji": "🔬",
    "title": {
      "uz": "Fan va tadqiqot",
      "ru": "Наука и исследования",
      "en": "Science and research"
    },
    "words": 16,
    "file": "vocabTopics2"
  },
  {
    "id": "business-economy",
    "emoji": "📈",
    "title": {
      "uz": "Biznes va iqtisodiyot",
      "ru": "Бизнес и экономика",
      "en": "Business and the economy"
    },
    "words": 16,
    "file": "vocabTopics2"
  },
  {
    "id": "arts-music",
    "emoji": "🎨",
    "title": {
      "uz": "Sanʼat va musiqa",
      "ru": "Искусство и музыка",
      "en": "Art and music"
    },
    "words": 16,
    "file": "vocabTopics2"
  },
  {
    "id": "celebrations-traditions",
    "emoji": "🎉",
    "title": {
      "uz": "Bayramlar va urf-odatlar",
      "ru": "Праздники и традиции",
      "en": "Celebrations and traditions"
    },
    "words": 16,
    "file": "vocabTopics2"
  },
  {
    "id": "clothes-appearance",
    "emoji": "👗",
    "title": {
      "uz": "Kiyim va tashqi koʻrinish",
      "ru": "Одежда и внешность",
      "en": "Clothes and appearance"
    },
    "words": 16,
    "file": "vocabTopics2"
  }
];
