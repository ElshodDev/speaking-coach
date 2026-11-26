# Ishga tushirish va deploy (egasi uchun eslatma)

README ilova nima va qanday qurilganini tushuntiradi. Bu sahifa esa loyihani o'zi ishga tushiradigan odam uchun: sozlamalar ro'yxati va buyruqlar.

## Lokal

Kerak: .NET 10 SDK, Node 22, [Gemini API kaliti](https://aistudio.google.com/apikey), [Neon](https://neon.tech) Postgres bazasi.

```bash
# backend
cd backend/SpeakingCoach.Api
dotnet user-secrets init
dotnet user-secrets set "Gemini:ApiKey" "<kalit>"
dotnet user-secrets set "ConnectionStrings:Default" "Host=...;Database=...;Username=...;Password=...;SSL Mode=Require"
dotnet user-secrets set "Admin:Emails" "siz@example.com"   # ixtiyoriy: admin panel kimga ko'rinadi
dotnet tool update --global dotnet-ef
# Migratsiyalar Infrastructure loyihasida, sozlamalar esa Api'da (startup):
dotnet ef database update --project ../SpeakingCoach.Infrastructure --startup-project .
dotnet run                      # http://localhost:5000

# frontend (ikkinchi terminal)
cd frontend/speaking-coach-web
npm ci
npm run dev                     # http://localhost:5173
```

Testlar:

```bash
dotnet test backend/SpeakingCoach.slnx     # barcha qatlamlar + arxitektura testlari
cd frontend/speaking-coach-web && npm test
```

## Analitika (Plausible, ixtiyoriy)

Cookie'siz, shaxsiy ma'lumotsiz statistika. Yoqish: Plausible'da `fluentuz.app` saytini qo'shing, Vercel'da `VITE_PLAUSIBLE_DOMAIN=fluentuz.app` o'rnating va Redeploy qiling. **O'chirish (feature flag): shu o'zgaruvchini olib tashlab qayta deploy.** Do Not Track yoqilgan brauzerlarga hech narsa yuborilmaydi; foydalanuvchi o'zi `localStorage["sc.analytics"]="off"` qilib o'chira oladi.

Hodisalar (`frontend/speaking-coach-web/src/analytics.ts`, qat'iy ro'yxat): `pageview` (faqat bo'lim nomi: `join/KOD` → `join`), `signup` (onboarding birinchi ko'rilganda, brauzerga bir marta), `first_practice_started`, `first_practice_completed`, `review_completed`, `mock_started`, `mock_completed`, `mic_consent_accepted`, `pwa_installed`. Xossalar: faqat `kind`/`section` (masalan `speaking`, `ielts-listening`). Plausible'da Goals bo'limiga shu nomlarni qo'shing.

**Retention (D1/D7):** `GET /api/admin/retention?days=30` (faqat admin, `Authorization: Bearer <token>`; `days` 1–90). Mavjud jadvallardan hisoblanadi (`Users`, `Activities`, `ReviewLogs`) — sxema o'zgarmaydi, demo hisoblar hisobga olinmaydi. Ta'rif: ro'yxatdan o'tgan kundan keyin aynan 1-/7-kuni (Toshkent vaqti) mashq qilgan yoki karta takrorlagan foydalanuvchilar ulushi; tugamagan kun `null`. Javobda umumiy foiz va kunlik guruhlar (cohorts) bor.

## Xato monitoringi (Sentry, ixtiyoriy)

Ikki loyiha oching (Sentry'da: .NET va Browser/JavaScript) va DSN'larni qo'ying. **O'chirish (feature flag): o'zgaruvchini olib tashlash.**

| Qayerda | O'zgaruvchi |
|---|---|
| Render (backend) | `Sentry__Dsn` (ixtiyoriy: `Sentry__Environment`) |
| Vercel (frontend) | `VITE_SENTRY_DSN` (ixtiyoriy: `VITE_SENTRY_ENV`, `VITE_SENTRY_RELEASE`); keyin Redeploy |

Maxfiylik: ovoz va so'rov tanasi, sarlavhalar, cookie, so'rov qismi (`?...`), foydalanuvchi va breadcrumbs yuborilmaydi; xato matnidagi email/token niqoblanadi; Gemini noto'g'ri javobidan nutq parchasi ("Javob: ...") kesiladi. Qoidalar: `backend/SpeakingCoach.Api/Web/SentryScrubber.cs` va `frontend/speaking-coach-web/src/monitoring.ts` (ikkalasi testlangan). Kutilgan holatlar (AI band 503, katta so'rov 413, bekor qilingan so'rov) xato sifatida yuborilmaydi. Frontend'da Sentry kutubxonasi kech (dinamik) yuklanadi va faqat DSN berilgan build'ga qo'shiladi (~130 kB gzip alohida bo'lak). Performance (traces) va replay o'chiq.

## Mikrofon roziligi

Birinchi yozishdan oldin "ovoz Google Gemini'ga yuboriladi, saqlanmaydi" roziligi so'raladi (UZ/RU/EN; qurilmada `localStorage["sc.micConsent.v1"]`). Hamma mikrofon ishlatiladigan joy (Gapirish, Part 2, Mock Speaking, AI suhbat, Shadowing) `requestMicConsent()` dan o'tadi. **O'chirish (feature flag): Vercel'da `VITE_MIC_CONSENT=off` va Redeploy.** Matn maxfiylik siyosati (`public/privacy.html`) bilan bir xil bo'lishi kerak.

## Feature flag'lar (backend, standart — yoqiq)

| Render o'zgaruvchisi | Nima qiladi |
|---|---|
| `Features__SpeakingFourCriteria` | Gapirish natijasi: 4 rasmiy IELTS mezoni (taxminiy band), talaffuz ("taxminiy") va bitta asosiy tuzatish. `false` — eski 3 mezonli ko'rinish. |
| `Features__PersonalPodcasts` | Shaxsiy podcast (Telegram `/podcast`): istalgan foydalanuvchi YouTube videoni o'ziga shadowing darsi qiladi (faqat o'zi ko'radi). `false` — o'chiq. Standart — yoqiq. |
| `Features__AiTutor` | AI ustoz (veb: Mashqlar → "AI ustoz"; Telegram: `/dars`): mavzu yozib mini-dars. `false` — o'chiq (`/api/tutor/*` 404, botda "o'chirilgan"). Standart — yoqiq. |
| `Features__DayOneDiagnostic` | Birinchi kunlik boshlang'ich test: bosh sahifada 4 ta qisqa mashq kartasi va tayyorlik bahosi birinchi kundan (Reading/Listening — ilovaning tayyor mashqlari darajasi bo'yicha past ishonch bilan). `false` — o'chiq, tayyorlik avvalgidek faqat mock bo'yicha. |

