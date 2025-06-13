using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dapper;
using IoTMonitoring.Api.Data.Connection;
using IoTMonitoring.Api.Data.Models;
using IoTMonitoring.Api.Data.Repositories.Interfaces;
using IoTMonitoring.Api.DTOs;
using IoTMonitoring.Api.Utilities;
using Microsoft.EntityFrameworkCore;

namespace IoTMonitoring.Api.Data.Repositories
{
    public class SensorRepository : ISensorRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public SensorRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<IEnumerable<Sensor>> GetAllAsync()
        {
            using (var connection = await _connectionFactory.CreateConnectionAsync())
            {
                const string sql = @"
                    SELECT s.*, sg.GroupName, sg.Location as GroupLocation, sg.Description as GroupDescription,
                           c.CompanyName, c.Address as CompanyAddress
                    FROM Sensors s
                    LEFT JOIN SensorGroups sg ON s.GroupID = sg.GroupID
                    LEFT JOIN Companies c ON sg.CompanyID = c.CompanyID
                    ORDER BY s.CreatedAt DESC";

                var result = await connection.QueryAsync<Sensor, SensorGroup, Company, Sensor>(
                    sql,
                    (sensor, group, company) =>
                    {
                        if (group != null)
                        {
                            group.Company = company;
                            sensor.SensorGroup = group;
                        }
                        return sensor;
                    },
                    splitOn: "GroupName,CompanyName"
                );

                return result;
            }
        }

        public async Task<Sensor> GetByIdAsync(int sensorId)
        {
            using (var connection = await _connectionFactory.CreateConnectionAsync())
            {
                return await connection.QuerySingleOrDefaultAsync<Sensor>(
                    "SELECT * FROM Sensors WHERE SensorID = @SensorId",
                    new { SensorId = sensorId });
            }
        }

        public async Task<Sensor> GetSensorWithDetailsAsync(int sensorId)
        {
            using (var connection = await _connectionFactory.CreateConnectionAsync())
            {
                const string sql = @"
                    SELECT s.*, sg.GroupName, sg.Location as GroupLocation, sg.Description as GroupDescription,
                           c.CompanyName, c.Address as CompanyAddress
                    FROM Sensors s
                    LEFT JOIN SensorGroups sg ON s.GroupID = sg.GroupID
                    LEFT JOIN Companies c ON sg.CompanyID = c.CompanyID
                    WHERE s.SensorID = @SensorId";

                var result = await connection.QueryAsync<Sensor, SensorGroup, Company, Sensor>(
                    sql,
                    (sensor, group, company) =>
                    {
                        if (group != null)
                        {
                            group.Company = company;
                            sensor.SensorGroup = group;
                        }
                        return sensor;
                    },
                    new { SensorId = sensorId },
                    splitOn: "GroupName,CompanyName"
                );

                return result.FirstOrDefault();
            }
        }

        public async Task<Sensor> GetByUUIDAsync(string sensorUUID)
        {
            using (var connection = await _connectionFactory.CreateConnectionAsync())
            {
                return await connection.QuerySingleOrDefaultAsync<Sensor>(
                    "SELECT * FROM Sensors WHERE SensorUUID = @SensorUUID",
                    new { SensorUUID = sensorUUID });
            }
        }

