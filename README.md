# AI Speaking Coach

Ingliz tili o'rganuvchilar uchun to'rtta mashq turi:

- **Gapirish** — mikrofondan yozib gapiring → Google Gemini bitta
  chaqiruvda transkripsiya qiladi va rubrika bo'yicha (ravonlik,
  grammatika, lug'at boyligi) baholaydi.
- **Yozish** — mavzuda insho yozing → Gemini uni IELTS Writing'ga o'xshash
  rubrika bo'yicha (vazifani bajarish, mantiqiy bog'lanish, grammatika,
  lug'at boyligi) baholaydi.
- **O'qish** — Gemini har safar yangi matn va 4 ta test savoli yaratadi;
  javoblarni server tekshiradi va har bir savolga izoh beradi.
- **Tinglash** — xuddi shunday, lekin matnni brauzer ovoz chiqarib o'qiydi
  (Web Speech API), matnning o'zi javob berilgandan keyin ko'rinadi.

Tizimga kirgan foydalanuvchining har bir urinishi ma'lumotlar bazasida
saqlanadi va faqat o'ziga ko'rinadi. Kirmasdan ham barcha mashqlar ishlaydi
— faqat natija tarixga yozilmaydi.

**Live demo**: https://speaking-coach-theta.vercel.app
(Backend bepul tarifda ishlaydi — 15 daqiqa foydalanilmasa "uxlaydi",
birinchi so'rov ~30-60 soniya uyg'onish vaqtini olishi mumkin.)

## Arxitektura

```
Brauzer (React + TypeScript, Vercel)
   │  Authorization: Bearer <token>  (kirgan bo'lsa)
   ▼
Backend (ASP.NET Core Minimal API, Render, Docker)
   │  Endpoints/   — auth, speaking/writing, reading/listening
   │  Services/    — GeminiClient (fallback + retry + qat'iy JSON),
   │                 baholash/yaratish servislari, AuthService
   ▼
Google Gemini
   - Speaking: audio → transkripsiya + baholash (bitta so'rov)
   - Writing:  matn → baholash
   - Reading/Listening: matn + savollar + to'g'ri javoblarni YARATISH
   │
   ▼
PostgreSQL (Neon)
   Activities       — barcha urinishlar (Type + UserId bo'yicha)
   Users, Sessions  — hisoblar va kirish sessiyalari
   PendingExercises — javob kutayotgan Reading/Listening mashqlari
```

Nega bitta so'rov (Speaking uchun): Gemini audio faylni to'g'ridan-to'g'ri
qabul qiladi, shuning uchun alohida Speech-to-Text (masalan Whisper)
qatlami shart emas — bu arxitekturani soddalashtiradi va bepul tier so'rov
limitini tejaydi.

Nega `GeminiClient` alohida klass: barcha servislar Gemini API'ga bir xil
tarzda murojaat qiladi (model band bo'lsa zaxira modelga o'tish, 503/429'da
qayta urinish, javobni qat'iy tekshirib o'qish) — bu mantiq faqat bir joyda
yozilgan.

## To'liq bepul stack

| Qism | Xizmat | Nega bepul |
|---|---|---|
| LLM (transkripsiya, baholash, mashq yaratish) | Google Gemini | AI Studio API kaliti, kredit karta shart emas |
| Ovoz chiqarib o'qish (Listening) | Brauzerning Web Speech API'si | Brauzerga o'rnatilgan, API kaliti kerak emas |
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

Bu `Migrations/` papkasidagi barcha migratsiyalarni (jadvallar, indekslar)
bazaga qo'llaydi. Kod modelini o'zgartirganingizda (yangi jadval yoki
ustun) avval `dotnet ef migrations add <Nom>` bilan yangi migratsiya
yaratiladi, keyin yana `dotnet ef database update`.

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

1. **Mehmon sifatida**: har bir tab'da "Siz mehmon sifatida
   ishlayapsiz..." eslatmasi ko'rinadi; mashqlar ishlaydi, tarix bo'sh.
2. **Ro'yxatdan o'ting** (yuqoridagi "Kirish" → "Ro'yxatdan o'tish").
3. **Gapirish**: "Yozishni boshlash" → 15-20 soniya gapiring →
   "To'xtatish" → transkript, ballar va tarixda 1 ta yozuv.
4. **Yozish**: kamida 20 belgilik insho → "Yuborish".
5. **O'qish**: "Mashqni boshlash" → matnni o'qing → 4 ta savolga javob →
   "Javoblarni tekshirish" → har bir savolga yashil/qizil belgi va izoh.
6. **Tinglash**: "Mashqni boshlash" → "Tinglash" → savollarga javob → matn
   faqat tekshirilgandan keyin ko'rinadi.
