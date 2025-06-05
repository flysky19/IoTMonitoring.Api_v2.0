using Microsoft.AspNetCore.Mvc;
using IoTMonitoring.Api.DTOs;
using IoTMonitoring.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using IoTMonitoring.Api.Services.SensorGroup.Interfaces;

namespace IoTMonitoring.Api.Controllers
{
    [Route("api/sensor-groups")]
    [ApiController]
    [Authorize]
    public class SensorGroupsController : ControllerBase
    {
        private readonly ISensorGroupService _sensorGroupService;
        private readonly ILogger<SensorGroupsController> _logger;

        public SensorGroupsController(ISensorGroupService sensorGroupService, ILogger<SensorGroupsController> logger)
        {
            _sensorGroupService = sensorGroupService;
            _logger = logger;
        }

        /// <summary>
        /// 센서 그룹 목록 조회
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SensorGroupDto>>> GetAllSensorGroups([FromQuery] int? companyId = null)
        {
            try
            {
                _logger.LogInformation($"센서 그룹 목록 조회 요청 - CompanyId: {companyId}");

                var groups = await _sensorGroupService.GetAllSensorGroupsAsync(companyId);

                _logger.LogInformation($"센서 그룹 목록 조회 완료 - 조회된 그룹 수: {groups?.Count() ?? 0}");

                return Ok(groups);
            }
            catch (Exception ex)
            {
                _logger.LogError($"센서 그룹 목록 조회 중 오류: {ex.Message}");
                return StatusCode(500, new { message = "센서 그룹 목록 조회 중 오류가 발생했습니다." });
            }
        }

        /// <summary>
        /// 센서 그룹 상세 조회
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<SensorGroupDetailDto>> GetSensorGroup(int id)
        {
            try
            {
                _logger.LogInformation($"센서 그룹 상세 조회 요청 - ID: {id}");

                var group = await _sensorGroupService.GetSensorGroupDetailAsync(id);

                _logger.LogInformation($"센서 그룹 상세 조회 완료 - ID: {id}, Name: {group.GroupName}");
                return Ok(group);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning($"센서 그룹을 찾을 수 없음 - ID: {id}, Message: {ex.Message}");
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"센서 그룹 조회 중 오류 (ID: {id}): {ex.Message}", ex);
                return StatusCode(500, new { message = "센서 그룹 조회 중 오류가 발생했습니다." });
            }
        }

        /// <summary>
        /// 새 센서 그룹 생성
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<SensorGroupDetailDto>> CreateSensorGroup([FromBody] SensorGroupCreateDto request)
        {
            try
            {
                _logger.LogInformation($"센서 그룹 생성 요청 - Name: {request.GroupName}");

                var group = await _sensorGroupService.CreateSensorGroupAsync(request);

                _logger.LogInformation($"센서 그룹 생성 완료 - ID: {group.GroupID}, Name: {group.GroupName}");
                return CreatedAtAction(nameof(GetSensorGroup), new { id = group.GroupID }, group);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning($"센서 그룹 생성 실패 - Name: {request.GroupName}, Message: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"센서 그룹 생성 중 오류: {ex.Message}", ex);
                return StatusCode(500, new { message = "센서 그룹 생성 중 오류가 발생했습니다." });
            }
        }

        /// <summary>
        /// 센서 그룹 수정
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<SensorGroupDetailDto>> UpdateSensorGroup(int id, [FromBody] SensorGroupUpdateDto request)
        {
            try
            {
                _logger.LogInformation($"센서 그룹 수정 요청 - ID: {id}");

                var group = await _sensorGroupService.UpdateSensorGroupAsync(id, request);

                _logger.LogInformation($"센서 그룹 수정 완료 - ID: {id}");
                return Ok(group);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning($"센서 그룹을 찾을 수 없음 - ID: {id}, Message: {ex.Message}");
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"센서 그룹 수정 중 오류 (ID: {id}): {ex.Message}");
                return StatusCode(500, new { message = "센서 그룹 수정 중 오류가 발생했습니다." });
            }
        }

        /// <summary>
        /// 센서 그룹 삭제
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> DeleteSensorGroup(int id)
        {
            try
            {
                await _sensorGroupService.DeleteSensorGroupAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"센서 그룹 삭제 중 오류 (ID: {id}): {ex.Message}");
                return StatusCode(500, new { message = "센서 그룹 삭제 중 오류가 발생했습니다." });
            }
        }

        /// <summary>
        /// 그룹 내 센서 목록 조회
        /// </summary>
        [HttpGet("{id}/sensors")]

        public async Task<ActionResult<IEnumerable<SensorDto>>> GetGroupSensors(int id)
        {
            try
            {
                var sensors = await _sensorGroupService.GetGroupSensorsAsync(id);
                return Ok(sensors);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"그룹 센서 목록 조회 중 오류 (ID: {id}): {ex.Message}");
                return StatusCode(500, new { message = "그룹 센서 목록 조회 중 오류가 발생했습니다." });
            }
        }
    }
}