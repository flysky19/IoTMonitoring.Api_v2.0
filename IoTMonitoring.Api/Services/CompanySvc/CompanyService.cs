using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IoTMonitoring.Api.Data.Models;
using IoTMonitoring.Api.Data.Repositories.Interfaces;
using IoTMonitoring.Api.Services.CompanySvc.Interfaces;
using IoTMonitoring.Api.DTOs;

namespace IoTMonitoring.Api.Services.CompanySvc
{
    public class CompanyService : ICompanyService
    {
        private readonly ICompanyRepository _companyRepository;

        public CompanyService(ICompanyRepository companyRepository)
        {
            _companyRepository = companyRepository;
        }

        public async Task<IEnumerable<CompanyDto>> GetAllCompaniesAsync(bool includeInactive = false)
        {
            var companies = await _companyRepository.GetAllAsync(includeInactive);
            var companyDtos = new List<CompanyDto>();

            foreach (var company in companies)
            {
                var dto = await MapToCompanyDtoAsync(company);
                companyDtos.Add(dto);
            }

            return companyDtos;
        }

        public async Task<CompanyDto> GetCompanyByIdAsync(int companyId)
        {
            var company = await _companyRepository.GetByIdAsync(companyId);
            if (company == null)
                return null;

            return await MapToCompanyDtoAsync(company);
        }

        public async Task<CompanyDetailDto> GetCompanyDetailAsync(int companyId)
        {
            var company = await _companyRepository.GetByIdAsync(companyId);
            if (company == null)
                return null;

            return new CompanyDetailDto
            {
                CompanyID = company.CompanyID,
                CompanyName = company.CompanyName,
                Address = company.Address,
                ContactPerson = company.ContactPerson,
                ContactPhone = company.ContactPhone,
                ContactEmail = company.ContactEmail,
                Active = company.Active,
                CreatedAt = company.CreatedAt,
                UpdatedAt = company.UpdatedAt,
                UserCount = await _companyRepository.GetUserCountByCompanyIdAsync(companyId),
                SensorCount = await _companyRepository.GetSensorCountByCompanyIdAsync(companyId),
                GroupCount = await _companyRepository.GetGroupCountByCompanyIdAsync(companyId)
            };
        }

