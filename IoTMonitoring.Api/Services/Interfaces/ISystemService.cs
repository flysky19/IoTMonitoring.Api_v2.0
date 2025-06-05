using IoTMonitoring.Api.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IoTMonitoring.Api.Services.Interfaces
{
    public interface ISystemService
    {
        // 시스템 설정 관리
        Task<IEnumerable<SystemSettingDto>> GetSystemSettingsAsync(string key = null);
        Task UpdateSystemSettingAsync(string key, SystemSettingUpdateDto settingDto);

        // 데이터 정리 이력 관리
        Task<IEnumerable<DataPurgeHistoryDto>> GetDataPurgeHistoryAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<Dictionary<string, int>> PurgeDataAsync(int retentionDays, IEnumerable<string> tableNames = null);

        // 시스템 상태 정보
        Task<SystemStatusDto> GetSystemStatusAsync();
        Task<DatabaseStatsDto> GetDatabaseStatsAsync();

        // 백업 및 복원
        Task<string> CreateDatabaseBackupAsync(string backupName = null);
        Task<bool> RestoreDatabaseFromBackupAsync(string backupPath);
        Task<IEnumerable<string>> GetAvailableBackupsAsync();
    }
}