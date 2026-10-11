using AntiTheftApp.Api.Data;
using AntiTheftApp.Api.Services;
using AntiTheftApp.Api.Tests.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace AntiTheftApp.Api.Tests.Integration;

/// <summary>
/// Boots the real API pipeline (routing, validation, JWT auth) in memory.
/// Swaps PostgreSQL for an in-memory DB and Firebase for a mock, so tests
/// need no Docker, no database server and no real credentials.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    public Mock<IFirebaseService> Firebase { get; } = new();
    private readonly string _dbName = Guid.NewGuid().ToString();

    public ApiFactory()
    {
        // Program.cs reads these while building, so they must be set before the host starts.
        Environment.SetEnvironmentVariable("Jwt__Key", TestConfig.JwtKey);
        Environment.SetEnvironmentVariable("Jwt__Issuer", TestConfig.Issuer);
        Environment.SetEnvironmentVariable("Jwt__Audience", TestConfig.Audience);
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", "Host=unused;Database=unused");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<AppDbContext>();
            services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(_dbName));

            services.RemoveAll<IFirebaseService>();
            services.AddSingleton(Firebase.Object);
        });
    }
}
