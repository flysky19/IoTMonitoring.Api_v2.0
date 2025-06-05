using IoTMonitoring.Api.Data.Models;
using IoTMonitoring.Api.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IoTMonitoring.Api.Services.CompanySvc.Interfaces
{
    public interface ICompanyService
    {
        // 회사 기본 CRUD 작업
        Task<IEnumerable<CompanyDto>> GetAllCompaniesAsync(bool includeInactive = false);
        Task<CompanyDetailDto> GetCompanyByIdAsync(int id);
        Task<CompanyDto> CreateCompanyAsync(CompanyCreateDto companyDto);
        Task UpdateCompanyAsync(int id, CompanyUpdateDto companyDto);
        Task DeactivateCompanyAsync(int id);
        Task DeleteCompanyAsync(int id);

        Task ActivateCompanyAsync(int id);
        
        // 회사 센서 그룹 관리
        Task<IEnumerable<SensorGroupDto>> GetCompanySensorGroupsAsync(int companyId);

        // 회사 사용자 관리
        Task<IEnumerable<UserDto>> GetCompanyUsersAsync(int companyId);
        Task<int> GetSensorCountByCompanyAsync(int companyId, bool activeOnly = true);

        Task<List<CompanyDto>> GetCompaniesByUserIdAsync(int userId);
        Task<List<CompanyDto>> GetAllCompaniesAsync();

    }
}
