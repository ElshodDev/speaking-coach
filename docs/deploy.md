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

## Render (backend)

Root directory: **`backend`**, Dockerfile path: **`./Dockerfile`**, Docker. (Backend 4 ta loyihadan iborat — qarang: [ARCHITECTURE.md](ARCHITECTURE.md) — shuning uchun build konteksti butun `backend` papkasi.) **Health check path: `/health/live`** (bazaga tegmaydi — Neon uxlab qolganda Render serverni behuda qayta ishga tushirmaydi; `/health` esa bazani ham tekshiradi, monitoring uchun). Environment:

| O'zgaruvchi | Nima uchun |
|---|---|
| `Gemini__ApiKey` | Gemini kaliti (majburiy) |
| `ConnectionStrings__Default` | Neon ulanish satri (majburiy) |
| `FrontendOrigin` | Sayt manzili — CORS va bot havolalari uchun. Bir nechta bo'lsa vergul bilan, birinchisi asosiy: `https://fluentuz.app,https://speaking-coach-theta.vercel.app` (eski manzil ham ishlab tursin) |
| `Admin__Emails` | Sayt adminlari emaillari, vergul bilan |
| `Email__BrevoApiKey`, `Email__FromAddress` | Email tasdiqlashni yoqadi (Brevo'da tasdiqlangan yuboruvchi) |
| `Email__FromName` | Xatdagi yuboruvchi nomi (standart: `Speaking Coach`) |
| `Email__DailyCap` | Kuniga ko'pi bilan nechta xat (standart: 250; Brevo bepul tarifi — 300). Tugasa, sayt Telegram yoki Google orqali kirishni taklif qiladi |
| `Google__ClientId` | Google orqali kirish (Web OAuth client, Vercel manzili ruxsat etilgan origin) |
| `Telegram__BotToken`, `Telegram__CronSecret` | Telegram bot, soatlik eslatmalar va **saytga Telegram orqali kirish** (token bo'lsa o'zi yoqiladi) |
| `Telegram__BotUsername` | Bot nomi (standart: `SpeakingCoachUzBot`) — kirish havolasi `t.me/<nom>` |
| `Telegram__PublicUrl` | Webhook uchun server manzili (bo'lmasa Render'ning `RENDER_EXTERNAL_URL`) |
| `Telegram__Admins` | Botdagi adminlar — faqat raqamli Telegram ID, vergul bilan (botga `/id` yozib bilasiz) |
| `Demo__Enabled` | `false` — demo hisobni o'chiradi (standart: yoqiq) |
| `Ai__ExercisesPerDay`, `Ai__WordsPerDay`, `Ai__ShadowingPerDay`, `Ai__Guest*PerDay` | Foydalanuvchi/mehmon uchun kunlik AI limitlari (ixtiyoriy) |
| `Ai__TalkTurnsPerDay` | AI suhbatda kuniga nechta javob (standart: 40) |
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
