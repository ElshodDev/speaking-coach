using System.Globalization;

namespace SpeakingCoach.Api.Services;

/// <summary>
/// Foydalanuvchiga ko'rinadigan server xabarlari uch tilda (uz, ru, en).
/// Frontend har bir so'rovda <c>Accept-Language</c> sarlavhasini yuboradi
/// (HTTP standarti) — xabar shu tilda qaytadi. Servislar matn emas, KALIT
/// qaytaradi (masalan <c>auth.email_taken</c>): shunda ular HTTP'ga bog'liq
/// bo'lmay qoladi va testlarda matn o'zgarishi testni buzmaydi.
/// </summary>
public static class Texts
{
    public const string DefaultLang = "uz";
    public static readonly string[] Langs = { "uz", "ru", "en" };

    private sealed record Entry(string Uz, string Ru, string En);

    private static readonly Dictionary<string, Entry> Map = new()
    {
        ["rate_limited"] = new(
            "Soʻrovlar juda koʻp — bir daqiqadan soʻng qayta urinib koʻring.",
            "Слишком много запросов — попробуйте снова через минуту.",
            "Too many requests — please try again in a minute."),
        ["ai_unavailable"] = new(
            "Sunʼiy intellekt hozir javob bermadi. Birozdan soʻng qayta urinib koʻring.",
            "ИИ сейчас не ответил. Попробуйте немного позже.",
            "The AI didn't respond just now. Please try again shortly."),

        ["auth.invalid_email"] = new("Email manzili notoʻgʻri.", "Неверный адрес электронной почты.", "The email address is not valid."),
        ["auth.password_short"] = new(
            "Parol kamida {0} ta belgidan iborat boʻlishi kerak.",
            "Пароль должен содержать не менее {0} символов.",
            "The password must be at least {0} characters long."),
        ["auth.password_long"] = new(
            "Parol {0} ta belgidan oshmasligi kerak.",
            "Пароль должен быть не длиннее {0} символов.",
            "The password must be at most {0} characters long."),
        ["auth.email_taken"] = new(
            "Bu email bilan allaqachon roʻyxatdan oʻtilgan.",
            "Этот email уже зарегистрирован.",
            "An account with this email already exists."),
        ["auth.wrong_credentials"] = new(
            "Email yoki parol notoʻgʻri.",
            "Неверный email или пароль.",
            "Wrong email or password."),
        ["auth.not_logged_in"] = new("Tizimga kirilmagan.", "Вы не вошли в систему.", "You are not logged in."),
        ["code.wrong"] = new("Kod notoʻgʻri.", "Неверный код.", "The code is incorrect."),
        ["code.wrong_left"] = new(
            "Kod notoʻgʻri. Yana {0} ta urinish qoldi.",
            "Неверный код. Осталось попыток: {0}.",
            "The code is incorrect. Attempts left: {0}."),
        ["code.too_many"] = new(
            "Urinishlar tugadi — yangi kod soʻrang.",
            "Попытки закончились — запросите новый код.",
            "Too many attempts — please request a new code."),
        ["code.expired"] = new(
            "Kodning muddati tugagan — yangi kod soʻrang.",
            "Срок действия кода истёк — запросите новый.",
            "The code has expired — please request a new one."),
        ["code.not_found"] = new(
            "Faol kod yoʻq — yangi kod soʻrang.",
            "Активного кода нет — запросите новый.",
            "There is no active code — please request a new one."),
        ["code.wait"] = new(
            "Yangi kodni {0} soniyadan soʻng soʻrash mumkin.",
            "Новый код можно запросить через {0} с.",
            "You can request a new code in {0} s."),
        ["email.send_failed"] = new(
            "Xatni yuborib boʻlmadi. Birozdan soʻng qayta urinib koʻring.",
            "Не удалось отправить письмо. Попробуйте немного позже.",
            "We couldn't send the email. Please try again shortly."),
        ["email.unavailable"] = new(
            "Email yuborish hozircha sozlanmagan.",
            "Отправка писем пока не настроена.",
            "Email sending is not set up yet."),
        ["google.invalid"] = new(
            "Google orqali kirib boʻlmadi. Qayta urinib koʻring.",
            "Не удалось войти через Google. Попробуйте ещё раз.",
            "Couldn't sign in with Google. Please try again."),
        ["google.unavailable"] = new(
            "Google orqali kirish hozircha sozlanmagan.",
            "Вход через Google пока не настроен.",
            "Google sign-in is not set up yet."),
        ["ai.limit_exercises"] = new(
            "Bugungi mashqlar limiti tugadi ({0} ta). Ertaga yana davom eting — takrorlash va lugʻat cheklanmagan.",
            "Лимит упражнений на сегодня исчерпан ({0}). Продолжите завтра — повторение и словарь без ограничений.",
            "You've used today's exercise limit ({0}). Come back tomorrow — review and vocabulary are unlimited."),
        ["ai.limit_guest"] = new(
            "Mehmonlar uchun kunlik limit tugadi ({0} ta mashq). Bepul hisob oching — kuniga {1} ta mashq.",
            "Дневной лимит для гостей исчерпан ({0} упражнений). Создайте бесплатный аккаунт — {1} упражнений в день.",
            "The daily guest limit is used up ({0} exercises). Create a free account to get {1} a day."),
        ["ai.limit_words"] = new(
            "Bugun {0} ta soʻz izohi olindi — limit tugadi. Ertaga yana davom eting.",
            "Сегодня получено {0} объяснений слов — лимит исчерпан. Продолжите завтра.",
            "You've looked up {0} words today — that's the daily limit. Come back tomorrow."),
        ["ai.limit_words_guest"] = new(
            "Mehmonlar uchun soʻz izohlari limiti tugadi ({0} ta). Bepul hisob oching — kuniga {1} ta.",
            "Лимит объяснений слов для гостей исчерпан ({0}). Создайте бесплатный аккаунт — {1} в день.",
            "The guest limit for word look-ups is used up ({0}). Create a free account to get {1} a day."),
        ["account.confirm_mismatch"] = new(
            "Tasdiqlash uchun hisobingiz emailini aynan kiriting.",
            "Для подтверждения введите точный email вашего аккаунта.",
            "To confirm, type your account's email exactly."),
        ["login.required"] = new("Tizimga kiring.", "Войдите в систему.", "Please log in."),
        ["login.review"] = new("Takrorlash uchun tizimga kiring.", "Войдите, чтобы повторять карточки.", "Log in to review your cards."),
        ["login.vocab"] = new("Lugʻatni saqlash uchun tizimga kiring.", "Войдите, чтобы сохранять словарь.", "Log in to save your vocabulary."),
        ["admin.only"] = new("Bu sahifa faqat administrator uchun.", "Эта страница только для администратора.", "This page is for the administrator only."),

        ["review.bad_grade"] = new(
            "Baho 0 (Yana), 1 (Qiyin), 2 (Yaxshi) yoki 3 (Oson) boʻlishi kerak.",
            "Оценка должна быть 0 (Снова), 1 (Трудно), 2 (Хорошо) или 3 (Легко).",
            "The grade must be 0 (Again), 1 (Hard), 2 (Good) or 3 (Easy)."),
        ["review.card_not_found"] = new("Karta topilmadi.", "Карточка не найдена.", "Card not found."),
        ["review.both_sides"] = new("Kartaning ikkala tomoni ham toʻldirilishi kerak.", "Заполните обе стороны карточки.", "Fill in both sides of the card."),
        ["review.phrase_exists"] = new("Bu ibora sizda allaqachon bor.", "Эта фраза у вас уже есть.", "You already have this phrase."),

        ["vocab.word_required"] = new("Soʻz va uning tarjimasi kerak.", "Нужны слово и его перевод.", "A word and its translation are required."),
        ["vocab.exists"] = new("Bu soʻz lugʻatingizda allaqachon bor.", "Это слово уже есть в вашем словаре.", "This word is already in your vocabulary."),
        ["word.invalid"] = new(
            "Soʻz notoʻgʻri: faqat ingliz harflari, koʻpi bilan 40 ta belgi.",
            "Неверное слово: только английские буквы, не более 40 символов.",
            "Invalid word: English letters only, up to 40 characters."),

        ["exercise.not_found"] = new(
            "Mashq topilmadi yoki uning muddati oʻtgan — yangisini oling.",
            "Упражнение не найдено или устарело — начните новое.",
            "Exercise not found or expired — please start a new one."),
        ["exercise.answers_count"] = new(
            "{0} ta javob kutilgan edi, {1} ta keldi.",
            "Ожидалось ответов: {0}, получено: {1}.",
            "Expected {0} answers but got {1}."),
        ["exercise.bad_answer"] = new(
            "{0}-savolga berilgan javob notoʻgʻri.",
            "Некорректный ответ на вопрос {0}.",
            "Invalid answer for question {0}."),

        ["speaking.multipart"] = new("multipart/form-data kutilgan edi.", "Ожидался формат multipart/form-data.", "multipart/form-data was expected."),
        ["speaking.no_audio"] = new("Audio fayl topilmadi yoki u boʻsh.", "Аудиофайл не найден или пуст.", "The audio file is missing or empty."),
        ["speaking.too_big"] = new("Fayl hajmi 10 MB dan katta.", "Размер файла больше 10 МБ.", "The file is larger than 10 MB."),
        ["topic.empty"] = new("Mavzu koʻrsatilmagan.", "Тема не указана.", "The topic is missing."),
        ["text.empty"] = new("Matn boʻsh boʻlishi mumkin emas.", "Текст не может быть пустым.", "The text cannot be empty."),
        ["text.too_short"] = new(
            "Matn juda qisqa (kamida {0} ta belgi kerak).",
            "Текст слишком короткий (нужно не менее {0} символов).",
            "The text is too short (at least {0} characters)."),
        ["text.too_long"] = new(
            "Matn juda uzun (koʻpi bilan {0} ta belgi).",
            "Текст слишком длинный (не более {0} символов).",
            "The text is too long (at most {0} characters)."),

        ["profile.bad_level"] = new("Daraja A2, B1, B2 yoki C1 boʻlishi kerak.", "Уровень должен быть A2, B1, B2 или C1.", "The level must be A2, B1, B2 or C1."),
        ["profile.bad_nickname"] = new(
            "Taxallus 2–30 ta belgidan iborat boʻlsin: harflar, raqamlar, boʻsh joy va . _ - ʼ belgilari.",
            "Никнейм — 2–30 символов: буквы, цифры, пробел и знаки . _ - '.",
            "The nickname must be 2–30 characters: letters, digits, spaces and . _ - '."),
        ["profile.nickname_required"] = new(
            "Musobaqada qatnashish uchun taxallus kiriting.",
            "Чтобы участвовать в соревновании, укажите никнейм.",
            "Enter a nickname to join the league."),
        ["profile.nickname_taken"] = new(
            "Bu taxallus band — boshqasini tanlang.",
            "Этот никнейм занят — выберите другой.",
            "This nickname is taken — please choose another."),
    };

