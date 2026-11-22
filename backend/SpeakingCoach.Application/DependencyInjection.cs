using Microsoft.AspNetCore.Identity;

namespace SpeakingCoach.Application;

/// <summary>Application qatlamining servislarini DI'ga qo'shish (Program.cs faqat shuni chaqiradi).</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<FeatureFlags>();
        services.AddMemoryCache();

        // Butun server bo'yicha Gemini byudjeti va saqlagich (Ai:GlobalPerMinute, Ai:GlobalPerDay, Ai:Models...).
        services.AddSingleton(sp => new AiGuardOptions(sp.GetRequiredService<IConfiguration>()));
        services.AddSingleton<AiGuard>();

        // AI bilan ishlovchi foydalanish holatlari: barchasi IGeminiClient orqali.
        services.AddSingleton<ISpeakingEvaluationService, GeminiSpeakingService>();
        services.AddSingleton<IPart2Coach, GeminiPart2Coach>();
        services.AddSingleton<IAnswerUpgrader, GeminiAnswerUpgrader>();
        services.AddSingleton<ITalkAi, GeminiTalkAi>();
        services.AddSingleton<ITutorAi, GeminiTutorAi>();
        services.AddSingleton<TutorLessonCache>();
        services.AddSingleton<IWritingEvaluationService, GeminiWritingService>();
        services.AddSingleton<IComprehensionService, GeminiComprehensionService>();
        services.AddSingleton<IWordService, GeminiWordService>();
        services.AddSingleton<IIeltsEvaluator, GeminiIeltsEvaluator>();
        services.AddSingleton<IMockGenerator, GeminiMockGenerator>();
        // Bot orqali test qo'shish — o'sha generator (bitta nusxa, ikki interfeys).
        services.AddSingleton<IMockAuthoring>(sp => (GeminiMockGenerator)sp.GetRequiredService<IMockGenerator>());
        services.AddSingleton<ICefrEvaluator, GeminiCefrEvaluator>();
        services.AddSingleton<IShadowingCoach, GeminiShadowingCoach>();
        services.AddSingleton<ContentAi>();

        // Limitlar va himoya.
        services.AddSingleton<AdminOptions>();
        services.AddSingleton<AiQuotaOptions>();
        services.AddSingleton<GuestQuotaStore>();
        services.AddSingleton<AiDailyCaps>();
        services.AddSingleton<AiInFlight>();
        services.AddSingleton<TalkLocks>();
        services.AddSingleton(new DemoGate());
        services.AddSingleton(new GenerationGate());

        // Telegram bot mantiqi (API mijozi — Infrastructure'da).
        services.AddSingleton<TelegramOptions>();
        services.AddScoped<TelegramBot>();
        services.AddSingleton<AuthoringRunner>();
        services.AddScoped<TelegramReminders>();
        services.AddSingleton<ReminderJob>();
        services.AddScoped<AssignmentNotifier>();

        // Bazaga bog'liq servislar — DbContext har so'rov uchun alohida (scoped), ular ham scoped.
        // PasswordHasher holatsiz (stateless) — singleton yetarli.
        services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddScoped<AuthService>();
        services.AddScoped<AiQuotaService>();
        services.AddScoped<ReferralService>();
        services.AddScoped<ReviewService>();
        services.AddScoped<TutorService>();
        services.AddScoped<PersonalShadowingService>();
        services.AddScoped<TodayService>();
        services.AddScoped<MockSets>();
        services.AddScoped<MockExamService>();
        services.AddScoped<TelegramLinkService>();
        services.AddScoped<ShadowingService>();
        services.AddScoped<ProgressService>();
        services.AddScoped<ReadinessService>();
        services.AddScoped<AdminService>();
        services.AddScoped<GroupService>();
        services.AddScoped<AccountDataService>();
        services.AddScoped<ComprehensionPractice>();
        services.AddScoped<RetentionService>();
        return services;
    }
}
