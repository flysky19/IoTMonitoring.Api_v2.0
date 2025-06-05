using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using IoTMonitoring.Api.Data.Connection;
using IoTMonitoring.Api.Data.Repositories.Interfaces;
using IoTMonitoring.Api.DTOs;

namespace IoTMonitoring.Api.Data.Repositories
{
    public class SensorDataRepository : ISensorDataRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public SensorDataRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<IEnumerable<dynamic>> GetParticleDataAsync(int sensorId, SensorDataRequestDto request)
        {
            using (var connection = await _connectionFactory.CreateConnectionAsync())
            {
                var sql = request.AggregationType.ToLower() switch
                {
                    "hourly" => GetParticleDataHourlySql(),
                    "daily" => GetParticleDataDailySql(),
                    _ => GetParticleDataRawSql()
                };

                return await connection.QueryAsync(sql, new
                {
                    SensorId = sensorId,
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    Limit = request.Limit
                });
            }
        }

        public async Task<IEnumerable<dynamic>> GetWindDataAsync(int sensorId, SensorDataRequestDto request)
        {
            using (var connection = await _connectionFactory.CreateConnectionAsync())
            {
                var sql = request.AggregationType.ToLower() switch
                {
                    "hourly" => GetWindDataHourlySql(),
                    "daily" => GetWindDataDailySql(),
                    _ => GetWindDataRawSql()
                };

                return await connection.QueryAsync(sql, new
                {
                    SensorId = sensorId,
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    Limit = request.Limit
                });
            }
        }

        public async Task<IEnumerable<dynamic>> GetTempHumidityDataAsync(int sensorId, SensorDataRequestDto request)
        {
            using (var connection = await _connectionFactory.CreateConnectionAsync())
            {
                var sql = request.AggregationType.ToLower() switch
                {
                    "hourly" => GetTempHumidityDataHourlySql(),
                    "daily" => GetTempHumidityDataDailySql(),
                    _ => GetTempHumidityDataRawSql()
                };

                return await connection.QueryAsync(sql, new
                {
                    SensorId = sensorId,
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    Limit = request.Limit
                });
            }
        }

        public async Task<IEnumerable<dynamic>> GetSpeakerStatusAsync(int sensorId, SensorDataRequestDto request)
        {
            using (var connection = await _connectionFactory.CreateConnectionAsync())
            {
                const string sql = @"
                    SELECT TOP(@Limit)
                        StatusID, SensorID, Timestamp, PowerStatus, Volume, Frequency
                    FROM SpeakerStatus
                    WHERE SensorID = @SensorId 
                      AND Timestamp >= @StartDate 
                      AND Timestamp <= @EndDate
                    ORDER BY Timestamp DESC";

                return await connection.QueryAsync(sql, new
                {
                    SensorId = sensorId,
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    Limit = request.Limit
                });
            }
        }

        public async Task<ParticleDataDto> AddParticleDataAsync(ParticleDataCreateDto dataDto)
        {
            using (var connection = await _connectionFactory.CreateConnectionAsync())
            {
                const string sql = @"
                    INSERT INTO ParticleData (
                        SensorID, Timestamp, PM1_0, PM2_5, PM4_0, PM10_0, PM_0_5, PM_5_0, RawData
                    ) 
                    OUTPUT INSERTED.DataID, INSERTED.SensorID, INSERTED.Timestamp,
                           INSERTED.PM1_0, INSERTED.PM2_5, INSERTED.PM4_0, INSERTED.PM10_0,
                           INSERTED.PM_0_5, INSERTED.PM_5_0, INSERTED.RawData
                    VALUES (
                        @SensorID, GETDATE(), @PM1_0, @PM2_5, @PM4_0, @PM10_0, @PM_0_5, @PM_5_0, @RawData
                    )";

                return await connection.QuerySingleAsync<ParticleDataDto>(sql, dataDto);
            }
        }

        public async Task<WindDataDto> AddWindDataAsync(WindDataCreateDto dataDto)
        {
            using (var connection = await _connectionFactory.CreateConnectionAsync())
            {
                const string sql = @"
                    INSERT INTO WindData (SensorID, Timestamp, WindSpeed, RawData) 
                    OUTPUT INSERTED.DataID, INSERTED.SensorID, INSERTED.Timestamp,
                           INSERTED.WindSpeed, INSERTED.RawData
                    VALUES (@SensorID, GETDATE(), @WindSpeed, @RawData)";

                return await connection.QuerySingleAsync<WindDataDto>(sql, dataDto);
            }
        }

        public async Task<TempHumidityDataDto> AddTempHumidityDataAsync(TempHumidityDataCreateDto dataDto)
        {
            using (var connection = await _connectionFactory.CreateConnectionAsync())
            {
                const string sql = @"
                    INSERT INTO TempHumidityData (SensorID, Timestamp, Temperature, Humidity, RawData) 
                    OUTPUT INSERTED.DataID, INSERTED.SensorID, INSERTED.Timestamp,
                           INSERTED.Temperature, INSERTED.Humidity, INSERTED.RawData
                    VALUES (@SensorID, GETDATE(), @Temperature, @Humidity, @RawData)";

                return await connection.QuerySingleAsync<TempHumidityDataDto>(sql, dataDto);
            }
        }

        #region SQL 쿼리 헬퍼 메서드

        private string GetParticleDataRawSql()
        {
            return @"
                SELECT TOP(@Limit)
                    DataID, SensorID, Timestamp, PM1_0, PM2_5, PM4_0, PM10_0, PM_0_5, PM_5_0
                FROM ParticleData
                WHERE SensorID = @SensorId 
                  AND Timestamp >= @StartDate 
                  AND Timestamp <= @EndDate
                ORDER BY Timestamp DESC";
        }

