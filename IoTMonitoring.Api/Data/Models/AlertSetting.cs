namespace IoTMonitoring.Api.Data.Models
{
    public class AlertSetting
    {
        public int AlertID { get; set; }
        public int? SensorID { get; set; }
        public string AlertType { get; set; }
        public string Parameter { get; set; }
        public string AlertCondition { get; set; }
        public float? ThresholdValue { get; set; }
        public bool Enabled { get; set; }
        public string NotificationMethod { get; set; }
        public string NotificationRecipient { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? CreatedBy { get; set; }

        // 탐색 속성
        public Sensor Sensor { get; set; }
        public User CreatedByUser { get; set; }
        public ICollection<AlertHistory> AlertHistories { get; set; }
    }
}