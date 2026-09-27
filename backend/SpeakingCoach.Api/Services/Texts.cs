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
        // ---- Telegram bot ----
        ["bot.menu_today"] = new("📋 Bugun", "📋 Сегодня", "📋 Today"),
        ["bot.cmd_today"] = new("Bugungi reja va vazifalar", "План на сегодня и задания", "Today's plan and assignments"),
        ["bot.today_title"] = new("📋 <b>Bugungi reja</b>", "📋 <b>План на сегодня</b>", "📋 <b>Today's plan</b>"),
        ["bot.exam_countdown"] = new("⏳ {0} imtihonigacha {1} kun", "⏳ До экзамена {0}: {1} дн.", "⏳ {1} days to your {0} exam"),
        ["bot.exam_today"] = new("🍀 {0} imtihoni — bugun! Omad!", "🍀 Экзамен {0} — сегодня! Удачи!", "🍀 Your {0} exam is today! Good luck!"),
        ["bot.target"] = new("maqsad: {0}", "цель: {0}", "target: {0}"),
        ["bot.today_no_goal"] = new("Maqsad belgilanmagan — saytda Profil → 🎯 Maqsadim.", "Цель не указана — на сайте: Профиль → 🎯 Моя цель.", "No goal set — on the website: Profile → 🎯 My goal."),
        ["bot.plan_review"] = new("🔁 Takrorlash — {0}/{1} ta karta", "🔁 Повторение — {0}/{1} карточек", "🔁 Review — {0}/{1} cards"),
        ["bot.kind_review"] = new("🔁 {0} ta kartani takrorlash", "🔁 Повторить {0} карточек", "🔁 Review {0} cards"),
        ["bot.kind_speaking"] = new("🎙 Gapirish mashqi", "🎙 Упражнение: говорение", "🎙 Speaking practice"),
        ["bot.kind_writing"] = new("✍️ Yozish mashqi", "✍️ Упражнение: письмо", "✍️ Writing practice"),
        ["bot.kind_reading"] = new("📖 Oʻqish mashqi", "📖 Упражнение: чтение", "📖 Reading practice"),
        ["bot.kind_listening"] = new("🎧 Tinglash mashqi", "🎧 Упражнение: аудирование", "🎧 Listening practice"),
        ["bot.kind_mock"] = new("🏁 {0} {1} — mock imtihon", "🏁 {0} {1} — пробный экзамен", "🏁 {0} {1} mock exam"),
        ["bot.today_progress"] = new("{0}/{1} bajarildi", "выполнено {0} из {1}", "{0} of {1} done"),
        ["bot.today_all_done"] = new("🎉 Bugungi reja bajarildi! Ertaga yana davom etamiz.", "🎉 План на сегодня выполнен! Завтра продолжим.", "🎉 Today's plan is done! See you tomorrow."),
        ["bot.today_tasks"] = new("📚 <b>Oʻqituvchi vazifalari</b>", "📚 <b>Задания учителя</b>", "📚 <b>Teacher assignments</b>"),
        ["bot.task_due"] = new(" · muddat: {0}", " · срок: {0}", " · due: {0}"),
        ["bot.task_overdue"] = new(" · ⚠️ muddati oʻtdi ({0})", " · ⚠️ срок прошёл ({0})", " · ⚠️ overdue ({0})"),
        ["bot.more_tasks"] = new("…yana {0} ta vazifa saytda", "…ещё {0} на сайте", "…{0} more on the website"),
        ["bot.tasks_button"] = new("📚 Vazifalarim", "📚 Мои задания", "📚 My assignments"),
        ["bot.reminder_tasks"] = new("📚 {0} ta vazifa kutmoqda.", "📚 Ждут выполнения заданий: {0}.", "📚 {0} assignments are waiting."),
        ["bot.reminder_tasks_overdue"] = new("📚 {0} ta vazifa kutmoqda, {1} tasining muddati oʻtdi.", "📚 Ждут выполнения заданий: {0}, у {1} срок прошёл.", "📚 {0} assignments are waiting, {1} overdue."),
        ["bot.new_task"] = new("👩‍🏫 <b>{0}</b> ({1}) yangi vazifa berdi:", "👩‍🏫 <b>{0}</b> ({1}) дал(а) новое задание:", "👩‍🏫 <b>{0}</b> ({1}) gave a new assignment:"),
        ["bot.new_task_due"] = new("⏰ Muddat: {0}", "⏰ Срок: {0}", "⏰ Due: {0}"),
        ["bot.task_do"] = new("▶️ Bajarish", "▶️ Выполнить", "▶️ Do it"),
        ["bot.today_button"] = new("📋 Bugungi reja", "📋 План на сегодня", "📋 Today's plan"),
        ["bot.cmd_word"] = new("Kunlik soʻz", "Слово дня", "Word of the day"),
        ["bot.word_of_day"] = new("🌟 <b>Kunlik soʻz</b>", "🌟 <b>Слово дня</b>", "🌟 <b>Word of the day</b>"),
        ["bot.menu_review"] = new("🔁 Takrorlash", "🔁 Повторение", "🔁 Review"),
        ["bot.menu_stats"] = new("📊 Natijalar", "📊 Прогресс", "📊 Progress"),
        ["bot.menu_settings"] = new("⚙️ Sozlamalar", "⚙️ Настройки", "⚙️ Settings"),
        ["bot.menu_help"] = new("❓ Yordam", "❓ Помощь", "❓ Help"),
        ["bot.cmd_review"] = new("Kartalarni takrorlash", "Повторить карточки", "Review your cards"),
        ["bot.cmd_stats"] = new("Seriya va natijalar", "Серия и результаты", "Streak and progress"),
        ["bot.cmd_settings"] = new("Eslatma vaqti va til", "Время напоминания и язык", "Reminder time and language"),
        ["bot.cmd_help"] = new("Bot nima qila oladi", "Что умеет бот", "What the bot can do"),
        ["bot.welcome"] = new(
            "👋 Salom! Men <b>Speaking Coach</b> botiman.\n\n📚 Inglizcha soʻz yuboring — maʼnosini tushuntiraman.\n🔁 Kartalaringizni shu yerning oʻzida takrorlaysiz.\n⏰ Har kuni bitta eslatma — seriya uzilmasin.\n\nToʻliq foydalanish uchun saytdagi hisobingizni ulang: <b>Profil → Telegramni ulash</b>.",
            "👋 Привет! Я бот <b>Speaking Coach</b>.\n\n📚 Пришлите английское слово — объясню, что оно значит.\n🔁 Повторяйте свои карточки прямо здесь.\n⏰ Одно напоминание в день — чтобы серия не прервалась.\n\nЧтобы пользоваться всем, подключите аккаунт на сайте: <b>Профиль → Подключить Telegram</b>.",
            "👋 Hi! I'm the <b>Speaking Coach</b> bot.\n\n📚 Send me an English word and I'll explain it.\n🔁 Review your cards right here in the chat.\n⏰ One reminder a day to keep your streak alive.\n\nTo use everything, connect your account on the website: <b>Profile → Connect Telegram</b>."),
        ["bot.link_button"] = new("🔗 Hisobni ulash", "🔗 Подключить аккаунт", "🔗 Connect account"),
        ["bot.linked"] = new(
            "✅ Hisob ulandi: <b>{0}</b>\n\nEndi pastdagi menyudan foydalaning yoki istalgan inglizcha soʻzni yuboring. Eslatma har kuni soat {1} da keladi — <b>⚙️ Sozlamalar</b>da oʻzgartirasiz.",
            "✅ Аккаунт подключён: <b>{0}</b>\n\nПользуйтесь меню ниже или пришлите любое английское слово. Напоминание будет приходить каждый день в {1} — изменить можно в <b>⚙️ Настройках</b>.",
            "✅ Account connected: <b>{0}</b>\n\nUse the menu below or send any English word. You'll get a reminder every day at {1} — change it in <b>⚙️ Settings</b>."),
        ["bot.link_invalid"] = new(
            "Havola eskirgan yoki notoʻgʻri. Saytda <b>Profil → Telegramni ulash</b> tugmasini qayta bosing.",
            "Ссылка устарела или неверна. Нажмите <b>Профиль → Подключить Telegram</b> на сайте ещё раз.",
            "This link has expired or is invalid. Press <b>Profile → Connect Telegram</b> on the website again."),
        ["bot.need_link"] = new(
            "Buning uchun avval saytdagi hisobingizni ulang.",
            "Для этого сначала подключите аккаунт на сайте.",
            "Please connect your website account first."),
        ["bot.help"] = new(
            "<b>Bot nima qila oladi</b>\n\n📚 Inglizcha soʻz yuboring — maʼnosi, misol va “Lugʻatga qoʻshish” tugmasi.\n🌟 /word — kunlik soʻz.\n📋 <b>Bugun</b> — bugungi reja, imtihongacha kunlar va oʻqituvchi vazifalari.\n🔁 <b>Takrorlash</b> — navbatdagi kartalar: javobni koʻrasiz va qanchalik eslaganingizni belgilaysiz.\n📊 <b>Natijalar</b> — seriya, bugungi maqsad va kartalar.\n⚙️ <b>Sozlamalar</b> — eslatma vaqti, til, hisobni uzish.\n📝 /add — test qoʻshish (oʻqituvchi va admin), /bank — testlar roʻyxati.\n\nHammasi saytdagi hisobingiz bilan bir xil: bu yerda takrorlagan kartangiz saytda ham takrorlangan boʻladi.",
            "<b>Что умеет бот</b>\n\n📚 Пришлите английское слово — значение, пример и кнопка «Добавить в словарь».\n🌟 /word — слово дня.\n📋 <b>Сегодня</b> — план на день, дни до экзамена и задания учителя.\n🔁 <b>Повторение</b> — карточки на сегодня: смотрите ответ и отмечаете, насколько хорошо вспомнили.\n📊 <b>Прогресс</b> — серия, цель на сегодня и карточки.\n⚙️ <b>Настройки</b> — время напоминания, язык, отключение аккаунта.\n📝 /add — добавить тест (учитель и админ), /bank — список тестов.\n\nВсё синхронизировано с сайтом: карточка, повторённая здесь, повторена и на сайте.",
            "<b>What the bot can do</b>\n\n📚 Send an English word — meaning, an example and an “Add to vocabulary” button.\n🌟 /word — word of the day.\n📋 <b>Today</b> — today's plan, days to your exam and teacher assignments.\n🔁 <b>Review</b> — your due cards: see the answer and rate how well you remembered.\n📊 <b>Progress</b> — streak, today's goal and cards.\n⚙️ <b>Settings</b> — reminder time, language, disconnect.\n📝 /add — add a test (teachers and admin), /bank — list of tests.\n\nEverything is synced with the website: a card reviewed here is reviewed there too."),
        ["bot.no_cards"] = new(
            "🗂 Hali kartalar yoʻq. Saytda mashq qiling yoki menga inglizcha soʻz yuboring — ular shu yerga tushadi.",
            "🗂 Карточек пока нет. Позанимайтесь на сайте или пришлите мне английское слово — они появятся здесь.",
            "🗂 No cards yet. Practise on the website or send me an English word — they'll show up here."),
        ["bot.all_done"] = new(
            "🎉 Barakalla! Hozircha takrorlanadigan karta qolmadi.\n🔥 Seriya: {0} kun · bugun {1} ta takrorlash",
            "🎉 Отлично! Карточек для повторения больше нет.\n🔥 Серия: {0} дн. · сегодня повторений: {1}",
            "🎉 Well done! Nothing left to review for now.\n🔥 Streak: {0} days · reviews today: {1}"),
        ["bot.prompt_word"] = new("📚 Soʻzning maʼnosini eslang:", "📚 Вспомните значение слова:", "📚 Recall the meaning:"),
        ["bot.prompt_correction"] = new("✏️ Buni qanday toʻgʻri aytasiz?", "✏️ Как сказать это правильно?", "✏️ How would you say this correctly?"),
        ["bot.prompt_question"] = new("❓ Savolga javob bering:", "❓ Ответьте на вопрос:", "❓ Answer the question:"),
        ["bot.left"] = new("Navbatda: {0} ta", "В очереди: {0}", "In the queue: {0}"),
        ["bot.show_answer"] = new("👀 Javobni koʻrsatish", "👀 Показать ответ", "👀 Show answer"),
        ["bot.how_well"] = new("Qanchalik esladingiz?", "Насколько хорошо вспомнили?", "How well did you remember?"),
        ["bot.grade_0"] = new("😕 Yana", "😕 Снова", "😕 Again"),
        ["bot.grade_1"] = new("😐 Qiyin", "😐 Трудно", "😐 Hard"),
        ["bot.grade_2"] = new("🙂 Yaxshi", "🙂 Хорошо", "🙂 Good"),
        ["bot.grade_3"] = new("😎 Oson", "😎 Легко", "😎 Easy"),
        ["bot.card_missing"] = new("Bu karta endi mavjud emas.", "Этой карточки больше нет.", "This card no longer exists."),
        ["bot.stats"] = new(
            "📊 <b>Natijalaringiz</b>\n\n🔥 Seriya: <b>{0}</b> kun\n🎯 Bugun: {1}/{2} ta takrorlash\n📚 Navbatda: {3} ta karta\n🗂 Jami: {4} ta karta",
            "📊 <b>Ваш прогресс</b>\n\n🔥 Серия: <b>{0}</b> дн.\n🎯 Сегодня: {1}/{2} повторений\n📚 В очереди: {3}\n🗂 Всего карточек: {4}",
            "📊 <b>Your progress</b>\n\n🔥 Streak: <b>{0}</b> days\n🎯 Today: {1}/{2} reviews\n📚 Due: {3} cards\n🗂 Total: {4} cards"),
        ["bot.open_site"] = new("🌐 Saytda ochish", "🌐 Открыть на сайте", "🌐 Open the website"),
        ["bot.settings"] = new(
            "⚙️ <b>Sozlamalar</b>\n\n⏰ Kunlik eslatma: <b>{0}</b>\n🌐 Til: <b>{1}</b>\n\nEslatma vaqtini yoki tilni tanlang:",
            "⚙️ <b>Настройки</b>\n\n⏰ Ежедневное напоминание: <b>{0}</b>\n🌐 Язык: <b>{1}</b>\n\nВыберите время напоминания или язык:",
            "⚙️ <b>Settings</b>\n\n⏰ Daily reminder: <b>{0}</b>\n🌐 Language: <b>{1}</b>\n\nChoose a reminder time or language:"),
        ["bot.reminder_off"] = new("oʻchiq", "выключено", "off"),
        ["bot.reminder_off_button"] = new("🔕 Oʻchirish", "🔕 Выключить", "🔕 Turn off"),
        ["bot.unlink_button"] = new("🔌 Hisobni uzish", "🔌 Отключить аккаунт", "🔌 Disconnect account"),
        ["bot.unlinked"] = new(
            "Hisob uzildi. Qayta ulash uchun saytda <b>Profil → Telegramni ulash</b>.",
            "Аккаунт отключён. Чтобы подключить снова: <b>Профиль → Подключить Telegram</b> на сайте.",
            "Account disconnected. To connect again: <b>Profile → Connect Telegram</b> on the website."),
        ["bot.saved"] = new("Saqlandi ✅", "Сохранено ✅", "Saved ✅"),
        ["bot.reminder_streak"] = new(
            "🔥 Seriyangiz: <b>{0}</b> kun — bugun ham davom ettiramizmi?",
            "🔥 Ваша серия: <b>{0}</b> дн. — продолжим сегодня?",
            "🔥 Your streak: <b>{0}</b> days — keep it going today?"),
        ["bot.reminder_start"] = new(
            "👋 Ingliz tili uchun bugun 2–3 daqiqa ajratamizmi?",
            "👋 Уделим сегодня английскому 2–3 минуты?",
            "👋 Got 2–3 minutes for English today?"),
        ["bot.reminder_due"] = new(
            "📚 {0} ta karta takrorlashni kutmoqda.",
            "📚 Карточек ждут повторения: {0}.",
            "📚 {0} cards are waiting for review."),
        ["bot.reminder_nodue"] = new(
            "📚 Menga yangi inglizcha soʻz yuboring yoki saytda bitta mashq bajaring.",
            "📚 Пришлите мне новое английское слово или сделайте одно упражнение на сайте.",
            "📚 Send me a new English word or do one exercise on the website."),
        ["bot.start_button"] = new("🔁 Boshlash", "🔁 Начать", "🔁 Start"),
        ["bot.add_button"] = new("➕ Lugʻatga qoʻshish", "➕ Добавить в словарь", "➕ Add to vocabulary"),
        ["bot.added"] = new("✅ Lugʻatga qoʻshildi", "✅ Добавлено в словарь", "✅ Added to vocabulary"),
        ["bot.exists"] = new("Bu soʻz lugʻatingizda allaqachon bor", "Это слово уже есть в вашем словаре", "This word is already in your vocabulary"),
        ["bot.guest_hint"] = new(
            "💡 Hisobingizni ulasangiz, soʻzlarni lugʻatga saqlab, takrorlab borasiz.",
            "💡 Подключите аккаунт, чтобы сохранять слова в словарь и повторять их.",
            "💡 Connect your account to save words to your vocabulary and review them."),
        ["bot.not_word"] = new(
            "Menga bitta inglizcha soʻz yuboring (masalan: <i>reliable</i>) — maʼnosini tushuntiraman. Yoki pastdagi menyudan foydalaning.",
            "Пришлите одно английское слово (например: <i>reliable</i>) — я объясню его значение. Или воспользуйтесь меню ниже.",
            "Send me a single English word (e.g. <i>reliable</i>) and I'll explain it. Or use the menu below."),
        ["bot.error"] = new(
            "Nimadir xato ketdi. Birozdan soʻng qayta urinib koʻring.",
            "Что-то пошло не так. Попробуйте немного позже.",
            "Something went wrong. Please try again shortly."),
        // ---- Bot: test qo'shish (/add) ----
        ["bot.cmd_add"] = new("Test qoʻshish (oʻqituvchi va admin)", "Добавить тест (учитель и админ)", "Add a test (teachers and admin)"),
        ["bot.cmd_bank"] = new("Test banki va mening testlarim", "Банк тестов и мои тесты", "Test bank and my tests"),
        ["bot.author_denied"] = new(
            "Test qoʻshish oʻqituvchilar va admin uchun. Saytdagi <b>Oʻqituvchi</b> boʻlimida guruh oching — shundan soʻng /add ishlaydi.",
            "Добавлять тесты могут учителя и админ. Создайте группу в разделе <b>Учитель</b> на сайте — после этого /add заработает.",
            "Adding tests is for teachers and the admin. Create a group in the <b>Teacher</b> section of the website, then /add will work."),
        ["bot.author_pick_exam"] = new(
            "📝 <b>Test qoʻshish</b>\n\nQaysi imtihon uchun?\n\n<i>Faqat oʻzingiz yozgan yoki foydalanish huquqingiz bor materiallarni yuboring. Test tekshirilgach, ilovadagi mock imtihonlarda chiqadi.</i>",
            "📝 <b>Добавление теста</b>\n\nДля какого экзамена?\n\n<i>Присылайте только свои материалы или те, на которые у вас есть права. После проверки тест появится в пробных экзаменах приложения.</i>",
            "📝 <b>Add a test</b>\n\nWhich exam is it for?\n\n<i>Only send material you wrote yourself or have the right to use. Once reviewed, the test appears in the app's mock exams.</i>"),
        ["bot.file_hint"] = new(
            "📎 Fayl oldim. Test qoʻshmoqchi boʻlsangiz, avval /add yozing: imtihon, boʻlim va “Materialim bor”ni tanlang — keyin faylni yuboring.",
            "📎 Файл получен. Чтобы добавить тест, сначала напишите /add: выберите экзамен, раздел и «Есть материал» — затем пришлите файл.",
            "📎 Got your file. To add a test, first send /add: pick the exam, the section and “I have material” — then send the file."),
        ["bot.author_pick_kind"] = new("{0}: qaysi boʻlim?", "{0}: какой раздел?", "{0}: which section?"),
        ["bot.author_pick_mode"] = new(
            "<b>{0}</b>\n\nMaterialingiz bormi yoki Gemini yangi test yaratsinmi?",
            "<b>{0}</b>\n\nУ вас есть материал или Gemini создаст новый тест?",
            "<b>{0}</b>\n\nDo you have material, or should Gemini write a new test?"),
        ["bot.author_mode_src"] = new("📎 Materialim bor (matn, PDF, rasm)", "📎 Есть материал (текст, PDF, фото)", "📎 I have material (text, PDF, photo)"),
        ["bot.author_mode_gen"] = new("✨ Mavzu yozaman — Gemini yaratsin", "✨ Напишу тему — пусть создаст Gemini", "✨ I'll give a topic — Gemini writes it"),
        ["bot.author_cancel_button"] = new("✖️ Bekor qilish", "✖️ Отмена", "✖️ Cancel"),
        ["bot.author_send_topic"] = new(
            "<b>{0}</b>\n\nMavzuni bitta xabarda yozing, masalan: <i>Environment</i> yoki <i>Technology in education</i>.\nBekor qilish: /cancel",
            "<b>{0}</b>\n\nНапишите тему одним сообщением, например: <i>Environment</i> или <i>Technology in education</i>.\nОтмена: /cancel",
            "<b>{0}</b>\n\nSend the topic in one message, e.g. <i>Environment</i> or <i>Technology in education</i>.\nCancel: /cancel"),
        ["bot.author_send_src"] = new(
            "<b>{0}</b>\n\nMaterialni yuboring: matn xabar, PDF yoki rasm (5 MB gacha). Savollar va toʻgʻri javoblar boʻlsa — saqlanadi; yetishmaganini Gemini toʻldiradi va ilova formatiga keltiradi.\nBekor qilish: /cancel",
            "<b>{0}</b>\n\nПришлите материал: текстом, PDF или фото (до 5 МБ). Вопросы и правильные ответы сохранятся; недостающее Gemini допишет и приведёт к формату приложения.\nОтмена: /cancel",
            "<b>{0}</b>\n\nSend the material: a text message, a PDF or a photo (up to 5 MB). Existing questions and answers are kept; Gemini fills in anything missing and converts it to the app's format.\nCancel: /cancel"),
        ["bot.author_listening_note"] = new(
            "🎧 Listening uchun audio emas, <b>matn (skript)</b> yuboring — ilova uni ovoz bilan oʻqib beradi.",
            "🎧 Для Listening пришлите не аудио, а <b>текст (скрипт)</b> — приложение озвучит его.",
            "🎧 For Listening, send the <b>script (text)</b>, not audio — the app reads it aloud."),
        ["bot.author_working"] = new(
            "⏳ Tayyorlanmoqda… Bu 1–3 daqiqa olishi mumkin. Tayyor boʻlgach shu yerga yuboraman.",
            "⏳ Готовлю… Это может занять 1–3 минуты. Пришлю сюда, когда будет готово.",
            "⏳ Working on it… This can take 1–3 minutes. I'll send it here when it's ready."),
        ["bot.author_busy"] = new(
            "⏳ Oldingi test hali tayyorlanmoqda — tugashini kuting.",
            "⏳ Предыдущий тест ещё готовится — дождитесь его.",
            "⏳ Your previous test is still being prepared — please wait for it."),
        ["bot.author_limit"] = new(
            "Bugungi limit tugadi: sutkasiga {0} ta test. Ertaga davom etamiz.",
            "Лимит на сегодня исчерпан: {0} тестов в сутки. Продолжим завтра.",
            "Today's limit is reached: {0} tests per day. Let's continue tomorrow."),
        ["bot.author_bad_file"] = new(
            "Bu fayl turini oʻqiy olmayman. PDF, JPG/PNG rasm yoki .txt yuboring.",
            "Не могу прочитать этот тип файла. Пришлите PDF, фото JPG/PNG или .txt.",
            "I can't read this file type. Send a PDF, a JPG/PNG photo or a .txt file."),
        ["bot.author_too_big"] = new("Fayl juda katta (koʻpi bilan {0} MB).", "Файл слишком большой (не более {0} МБ).", "The file is too large (max {0} MB)."),
        ["bot.author_too_short"] = new(
            "Matn juda qisqa. Toʻliq materialni yuboring yoki /cancel.",
            "Текст слишком короткий. Пришлите материал полностью или /cancel.",
            "The text is too short. Send the full material or /cancel."),
        ["bot.author_topic_bad"] = new(
            "Mavzu 3–{0} belgidan iborat matn boʻlsin (fayl emas).",
            "Тема — текст длиной 3–{0} символов (не файл).",
            "The topic should be text of 3–{0} characters (not a file)."),
        ["bot.author_failed"] = new(
            "❌ Testni tayyorlab boʻlmadi: materialdan toʻliq test chiqmadi yoki AI band. Boshqa material yoki mavzu bilan qayta urinib koʻring (yoki /cancel).",
            "❌ Не удалось подготовить тест: из материала не получился полный тест или AI занят. Попробуйте другой материал или тему (или /cancel).",
            "❌ Couldn't prepare the test: the material didn't make a complete test or the AI is busy. Try other material or a topic (or /cancel)."),
        ["bot.author_reason"] = new("Sabab: {0}", "Причина: {0}", "Reason: {0}"),
        ["bot.author_ready"] = new("✅ <b>{0}</b> — qoralama tayyor.", "✅ <b>{0}</b> — черновик готов.", "✅ <b>{0}</b> — draft ready."),
        ["bot.author_ready_admin"] = new(
            "Toʻliq matn (javoblar bilan) — fayl sifatida quyida. Tekshirib, <b>Chop etish</b>ni bosing — test darhol bankka qoʻshiladi.",
            "Полный текст (с ответами) — файлом ниже. Проверьте и нажмите <b>Опубликовать</b> — тест сразу попадёт в банк.",
            "The full text (with answers) is in the file below. Check it and tap <b>Publish</b> — the test goes straight into the bank."),
        ["bot.author_ready_teacher"] = new(
            "Toʻliq matn (javoblar bilan) — fayl sifatida quyida. Tekshirib, <b>Adminga yuborish</b>ni bosing — tasdiqlangach bankka qoʻshiladi.",
            "Полный текст (с ответами) — файлом ниже. Проверьте и нажмите <b>Отправить админу</b> — после одобрения тест попадёт в банк.",
            "The full text (with answers) is in the file below. Check it and tap <b>Send to admin</b> — it joins the bank once approved."),
        ["bot.author_publish"] = new("✅ Chop etish", "✅ Опубликовать", "✅ Publish"),
        ["bot.author_submit"] = new("📨 Adminga yuborish", "📨 Отправить админу", "📨 Send to admin"),
        ["bot.author_delete"] = new("🗑 Oʻchirish", "🗑 Удалить", "🗑 Delete"),
        ["bot.author_approve"] = new("✅ Tasdiqlash", "✅ Одобрить", "✅ Approve"),
        ["bot.author_reject"] = new("❌ Rad etish", "❌ Отклонить", "❌ Reject"),
        ["bot.author_view"] = new("👁 Koʻrish", "👁 Открыть", "👁 View"),
        ["bot.author_cancelled"] = new("Bekor qilindi.", "Отменено.", "Cancelled."),
        ["bot.author_published"] = new(
            "✅ Chop etildi — test endi mock imtihonlarda chiqadi.",
            "✅ Опубликовано — тест теперь появляется в пробных экзаменах.",
            "✅ Published — the test now appears in mock exams."),
        ["bot.author_submitted"] = new(
            "📨 Adminga yuborildi. Tasdiqlansa, shu yerda xabar beraman.",
            "📨 Отправлено админу. Сообщу здесь, когда тест одобрят.",
            "📨 Sent to the admin. I'll let you know here once it's approved."),
        ["bot.author_deleted"] = new("🗑 Oʻchirildi.", "🗑 Удалено.", "🗑 Deleted."),
        ["bot.author_rejected"] = new("❌ Rad etildi, muallifga xabar berildi.", "❌ Отклонено, автор уведомлён.", "❌ Rejected, the author has been notified."),
        ["bot.author_not_allowed"] = new(
            "Bu amalni bajarib boʻlmaydi (holat oʻzgargan yoki ruxsat yoʻq).",
            "Это действие недоступно (статус изменился или нет прав).",
            "This action isn't available (the status changed or you lack permission)."),
        ["bot.author_review"] = new(
            "📥 <b>Tekshiruvga yangi test keldi</b>\nMuallif: {0}",
            "📥 <b>Новый тест на проверку</b>\nАвтор: {0}",
            "📥 <b>A new test is waiting for review</b>\nAuthor: {0}"),
        ["bot.author_approved_note"] = new(
            "🎉 Testingiz tasdiqlandi va bankka qoʻshildi: <b>{0}</b>",
            "🎉 Ваш тест одобрен и добавлен в банк: <b>{0}</b>",
            "🎉 Your test was approved and added to the bank: <b>{0}</b>"),
        ["bot.author_rejected_note"] = new(
            "Testingiz rad etildi: <b>{0}</b>. Materialni tuzatib, /add bilan qayta yuborishingiz mumkin.",
            "Ваш тест отклонён: <b>{0}</b>. Можно исправить материал и отправить снова через /add.",
            "Your test was rejected: <b>{0}</b>. You can fix the material and send it again with /add."),
        ["bot.bank_title"] = new(
            "📚 <b>Test banki</b> — chop etilgan (tekshiruvda)",
            "📚 <b>Банк тестов</b> — опубликовано (на проверке)",
            "📚 <b>Test bank</b> — published (in review)"),
        ["bot.bank_static"] = new(
            "Speaking va Writing uchun ilovada yana tayyor variantlar bor.",
            "Для Speaking и Writing в приложении есть ещё встроенные варианты.",
            "Speaking and Writing also have built-in sets in the app."),
        ["bot.bank_pending"] = new("📥 Tekshiruvni kutmoqda: {0} ta", "📥 Ждут проверки: {0}", "📥 Waiting for review: {0}"),
        ["bot.bank_mine"] = new("📚 <b>Mening testlarim</b>", "📚 <b>Мои тесты</b>", "📚 <b>My tests</b>"),
        ["bot.bank_empty"] = new("Hali test qoʻshmagansiz. /add bilan boshlang.", "Вы ещё не добавляли тестов. Начните с /add.", "You haven't added any tests yet. Start with /add."),
        ["bot.status_published"] = new("chop etilgan", "опубликован", "published"),
        ["bot.status_draft"] = new("qoralama", "черновик", "draft"),
        ["bot.status_pending"] = new("tekshiruvda", "на проверке", "in review"),
        ["bot.status_rejected"] = new("rad etilgan", "отклонён", "rejected"),
        ["login.required"] = new("Tizimga kiring.", "Войдите в систему.", "Please log in."),
        ["group.not_found"] = new("Guruh yoki vazifa topilmadi.", "Группа или задание не найдены.", "Group or assignment not found."),
        ["group.name_invalid"] = new("Guruh nomi 2–80 belgidan iborat boʻlsin.", "Название группы — от 2 до 80 символов.", "The group name must be 2–80 characters."),
        ["group.limit"] = new("Koʻpi bilan {0} ta guruh ochish mumkin.", "Можно создать не более {0} групп.", "You can create up to {0} groups."),
        ["group.full"] = new("Guruh toʻla ({0} ta oʻquvchi).", "Группа заполнена ({0} учеников).", "The group is full ({0} students)."),
        ["group.student_limit"] = new("Koʻpi bilan {0} ta guruhga qoʻshilish mumkin.", "Можно состоять не более чем в {0} группах.", "You can join up to {0} groups."),
        ["group.bad_code"] = new("Bunday taklif kodi topilmadi. Oʻqituvchingizdan yangi havola soʻrang.", "Такой код приглашения не найден. Попросите у учителя новую ссылку.", "This invite code wasn’t found. Ask your teacher for a new link."),
        ["group.own_group"] = new("Bu sizning guruhingiz — siz uning oʻqituvchisisiz.", "Это ваша группа — вы её учитель.", "This is your own group — you are its teacher."),
        ["assignment.bad_kind"] = new("Vazifa turi notoʻgʻri.", "Неверный тип задания.", "Invalid assignment type."),
        ["assignment.bad_target"] = new("Kartalar soni {0} dan {1} gacha boʻlsin.", "Количество карточек — от {0} до {1}.", "The number of cards must be between {0} and {1}."),
        ["assignment.bad_due"] = new("Muddat notoʻgʻri: u kelajakda va bir yil ichida boʻlsin.", "Неверный срок: он должен быть в будущем и в пределах года.", "Invalid due date: it must be in the future and within a year."),
        ["assignment.limit"] = new("Bitta guruhda koʻpi bilan {0} ta vazifa.", "В одной группе не более {0} заданий.", "Up to {0} assignments per group."),
        ["mock.login"] = new(
            "Mock imtihon natijasi saqlanadi — buning uchun tizimga kiring.",
            "Результат пробного экзамена сохраняется — для этого войдите в систему.",
            "Mock exam results are saved, so please log in first."),
        ["mock.limit"] = new(
            "Bir sutkada {0} ta Speaking/Writing mock imtihonini topshirish mumkin (Listening va Reading cheklanmagan).",
            "Лимит пробных экзаменов Speaking/Writing за сутки: {0} (Listening и Reading без ограничений).",
            "You can take {0} Speaking/Writing mock exams per 24 hours (Listening and Reading are unlimited)."),
        ["mock.generate_limit"] = new(
            "Bugun yangi test yaratish limiti tugadi. Ertaga yana urinib koʻring yoki Speaking/Writing mock imtihonini topshiring.",
            "Лимит создания новых тестов на сегодня исчерпан. Попробуйте завтра или сдайте Speaking/Writing.",
            "Today's limit for creating new tests is used up. Try again tomorrow, or take a Speaking/Writing mock."),
        ["mock.bad_set"] = new(
            "Imtihon topilmadi. Sahifani yangilab, qaytadan boshlang.",
            "Экзамен не найден. Обновите страницу и начните заново.",
            "Exam not found. Refresh the page and start again."),
        ["mock.no_answers"] = new(
            "Birorta javob yozib olinmadi — baholash uchun kamida bitta savolga javob bering.",
            "Не записано ни одного ответа — ответьте хотя бы на один вопрос.",
            "No answers were recorded — answer at least one question to get a score."),
        ["mock.too_big"] = new(
            "Audio hajmi juda katta ({0} MB dan oshmasin).",
            "Слишком большой объём аудио (не более {0} МБ).",
            "The audio is too large (max {0} MB)."),
        ["mock.empty_text"] = new(
            "Ikkala vazifa ham boʻsh — baholash uchun kamida bittasini yozing.",
            "Оба задания пустые — напишите хотя бы одно.",
            "Both tasks are empty — write at least one to get a score."),
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

        ["goal.bad_goal"] = new("Maqsadni tanlang: IELTS, CEFR yoki umumiy ingliz tili.", "Выберите цель: IELTS, CEFR или общий английский.", "Choose a goal: IELTS, CEFR or general English."),
        ["goal.bad_target"] = new("Bu maqsad ball tanlangan imtihonga mos emas.", "Этот целевой балл не подходит к выбранному экзамену.", "This target score does not match the chosen exam."),
        ["goal.bad_date"] = new("Imtihon sanasi bugundan keyingi 2 yil ichida boʻlishi kerak.", "Дата экзамена должна быть в ближайшие 2 года, не в прошлом.", "The exam date must be within the next 2 years, not in the past."),
        ["goal.bad_minutes"] = new("Kunlik vaqt 10, 20, 30 yoki 45 daqiqa boʻlishi kerak.", "Время в день: 10, 20, 30 или 45 минут.", "Daily time must be 10, 20, 30 or 45 minutes."),
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
