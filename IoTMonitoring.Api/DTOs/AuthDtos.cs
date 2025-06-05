// DTOs/AuthDtos.cs
namespace IoTMonitoring.Api.DTOs
{
    // 로그인 요청 DTO
    public class LoginDto
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    // 인증 결과 DTO
    public class AuthResultDto
    {
        public bool Success { get; set; }
        public string Token { get; set; }
        public string RefreshToken { get; set; }
        public DateTime TokenExpiration { get; set; }
        public UserDto User { get; set; }
    }

    // 토큰 갱신 요청 DTO
    public class RefreshTokenDto
    {
        public string RefreshToken { get; set; }
    }

    // 비밀번호 변경 DTO
    public class ChangePasswordDto
    {
        public string Username { get; set; }
        public string CurrentPassword { get; set; }
        public string NewPassword { get; set; }
    }

    // 비밀번호 재설정 요청 DTO
    public class ForgotPasswordDto
    {
        public string Email { get; set; }
    }

    // 비밀번호 재설정 DTO
    public class ResetPasswordDto
    {
        public string Token { get; set; }
        public string Email { get; set; }
        public string NewPassword { get; set; }
    }
}