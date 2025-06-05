// DTOs/DashboardDtos.cs
namespace IoTMonitoring.Api.DTOs
{
    // 센서 상태 요약 DTO
    public class SensorStatusSummaryDto
    {
        public int TotalSensors { get; set; }
        public int OnlineSensors { get; set; }
        public int OfflineSensors { get; set; }
        public int MaintenanceSensors { get; set; }
        public Dictionary<string, int> SensorsByType { get; set; }
        public Dictionary<string, int> OnlineSensorsByType { get; set; }
        public float OverallUptimePercentage { get; set; }
    }

    // 센서 차트 데이터 DTO
    public class SensorChartDataDto
    {
        public int SensorID { get; set; }
        public string SensorName { get; set; }
        public string SensorType { get; set; }
        public string ChartType { get; set; }
        public string TimeRange { get; set; }
        public List<ChartDataPointDto> DataPoints { get; set; }
    }

    // 차트 데이터 포인트 DTO
    public class ChartDataPointDto
    {
        public DateTime Timestamp { get; set; }
        public Dictionary<string, float?> Values { get; set; }
    }

    // 센서 가동 시간 통계 DTO
    public class SensorUptimeStatDto
    {
        public int SensorID { get; set; }
        public string SensorName { get; set; }
        public string SensorType { get; set; }
        public string GroupName { get; set; }
        public DateTime DateDay { get; set; }
        public float UptimePercentage { get; set; }
        public int TotalOnlineSeconds { get; set; }
        public int TotalOfflineSeconds { get; set; }
        public int ConnectionCount { get; set; }
    }
}