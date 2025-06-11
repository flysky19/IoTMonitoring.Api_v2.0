using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using IoTMonitoring.Api.Data.Connection;
using IoTMonitoring.Api.Data.Models;
using IoTMonitoring.Api.Data.Repositories.Interfaces;
using IoTMonitoring.Api.Utilities;
using Microsoft.Extensions.Logging;

namespace IoTMonitoring.Api.Data.Repositories
{
    public class SensorGroupRepository : ISensorGroupRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly ILogger<SensorGroupRepository> _logger;

        public SensorGroupRepository(IDbConnectionFactory connectionFactory, ILogger<SensorGroupRepository> logger)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<SensorGroup>> GetAllAsync(int? companyId = null)
        {
            try
            {
                _logger.LogInformation("센서 그룹 목록 조회 - CompanyId: {companyId}", companyId);

                string sql = @"
                    SELECT * FROM SensorGroups 
                    WHERE Active = 1
                    AND (@CompanyId IS NULL OR CompanyID = @CompanyId)
                    ORDER BY GroupName";

                using (var connection = await _connectionFactory.CreateConnectionAsync())
                {
                    return await connection.QueryAsync<SensorGroup>(sql, new { CompanyId = companyId });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "센서 그룹 목록 조회 실패");
                throw;
            }
        }

        public async Task<IEnumerable<SensorGroup>> GetAllWithCompanyAsync(int? companyId = null)
        {
            try
            {
                _logger.LogInformation("센서 그룹 목록 조회 (회사 정보 포함) - CompanyId: {companyId}", companyId);

                string sql = @"
                    SELECT sg.*, c.*
                    FROM SensorGroups sg
                    INNER JOIN Companies c ON sg.CompanyID = c.CompanyID
                    WHERE sg.Active = 1
                    AND (@CompanyId IS NULL OR sg.CompanyID = @CompanyId)
                    ORDER BY sg.GroupName";

                using (var connection = await _connectionFactory.CreateConnectionAsync())
                {
                    var sensorGroupDictionary = new Dictionary<int, SensorGroup>();

                    var result = await connection.QueryAsync<SensorGroup, Company, SensorGroup>(
                        sql,
                        (sensorGroup, company) =>
                        {
                            if (!sensorGroupDictionary.TryGetValue(sensorGroup.GroupID, out var existingGroup))
                            {
                                existingGroup = sensorGroup;
                                existingGroup.Company = company;
                                sensorGroupDictionary.Add(existingGroup.GroupID, existingGroup);
                            }
                            return existingGroup;
                        },
                        new { CompanyId = companyId },
                        splitOn: "CompanyID"
                    );

                    _logger.LogInformation("센서 그룹 목록 조회 완료 (회사 정보 포함)");
                    return sensorGroupDictionary.Values.ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "센서 그룹 목록 조회 실패 (회사 정보 포함)");
                throw;
            }
        }

        public async Task<SensorGroup> GetByIdAsync(int groupId)
        {
            try
            {
                string sql = @"
                    SELECT * FROM SensorGroups 
                    WHERE GroupID = @GroupId AND Active = 1";

                using (var connection = await _connectionFactory.CreateConnectionAsync())
                {
                    return await connection.QuerySingleOrDefaultAsync<SensorGroup>(sql, new { GroupId = groupId });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "센서 그룹 조회 실패 - GroupId: {groupId}", groupId);
                throw;
            }
        }

        public async Task<SensorGroup> GetByIdWithCompanyAsync(int groupId)
        {
            try
            {
                string sql = @"
                    SELECT sg.*, c.*
                    FROM SensorGroups sg
                    INNER JOIN Companies c ON sg.CompanyID = c.CompanyID
                    WHERE sg.GroupID = @GroupId AND sg.Active = 1";

                using (var connection = await _connectionFactory.CreateConnectionAsync())
                {
                    var result = await connection.QueryAsync<SensorGroup, Company, SensorGroup>(
                        sql,
                        (sensorGroup, company) =>
                        {
                            sensorGroup.Company = company;
                            return sensorGroup;
                        },
                        new { GroupId = groupId },
                        splitOn: "CompanyID"
                    );

                    return result.FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "센서 그룹 조회 실패 (회사 정보 포함) - GroupId: {groupId}", groupId);
                throw;
            }
        }

