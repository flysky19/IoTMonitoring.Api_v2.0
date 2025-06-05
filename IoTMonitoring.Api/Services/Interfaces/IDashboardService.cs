using IoTMonitoring.Api.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IoTMonitoring.Api.Services.Interfaces
{
    public interface IDashboardService
    {
        // 대시보드 요약 데이터
        Task<SensorStatusSummaryDto> GetSensorStatusSummaryAsync(int? companyId = null);
        Task<IEnumerable<RecentAlertDto>> GetRecentAlertsAsync(int? companyId = null, int count = 10);
        Task<IEnumerable<SensorChartDataDto>> GetSensorChartDataAsync(IEnumerable<int> sensorIds, string chartType, string timeRange = "24h");
        Task<IEnumerable<SensorUptimeStatDto>> GetSensorUptimeStatsAsync(int? groupId = null, int days = 7);

        // 대시보드 위젯 데이터
        Task<Dictionary<string, object>> GetDashboardWidgetDataAsync(string widgetType, Dictionary<string, object> parameters);

        // 사용자별 대시보드 설정 관리
        Task<Dictionary<string, object>> GetUserDashboardSettingsAsync(int userId);
        Task UpdateUserDashboardSettingsAsync(int userId, Dictionary<string, object> settings);
    }
}