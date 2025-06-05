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
    public class UserRepository : IUserRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public UserRepository(IDbConnectionFactory connectionFactory)
        {
             _connectionFactory = connectionFactory;
        }

        public async Task<User> GetByIdAsync(int userId)
        {
            using (var connection = await _connectionFactory.CreateConnectionAsync())
            {
                return await connection.QuerySingleOrDefaultAsync<User>(
                    "SELECT * FROM Users WHERE UserID = @UserID",
                    new { UserID = userId });
            }
        }

        public async Task<User> GetByUsernameAsync(string username)
        {
            using (var connection = await _connectionFactory.CreateConnectionAsync())
            {
                return await connection.QuerySingleOrDefaultAsync<User>(
                    "SELECT * FROM Users WHERE Username = @Username",
                    new { Username = username });
            }
        }

        public async Task<User> GetByEmailAsync(string email)
        {
            using (var connection = await _connectionFactory.CreateConnectionAsync())
            {
                return await connection.QuerySingleOrDefaultAsync<User>(
                    "SELECT * FROM Users WHERE Email = @Email",
                    new { Email = email });
            }
        }

        public async Task<bool> UpdateAsync(User user, List<int> companyIds = null)
        {
            //using (var connection = await _connectionFactory.CreateConnectionAsync())
            //{
            //    var rowsAffected = await connection.ExecuteAsync(@"
            //        UPDATE Users 
            //        SET Username = @Username, 
            //            Email = @Email, 
            //            FullName = @FullName, 
            //            Phone = @Phone,
            //            IsActive = @IsActive,
            //            UpdatedAt = @UpdatedAt,
            //            LastLogin = @LastLogin
            //        WHERE UserID = @UserID",
            //        user);

            //    return rowsAffected > 0;
            //}

            using (var connection = await _connectionFactory.CreateConnectionAsync())
            {
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // 1. Users 테이블 업데이트
                        var userUpdateSql = @"
                            UPDATE Users 
                            SET Username = @Username, 
                                Email = @Email, 
                                FullName = @FullName, 
                                Phone = @Phone,
                                Role = @Role,
                                IsActive = @IsActive,
                                UpdatedAt = @UpdatedAt
                            WHERE UserID = @UserID";

                        var rowsAffected = await connection.ExecuteAsync(
                            userUpdateSql,
                            user,
                            transaction);

                        if (rowsAffected == 0)
                        {
                            throw new KeyNotFoundException($"사용자 ID {user.UserID}를 찾을 수 없습니다.");
                        }

                        // 2. UserCompanies 테이블 업데이트 (companyIds가 제공된 경우에만)
                        if (companyIds != null)
                        {
                            // 기존 회사 할당 모두 제거
                            await connection.ExecuteAsync(
                                "DELETE FROM UserCompanies WHERE UserID = @UserID",
                                new { UserID = user.UserID },
                                transaction);

                            // 새로운 회사 할당 추가
                            if (companyIds.Any())
                            {
                                var insertSql = @"
                                    INSERT INTO UserCompanies (UserID, CompanyID, CreatedAt) 
                                    VALUES (@UserID, @CompanyID, GETUTCDATE())";

                                foreach (var companyId in companyIds)
                                {
                                    await connection.ExecuteAsync(
                                        insertSql,
                                        new { UserID = user.UserID, CompanyID = companyId },
                                        transaction);
                                }
                            }
                        }

                        transaction.Commit();
                        return true;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public async Task<IEnumerable<User>> GetAllAsync(bool includeInactive = false)
        {
            using (var connection = await _connectionFactory.CreateConnectionAsync())
            {
                var sql = includeInactive
                    ? "SELECT * FROM Users ORDER BY Username"
                    : "SELECT * FROM Users WHERE IsActive = 1 ORDER BY Username";

                return await connection.QueryAsync<User>(sql);
            }
        }

        public async Task<User> GetUserWithCompanyAsync(int userId)
        {
            
            using (var connection = await _connectionFactory.CreateConnectionAsync())
            {
                var sql = @"
                    SELECT * FROM Users WHERE UserID = @userId;
                    SELECT CompanyID FROM UserCompanies WHERE UserID = @userId;";

                using (var multi = await connection.QueryMultipleAsync(sql, new { userId }))
                {
                    var user = await multi.ReadSingleOrDefaultAsync<User>();
                    if (user != null)
                    {
                        var companyIds = await multi.ReadAsync<int>();
                        user.UserCompanies = companyIds.Select(id => new UserCompany
                        {
                            UserID = userId,
                            CompanyID = id
                        }).ToList();
                    }
                    return user;
                }
            }
        }

        public async Task<IEnumerable<User>> GetUsersWithCompanyAsync(bool includeInactive = false)
        {
            using (var connection = await _connectionFactory.CreateConnectionAsync())
            {
                var sql = @"
            SELECT u.*, c.CompanyID, c.CompanyName, c.Active as CompanyActive
            FROM Users u
            LEFT JOIN UserCompanies uc ON u.UserID = uc.UserID
            LEFT JOIN Companies c ON uc.CompanyID = c.CompanyID
            WHERE (@includeInactive = 1 OR u.IsActive = 1)
            ORDER BY u.UserID";

                var userDict = new Dictionary<int, User>();

                await connection.QueryAsync<User, dynamic, User>(
                    sql,
                    (user, company) =>
                    {
                        if (!userDict.TryGetValue(user.UserID, out var existingUser))
                        {
                            existingUser = user;
                            existingUser.UserCompanies = new List<UserCompany>();
                            existingUser.AssignedCompanies = new List<Company>();
                            userDict.Add(user.UserID, existingUser);
                        }

                        if (company?.CompanyID != null)
                        {
                            existingUser.UserCompanies.Add(new UserCompany
                            {
                                UserID = user.UserID,
                                CompanyID = company.CompanyID
                            });

                            existingUser.AssignedCompanies.Add(new Company
                            {
                                CompanyID = company.CompanyID,
                                CompanyName = company.CompanyName,
                                Active = company.CompanyActive
                            });
                        }

                        return existingUser;
                    },
                    new { includeInactive },
                    splitOn: "CompanyID"
                );

                return userDict.Values;
            }
        }

        public async Task<int> CreateAsync(User user, List<int> companyIds)
        {
            using (var connection = await _connectionFactory.CreateConnectionAsync())
            {
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // 1. 사용자 기본 정보 저장 (Role 포함)
                        var sql = @"
                    INSERT INTO Users (
                        Username, Password, Email, FullName, Phone, 
                        Role, IsActive, CreatedAt, UpdatedAt
                    ) VALUES (
                        @Username, @Password, @Email, @FullName, @Phone, 
                        @Role, @IsActive, @CreatedAt, @UpdatedAt
                    );
                    SELECT CAST(SCOPE_IDENTITY() as int);";

                        var userId = await connection.QuerySingleAsync<int>(sql, user, transaction);

                        // 2. 사용자-회사 관계만 저장
                        if (companyIds?.Any() == true)
                        {
                            var companySql = @"
                        INSERT INTO UserCompanies (UserID, CompanyID) 
                        VALUES (@UserID, @CompanyID)";

                            foreach (var companyId in companyIds)
                            {
                                await connection.ExecuteAsync(companySql,
                                    new { UserID = userId, CompanyID = companyId },
                                    transaction);
                            }
                        }

                        transaction.Commit();
                        return userId;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }


        //public async Task<bool> DeleteAsync(int userId)
        //{
        //    using (var connection = await _connectionFactory.CreateConnectionAsync())
        //    {
        //        var rowsAffected = await connection.ExecuteAsync(
        //            "DELETE FROM Users WHERE UserID = @UserID",
        //            new { UserID = userId });

        //        return rowsAffected > 0;
        //    }
        //}

        public async Task<bool> ExistsByUsernameAsync(string username, int? excludeUserId = null)
        {
            using (var connection = await _connectionFactory.CreateConnectionAsync())
            {
                var sql = "SELECT COUNT(1) FROM Users WHERE Username = @Username";
                var parameters = new DynamicParameters();
                parameters.Add("Username", username);

                if (excludeUserId.HasValue)
                {
                    sql += " AND UserID != @ExcludeUserId";
                    parameters.Add("ExcludeUserId", excludeUserId.Value);
                }

                var count = await connection.ExecuteScalarAsync<int>(sql, parameters);
                return count > 0;
            }
        }

        public async Task<bool> ExistsByEmailAsync(string email, int? excludeUserId = null)
        {
            using (var connection = await _connectionFactory.CreateConnectionAsync())
            {
                var sql = "SELECT COUNT(1) FROM Users WHERE Email = @Email";
                var parameters = new DynamicParameters();
                parameters.Add("Email", email);

                if (excludeUserId.HasValue)
                {
                    sql += " AND UserID != @ExcludeUserId";
                    parameters.Add("ExcludeUserId", excludeUserId.Value);
                }

                var count = await connection.ExecuteScalarAsync<int>(sql, parameters);
                return count > 0;
            }
        }

        public async Task<bool> UpdatePasswordAsync(int userId, string passwordHash)
        {
            using (var connection = await _connectionFactory.CreateConnectionAsync())
            {
                //var rowsAffected = await connection.ExecuteAsync(
                //    @"UPDATE Users 
                //      SET Password = @PasswordHash, UpdatedAt = GETUTCDATE() 
                //      WHERE UserID = @UserID",
                //    new { UserID = userId, Password = passwordHash });
                var rowsAffected = await connection.ExecuteAsync(
                       @"UPDATE Users 
                          SET Password = @Password, UpdatedAt = GETUTCDATE() 
                          WHERE UserID = @UserID",
                       new { UserID = userId, Password = passwordHash });
                return rowsAffected > 0;
            }
        }

        public async Task<bool> UpdateLastLoginAsync(int userId)
        {
            using (var connection = await _connectionFactory.CreateConnectionAsync())
            {
                var rowsAffected = await connection.ExecuteAsync(
                    "UPDATE Users SET LastLogin = GETUTCDATE() WHERE UserID = @UserID",
                    new { UserID = userId });

                return rowsAffected > 0;
            }
        }

        public async Task<IEnumerable<User>> GetUsersByCompanyIdAsync(int companyId)
        {
            using (var connection = await _connectionFactory.CreateConnectionAsync())
            {
                return await connection.QueryAsync<User>(
                    "SELECT * FROM Users WHERE CompanyID = @CompanyID AND IsActive = 1",
                    new { CompanyID = companyId });
            }
        }

        // 사용자에게 회사 할당
        public async Task<bool> AssignCompanyToUserAsync(int userId, int companyId)
        {
            using (var connection = await _connectionFactory.CreateConnectionAsync())
            {
                // 이미 할당되어 있는지 확인
                var exists = await connection.ExecuteScalarAsync<int>(
                    @"SELECT COUNT(1) FROM UserCompanies 
              WHERE UserID = @UserID AND CompanyID = @CompanyID",
                    new { UserID = userId, CompanyID = companyId });

                if (exists > 0)
                    return false; // 이미 할당됨

                var rowsAffected = await connection.ExecuteAsync(
                    @"INSERT INTO UserCompanies (UserID, CompanyID, CreatedAt) 
              VALUES (@UserID, @CompanyID, GETUTCDATE())",
                    new { UserID = userId, CompanyID = companyId });

                return rowsAffected > 0;
            }
        }

        // 사용자에서 회사 할당 제거
        public async Task<bool> RemoveCompanyFromUserAsync(int userId, int companyId)
        {
            using (var connection = await _connectionFactory.CreateConnectionAsync())
            {
                var rowsAffected = await connection.ExecuteAsync(
                    @"DELETE FROM UserCompanies 
              WHERE UserID = @UserID AND CompanyID = @CompanyID",
                    new { UserID = userId, CompanyID = companyId });

                return rowsAffected > 0;
            }
        }

        // 사용자의 모든 회사 할당 제거
        public async Task<bool> RemoveAllCompaniesFromUserAsync(int userId)
        {
            using (var connection = await _connectionFactory.CreateConnectionAsync())
            {
                var rowsAffected = await connection.ExecuteAsync(
                    "DELETE FROM UserCompanies WHERE UserID = @UserID",
                    new { UserID = userId });

                return rowsAffected > 0;
            }
        }

        // 사용자의 회사 목록 업데이트 (기존 것 모두 삭제 후 새로 추가)
        public async Task UpdateUserCompaniesAsync(int userId, List<int> companyIds)
        {
            using (var connection = await _connectionFactory.CreateConnectionAsync())
            {
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // 기존 할당 모두 제거
                        await connection.ExecuteAsync(
                            "DELETE FROM UserCompanies WHERE UserID = @UserID",
                            new { UserID = userId },
                            transaction);

                        // 새로운 할당 추가
                        if (companyIds != null && companyIds.Any())
                        {
                            var values = companyIds.Select(cid =>
                                $"({userId}, {cid}, GETUTCDATE())").ToList();

                            var sql = $@"INSERT INTO UserCompanies (UserID, CompanyID, CreatedAt) 
                                VALUES {string.Join(", ", values)}";

                            await connection.ExecuteAsync(sql, transaction: transaction);
                        }

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public async Task<bool> DeleteAsync(int userId)
        {
            using (var connection = await _connectionFactory.CreateConnectionAsync())
            {
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // 1. UserCompanies 관계 먼저 삭제
                        var deleteCompaniesSql = @"
                                DELETE FROM UserCompanies 
                                WHERE UserID = @UserID";

                        await connection.ExecuteAsync(deleteCompaniesSql, new { UserID = userId }, transaction);

                        // 2. 사용자 삭제
                        var deleteUserSql = @"
                                DELETE FROM Users 
                                WHERE UserID = @UserID";

                        var affectedRows = await connection.ExecuteAsync(deleteUserSql, new { UserID = userId }, transaction);

                        if (affectedRows == 0)
                        {
                            throw new KeyNotFoundException($"사용자 ID {userId}를 찾을 수 없습니다.");
                        }

                        transaction.Commit();

                        return affectedRows > 0;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                    finally
                    {
                        
                    }
                }
            }
        }
    }
}