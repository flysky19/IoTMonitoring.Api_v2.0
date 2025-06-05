namespace IoTMonitoring.Api.Data.Models
{
    public class DailyStatistic
    {
        public long StatID { get; set; }
        public int SensorID { get; set; }
        public DateTime DateDay { get; set; }
        public float? MinValue { get; set; }
        public float? MaxValue { get; set; }
        public float? AvgValue { get; set; }
        public float? StdDev { get; set; }
        public int? MeasurementCount { get; set; }
        public string DataType { get; set; }
        public DateTime CreatedAt { get; set; }

        // 탐색 속성
        public Sensor Sensor { get; set; }
    }
}