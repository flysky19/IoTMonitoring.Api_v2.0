namespace IoTMonitoring.Api.Data.Models
{
    public class Sensor
    {
        public int SensorID { get; set; }
        public int? GroupID { get; set; }
        public string SensorType { get; set; }
        public string SensorUUID { get; set; }
        public string Name { get; set; }
        public string Model { get; set; }
        public string FirmwareVersion { get; set; }
        public string Status { get; set; }
        public string ConnectionStatus { get; set; }
        public DateTime? LastCommunication { get; set; }
        public DateTime? LastHeartbeat { get; set; }
        public int HeartbeatInterval { get; set; }
        public int ConnectionTimeout { get; set; }
        public DateTime? InstallationDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // 탐색 속성
        public SensorGroup SensorGroup { get; set; }
    }
}