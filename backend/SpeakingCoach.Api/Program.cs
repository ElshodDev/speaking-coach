using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SpeakingCoach.Api.Data;
using SpeakingCoach.Api.Endpoints;
using SpeakingCoach.Api.Services;

var builder = WebApplication.CreateBuilder(args);

var frontendOrigin = builder.Configuration["FrontendOrigin"] ?? "http://localhost:5173";

builder.Services.AddCors(options =>
{
    // Token cookie'da emas, "Authorization" sarlavhasida yuboriladi —
    // shuning uchun AllowCredentials() shart emas; AllowAnyHeader() esa
    // Authorization sarlavhasiga ham ruxsat beradi.
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins(frontendOrigin)
              .AllowAnyMethod()
              .AllowAnyHeader());
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

// So'rovlar sonini cheklash (rate limiting):
// - "auth": IP uchun daqiqasiga 10 ta — parolni ketma-ket taxmin qilishni sekinlashtiradi;
// - "ai": foydalanuvchi (token) yoki mehmon (IP) uchun daqiqasiga 30 ta;
// - GlobalLimiter: AI so'rovlari bir vaqtda nechta bo'lishi (foydalanuvchi,
//   IP va butun server bo'yicha) — batafsil Services/RateLimits.cs da.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, ct) =>
    {
        await context.HttpContext.Response.WriteAsJsonAsync(
            context.HttpContext.Request.Error("rate_limited"), ct);
    };

    options.AddPolicy("auth", http => RateLimitPartition.GetFixedWindowLimiter(
        RateLimits.Ip(http),
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));

    options.AddPolicy(RateLimits.AiPolicy, http => RateLimitPartition.GetFixedWindowLimiter(
        RateLimits.User(http),
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 30, Window = TimeSpan.FromMinutes(1) }));

    options.AddPolicy(RateLimits.JoinPolicy, http => RateLimitPartition.GetFixedWindowLimiter(
        RateLimits.Ip(http),
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 60, Window = TimeSpan.FromMinutes(1) }));

    options.GlobalLimiter = RateLimits.AiGlobal();
});

builder.Services.AddHttpClient();

// GeminiClient — barcha servislar shu bitta klass orqali Gemini'ga so'rov
// yuboradi (model fallback + retry + qat'iy JSON o'qish bir joyda).
builder.Services.AddSingleton<GeminiClient>();
builder.Services.AddSingleton<ISpeakingEvaluationService, GeminiSpeakingService>();
builder.Services.AddSingleton<IWritingEvaluationService, GeminiWritingService>();
builder.Services.AddSingleton<IComprehensionService, GeminiComprehensionService>();
builder.Services.AddSingleton<IWordService, GeminiWordService>();
builder.Services.AddSingleton<SpeakingCoach.Api.Services.Mock.IIeltsEvaluator, SpeakingCoach.Api.Services.Mock.GeminiIeltsEvaluator>();
builder.Services.AddSingleton<SpeakingCoach.Api.Services.Mock.IMockGenerator, SpeakingCoach.Api.Services.Mock.GeminiMockGenerator>();
// Bot orqali test qo'shish — o'sha generator (bitta nusxa, ikki interfeys).
builder.Services.AddSingleton<SpeakingCoach.Api.Services.Mock.IMockAuthoring>(sp => (SpeakingCoach.Api.Services.Mock.GeminiMockGenerator)sp.GetRequiredService<SpeakingCoach.Api.Services.Mock.IMockGenerator>());
builder.Services.AddSingleton<SpeakingCoach.Api.Services.Mock.ICefrEvaluator, SpeakingCoach.Api.Services.Mock.GeminiCefrEvaluator>();
builder.Services.AddSingleton<AdminOptions>();
builder.Services.AddSingleton<IEmailSender, BrevoEmailSender>();
builder.Services.AddSingleton<IGoogleSignIn, GoogleSignInService>();
builder.Services.AddSingleton<AiQuotaOptions>();
builder.Services.AddSingleton<GuestQuotaStore>();
builder.Services.AddSingleton(new DemoGate());
builder.Services.AddSingleton<SpeakingCoach.Api.Services.Content.ContentAi>();
builder.Services.AddScoped<AiQuotaService>();

// Telegram bot: token bo'lmasa — hammasi o'chiq, ilova avvalgidek ishlaydi.
builder.Services.AddSingleton<SpeakingCoach.Api.Services.Telegram.TelegramOptions>();
builder.Services.AddSingleton<SpeakingCoach.Api.Services.Telegram.ITelegramApi, SpeakingCoach.Api.Services.Telegram.TelegramApi>();
builder.Services.AddScoped<SpeakingCoach.Api.Services.Telegram.TelegramBot>();
builder.Services.AddSingleton<SpeakingCoach.Api.Services.Telegram.AuthoringRunner>();
builder.Services.AddScoped<SpeakingCoach.Api.Services.Telegram.TelegramReminders>();
builder.Services.AddScoped<SpeakingCoach.Api.Services.Telegram.AssignmentNotifier>();
builder.Services.AddHostedService<SpeakingCoach.Api.Services.Telegram.TelegramStartup>();

// Ishga tushganda: bazada yetishmayotgan jadval/migratsiya bo'lsa — logga aniq xato.
builder.Services.AddHostedService<SchemaStartupCheck>();

// PasswordHasher holatsiz (stateless) — singleton yetarli. AuthService va
// ReviewService esa AppDbContext'ga bog'liq; DbContext har so'rov uchun
// alohida (scoped) bo'lgani uchun ular ham scoped.
builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ReviewService>();
builder.Services.AddScoped<TodayService>();
builder.Services.AddScoped<SpeakingCoach.Api.Services.Mock.MockSets>();
builder.Services.AddSingleton(new SpeakingCoach.Api.Services.Mock.GenerationGate());
builder.Services.AddScoped<SpeakingCoach.Api.Services.Mock.ShadowingService>();
builder.Services.AddSingleton<SpeakingCoach.Api.Services.Mock.IShadowingCoach, SpeakingCoach.Api.Services.Mock.GeminiShadowingCoach>();
builder.Services.AddScoped<ProgressService>();
builder.Services.AddScoped<AdminService>();

// Ulanish satri (connection string) standart ASP.NET Core konvensiyasi
// bo'yicha "ConnectionStrings:Default" nomi bilan o'qiladi. Lokalda:
// dotnet user-secrets set "ConnectionStrings:Default" "...".
// Render'da: ConnectionStrings__Default environment variable.
var connectionString = DbConnection.Normalize(builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException(
        "ConnectionStrings:Default sozlanmagan. Lokalda: dotnet user-secrets set " +
        "\"ConnectionStrings:Default\" \"<Neon'dan olingan connection string>\"."));
// Neon (serverless) ba'zan ulanishni uzib qo'yadi yoki "uyg'onayotgan" bo'ladi —
// vaqtinchalik xatolarda so'rov 3 martagacha qayta bajariladi.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure(3)));

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
app.UseRateLimiter();

// Eski versiyalar speaking audiolarini shu papkaga yozgan; endi yozilmaydi,
// faqat hisob o'chirilganda qolgan eski fayllar tozalanadi.
var uploadsPath = Path.Combine(builder.Environment.ContentRootPath, "uploads");

app.MapGet("/", () => Results.Ok(new { status = "SpeakingCoach.Api ishlayapti" }));

// Monitoring (masalan UptimeRobot) va Render uchun: server tirikmi VA
// bazaga ulana oladimi. Baza ishlamasa 503 — "server bor, lekin xizmat
// ko'rsata olmaydi".
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

var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
app.Run($"http://0.0.0.0:{port}");
