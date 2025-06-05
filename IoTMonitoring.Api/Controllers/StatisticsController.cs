// Controllers/StatisticsController.cs
using Microsoft.AspNetCore.Mvc;
using IoTMonitoring.Api.DTOs;
using IoTMonitoring.Api.Services.Interfaces;

namespace IoTMonitoring.Api.Controllers
{
    [ApiController]
    [Route("api/statistics")]
    public class StatisticsController : ControllerBase
    {
        private readonly IStatisticsService _statisticsService;

        public StatisticsController(IStatisticsService statisticsService)
        {
            _statisticsService = statisticsService;
        }

        // 일별 데이터 통계
        [HttpGet("daily")]
        public async Task<ActionResult<IEnumerable<DailyStatisticDto>>> GetDailyStatistics(
            [FromQuery] int sensorId,
            [FromQuery] string dataType,
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            try
            {
                var statistics = await _statisticsService.GetDailyStatisticsAsync(sensorId, dataType, startDate, endDate);
                return Ok(statistics);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // 센서 가동 시간 통계
        [HttpGet("uptime")]
        public async Task<ActionResult<IEnumerable<UptimeStatisticDto>>> GetUptimeStatistics(
            [FromQuery] int? sensorId = null,
            [FromQuery] int? groupId = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            if (!sensorId.HasValue && !groupId.HasValue)
                return BadRequest("Either sensorId or groupId must be provided");

            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;

            var statistics = await _statisticsService.GetUptimeStatisticsAsync(sensorId, groupId, start, end);
            return Ok(statistics);
        }

        // 데이터 집계 (시간별, 일별, 월별)
        [HttpGet("aggregated")]
        public async Task<ActionResult<IEnumerable<AggregatedDataDto>>> GetAggregatedData(
            [FromQuery] int sensorId,
            [FromQuery] string aggregationType, // hourly, daily, monthly
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            try
            {
                var data = await _statisticsService.GetAggregatedDataAsync(sensorId, aggregationType, startDate, endDate);
                return Ok(data);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        // 센서 타입별 통계 요약
        [HttpGet("summary-by-type")]
        public async Task<ActionResult<IEnumerable<SensorTypeSummaryDto>>> GetSensorTypeSummary(
            [FromQuery] int? companyId = null,
            [FromQuery] DateTime? date = null)
        {
            var targetDate = date ?? DateTime.UtcNow.Date;
            var summary = await _statisticsService.GetSensorTypeSummaryAsync(companyId, targetDate);
            return Ok(summary);
        }
    }
}