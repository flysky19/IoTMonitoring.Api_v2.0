using IoTMonitoring.Api.Data.Models;
using IoTMonitoring.Api.DTOs;

namespace IoTMonitoring.Api.Mappers.Interfaces
{
    public interface ISensorMapper
    {
        SensorDto ToDto(Sensor sensor);
        SensorDetailDto ToDetailDto(Sensor sensor);
        Sensor ToEntity(SensorCreateDto request);
        void UpdateEntity(Sensor sensor, SensorUpdateDto request);
        SensorStatusDto ToStatusDto(Sensor sensor);
    }
}