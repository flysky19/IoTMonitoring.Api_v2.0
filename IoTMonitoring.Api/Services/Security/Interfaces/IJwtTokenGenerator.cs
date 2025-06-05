using System.Security.Claims;
using IoTMonitoring.Api.Data.Models;
using IoTMonitoring.Api.Services.Security.Models;

namespace IoTMonitoring.Api.Services.Security.Interfaces
{
    public interface IJwtTokenGenerator
    {
        TokenResult GenerateTokens(User user);
        ClaimsPrincipal ValidateToken(string token);
        bool TryParseRefreshToken(string refreshToken, out RefreshTokenData refreshTokenData);
    }
}