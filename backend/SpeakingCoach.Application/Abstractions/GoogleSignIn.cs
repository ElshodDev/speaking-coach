using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SpeakingCoach.Application.Abstractions;

/// <summary>Google ID token'idan olinadigan, tekshirilgan ma'lumotlar.</summary>
public record GooglePayload(string Subject, string Email, string? Name);

public interface IGoogleSignIn
{
    /// <summary>Client ID sozlanganmi (sozlanmagan bo'lsa tugma ko'rsatilmaydi).</summary>
    string? ClientId { get; }

    Task<GooglePayload?> ValidateAsync(string? idToken, CancellationToken ct = default);
}
