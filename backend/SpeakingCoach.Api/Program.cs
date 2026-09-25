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

builder.Services.AddHttpClient();

// GeminiClient — barcha servislar shu bitta klass orqali Gemini'ga so'rov
// yuboradi (model fallback + retry + qat'iy JSON o'qish bir joyda).
builder.Services.AddSingleton<GeminiClient>();
builder.Services.AddSingleton<ISpeakingEvaluationService, GeminiSpeakingService>();
builder.Services.AddSingleton<IWritingEvaluationService, GeminiWritingService>();
builder.Services.AddSingleton<IComprehensionService, GeminiComprehensionService>();

// Autentifikatsiya: PasswordHasher holatsiz (stateless) — singleton yetarli.
// AuthService esa AppDbContext'ga bog'liq, DbContext har so'rov uchun
// alohida (scoped) bo'lgani uchun u ham scoped.
builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<AuthService>();

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

app.UseCors("AllowFrontend");

var uploadsPath = Path.Combine(builder.Environment.ContentRootPath, "uploads");
Directory.CreateDirectory(uploadsPath);

app.MapGet("/", () => Results.Ok(new { status = "SpeakingCoach.Api ishlayapti" }));

// Endpoint'lar mavzu bo'yicha alohida fayllarda (Endpoints/ papkasi) —
// Program.cs faqat sozlash va ulash bilan shug'ullanadi.
app.MapAuthEndpoints();
app.MapSpeakingWritingEndpoints(uploadsPath);
app.MapComprehensionEndpoints();

var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
app.Run($"http://0.0.0.0:{port}");
