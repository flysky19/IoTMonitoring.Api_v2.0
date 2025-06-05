using IoTMonitoring.Api.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IoTMonitoring.Api.Services.Interfaces
{
    public interface IStatisticsService
    {
        // 통계 데이터 조회
        Task<IEnumerable<DailyStatisticDto>> GetDailyStatisticsAsync(int sensorId, string dataType, DateTime startDate, DateTime endDate);
        Task<IEnumerable<UptimeStatisticDto>> GetUptimeStatisticsAsync(int? sensorId = null, int? groupId = null, DateTime? startDate = null, DateTime? endDate = null);
        Task<IEnumerable<AggregatedDataDto>> GetAggregatedDataAsync(int sensorId, string aggregationType, DateTime startDate, DateTime endDate);
        Task<IEnumerable<SensorTypeSummaryDto>> GetSensorTypeSummaryAsync(int? companyId = null, DateTime? date = null);

        // 통계 데이터 계산 및 저장
        Task CalculateAndStoreDailyStatisticsAsync(DateTime date);
        Task CalculateAndStoreUptimeStatisticsAsync(DateTime date);

        // 보고서 생성
        Task<byte[]> GenerateStatisticsReportAsync(int? companyId, int? groupId, DateTime startDate, DateTime endDate, string reportType);
    }
}