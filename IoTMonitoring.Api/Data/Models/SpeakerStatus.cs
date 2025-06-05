namespace IoTMonitoring.Api.Data.Models
{
    public class SpeakerStatus
    {
        public long StatusID { get; set; }
        public int SensorID { get; set; }
        public DateTime Timestamp { get; set; }
        public bool PowerStatus { get; set; }
        public byte? Volume { get; set; }
        public float? Frequency { get; set; }
        public int? LastUpdatedBy { get; set; }

        // 탐색 속성
        public Sensor Sensor { get; set; }
        public User LastUpdatedByUser { get; set; }
    }
}