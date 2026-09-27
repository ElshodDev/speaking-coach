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

Root directory: `backend/SpeakingCoach.Api`, Docker. Environment:

| O'zgaruvchi | Nima uchun |
|---|---|
| `Gemini__ApiKey` | Gemini kaliti (majburiy) |
| `ConnectionStrings__Default` | Neon ulanish satri (majburiy) |
| `FrontendOrigin` | Vercel manzili — CORS uchun |
| `Admin__Emails` | Sayt adminlari emaillari, vergul bilan |
| `Email__BrevoApiKey`, `Email__FromAddress` | Email tasdiqlashni yoqadi (Brevo'da tasdiqlangan yuboruvchi) |
| `Google__ClientId` | Google orqali kirish (Web OAuth client, Vercel manzili ruxsat etilgan origin) |
| `Telegram__BotToken`, `Telegram__CronSecret` | Telegram bot va soatlik eslatmalar |
| `Telegram__Admins` | Botdagi adminlar — faqat raqamli Telegram ID, vergul bilan (botga `/id` yozib bilasiz) |
| `Demo__Enabled` | `false` — demo hisobni o'chiradi (standart: yoqiq) |
| `Ai__ExercisesPerDay` va boshqa `Ai__*` | Kunlik AI limitlari (ixtiyoriy) |

GitHub → Settings → Secrets → Actions: `API_URL` (Render manzili) va `CRON_SECRET` (`Telegram__CronSecret` bilan bir xil) — eslatmalar workflow'i uchun.

## Vercel (frontend)

Root directory: `frontend/speaking-coach-web`; environment: `VITE_API_URL` (Render manzili). Xavfsizlik sarlavhalari `vercel.json` da.

## Migratsiyalar

Render ularni o'zi bajarmaydi: sxemaga bog'liq kodni push qilishdan oldin lokalda `dotnet ef database update` bajariladi.

## Nechta foydalanuvchi ro'yxatdan o'tgan

- **Sayt:** `Admin__Emails` dagi email bilan kiring → Profil → **Admin panel** (`#/admin`). U yerda: jami va email tasdiqlangan foydalanuvchilar, bugun va shu haftadagi yangilar, bugun/7/30 kunda faollar, kunlik grafik. Demo hisoblar hisobga kirmaydi.
- **Telegram:** botga `/admin` yozing (faqat `Telegram__Admins` dagi raqamli ID yoki admin emaili bilan ulangan hisob). Bot o'sha sonlarni qisqa xabarda yuboradi.
- "Ro'yxatdan o'tgan" deb **email tasdiqlanganlar** soniga qarang: parolsiz kirishda kod so'rab, uni kiritmagan odam ham bazada tasdiqlanmagan yozuv bo'lib qoladi.
