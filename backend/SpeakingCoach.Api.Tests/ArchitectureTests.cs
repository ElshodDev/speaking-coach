using System.Reflection;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Clean Architecture qoidasi: bog'liqliklar faqat ichkariga qarab yo'naladi.
/// Domain ← Application ← Infrastructure ← Api. Kimdir qoidani buzsa (masalan,
/// Domain'dan EF'ga yoki Application'dan ASP.NET'ga murojaat qilsa) — test yiqiladi.
/// </summary>
public class ArchitectureTests
{
    private static readonly Assembly Domain = typeof(User).Assembly;
    private static readonly Assembly Application = typeof(AuthService).Assembly;
    private static readonly Assembly Infrastructure = typeof(AppDbContext).Assembly;

    private static HashSet<string> Refs(Assembly a) => a.GetReferencedAssemblies().Select(r => r.Name!).ToHashSet();

    [Fact]
    public void Domain_depends_on_nothing_but_the_base_library()
    {
        var refs = Refs(Domain);
        Assert.DoesNotContain(refs, r => r.StartsWith("SpeakingCoach"));
        Assert.DoesNotContain(refs, r => r.StartsWith("Microsoft.EntityFrameworkCore"));
        Assert.DoesNotContain(refs, r => r.StartsWith("Microsoft.AspNetCore"));
        Assert.DoesNotContain(refs, r => r.StartsWith("Npgsql"));
    }

    [Fact]
    public void Application_depends_only_on_domain_and_no_web_or_database_provider()
    {
        var refs = Refs(Application);
        Assert.Contains("SpeakingCoach.Domain", refs);
        Assert.DoesNotContain("SpeakingCoach.Infrastructure", refs);
        Assert.DoesNotContain("SpeakingCoach.Api", refs);
        Assert.DoesNotContain(refs, r => r.StartsWith("Npgsql"));
        // HttpRequest/IResult — faqat Api qatlamida.
        Assert.DoesNotContain("Microsoft.AspNetCore.Http.Abstractions", refs);
        Assert.DoesNotContain("Microsoft.AspNetCore.Http", refs);
        Assert.DoesNotContain(Application.GetTypes(), t => t.GetMethods().Any(m => m.GetParameters().Any(p => p.ParameterType.FullName == "Microsoft.AspNetCore.Http.HttpRequest")));
    }

    [Fact]
    public void Infrastructure_does_not_depend_on_the_api()
    {
        Assert.DoesNotContain("SpeakingCoach.Api", Refs(Infrastructure));
    }

    [Fact]
    public void Namespaces_match_their_layer()
    {
        Assert.All(Domain.GetTypes().Where(t => t.Namespace is not null && !t.Namespace.StartsWith("System")), t => Assert.StartsWith("SpeakingCoach.Domain", t.Namespace));
        Assert.All(Application.GetTypes().Where(t => t.Namespace is not null && !t.Namespace.StartsWith("System") && !t.Namespace.StartsWith("Microsoft")), t => Assert.StartsWith("SpeakingCoach.Application", t.Namespace));
    }

    [Fact]
    public void Ef_model_matches_the_migrations_snapshot()
    {
        // Ulanishsiz: faqat model va oxirgi migratsiya snapshot'i solishtiriladi.
        // Entity'lar yangi namespace'ga ko'chganda ham baza sxemasi o'zgarmasligi shart.
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=none;Username=none;Password=none")
            .Options;
        using var db = new AppDbContext(options);
        Assert.False(db.Database.HasPendingModelChanges(), "Model migratsiyalardan farq qiladi — yangi migratsiya kerak yoki snapshot buzilgan.");
    }

    // Mantiq Application servislariga ko'chirilgan endpointlar: ularda AppDbContext (konkret baza) ishlatilmaydi.
    // Yangi endpointlar ham shu yo'ldan borishi uchun ro'yxat asta-sekin kengaytiriladi (qarang: docs/ARCHITECTURE.md).
    [Theory]
    [InlineData(typeof(SpeakingCoach.Api.Endpoints.GroupEndpoints))]
    [InlineData(typeof(SpeakingCoach.Api.Endpoints.AccountEndpoints))]
    [InlineData(typeof(SpeakingCoach.Api.Endpoints.ComprehensionEndpoints))]
    [InlineData(typeof(SpeakingCoach.Api.Endpoints.HistoryQueries))]
    [InlineData(typeof(SpeakingCoach.Api.Endpoints.MockEndpoints))]
    [InlineData(typeof(SpeakingCoach.Api.Endpoints.TelegramEndpoints))]
    public void Thin_endpoints_do_not_touch_the_concrete_db_context(Type endpoints)
    {
        const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;
        // Lambdalar kompilyator yaratgan ichki sinfda ("<>c") turadi — ularni ham tekshiramiz.
        var types = new[] { endpoints }.Concat(endpoints.GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Public));
        var offenders = types
            .SelectMany(t => t.GetMethods(all))
            .Where(m => m.GetParameters().Any(p => p.ParameterType == typeof(AppDbContext)) || m.ReturnType == typeof(AppDbContext))
            .Select(m => $"{m.DeclaringType!.Name}.{m.Name}")
            .ToList();
        Assert.True(offenders.Count == 0, $"{endpoints.Name} AppDbContext'ga to'g'ridan-to'g'ri bog'liq: {string.Join(", ", offenders)}. Mantiqni Application servisiga ko'chiring.");
    }
}
