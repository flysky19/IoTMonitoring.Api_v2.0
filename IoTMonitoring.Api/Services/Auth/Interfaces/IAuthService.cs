using System.Threading.Tasks;
using IoTMonitoring.Api.DTOs.Auth;

namespace IoTMonitoring.Api.Services.Auth.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResponse> LoginAsync(LoginRequest request);
        Task<LoginResponse> RefreshTokenAsync(string refreshToken);
        Task<bool> ValidateTokenAsync(string token);

        bool IsAdmin(string username);
    }
}