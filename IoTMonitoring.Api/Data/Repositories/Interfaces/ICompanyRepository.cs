// Data/Repositories/Interfaces/ICompanyRepository.cs
using System.Collections.Generic;
using System.Threading.Tasks;
using IoTMonitoring.Api.Data.Models;

namespace IoTMonitoring.Api.Data.Repositories.Interfaces
{
    public interface ICompanyRepository
    {
        Task<IEnumerable<Company>> GetAllAsync(bool includeInactive = false);
        Task<Company> GetByIdAsync(int companyId);
        Task<Company> GetByNameAsync(string name);
        Task<Company> GetByContactPersonAsync(string contactPerson);
        Task<int> CreateAsync(Company company);
        Task<bool> UpdateAsync(Company company);
        Task<bool> DeleteAsync(int companyId);
        Task<bool> ExistsAsync(int companyId);
        Task<bool> ExistsByNameAsync(string name, int? excludeCompanyId = null);
        Task<bool> ExistsByContactEmailAsync(string email, int? excludeCompanyId = null);
        Task<IEnumerable<Company>> GetCompaniesByIdsAsync(IEnumerable<int> companyIds);
        Task<int> GetUserCountByCompanyIdAsync(int companyId);
        Task<int> GetSensorCountByCompanyIdAsync(int companyId);
        Task<int> GetGroupCountByCompanyIdAsync(int companyId);
        Task<bool> ActivateAsync(int companyId);
        Task<bool> DeactivateAsync(int companyId);

        Task<IEnumerable<SensorGroup>> GetSensorGroupsByCompanyIdAsync(int companyId);
        Task<IEnumerable<User>> GetUsersByCompanyIdAsync(int companyId);
        Task<int> GetActiveSensorCountByCompanyIdAsync(int companyId);
    }
}