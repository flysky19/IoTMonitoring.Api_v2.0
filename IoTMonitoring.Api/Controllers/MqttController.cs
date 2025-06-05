// Controllers/MqttController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using IoTMonitoring.Api.DTOs;
using IoTMonitoring.Api.Services.Interfaces;

namespace IoTMonitoring.Api.Controllers
{
    [ApiController]
    [Route("api/mqtt")]
    [Authorize(Roles = "Admin")] // 관리자만 MQTT 설정 접근 가능
    public class MqttController : ControllerBase
    {
        private readonly IMqttManagementService _mqttService;

        public MqttController(IMqttManagementService mqttService)
        {
            _mqttService = mqttService;
        }

        // MQTT 설정 조회
        [HttpGet("settings")]
        public async Task<ActionResult<MqttSettingsDto>> GetMqttSettings()
        {
            var settings = await _mqttService.GetMqttSettingsAsync();
            return Ok(settings);
        }

        // MQTT 설정 업데이트
        [HttpPut("settings")]
        public async Task<ActionResult> UpdateMqttSettings([FromBody] MqttSettingsUpdateDto settingsDto)
        {
            try
            {
                await _mqttService.UpdateMqttSettingsAsync(settingsDto);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        // MQTT 연결 상태 확인
        [HttpGet("status")]
        public async Task<ActionResult<MqttConnectionStatusDto>> GetConnectionStatus()
        {
            var status = await _mqttService.GetConnectionStatusAsync();
            return Ok(status);
        }

        // MQTT 서비스 다시 시작
        [HttpPost("restart")]
        public async Task<ActionResult> RestartMqttService()
        {
            try
            {
                await _mqttService.RestartMqttServiceAsync();
                return Ok(new { Message = "MQTT service restarted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        // MQTT 메시지 로그 조회
        [HttpGet("logs")]
        public async Task<ActionResult<IEnumerable<MqttMessageLogDto>>> GetMqttLogs(
            [FromQuery] string topic = null,
            [FromQuery] string direction = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var logs = await _mqttService.GetMqttLogsAsync(topic, direction, startDate, endDate, page, pageSize);
            return Ok(logs);
        }

        // 토픽 발행 테스트
        [HttpPost("publish-test")]
        public async Task<ActionResult> PublishTestMessage([FromBody] MqttPublishTestDto publishDto)
        {
            try
            {
                await _mqttService.PublishTestMessageAsync(publishDto.Topic, publishDto.Payload, publishDto.QoS, publishDto.Retain);
                return Ok(new { Message = "Test message published successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }
    }
}