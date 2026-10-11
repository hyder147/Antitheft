using Microsoft.Extensions.Configuration;

namespace AntiTheftApp.Api.Tests.Helpers;

public static class TestConfig
{
    // Dummy values used ONLY by tests — not real credentials.
    public const string JwtKey = "unit-test-signing-key-at-least-32-chars-long!!";
    public const string Issuer = "AntiTheftApp.Tests";
    public const string Audience = "AntiTheftApp.Tests.Client";

    public static IConfiguration Create() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = JwtKey,
                ["Jwt:Issuer"] = Issuer,
                ["Jwt:Audience"] = Audience,
                ["Jwt:ExpiryMinutes"] = "60"
            })
            .Build();
}
