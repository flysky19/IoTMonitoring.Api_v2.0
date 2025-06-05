using System.ComponentModel.DataAnnotations;

namespace IoTMonitoring.Api.Data.Models
{
    public class AlertHistory
    {
        [Key]
        public long HistoryID { get; set; }
        public int? AlertID { get; set; }
        public int? SensorID { get; set; }
        public DateTime TriggeredAt { get; set; }
        public string AlertMessage { get; set; }
        public float? DataValue { get; set; }
        public bool NotificationSent { get; set; }
        public DateTime? NotificationSentAt { get; set; }
        public bool Acknowledged { get; set; }
        public DateTime? AcknowledgedAt { get; set; }
        public int? AcknowledgedBy { get; set; }

        // 탐색 속성
        public AlertSetting AlertSetting { get; set; }
        public Sensor Sensor { get; set; }
        public User AcknowledgedByUser { get; set; }
    }
}