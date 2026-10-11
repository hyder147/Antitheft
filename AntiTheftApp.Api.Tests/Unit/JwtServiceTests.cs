using System.IdentityModel.Tokens.Jwt;
using AntiTheftApp.Api.Models;
using AntiTheftApp.Api.Services;
using AntiTheftApp.Api.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace AntiTheftApp.Api.Tests.Unit;

public class JwtServiceTests
{
    [Fact]
    public void GenerateToken_ContainsUserIdEmailIssuerAndAudience()
    {
        var user = new User { Email = "ali@example.com" };
        var service = new JwtService(TestConfig.Create());

        var (token, _) = service.GenerateToken(user);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        jwt.Subject.Should().Be(user.Id.ToString());
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == "ali@example.com");
        jwt.Issuer.Should().Be(TestConfig.Issuer);
        jwt.Audiences.Should().Contain(TestConfig.Audience);
    }

    [Fact]
    public void GenerateToken_ExpiryMatchesConfiguredMinutes()
    {
        var service = new JwtService(TestConfig.Create());

        var (_, expiresAt) = service.GenerateToken(new User { Email = "a@b.com" });

        expiresAt.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(60), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void GenerateToken_WithoutKey_Throws()
    {
        var emptyConfig = new ConfigurationBuilder().Build();
        var service = new JwtService(emptyConfig);

        var act = () => service.GenerateToken(new User { Email = "a@b.com" });

        act.Should().Throw<InvalidOperationException>().WithMessage("*Jwt:Key*");
    }

    [Fact]
    public void GenerateToken_ProducesUniqueTokensEachCall()
    {
        var service = new JwtService(TestConfig.Create());
        var user = new User { Email = "a@b.com" };

        var (t1, _) = service.GenerateToken(user);
        var (t2, _) = service.GenerateToken(user);

        t1.Should().NotBe(t2); // unique jti claim
    }
}
