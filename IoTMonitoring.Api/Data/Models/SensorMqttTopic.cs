namespace IoTMonitoring.Api.Data.Models
{
    public class SensorMqttTopic
    {
        public int TopicID { get; set; }
        public int SensorID { get; set; }
        public string DataTopic { get; set; }
        public string ControlTopic { get; set; }
        public string StatusTopic { get; set; }
        public string HeartbeatTopic { get; set; }
        public byte QoS { get; set; }
        public bool Retained { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // 탐색 속성
        public Sensor Sensor { get; set; }
    }
}