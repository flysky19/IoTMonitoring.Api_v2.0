namespace IoTMonitoring.Api.Data.Models
{
    public class SensorUptimeStat
    {
        public long StatID { get; set; }
        public int SensorID { get; set; }
        public DateTime DateDay { get; set; }
        public int TotalOnlineSeconds { get; set; }
        public int TotalOfflineSeconds { get; set; }
        public float? UptimePercentage { get; set; }
        public int ConnectionCount { get; set; }
        public int DisconnectionCount { get; set; }
        public int? AvgOnlineDuration { get; set; }
        public int? AvgOfflineDuration { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // 탐색 속성
        public Sensor Sensor { get; set; }
    }
}