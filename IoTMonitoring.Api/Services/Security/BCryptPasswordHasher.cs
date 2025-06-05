using IoTMonitoring.Api.Services.Security.Interfaces;

namespace IoTMonitoring.Api.Services.Security
{
    public class BCryptPasswordHasher : IPasswordHasher
    {
        public string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        public bool VerifyPassword(string password, string hashedPassword)
        {
            var result = BCrypt.Net.BCrypt.Verify(password, hashedPassword);
            Console.WriteLine($"검증 요청: 비밀번호='{password}', 해시='{hashedPassword}', 결과={result}");
            return result;
        }
    }
}