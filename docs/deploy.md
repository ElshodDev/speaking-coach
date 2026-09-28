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
dotnet ef database update
dotnet run                      # http://localhost:5000

# frontend (ikkinchi terminal)
cd frontend/speaking-coach-web
npm ci
npm run dev                     # http://localhost:5173
```

Testlar:

```bash
dotnet test backend/SpeakingCoach.Api.Tests
cd frontend/speaking-coach-web && npm test
```

## Render (backend)

Root directory: `backend/SpeakingCoach.Api`, Docker. **Health check path: `/health/live`** (bazaga tegmaydi — Neon uxlab qolganda Render serverni behuda qayta ishga tushirmaydi; `/health` esa bazani ham tekshiradi, monitoring uchun). Environment:

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

## Vercel (frontend)

Root directory: `frontend/speaking-coach-web`; environment: `VITE_API_URL` (Render manzili) va `VITE_SITE_URL` (saytning o'z manzili — Telegram/Instagram havola ko'rinishidagi rasm uchun; hozir `https://fluentuz.app`). Domen o'zgarsa, `VITE_SITE_URL` va Render'dagi `FrontendOrigin`ni yangilang. Xavfsizlik sarlavhalari `vercel.json` da.

## Migratsiyalar

Render ularni o'zi bajarmaydi: sxemaga bog'liq kodni push qilishdan oldin lokalda `dotnet ef database update` bajariladi.

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
