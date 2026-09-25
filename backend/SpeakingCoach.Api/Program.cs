using Microsoft.EntityFrameworkCore;
using SpeakingCoach.Api.Data;
using SpeakingCoach.Api.Services;

var builder = WebApplication.CreateBuilder(args);

var frontendOrigin = builder.Configuration["FrontendOrigin"] ?? "http://localhost:5173";

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins(frontendOrigin)
              .AllowAnyMethod()
              .AllowAnyHeader());
});

builder.Services.AddHttpClient();

// GeminiClient — Speaking va Writing ikkalasi ham shu bitta klass orqali
// Gemini'ga so'rov yuboradi (model fallback + retry mantiqi bir joyda).
builder.Services.AddSingleton<GeminiClient>();
builder.Services.AddSingleton<ISpeakingEvaluationService, GeminiSpeakingService>();
builder.Services.AddSingleton<IWritingEvaluationService, GeminiWritingService>();

// Ulanish satri (connection string) standart ASP.NET Core konvensiyasi
// bo'yicha "ConnectionStrings:Default" nomi bilan o'qiladi. Lokalda:
// dotnet user-secrets set "ConnectionStrings:Default" "postgresql://...".
// Railway/Render'da: ConnectionStrings__Default environment variable.
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

app.MapPost("/api/speaking/submit", async (
    HttpRequest request,
    ISpeakingEvaluationService evaluationService,
    AppDbContext db,
    ILogger<Program> logger) =>
{
    if (!request.HasFormContentType)
    {
        return Results.BadRequest(new { error = "multipart/form-data kutilgan edi" });
    }

    var form = await request.ReadFormAsync();
    var audioFile = form.Files.GetFile("audio");
    var topic = form["topic"].ToString();

    if (audioFile is null || audioFile.Length == 0)
    {
        return Results.BadRequest(new { error = "audio fayl topilmadi yoki bo'sh" });
    }

    if (string.IsNullOrWhiteSpace(topic))
    {
        return Results.BadRequest(new { error = "topic maydoni bo'sh bo'lishi mumkin emas" });
    }

    const long maxBytes = 10 * 1024 * 1024; // 10 MB
    if (audioFile.Length > maxBytes)
    {
        return Results.BadRequest(new { error = "Fayl hajmi 10MB dan katta" });
    }

    var submissionId = Guid.NewGuid();
    var fileName = $"{submissionId}.webm";
    var filePath = Path.Combine(uploadsPath, fileName);

    byte[] audioBytes;
    using (var memoryStream = new MemoryStream())
    {
        await audioFile.CopyToAsync(memoryStream);
        audioBytes = memoryStream.ToArray();
    }
    await File.WriteAllBytesAsync(filePath, audioBytes, default);

    try
    {
        logger.LogInformation("Submission {Id}: Gemini'ga yuborilmoqda ({Size} bayt)", submissionId, audioBytes.Length);
        var evaluation = await evaluationService.EvaluateAsync(topic, audioBytes, audioFile.ContentType);

        // Natijani bazaga yozamiz. Bu Gemini so'rovidan KEYIN qilinadi —
        // agar Gemini xato qaytarsa (masalan 503), hech narsa saqlanmaydi,
        // shuning uchun tarixda faqat muvaffaqiyatli urinishlar ko'rinadi.
        var activity = new Activity
        {
            Id = submissionId,
            Type = ActivityType.Speaking,
            CreatedAtUtc = DateTime.UtcNow,
            PromptData = System.Text.Json.JsonSerializer.Serialize(new { topic }),
            ResponseData = System.Text.Json.JsonSerializer.Serialize(evaluation),
        };
        db.Activities.Add(activity);
        await db.SaveChangesAsync();

        return Results.Ok(new { submissionId, evaluation });
    }
    catch (Exception ex)
    {
        // Setup bosqichida tuzatish tezroq bo'lishi uchun xato sababini
        // ochiq qoldiryapmiz (masalan noto'g'ri API kalit). Real
        // foydalanuvchilar bilan ishga tushirganda buni generic xabarga
        // almashtiring — xavfsizlik jadvalida muhokama qilingan.
        logger.LogError(ex, "Submission {Id}: xato", submissionId);
        return Results.Problem(detail: ex.Message, statusCode: 502);
    }
});

