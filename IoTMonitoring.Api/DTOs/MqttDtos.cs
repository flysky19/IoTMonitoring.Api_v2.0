// DTOs/MqttDtos.cs
namespace IoTMonitoring.Api.DTOs
{
    // MQTT 설정 DTO
    public class MqttSettingsDto
    {
        public int SettingID { get; set; }
        public string BrokerAddress { get; set; }
        public int BrokerPort { get; set; }
        public string Username { get; set; }
        public bool HasPassword { get; set; } // 보안을 위해 실제 비밀번호 대신 존재 여부만 반환
        public string ClientID { get; set; }
        public bool UseSSL { get; set; }
        public bool HasCACertificate { get; set; }
        public DateTime? LastConnectedAt { get; set; }
        public string ConnectionStatus { get; set; }
    }

    // MQTT 설정 업데이트 DTO
    public class MqttSettingsUpdateDto
    {
        public string BrokerAddress { get; set; }
        public int BrokerPort { get; set; }
        public string Username { get; set; }
        public string Password { get; set; } // 변경할 경우에만 입력
        public string ClientID { get; set; }
        public bool UseSSL { get; set; }
        public string CACertificate { get; set; } // 변경할 경우에만 입력
    }

    // MQTT 연결 상태 DTO
    public class MqttConnectionStatusDto
    {
        public bool IsConnected { get; set; }
        public string Status { get; set; }
        public DateTime? LastConnectedAt { get; set; }
        public string BrokerAddress { get; set; }
        public int BrokerPort { get; set; }
        public string ClientID { get; set; }
        public int ConnectedSensorCount { get; set; }
        public List<string> SubscribedTopics { get; set; }
        public string ErrorMessage { get; set; }
    }

    // MQTT 메시지 로그 DTO
    public class MqttMessageLogDto
    {
        public long LogID { get; set; }
        public string Topic { get; set; }
        public string Message { get; set; }
        public string Direction { get; set; }
        public DateTime Timestamp { get; set; }
        public bool Processed { get; set; }
        public string ProcessingResult { get; set; }
    }

    // MQTT 발행 테스트 DTO
    public class MqttPublishTestDto
    {
        public string Topic { get; set; }
        public string Payload { get; set; }
        public byte QoS { get; set; } = 0;
        public bool Retain { get; set; } = false;
    }
}