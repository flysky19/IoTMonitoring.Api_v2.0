// Controllers/DashboardController.cs
using Microsoft.AspNetCore.Mvc;
using IoTMonitoring.Api.DTOs;
using IoTMonitoring.Api.Services.Interfaces;

namespace IoTMonitoring.Api.Controllers
{
    [ApiController]
    [Route("api/dashboard")]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        // 센서 상태 요약
        [HttpGet("sensor-status-summary")]
        public async Task<ActionResult<SensorStatusSummaryDto>> GetSensorStatusSummary([FromQuery] int? companyId = null)
        {
            var summary = await _dashboardService.GetSensorStatusSummaryAsync(companyId);
            return Ok(summary);
        }

        // 최근 알림 목록
        [HttpGet("recent-alerts")]
        public async Task<ActionResult<IEnumerable<RecentAlertDto>>> GetRecentAlerts(
            [FromQuery] int? companyId = null,
            [FromQuery] int count = 10)
        {
            var alerts = await _dashboardService.GetRecentAlertsAsync(companyId, count);
            return Ok(alerts);
        }

        // 센서 데이터 차트 정보
        [HttpGet("sensor-charts")]
        public async Task<ActionResult<IEnumerable<SensorChartDataDto>>> GetSensorChartData(
            [FromQuery] int[] sensorIds,
            [FromQuery] string chartType,
            [FromQuery] string timeRange = "24h")
        {
            try
            {
                var chartData = await _dashboardService.GetSensorChartDataAsync(sensorIds, chartType, timeRange);
                return Ok(chartData);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // 센서 가동 시간 통계
        [HttpGet("uptime-stats")]
        public async Task<ActionResult<IEnumerable<SensorUptimeStatDto>>> GetUptimeStats(
            [FromQuery] int? groupId = null,
            [FromQuery] int days = 7)
        {
            var stats = await _dashboardService.GetSensorUptimeStatsAsync(groupId, days);
            return Ok(stats);
        }
    }
}