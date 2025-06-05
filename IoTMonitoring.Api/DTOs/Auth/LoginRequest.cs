using System.ComponentModel.DataAnnotations;

namespace IoTMonitoring.Api.DTOs.Auth
{
    public class LoginRequest
    {
        [Required]
        public string Username { get; set; }

        [Required]
        public string Password { get; set; }
    }
}