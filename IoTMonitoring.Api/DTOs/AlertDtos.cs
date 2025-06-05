// DTOs/AlertDtos.cs
namespace IoTMonitoring.Api.DTOs
{
    // 알림 설정 기본 정보 DTO
    public class AlertSettingDto
    {
        public int AlertID { get; set; }
        public int? SensorID { get; set; }
        public string SensorName { get; set; }
        public string SensorType { get; set; }
        public string AlertType { get; set; }
        public string Parameter { get; set; }
        public string AlertCondition { get; set; }
        public float? ThresholdValue { get; set; }
        public bool Enabled { get; set; }
        public string NotificationMethod { get; set; }
    }

    // 알림 설정 상세 정보 DTO
    public class AlertSettingDetailDto : AlertSettingDto
    {
        public string NotificationRecipient { get; set; }
        public string CreatedByUser { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    // 알림 설정 생성 DTO
    public class AlertSettingCreateDto
    {
        public int? SensorID { get; set; }
        public string AlertType { get; set; }
        public string Parameter { get; set; }
        public string AlertCondition { get; set; }
        public float? ThresholdValue { get; set; }
        public bool Enabled { get; set; } = true;
        public string NotificationMethod { get; set; }
        public string NotificationRecipient { get; set; }
    }

    // 알림 설정 업데이트 DTO
    public class AlertSettingUpdateDto
    {
        public string Parameter { get; set; }
        public string AlertCondition { get; set; }
        public float? ThresholdValue { get; set; }
        public bool Enabled { get; set; }
        public string NotificationMethod { get; set; }
        public string NotificationRecipient { get; set; }
    }

    // 알림 이력 DTO
    public class AlertHistoryDto
    {
        public long HistoryID { get; set; }
        public int? AlertID { get; set; }
        public string AlertType { get; set; }
        public int? SensorID { get; set; }
        public string SensorName { get; set; }
        public DateTime TriggeredAt { get; set; }
        public string AlertMessage { get; set; }
        public float? DataValue { get; set; }
        public bool NotificationSent { get; set; }
        public DateTime? NotificationSentAt { get; set; }
        public bool Acknowledged { get; set; }
        public DateTime? AcknowledgedAt { get; set; }
        public string AcknowledgedBy { get; set; }
    }

    // 최근 알림 DTO (대시보드용)
    public class RecentAlertDto
    {
        public long HistoryID { get; set; }
        public string AlertType { get; set; }
        public string SensorName { get; set; }
        public string GroupName { get; set; }
        public DateTime TriggeredAt { get; set; }
        public string AlertMessage { get; set; }
        public bool Acknowledged { get; set; }
        public string Severity { get; set; } // High, Medium, Low (derived from alert type)
    }
}