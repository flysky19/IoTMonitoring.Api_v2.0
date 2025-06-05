namespace IoTMonitoring.Api.Data.Models
{
    public class MqttSetting
    {
        public int SettingID { get; set; }
        public string BrokerAddress { get; set; }
        public int BrokerPort { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string ClientID { get; set; }
        public bool UseSSL { get; set; }
        public string CACertificate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? LastConnectedAt { get; set; }
        public string ConnectionStatus { get; set; }
    }
}