O'chirish uchun qiymatni `false` qiling va servisni qayta ishga tushiring. Yangi maydonlar mavjud javobga **qo'shimcha** (ixtiyoriy) — eski mijozlar va eski yozuvlar buzilmaydi.

## Render (backend)

Root directory: **`backend`**, Dockerfile path: **`./Dockerfile`**, Docker build context directory: **`.`** (tasdiqlangan ishlaydigan sozlama, 2026-11). Uchalasi bir-biriga mos bo'lishi shart: Root Directory o'rnatilsa, Render o'sha papkani loyiha ildizi qiladi va qolgan yo'llar unga nisbatan o'qiladi. Xatolar: `failed to read dockerfile: open Dockerfile` — yo'l noto'g'ri (masalan, bo'sh joy bilan ikkita yo'l yozilgan); `lstat .../src/backend: no such file` — Root Directory `backend` va Context ham `backend` qilib qo'yilgan (yo'l ikki marta qo'shiladi). Build yiqilsa, Render **eski versiyani ishlatishda davom etadi** — yangi kod jonli bo'lmaydi; tekshirish: `GET /api/referral` `401` bersa yangi backend, `404` bo'lsa eski. Docker. (Backend 4 ta loyihadan iborat — qarang: [ARCHITECTURE.md](ARCHITECTURE.md) — shuning uchun build konteksti butun `backend` papkasi.) **Health check path: `/health/live`** (bazaga tegmaydi — Neon uxlab qolganda Render serverni behuda qayta ishga tushirmaydi; `/health` esa bazani ham tekshiradi, monitoring uchun). Environment:

