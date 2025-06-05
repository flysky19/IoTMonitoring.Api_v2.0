// Controllers/SystemController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using IoTMonitoring.Api.DTOs;
using IoTMonitoring.Api.Services.Interfaces;

namespace IoTMonitoring.Api.Controllers
{
    [ApiController]
    [Route("api/system")]
    [Authorize(Roles = "Admin")] // 관리자만 시스템 설정 접근 가능
    public class SystemController : ControllerBase
    {
        private readonly ISystemService _systemService;

        public SystemController(ISystemService systemService)
        {
            _systemService = systemService;
        }

        // 시스템 설정 조회
        [HttpGet("settings")]
        public async Task<ActionResult<IEnumerable<SystemSettingDto>>> GetSystemSettings([FromQuery] string key = null)
        {
            var settings = await _systemService.GetSystemSettingsAsync(key);
            return Ok(settings);
        }

        // 시스템 설정 업데이트
        [HttpPut("settings/{key}")]
        public async Task<ActionResult> UpdateSystemSetting(string key, [FromBody] SystemSettingUpdateDto settingDto)
        {
            try
            {
                await _systemService.UpdateSystemSettingAsync(key, settingDto);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"Setting with key '{key}' not found");
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // 데이터 정리 이력 조회
        [HttpGet("data-purge-history")]
        public async Task<ActionResult<IEnumerable<DataPurgeHistoryDto>>> GetDataPurgeHistory(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            var history = await _systemService.GetDataPurgeHistoryAsync(startDate, endDate);
            return Ok(history);
        }

        // 수동 데이터 정리 실행
        [HttpPost("purge-data")]
        public async Task<ActionResult> PurgeData([FromBody] ManualDataPurgeDto purgeDto)
        {
            try
            {
                var result = await _systemService.PurgeDataAsync(purgeDto.RetentionDays, purgeDto.TableNames);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        // 시스템 상태 정보 조회
        [HttpGet("status")]
        public async Task<ActionResult<SystemStatusDto>> GetSystemStatus()
        {
            var status = await _systemService.GetSystemStatusAsync();
            return Ok(status);
        }

        // 데이터베이스 통계 조회
        [HttpGet("database-stats")]
        public async Task<ActionResult<DatabaseStatsDto>> GetDatabaseStats()
        {
            var stats = await _systemService.GetDatabaseStatsAsync();
            return Ok(stats);
        }
    }
}