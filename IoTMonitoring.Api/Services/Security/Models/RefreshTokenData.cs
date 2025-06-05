using System;

namespace IoTMonitoring.Api.Services.Security.Models
{
    public class RefreshTokenData
    {
        public int UserId { get; set; }
        public DateTime ExpiryDate { get; set; }
    }
}