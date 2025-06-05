using IoTMonitoring.Api.DTOs;
using System.Threading.Tasks;

namespace IoTMonitoring.Api.Services.Interfaces
{
    public interface IAuthService2222
    {
        // 인증 관련 작업
        Task<AuthResultDto> LoginAsync(string username, string password);
        Task<AuthResultDto> RefreshTokenAsync(string refreshToken);
        Task RevokeTokenAsync(string refreshToken);

        // 비밀번호 관련 작업
        Task ChangePasswordAsync(string username, string currentPassword, string newPassword);
        Task RequestPasswordResetAsync(string email);
        Task ResetPasswordAsync(string token, string email, string newPassword);

        // 토큰 검증
        Task<bool> ValidateTokenAsync(string token);
        Task<UserDto> GetUserFromTokenAsync(string token);
    }
}