using Microsoft.EntityFrameworkCore;

namespace SpeakingCoach.Infrastructure;

/// <summary>Infrastructure qatlami: baza, tashqi xizmatlar mijozlari va fon vazifalari.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Ulanish satri standart ASP.NET Core konvensiyasi bo'yicha "ConnectionStrings:Default".
        // Lokalda: dotnet user-secrets set "ConnectionStrings:Default" "...". Render'da: ConnectionStrings__Default.
        var connectionString = DbConnection.Normalize(configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:Default sozlanmagan. Lokalda: dotnet user-secrets set " +
                "\"ConnectionStrings:Default\" \"<Neon'dan olingan connection string>\"."));
        // Neon (serverless) ba'zan ulanishni uzib qo'yadi yoki "uyg'onayotgan" bo'ladi —
        // vaqtinchalik xatolarda so'rov 3 martagacha qayta bajariladi.
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure(3)));
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<IMockRows, MockRowsQuery>();
        services.AddScoped<ISchemaInspector, SchemaInspector>();

        // Tashqi xizmatlar.
        services.AddHttpClient();
        // GeminiClient — barcha AI so'rovlari shu orqali (model zaxirasi + qayta urinish bir joyda).
        services.AddSingleton<IGeminiClient, GeminiClient>();
        services.AddSingleton<IEmailSender, BrevoEmailSender>();
        services.AddSingleton<IGoogleSignIn, GoogleSignInService>();
        // Telegram: token bo'lmasa — hammasi o'chiq, ilova avvalgidek ishlaydi.
        services.AddSingleton<ITelegramApi, TelegramApi>();

        // Fon vazifalari: webhook o'rnatish, sxema tekshiruvi (yetishmayotgan migratsiya — logga aniq xato),
        // eski yozuvlarni tozalash (sessiyalar, kodlar, tashlab ketilgan hisoblar).
        services.AddHostedService<TelegramStartup>();
        services.AddHostedService<SchemaStartupCheck>();
        services.AddHostedService<MaintenanceService>();
        return services;
    }
}
