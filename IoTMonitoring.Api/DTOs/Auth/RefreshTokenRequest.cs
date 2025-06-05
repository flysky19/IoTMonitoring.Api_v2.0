using System.ComponentModel.DataAnnotations;

namespace IoTMonitoring.Api.DTOs.Auth
{
    public class RefreshTokenRequest
    {
        [Required]
        public string RefreshToken { get; set; }
    }
}