| O'zgaruvchi | Nima uchun |
|---|---|
| `Gemini__ApiKey` | Gemini kaliti (majburiy) |
| `ConnectionStrings__Default` | Neon ulanish satri (majburiy) |
| `FrontendOrigin` | Sayt manzili — CORS va bot havolalari uchun. Bir nechta bo'lsa vergul bilan, birinchisi asosiy: `https://fluentuz.app,https://speaking-coach-theta.vercel.app` (eski manzil ham ishlab tursin) |
| `Admin__Emails` | Sayt adminlari emaillari, vergul bilan |
| `Email__BrevoApiKey`, `Email__FromAddress` | Email tasdiqlashni yoqadi (Brevo'da tasdiqlangan yuboruvchi) |
| `Email__FromName` | Xatdagi yuboruvchi nomi (standart: `FluentUZ`) |
| `Email__DailyCap` | Kuniga ko'pi bilan nechta xat (standart: 250; Brevo bepul tarifi — 300). Tugasa, sayt Telegram yoki Google orqali kirishni taklif qiladi |
| `Google__ClientId` | Google orqali kirish (Web OAuth client, Vercel manzili ruxsat etilgan origin) |
| `Telegram__BotToken`, `Telegram__CronSecret` | Telegram bot, soatlik eslatmalar va **saytga Telegram orqali kirish** (token bo'lsa o'zi yoqiladi) |
| `Telegram__BotUsername` | Bot nomi (standart: `SpeakingCoachUzBot`) — kirish havolasi `t.me/<nom>` |
| `Telegram__PublicUrl` | Webhook uchun server manzili (bo'lmasa Render'ning `RENDER_EXTERNAL_URL`) |
| `Telegram__Admins` | Botdagi adminlar — faqat raqamli Telegram ID, vergul bilan (botga `/id` yozib bilasiz) |
| `Demo__Enabled` | `false` — demo hisobni o'chiradi (standart: yoqiq) |
| `Ai__ExercisesPerDay`, `Ai__WordsPerDay`, `Ai__ShadowingPerDay`, `Ai__Guest*PerDay` | Foydalanuvchi/mehmon uchun kunlik AI limitlari (ixtiyoriy) |
| `Ai__TalkTurnsPerDay` | AI suhbatda kuniga nechta javob (standart: 40) |
| `Ai__PodcastsPerDay` | Shaxsiy podcast: kuniga nechta video tayyorlash (standart: 3; admin — cheksiz). Har video 1 ta katta Gemini so'rovi (video tinglanadi) |
| `Ai__TutorCallsPerDay` | AI ustozda kuniga nechta savol/yozma baholash (standart: 30; darsning o'zi — oddiy mashq limitidan) |
| `Ai__AuthoringPerDay` | Bot orqali test tayyorlash, kuniga (standart: 10) |
| `Ai__GlobalPerMinute`, `Ai__GlobalPerDay` | Butun server bo'yicha Gemini so'rovlari: daqiqasiga 12, kuniga 1400 (Toshkent kuni). Oshsa — foydalanuvchiga "AI hozir band" (503) |
| `Ai__BreakerFailures`, `Ai__BreakerSeconds`, `Ai__QuotaCooldownMinutes` | Gemini ketma-ket xato qilsa — 5 xatodan keyin 60 s dam; kvotasi tugagan model 30 daqiqa chetlab o'tiladi |
| `Ai__Models` | Gemini modellari, vergul bilan, tartib bo'yicha (standart kodda) |
| `Mock__PerDay`, `Mock__GeneratePerDay` | Mock imtihon baholash va AI test tuzish limitlari |

AI limitlari Toshkent vaqti bilan yarim tunda yangilanadi.

GitHub → Settings → Secrets → Actions: `API_URL` (Render manzili) va `CRON_SECRET` (`Telegram__CronSecret` bilan bir xil) — eslatmalar workflow'i uchun. Eslatma: repoda 60 kun commit bo'lmasa, GitHub rejalashtirilgan workflow'larni o'chirib qo'yadi — Actions sahifasida qayta yoqing.

**Muhim — GitHub'ning soatlik cron'i ishonchsiz.** Bepul repolarda GitHub rejalashtirilgan workflow'larni kechiktiradi va tashlab yuboradi: 2026-noyabrda "har soatda" o'rniga kuniga 3–4 marta ishlagan, kechqurungi (masalan 20:00) eslatmalar 11 kunning 7 tasida umuman yuborilmagan. Ishonchli yo'l — tashqi bepul cron xizmati, masalan [cron-job.org](https://cron-job.org):

1. Yangi cron job: URL `https://<Render manzili>/api/telegram/cron`, **Every hour**, minut — `7`.
2. Advanced → Request method: **POST**; Headers: `X-Cron-Secret: <Telegram__CronSecret qiymati>`.
3. Saqlang va "Test run" — javob `202` (`{"started":true}`) bo'lishi kerak.

Ikki manbadan chaqirish xavfsiz: bir kunda bitta foydalanuvchiga faqat bitta eslatma ketadi (LastReminderDate), parallel yurish esa bittaga cheklangan. GitHub workflow'ini zaxira sifatida qoldirish mumkin.

## Vercel (frontend)

Root directory: `frontend/speaking-coach-web`; environment: `VITE_API_URL` (Render manzili) va `VITE_SITE_URL` (saytning o'z manzili — Telegram/Instagram havola ko'rinishidagi rasm uchun; hozir `https://fluentuz.app`). Domen o'zgarsa, `VITE_SITE_URL` va Render'dagi `FrontendOrigin`ni yangilang. Xavfsizlik sarlavhalari `vercel.json` da.

**Content-Security-Policy** (`vercel.json`): API faqat `https://*.onrender.com` dan chaqirilishi mumkin. Backend boshqa manzilga (o'z domeni, Railway…) ko'chsa, uni `connect-src` ga qo'shing — aks holda `vite build` ataylab to'xtaydi (`VITE_API_URL` CSP'ga mos emasligini aytadi), sayt API'ga ulana olmay qolmasin.

## Migratsiyalar

Render ularni o'zi bajarmaydi: sxemaga bog'liq kodni push qilishdan oldin lokalda `dotnet ef database update --project backend/SpeakingCoach.Infrastructure --startup-project backend/SpeakingCoach.Api` bajariladi. Yangi migratsiya: `dotnet ef migrations add <Nom> --project backend/SpeakingCoach.Infrastructure --startup-project backend/SpeakingCoach.Api`.

`AddNicknameUniqueIndex` — taxalluslar uchun `lower("DisplayName")` noyob indeksi. Bazada allaqachon takroriy taxalluslar bo'lsa, migratsiya yiqilmaydi, faqat indeksni yaratmaydi (NOTICE). Tekshirish: `SELECT lower("DisplayName"), count(*) FROM "Users" WHERE "DisplayName" IS NOT NULL GROUP BY 1 HAVING count(*) > 1;` — takrorlarni tuzatib, migratsiyani qayta qo'llang (`dotnet ef database update 20260928090000_AddTelegramLoginAndFeedback`, keyin yana `dotnet ef database update`).

## Nechta foydalanuvchi ro'yxatdan o'tgan

- **Sayt:** `Admin__Emails` dagi email bilan kiring → Profil → **Admin panel** (`#/admin`). U yerda: jami va email tasdiqlangan foydalanuvchilar, bugun va shu haftadagi yangilar, bugun/7/30 kunda faollar, kunlik grafik. Demo hisoblar hisobga kirmaydi.
- **Telegram:** botga `/admin` yozing (faqat `Telegram__Admins` dagi raqamli ID yoki admin emaili bilan ulangan hisob). Bot o'sha sonlarni qisqa xabarda yuboradi.
- Admin panelda **qanday ro'yxatdan o'tishgan** jadvali bor: Telegram (sayt), Telegram Mini App, Google, email kodi, email + parol. Eski hisoblar (bu statistika qo'shilgunga qadar ochilgan) "Boshqa / eski" qatorida. Yana: nechta hisobga Telegram ulangan va ulardan nechtasi 7 kunda botda faol bo'lgan.
- "Email tasdiqlangan" soni faqat haqiqiy emaillarni sanaydi (Telegram hisobining ichki `tg-…@telegram.invalid` manzili hisobga kirmaydi). Kod so'rab, uni kiritmagan va hech narsa qilmagan yozuvlar 7 kundan keyin avtomatik o'chiriladi.
- **Fikrlar:** saytning pastidagi "Fikr bildirish" tugmasi orqali yozilganlar admin panelning oxirida ko'rinadi; `Telegram__Admins` dagi adminlarga bot darhol xabar ham yuboradi.

## Domen (fluentuz.app)

Domen Name.com'da (Student Pack, 2027-09-28 gacha; avtomatik yangilanish o'chiq). DNS Name.com'ning o'zida:

| Type | Host | Qiymat | Nima uchun |
|---|---|---|---|
| A | (bo'sh) | Vercel → Settings → Domains ko'rsatgan IP | `fluentuz.app` → sayt |
| CNAME | `www` | Vercel ko'rsatgan `…vercel-dns…` manzil | `www` → 308 bilan `fluentuz.app` ga |

Domen o'zgarsa, uchta joyni yangilang: Render'dagi `FrontendOrigin` (birinchisi — asosiy), Vercel'dagi `VITE_SITE_URL` (keyin Redeploy) va Google Cloud Console → OAuth client → Authorized JavaScript origins.

## Landing, SEO (Partiya C)

- `frontend/speaking-coach-web/index.html`: sarlavha **FluentUZ**, meta description, Open Graph/Twitter, JSON-LD (`WebApplication`) va `#root` ichida statik landing matni. Qidiruv robotlari va JS yuklanguncha shu ko'rinadi; React ishga tushgach ilova uning o'rnini oladi. Router'ga tegilmagan.
- `robots.txt` va `sitemap.xml` build paytida yoziladi (`vite.config.ts` → `src/seo.ts`), manzil `VITE_SITE_URL` dan. Sitemap'da faqat `/`, `/privacy.html`, `/terms.html` (hash marshrutlar alohida sahifa emas).
- PWA nomi: `manifest.webmanifest` → "FluentUZ". Ilova ichidagi sarlavha, ulashish kartasi, maxfiylik/shartlar sahifalari, email va Telegram matnlari ham "FluentUZ".
- Qaytarish: `index.html` va `manifest.webmanifest` ni oldingi holatga qaytarish kifoya (funksiya yoki ma'lumotga ta'sir yo'q).

## PWA o'rnatish va yangilanish

Foydalanuvchilar FluentUZ'ni telefon brauzeri menyusidan **Install app / Add to Home Screen** orqali o'rnatishi mumkin; do'konga joylash yoki qayta o'rnatish shart emas. Vercel'ga yangi frontend deploy qilinganda ilova internetga ulangan paytda yangi versiyani oladi. Ilova ochiq turgan vaqtda yangilanish tayyor bo'lsa, **Yangilash** tugmasi chiqadi; foydalanuvchi bosganda sahifa yangi versiya bilan qayta ochiladi. Oflaynda oxirgi keshdagi versiya ishlashda davom etadi.

## Oflayn takrorlash

Internet bo'lmaganda ham bugungi kartalarni takrorlash mumkin (faqat frontend; API va baza o'zgarmagan).
- **Qanday ishlaydi:** Takrorlash sahifasi internet bor paytda ochilganda bugungi muddati kelgan kartalar (`GET /api/review/due`, 50 tagacha) qurilmada (`localStorage["sc.offReview.v1"]`) saqlanadi. Oflaynda shu kartalar ko'rsatiladi, baholar navbatga yoziladi va internet qaytganda ketma-ket yuboriladi (server takror/erta baholarni o'zi e'tiborsiz qoldiradi, shuning uchun qayta yuborish xavfsiz).
- **Chegaralar:** faqat bugungi kartalar (ertasi kuni kartalar o'chadi, yuborilmagan baholar saqlanadi); faqat bitta hisob uchun; chiqishda hammasi o'chadi; oflaynda karta qo'shish/o'chirish va statistika ishlamaydi. Sahifa oflaynda ochilishi uchun Takrorlashni bir marta internet bilan ochgan bo'lish kerak (service worker kodni keshlaydi).
- **Qaytarish (feature flag):** Vercel'da `VITE_OFFLINE_REVIEW=off` va Redeploy — hech narsa saqlanmaydi, eskisi o'chadi.
- **Diqqat:** baho oflaynda berilgan vaqt emas, serverga yetib kelgan vaqt bo'yicha hisoblanadi (oraliqlar bir necha soatga siljishi mumkin).


## Yangi menyu ("Kundalik" dizayni) — VITE_NEW_NAV

- **Nima o'zgaradi:** pastda 5 tugma (Asosiy · Takrorlash · AI ustoz (markazda) · Mashqlar · Natijalar); Lug'at Takrorlash ichida "Kartalar | Lug'at" bo'lib turadi (eski `#/vocab` havolasi ishlayveradi); tepada sarlavha, seriya, **Fikr**, **Hammasi** (qidiruvli, barcha bo'limlar) va avatar. Rang va shakllar — `src/newNav.css` (faqat `.app.nn` ichida).
- **Yoqish:** Vercel'da `VITE_NEW_NAV=true` va Redeploy. Bayroq berilmasa yoki `true` bo'lmasa, hozirgi menyu o'zgarishsiz qoladi.
- **Qaytarish:** o'zgaruvchini o'chirib, Redeploy qiling. Baza, API va kirish tizimiga tegilmagan.

## Taklif (referral)

Doʻstni taklif qilish: profilda havola (`https://fluentuz.app/?ref=KOD`), doʻst shu havola bilan roʻyxatdan oʻtsa (email yoki Google), **ikkalasiga ham 7 kun davomida kuniga +5 ta qoʻshimcha AI mashq** beriladi.

- **MIGRATSIYA AVVAL (muhim):** bu yangilanish `Users` jadvaliga nullable `ReferredByUserId` ustuni va indeks qoʻshadi. Backend'ni Render'ga deploy qilishdan **oldin** Neon'da migratsiyani qoʻllang: `dotnet ef database update --project backend/SpeakingCoach.Infrastructure --startup-project backend/SpeakingCoach.Api` (yoki `dotnet ef migrations script 20261120120000_AddNicknameUniqueIndex -i` chiqargan SQL'ni Neon konsolida ishga tushiring). Aks holda yangi kod mavjud boʻlmagan ustunni soʻraydi va hamma narsa 500 beradi.
- **Feature flag:** Render'da `Features__Referral=false` — taklif kodi qabul qilinmaydi, mukofot berilmaydi, profilda karta koʻrinmaydi (`GET /api/referral` → `enabled:false`). Standart — yoqiq.
- **Qaytarish (rollback):** 1) `Features__Referral=false`; 2) kerak boʻlsa kodni oldingi versiyaga qaytaring; 3) shundan keyingina ustunni oʻchiring: `dotnet ef database update 20261120120000_AddNicknameUniqueIndex` (yoki `DROP INDEX "IX_Users_ReferredByUserId"; ALTER TABLE "Users" DROP COLUMN "ReferredByUserId";`). Ustun oʻchirilgach yangi kod ishlamaydi — tartibni buzmang.
- **API (additiv):** `GET /api/referral` (yangi); `POST /api/auth/register` va `/api/auth/google` ixtiyoriy `ref` maydonini qabul qiladi. Mavjud maydonlar oʻzgarmagan.
- **Hisoblash (bazada alohida mukofot jadvali yoʻq):** kod = foydalanuvchi ID'sining base64url koʻrinishi (22 belgi). Taklif qilingan hisob uchun mukofot — hisob ochilganidan 7 kun; taklif qilgan uchun — oxirgi 7 kunda **emaili tasdiqlangan** doʻsti qoʻshilgan boʻlsa. Mukofotlar **qoʻshilmaydi** (nechta doʻst boʻlsa ham +5), shuning uchun suiisteʼmol chegaralangan. Demo hisoblar qatnashmaydi.
- **Telegram orqali kirish:** hisob ochilgandan keyin brauzer saqlangan kodni `POST /api/referral/claim {ref}` bilan yuboradi (kod hisob ochilgan paytda serverga yetmaydi). Server faqat Telegram orqali **1 soat ichida** ochilgan, hali taklif biriktirilmagan hisobga biriktiradi (atomar, bir marta); eski hisob, noto'g'ri kod, demo yoki `Features__Referral=false` bo'lsa — `{claimed:false}`. Yangi migratsiya **kerak emas** (xuddi shu `ReferredByUserId` ustuni). Saytdan "Telegram orqali kirish" va Mini App (`/api/auth/telegram`) — ikkalasi ham shu yo'ldan.
- **Hozircha qamrab olinmagan:** Mini App ichida ochilganda `?ref=` bo'lmasa (masalan, botning menyu tugmasidan) kod yo'q — `t.me/bot/app?startapp=...` orqali taklif havolasi hali qo'llab-quvvatlanmaydi. Kod Plausible'ga yuborilmaydi.

### Eslatmalar ishlayotganini tekshirish (3 daqiqa)

1. **cron-job.org** (yoki GitHub Actions → "Telegram reminders") tarixida oxirgi chaqiruvlar `202` bilan tugaganini ko'ring; har soatda bittadan bo'lishi kerak.
2. **Render logida** har yurishdan keyin shu qator chiqadi: `Eslatmalar: tekshirildi N, yuborildi N, maqsad bajarilgan N, o'chirildi N; tozalandi N`. Soat bo'yicha ketma-ket chiqmasa — cron ishlamayapti.
3. **Telegram'da o'zingizga:** botda ⚙️ Sozlamalar → eslatma soatini hozirgi soatdan 1 soat keyinga qo'ying, shu soatda xabar kelishini kuting (maqsadingiz hali bajarilmagan bo'lishi kerak, aks holda xabar kelmaydi).
4. Cron `401/403` bersa — `X-Cron-Secret` Render'dagi `Telegram__CronSecret` bilan bir xil emas.