        public async Task<CompanyDto> CreateCompanyAsync(CompanyCreateDto companyDto)
        {
            // 유효성 검사
            if (string.IsNullOrWhiteSpace(companyDto.CompanyName))
                throw new ArgumentException("Company name is required");

            // 중복 검사
            if (await _companyRepository.ExistsByNameAsync(companyDto.CompanyName))
                throw new ArgumentException($"Company with name '{companyDto.CompanyName}' already exists");

            if (!string.IsNullOrWhiteSpace(companyDto.ContactEmail))
            {
                if (await _companyRepository.ExistsByContactEmailAsync(companyDto.ContactEmail))
                    throw new ArgumentException($"Company with email '{companyDto.ContactEmail}' already exists");
            }

            var company = new Company
            {
                CompanyName = companyDto.CompanyName.Trim(),
                Address = companyDto.Address?.Trim(),
                ContactPerson = companyDto.ContactPerson?.Trim(),
                ContactPhone = companyDto.ContactPhone?.Trim(),
                ContactEmail = companyDto.ContactEmail?.Trim(),
                Active = companyDto.Active ? true : false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var companyId = await _companyRepository.CreateAsync(company);
            company.CompanyID = companyId;

            return await MapToCompanyDtoAsync(company);
        }

        public async Task UpdateCompanyAsync(int companyId, CompanyUpdateDto companyDto)
        {
            var company = await _companyRepository.GetByIdAsync(companyId);
            if (company == null)
                throw new KeyNotFoundException($"Company with ID {companyId} not found");

            // 유효성 검사
            if (string.IsNullOrWhiteSpace(companyDto.CompanyName))
                throw new ArgumentException("Company name is required");

            // 중복 검사 (자기 자신 제외)
            if (await _companyRepository.ExistsByNameAsync(companyDto.CompanyName, companyId))
                throw new ArgumentException($"Company with name '{companyDto.CompanyName}' already exists");

            if (!string.IsNullOrWhiteSpace(companyDto.ContactEmail))
            {
                if (await _companyRepository.ExistsByContactEmailAsync(companyDto.ContactEmail, companyId))
                    throw new ArgumentException($"Company with email '{companyDto.ContactEmail}' already exists");
            }

            // 업데이트
            company.CompanyName = companyDto.CompanyName.Trim();
            company.Address = companyDto.Address?.Trim();
            company.ContactPerson = companyDto.ContactPerson?.Trim();
            company.ContactPhone = companyDto.ContactPhone?.Trim();
            company.ContactEmail = companyDto.ContactEmail?.Trim();
            company.Active = companyDto.Active ? true: false;
            company.UpdatedAt = DateTime.UtcNow;

            await _companyRepository.UpdateAsync(company);
        }

        public async Task DeleteCompanyAsync(int companyId)
        {
            var company = await _companyRepository.GetByIdAsync(companyId);
            if (company == null)
                throw new KeyNotFoundException($"Company with ID {companyId} not found");

            // 회사에 속한 사용자가 있는지 확인
            var userCount = await _companyRepository.GetUserCountByCompanyIdAsync(companyId);
            if (userCount > 0)
                throw new InvalidOperationException($"Cannot delete company with {userCount} active users");

            // 회사에 속한 센서가 있는지 확인
            var sensorCount = await _companyRepository.GetSensorCountByCompanyIdAsync(companyId);
            if (sensorCount > 0)
                throw new InvalidOperationException($"Cannot delete company with {sensorCount} active sensors");

            await _companyRepository.DeleteAsync(companyId);
        }

        public async Task<bool> ExistsAsync(int companyId)
        {
            return await _companyRepository.ExistsAsync(companyId);
        }

        public async Task ActivateCompanyAsync(int companyId)
        {
            var company = await _companyRepository.GetByIdAsync(companyId);
            if (company == null)
                throw new KeyNotFoundException($"Company with ID {companyId} not found");

            if (company.Active)
                return; // 이미 활성 상태

            await _companyRepository.ActivateAsync(companyId);
        }

        public async Task DeactivateCompanyAsync(int companyId)
        {
            var company = await _companyRepository.GetByIdAsync(companyId);
            if (company == null)
                throw new KeyNotFoundException($"Company with ID {companyId} not found");

            if (!company.Active)
                return; // 이미 비활성 상태

            // 활성 사용자가 있는지 확인
            var userCount = await _companyRepository.GetUserCountByCompanyIdAsync(companyId);
            if (userCount > 0)
                throw new InvalidOperationException($"Cannot deactivate company with {userCount} active users");

            await _companyRepository.DeactivateAsync(companyId);
        }

        public async Task<IEnumerable<CompanyDto>> GetCompaniesByIdsAsync(IEnumerable<int> companyIds)
        {
            if (companyIds == null || !companyIds.Any())
                return new List<CompanyDto>();

            var companies = await _companyRepository.GetCompaniesByIdsAsync(companyIds);
            var companyDtos = new List<CompanyDto>();

            foreach (var company in companies)
            {
                var dto = await MapToCompanyDtoAsync(company);
                companyDtos.Add(dto);
            }

            return companyDtos;
        }

        public async Task<bool> IsCompanyActiveAsync(int companyId)
        {
            var company = await _companyRepository.GetByIdAsync(companyId);
            return company?.Active ?? false;
        }

        private async Task<CompanyDto> MapToCompanyDtoAsync(Company company)
        {
            return new CompanyDto
            {
                CompanyID = company.CompanyID,
                CompanyName = company.CompanyName,
                Address = company.Address,
                ContactPerson = company.ContactPerson,
                ContactPhone = company.ContactPhone,
                ContactEmail = company.ContactEmail,
                Active = company.Active,
                UserCount = await _companyRepository.GetUserCountByCompanyIdAsync(company.CompanyID),
                SensorCount = await _companyRepository.GetSensorCountByCompanyIdAsync(company.CompanyID),
                GroupCount = await _companyRepository.GetGroupCountByCompanyIdAsync(company.CompanyID)
            };
        }

        Task<CompanyDetailDto> ICompanyService.GetCompanyByIdAsync(int id)
        {
            return GetCompanyDetailAsync(id);
        }

        public async Task<IEnumerable<SensorGroupDto>> GetCompanySensorGroupsAsync(int companyId)
        {
            // 회사 존재 여부 확인
            var company = await _companyRepository.GetByIdAsync(companyId);
            if (company == null)
                throw new KeyNotFoundException($"Company with ID {companyId} not found");

            // Repository에서 센서 그룹 가져오기
            var sensorGroups = await _companyRepository.GetSensorGroupsByCompanyIdAsync(companyId);

            // DTO로 변환
            var sensorGroupDtos = sensorGroups.Select(sg => new SensorGroupDetailDto
            {
                GroupID = sg.GroupID,
                GroupName = sg.GroupName,
                Description = sg.Description,
                CompanyID = sg.CompanyID,
                CompanyName = company.CompanyName,
                IsActive = sg.Active,
                CreatedAt = sg.CreatedAt,
                UpdatedAt = sg.UpdatedAt,
                SensorCount = sg.Sensors?.Count ?? 0
            }).ToList();

            return sensorGroupDtos;
        }

        public async Task<IEnumerable<UserDto>> GetCompanyUsersAsync(int companyId)
        {
            // 회사 존재 여부 확인
            var company = await _companyRepository.GetByIdAsync(companyId);
            if (company == null)
                throw new KeyNotFoundException($"Company with ID {companyId} not found");

            // Repository에서 사용자 가져오기
            var users = await _companyRepository.GetUsersByCompanyIdAsync(companyId);

            // DTO로 변환
            var userDtos = users.Select(u => new UserDto
            {
                UserID = u.UserID,
                Username = u.Username,
                Email = u.Email,
                FullName = u.FullName,
                Phone = u.Phone,
                Role = u.Role,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt,
                LastLogin = u.LastLogin,
                CompanyIDs = u.UserCompanies?.Select(uc => uc.CompanyID).ToList() ?? new List<int>(),
            }).ToList();

            return userDtos;
        }

        public async Task<int> GetSensorCountByCompanyAsync(int companyId, bool activeOnly = true)
        {
            var exists = await _companyRepository.ExistsAsync(companyId);
            if (!exists)
                throw new KeyNotFoundException($"Company with ID {companyId} not found");

            // Repository에서 센서 개수 가져오기
            if (activeOnly)
            {
                return await _companyRepository.GetActiveSensorCountByCompanyIdAsync(companyId);
            }
            else
            {
                return await _companyRepository.GetSensorCountByCompanyIdAsync(companyId);
            }
        }
    }
}