// DTOs/StatisticsDtos.cs
namespace IoTMonitoring.Api.DTOs
{
    // 일별 통계 DTO
    public class DailyStatisticDto
    {
        public int SensorID { get; set; }
        public string SensorName { get; set; }
        public DateTime DateDay { get; set; }
        public float? MinValue { get; set; }
        public float? MaxValue { get; set; }
        public float? AvgValue { get; set; }
        public float? StdDev { get; set; }
        public int? MeasurementCount { get; set; }
        public string DataType { get; set; }
    }

    // 가동 시간 통계 DTO
    public class UptimeStatisticDto
    {
        public int SensorID { get; set; }
        public string SensorName { get; set; }
        public DateTime DateDay { get; set; }
        public float UptimePercentage { get; set; }
        public int TotalOnlineSeconds { get; set; }
        public int TotalOfflineSeconds { get; set; }
        public int ConnectionCount { get; set; }
        public int DisconnectionCount { get; set; }
        public int? AvgOnlineDuration { get; set; }
        public int? AvgOfflineDuration { get; set; }
    }

    // 센서 타입별 요약 통계 DTO
    public class SensorTypeSummaryDto
    {
        public string SensorType { get; set; }
        public int TotalSensors { get; set; }
        public int OnlineSensors { get; set; }
        public float AverageUptimePercentage { get; set; }
        public Dictionary<string, float?> AverageValues { get; set; }
        public Dictionary<string, float?> LatestValues { get; set; }
    }
}