Bosh sahifadagi "Telegramda eslatma" taklifi: "Ulash" Telegram'ni darhol ochadi (profilga o'tkazmaydi); "Keyinroq" 7 kunga yashiradi (`localStorage["sc.tgPromo.dismissedAt"]`).

## Fikr so'rovlari (bir bosishli baholar)

Foydalanuvchi fikrini faol yig'ish uchun kichik, rad etish oson so'rovlar (hammasi ixtiyoriy):
- **Birinchi mashqdan keyin** (har qurilmada bir marta): "Bu mashq foydali bo'ldimi?" — 👍/👎. 👎 bo'lsa, ixtiyoriy izoh so'raladi (mavjud `/api/feedback` orqali, adminga Telegram xabari bilan). 15 soniya javobsiz qolsa o'zi yopiladi va qayta so'ralmaydi.
- **AI natijasi ostida** (gapirish, yozish, Part 2): "AI bahosi adolatli tuyuldimi?" — Adolatli / Yuqori / Past. Maqsad: AI bahosi odamniki bilan qanchalik mosligini o'lchash.
- **Bosh sahifada** (bir marta): "Bizni qayerdan topdingiz?" — Instagram, Telegram, do'st, o'qituvchi, qidiruv, boshqa (UTM'ni ko'rmaydigan Plausible o'rniga).
- Mashqlar, Takrorlash va Natijalar sahifalari oxirida "Fikr bildirish" havolasi (footer telefonda ishchi ekranlarda yashirin).

**Texnik:** yangi additiv `POST /api/rating {key, page}` (mehmon ham yuboradi; `write` limiti) va admin uchun `GET /api/admin/ratings?days=30`. Mavjud `/api/feedback` o'zgarmagan (u `rating` turini qabul QILMAYDI). Baholar mavjud `Feedbacks` jadvaliga `Kind = "rating"` bilan yoziladi — **sxema o'zgarmaydi, migratsiya kerak emas**. Kalit qat'iy ro'yxatdan (`RatingRules`; erkin matn yo'q), birinchi mashq va manba bir foydalanuvchidan bir marta qabul qilinadi. Baholar adminning fikrlar ro'yxatiga va "Fikrlar (7 kun)" sonoqchisiga kirmaydi, adminga Telegram xabari ham yuborilmaydi; ular Admin sahifasidagi "Bir bosishli baholar" kartasida jamlanadi.

**Feature flag:** Vercel'da `VITE_FEEDBACK_PROMPTS=off` va Redeploy — hamma so'rovlar o'chadi (qo'lda "Fikr bildirish" qoladi). Standart — yoqiq. Backend'da o'chirgich kerak emas (endpoint faqat so'rov kelganda ishlaydi).

**Maxfiylik:** ovoz va shaxsiy ma'lumot yuborilmaydi; faqat tanlov kaliti, bo'lim nomi (`#/practice`, identifikatorsiz), til va (kirgan bo'lsa) hisob. Hisob o'chirilsa, baholar ham o'chadi. `privacy.html` yangilangan.

**Natijani o'qish:** Admin → "Bir bosishli baholar": birinchi mashq foydali/foydasiz ulushi, AI adolatliligi (har ko'nikma uchun Adolatli/Yuqori/Past %, n bilan), manba bo'yicha sonlar. Namuna hajmi (n) kichik bo'lsa, xulosa chiqarmang.

## AI ustoz (mavzu bo'yicha mini-dars)

Foydalanuvchi mavzu yozadi ("Modal verbs") — AI bitta so'rovda butun dars yaratadi: 3 qisqa tushuntirish → 6 mashq → o'z gaplari (ixtiyoriy) → 4 ta mini-test. Javoblar **serverda** tekshiriladi (AI'siz), to'g'ri javob brauzerga/botga faqat javob berilgandan keyin ketadi. Xatolar avtomatik takrorlash kartalariga tushadi.

- **Veb:** Mashqlar → "AI ustoz" (`/api/tutor/*`). **Telegram:** `/dars` yoki `/dars Modal verbs`; mashq variantlari tugma sifatida.
- **Baza sxemasi o'zgarmaydi, migratsiya kerak emas:** dars holati `PendingExercises`da (1 kundan keyin tozalanadi), natija `Activities`da (tur matn — `Lesson`).
- **Limitlar:** dars boshlash — oddiy kunlik mashq limitidan (`Ai__ExercisesPerDay`); savol va yozma baholash — `Ai__TutorCallsPerDay` (standart 30); darsga ko'pi bilan 5 ta savol. Mavzudan tashqari (inglizcha o'rganishga oid bo'lmagan) so'rov limit sarflamaydi.
- **O'chirish:** Render'da `Features__AiTutor=false` va qayta ishga tushirish.
- **Lokal/e2e sinov:** `Gemini__BaseUrl` va `Telegram__ApiBaseUrl` soxta serverga yo'naltiriladi (production'da bo'sh qoldiriladi).

### Ustoz vazifasi: AI dars (guruh uchun)

O'qituvchi guruhga **"AI dars (mavzu)"** turidagi vazifa beradi ("Modal verbs"). O'quvchi uni veb (Vazifalarim → Boshlash) yoki Telegram (xabar tugmasi, "Bugun" ro'yxati) orqali bir bosishda boshlaydi.

- **Baza sxemasi o'zgarmaydi, migratsiya kerak emas:** tur `Assignments.Kind` ustunida `lesson:<mavzu>` ko'rinishida (mavzu ko'pi bilan 33 belgi, ustun 40 belgi); bajarilish `Activities`dagi `Lesson` yozuvining mavzusidan aniqlanadi (vazifa berilgandan keyin SHU mavzu bo'yicha birinchi tugatilgan dars).
- **Yangi:** `GET /api/teacher/assignments/{id}/insights` (faqat guruh egasi): bajarganlar, mini-test o'rtachasi, o'zlashtirish, sinfdagi eng ko'p xatolar. `POST /api/tutor/start` ixtiyoriy `assignmentId` maydonini qabul qiladi (mavzu vazifadan olinadi; faqat guruh a'zosi yoki egasi).
- **Umumiy dars:** vazifa bo'yicha dars mavzu+daraja+til bo'yicha 12 soat xotirada saqlanadi — butun sinf uchun Gemini bitta marta chaqiriladi va hamma bir xil savollarni oladi (o'qituvchi umumiy xatolarni solishtira oladi). Server qayta ishga tushsa kesh bo'shaydi.
- **Limit:** o'quvchi uchun oddiy kunlik mashq limiti sarflanadi (`Ai__ExercisesPerDay`); o'qituvchi uchun qo'shimcha limit yo'q.
- **O'chirish:** `Features__AiTutor=false` — vazifa turi ham ishlamaydi (o'quvchida "o'chirilgan").

### AI ustoz tavsiyasi ("Xatolaringiz asosida sizga shu mavzu kerak")

"Mening xatolarim" tahlili (AI'siz) xato turlarini AI ustoz mavzulariga bog'laydi: artikllar → *Articles (a/an/the)*, predloglar → *Prepositions of time and place*, ega-fe'l moslashuvi → *Subject-verb agreement*, zamonlar → *Past Simple vs Present Perfect*, ko'plik → *Countable and uncountable nouns*, qiyosiy daraja → *Comparatives and superlatives*, so'z tartibi → *English word order*. Imlo, so'z tanlash va "boshqa" turlariga mavzu tavsiya qilinmaydi.

- **Qayerda ko'rinadi:** Bosh sahifa (1 ta), Xatolarim (3 tagacha), mock natijasi (1 ta), AI ustoz sahifasi (3 tagacha; bosilsa mavzu oldindan yoziladi, "Darsni boshlash"ni o'zi bosadi) va Telegram `/dars` (🎯 tugmalar, birinchi qatorlarda).
- **Qoida:** tur bo'yicha kamida 2 ta xato; avval oxirgi 30 kundagilar; 14 kun ichida **yaxshi o'zlashtirilgan** (mini-test ≥75%) mavzu qayta tavsiya qilinmaydi.
- **Baza sxemasi o'zgarmaydi, migratsiya kerak emas.** AI chaqirilmaydi va limit sarflanmaydi (faqat mavjud `Activities` o'qiladi). Yangi endpoint: `GET /api/tutor/recommendations`.
- **O'chirish:** `Features__AiTutor=false` — tavsiyalar bo'sh qaytadi (kartalar ko'rinmaydi).

### AI ustoz o'lchovlari (admin → "AI ustoz")

Pilot paytida darslar sifatini raqam bilan ko'rish uchun (Admin paneli, oxirgi 30 kun):
- **Boshlangan darslar** va ular qayerdan boshlangani: o'zi tanlagan / **tavsiyadan** / **ustoz vazifasidan**; **tugatilgan** darslar, o'quvchilar soni va boshlanganlarning necha foizi tugagani.
- **Mini-test o'rtachasi**, o'zlashtirish (yaxshi / o'rtacha / qiyin), o'z gaplarini yozganlar.
- **Dars bahosi** (👍/👎, saytda ham, Telegram'da ham): 👎 bo'lsa, ixtiyoriy izoh oddiy fikrlar ro'yxatiga `[AI ustoz: mavzu] …` bilan tushadi.
- Ustozlarning AI dars vazifalari soni va **eng ko'p o'rganilgan mavzular**.
- Plausible (yoqilgan bo'lsa): `tutor_started`, `tutor_completed` hodisalari (xossasiz, identifikatorsiz).

**Baza sxemasi o'zgarmaydi, migratsiya kerak emas.** Hodisalar mavjud `Feedbacks` jadvaliga `Kind = "rating"` bilan yoziladi (`lesson_start:free|recommended|assigned` — faqat server yozadi, tashqaridan `POST /api/rating` bilan yozib bo'lmaydi; `lesson:up|down` — foydalanuvchi bahosi). Ular fikrlar ro'yxatiga va "Fikrlar (7 kun)" sonoqchisiga kirmaydi; hisob o'chirilsa, ular ham o'chadi. Yangi endpoint: `GET /api/admin/tutor?days=30` (faqat admin). Eslatma: bu o'zgarishdan oldingi darslar uchun "boshlangan" hodisasi yo'q, shuning uchun "tugatilgan ulushi" faqat yangi davr uchun to'g'ri.

### Shaxsiy podcast (`/podcast`)

Istalgan foydalanuvchi (o'qituvchi yoki admin bo'lishi shart emas) Telegram botda `/podcast` yozib YouTube havolasini yuboradi (`https://youtu.be/ID 1:20-3:40`, ixtiyoriy daraja `B1`): Gemini videoni tinglab gaplarga bo'ladi va **shadowing darsi** tayyorlaydi (1–3 daqiqa, fonda). Bot "▶️ Mashq qilish" tugmasini yuboradi: sayt **Telegram ichida (Mini App)** ochiladi — video, gap-gap takrorlash va AI baho mavjud Shadowing mashqidagidek. Darslar saytda ham **Shadowing → "Mening videolarim"** bo'limida ko'rinadi. `/podcast` ro'yxatda ularni o'chirish tugmasi bor.

- **Faqat egasiga ko'rinadi.** Umumiy Shadowing ro'yxatiga, boshqa foydalanuvchiga va admin ko'rib chiqish navbatiga tushmaydi (boshqa hisobdan dars ochish — 404; mehmon — 404). Umumiy kutubxonaga qo'shish — avvalgidek `/add` (o'qituvchi/admin).
- **Baza sxemasi o'zgarmaydi, migratsiya kerak emas:** dars `MockTests`da, holat `personal` (ustun 16 belgi), muallif `CreatedByUserId`. Hisob o'chirilganda shaxsiy darslar ham o'chadi (chop etilganlar bankda qoladi); ma'lumotlar eksportida bor (`mockTestsAuthored`).
- **Limitlar:** kuniga `Ai__PodcastsPerDay` (standart 3, admin cheksiz), saqlanadigan darslar ko'pi bilan 30; chatda bir vaqtda bitta ish; AI xato bersa urinish qaytariladi. Video 10 soniyadan 5 daqiqagacha (vaqt yozilmasa — boshidan 3 daqiqa).
- **O'chirish:** `Features__PersonalPodcasts=false`.
- **Eslatma:** faqat shaxsiy mashq uchun; ochiq (public) YouTube video va aniq inglizcha nutq kerak. Sifat Gemini'ning videoni eshitishiga bog'liq.

### Tezlik (audit natijasi)

- **API javoblari siqiladi** (Brotli, bo'lmasa Gzip): 1 KB dan katta JSON (kartalar, natijalar, testlar) taxminan 4–5 marta kichrayadi. Sozlama kerak emas; brauzer `Accept-Encoding` yuboradi.
- **Frontend fayllari uzoq keshlanadi** (`/assets/*` — nomida hash bor, `immutable`, 1 yil): qayta kirganda yuzlab KB qayta yuklanmaydi. `index.html` va `sw.js` har safar tekshiriladi (yangi versiya darrov chiqadi).
- **Kam baza safari:** har so'rovda sessiya va foydalanuvchi bitta so'rovda; `/api/review/stats` (har sahifada chaqiriladi) 7 o'rniga 4–5 so'rov. Neon'da har baza safari tarmoq kechikishini qo'shadi, shuning uchun bu bevosita tezlikka ta'sir qiladi.