        public async Task<SensorGroup> GetByNameAndCompanyAsync(string groupName, int? companyId)
        {
            try
            {
                string sql = @"
                    SELECT * FROM SensorGroups 
                    WHERE GroupName = @GroupName 
                    AND CompanyID = @CompanyId 
                    AND Active = 1";

                using (var connection = await _connectionFactory.CreateConnectionAsync())
                {
                    return await connection.QuerySingleOrDefaultAsync<SensorGroup>(
                        sql,
                        new { GroupName = groupName, CompanyId = companyId }
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "센서 그룹 조회 실패 - GroupName: {groupName}, CompanyId: {companyId}", groupName, companyId);
                throw;
            }
        }

        public async Task<SensorGroup> CreateAsync(SensorGroup sensorGroup)
        {
            try
            {
                string sql = @"
                    INSERT INTO SensorGroups (
                        CompanyID, GroupName, Location, Description, CreatedAt, Active
                    ) VALUES (
                        @CompanyID, @GroupName, @Location, @Description, @CreatedAt, @Active
                    );
                    SELECT CAST(SCOPE_IDENTITY() as int);";

                using (var connection = await _connectionFactory.CreateConnectionAsync())
                {
                    // 현재 시간 설정
                    if (sensorGroup.CreatedAt == default)
                        sensorGroup.CreatedAt = DateTimeHelper.Now;

                    // Active 기본값 설정
                    if (!sensorGroup.Active)
                        sensorGroup.Active = true;

                    int newId = await connection.QuerySingleAsync<int>(sql, new
                    {
                        sensorGroup.CompanyID,
                        sensorGroup.GroupName,
                        sensorGroup.Location,
                        sensorGroup.Description,
                        sensorGroup.CreatedAt,
                        sensorGroup.Active
                    });

                    sensorGroup.GroupID = newId;
                    return sensorGroup;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "센서 그룹 생성 실패 - GroupName: {groupName}", sensorGroup.GroupName);
                throw;
            }
        }

        public async Task<SensorGroup> UpdateAsync(SensorGroup sensorGroup)
        {
            try
            {
                string sql = @"
                    UPDATE SensorGroups 
                    SET GroupName = @GroupName,
                        Location = @Location,
                        Description = @Description,
                        UpdatedAt = @UpdatedAt
                    WHERE GroupID = @GroupID AND Active = 1;
                    
                    SELECT @@ROWCOUNT;";

                using (var connection = await _connectionFactory.CreateConnectionAsync())
                {
                    // 업데이트 시간 설정
                    sensorGroup.UpdatedAt = DateTimeHelper.Now;

                    int rowsAffected = await connection.ExecuteScalarAsync<int>(sql, new
                    {
                        sensorGroup.GroupID,
                        sensorGroup.GroupName,
                        sensorGroup.Location,
                        sensorGroup.Description,
                        sensorGroup.UpdatedAt
                    });

                    if (rowsAffected == 0)
                        return null; // 업데이트할 그룹이 없는 경우

                    return sensorGroup;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "센서 그룹 수정 실패 - GroupId: {groupId}", sensorGroup.GroupID);
                throw;
            }
        }

        public async Task DeleteAsync(int groupId)
        {
            try
            {
                // 실제 삭제 대신 Active 플래그를 false로 설정 (소프트 삭제)
                string sql = @"
                    UPDATE SensorGroups 
                    SET Active = 0, 
                        UpdatedAt = @UpdatedAt
                    WHERE GroupID = @GroupID";

                using (var connection = await _connectionFactory.CreateConnectionAsync())
                {
                    await connection.ExecuteAsync(sql, new
                    {
                        GroupID = groupId,
                        UpdatedAt = DateTimeHelper.Now
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "센서 그룹 삭제 실패 - GroupId: {groupId}", groupId);
                throw;
            }
        }

        public async Task<bool> ExistsAsync(int groupId)
        {
            try
            {
                string sql = @"
                    SELECT COUNT(1) 
                    FROM SensorGroups 
                    WHERE GroupID = @GroupId AND Active = 1";

                using (var connection = await _connectionFactory.CreateConnectionAsync())
                {
                    int count = await connection.ExecuteScalarAsync<int>(sql, new { GroupId = groupId });
                    return count > 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "센서 그룹 존재 여부 확인 실패 - GroupId: {groupId}", groupId);
                throw;
            }
        }

        public async Task<int> GetCountByCompanyIdAsync(int companyId)
        {
            try
            {
                string sql = @"
                    SELECT COUNT(1) 
                    FROM SensorGroups 
                    WHERE CompanyID = @CompanyId AND Active = 1";

                using (var connection = await _connectionFactory.CreateConnectionAsync())
                {
                    return await connection.ExecuteScalarAsync<int>(sql, new { CompanyId = companyId });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "회사별 센서 그룹 개수 조회 실패 - CompanyId: {companyId}", companyId);
                throw;
            }
        }
    }
}