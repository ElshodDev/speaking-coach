using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SpeakingCoach.Api.Endpoints;
var builder = WebApplication.CreateBuilder(args);

// Bir nechta manzil bo'lishi mumkin (vergul bilan): birinchisi — asosiy (masalan, o'z domen),
// qolganlari — eski manzil (vercel.app) ham ishlab tursin, o'rnatilgan PWA va eski havolalar uchun.
var frontendOrigins = FrontendOrigins.Parse(builder.Configuration["FrontendOrigin"]);

builder.Services.AddCors(options =>
{
    // Token cookie'da emas, "Authorization" sarlavhasida yuboriladi —
    // shuning uchun AllowCredentials() shart emas; AllowAnyHeader() esa
    // Authorization sarlavhasiga ham ruxsat beradi. Retry-After — brauzer
    // JS'i "qancha kutish kerak"ni o'qiy olsin (429/503 javoblarida).
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins(frontendOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .WithExposedHeaders("Retry-After"));
});

// Render ilovamizning oldida "proxy" bo'lib turadi: server ko'radigan IP —
// proxy'niki, foydalanuvchiniki emas. Haqiqiy IP X-Forwarded-For
// sarlavhasida keladi. ForwardLimit=1 (standart) — faqat eng oxirgi
// (Render qo'shgan) qiymatga ishoniladi, foydalanuvchi o'zi yozib yuborgan
// soxta qiymatlarga emas. Bu rate limiting uchun kerak (pastda).
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// So'rovlar sonini cheklash (rate limiting) — nomli siyosatlar ("auth", "code",
// "poll", "write", "ai", "join") va AI uchun umumiy bir vaqtdagi cheklov:
// batafsil Web/RateLimits.cs da. Rad etilsa — 429 + Retry-After.
builder.Services.AddRateLimiter(RateLimits.Configure);

// So'rov hajmi: umumiy 2 MB (JSON), audio endpoint'lar o'z chegarasini beradi (Web/Uploads.cs).
Uploads.ConfigureDefaults(builder);

// Clean Architecture: har bir qatlam o'z servislarini o'zi ro'yxatdan o'tkazadi.
// Domain — hech narsaga bog'liq emas; Application — Domain'ga; Infrastructure — Application'ga;
// Api (shu loyiha) — hammasini yig'adi (composition root).
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Tartib muhim: avval haqiqiy IP aniqlanadi, keyin CORS, keyin cheklov.
app.UseForwardedHeaders();
// API faqat JSON qaytaradi: brauzer turini "taxmin qilmasin", manzil begona saytlarga sizmasin.
app.Use((context, next) =>
{
    context.Response.Headers.XContentTypeOptions = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    return next(context);
});
app.UseCors("AllowFrontend");
// "AI band" (503 + Retry-After) va juda katta so'rov (413) — hamma endpoint uchun bir joyda.
app.UseAiErrors();
app.UseRateLimiter();

// Eski versiyalar speaking audiolarini shu papkaga yozgan; endi yozilmaydi,
// faqat hisob o'chirilganda qolgan eski fayllar tozalanadi.
var uploadsPath = Path.Combine(builder.Environment.ContentRootPath, "uploads");

app.MapGet("/", () => Results.Ok(new { status = "SpeakingCoach.Api ishlayapti" }));

// Render health check uchun: faqat "jarayon tirikmi" — bazaga murojaat yo'q,
// darhol 200. (Neon uxlab qolganda Render serverni behuda qayta ishga tushirmasin.)
app.MapGet("/health/live", () => Results.Ok(new { status = "ok" }));

// Monitoring (masalan UptimeRobot) uchun: server tirikmi VA bazaga ulana
// oladimi. Baza ishlamasa 503 — "server bor, lekin xizmat ko'rsata olmaydi".
app.MapGet("/health", async (AppDbContext db) =>
{
    var dbOk = await db.Database.CanConnectAsync();
    return dbOk
        ? Results.Ok(new { status = "ok", database = "ok" })
        : Results.Json(new { status = "degraded", database = "unreachable" }, statusCode: StatusCodes.Status503ServiceUnavailable);
});

// Endpoint'lar mavzu bo'yicha alohida fayllarda (Endpoints/ papkasi) —
// Program.cs faqat sozlash va ulash bilan shug'ullanadi.
app.MapAuthEndpoints();
app.MapSpeakingWritingEndpoints();
app.MapComprehensionEndpoints();
app.MapReviewEndpoints();
app.MapProfileEndpoints();
app.MapVocabEndpoints();
app.MapAccountEndpoints(uploadsPath);
app.MapTelegramEndpoints();
app.MapMockEndpoints();
app.MapGroupEndpoints();
app.MapFunEndpoints();
app.MapShadowingEndpoints();
app.MapContentEndpoints();
app.MapMistakeEndpoints();
app.MapTalkEndpoints();
app.MapPart2Endpoints();
app.MapUpgradeEndpoints();
app.MapDictationEndpoints();
app.MapFeedbackEndpoints();

var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
app.Run($"http://0.0.0.0:{port}");
