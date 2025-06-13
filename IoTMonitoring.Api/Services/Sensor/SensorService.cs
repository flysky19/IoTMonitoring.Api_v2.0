using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IoTMonitoring.Api.Data.Models;
using IoTMonitoring.Api.Data.Repositories.Interfaces;
using IoTMonitoring.Api.DTOs;
using IoTMonitoring.Api.Services.Sensor.Interfaces;
using IoTMonitoring.Api.Services.Logging.Interfaces;
using IoTMonitoring.Api.Mappers.Interfaces;
using Microsoft.AspNetCore.Mvc;
using IoTMonitoring.Api.Utilities;

namespace IoTMonitoring.Api.Services.Sensor
{
    public class SensorService : ISensorService
    {
        private readonly ISensorRepository _sensorRepository;
        private readonly ISensorDataRepository _sensorDataRepository;
        private readonly ISensorMqttTopicRepository _mqttTopicRepository;
        private readonly ISensorConnectionHistoryRepository _connectionHistoryRepository;
        private readonly ISensorMapper _sensorMapper;
        private readonly IAppLogger _logger;

        public SensorService(
            ISensorRepository sensorRepository,
            ISensorDataRepository sensorDataRepository,
            ISensorMqttTopicRepository mqttTopicRepository,
            ISensorConnectionHistoryRepository connectionHistoryRepository,
            ISensorMapper sensorMapper,
            IAppLogger logger)
        {
            _sensorRepository = sensorRepository;
            _sensorDataRepository = sensorDataRepository;
            _mqttTopicRepository = mqttTopicRepository;
            _connectionHistoryRepository = connectionHistoryRepository;
            _sensorMapper = sensorMapper;
            _logger = logger;
        }

        #region 센서 기본 CRUD 작업

