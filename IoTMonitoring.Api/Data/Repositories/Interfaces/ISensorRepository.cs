using IoTMonitoring.Api.Data.Models;
using IoTMonitoring.Api.DTOs;

namespace IoTMonitoring.Api.Data.Repositories.Interfaces
{
    public interface ISensorRepository
    {
        // 기본 CRUD
        Task<IEnumerable<Sensor>> GetAllAsync();
        Task<Sensor> GetByIdAsync(int sensorId);
        Task<Sensor> GetByUUIDAsync(string sensorUUID);
        Task<int> CreateAsync(Sensor sensor);
        Task<bool> UpdateAsync(Sensor sensor);
        Task<bool> DeleteAsync(int sensorId);

        // 필터링 조회
        Task<IEnumerable<Sensor>> GetSensorsWithFiltersAsync(int? groupId, string status, string connectionStatus);
        Task<Sensor> GetSensorWithDetailsAsync(int sensorId);
        Task<IEnumerable<Sensor>> GetByGroupIdAsync(int groupId);
        Task<IEnumerable<Sensor>> GetBySensorTypeAsync(string sensorType);
        Task<IEnumerable<Sensor>> GetByStatusAsync(string status);
        Task<IEnumerable<Sensor>> GetByConnectionStatusAsync(string connectionStatus);

        Task<Dictionary<string, int>> GetCountBySensorTypeAsync(int? groupId = null);

        // 연결 상태 관리
        Task<bool> UpdateConnectionStatusAsync(int sensorId, string connectionStatus);
        Task<bool> UpdateHeartbeatAsync(int sensorId);

        // 유틸리티
        Task<bool> ExistsByUUIDAsync(string sensorUUID);

        Task<int> GetCountByGroupIdAsync(int groupId);

        Task<int> GetActiveCountAsync();
        Task<int> GetTotalCountAsync();
        Task<IEnumerable<SensorDto>> GetByGroupIdAsDtoAsync(int groupId);
        Task<Dictionary<string, int>> GetCountByStatusAsync();
        Task<Dictionary<string, int>> GetCountByConnectionStatusAsync(int? groupId = null);
        
    }
}