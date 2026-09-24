# Speaking Coach

Audio → Google Gemini (bir chaqiruvda transkripsiya + rubrika bo'yicha
baholash) → natija. **To'liq bepul** — Gemini API kaliti kredit karta talab
qilmaydi.

## 1. Bepul Gemini API kalitini olish

1. [aistudio.google.com/apikey](https://aistudio.google.com/apikey) ga
   Google akkauntingiz bilan kiring
2. "Create API key" bosing — bir necha soniyada tayyor bo'ladi
3. Kalitni nusxalab oling

Bepul tier cheklovi bor (daqiqasiga va kuniga so'rov soni), lekin portfolio
demosi va sinov uchun yetarli. Chegara haqida aniq raqamlarni
[ai.google.dev/pricing](https://ai.google.dev/pricing) sahifasidan
tekshiring — vaqt o'tishi bilan o'zgarishi mumkin.

## 2. Kalitni loyihaga qo'shish

```
cd backend/SpeakingCoach.Api
dotnet user-secrets init
dotnet user-secrets set "Gemini:ApiKey" "sizning-kalitingiz"
```

## 3. Backend

```
dotnet restore
dotnet build
dotnet run
```

Bu safar tashqi NuGet paketi yo'q (faqat oddiy `HttpClient`), shuning uchun
oldingi versiyadagi preview-paket build xatolari endi bo'lmasligi kerak.

## 4. Frontend

Yangi terminalda:

```
cd frontend/speaking-coach-web
npm install
npm run dev
```

`http://localhost:5173` ni oching.

## 5. Sinov

"Yozishni boshlash" → 15-20 soniya gapiring → "To'xtatish" → kuting
(5-15 soniya). Transkript va uchta ball chiqishi kerak.

**Agar "Gemini API xatosi (400)" yoki shunga o'xshash chiqsa** — backend
terminalidagi to'liq xabarni o'qing, u aynan sababni ko'rsatadi (masalan,
noto'g'ri model nomi yoki audio format rad etilgani). Shu xabarni menga
yuboring.

---

## Deploy qilish — Vercel + Railway

Arxitektura o'zgarmadi (frontend → Vercel, backend → Railway/Render
Dockerfile orqali), faqat environment variable nomi farqli:

- Railway'da `OpenAI__ApiKey` o'rniga **`Gemini__ApiKey`** qo'shing
- `FrontendOrigin` va `VITE_API_URL` sozlash — oldingi bosqichdagi bilan bir xil

To'liq qadamlar (GitHub → Railway → Vercel → CORS to'ldirish) — bu README'ning
oldingi versiyasida batafsil yozilgan edi, tuzilishi o'zgarmadi.

## Bilib qo'yish kerak bo'lgan cheklov

Bepul tier — demo va portfolio uchun yetarli, lekin real ko'p foydalanuvchi
uchun emas: so'rov soni daqiqada va kunda cheklangan. Agar loyiha real
foydalanuvchilarga ochilsa, shu joyda to'lov rejasiga o'tish yoki boshqa
provayderga almashtirish kerak bo'ladi — buni README'ning "Cheklovlar"
bo'limiga yozib qo'ying, bu ham portfolio uchun yaxshi signal (nima
bilmasligi va nega ekanini bilish).

## Keyingi texnik qadamlar

1. PostgreSQL + EF Core — hozir hech narsa doimiy saqlanmayapti
2. Bir xil audio/matnni 3 marta yuborib, ball qanchalik tarqalishini o'lchash
3. Writing oqimi — xuddi shu Gemini arxitekturasi, audio o'rniga matn

## Ishlatishdan oldin tushunishingiz kerak bo'lgan savollar

- Nega endi alohida `TranscriptionService` yo'q — Gemini buni qanday
  bittasiga birlashtirgan?
- `inline_data` va `mime_type` nima uchun kerak, `audioMimeType.Split(';')[0]`
  qatori nimani hal qiladi?
- `generationConfig.responseMimeType = "application/json"` nima uchun muhim —
  bu bo'lmasa nima o'zgaradi?

Javob berolmasangiz — `SpeakingEvaluationService.cs`ni oching, o'qing.
