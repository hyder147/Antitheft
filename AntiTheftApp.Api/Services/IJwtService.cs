using AntiTheftApp.Api.Models;

namespace AntiTheftApp.Api.Services;

public interface IJwtService
{
    (string token, DateTime expiresAt) GenerateToken(User user);
}
