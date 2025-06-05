using System.Collections.Generic;
using System.Threading.Tasks;
using IoTMonitoring.Api.DTOs;

namespace IoTMonitoring.Api.Data.Repositories.Interfaces
{
    public interface ISensorDataRepository
    {
        // 데이터 조회
        Task<IEnumerable<dynamic>> GetParticleDataAsync(int sensorId, SensorDataRequestDto request);
        Task<IEnumerable<dynamic>> GetWindDataAsync(int sensorId, SensorDataRequestDto request);
        Task<IEnumerable<dynamic>> GetTempHumidityDataAsync(int sensorId, SensorDataRequestDto request);
        Task<IEnumerable<dynamic>> GetSpeakerStatusAsync(int sensorId, SensorDataRequestDto request);

        // 데이터 추가
        Task<ParticleDataDto> AddParticleDataAsync(ParticleDataCreateDto dataDto);
        Task<WindDataDto> AddWindDataAsync(WindDataCreateDto dataDto);
        Task<TempHumidityDataDto> AddTempHumidityDataAsync(TempHumidityDataCreateDto dataDto);
    }
}