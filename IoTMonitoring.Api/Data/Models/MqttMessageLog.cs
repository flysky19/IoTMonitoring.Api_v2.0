namespace IoTMonitoring.Api.Data.Models
{
    public class MqttMessageLog
    {
        public long LogID { get; set; }
        public string Topic { get; set; }
        public string Message { get; set; }
        public string Direction { get; set; }
        public DateTime Timestamp { get; set; }
        public bool Processed { get; set; }
        public string ProcessingResult { get; set; }
    }
}
