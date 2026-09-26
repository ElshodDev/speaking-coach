namespace SpeakingCoach.Api.Services;

/// <summary>Kunlik so'z: inglizcha so'z, turkum, daraja, ta'rif, misol va tarjimalar.</summary>
public record DailyWord(string Word, string Pos, string Level, string DefinitionEn, string Example, string Uz, string Ru)
{
    public string Translation(string lang) => lang switch { "ru" => Ru, "en" => DefinitionEn, _ => Uz };
}

/// <summary>
/// "Kunlik so'z" — qo'lda tanlangan B1–C1 so'zlar (IELTS va CEFR
/// imtihonlarida tez-tez uchraydigan akademik va kundalik so'zlar).
/// AI ishlatilmaydi: har kuni bitta so'z, hamma uchun bir xil (sana
/// bo'yicha), tarjimalar oldindan tekshirilgan.
/// </summary>
public static class DailyWords
{
    public static readonly DailyWord[] All =
    [
        new("achieve", "verb", "B1", "to succeed in doing something after effort", "She worked hard to achieve her goals.", "erishmoq", "достигать"),
        new("affordable", "adjective", "B1", "cheap enough for most people to buy", "The city needs more affordable housing.", "arzon, hamyonbop", "доступный (по цене)"),
        new("approach", "noun", "B2", "a way of dealing with a problem", "We need a new approach to teaching grammar.", "yondashuv", "подход"),
        new("argue", "verb", "B1", "to disagree in words; or to give reasons for an opinion", "Some people argue that exams are unfair.", "bahslashmoq; daʼvo qilmoq", "спорить; утверждать"),
        new("assume", "verb", "B2", "to think something is true without proof", "Don't assume everyone agrees with you.", "faraz qilmoq", "предполагать"),
        new("available", "adjective", "B1", "able to be used or obtained", "Tickets are available online.", "mavjud, bor", "доступный, имеющийся"),
        new("benefit", "noun", "B1", "an advantage or good effect", "Regular exercise has many benefits.", "foyda", "польза, преимущество"),
        new("challenge", "noun", "B1", "something new and difficult that tests your ability", "Learning a language is a real challenge.", "qiyin sinov, murakkab vazifa", "вызов, трудная задача"),
        new("claim", "verb", "B2", "to say that something is true, often without proof", "The company claims its product is the best.", "daʼvo qilmoq", "заявлять, утверждать"),
        new("community", "noun", "B1", "people who live in one area or share interests", "The whole community helped to clean the park.", "jamoa, mahalla", "сообщество"),
        new("consequence", "noun", "B2", "a result of an action, often a bad one", "Pollution has serious consequences for health.", "oqibat", "последствие"),
        new("consider", "verb", "B1", "to think carefully about something", "Please consider all the options.", "koʻrib chiqmoq, oʻylab koʻrmoq", "рассматривать, обдумывать"),
        new("contribute", "verb", "B2", "to help to cause or achieve something", "Tourism contributes to the local economy.", "hissa qoʻshmoq", "вносить вклад"),
        new("convenient", "adjective", "B1", "easy to use or suitable for your needs", "Online shopping is very convenient.", "qulay", "удобный"),
        new("crucial", "adjective", "B2", "extremely important", "Sleep is crucial for memory.", "hal qiluvchi, juda muhim", "решающий, крайне важный"),
        new("decline", "verb", "B2", "to become less in amount or quality", "The number of readers has declined.", "kamaymoq, pasaymoq", "снижаться, сокращаться"),
        new("demand", "noun", "B2", "the need or desire for something", "There is a high demand for engineers.", "talab", "спрос"),
        new("develop", "verb", "B1", "to grow or change into something bigger or better", "Children develop quickly in the first years.", "rivojlanmoq", "развиваться"),
        new("efficient", "adjective", "B2", "working well without wasting time or energy", "Electric cars are more efficient.", "samarali", "эффективный"),
        new("encourage", "verb", "B1", "to give someone confidence or support to do something", "My teacher encouraged me to take the exam.", "ragʻbatlantirmoq", "поощрять, вдохновлять"),
        new("environment", "noun", "B1", "the natural world of land, water and air", "We must protect the environment.", "atrof-muhit", "окружающая среда"),
        new("essential", "adjective", "B2", "completely necessary", "Water is essential for life.", "zarur, muhim", "необходимый, важнейший"),
        new("evidence", "noun", "B2", "facts that show something is true", "There is no evidence that he was there.", "dalil", "доказательство"),
        new("expand", "verb", "B2", "to become larger", "The company plans to expand abroad.", "kengaymoq", "расширяться"),
        new("factor", "noun", "B2", "one of the things that cause a result", "Cost is an important factor.", "omil", "фактор"),
        new("frequent", "adjective", "B1", "happening often", "She is a frequent visitor to the library.", "tez-tez boʻladigan", "частый"),
        new("generation", "noun", "B1", "people of about the same age", "The younger generation uses social media a lot.", "avlod", "поколение"),
        new("impact", "noun", "B2", "a strong effect", "Technology has a huge impact on our lives.", "taʼsir", "влияние, воздействие"),
        new("improve", "verb", "B1", "to make or become better", "Reading every day will improve your vocabulary.", "yaxshilamoq", "улучшать"),
        new("increase", "verb", "B1", "to become larger in amount", "Prices increased by ten percent.", "oshmoq, koʻpaymoq", "увеличиваться, расти"),
        new("influence", "verb", "B2", "to affect how someone thinks or behaves", "Parents influence their children's choices.", "taʼsir qilmoq", "влиять"),
        new("issue", "noun", "B2", "an important problem or topic", "Climate change is a global issue.", "masala, muammo", "вопрос, проблема"),
        new("maintain", "verb", "B2", "to keep something at the same level", "It's hard to maintain a healthy diet.", "saqlab turmoq", "поддерживать, сохранять"),
        new("method", "noun", "B1", "a way of doing something", "This method is quick and cheap.", "usul", "метод, способ"),
        new("obvious", "adjective", "B2", "easy to see or understand", "The answer seems obvious.", "aniq, ravshan", "очевидный"),
        new("opportunity", "noun", "B1", "a chance to do something", "The job is a great opportunity.", "imkoniyat", "возможность"),
        new("option", "noun", "B1", "a choice", "You have two options.", "variant, tanlov", "вариант, выбор"),
        new("participate", "verb", "B2", "to take part in an activity", "All students participated in the project.", "ishtirok etmoq", "участвовать"),
        new("particular", "adjective", "B1", "special or specific", "Is there a particular reason?", "alohida, muayyan", "особый, определённый"),
        new("prevent", "verb", "B1", "to stop something from happening", "Washing hands helps prevent illness.", "oldini olmoq", "предотвращать"),
        new("previous", "adjective", "B1", "happening before", "In the previous lesson we learned verbs.", "oldingi", "предыдущий"),
        new("priority", "noun", "B2", "something more important than other things", "Safety is our top priority.", "ustuvor vazifa", "приоритет"),
        new("reduce", "verb", "B1", "to make something smaller or less", "We should reduce plastic waste.", "kamaytirmoq", "сокращать, уменьшать"),
        new("rely", "verb", "B2", "to depend on someone or something", "Many people rely on public transport.", "tayanmoq", "полагаться"),
        new("require", "verb", "B2", "to need something", "The course requires a lot of practice.", "talab qilmoq", "требовать"),
        new("resource", "noun", "B2", "something useful, such as money or materials", "Water is a limited resource.", "manba, resurs", "ресурс"),
        new("responsible", "adjective", "B1", "having a duty to deal with something", "Who is responsible for this project?", "masʼul", "ответственный"),
        new("significant", "adjective", "B2", "important or large enough to notice", "There was a significant rise in prices.", "sezilarli, muhim", "значительный"),
        new("solution", "noun", "B1", "a way of solving a problem", "We found a simple solution.", "yechim", "решение"),
        new("sufficient", "adjective", "C1", "enough for a particular purpose", "Is one hour sufficient for the test?", "yetarli", "достаточный"),
        new("sustainable", "adjective", "C1", "able to continue without harming the environment", "We need sustainable energy sources.", "barqaror, tabiatga zararsiz", "устойчивый (экологичный)"),
        new("tend", "verb", "B2", "to usually do or be something", "Teenagers tend to sleep late.", "moyil boʻlmoq", "иметь склонность"),
        new("trend", "noun", "B2", "a general change or development", "There is a trend towards working from home.", "tendensiya, yoʻnalish", "тенденция"),
        new("valuable", "adjective", "B1", "very useful or important", "Thank you for your valuable advice.", "qimmatli", "ценный"),
        new("various", "adjective", "B1", "several different", "The shop sells various kinds of tea.", "turli, har xil", "различный"),
        new("widespread", "adjective", "C1", "existing in many places or among many people", "Smartphones are widespread among students.", "keng tarqalgan", "широко распространённый"),
        new("acknowledge", "verb", "C1", "to accept that something is true", "He acknowledged his mistake.", "tan olmoq", "признавать"),
        new("controversial", "adjective", "C1", "causing a lot of disagreement", "It is a controversial decision.", "bahsli, munozarali", "спорный"),
        new("enhance", "verb", "C1", "to improve the quality of something", "Music can enhance your mood.", "yaxshilamoq, kuchaytirmoq", "улучшать, усиливать"),
        new("inevitable", "adjective", "C1", "certain to happen", "Change is inevitable.", "muqarrar", "неизбежный"),
    ];

    /// <summary>Sana bo'yicha (hamma uchun bir xil) — ketma-ket kunlarda takrorlanmaydi.</summary>
    public static DailyWord ForDate(DateOnly date) => All[((date.DayNumber % All.Length) + All.Length) % All.Length];
}
