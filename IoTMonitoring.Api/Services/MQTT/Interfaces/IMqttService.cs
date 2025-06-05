// Services/MQTT/Interfaces/IMqttService.cs
using System;
using System.Threading.Tasks;

namespace IoTMonitoring.Api.Services.MQTT.Interfaces
{
    public interface IMqttService
    {
        Task StartAsync();
        Task StopAsync();
        Task SubscribeToSensorTopicsAsync();
        Task PublishAsync(string topic, string message, int qos = 0, bool retain = false);
        bool IsConnected { get; }
        event EventHandler<SensorDataReceivedEventArgs> OnSensorDataReceived;
    }

    public class SensorDataReceivedEventArgs : EventArgs
    {
        public string SensorUuid { get; set; }
        public string MessageType { get; set; }
        public string Payload { get; set; }
        public DateTime Timestamp { get; set; }
    }
}