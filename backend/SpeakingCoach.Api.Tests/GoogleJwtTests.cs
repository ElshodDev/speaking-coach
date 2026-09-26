using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SpeakingCoach.Api.Services;

namespace SpeakingCoach.Api.Tests;

public class GoogleJwtTests
{
    private const string ClientId = "123-abc.apps.googleusercontent.com";
    private static readonly DateTime Now = new(2026, 9, 26, 7, 0, 0, DateTimeKind.Utc);
    private static readonly RSA Key = RSA.Create(2048);
    private static readonly Dictionary<string, RSAParameters> Keys = new() { ["k1"] = Key.ExportParameters(false) };

    private static string B64(byte[] b) => Convert.ToBase64String(b).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static string Token(
        object? payload = null, string kid = "k1", string alg = "RS256", RSA? signWith = null)
    {
        payload ??= new
        {
            iss = "https://accounts.google.com",
            aud = ClientId,
            sub = "1122334455",
            email = "Aziza@Gmail.com",
            email_verified = true,
            name = "Aziza",
            exp = new DateTimeOffset(Now.AddMinutes(30)).ToUnixTimeSeconds(),
        };
        var header = B64(JsonSerializer.SerializeToUtf8Bytes(new { alg, kid, typ = "JWT" }));
        var body = B64(JsonSerializer.SerializeToUtf8Bytes(payload));
        var sig = (signWith ?? Key).SignData(Encoding.ASCII.GetBytes($"{header}.{body}"), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return $"{header}.{body}.{B64(sig)}";
    }

    private static object Claims(string iss = "accounts.google.com", string aud = ClientId, bool verified = true, int expMinutes = 30, string? email = "a@gmail.com") => new
    {
        iss, aud, sub = "1", email, email_verified = verified,
        exp = new DateTimeOffset(Now.AddMinutes(expMinutes)).ToUnixTimeSeconds(),
    };

    [Fact]
    public void Valid_token_gives_normalized_email()
    {
        var p = GoogleJwt.Validate(Token(), Keys, ClientId, Now, out var error);

        Assert.Null(error);
        Assert.Equal("aziza@gmail.com", p!.Email);
        Assert.Equal("1122334455", p.Subject);
        Assert.Equal("Aziza", p.Name);
    }

    [Fact]
    public void Token_for_another_app_is_rejected()
    {
        Assert.Null(GoogleJwt.Validate(Token(Claims(aud: "other-app")), Keys, ClientId, Now, out var error));
        Assert.Equal("aud", error);
    }

    [Fact]
    public void Token_from_another_issuer_is_rejected()
    {
        Assert.Null(GoogleJwt.Validate(Token(Claims(iss: "https://evil.example")), Keys, ClientId, Now, out var error));
        Assert.Equal("iss", error);
    }

    [Fact]
    public void Expired_token_is_rejected_but_small_clock_skew_is_allowed()
    {
        Assert.Null(GoogleJwt.Validate(Token(Claims(expMinutes: -10)), Keys, ClientId, Now, out var error));
        Assert.Equal("expired", error);
        Assert.NotNull(GoogleJwt.Validate(Token(Claims(expMinutes: -2)), Keys, ClientId, Now, out _));
    }

    [Fact]
    public void Unverified_google_email_is_rejected()
    {
        Assert.Null(GoogleJwt.Validate(Token(Claims(verified: false)), Keys, ClientId, Now, out var error));
        Assert.Equal("email_unverified", error);
    }

    [Fact]
    public void Token_signed_with_another_key_is_rejected()
    {
        using var attacker = RSA.Create(2048);
        Assert.Null(GoogleJwt.Validate(Token(signWith: attacker), Keys, ClientId, Now, out var error));
        Assert.Equal("signature", error);
    }

    [Fact]
    public void Tampered_payload_is_rejected()
    {
        var parts = Token().Split('.');
        var forged = B64(JsonSerializer.SerializeToUtf8Bytes(Claims(email: "admin@gmail.com")));
        Assert.Null(GoogleJwt.Validate($"{parts[0]}.{forged}.{parts[2]}", Keys, ClientId, Now, out var error));
        Assert.Equal("signature", error);
    }

    [Fact]
    public void Unknown_key_and_non_rs256_algorithms_are_rejected()
    {
        Assert.Null(GoogleJwt.Validate(Token(kid: "k2"), Keys, ClientId, Now, out var e1));
        Assert.Equal("kid", e1);
        Assert.Null(GoogleJwt.Validate(Token(alg: "none"), Keys, ClientId, Now, out var e2));
        Assert.Equal("alg", e2);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("a.b.c")]
    public void Garbage_is_rejected_without_exceptions(string? token)
    {
        Assert.Null(GoogleJwt.Validate(token, Keys, ClientId, Now, out var error));
        Assert.NotNull(error);
    }

    [Fact]
    public void Jwks_is_parsed_into_rsa_keys()
    {
        var p = Key.ExportParameters(false);
        var json = JsonSerializer.Serialize(new
        {
            keys = new[] { new { kid = "k9", kty = "RSA", alg = "RS256", use = "sig", n = B64(p.Modulus!), e = B64(p.Exponent!) } },
        });

        var keys = GoogleJwt.ParseJwks(json);

        Assert.NotNull(GoogleJwt.Validate(Token(kid: "k9"), keys, ClientId, Now, out _));
    }
}
