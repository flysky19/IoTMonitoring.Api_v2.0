using IoTMonitoring.Api.Data.Models;
using IoTMonitoring.Api.DTOs;

namespace IoTMonitoring.Api.Mappers.Interfaces
{
    public interface ISensorGroupMapper
    {
        SensorGroupDto ToDto(SensorGroup entity);
        SensorGroupDetailDto ToDetailDto(SensorGroup entity);
        SensorGroup ToEntity(SensorGroupCreateDto createDto);
        void UpdateEntity(SensorGroup entity, SensorGroupUpdateDto updateDto);
    }
}