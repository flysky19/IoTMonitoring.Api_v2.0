using IoTMonitoring.Api.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IoTMonitoring.Api.Services.Interfaces
{
    public interface IMqttManagementService
    {
        // MQTT 설정 관리
        Task<MqttSettingsDto> GetMqttSettingsAsync();
        Task UpdateMqttSettingsAsync(MqttSettingsUpdateDto settingsDto);

        // MQTT 연결 관리
        Task<MqttConnectionStatusDto> GetConnectionStatusAsync();
        Task RestartMqttServiceAsync();

        // MQTT 로그 조회
        Task<PagedResultDto<MqttMessageLogDto>> GetMqttLogsAsync(
            string topic = null,
            string direction = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            int page = 1,
            int pageSize = 50);

        // MQTT 테스트 메시지 발행
        Task PublishTestMessageAsync(string topic, string payload, byte qos = 0, bool retain = false);

        // MQTT 토픽 관리
        Task<IEnumerable<string>> GetAllActiveTopicsAsync();
        Task<Dictionary<string, int>> GetTopicSubscriptionCountsAsync();
    }
}