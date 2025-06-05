using IoTMonitoring.Api.Data.Models;

namespace IoTMonitoring.Api.Data.Repositories.Interfaces
{
    public interface ISensorGroupRepository
    {
        Task<IEnumerable<SensorGroup>> GetAllAsync(int? companyId = null);
        Task<IEnumerable<SensorGroup>> GetAllWithCompanyAsync(int? companyId = null);
        Task<SensorGroup> GetByIdAsync(int groupId);
        Task<SensorGroup> GetByIdWithCompanyAsync(int groupId);
        Task<SensorGroup> GetByNameAndCompanyAsync(string groupName, int? companyId);
        Task<SensorGroup> CreateAsync(SensorGroup sensorGroup);
        Task<SensorGroup> UpdateAsync(SensorGroup sensorGroup);
        Task DeleteAsync(int groupId);
        Task<bool> ExistsAsync(int groupId);
        Task<int> GetCountByCompanyIdAsync(int companyId);
    }
}