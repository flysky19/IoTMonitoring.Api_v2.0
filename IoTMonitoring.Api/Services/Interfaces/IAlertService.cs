using IoTMonitoring.Api.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IoTMonitoring.Api.Services.Interfaces
{
    public interface IAlertService
    {
        // 알림 설정 관련 작업
        Task<IEnumerable<AlertSettingDto>> GetAlertSettingsAsync(int? sensorId = null, string alertType = null);
        Task<AlertSettingDetailDto> GetAlertSettingByIdAsync(int id);
        Task<AlertSettingDto> CreateAlertSettingAsync(AlertSettingCreateDto settingDto);
        Task UpdateAlertSettingAsync(int id, AlertSettingUpdateDto settingDto);
        Task DeleteAlertSettingAsync(int id);

        // 알림 이력 관련 작업
        Task<PagedResultDto<AlertHistoryDto>> GetAlertHistoryAsync(
            int? sensorId = null,
            int? alertId = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            bool? acknowledged = null,
            int page = 1,
            int pageSize = 50);
        Task AcknowledgeAlertAsync(long historyId);

        // 알림 처리 작업
        Task ProcessSensorDataForAlertsAsync(int sensorId, string sensorType, dynamic sensorData);
        Task ProcessConnectionStatusForAlertsAsync(int sensorId, string oldStatus, string newStatus);

        // 알림 발송 작업
        Task SendAlertNotificationAsync(AlertHistoryDto alert);
        Task<bool> TestAlertNotificationAsync(int alertId, string notificationMethod, string recipient);
    }
}