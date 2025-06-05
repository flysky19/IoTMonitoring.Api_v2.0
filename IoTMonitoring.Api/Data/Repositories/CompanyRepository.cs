using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using IoTMonitoring.Api.Data.Models;
using IoTMonitoring.Api.Data.Repositories.Interfaces;
using IoTMonitoring.Api.Data.Connection;

namespace IoTMonitoring.Api.Data.Repositories
{
    public class CompanyRepository : ICompanyRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public CompanyRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<IEnumerable<Company>> GetAllAsync(bool includeInactive = false)
        {
            using (var connection = await _connectionFactory.CreateConnectionAsync())
            {
                var sql = includeInactive
                    ? "SELECT * FROM Companies ORDER BY CompanyName"
                    : "SELECT * FROM Companies WHERE Active = 1 ORDER BY CompanyName";

                return await connection.QueryAsync<Company>(sql);
            }
        }

        public async Task<Company> GetByIdAsync(int companyId)
        {
            using (var connection = await _connectionFactory.CreateConnectionAsync())
            {
                return await connection.QuerySingleOrDefaultAsync<Company>(
                    "SELECT * FROM Companies WHERE CompanyID = @CompanyID",
                    new { CompanyID = companyId });
            }
        }

        public async Task<Company> GetByNameAsync(string name)
        {
            using (var connection = await _connectionFactory.CreateConnectionAsync())
            {
                return await connection.QuerySingleOrDefaultAsync<Company>(
                    "SELECT * FROM Companies WHERE CompanyName = @CompanyName",
                    new { CompanyName = name });
            }
        }

        public async Task<Company> GetByContactPersonAsync(string contactPerson)
        {
            using (var connection = await _connectionFactory.CreateConnectionAsync())
            {
                return await connection.QuerySingleOrDefaultAsync<Company>(
                    "SELECT * FROM Companies WHERE ContactPerson = @ContactPerson",
                    new { ContactPerson = contactPerson });
            }
        }

        public async Task<int> CreateAsync(Company company)
        {
            using (var connection = await _connectionFactory.CreateConnectionAsync())
            {
                var sql = @"
                    INSERT INTO Companies (
                        CompanyName, Address, ContactPerson, 
                        ContactPhone, ContactEmail, 
                        Active, CreatedAt, UpdatedAt
                    ) VALUES (
                        @CompanyName, @Address, @ContactPerson, 
                        @ContactPhone, @ContactEmail, 
                        @Active, GETUTCDATE(), GETUTCDATE()
                    );
                    SELECT CAST(SCOPE_IDENTITY() as int);";

                var companyId = await connection.QuerySingleAsync<int>(sql, company);
                return companyId;
            }
        }

        public async Task<bool> UpdateAsync(Company company)
        {
            using (var connection = await _connectionFactory.CreateConnectionAsync())
            {
                var sql = @"
                    UPDATE Companies 
                    SET CompanyName = @CompanyName,
                        Address = @Address,
                        ContactPerson = @ContactPerson,
                        ContactPhone = @ContactPhone,
                        ContactEmail = @ContactEmail,
                        Active = @Active,
                        UpdatedAt = GETUTCDATE()
                    WHERE CompanyID = @CompanyID";

                var rowsAffected = await connection.ExecuteAsync(sql, company);
                return rowsAffected > 0;
            }
        }