7. Sahifani yangilang — kirgan holat saqlanib qoladi. "Chiqish" — tarix
   yana bo'sh ko'rinadi.

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
5. **Migratsiyalar**: Render ularni o'zi qo'llamaydi. Lokal va production
   bitta Neon bazasini ishlatgani uchun, yangi migratsiyani lokalda
   `dotnet ef database update` bilan qo'llab, keyin push qiling — shunda
   yangi kod deploy bo'lganda jadvallar allaqachon tayyor turadi.

## Login qanday ishlaydi

- **Parol**: ASP.NET Core'ning `PasswordHasher` (PBKDF2, tasodifiy tuz,
  ko'p marta takrorlash). Parolning o'zi hech qayerda saqlanmaydi.
- **Token**: kirganda 32 baytli kriptografik tasodifiy token yaratiladi.
  Brauzer uni `Authorization: Bearer ...` sarlavhasida yuboradi. Bazada
  tokenning o'zi emas, **SHA-256 xeshi** saqlanadi — baza sizib chiqsa
  ham, undagi xeshlar bilan hech kimning nomidan kirib bo'lmaydi.
- **Nega JWT emas**: sessiya bazada bo'lgani uchun "Chiqish" tokenni
  **darhol** bekor qiladi (JWT'ni muddatidan oldin bekor qilib bo'lmaydi).
  Tashqi paket yoki maxfiy kalit sozlash ham kerak emas. Narxi — har bir
  so'rovda indekslangan bitta qo'shimcha DB so'rovi.
- **Nega cookie emas**: frontend (`vercel.app`) va backend (`onrender.com`)
  turli domenlarda — brauzerlar bunday "third-party" cookie'larni tobora
  ko'proq bloklaydi. Token `localStorage`'da saqlanadi (kamchiligi pastda).
- Noto'g'ri email va noto'g'ri parol uchun **bir xil** xabar qaytariladi —
  aks holda qaysi email ro'yxatdan o'tganini aniqlab bo'lardi.
- Email bazada **unique index** bilan himoyalangan — bir vaqtda kelgan ikki
  so'rov ham bitta email'ni ikki marta ro'yxatdan o'tkaza olmaydi.

## O'qish va Tinglash qanday ishlaydi

- Gemini **yaratadi** (matn, 4 ta savol, to'g'ri javoblar, izohlar), lekin
  **tekshirmaydi**. Test savolining to'g'ri javobi oldindan ma'lum —
  uni LLM'dan qayta so'rash sekin, pullik va (barqarorlik testida
  ko'rganimizdek) tasodifiy bo'lardi. Tekshirish — oddiy C# kodi
  (`GeminiComprehensionService.Grade`).
- To'g'ri javoblar **brauzerga yuborilmaydi**: to'liq mashq
  `PendingExercises` jadvalida saqlanadi, brauzer faqat matn va
  savollarni oladi. Javob kelganda server o'sha yozuvni o'qib tekshiradi
  va o'chiradi. Tashlab ketilgan mashqlar 1 kundan keyin tozalanadi.
- Gemini ba'zan talabni buzadi (3 ta savol, 5 ta variant, noto'g'ri
  indeks) — bunday mashq foydalanuvchiga ko'rsatilmaydi, xato qaytadi.
- Tinglashda matn gaplarga bo'lib o'qiladi: Chrome'da bitta uzun
  "utterance" ~15 soniyadan keyin uzilib qolishi ma'lum muammo.

## Gemini javobini qat'iy tekshirish

Standart holatda `System.Text.Json` JSON'da yetishmayotgan maydonni
**jimgina** standart qiymat bilan to'ldiradi: Gemini `"score"` o'rniga
boshqa nom yozsa, ball **0** bo'lib bazaga saqlanardi va xato hech qayerda
ko'rinmasdi. Endi `GeminiClient.StrictJson` sozlamasi
(`RespectRequiredConstructorParameters`, `RespectNullableAnnotations`) va
`ScoreGuard` (0-100 oralig'i, izoh bo'sh emas) bunday javobni rad etadi —
foydalanuvchi xato ko'radi va qayta urinadi, bazaga noto'g'ri ma'lumot
tushmaydi. Eslatma: 0 ballning o'zi xato emas — mavzuga aloqasiz matn uchun
Gemini haqiqatan 0 berishi mumkin.

## LLM baholash barqarorligi

Muammo: LLM hakam sifatida 100% deterministik emas — `temperature=0.2`
bo'lsa ham, bitta va aynan bir xil insho yoki audio ikki marta
yuborilsa, ballar farq qilishi mumkin. Agar farq katta bo'lsa,
foydalanuvchi "grammatikam 65 dan 80 ga ko'tarildi" deb o'ylashi mumkin,
aslida esa hech narsa o'zgarmagan — shunchaki model tasodifiyligi.

