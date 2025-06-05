namespace IoTMonitoring.Api.Data.Models
{
    public class SensorConnectionHistory
    {
        public long HistoryID { get; set; }
        public int SensorID { get; set; }
        public DateTime StatusChangeTime { get; set; }
        public string OldStatus { get; set; }
        public string NewStatus { get; set; }
        public string ChangeReason { get; set; }

        // 탐색 속성
        public Sensor Sensor { get; set; }
    }
}