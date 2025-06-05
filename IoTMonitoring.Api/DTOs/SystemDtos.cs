// DTOs/SystemDtos.cs
namespace IoTMonitoring.Api.DTOs
{
    // 시스템 설정 DTO
    public class SystemSettingDto
    {
        public int SettingID { get; set; }
        public string SettingKey { get; set; }
        public string SettingValue { get; set; }
        public string DataType { get; set; }
        public string Description { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string UpdatedBy { get; set; }
    }

    // 시스템 설정 업데이트 DTO
    public class SystemSettingUpdateDto
    {
        public string SettingValue { get; set; }
    }

    // 데이터 정리 이력 DTO
    public class DataPurgeHistoryDto
    {
        public long PurgeID { get; set; }
        public DateTime ExecutionTime { get; set; }
        public string TableName { get; set; }
        public int RowsDeleted { get; set; }
        public int RetentionDays { get; set; }
        public DateTime CutoffDate { get; set; }
    }

    // 수동 데이터 정리 요청 DTO
    public class ManualDataPurgeDto
    {
        public int RetentionDays { get; set; }
        public List<string> TableNames { get; set; }
    }

    // 시스템 상태 DTO
    public class SystemStatusDto
    {
        public bool MqttServiceRunning { get; set; }
        public bool DatabaseConnected { get; set; }
        public int ActiveSensorCount { get; set; }
        public int OnlineSensorCount { get; set; }
        public int PendingAlertCount { get; set; }
        public DateTime ServerTime { get; set; }
        public string ServerTimeZone { get; set; }
        public TimeSpan Uptime { get; set; }
        public Dictionary<string, string> VersionInfo { get; set; }
    }

    // 데이터베이스 통계 DTO
    public class DatabaseStatsDto
    {
        public long TotalSensorDataCount { get; set; }
        public Dictionary<string, long> DataCountByTable { get; set; }
        public Dictionary<string, DateTime> OldestDataByTable { get; set; }
        public Dictionary<string, long> TableSizeInBytes { get; set; }
        public long TotalDatabaseSizeInBytes { get; set; }
        public int DataRetentionDays { get; set; }
        public DateTime LastPurgeDate { get; set; }
    }
}