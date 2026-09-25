# AI Speaking Coach

Ingliz tili o'rganuvchilar uchun ikkita mashq turi bor:

- **Gapirish** — mikrofondan yozib gapiring → Google Gemini bitta
  chaqiruvda transkripsiya qiladi va rubrika bo'yicha (ravonlik,
  grammatika, lug'at boyligi) baholaydi.
- **Yozish** — mavzuda insho yozing → Gemini uni IELTS Writing'ga o'xshash
  rubrika bo'yicha (vazifani bajarish, mantiqiy bog'lanish, grammatika,
  lug'at boyligi) baholaydi.

Ikkalasida ham aniq tuzatishlar beriladi va har bir urinish ma'lumotlar
bazasida saqlanadi — sahifani yangilasangiz ham tarixingiz yo'qolmaydi.

**Live demo**: https://speaking-coach-theta.vercel.app
(Backend bepul tarifda ishlaydi — 15 daqiqa foydalanilmasa "uxlaydi",
birinchi so'rov ~30-60 soniya uyg'onish vaqtini olishi mumkin.)

## Arxitektura

```
Brauzer (React + TypeScript, Vercel)
   │  Gapirish: audio (webm) + mavzu     Yozish: matn + mavzu
   ▼
Backend (ASP.NET Core Minimal API, Render, Docker)
   │  GeminiClient — ikkalasi ham shu bitta klass orqali
   │  (model fallback + retry bir joyda yozilgan)
   ▼
Google Gemini (Speaking: multimodal — transkripsiya + baholash bitta
so'rovda. Writing: faqat matn — baholash bitta so'rovda)
   │
   ▼
PostgreSQL (Neon) — har bir urinish Activity sifatida saqlanadi
(Type = Speaking yoki Writing, bitta jadval)
```

Nega bitta so'rov (Speaking uchun): Gemini audio faylni to'g'ridan-to'g'ri
qabul qiladi, shuning uchun alohida Speech-to-Text (masalan Whisper)
qatlami shart emas — bu arxitekturani soddalashtiradi va bepul tier so'rov
limitini tejaydi.

Nega `GeminiClient` alohida klass: Speaking va Writing ikkalasi ham Gemini
API'ga bir xil tarzda murojaat qiladi (model band bo'lsa zaxira modelga
o'tish, 503/429'da qayta urinish) — bu mantiq faqat bir joyda yozilgan,
ikkala servis (`GeminiSpeakingService`, `GeminiWritingService`) uni faqat
ishlatadi, qayta yozmaydi.

## To'liq bepul stack

| Qism | Xizmat | Nega bepul |
|---|---|---|
| LLM (transkripsiya + baholash) | Google Gemini | AI Studio API kaliti, kredit karta shart emas |
| Ma'lumotlar bazasi | Neon Postgres | 0.5GB bepul, kredit karta shart emas |
| Backend hosting | Render | Web Service bepul tarifi, kredit karta shart emas (15 daq. faolsizlikdan keyin uxlaydi) |
| Frontend hosting | Vercel | Bepul tarif, static/SPA loyihalar uchun yetarli |

## Lokal ishga tushirish

### 1. Gemini API kaliti

