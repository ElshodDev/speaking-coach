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

// So'rovlar sonini cheklash (rate limiting), har bir IP uchun alohida:
// - "auth": daqiqasiga 10 ta — parolni ketma-ket taxmin qilishni sekinlashtiradi;
// - "ai": daqiqasiga 30 ta — Gemini bepul kvotasini bitta odam tugatib
//   qo'ymasligi uchun (barqarorlik testi 6 ta so'rov — bemalol sig'adi).
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, ct) =>
    {
        await context.HttpContext.Response.WriteAsJsonAsync(
            context.HttpContext.Request.Error("rate_limited"), ct);
    };

    options.AddPolicy("auth", http => RateLimitPartition.GetFixedWindowLimiter(
        http.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));

    options.AddPolicy("ai", http => RateLimitPartition.GetFixedWindowLimiter(
        http.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 30, Window = TimeSpan.FromMinutes(1) }));
});

builder.Services.AddHttpClient();

// GeminiClient — barcha servislar shu bitta klass orqali Gemini'ga so'rov
// yuboradi (model fallback + retry + qat'iy JSON o'qish bir joyda).
builder.Services.AddSingleton<GeminiClient>();
builder.Services.AddSingleton<ISpeakingEvaluationService, GeminiSpeakingService>();
builder.Services.AddSingleton<IWritingEvaluationService, GeminiWritingService>();
builder.Services.AddSingleton<IComprehensionService, GeminiComprehensionService>();
builder.Services.AddSingleton<IWordService, GeminiWordService>();
builder.Services.AddSingleton<AdminOptions>();
builder.Services.AddSingleton<IEmailSender, BrevoEmailSender>();
builder.Services.AddSingleton<IGoogleSignIn, GoogleSignInService>();
builder.Services.AddSingleton<AiQuotaOptions>();
builder.Services.AddSingleton<GuestQuotaStore>();
builder.Services.AddScoped<AiQuotaService>();

// PasswordHasher holatsiz (stateless) — singleton yetarli. AuthService va
// ReviewService esa AppDbContext'ga bog'liq; DbContext har so'rov uchun
// alohida (scoped) bo'lgani uchun ular ham scoped.
builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ReviewService>();
builder.Services.AddScoped<ProgressService>();
builder.Services.AddScoped<AdminService>();

// Ulanish satri (connection string) standart ASP.NET Core konvensiyasi
// bo'yicha "ConnectionStrings:Default" nomi bilan o'qiladi. Lokalda:
// dotnet user-secrets set "ConnectionStrings:Default" "...".
// Render'da: ConnectionStrings__Default environment variable.
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException(
        "ConnectionStrings:Default sozlanmagan. Lokalda: dotnet user-secrets set " +
        "\"ConnectionStrings:Default\" \"<Neon'dan olingan connection string>\".");
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

var app = builder.Build();

// Tartib muhim: avval haqiqiy IP aniqlanadi, keyin CORS, keyin cheklov.
app.UseForwardedHeaders();
app.UseCors("AllowFrontend");
app.UseRateLimiter();

var uploadsPath = Path.Combine(builder.Environment.ContentRootPath, "uploads");
Directory.CreateDirectory(uploadsPath);

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
app.MapSpeakingWritingEndpoints(uploadsPath);
app.MapComprehensionEndpoints();
app.MapReviewEndpoints();
app.MapProfileEndpoints();
app.MapVocabEndpoints();
app.MapAccountEndpoints(uploadsPath);

var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
app.Run($"http://0.0.0.0:{port}");
