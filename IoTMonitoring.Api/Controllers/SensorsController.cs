using Microsoft.AspNetCore.Mvc;
using IoTMonitoring.Api.DTOs;
using IoTMonitoring.Api.Services.Interfaces;
using IoTMonitoring.Api.Services.Sensor.Interfaces;
using Microsoft.AspNetCore.Authorization;
using IoTMonitoring.Api.Services.Logging.Interfaces;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace IoTMonitoring.Api.Controllers
{
    [Route("api/sensors")]
    [ApiController]
    [Authorize]
    public class SensorsController : ControllerBase
    {
        private readonly ISensorService _sensorService;
        private readonly ILogger<SensorsController> _logger;

        public SensorsController(ISensorService sensorService, ILogger<SensorsController> logger)
        {
            _sensorService = sensorService;
            _logger = logger;
        }

        /// <summary>
        /// 센서 목록 조회 (필터링 지원)
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SensorDto>>> GetAllSensors(
            [FromQuery] int? groupId = null,
            [FromQuery] string status = null,
            [FromQuery] string connectionStatus = null)
        {
            try
            {
                _logger.LogInformation($"센서 목록 조회 요청 - GroupId: {groupId}, Status: {status}, ConnectionStatus: {connectionStatus}");

                var sensors = await _sensorService.GetAllSensorsAsync(groupId, status, connectionStatus);

                _logger.LogInformation($"센서 목록 조회 완료 - 조회된 센서 수: {sensors?.Count() ?? 0}");

                return Ok(sensors);
            }
            catch (Exception ex)
            {
                _logger.LogError($"센서 목록 조회 중 오류: {ex.Message}");
                return StatusCode(500, new { message = "센서 목록 조회 중 오류가 발생했습니다." });
            }
        }


        [HttpGet("{id}")]
        public async Task<ActionResult<SensorDetailDto>> GetSensorDetail(int id)
        {
            try
            {
                _logger.LogInformation($"센서 상세 조회 요청 - ID: {id}");

                var sensor = await _sensorService.GetSensorDetailAsync(id);

                _logger.LogInformation($"센서 상세 조회 완료 - ID: {id}, Name: {sensor.Name}");
                return Ok(sensor);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning($"센서를 찾을 수 없음 - ID: {id}, Message: {ex.Message}");
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"센서 조회 중 오류 (ID: {id}): {ex.Message}", ex);
                return StatusCode(500, new { message = "센서 조회 중 오류가 발생했습니다." });
            }
        }

        [HttpGet("{id}/data")]
        public async Task<ActionResult<IEnumerable<dynamic>>> GetSensorData(
            int id,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int? limit = 1000,
            [FromQuery] string aggregationType = "Raw")
        {
            try
            {
                // 입력 검증
                if (limit <= 0 || limit > 10000)
                {
                    return BadRequest(new { message = "Limit은 1 이상 10000 이하여야 합니다." });
                }

                if (startDate.HasValue && endDate.HasValue && startDate > endDate)
                {
                    return BadRequest(new { message = "시작 날짜는 종료 날짜보다 이전이어야 합니다." });
                }

                _logger.LogInformation($"센서 데이터 조회 요청 - ID: {id}, StartDate: {startDate}, EndDate: {endDate}, Limit: {limit}");


                var request = new SensorDataRequestDto
                {
                    StartDate = startDate ?? DateTime.Now.AddDays(-1), // 기본: 1일 전부터
                    EndDate = endDate ?? DateTime.Now, // 기본: 현재까지
                    Limit = limit,
                    AggregationType = aggregationType
                };

                var data = await _sensorService.GetSensorDataAsync(id, request);

                _logger.LogInformation($"센서 데이터 조회 완료 - ID: {id}, 조회된 레코드 수: {data?.Count() ?? 0}");

                return Ok(data);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning($"센서를 찾을 수 없음 - ID: {id}, Message: {ex.Message}");
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning($"잘못된 요청 - ID: {id}, Message: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"센서 데이터 조회 중 오류 (ID: {id}): {ex.Message}", ex);
                return StatusCode(500, new { message = "센서 데이터 조회 중 오류가 발생했습니다." });
            }
        }

        /// <summary>
        /// 새 센서 등록
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<SensorDetailDto>> CreateSensor([FromBody] SensorCreateDto request)
        {
            try
            {
                _logger.LogInformation($"센서 생성 요청 - UUID: {request.SensorUUID}, Name: {request.Name}");

                var sensor = await _sensorService.CreateSensorAsync(request);

                _logger.LogInformation($"센서 생성 완료 - ID: {sensor.SensorID}, UUID: {sensor.SensorUUID}");
                return CreatedAtAction(nameof(GetSensorDetail), new { id = sensor.SensorID }, sensor);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning($"센서 생성 실패 - UUID: {request.SensorUUID}, Message: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"센서 생성 중 오류: {ex.Message}", ex);
                return StatusCode(500, new { message = "센서 생성 중 오류가 발생했습니다." });
            }
        }

        /// <summary>
        /// 센서의 그룹 변경
        /// </summary>
        [HttpPatch("{id}/group")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> UpdateSensorGroup(int id, [FromBody] UpdateSensorGroupDto request)
        {
            try
            {
                var groupId = request?.GroupID ?? 0;
                await _sensorService.UpdateSensorGroupIdAsync(id, groupId);

                return Ok(new { message = "센서 그룹이 변경되었습니다." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"센서 그룹 변경 중 오류 (ID: {id}): {ex.Message}");
                return StatusCode(500, new { message = "센서 그룹 변경 중 오류가 발생했습니다." });
            }
        }

        /// <summary>
        /// 센서 하트비트 업데이트
        /// </summary>
        [HttpPost("{id}/heartbeat")]
        public async Task<ActionResult> UpdateHeartbeat(int id)
        {
            try
            {
                _logger.LogInformation($"센서 하트비트 업데이트 요청 - ID: {id}");

                await _sensorService.UpdateSensorHeartbeatAsync(id);

                _logger.LogInformation($"센서 하트비트 업데이트 완료 - ID: {id}");
                return Ok(new { message = "하트비트가 업데이트되었습니다." });
            }
            catch (Exception ex)
            {
                _logger.LogError($"센서 하트비트 업데이트 중 오류 (ID: {id}): {ex.Message}", ex);
                return StatusCode(500, new { message = "하트비트 업데이트 중 오류가 발생했습니다." });
            }
        }

        /// <summary>
        /// 센서 정보 수정
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<SensorDetailDto>> UpdateSensor(int id, [FromBody] SensorUpdateDto request)
        {
            try
            {
                _logger.LogInformation($"센서 수정 요청 - ID: {id}");

                var sensor = await _sensorService.UpdateSensorAsync(id, request);

                _logger.LogInformation($"센서 수정 완료 - ID: {id}");
                return Ok(sensor);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning($"센서를 찾을 수 없음 - ID: {id}, Message: {ex.Message}");
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning($"센서 수정 실패 - ID: {id}, Message: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"센서 수정 중 오류 (ID: {id}): {ex.Message}", ex);
                return StatusCode(500, new { message = "센서 수정 중 오류가 발생했습니다." });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteSensor(int id)
        {
            try
            {
                await _sensorService.DeleteSensorAsync(id);

                return NoContent(); // 204 No Content
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "센서 삭제 중 오류가 발생했습니다.");
            }
        }

        [HttpPut("{id}/activate")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> ActivateSensor(int id)
        {
            try
            {
                await _sensorService.ActivateSensorAsync(id);

                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"센서 비활성화 중 오류 (ID: {id}): {ex.Message}");
                return StatusCode(500, new { message = "센서 비활성화 중 오류가 발생했습니다." });
            }
        }

        /// <summary>
        /// 센서 비활성화 (삭제 대신)
        /// </summary>
        [HttpPut("{id}/deactivate")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> DeactivateSensor(int id)
        {
            try
            {
                await _sensorService.DeactivateSensorAsync(id);
                
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"센서 비활성화 중 오류 (ID: {id}): {ex.Message}");
                return StatusCode(500, new { message = "센서 비활성화 중 오류가 발생했습니다." });
            }
        }

        /// <summary>
        /// 센서 MQTT 토픽 조회
        /// </summary>
        [HttpGet("{id}/mqtt-topics")]
        public async Task<ActionResult<SensorMqttTopicDto>> GetSensorMqttTopics(int id)
        {
            try
            {
                var topics = await _sensorService.GetSensorMqttTopicsAsync(id);
                return Ok(topics);
            }
            catch (Exception ex)
            {
                _logger.LogError($"센서 MQTT 토픽 조회 중 오류 (ID: {id}): {ex.Message}");
                return StatusCode(500, new { message = "MQTT 토픽 조회 중 오류가 발생했습니다." });
            }
        }
    }
}