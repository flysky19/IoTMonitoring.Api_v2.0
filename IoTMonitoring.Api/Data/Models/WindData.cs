namespace IoTMonitoring.Api.Data.Models
{
    public class WindData
    {
        public long DataID { get; set; }
        public int SensorID { get; set; }
        public DateTime Timestamp { get; set; }
        public float? WindSpeed { get; set; }
        public string RawData { get; set; } // JSON 형식의 데이터

        // 탐색 속성
        public Sensor Sensor { get; set; }
    }
}