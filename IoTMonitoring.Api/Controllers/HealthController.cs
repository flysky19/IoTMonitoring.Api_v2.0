using IoTMonitoring.Api.Data.Connection;
using IoTMonitoring.Api.Services.MQTT.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace IoTMonitoring.Api.Controllers
{
    // 4. 건강 상태 체크 엔드포인트
    [ApiController]
    [Route("api/health")]
    public class HealthController : ControllerBase
    {
        private readonly IDbConnectionFactory _dbFactory;
        private readonly IMqttService _mqttService;

        public HealthController(IDbConnectionFactory dbFactory, IMqttService mqttService)
        {
            _dbFactory = dbFactory;
            _mqttService = mqttService;
        }

        [HttpGet]
        public async Task<ActionResult> GetHealth()
        {
            var health = new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                services = new
                {
                    database = await CheckDatabaseHealth(),
                    mqtt = CheckMqttHealth(),
                    api = "healthy"
                }
            };

            return Ok(health);
        }

        private async Task<string> CheckDatabaseHealth()
        {
            try
            {
                using var connection = await _dbFactory.CreateConnectionAsync();
                return "healthy";
            }
            catch
            {
                return "unhealthy";
            }
        }

        private string CheckMqttHealth()
        {
            return _mqttService.IsConnected ? "healthy" : "unhealthy";
        }
    }
}
