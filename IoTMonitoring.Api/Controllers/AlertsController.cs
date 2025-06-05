// Controllers/AlertsController.cs
using Microsoft.AspNetCore.Mvc;
using IoTMonitoring.Api.DTOs;
using IoTMonitoring.Api.Services.Interfaces;

namespace IoTMonitoring.Api.Controllers
{
    [ApiController]
    [Route("api/alerts")]
    public class AlertsController : ControllerBase
    {
        private readonly IAlertService _alertService;

        public AlertsController(IAlertService alertService)
        {
            _alertService = alertService;
        }

        // 알림 설정 목록 조회
        [HttpGet("settings")]
        public async Task<ActionResult<IEnumerable<AlertSettingDto>>> GetAlertSettings(
            [FromQuery] int? sensorId = null,
            [FromQuery] string alertType = null)
        {
            var settings = await _alertService.GetAlertSettingsAsync(sensorId, alertType);
            return Ok(settings);
        }

        // 알림 설정 상세 조회
        [HttpGet("settings/{id}")]
        public async Task<ActionResult<AlertSettingDetailDto>> GetAlertSetting(int id)
        {
            var setting = await _alertService.GetAlertSettingByIdAsync(id);
            if (setting == null)
                return NotFound();

            return Ok(setting);
        }

        // 알림 설정 생성
        [HttpPost("settings")]
        public async Task<ActionResult<AlertSettingDto>> CreateAlertSetting([FromBody] AlertSettingCreateDto settingDto)
        {
            try
            {
                var createdSetting = await _alertService.CreateAlertSettingAsync(settingDto);
                return CreatedAtAction(nameof(GetAlertSetting), new { id = createdSetting.AlertID }, createdSetting);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // 알림 설정 수정
        [HttpPut("settings/{id}")]
        public async Task<ActionResult> UpdateAlertSetting(int id, [FromBody] AlertSettingUpdateDto settingDto)
        {
            try
            {
                await _alertService.UpdateAlertSettingAsync(id, settingDto);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"Alert setting with ID {id} not found");
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // 알림 설정 삭제
        [HttpDelete("settings/{id}")]
        public async Task<ActionResult> DeleteAlertSetting(int id)
        {
            try
            {
                await _alertService.DeleteAlertSettingAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"Alert setting with ID {id} not found");
            }
        }

        // 알림 이력 조회
        [HttpGet("history")]
        public async Task<ActionResult<IEnumerable<AlertHistoryDto>>> GetAlertHistory(
            [FromQuery] int? sensorId = null,
            [FromQuery] int? alertId = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] bool? acknowledged = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var history = await _alertService.GetAlertHistoryAsync(
                sensorId, alertId, startDate, endDate, acknowledged, page, pageSize);

            return Ok(history);
        }

        // 알림 처리 (확인)
        [HttpPost("history/{id}/acknowledge")]
        public async Task<ActionResult> AcknowledgeAlert(long id)
        {
            try
            {
                await _alertService.AcknowledgeAlertAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"Alert history with ID {id} not found");
            }
        }
    }
}