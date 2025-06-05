// DTOs/SpeakerDtos.cs
namespace IoTMonitoring.Api.DTOs
{
    // 스피커 상태 DTO
    public class SpeakerStatusDto
    {
        public int SensorID { get; set; }
        public string SensorName { get; set; }
        public DateTime Timestamp { get; set; }
        public bool PowerStatus { get; set; }
        public int? Volume { get; set; }
        public float? Frequency { get; set; }
        public string LastUpdatedByUser { get; set; }
        public DateTime? LastUpdatedAt { get; set; }
    }

    // 스피커 제어 요청 DTO
    public class SpeakerControlDto
    {
        public bool? PowerStatus { get; set; }
        public int? Volume { get; set; }
        public float? Frequency { get; set; }
        public string UpdateReason { get; set; }
    }

    // 스피커 제어 결과 DTO
    public class SpeakerControlResultDto
    {
        public int SensorID { get; set; }
        public bool CommandSent { get; set; }
        public DateTime CommandTime { get; set; }
        public SpeakerControlDto ControlSettings { get; set; }
        public string Message { get; set; }
    }

    // 스피커 제어 이력 DTO
    public class SpeakerControlHistoryDto
    {
        public long HistoryID { get; set; }
        public int SensorID { get; set; }
        public string SensorName { get; set; }
        public DateTime Timestamp { get; set; }
        public bool? PowerStatus { get; set; }
        public int? Volume { get; set; }
        public float? Frequency { get; set; }
        public string UpdatedByUser { get; set; }
        public string UpdateReason { get; set; }
    }
}