using System;
using System.Threading.Tasks;

namespace IoTMonitoring.Api.Services.Interfaces
{
    // MQTT 클라이언트 서비스 (백그라운드 서비스용)
    public interface IMqttClientService
    {
        // MQTT 연결 관리
        Task StartAsync();
        Task StopAsync();
        Task RestartAsync();
        bool IsConnected { get; }

        // 메시지 발행
        Task PublishAsync(string topic, string payload, byte qos = 0, bool retain = false);

        // 토픽 구독
        Task SubscribeAsync(string topic, byte qos = 0);
        Task UnsubscribeAsync(string topic);

        // 이벤트
        event Func<string, string, Task> MessageReceivedAsync;
        event Func<bool, Task> ConnectionStatusChangedAsync;
    }
}