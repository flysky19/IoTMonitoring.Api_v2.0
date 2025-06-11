using System;
using IoTMonitoring.Api.Data.Models;
using IoTMonitoring.Api.DTOs;
using IoTMonitoring.Api.Mappers.Interfaces;
using IoTMonitoring.Api.Utilities;

namespace IoTMonitoring.Api.Mappers
{
    public class SensorMapper : ISensorMapper
    {
        public SensorDto ToDto(Sensor sensor)
        {
            return new SensorDto
            {
                SensorID = sensor.SensorID,
                SensorUUID = sensor.SensorUUID,
                Name = sensor.Name,
                SensorType = sensor.SensorType,
                Status = sensor.Status,
                ConnectionStatus = sensor.ConnectionStatus,
                LastCommunication = sensor.LastCommunication,
                GroupID = sensor.GroupID,
                GroupName = sensor.SensorGroup?.GroupName,
                Location = sensor.SensorGroup?.Location
            };
        }

        public SensorDetailDto ToDetailDto(Sensor sensor)
        {
            return new SensorDetailDto
            {
                SensorID = sensor.SensorID,
                SensorUUID = sensor.SensorUUID,
                Name = sensor.Name,
                SensorType = sensor.SensorType,
                Status = sensor.Status,
                ConnectionStatus = sensor.ConnectionStatus,
                LastCommunication = sensor.LastCommunication,
                GroupID = sensor.GroupID,
                GroupName = sensor.SensorGroup?.GroupName,
                Location = sensor.SensorGroup?.Location,
                Model = sensor.Model,
                FirmwareVersion = sensor.FirmwareVersion,
                LastHeartbeat = sensor.LastHeartbeat,
                HeartbeatInterval = sensor.HeartbeatInterval,
                ConnectionTimeout = sensor.ConnectionTimeout,
                InstallationDate = sensor.InstallationDate,
                CreatedAt = sensor.CreatedAt,
                UpdatedAt = sensor.UpdatedAt,
                // MQTT 토픽은 별도로 조회하여 설정해야 함
                MqttTopics = null
            };
        }

        public Sensor ToEntity(SensorCreateDto request)
        {
            return new Sensor
            {
                GroupID = request.GroupID,
                SensorType = request.SensorType,
                SensorUUID = request.SensorUUID,
                Name = request.Name,
                Model = request.Model,
                FirmwareVersion = request.FirmwareVersion,
                Status = request.Status,
                HeartbeatInterval = request.HeartbeatInterval,
                ConnectionTimeout = request.ConnectionTimeout,
                InstallationDate = request.InstallationDate,
                CreatedAt = DateTime.UtcNow,
                ConnectionStatus = "unknown"
            };
        }

        public void UpdateEntity(Sensor sensor, SensorUpdateDto request)
        {
            if (request.GroupID.HasValue) sensor.GroupID = request.GroupID;
            if (!string.IsNullOrEmpty(request.Name)) sensor.Name = request.Name;
            if (!string.IsNullOrEmpty(request.Model)) sensor.Model = request.Model;
            if (!string.IsNullOrEmpty(request.FirmwareVersion)) sensor.FirmwareVersion = request.FirmwareVersion;
            if (request.HeartbeatInterval > 0) sensor.HeartbeatInterval = request.HeartbeatInterval;
            if (request.ConnectionTimeout > 0) sensor.ConnectionTimeout = request.ConnectionTimeout;
            if (request.InstallationDate.HasValue) sensor.InstallationDate = request.InstallationDate;

            sensor.UpdatedAt = DateTimeHelper.Now;
        }

        public SensorStatusDto ToStatusDto(Sensor sensor)
        {
            var now = DateTimeHelper.Now;
            var isOnline = sensor.ConnectionStatus == "online" &&
                          sensor.LastHeartbeat.HasValue &&
                          (now - sensor.LastHeartbeat.Value).TotalSeconds <= sensor.ConnectionTimeout;

            var statusMessage = sensor.ConnectionStatus switch
            {
                "online" when isOnline => "정상 작동 중",
                "online" when !isOnline => "통신 지연",
                "offline" => "연결 끊김",
                "unknown" => "상태 확인 중",
                _ => "알 수 없음"
            };

            return new SensorStatusDto
            {
                SensorID = sensor.SensorID,
                Name = sensor.Name,
                SensorType = sensor.SensorType,
                Status = sensor.Status,
                ConnectionStatus = sensor.ConnectionStatus,
                LastCommunication = sensor.LastCommunication,
                LastHeartbeat = sensor.LastHeartbeat,
                IsOnline = isOnline,
                UptimePercentage = CalculateUptimePercentage(sensor),
                StatusMessage = statusMessage
            };
        }

        private int CalculateUptimePercentage(Sensor sensor)
        {
            // 간단한 가동률 계산 (실제로는 SensorUptimeStats 테이블을 참조해야 함)
            if (!sensor.LastHeartbeat.HasValue) return 0;

            var daysSinceLastHeartbeat = (DateTimeHelper.Now - sensor.LastHeartbeat.Value).TotalDays;
            if (daysSinceLastHeartbeat > 1) return 0;

            return sensor.ConnectionStatus == "online" ? 100 : 50;
        }
    }
}