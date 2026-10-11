using AntiTheftApp.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace AntiTheftApp.Api.Tests.Helpers;

public static class TestDb
{
    /// <summary>Fresh, isolated in-memory database for every test.</summary>
    public static AppDbContext Create()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }
}