1. [aistudio.google.com/apikey](https://aistudio.google.com/apikey) ga Google
   akkauntingiz bilan kiring, "Create API key" bosing.

### 2. Postgres bazasi (Neon)

1. [neon.tech](https://neon.tech) da bepul loyiha yarating
2. "Connect" bo'limidan connection string oling (pooler**siz** versiyasini —
   manzilida `-pooler` bo'lmagan), quyidagi ko'rinishga o'zgartiring:
   ```
   Host=<host>;Database=<baza>;Username=<user>;Password=<parol>;SSL Mode=Require;Channel Binding=Require;
   ```

### 3. Sirlarni (secrets) qo'shish

```bash
cd backend/SpeakingCoach.Api
dotnet user-secrets init
dotnet user-secrets set "Gemini:ApiKey" "sizning-gemini-kalitingiz"
dotnet user-secrets set "ConnectionStrings:Default" "yuqoridagi Postgres qatori"
```

### 4. Baza sxemasini yaratish

```bash
dotnet tool update --global dotnet-ef
dotnet restore
dotnet ef database update
```

### 5. Backend

```bash
dotnet run
```

### 6. Frontend

Yangi terminalda:

```bash
cd frontend/speaking-coach-web
npm install
npm run dev
```

`http://localhost:5173` ni oching.

### 7. Sinov

**Gapirish** tab'ida: "Yozishni boshlash" → 15-20 soniya gapiring →
"To'xtatish" → kuting (5-15 soniya, ba'zan Gemini band bo'lsa 20-30
soniyagacha — backend avtomatik qayta uradi). Transkript, to'rtta ball va
"Oldingi urinishlar" ro'yxati chiqishi kerak.

**Yozish** tab'ida: mavzuda kamida 20 belgilik insho yozing → "Yuborish" →
xuddi shunday kuting. Baholash va "Oldingi urinishlar" (endi Writing
turidagi yozuvlar) ko'rinishi kerak — bu ikkalasi bir xil `Activities`
jadvalidan, faqat `Type` ustuni bo'yicha ajratilib o'qiladi.

---

## Deploy qilish — Vercel + Render + Neon

1. **GitHub**: loyihani push qiling (`.gitignore` allaqachon `bin/`, `obj/`,
   `node_modules/`, `uploads/` ni chetlab o'tadi — sirlar hech qachon
   repoga tushmaydi, ular faqat `dotnet user-secrets` orqali lokalda yoki
   hosting'ning environment variable'larida saqlanadi).
2. **Render** (backend): "New Web Service" → repo'ni ulang → Root
   Directory = `backend/SpeakingCoach.Api` → Docker avtomatik aniqlanadi →
   Free tarif → Environment Variables: `Gemini__ApiKey`,
   `ConnectionStrings__Default` (qo'sh pastki chiziq — .NET buni
   `Gemini:ApiKey` bilan bir xil deb tushunadi).
3. **Vercel** (frontend): "Add New Project" → repo'ni ulang → Root
   Directory = `frontend/speaking-coach-web` → Environment Variable:
   `VITE_API_URL` = Render manzilingiz.
4. **CORS**: Vercel manzilini bilib olgach, Render'ga qaytib
   `FrontendOrigin` environment variable qo'shing (Vercel manzilingiz,
   `/` siz) — aks holda brauzer "Failed to fetch" xatosi beradi.

## Bilib qo'yish kerak bo'lgan cheklovlar

- **Bepul tier limitlari**: Gemini'da daqiqa/kunlik so'rov chegarasi bor;
  Render'ning bepul backend'i 15 daqiqa harakatsizlikdan keyin uxlaydi.
  Demo/portfolio uchun yetarli, real ko'p foydalanuvchili mahsulot uchun
  emas — buni bilish va tushuntira olish ham portfolio uchun yaxshi signal.
- **Login yo'q**: Hozircha barcha "Oldingi urinishlar" umumiy ro'yxatda —
  foydalanuvchi hisoblari yo'q. Bu ataylab shunday (login — alohida katta
  ish: parol, sessiya, xavfsizlik).
- **Audio fayllar saqlanmaydi**: `uploads/` papkasiga yoziladi, lekin
  Render'ning fayl tizimi "ephemeral" (server qayta ishga tushganda
  o'chadi). Faqat baholash natijasi (matn, ball) bazada doimiy saqlanadi.

## Keyingi texnik qadamlar

1. ✅ ~~PostgreSQL + EF Core~~ — bajarildi
2. ✅ ~~Deploy (Vercel + Render + Neon)~~ — bajarildi
3. ✅ ~~Writing oqimi~~ — bajarildi (Gemini so'rov qismi `GeminiClient`ga
   chiqarildi, Speaking va Writing endi shu bitta klassni ishlatadi)
4. Bir xil audio/matnni bir necha marta yuborib, ball qanchalik
   tarqalishini o'lchash (LLM baholash barqarorligi)
5. Reading / Listening turlari — `ActivityType` enum'ida joy tayyor, lekin
   hali endpoint/UI yo'q
6. Foydalanuvchi hisoblari (login) — hozir "Oldingi urinishlar" hammaga
   umumiy

## Ishlatishdan oldin tushunishingiz kerak bo'lgan savollar

- Nega bitta `Activities` jadvali bor, Speaking/Writing/Reading uchun
  alohida jadval emas? `PromptData`/`ResponseData` nega jsonb?
- `GeminiClient.SendWithFallbackAsync` nima uchun ikkita model ishlatadi,
  va nega faqat 503/429'da qayta uradi? (Avval bu kod
  `SpeakingEvaluationService.cs` ichida edi — Writing qo'shilganda nega
  alohida klassga chiqarib yuborildi?)
- `FrontendOrigin` nima uchun kerak — bo'lmasa nima o'zgaradi (CORS)?
- `ConnectionStrings__Default` va `ConnectionStrings:Default` — bular
  nega bir xil narsa, lekin yozilishi farq qiladi?
- Writing qo'shilganda nega yangi EF Core migratsiya (`dotnet ef
  migrations add ...`) kerak bo'lmadi?

Javob berolmasangiz — tegishli fayllarni (`Program.cs`,
`Services/GeminiClient.cs`, `Services/WritingEvaluationService.cs`,
`Data/AppDbContext.cs`) oching, o'qing.
