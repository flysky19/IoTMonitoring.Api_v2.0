using IoTMonitoring.Api.Services.SensorData.Interfaces;
using IoTMonitoring.Api.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using IoTMonitoring.Api.Data.Models;
using IoTMonitoring.Api.Data.DbContext;

namespace IoTMonitoring.Api.Services.SensorData
{
    public class SensorDataService : ISensorDataService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SensorDataService> _logger;

        public SensorDataService(ApplicationDbContext context, ILogger<SensorDataService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SaveSensorDataAsync(int sensorId, string sensorType, string jsonData)
        {
            try
            {
                _logger.LogInformation($"센서 데이터 저장 시작 - SensorID: {sensorId}, Type: {sensorType}");

                switch (sensorType.ToLower())
                {
                    case "temp_humidity":
                        await SaveTempHumidityDataAsync(sensorId, jsonData);
                        break;
                    case "particle":
                        await SaveParticleDataAsync(sensorId, jsonData);
                        break;
                    case "wind":
                        await SaveWindDataAsync(sensorId, jsonData);
                        break;
                    default:
                        _logger.LogWarning($"지원하지 않는 센서 타입: {sensorType}");
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"센서 데이터 저장 실패 - SensorID: {sensorId}, Error: {ex.Message}");
                throw;
            }
        }

        private async Task SaveTempHumidityDataAsync(int sensorId, string jsonData)
        {
            var data = JsonSerializer.Deserialize<JsonElement>(jsonData);

            var tempHumidityData = new TempHumidityData
            {
                SensorID = sensorId,
                Timestamp = data.TryGetProperty("timestamp", out var timestampProp)
                    ? DateTime.Parse(timestampProp.GetString())
                    : DateTime.UtcNow,
                Temperature = data.TryGetProperty("temperature", out var tempProp)
                    ? tempProp.GetSingle()
                    : null,
                Humidity = data.TryGetProperty("humidity", out var humidityProp)
                    ? humidityProp.GetSingle()
                    : null,
                RawData = jsonData
            };

            _context.TempHumidityData.Add(tempHumidityData);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"온습도 데이터 저장 완료 - SensorID: {sensorId}, Temp: {tempHumidityData.Temperature}, Humidity: {tempHumidityData.Humidity}");
        }

        private async Task SaveParticleDataAsync(int sensorId, string jsonData)
        {
            var data = JsonSerializer.Deserialize<JsonElement>(jsonData);

            var particleData = new ParticleData
            {
                SensorID = sensorId,
                Timestamp = data.TryGetProperty("timestamp", out var timestampProp)
                    ? DateTime.Parse(timestampProp.GetString())
                    : DateTime.UtcNow,
                PM1_0 = data.TryGetProperty("pm1_0", out var pm1Prop) ? pm1Prop.GetSingle() : null,
                PM2_5 = data.TryGetProperty("pm2_5", out var pm25Prop) ? pm25Prop.GetSingle() : null,
                PM4_0 = data.TryGetProperty("pm4_0", out var pm4Prop) ? pm4Prop.GetSingle() : null,
                PM10_0 = data.TryGetProperty("pm10_0", out var pm10Prop) ? pm10Prop.GetSingle() : null,
                PM_0_5 = data.TryGetProperty("pm_0_5", out var pm05Prop) ? pm05Prop.GetSingle() : null,
                PM_5_0 = data.TryGetProperty("pm_5_0", out var pm50Prop) ? pm50Prop.GetSingle() : null,
                RawData = jsonData
            };

            _context.ParticleData.Add(particleData);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"파티클 데이터 저장 완료 - SensorID: {sensorId}");
        }

        private async Task SaveWindDataAsync(int sensorId, string jsonData)
        {
            // WindData 구현
            _logger.LogInformation($"풍향 데이터 저장 - SensorID: {sensorId}");
        }

        public async Task<IEnumerable<dynamic>> GetSensorDataAsync(int sensorId, DateTime startDate, DateTime endDate, int limit)
        {
            // 센서 타입에 따라 해당 데이터 조회
            var sensor = await _context.Sensors.FindAsync(sensorId);
            if (sensor == null) return new List<dynamic>();

            switch (sensor.SensorType.ToLower())
            {
                case "temp_humidity":
                    return await _context.TempHumidityData
                        .Where(d => d.SensorID == sensorId && d.Timestamp >= startDate && d.Timestamp <= endDate)
                        .OrderByDescending(d => d.Timestamp)
                        .Take(limit)
                        .Select(d => new {
                            d.DataID,
                            d.Timestamp,
                            d.Temperature,
                            d.Humidity
                        })
                        .ToListAsync();

                case "particle":
                    return await _context.ParticleData
                        .Where(d => d.SensorID == sensorId && d.Timestamp >= startDate && d.Timestamp <= endDate)
                        .OrderByDescending(d => d.Timestamp)
                        .Take(limit)
                        .Select(d => new {
                            d.DataID,
                            d.Timestamp,
                            d.PM1_0,
                            d.PM2_5,
                            d.PM10_0
                        })
                        .ToListAsync();

                default:
                    return new List<dynamic>();
            }
        }
    }
}