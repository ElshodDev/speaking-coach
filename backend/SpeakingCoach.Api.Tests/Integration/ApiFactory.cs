using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
namespace SpeakingCoach.Api.Tests.Integration;

/// <summary>
/// Haqiqiy HTTP qatlami uchun test serveri: butun ilova (marshrutlar, rate limiting, CORS, xato xaritasi) ishga tushadi,
/// lekin baza — xotiradagi SQLite, Gemini/Telegram/email — soxta. Postgres'ga xos so'rovlar (IMockRows, IPracticeHistory)
/// xotirada LINQ bilan almashtirilgan; fon vazifalari (webhook, sxema tekshiruvi, tozalash) o'chirilgan.
/// Har bir test sinfi o'z nusxasini oladi (IClassFixture) — testlar bir-biriga ta'sir qilmasligi uchun foydalanuvchilar har safar yangi.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    public const string AdminEmail = "admin@site.uz";
    public const string FrontendOrigin = "https://app.example.com";
    public const string TelegramToken = "123:TESTTOKEN";

    // Har bir fabrika — o'z xotiradagi SQLite bazasi (ulanish ochiq turadi). SQLite ExecuteUpdate/ExecuteDelete'ni qo'llaydi (EF InMemory — yo'q),
    // shuning uchun limitlar, sessiya uzaytirish va tozalash so'rovlari haqiqiy SQL bilan ishlaydi.
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly Dictionary<string, string?> _settings;

    /// <summary>Soxta AI: test o'zgartiradi (xato yoki tayyor natija).</summary>
    public FakeAi Ai { get; } = new();

    public ApiFactory() : this(telegram: false) { }

    protected ApiFactory(bool telegram)
    {
        _settings = new()
        {
            ["ConnectionStrings:Default"] = "Host=localhost;Database=integration;Username=t;Password=t",
            ["FrontendOrigin"] = FrontendOrigin,
            ["Admin:Emails"] = AdminEmail,
            ["Gemini:ApiKey"] = "test-key-never-used",   // soxta: AI chaqiruvlari Fakes.cs bilan almashtirilgan
        };
        if (telegram)
        {
            _settings["Telegram:BotToken"] = TelegramToken;
            _settings["Telegram:BotUsername"] = "TestBot";
            _settings["Telegram:CronSecret"] = "cron-secret";
            _settings["RENDER_EXTERNAL_URL"] = "https://api.example.com";
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        foreach (var (key, value) in _settings) builder.UseSetting(key, value);

        builder.ConfigureServices(services =>
        {
            // Baza: Npgsql o'rniga xotira.
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<DbContextOptions>();
            foreach (var d in services.Where(d => d.ServiceType.IsGenericType
                && d.ServiceType.Name.StartsWith("IDbContextOptionsConfiguration")
                && d.ServiceType.GenericTypeArguments[0] == typeof(AppDbContext)).ToList())
                services.Remove(d);
            _connection.Open();
            services.AddDbContext<AppDbContext>(o => o.UseSqlite(_connection));

            // Fon vazifalari: tarmoq va Postgres'ga bog'liq.
            foreach (var d in services.Where(d => d.ServiceType == typeof(IHostedService) && d.ImplementationType?.Namespace?.StartsWith("SpeakingCoach") == true).ToList())
                services.Remove(d);

            // Postgres'ga xos so'rovlar — xotirada.
            services.RemoveAll<IMockRows>();
            services.AddScoped<IMockRows, InMemoryMockRows>();
            services.RemoveAll<IPracticeHistory>();
            services.AddScoped<IPracticeHistory, InMemoryPracticeHistory>();

            // Tashqi xizmatlar — soxta.
            services.RemoveAll<ISpeakingEvaluationService>();
            services.AddSingleton<ISpeakingEvaluationService>(Ai);
            services.RemoveAll<ITalkAi>();
            services.AddSingleton<ITalkAi>(Ai);
            services.RemoveAll<IWritingEvaluationService>();
            services.AddSingleton<IWritingEvaluationService>(Ai);
            services.RemoveAll<ITelegramApi>();
            services.AddSingleton(Ai.Telegram.Api);
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var scope = host.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
        return host;
    }

    /// <summary>Yangi foydalanuvchi va unga sessiya; "Authorization: Bearer" bilan tayyor mijoz.</summary>
    public async Task<TestUser> CreateUserAsync(string? email = null, string? displayName = null)
    {
        email ??= $"user-{Guid.NewGuid():N}@site.uz";
        var token = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        User user;
        using (var scope = Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            // Email noyob: bir xil email (masalan, admin) qayta so'ralsa — o'sha foydalanuvchiga yangi sessiya.
            user = await db.Users.FirstOrDefaultAsync(u => u.Email == email)
                ?? new User { Id = Guid.NewGuid(), Email = email, DisplayName = displayName, Level = "B1", CreatedAtUtc = DateTime.UtcNow, EmailVerifiedAtUtc = DateTime.UtcNow };
            if (db.Entry(user).State == EntityState.Detached) db.Users.Add(user);
            db.Sessions.Add(new Session
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TokenHash = AuthService.HashToken(token),
                CreatedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(1),
            });
            await db.SaveChangesAsync();
        }
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return new TestUser(user.Id, email, client);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _connection.Dispose();
    }

    /// <summary>Bazadagi ma'lumotni tekshirish/tayyorlash uchun yordamchi.</summary>
    public async Task<T> WithDbAsync<T>(Func<AppDbContext, Task<T>> action)
    {
        using var scope = Services.CreateScope();
        return await action(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    public Task WithDbAsync(Func<AppDbContext, Task> action) => WithDbAsync<int>(async db => { await action(db); return 0; });
}

/// <summary>Telegram ulangan test serveri (Telegram:BotToken berilgan).</summary>
public class TelegramApiFactory : ApiFactory
{
    public TelegramApiFactory() : base(telegram: true) { }
}

public record TestUser(Guid Id, string Email, HttpClient Client);

public static class JsonExtensions
{
    public static async Task<JsonElement> JsonAsync(this HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(text);
        return doc.RootElement.Clone();
    }

    public static StringContent Json(object body) =>
        new(JsonSerializer.Serialize(body), System.Text.Encoding.UTF8, "application/json");
}

public static class Msg
{
    /// <summary>Xato matni standart tilda (so'rovda Accept-Language bo'lmasa shu til qaytadi).</summary>
    public static string Err(string key, params object?[] args) => Texts.Get(Texts.DefaultLang, key, args);
}
