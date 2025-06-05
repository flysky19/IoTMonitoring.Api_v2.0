// Controllers/CompaniesController.cs
using Microsoft.AspNetCore.Mvc;
using IoTMonitoring.Api.DTOs;
using IoTMonitoring.Api.Services.CompanySvc.Interfaces;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace IoTMonitoring.Api.Controllers
{
    [ApiController]
    [Route("api/companies")]
    [Authorize] // 인증 필요
    public class CompaniesController : ControllerBase
    {
        private readonly ICompanyService _companyService;
        private readonly ILogger<CompaniesController> _logger;

        public CompaniesController(ICompanyService companyService, ILogger<CompaniesController> logger)
        {
            _companyService = companyService;
            _logger = logger;
        }

        // 모든 업체 조회
        [HttpGet]
        [Authorize(Roles = "Admin,User")] // Admin과 User 모두 조회 가능
        //[AllowAnonymous]
        public async Task<ActionResult<IEnumerable<CompanyDto>>> GetCompanies([FromQuery] bool includeInactive = false)
        {
            var companies = await _companyService.GetAllCompaniesAsync(includeInactive);
            return Ok(companies);
        }

        // 업체 상세 정보 조회
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,User")]
        //[AllowAnonymous]
        public async Task<ActionResult<CompanyDetailDto>> GetCompany(int id)
        {
            var company = await _companyService.GetCompanyByIdAsync(id);
            if (company == null)
                return NotFound();

            return Ok(company);
        }

        // 업체 추가
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<CompanyDto>> CreateCompany([FromBody] CompanyCreateDto companyDto)
        {
            try
            {
                var createdCompany = await _companyService.CreateCompanyAsync(companyDto);
                return CreatedAtAction(nameof(GetCompany), new { id = createdCompany.CompanyID }, createdCompany);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // 업체 정보 수정
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> UpdateCompany(int id, [FromBody] CompanyUpdateDto companyDto)
        {
            try
            {
                await _companyService.UpdateCompanyAsync(id, companyDto);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"Company with ID {id} not found");
            }
        }

        // 회사 삭제 (관리자 전용)
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> DeleteCompany(int id)
        {
            try
            {
                await _companyService.DeleteCompanyAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"Company with ID {id} not found");
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // 회사 활성화 (관리자 전용)
        [HttpPost("{id}/activate")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> ActivateCompany(int id)
        {
            try
            {
                await _companyService.ActivateCompanyAsync(id);
                return Ok(new { message = "Company activated successfully" });
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"Company with ID {id} not found");
            }
        }

        // 업체 비활성화
        [HttpPost("{id}/deactivate")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> DeactivateCompany(int id)
        {
            try
            {
                await _companyService.DeactivateCompanyAsync(id);
                return Ok(new { message = "Company deactivated successfully" });
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"Company with ID {id} not found");
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // 업체의 센서 그룹 목록 조회
        [HttpGet("{id}/sensor-groups")]
        public async Task<ActionResult<IEnumerable<SensorGroupDto>>> GetCompanySensorGroups(int id)
        {
            try
            {
                var groups = await _companyService.GetCompanySensorGroupsAsync(id);
                return Ok(groups);
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"Company with ID {id} not found");
            }
        }

        /// <summary>
        /// 현재 사용자가 속한 회사 목록 조회
        /// </summary>
        [HttpGet("users")]
        public async Task<IActionResult> GetUserCompanies()
        {
            try
            {
                var userIdClaim = User.FindFirst("UserId") ?? User.FindFirst(ClaimTypes.NameIdentifier);
                
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized("사용자 정보를 찾을 수 없습니다.");
                }

                var companies = await _companyService.GetCompaniesByUserIdAsync(userId);

                return Ok(companies);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "사용자 회사 목록 조회 중 오류 발생");
                return StatusCode(500, new { message = "서버 오류가 발생했습니다." });
            }
        }

        /// <summary>
        /// 모든 회사 목록 조회 (관리자용)
        /// </summary>
        [HttpGet("all")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllCompanies()
        {
            try
            {
                var companies = await _companyService.GetAllCompaniesAsync();
                return Ok(companies);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "전체 회사 목록 조회 중 오류 발생");
                return StatusCode(500, new { message = "서버 오류가 발생했습니다." });
            }
        }
    }
}