        public async Task<bool> DeleteAsync(int companyId)
        {
            using (var connection = await _connectionFactory.CreateConnectionAsync())
            {
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // 1. 관련 데이터 삭제 (예: 사용자-회사 관계)
                        await connection.ExecuteAsync(
                            "DELETE FROM UserCompanies WHERE CompanyID = @CompanyID",
                            new { CompanyID = companyId },
                            transaction);

                        // 2. 관련 센서 그룹 삭제 또는 연결 해제
                        await connection.ExecuteAsync(
                            "DELETE FROM SensorGroups WHERE CompanyID = @CompanyID",
                            new { CompanyID = companyId },
                            transaction);

                        // 3. 회사 삭제
                        var rowsAffected = await connection.ExecuteAsync(
                            "DELETE FROM Companies WHERE CompanyID = @CompanyID",
                            new { CompanyID = companyId },
                            transaction);

                        transaction.Commit();
                        return rowsAffected > 0;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public async Task<bool> ExistsAsync(int companyId)
        {
            using (var connection = await _connectionFactory.CreateConnectionAsync())
            {
                var count = await connection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(1) FROM Companies WHERE CompanyID = @CompanyID",
                    new { CompanyID = companyId });

                return count > 0;
            }
        }

        public async Task<bool> ExistsByNameAsync(string name, int? excludeCompanyId = null)
        {
            using (var connection = await _connectionFactory.CreateConnectionAsync())
            {
                var sql = "SELECT COUNT(1) FROM Companies WHERE CompanyName = @CompanyName";
                var parameters = new DynamicParameters();
                parameters.Add("CompanyName", name);

                if (excludeCompanyId.HasValue)
                {
                    sql += " AND CompanyID != @ExcludeCompanyId";
                    parameters.Add("ExcludeCompanyId", excludeCompanyId.Value);
                }

                var count = await connection.ExecuteScalarAsync<int>(sql, parameters);
                return count > 0;
            }
        }

        public async Task<bool> ExistsByContactEmailAsync(string email, int? excludeCompanyId = null)
        {
            using (var connection = await _connectionFactory.CreateConnectionAsync())
            {
                var sql = "SELECT COUNT(1) FROM Companies WHERE ContactEmail = @ContactEmail";
                var parameters = new DynamicParameters();
                parameters.Add("ContactEmail", email);

                if (excludeCompanyId.HasValue)
                {
                    sql += " AND CompanyID != @ExcludeCompanyId";
                    parameters.Add("ExcludeCompanyId", excludeCompanyId.Value);
                }

                var count = await connection.ExecuteScalarAsync<int>(sql, parameters);
                return count > 0;
            }
        }

        public async Task<IEnumerable<Company>> GetCompaniesByIdsAsync(IEnumerable<int> companyIds)
        {
            if (companyIds == null || !companyIds.Any())
                return new List<Company>();

            using (var connection = await _connectionFactory.CreateConnectionAsync())
            {
                return await connection.QueryAsync<Company>(
                    "SELECT * FROM Companies WHERE CompanyID IN @CompanyIds",
                    new { CompanyIds = companyIds });
            }
        }

        public async Task<int> GetUserCountByCompanyIdAsync(int companyId)
        {
            using (var connection = await _connectionFactory.CreateConnectionAsync())
            {
                return await connection.ExecuteScalarAsync<int>(
                    @"SELECT COUNT(1) 
                      FROM UserCompanies uc
                      INNER JOIN Users u ON uc.UserID = u.UserID
                      WHERE uc.CompanyID = @CompanyID AND u.IsActive = 1",
                    new { CompanyID = companyId });
            }
        }

        public async Task<int> GetSensorCountByCompanyIdAsync(int companyId)
        {
            using (var connection = await _connectionFactory.CreateConnectionAsync())
            {
                return await connection.ExecuteScalarAsync<int>(
                    @"SELECT COUNT(DISTINCT s.SensorID) 
                      FROM Sensors s
                      INNER JOIN SensorGroups sg ON s.GroupID = sg.GroupID
                      WHERE sg.CompanyID = @CompanyID AND s.Status = 'active'",
                    new { CompanyID = companyId });
            }
        }

        public async Task<int> GetGroupCountByCompanyIdAsync(int companyId)
        {
            using (var connection = await _connectionFactory.CreateConnectionAsync())
            {
                return await connection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(1) FROM SensorGroups WHERE CompanyID = @CompanyID AND Active = 1",
                    new { CompanyID = companyId });
            }
        }