        public async Task<IEnumerable<Sensor>> GetSensorsWithFiltersAsync(int? companyId, int? groupId, string status, string connectionStatus)
        {
            using (var connection = await _connectionFactory.CreateConnectionAsync())
            {
                var whereConditions = new List<string>();
                var parameters = new DynamicParameters();

                if (companyId.HasValue)
                {
                    whereConditions.Add("s.CompanyID = @CompanyId");
                    parameters.Add("CompanyId", companyId.Value);
                }

                if (groupId.HasValue)
                {
                    whereConditions.Add("s.GroupID = @GroupId");
                    parameters.Add("GroupId", groupId.Value);
                }

                if (!string.IsNullOrEmpty(status))
                {
                    whereConditions.Add("s.Status = @Status");
                    parameters.Add("Status", status);
                }

                if (!string.IsNullOrEmpty(connectionStatus))
                {
                    whereConditions.Add("s.ConnectionStatus = @ConnectionStatus");
                    parameters.Add("ConnectionStatus", connectionStatus);
                }

                var whereClause = whereConditions.Any() ? "WHERE " + string.Join(" AND ", whereConditions) : "";

                var sql = $@"
                    SELECT s.*, sg.GroupName, sg.Location as GroupLocation, sg.Description as GroupDescription
                    FROM Sensors s
                    LEFT JOIN SensorGroups sg ON s.GroupID = sg.GroupID
                    {whereClause}
                    ORDER BY s.CreatedAt DESC";

                var result = await connection.QueryAsync<Sensor, SensorGroup, Sensor>(
                    sql,
                    (sensor, group) =>
                    {
                        sensor.SensorGroup = group;
                        return sensor;
                    },
                    parameters,
                    splitOn: "GroupName"
                );

                return result;
            }
        }

        public async Task<IEnumerable<Sensor>> GetByGroupIdAsync(int groupId)
        {
            using (var connection = await _connectionFactory.CreateConnectionAsync())
            {
                return await connection.QueryAsync<Sensor>(
                    "SELECT * FROM Sensors WHERE GroupID = @GroupId ORDER BY Name",
                    new { GroupId = groupId });
            }
        }

        public async Task<IEnumerable<Sensor>> GetBySensorTypeAsync(string sensorType)
        {
            using (var connection = await _connectionFactory.CreateConnectionAsync())
            {
                return await connection.QueryAsync<Sensor>(
                    "SELECT * FROM Sensors WHERE SensorType = @SensorType ORDER BY Name",
                    new { SensorType = sensorType });
            }
        }

        public async Task<IEnumerable<Sensor>> GetByStatusAsync(string status)
        {
            using (var connection = await _connectionFactory.CreateConnectionAsync())
            {
                return await connection.QueryAsync<Sensor>(
                    "SELECT * FROM Sensors WHERE Status = @Status ORDER BY Name",
                    new { Status = status });
            }
        }

        public async Task<IEnumerable<Sensor>> GetByConnectionStatusAsync(string connectionStatus)
        {
            using (var connection = await _connectionFactory.CreateConnectionAsync())
            {
                return await connection.QueryAsync<Sensor>(
                    "SELECT * FROM Sensors WHERE ConnectionStatus = @ConnectionStatus ORDER BY Name",
                    new { ConnectionStatus = connectionStatus });
            }
        }

        public async Task<int> CreateAsync(Sensor sensor)
        {
            using (var connection = await _connectionFactory.CreateConnectionAsync())
            {
                const string sql = @"
                    INSERT INTO Sensors (
                        GroupID, SensorType, SensorUUID, Name, Model, FirmwareVersion, 
                        Status, ConnectionStatus, HeartbeatInterval, ConnectionTimeout, 
                        InstallationDate, CreatedAt
                    ) 
                    OUTPUT INSERTED.SensorID
                    VALUES (
                        @GroupId, @SensorType, @SensorUUID, @Name, @Model, @FirmwareVersion,
                        @Status, @ConnectionStatus, @HeartbeatInterval, @ConnectionTimeout,
                        @InstallationDate, @CreatedAt
                    )";

                return await connection.QuerySingleAsync<int>(sql, sensor);
            }
        }

