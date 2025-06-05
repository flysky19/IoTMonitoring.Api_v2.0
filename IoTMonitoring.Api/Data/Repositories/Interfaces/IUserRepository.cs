using System.Threading.Tasks;
using IoTMonitoring.Api.Data.Models;

namespace IoTMonitoring.Api.Data.Repositories.Interfaces
{
    public interface IUserRepository
    {
        Task<User> GetByIdAsync(int userId);
        Task<User> GetByUsernameAsync(string username);
        Task<User> GetByEmailAsync(string email);
        Task<bool> UpdateAsync(User user, List<int> companyIds = null);


        // 추가 메서드들
        Task<IEnumerable<User>> GetAllAsync(bool includeInactive = false);
        Task<User> GetUserWithCompanyAsync(int userId);
        Task<IEnumerable<User>> GetUsersWithCompanyAsync(bool includeInactive = false);
        //Task<int> CreateAsync(User user);
        Task<int> CreateAsync(User user, List<int> companyIds);
        Task<bool> DeleteAsync(int userId);
        Task<bool> ExistsByUsernameAsync(string username, int? excludeUserId = null);
        Task<bool> ExistsByEmailAsync(string email, int? excludeUserId = null);
        Task<bool> UpdatePasswordAsync(int userId, string passwordHash);
        Task<bool> UpdateLastLoginAsync(int userId);
        Task<IEnumerable<User>> GetUsersByCompanyIdAsync(int companyId);

        Task<bool> AssignCompanyToUserAsync(int userId, int companyId);
        Task<bool> RemoveCompanyFromUserAsync(int userId, int companyId);
        Task<bool> RemoveAllCompaniesFromUserAsync(int userId);
        Task UpdateUserCompaniesAsync(int userId, List<int> companyIds);
    }
}