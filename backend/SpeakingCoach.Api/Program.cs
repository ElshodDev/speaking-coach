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
builder.Services.AddSingleton<ISpeakingEvaluationService, GeminiSpeakingService>();

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
        logger.LogError(ex, "Submission {Id}: xato", submissionId);
        return Results.Problem(detail: ex.Message, statusCode: 502);
    }
});

app.MapGet("/api/speaking/history", async (AppDbContext db) =>
{
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

var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
app.Run($"http://0.0.0.0:{port}");