        public async Task<bool> ActivateAsync(int companyId)
        {
            using (var connection = await _connectionFactory.CreateConnectionAsync())
            {
                var rowsAffected = await connection.ExecuteAsync(
                    @"UPDATE Companies 
                      SET Active = 1, UpdatedAt = GETUTCDATE() 
                      WHERE CompanyID = @CompanyID",
                    new { CompanyID = companyId });

                return rowsAffected > 0;
            }
        }

        public async Task<bool> DeactivateAsync(int companyId)
        {
            using (var connection = await _connectionFactory.CreateConnectionAsync())
            {
                var rowsAffected = await connection.ExecuteAsync(
                    @"UPDATE Companies 
                      SET Active = 0, UpdatedAt = GETUTCDATE() 
                      WHERE CompanyID = @CompanyID",
                    new { CompanyID = companyId });

                return rowsAffected > 0;
            }
        }

        public async Task<IEnumerable<SensorGroup>> GetSensorGroupsByCompanyIdAsync(int companyId)
        {
            using (var connection = await _connectionFactory.CreateConnectionAsync())
            {
                var sql = @"
            SELECT sg.*, COUNT(s.SensorID) as SensorCount
            FROM SensorGroups sg
            LEFT JOIN Sensors s ON sg.GroupID = s.GroupID
            WHERE sg.CompanyID = @CompanyID
            GROUP BY sg.GroupID, sg.GroupName, sg.Description, 
                     sg.CompanyID, sg.IsActive, sg.CreatedAt, sg.UpdatedAt
            ORDER BY sg.GroupName";

                var groups = await connection.QueryAsync<SensorGroup>(sql, new { CompanyID = companyId });
                return groups;
            }
        }

        public async Task<IEnumerable<User>> GetUsersByCompanyIdAsync(int companyId)
        {
            using (var connection = await _connectionFactory.CreateConnectionAsync())
            {
                var sql = @"
            SELECT u.*
            FROM Users u
            INNER JOIN UserCompanies uc ON u.UserID = uc.UserID
            WHERE uc.CompanyID = @CompanyID AND u.IsActive = 1
            ORDER BY u.Username";

                return await connection.QueryAsync<User>(sql, new { CompanyID = companyId });
            }
        }

        public async Task<int> GetActiveSensorCountByCompanyIdAsync(int companyId)
        {
            using (var connection = await _connectionFactory.CreateConnectionAsync())
            {
                var sql = @"
            SELECT COUNT(DISTINCT s.SensorID)
            FROM Sensors s
            INNER JOIN SensorGroups sg ON s.GroupID = sg.GroupID
            WHERE sg.CompanyID = @CompanyID 
                AND s.Status = 'active' 
                AND sg.IsActive = 1";

                return await connection.ExecuteScalarAsync<int>(sql, new { CompanyID = companyId });
            }
        }

        /// <summary>
        /// 특정 사용자가 접근 가능한 회사 목록 조회
        /// </summary>
        public async Task<IEnumerable<Company>> GetCompaniesByUserIdAsync(int userId)
        {
            using (var connection = await _connectionFactory.CreateConnectionAsync())
            {
                var sql = @"
                SELECT c.* 
                FROM Companies c
                INNER JOIN UserCompanies uc ON c.CompanyID = uc.CompanyID
                WHERE uc.UserID = @UserID AND c.Active = 1
                ORDER BY c.CompanyName";

                return await connection.QueryAsync<Company>(sql, new { UserID = userId });
            }
        }

        /// <summary>
        /// 사용자가 특정 회사에 접근 권한이 있는지 확인
        /// </summary>
        public async Task<bool> UserHasAccessToCompanyAsync(int userId, int companyId)
        {
            using (var connection = await _connectionFactory.CreateConnectionAsync())
            {
                var count = await connection.ExecuteScalarAsync<int>(
                    @"SELECT COUNT(1) 
                  FROM UserCompanies 
                  WHERE UserID = @UserID AND CompanyID = @CompanyID",
                    new { UserID = userId, CompanyID = companyId });

                return count > 0;
            }
        }
    }
}