    public static IEnumerable<string> Keys => Map.Keys;

    /// <summary>
    /// Accept-Language'dan birinchi qo'llab-quvvatlanadigan tilni oladi:
    /// "ru-RU,ru;q=0.9,en;q=0.8" → "ru". Topilmasa — o'zbekcha.
    /// </summary>
    public static string ParseLang(string? acceptLanguage)
    {
        if (string.IsNullOrWhiteSpace(acceptLanguage)) return DefaultLang;
        foreach (var part in acceptLanguage.Split(','))
        {
            var tag = part.Split(';')[0].Trim().ToLowerInvariant();
            var baseLang = tag.Split('-')[0];
            if (Langs.Contains(baseLang)) return baseLang;
        }
        return DefaultLang;
    }

    public static string LangOf(HttpRequest request) => ParseLang(request.Headers.AcceptLanguage.ToString());

    public static string Get(string lang, string key, params object?[] args)
    {
        if (!Map.TryGetValue(key, out var e)) return key;
        var template = lang switch { "ru" => e.Ru, "en" => e.En, _ => e.Uz };
        return args.Length == 0 ? template : string.Format(CultureInfo.InvariantCulture, template, args);
    }

    /// <summary>Endpoint'lar uchun qisqartma: <c>request.T("vocab.exists")</c>.</summary>
    public static string T(this HttpRequest request, string key, params object?[] args) =>
        Get(LangOf(request), key, args);

    /// <summary>Qisqartma: <c>{ error = "..." }</c> ko'rinishidagi JSON.</summary>
    public static object Error(this HttpRequest request, string key, params object?[] args) =>
        new { error = request.T(key, args) };
}

/// <summary>
/// Foydalanuvchi kiritgan ma'lumot xato bo'lganda (masalan, javoblar soni mos
/// emas). Xabar o'rniga kalit va parametrlar olib yuradi — endpoint uni
/// so'rov tilida matnga aylantiradi.
/// </summary>
public class UserInputException : ArgumentException
{
    public string Key { get; }
    public object?[] Args { get; }

    public UserInputException(string key, params object?[] args) : base(Texts.Get(Texts.DefaultLang, key, args))
    {
        Key = key;
        Args = args;
    }
}