        public async Task<bool> UpdateAsync(Sensor sensor)
        {
            var GroupID = sensor.GroupID == 0 ? (int?)null : sensor.GroupID;  // 0을 NULL로 변환

            using (var connection = await _connectionFactory.CreateConnectionAsync())
            {
                const string sql = @"
                    UPDATE Sensors SET
                        GroupID = @GroupID,
                        Name = @Name,
                        Model = @Model,
                        FirmwareVersion = @FirmwareVersion,
                        Status = @Status,
                        HeartbeatInterval = @HeartbeatInterval,
                        ConnectionTimeout = @ConnectionTimeout,
                        InstallationDate = @InstallationDate,
                        UpdatedAt = @UpdatedAt
                    WHERE SensorID = @SensorId";

                var parameters = new
                {
                    GroupID = sensor.GroupID == 0 ? (int?)null : sensor.GroupID,
                    Name = sensor.Name,
                    Model = sensor.Model,
                    FirmwareVersion = sensor.FirmwareVersion,
                    Status = sensor.Status,
                    HeartbeatInterval = sensor.HeartbeatInterval,
                    ConnectionTimeout = sensor.ConnectionTimeout,
                    InstallationDate = sensor.InstallationDate,
                    UpdatedAt = sensor.UpdatedAt,
                    SensorID = sensor.SensorID  // WHERE 절에 사용되는 ID
                };

                var rowsAffected = await connection.ExecuteAsync(sql, parameters);
                return rowsAffected > 0;
            }
        }

        public async Task<bool> DeleteAsync(int sensorId)
        {
            using (var connection = await _connectionFactory.CreateConnectionAsync())
            {
                var rowsAffected = await connection.ExecuteAsync(
                    "DELETE FROM Sensors WHERE SensorID = @SensorId",
                    new { SensorId = sensorId });

                return rowsAffected > 0;
            }
        }

        public async Task<bool> UpdateConnectionStatusAsync(int sensorId, string connectionStatus)
        {
            using (var connection = await _connectionFactory.CreateConnectionAsync())
            {
                const string sql = @"
                    UPDATE Sensors SET
                        ConnectionStatus = @ConnectionStatus,
                        LastCommunication = @LastCommunication,
                        UpdatedAt = @UpdatedAt
                    WHERE SensorID = @SensorId";

                var rowsAffected = await connection.ExecuteAsync(sql, new
                {
                    SensorId = sensorId,
                    ConnectionStatus = connectionStatus,
                    LastCommunication = DateTimeHelper.Now,
                    UpdatedAt = DateTimeHelper.Now
                });

                return rowsAffected > 0;
            }
        }

        public async Task<bool> UpdateHeartbeatAsync(int sensorId)
        {
            using (var connection = await _connectionFactory.CreateConnectionAsync())
            {
                const string sql = @"
                    UPDATE Sensors SET
                        LastHeartbeat = @LastHeartbeat,
                        LastCommunication = @LastCommunication,
                        ConnectionStatus = 'online',
                        UpdatedAt = @UpdatedAt
                    WHERE SensorID = @SensorId";

                var now = DateTimeHelper.Now;
                var rowsAffected = await connection.ExecuteAsync(sql, new
                {
                    SensorId = sensorId,
                    LastHeartbeat = now,
                    LastCommunication = now,
                    UpdatedAt = now
                });

                return rowsAffected > 0;
            }
        }

        public async Task<bool> ExistsByUUIDAsync(string sensorUUID)
        {
            using (var connection = await _connectionFactory.CreateConnectionAsync())
            {
                var count = await connection.QuerySingleAsync<int>(
                    "SELECT COUNT(*) FROM Sensors WHERE SensorUUID = @SensorUUID",
                    new { SensorUUID = sensorUUID });

                return count > 0;
            }
        }

        /// <summary>
        /// 센서 타입별 개수 조회 (그룹별 필터 옵션)
        /// </summary>
        public async Task<Dictionary<string, int>> GetCountBySensorTypeAsync(int? groupId = null)
        {
            using (var connection = await _connectionFactory.CreateConnectionAsync())
            {
                var whereClause = groupId.HasValue ? "WHERE GroupID = @GroupId" : "";
                var sql = $@"
            SELECT SensorType, COUNT(*) as Count
            FROM Sensors 
            {whereClause}
            GROUP BY SensorType";

                var parameters = groupId.HasValue ? new { GroupId = groupId.Value } : null;
                var result = await connection.QueryAsync(sql, parameters);

                return result.ToDictionary(
                    row => (string)row.SensorType,
                    row => (int)row.Count
                );
            }
        }