app.MapGet("/api/speaking/history", async (AppDbContext db) =>
{
    // Eng so'nggi 20 ta Speaking urinishini qaytaradi, yangisi birinchi.
    // Hozircha barcha foydalanuvchilarning natijalari birga ko'rinadi —
    // login tizimi yo'q. Bu ataylab shunday: login qo'shish alohida katta
    // ish (parol, sessiya, xavfsizlik), portfolio uchun hozircha "bazaga
    // yozish va o'qish ishlayapti"ni ko'rsatish yetarli.
    var items = await db.Activities
        .Where(a => a.Type == ActivityType.Speaking)
        .OrderByDescending(a => a.CreatedAtUtc)
        .Take(20)
        .Select(a => new
        {
            a.Id,
            a.CreatedAtUtc,
            a.PromptData,
            a.ResponseData,
        })
        .ToListAsync();

    return Results.Ok(items);
});

// Writing so'rov shakli — Speaking'dan farqli, JSON body sifatida keladi
// (audio fayl yo'q, shuning uchun multipart/form-data shart emas).
app.MapPost("/api/writing/submit", async (
    WritingSubmitRequest request,
    IWritingEvaluationService evaluationService,
    AppDbContext db,
    ILogger<Program> logger) =>
{
    if (string.IsNullOrWhiteSpace(request.Topic))
    {
        return Results.BadRequest(new { error = "topic maydoni bo'sh bo'lishi mumkin emas" });
    }

    if (string.IsNullOrWhiteSpace(request.Text))
    {
        return Results.BadRequest(new { error = "text maydoni bo'sh bo'lishi mumkin emas" });
    }

    // Juda qisqa matn Gemini'dan mazmunli baholash olishga yetarli emas;
    // juda uzun matn esa (nusxa-ko'chirilgan kitob bo'limi kabi) prompt
    // hajmini keraksiz shishiradi — shuning uchun ikkala tomondan chegara.
    const int minChars = 20;
    const int maxChars = 5000;
    if (request.Text.Length < minChars)
    {
        return Results.BadRequest(new { error = $"Matn juda qisqa (kamida {minChars} belgi kerak)" });
    }
    if (request.Text.Length > maxChars)
    {
        return Results.BadRequest(new { error = $"Matn juda uzun (ko'pi bilan {maxChars} belgi)" });
    }

    var submissionId = Guid.NewGuid();

    try
    {
        logger.LogInformation("Writing submission {Id}: Gemini'ga yuborilmoqda ({Length} belgi)", submissionId, request.Text.Length);
        var evaluation = await evaluationService.EvaluateAsync(request.Topic, request.Text);

        var activity = new Activity
        {
            Id = submissionId,
            Type = ActivityType.Writing,
            CreatedAtUtc = DateTime.UtcNow,
            PromptData = System.Text.Json.JsonSerializer.Serialize(new { topic = request.Topic, text = request.Text }),
            ResponseData = System.Text.Json.JsonSerializer.Serialize(evaluation),
        };
        db.Activities.Add(activity);
        await db.SaveChangesAsync();

        return Results.Ok(new { submissionId, evaluation });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Writing submission {Id}: xato", submissionId);
        return Results.Problem(detail: ex.Message, statusCode: 502);
    }
});

app.MapGet("/api/writing/history", async (AppDbContext db) =>
{
    // Speaking'dagi /api/speaking/history bilan bir xil mantiq, faqat
    // Type filtri boshqa — bitta Activities jadvali bo'lgani uchun bu
    // ikki endpoint deyarli bir xil, faqat WHERE sharti farq qiladi.
    var items = await db.Activities
        .Where(a => a.Type == ActivityType.Writing)
        .OrderByDescending(a => a.CreatedAtUtc)
        .Take(20)
        .Select(a => new
        {
            a.Id,
            a.CreatedAtUtc,
            a.PromptData,
            a.ResponseData,
        })
        .ToListAsync();

    return Results.Ok(items);
});

var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
app.Run($"http://0.0.0.0:{port}");

// Minimal API'da JSON body qabul qilish uchun oddiy record — ASP.NET Core
// buni so'rov gavdasidan (request body) avtomatik deserialize qiladi.
record WritingSubmitRequest(string Topic, string Text);
