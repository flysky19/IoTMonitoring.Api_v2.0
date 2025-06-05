using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IoTMonitoring.Api.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace IoTMonitoring.Api.Services.Sensor.Interfaces
{
    public interface ISensorService
    {
        // 센서 기본 CRUD 작업
        Task<IEnumerable<SensorDto>> GetAllSensorsAsync(int? groupId = null, string status = null, string connectionStatus = null);
        Task<SensorDetailDto> GetSensorDetailAsync(int id);
        Task<SensorDetailDto> GetSensorByUUIDAsync(string sensorUUID); // 추가: UUID로 조회
        Task<SensorDetailDto> CreateSensorAsync(SensorCreateDto sensorDto);
        Task<SensorDetailDto> UpdateSensorAsync(int id, SensorUpdateDto sensorDto);

        Task DeactivateSensorAsync(int id);
        Task ActivateSensorAsync(int id);

        // 센서 상태 관련 (추가)
        Task<IEnumerable<SensorStatusDto>> GetSensorsStatusAsync();
        Task<SensorStatusDto> GetSensorStatusAsync(int sensorId);

        // 센서 데이터 관련 작업
        Task<IEnumerable<dynamic>> GetSensorDataAsync(int sensorId, SensorDataRequestDto request);
        Task<ParticleDataDto> AddParticleDataAsync(ParticleDataCreateDto dataDto);
        Task<WindDataDto> AddWindDataAsync(WindDataCreateDto dataDto);
        Task<TempHumidityDataDto> AddTempHumidityDataAsync(TempHumidityDataCreateDto dataDto);

        // 센서 연결 관련 작업
        Task UpdateSensorConnectionStatusAsync(int sensorId, string connectionStatus, string reason = null);
        Task UpdateSensorHeartbeatAsync(int sensorId);
        Task<IEnumerable<SensorConnectionHistoryDto>> GetSensorConnectionHistoryAsync(int sensorId, DateTime startDate, DateTime endDate, int limit = 100);

        // MQTT 토픽 관련 작업
        Task<SensorMqttTopicDto> GetSensorMqttTopicsAsync(int sensorId);
        Task<SensorMqttTopicDto> UpdateSensorMqttTopicsAsync(int sensorId, SensorMqttTopicUpdateDto topicDto);

        Task<SensorDetailDto> UpdateSensorGroupIdAsync(int id, int groupid);

        Task DeleteSensorAsync(int id);

    }
}