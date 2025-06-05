using System;

namespace IoTMonitoring.Api.DTOs.Auth
{
    public class LoginResponse
    {
        public string Token { get; set; }
        public string RefreshToken { get; set; }
        public DateTime Expiration { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
        public DateTime LastLogin { get; set; }
        public string Role { get; set; }
    }
}