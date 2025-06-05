// DTOs/SensorDtos.cs
using IoTMonitoring.Api.Data.Models;

namespace IoTMonitoring.Api.DTOs
{
    // 센서 기본 정보 DTO
    public class SensorDto
    {
        public int SensorID { get; set; }
        public string SensorUUID { get; set; }
        public string Name { get; set; }
        public string SensorType { get; set; }
        public string Status { get; set; }
        public string ConnectionStatus { get; set; }
        public DateTime? LastCommunication { get; set; }
        public int? GroupID { get; set; }
        public string GroupName { get; set; }
        public string Location { get; set; }
    }

    // 센서 상세 정보 DTO
    public class SensorDetailDto : SensorDto
    {
        public string Model { get; set; }
        public string FirmwareVersion { get; set; }
        public DateTime? LastHeartbeat { get; set; }
        public int HeartbeatInterval { get; set; }
        public int ConnectionTimeout { get; set; }
        public DateTime? InstallationDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public SensorMqttTopicDto MqttTopics { get; set; }
    }

    // 센서 생성 DTO
    public class SensorCreateDto
    {
        public int? GroupID { get; set; }
        public string SensorType { get; set; }
        public string SensorUUID { get; set; }
        public string Name { get; set; }
        public string Model { get; set; }
        public string FirmwareVersion { get; set; }
        public string Status { get; set; } = "active";
        public int HeartbeatInterval { get; set; } = 60;
        public int ConnectionTimeout { get; set; } = 180;
        public DateTime? InstallationDate { get; set; }
        public SensorMqttTopicCreateDto MqttTopics { get; set; }
    }

    // 센서 업데이트 DTO
    public class SensorUpdateDto
    {
        public int? GroupID { get; set; }
        public string Name { get; set; }
        public string Model { get; set; }
        public string FirmwareVersion { get; set; }
        public int HeartbeatInterval { get; set; }
        public int ConnectionTimeout { get; set; }
        public DateTime? InstallationDate { get; set; }
    }

    public class UpdateSensorGroupDto
    {
        public int? GroupID { get; set; }
    }

    // 센서 데이터 요청 DTO
    public class SensorDataRequestDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int? Limit { get; set; }
        public string AggregationType { get; set; } = "Raw"; // Raw, Hourly, Daily
    }

    // 센서 MQTT 토픽 DTO
    public class SensorMqttTopicDto
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

        // Navigation Property
        public Sensor Sensor { get; set; }
    }

    // 센서 MQTT 토픽 생성 DTO
    public class SensorMqttTopicCreateDto
    {
        public string DataTopic { get; set; }
        public string ControlTopic { get; set; }
        public string StatusTopic { get; set; }
        public string HeartbeatTopic { get; set; }
        public byte QoS { get; set; } = 0;
        public bool Retained { get; set; } = false;
    }

    // 센서 MQTT 토픽 업데이트 DTO
    public class SensorMqttTopicUpdateDto
    {
        public string DataTopic { get; set; }
        public string ControlTopic { get; set; }
        public string StatusTopic { get; set; }
        public string HeartbeatTopic { get; set; }
        public byte QoS { get; set; }
        public bool Retained { get; set; }
    }

    // 센서 상태 DTO (추가)
    public class SensorStatusDto
    {
        public int SensorID { get; set; }
        public string Name { get; set; }
        public string SensorType { get; set; }
        public string Status { get; set; }
        public string ConnectionStatus { get; set; }
        public DateTime? LastCommunication { get; set; }
        public DateTime? LastHeartbeat { get; set; }
        public bool IsOnline { get; set; }
        public int UptimePercentage { get; set; }
        public string StatusMessage { get; set; }
    }
}