using IoTMonitoring.Api.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IoTMonitoring.Api.Services.Interfaces
{
    public interface ISpeakerService
    {
        // 스피커 상태 조회
        Task<SpeakerStatusDto> GetStatusAsync(int sensorId);

        // 스피커 제어
        Task<SpeakerControlResultDto> ControlSpeakerAsync(int sensorId, SpeakerControlDto controlRequest);

        // 제어 이력 조회
        Task<IEnumerable<SpeakerControlHistoryDto>> GetControlHistoryAsync(int sensorId, DateTime startDate, DateTime endDate, int limit = 50);

        // 스피커 상태 업데이트 (MQTT로부터 수신한 상태를 저장)
        Task UpdateSpeakerStatusAsync(int sensorId, bool powerStatus, int? volume, float? frequency);

        // 여러 스피커 일괄 제어
        Task<IEnumerable<SpeakerControlResultDto>> BulkControlSpeakersAsync(IEnumerable<int> sensorIds, SpeakerControlDto controlRequest);
    }
}