Yechim: Gapirish va Yozish natijalari ostida **"Barqarorlikni tekshirish
(5x)"** tugmasi bor. U aynan o'sha kirishni Gemini'ga yana 5 marta
**ketma-ket** yuboradi va har bir mezon bo'yicha ballar, o'rtacha, min–max
farqi va baho (≤5 barqaror, 6–15 o'rtacha, >15 beqaror) jadvalini
ko'rsatadi.

- Test so'rovlari `?save=false` bilan yuboriladi — backend baholaydi,
  lekin bazaga yozmaydi.
- Ketma-ket, parallel emas: parallel 5 ta so'rov Gemini bepul tier'ining
  daqiqalik limitiga (429) darhol urilardi.
- Kod: `frontend/.../src/Stability.tsx`.

## Bilib qo'yish kerak bo'lgan cheklovlar

- **Bepul tier limitlari**: Gemini'da daqiqa/kunlik so'rov chegarasi bor;
  Render'ning bepul backend'i 15 daqiqa harakatsizlikdan keyin uxlaydi.
- **Token `localStorage`'da**: saytda XSS zaifligi bo'lsa, begona skript
  tokenni o'qiy oladi. React matnni avtomatik "escape" qiladi va loyihada
  `dangerouslySetInnerHTML` yo'q, lekin bu httpOnly cookie darajasidagi
  himoya emas.
- **Kirishga urinishlar cheklanmagan (rate limiting yo'q)**: parolni
  ketma-ket taxmin qilishga to'sqinlik qiladigan narsa hozircha faqat
  PBKDF2'ning sekinligi.
- **Parolni tiklash va email tasdiqlash yo'q**.
- **Login'dan oldingi yozuvlar** (`UserId` bo'sh) hech kimning tarixida
  ko'rinmaydi, lekin bazada qoladi.
- **Audio fayllar doimiy saqlanmaydi**: Render'ning fayl tizimi
  "ephemeral". Faqat baholash natijasi bazada saqlanadi.
- **Tinglashdagi ovoz sifati** brauzer va operatsion tizimga bog'liq
  (Chrome/Edge'da eng yaxshi).

## Keyingi texnik qadamlar

1. ✅ ~~PostgreSQL + EF Core~~
2. ✅ ~~Deploy (Vercel + Render + Neon)~~
3. ✅ ~~Writing oqimi~~ (+ umumiy `GeminiClient`)
4. ✅ ~~LLM baholash barqarorligini o'lchash~~
5. ✅ ~~Reading / Listening~~
6. ✅ ~~Foydalanuvchi hisoblari (login)~~
7. Kirish endpoint'lariga rate limiting (ASP.NET Core'ning o'rnatilgan
   `RateLimiter`'i; Render proxy ortida bo'lgani uchun `X-Forwarded-For`ni
   to'g'ri sozlash kerak)
8. Parolni tiklash (email yuborish xizmati kerak)
9. Avtomatik testlar (xUnit + `WebApplicationFactory`) va GitHub Actions

## Ishlatishdan oldin tushunishingiz kerak bo'lgan savollar

- Nega bitta `Activities` jadvali bor, Speaking/Writing/Reading uchun
  alohida jadval emas? `PromptData`/`ResponseData` nega jsonb?
- `GeminiClient.SendWithFallbackAsync` nima uchun ikkita model ishlatadi,
  va nega faqat 503/429'da qayta uradi?
- `FrontendOrigin` nima uchun kerak — bo'lmasa nima o'zgaradi (CORS)?
- `ConnectionStrings__Default` va `ConnectionStrings:Default` — bular
  nega bir xil narsa, lekin yozilishi farq qiladi?
- Barqarorlik testida nega `?save=false` kerak, va nega so'rovlar
  parallel emas, ketma-ket yuboriladi?
- Nega bazada token emas, uning xeshi saqlanadi? Nega JWT emas?
- Nega Reading/Listening javoblarini Gemini emas, oddiy C# kodi tekshiradi?
  Nega to'g'ri javoblar brauzerga yuborilmaydi?
- Nega `Activity.UserId` nullable? Uni majburiy (NOT NULL) qilsak,
  migratsiya paytida nima bo'lardi?
- `RespectRequiredConstructorParameters` bo'lmasa, Gemini javobida
  `"score"` yo'q bo'lsa nima bo'lardi?

Javob berolmasangiz — tegishli fayllarni (`Program.cs`, `Endpoints/`,
`Services/AuthService.cs`, `Services/GeminiClient.cs`,
`Services/ComprehensionService.cs`, `Data/AppDbContext.cs`) oching, o'qing.