        private string GetParticleDataHourlySql()
        {
            return @"
                SELECT 
                    SensorID,
                    DATEADD(HOUR, DATEDIFF(HOUR, 0, Timestamp), 0) as Timestamp,
                    AVG(PM1_0) as PM1_0,
                    AVG(PM2_5) as PM2_5,
                    AVG(PM4_0) as PM4_0,
                    AVG(PM10_0) as PM10_0,
                    AVG(PM_0_5) as PM_0_5,
                    AVG(PM_5_0) as PM_5_0,
                    COUNT(*) as DataCount
                FROM ParticleData
                WHERE SensorID = @SensorId 
                  AND Timestamp >= @StartDate 
                  AND Timestamp <= @EndDate
                GROUP BY SensorID, DATEADD(HOUR, DATEDIFF(HOUR, 0, Timestamp), 0)
                ORDER BY Timestamp DESC";
        }

        private string GetParticleDataDailySql()
        {
            return @"
                SELECT 
                    SensorID,
                    CAST(Timestamp as DATE) as Timestamp,
                    AVG(PM1_0) as PM1_0,
                    AVG(PM2_5) as PM2_5,
                    AVG(PM4_0) as PM4_0,
                    AVG(PM10_0) as PM10_0,
                    AVG(PM_0_5) as PM_0_5,
                    AVG(PM_5_0) as PM_5_0,
                    COUNT(*) as DataCount
                FROM ParticleData
                WHERE SensorID = @SensorId 
                  AND Timestamp >= @StartDate 
                  AND Timestamp <= @EndDate
                GROUP BY SensorID, CAST(Timestamp as DATE)
                ORDER BY Timestamp DESC";
        }

        private string GetWindDataRawSql()
        {
            return @"
                SELECT TOP(@Limit)
                    DataID, SensorID, Timestamp, WindSpeed
                FROM WindData
                WHERE SensorID = @SensorId 
                  AND Timestamp >= @StartDate 
                  AND Timestamp <= @EndDate
                ORDER BY Timestamp DESC";
        }

        private string GetWindDataHourlySql()
        {
            return @"
                SELECT 
                    SensorID,
                    DATEADD(HOUR, DATEDIFF(HOUR, 0, Timestamp), 0) as Timestamp,
                    AVG(WindSpeed) as WindSpeed,
                    MAX(WindSpeed) as MaxWindSpeed,
                    MIN(WindSpeed) as MinWindSpeed,
                    COUNT(*) as DataCount
                FROM WindData
                WHERE SensorID = @SensorId 
                  AND Timestamp >= @StartDate 
                  AND Timestamp <= @EndDate
                GROUP BY SensorID, DATEADD(HOUR, DATEDIFF(HOUR, 0, Timestamp), 0)
                ORDER BY Timestamp DESC";
        }

        private string GetWindDataDailySql()
        {
            return @"
                SELECT 
                    SensorID,
                    CAST(Timestamp as DATE) as Timestamp,
                    AVG(WindSpeed) as WindSpeed,
                    MAX(WindSpeed) as MaxWindSpeed,
                    MIN(WindSpeed) as MinWindSpeed,
                    COUNT(*) as DataCount
                FROM WindData
                WHERE SensorID = @SensorId 
                  AND Timestamp >= @StartDate 
                  AND Timestamp <= @EndDate
                GROUP BY SensorID, CAST(Timestamp as DATE)
                ORDER BY Timestamp DESC";
        }

        private string GetTempHumidityDataRawSql()
        {
            return @"
                SELECT TOP(@Limit)
                    DataID, SensorID, Timestamp, Temperature, Humidity
                FROM TempHumidityData
                WHERE SensorID = @SensorId 
                  AND Timestamp >= @StartDate 
                  AND Timestamp <= @EndDate
                ORDER BY Timestamp DESC";
        }

        private string GetTempHumidityDataHourlySql()
        {
            return @"
                SELECT 
                    SensorID,
                    DATEADD(HOUR, DATEDIFF(HOUR, 0, Timestamp), 0) as Timestamp,
                    AVG(Temperature) as Temperature,
                    AVG(Humidity) as Humidity,
                    MAX(Temperature) as MaxTemperature,
                    MIN(Temperature) as MinTemperature,
                    MAX(Humidity) as MaxHumidity,
                    MIN(Humidity) as MinHumidity,
                    COUNT(*) as DataCount
                FROM TempHumidityData
                WHERE SensorID = @SensorId 
                  AND Timestamp >= @StartDate 
                  AND Timestamp <= @EndDate
                GROUP BY SensorID, DATEADD(HOUR, DATEDIFF(HOUR, 0, Timestamp), 0)
                ORDER BY Timestamp DESC";
        }

        private string GetTempHumidityDataDailySql()
        {
            return @"
                SELECT 
                    SensorID,
                    CAST(Timestamp as DATE) as Timestamp,
                    AVG(Temperature) as Temperature,
                    AVG(Humidity) as Humidity,
                    MAX(Temperature) as MaxTemperature,
                    MIN(Temperature) as MinTemperature,
                    MAX(Humidity) as MaxHumidity,
                    MIN(Humidity) as MinHumidity,
                    COUNT(*) as DataCount
                FROM TempHumidityData
                WHERE SensorID = @SensorId 
                  AND Timestamp >= @StartDate 
                  AND Timestamp <= @EndDate
                GROUP BY SensorID, CAST(Timestamp as DATE)
                ORDER BY Timestamp DESC";
        }

        #endregion
    }
}