        public async Task<IEnumerable<SensorDto>> GetAllSensorsAsync(int? companyId = null, int? groupId = null, string status = null, string connectionStatus = null)
        {
            try
            {
                _logger.LogInformation($"센서 목록 조회 시작 - CompanyId: {companyId},GroupId: {groupId}, Status: {status}, ConnectionStatus: {connectionStatus}");

                var sensors = await _sensorRepository.GetSensorsWithFiltersAsync(companyId, groupId, status, connectionStatus);
                var result = sensors.Select(_sensorMapper.ToDto).ToList();

                _logger.LogInformation($"센서 목록 조회 완료 - 조회된 센서 수: {result.Count}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"센서 목록 조회 중 오류: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<SensorDetailDto> GetSensorDetailAsync(int id)
        {
            try
            {
                _logger.LogInformation($"센서 상세 조회 시작 - ID: {id}");

                var sensor = await _sensorRepository.GetSensorWithDetailsAsync(id);
                if (sensor == null)
                {
                    throw new KeyNotFoundException($"센서 ID {id}를 찾을 수 없습니다.");
                }

                var result = _sensorMapper.ToDetailDto(sensor);

                // MQTT 토픽 정보 추가
                var mqttTopics = await _mqttTopicRepository.GetBySensorIdAsync(id);
                if (mqttTopics != null)
                {
                    result.MqttTopics = new SensorMqttTopicDto
                    {
                        TopicID = mqttTopics.TopicID,
                        SensorID = mqttTopics.SensorID,
                        DataTopic = mqttTopics.DataTopic,
                        ControlTopic = mqttTopics.ControlTopic,
                        StatusTopic = mqttTopics.StatusTopic,
                        HeartbeatTopic = mqttTopics.HeartbeatTopic,
                        QoS = mqttTopics.QoS,
                        Retained = mqttTopics.Retained
                    };
                }

                _logger.LogInformation($"센서 상세 조회 완료 - ID: {id}, Name: {result.Name}");
                return result;
            }
            catch (KeyNotFoundException)
            {
                _logger.LogWarning($"센서를 찾을 수 없음 - ID: {id}");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError($"센서 상세 조회 중 오류 (ID: {id}): {ex.Message}", ex);
                throw;
            }
        }

        public async Task<SensorDetailDto> GetSensorByUUIDAsync(string sensorUUID)
        {
            try
            {
                _logger.LogInformation($"센서 UUID 조회 시작 - UUID: {sensorUUID}");

                var sensor = await _sensorRepository.GetByUUIDAsync(sensorUUID);
                if (sensor == null)
                {
                    throw new KeyNotFoundException($"센서 UUID {sensorUUID}를 찾을 수 없습니다.");
                }

                return await GetSensorDetailAsync(sensor.SensorID);
            }
            catch (KeyNotFoundException)
            {
                _logger.LogWarning($"센서를 찾을 수 없음 - UUID: {sensorUUID}");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError($"센서 UUID 조회 중 오류 (UUID: {sensorUUID}): {ex.Message}", ex);
                throw;
            }
        }

        public async Task<SensorDetailDto> CreateSensorAsync(SensorCreateDto sensorDto)
        {
            try
            {
                _logger.LogInformation($"센서 생성 시작 - UUID: {sensorDto.SensorUUID}, Name: {sensorDto.Name}");

                // UUID 중복 체크
                if (await _sensorRepository.ExistsByUUIDAsync(sensorDto.SensorUUID))
                {
                    throw new InvalidOperationException($"센서 UUID {sensorDto.SensorUUID}가 이미 존재합니다.");
                }

                // 센서 생성
                var sensor = _sensorMapper.ToEntity(sensorDto);
                var sensorId = await _sensorRepository.CreateAsync(sensor);
                sensor.SensorID = sensorId;

                // MQTT 토픽 생성 (제공된 경우)
                if (sensorDto.MqttTopics != null)
                {
                    var mqttTopic = new SensorMqttTopic
                    {
                        SensorID = sensorId,
                        DataTopic = sensorDto.MqttTopics.DataTopic,
                        ControlTopic = sensorDto.MqttTopics.ControlTopic,
                        StatusTopic = sensorDto.MqttTopics.StatusTopic,
                        HeartbeatTopic = sensorDto.MqttTopics.HeartbeatTopic,
                        QoS = sensorDto.MqttTopics.QoS,
                        Retained = sensorDto.MqttTopics.Retained,
                        CreatedAt = DateTimeHelper.Now
                    };

                    await _mqttTopicRepository.CreateAsync(mqttTopic);
                }

                _logger.LogInformation($"센서 생성 완료 - ID: {sensorId}, UUID: {sensorDto.SensorUUID}");
                return await GetSensorDetailAsync(sensorId);
            }
            catch (InvalidOperationException)
            {
                _logger.LogWarning($"센서 생성 실패 - UUID 중복: {sensorDto.SensorUUID}");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError($"센서 생성 중 오류: {ex.Message}", ex);
                throw;
            }
        }
        public async Task<SensorDetailDto> UpdateSensorGroupIdAsync(int id, int groupid)
        {
            try
            {
                _logger.LogInformation($"센서 그룹 정보 수정 시작 - ID: {id}");

                var existingSensor = await _sensorRepository.GetByIdAsync(id);
                if (existingSensor == null)
                {
                    throw new KeyNotFoundException($"센서 ID {id}를 찾을 수 없습니다.");
                }

                existingSensor.GroupID = groupid;
                existingSensor.UpdatedAt = DateTimeHelper.Now;

                // 업데이트할 필드만 수정
                await _sensorRepository.UpdateAsync(existingSensor);

                _logger.LogInformation($"센서 수정 완료 - ID: {id}");
                return await GetSensorDetailAsync(id);
            }
            catch (KeyNotFoundException)
            {
                _logger.LogWarning($"센서를 찾을 수 없음 - ID: {id}");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError($"센서 수정 중 오류 (ID: {id}): {ex.Message}", ex);
                throw;
            }
        }

        public async Task<SensorDetailDto> UpdateSensorAsync(int id, SensorUpdateDto sensorDto)
        {
            try
            {
                _logger.LogInformation($"센서 수정 시작 - ID: {id}");

                var existingSensor = await _sensorRepository.GetByIdAsync(id);
                if (existingSensor == null)
                {
                    throw new KeyNotFoundException($"센서 ID {id}를 찾을 수 없습니다.");
                }

                // 업데이트할 필드만 수정
                _sensorMapper.UpdateEntity(existingSensor, sensorDto);
                await _sensorRepository.UpdateAsync(existingSensor);

                _logger.LogInformation($"센서 수정 완료 - ID: {id}");
                return await GetSensorDetailAsync(id);
            }
            catch (KeyNotFoundException)
            {
                _logger.LogWarning($"센서를 찾을 수 없음 - ID: {id}");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError($"센서 수정 중 오류 (ID: {id}): {ex.Message}", ex);
                throw;
            }
        }


        #endregion

        #region 센서 상태 관련

        public async Task<IEnumerable<SensorStatusDto>> GetSensorsStatusAsync()
        {
            try
            {
                _logger.LogInformation("모든 센서 상태 조회 시작");

                var sensors = await _sensorRepository.GetAllAsync();
                var result = sensors.Select(_sensorMapper.ToStatusDto).ToList();

                _logger.LogInformation($"센서 상태 조회 완료 - 조회된 센서 수: {result.Count}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"센서 상태 조회 중 오류: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<SensorStatusDto> GetSensorStatusAsync(int sensorId)
        {
            try
            {
                _logger.LogInformation($"센서 상태 조회 시작 - ID: {sensorId}");

                var sensor = await _sensorRepository.GetByIdAsync(sensorId);
                if (sensor == null)
                {
                    throw new KeyNotFoundException($"센서 ID {sensorId}를 찾을 수 없습니다.");
                }

                var result = _sensorMapper.ToStatusDto(sensor);
                _logger.LogInformation($"센서 상태 조회 완료 - ID: {sensorId}");
                return result;
            }
            catch (KeyNotFoundException)
            {
                _logger.LogWarning($"센서를 찾을 수 없음 - ID: {sensorId}");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError($"센서 상태 조회 중 오류 (ID: {sensorId}): {ex.Message}", ex);
                throw;
            }
        }

        #endregion

        #region 센서 데이터 관련 작업

        public async Task<IEnumerable<dynamic>> GetSensorDataAsync(int sensorId, SensorDataRequestDto request)
        {
            try
            {
                _logger.LogInformation($"센서 데이터 조회 시작 - ID: {sensorId}, StartDate: {request.StartDate}, EndDate: {request.EndDate}");

                // 센서 존재 확인
                var sensor = await _sensorRepository.GetByIdAsync(sensorId);
                if (sensor == null)
                {
                    throw new KeyNotFoundException($"센서 ID {sensorId}를 찾을 수 없습니다.");
                }

                // 센서 타입에 따라 다른 데이터 조회
                IEnumerable<dynamic> result = sensor.SensorType switch
                {
                    "particle" => await _sensorDataRepository.GetParticleDataAsync(sensorId, request),
                    "wind" => await _sensorDataRepository.GetWindDataAsync(sensorId, request),
                    "temp_humidity" => await _sensorDataRepository.GetTempHumidityDataAsync(sensorId, request),
                    "speaker" => await _sensorDataRepository.GetSpeakerStatusAsync(sensorId, request),
                    _ => throw new ArgumentException($"지원하지 않는 센서 타입: {sensor.SensorType}")
                };

                _logger.LogInformation($"센서 데이터 조회 완료 - ID: {sensorId}, 조회된 레코드 수: {result?.Count() ?? 0}");
                return result;
            }
            catch (KeyNotFoundException)
            {
                _logger.LogWarning($"센서를 찾을 수 없음 - ID: {sensorId}");
                throw;
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning($"잘못된 요청 - ID: {sensorId}, Message: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError($"센서 데이터 조회 중 오류 (ID: {sensorId}): {ex.Message}", ex);
                throw;
            }
        }

        public async Task<ParticleDataDto> AddParticleDataAsync(ParticleDataCreateDto dataDto)
        {
            try
            {
                _logger.LogInformation($"파티클 데이터 추가 시작 - SensorID: {dataDto.SensorID}");

                // 센서 존재 및 타입 확인
                var sensor = await _sensorRepository.GetByIdAsync(dataDto.SensorID);
                if (sensor == null)
                {
                    throw new KeyNotFoundException($"센서 ID {dataDto.SensorID}를 찾을 수 없습니다.");
                }

                if (sensor.SensorType != "particle")
                {
                    throw new ArgumentException($"센서 ID {dataDto.SensorID}는 파티클 센서가 아닙니다.");
                }

                var result = await _sensorDataRepository.AddParticleDataAsync(dataDto);

                // 센서 마지막 통신 시간 업데이트
                await UpdateSensorHeartbeatAsync(dataDto.SensorID);

                _logger.LogInformation($"파티클 데이터 추가 완료 - SensorID: {dataDto.SensorID}, DataID: {result.DataID}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"파티클 데이터 추가 중 오류 (SensorID: {dataDto.SensorID}): {ex.Message}", ex);
                throw;
            }
        }

        public async Task<WindDataDto> AddWindDataAsync(WindDataCreateDto dataDto)
        {
            try
            {
                _logger.LogInformation($"풍향 데이터 추가 시작 - SensorID: {dataDto.SensorID}");

                // 센서 존재 및 타입 확인
                var sensor = await _sensorRepository.GetByIdAsync(dataDto.SensorID);
                if (sensor == null)
                {
                    throw new KeyNotFoundException($"센서 ID {dataDto.SensorID}를 찾을 수 없습니다.");
                }

                if (sensor.SensorType != "wind")
                {
                    throw new ArgumentException($"센서 ID {dataDto.SensorID}는 풍향 센서가 아닙니다.");
                }

                var result = await _sensorDataRepository.AddWindDataAsync(dataDto);

                // 센서 마지막 통신 시간 업데이트
                await UpdateSensorHeartbeatAsync(dataDto.SensorID);

                _logger.LogInformation($"풍향 데이터 추가 완료 - SensorID: {dataDto.SensorID}, DataID: {result.DataID}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"풍향 데이터 추가 중 오류 (SensorID: {dataDto.SensorID}): {ex.Message}", ex);
                throw;
            }
        }

        public async Task<TempHumidityDataDto> AddTempHumidityDataAsync(TempHumidityDataCreateDto dataDto)
        {
            try
            {
                _logger.LogInformation($"온습도 데이터 추가 시작 - SensorID: {dataDto.SensorID}");

                // 센서 존재 및 타입 확인
                var sensor = await _sensorRepository.GetByIdAsync(dataDto.SensorID);
                if (sensor == null)
                {
                    throw new KeyNotFoundException($"센서 ID {dataDto.SensorID}를 찾을 수 없습니다.");
                }

                if (sensor.SensorType != "temp_humidity")
                {
                    throw new ArgumentException($"센서 ID {dataDto.SensorID}는 온습도 센서가 아닙니다.");
                }

                var result = await _sensorDataRepository.AddTempHumidityDataAsync(dataDto);

                // 센서 마지막 통신 시간 업데이트
                await UpdateSensorHeartbeatAsync(dataDto.SensorID);

                _logger.LogInformation($"온습도 데이터 추가 완료 - SensorID: {dataDto.SensorID}, DataID: {result.DataID}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"온습도 데이터 추가 중 오류 (SensorID: {dataDto.SensorID}): {ex.Message}", ex);
                throw;
            }
        }

        #endregion

        #region 센서 연결 관련 작업

        public async Task UpdateSensorConnectionStatusAsync(int sensorId, string connectionStatus, string reason = null)
        {
            try
            {
                _logger.LogInformation($"센서 연결 상태 업데이트 시작 - ID: {sensorId}, Status: {connectionStatus}");

                var sensor = await _sensorRepository.GetByIdAsync(sensorId);
                if (sensor == null)
                {
                    throw new KeyNotFoundException($"센서 ID {sensorId}를 찾을 수 없습니다.");
                }

                var oldStatus = sensor.ConnectionStatus;

                // 연결 상태 업데이트
                await _sensorRepository.UpdateConnectionStatusAsync(sensorId, connectionStatus);

                // 연결 이력 추가 (상태가 변경된 경우에만)
                if (oldStatus != connectionStatus)
                {
                    await _connectionHistoryRepository.AddConnectionHistoryAsync(
                        sensorId, oldStatus, connectionStatus, reason ?? "Manual update");
                }

                _logger.LogInformation($"센서 연결 상태 업데이트 완료 - ID: {sensorId}, {oldStatus} -> {connectionStatus}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"센서 연결 상태 업데이트 중 오류 (ID: {sensorId}): {ex.Message}", ex);
                throw;
            }
        }

        public async Task UpdateSensorHeartbeatAsync(int sensorId)
        {
            try
            {
                _logger.LogInformation($"센서 하트비트 업데이트 시작 - ID: {sensorId}");

                var sensor = await _sensorRepository.GetByIdAsync(sensorId);
                if (sensor == null)
                {
                    throw new KeyNotFoundException($"센서 ID {sensorId}를 찾을 수 없습니다.");
                }

                await _sensorRepository.UpdateHeartbeatAsync(sensorId);

                _logger.LogInformation($"센서 하트비트 업데이트 완료 - ID: {sensorId}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"센서 하트비트 업데이트 중 오류 (ID: {sensorId}): {ex.Message}", ex);
                throw;
            }
        }

        public async Task<IEnumerable<SensorConnectionHistoryDto>> GetSensorConnectionHistoryAsync(int sensorId, DateTime startDate, DateTime endDate, int limit = 100)
        {
            try
            {
                _logger.LogInformation($"센서 연결 이력 조회 시작 - ID: {sensorId}, StartDate: {startDate}, EndDate: {endDate}");

                // 센서 존재 확인
                var sensor = await _sensorRepository.GetByIdAsync(sensorId);
                if (sensor == null)
                {
                    throw new KeyNotFoundException($"센서 ID {sensorId}를 찾을 수 없습니다.");
                }

                var result = await _connectionHistoryRepository.GetConnectionHistoryAsync(sensorId, startDate, endDate, limit);

                _logger.LogInformation($"센서 연결 이력 조회 완료 - ID: {sensorId}, 조회된 레코드 수: {result?.Count() ?? 0}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"센서 연결 이력 조회 중 오류 (ID: {sensorId}): {ex.Message}", ex);
                throw;
            }
        }

        #endregion

        #region MQTT 토픽 관련 작업

        public async Task<SensorMqttTopicDto> GetSensorMqttTopicsAsync(int sensorId)
        {
            try
            {
                _logger.LogInformation($"센서 MQTT 토픽 조회 시작 - ID: {sensorId}");

                // 센서 존재 확인
                var sensor = await _sensorRepository.GetByIdAsync(sensorId);
                if (sensor == null)
                {
                    throw new KeyNotFoundException($"센서 ID {sensorId}를 찾을 수 없습니다.");
                }

                var mqttTopic = await _mqttTopicRepository.GetBySensorIdAsync(sensorId);
                if (mqttTopic == null)
                {
                    throw new KeyNotFoundException($"센서 ID {sensorId}의 MQTT 토픽을 찾을 수 없습니다.");
                }

                var result = new SensorMqttTopicDto
                {
                    TopicID = mqttTopic.TopicID,
                    SensorID = mqttTopic.SensorID,
                    DataTopic = mqttTopic.DataTopic,
                    ControlTopic = mqttTopic.ControlTopic,
                    StatusTopic = mqttTopic.StatusTopic,
                    HeartbeatTopic = mqttTopic.HeartbeatTopic,
                    QoS = mqttTopic.QoS,
                    Retained = mqttTopic.Retained
                };

                _logger.LogInformation($"센서 MQTT 토픽 조회 완료 - ID: {sensorId}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"센서 MQTT 토픽 조회 중 오류 (ID: {sensorId}): {ex.Message}", ex);
                throw;
            }
        }

        public async Task<SensorMqttTopicDto> UpdateSensorMqttTopicsAsync(int sensorId, SensorMqttTopicUpdateDto topicDto)
        {
            try
            {
                _logger.LogInformation($"센서 MQTT 토픽 업데이트 시작 - ID: {sensorId}");

                // 센서 존재 확인
                var sensor = await _sensorRepository.GetByIdAsync(sensorId);
                if (sensor == null)
                {
                    throw new KeyNotFoundException($"센서 ID {sensorId}를 찾을 수 없습니다.");
                }

                var mqttTopic = await _mqttTopicRepository.GetBySensorIdAsync(sensorId);
                if (mqttTopic == null)
                {
                    throw new KeyNotFoundException($"센서 ID {sensorId}의 MQTT 토픽을 찾을 수 없습니다.");
                }

                // 업데이트할 필드만 수정
                if (!string.IsNullOrEmpty(topicDto.DataTopic)) mqttTopic.DataTopic = topicDto.DataTopic;
                if (!string.IsNullOrEmpty(topicDto.ControlTopic)) mqttTopic.ControlTopic = topicDto.ControlTopic;
                if (!string.IsNullOrEmpty(topicDto.StatusTopic)) mqttTopic.StatusTopic = topicDto.StatusTopic;
                if (!string.IsNullOrEmpty(topicDto.HeartbeatTopic)) mqttTopic.HeartbeatTopic = topicDto.HeartbeatTopic;
                mqttTopic.QoS = topicDto.QoS;
                mqttTopic.Retained = topicDto.Retained;
                mqttTopic.UpdatedAt = DateTimeHelper.Now;

                await _mqttTopicRepository.UpdateAsync(mqttTopic);

                _logger.LogInformation($"센서 MQTT 토픽 업데이트 완료 - ID: {sensorId}");
                return await GetSensorMqttTopicsAsync(sensorId);
            }
            catch (Exception ex)
            {
                _logger.LogError($"센서 MQTT 토픽 업데이트 중 오류 (ID: {sensorId}): {ex.Message}", ex);
                throw;
            }
        }

        public async Task DeactivateSensorAsync(int id)
        {
            try
            {
                _logger.LogInformation($"센서 비활성화 시작 - ID: {id}");

                var sensor = await _sensorRepository.GetByIdAsync(id);
                if (sensor == null)
                {
                    throw new KeyNotFoundException($"센서 ID {id}를 찾을 수 없습니다.");
                }

                // 물리적 삭제 대신 상태를 'inactive'로 변경
                sensor.Status = "inactive";
                sensor.UpdatedAt = DateTimeHelper.Now;
                await _sensorRepository.UpdateAsync(sensor);

                _logger.LogInformation($"센서 비활성화 완료 - ID: {id}");
            }
            catch (KeyNotFoundException)
            {
                _logger.LogWarning($"센서를 찾을 수 없음 - ID: {id}");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError($"센서 비활성화 중 오류 (ID: {id}): {ex.Message}", ex);
                throw;
            }
        }

        public async Task ActivateSensorAsync(int id)
        {
            try
            {
                _logger.LogInformation($"센서 활성화 시작 - ID: {id}");

                var sensor = await _sensorRepository.GetByIdAsync(id);
                if (sensor == null)
                {
                    throw new KeyNotFoundException($"센서 ID {id}를 찾을 수 없습니다.");
                }

                // 물리적 삭제 대신 상태를 'inactive'로 변경
                sensor.Status = "active";
                sensor.UpdatedAt = DateTimeHelper.Now;
                await _sensorRepository.UpdateAsync(sensor);

                _logger.LogInformation($"센서 활성화 완료 - ID: {id}");
            }
            catch (KeyNotFoundException)
            {
                _logger.LogWarning($"센서를 찾을 수 없음 - ID: {id}");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError($"센서 활성화 중 오류 (ID: {id}): {ex.Message}", ex);
                throw;
            }
        }

        public async Task DeleteSensorAsync(int id)
        {
            try
            {
                _logger.LogInformation($"센서 활성화 시작 - ID: {id}");

                var sensor = await _sensorRepository.GetByIdAsync(id);
                if (sensor == null)
                {
                    throw new KeyNotFoundException($"센서 ID {id}를 찾을 수 없습니다.");
                }

                await _sensorRepository.DeleteAsync(id);

                _logger.LogInformation($"센서 삭제 완료 - ID: {id}");
            }
            catch (KeyNotFoundException)
            {
                _logger.LogWarning($"센서를 찾을 수 없음 - ID: {id}");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError($"센서 삭제 중 오류 (ID: {id}): {ex.Message}", ex);
                throw;
            }
        }

        #endregion
    }
}