        public async Task<int> GetCountByGroupIdAsync(int groupId)
        {
            using (var connection = await _connectionFactory.CreateConnectionAsync())
            {
                const string sql = @"
                    SELECT COUNT(*) 
                    FROM Sensors 
                    WHERE GroupID = @GroupId AND Status = 'active'";

                return await connection.QuerySingleAsync<int>(sql, new { GroupId = groupId });
            }
        }

        /// <summary>
        /// 연결 상태별 센서 개수 조회 (그룹별 필터 옵션)
        /// </summary>
        public async Task<Dictionary<string, int>> GetCountByConnectionStatusAsync(int? groupId = null)
        {
            using (var connection = await _connectionFactory.CreateConnectionAsync())
            {
                var whereClause = groupId.HasValue ? "WHERE GroupID = @GroupId" : "";
                var sql = $@"
            SELECT ConnectionStatus, COUNT(*) as Count
            FROM Sensors 
            {whereClause}
            GROUP BY ConnectionStatus";

                var parameters = groupId.HasValue ? new { GroupId = groupId.Value } : null;
                var result = await connection.QueryAsync(sql, parameters);

                return result.ToDictionary(
                    row => (string)row.ConnectionStatus,
                    row => (int)row.Count
                );
            }
        }

        /// <summary>
        /// 상태별 센서 개수 조회
        /// </summary>
        public async Task<Dictionary<string, int>> GetCountByStatusAsync()
        {
            using (var connection = await _connectionFactory.CreateConnectionAsync())
            {
                const string sql = @"
            SELECT Status, COUNT(*) as Count
            FROM Sensors 
            GROUP BY Status";

                var result = await connection.QueryAsync(sql);

                return result.ToDictionary(
                    row => (string)row.Status,
                    row => (int)row.Count
                );
            }
        }

        /// <summary>
        /// 그룹별 센서 목록 조회 (SensorDto 형태로)
        /// </summary>
        public async Task<IEnumerable<SensorDto>> GetByGroupIdAsDtoAsync(int groupId)
        {
            using (var connection = await _connectionFactory.CreateConnectionAsync())
            {
                const string sql = @"
            SELECT s.SensorID, s.GroupID, s.SensorType, s.SensorUUID, s.Name, s.Model, 
                   s.FirmwareVersion, s.Status, s.ConnectionStatus, s.LastCommunication, 
                   s.LastHeartbeat, s.HeartbeatInterval, s.ConnectionTimeout, 
                   s.InstallationDate, s.CreatedAt, s.UpdatedAt,
                   sg.GroupName
            FROM Sensors s
            LEFT JOIN SensorGroups sg ON s.GroupID = sg.GroupID
            WHERE s.GroupID = @GroupId
            ORDER BY s.Name";

                return await connection.QueryAsync<SensorDto>(sql, new { GroupId = groupId });
            }
        }

        /// <summary>
        /// 전체 센서 개수 조회
        /// </summary>
        public async Task<int> GetTotalCountAsync()
        {
            using (var connection = await _connectionFactory.CreateConnectionAsync())
            {
                return await connection.QuerySingleAsync<int>("SELECT COUNT(*) FROM Sensors");
            }
        }

        /// <summary>
        /// 활성 센서 개수 조회
        /// </summary>
        public async Task<int> GetActiveCountAsync()
        {
            using (var connection = await _connectionFactory.CreateConnectionAsync())
            {
                return await connection.QuerySingleAsync<int>(
                    "SELECT COUNT(*) FROM Sensors WHERE Status = 'active'"
                );
            }
        }

        /// <summary>
        /// 온라인 센서 개수 조회
        /// </summary>
        public async Task<int> GetOnlineCountAsync()
        {
            using (var connection = await _connectionFactory.CreateConnectionAsync())
            {
                return await connection.QuerySingleAsync<int>(
                    "SELECT COUNT(*) FROM Sensors WHERE ConnectionStatus = 'online'"
                );
            }
        }
    }
}