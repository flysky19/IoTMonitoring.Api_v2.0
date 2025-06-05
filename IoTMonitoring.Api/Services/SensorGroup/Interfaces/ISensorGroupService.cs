using IoTMonitoring.Api.DTOs;

namespace IoTMonitoring.Api.Services.SensorGroup.Interfaces
{
    public interface ISensorGroupService
    {
        Task<IEnumerable<SensorGroupDto>> GetAllSensorGroupsAsync(int? companyId = null);
        Task<SensorGroupDetailDto> GetSensorGroupDetailAsync(int groupId);
        Task<SensorGroupDetailDto> CreateSensorGroupAsync(SensorGroupCreateDto createDto);
        Task<SensorGroupDetailDto> UpdateSensorGroupAsync(int groupId, SensorGroupUpdateDto updateDto);
        Task DeleteSensorGroupAsync(int groupId);
        Task<IEnumerable<SensorDto>> GetGroupSensorsAsync(int groupId);
        Task<bool> GroupExistsAsync(int groupId);
    }
}