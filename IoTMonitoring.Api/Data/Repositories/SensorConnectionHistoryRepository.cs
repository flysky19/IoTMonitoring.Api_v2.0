using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using IoTMonitoring.Api.Data.Connection;
using IoTMonitoring.Api.Data.Repositories.Interfaces;
using IoTMonitoring.Api.DTOs;

namespace IoTMonitoring.Api.Data.Repositories
{
    public class SensorConnectionHistoryRepository : ISensorConnectionHistoryRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public SensorConnectionHistoryRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<IEnumerable<SensorConnectionHistoryDto>> GetConnectionHistoryAsync(int sensorId, DateTime startDate, DateTime endDate, int limit)
        {
            using (var connection = await _connectionFactory.CreateConnectionAsync())
            {
                const string sql = @"
                    SELECT TOP(@Limit)
                        HistoryID, SensorID, StatusChangeTime, OldStatus, NewStatus, ChangeReason
                    FROM SensorConnectionHistory
                    WHERE SensorID = @SensorId 
                      AND StatusChangeTime >= @StartDate 
                      AND StatusChangeTime <= @EndDate
                    ORDER BY StatusChangeTime DESC";

                return await connection.QueryAsync<SensorConnectionHistoryDto>(sql, new
                {
                    SensorId = sensorId,
                    StartDate = startDate,
                    EndDate = endDate,
                    Limit = limit
                });
            }
        }

        public async Task<IEnumerable<SensorConnectionHistoryDto>> GetRecentConnectionHistoryAsync(int sensorId, int limit = 10)
        {
            using (var connection = await _connectionFactory.CreateConnectionAsync())
            {
                const string sql = @"
                    SELECT TOP(@Limit)
                        HistoryID, SensorID, StatusChangeTime, OldStatus, NewStatus, ChangeReason
                    FROM SensorConnectionHistory
                    WHERE SensorID = @SensorId
                    ORDER BY StatusChangeTime DESC";

                return await connection.QueryAsync<SensorConnectionHistoryDto>(sql, new
                {
                    SensorId = sensorId,
                    Limit = limit
                });
            }
        }

        public async Task AddConnectionHistoryAsync(int sensorId, string oldStatus, string newStatus, string reason)
        {
            using (var connection = await _connectionFactory.CreateConnectionAsync())
            {
                const string sql = @"
                    INSERT INTO SensorConnectionHistory (
                        SensorID, StatusChangeTime, OldStatus, NewStatus, ChangeReason
                    ) VALUES (
                        @SensorId, GETDATE(), @OldStatus, @NewStatus, @ChangeReason
                    )";

                await connection.ExecuteAsync(sql, new
                {
                    SensorId = sensorId,
                    OldStatus = oldStatus,
                    NewStatus = newStatus,
                    ChangeReason = reason
                });
            }
        }

        public async Task<int> GetConnectionCountAsync(int sensorId, DateTime startDate, DateTime endDate)
        {
            using (var connection = await _connectionFactory.CreateConnectionAsync())
            {
                const string sql = @"
                    SELECT COUNT(*)
                    FROM SensorConnectionHistory
                    WHERE SensorID = @SensorId 
                      AND StatusChangeTime >= @StartDate 
                      AND StatusChangeTime <= @EndDate";

                return await connection.QuerySingleAsync<int>(sql, new
                {
                    SensorId = sensorId,
                    StartDate = startDate,
                    EndDate = endDate
                });
            }
        }
    }
}