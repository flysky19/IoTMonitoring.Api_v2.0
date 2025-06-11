// Controllers/SpeakersController.cs
using Microsoft.AspNetCore.Mvc;
using IoTMonitoring.Api.DTOs;
using IoTMonitoring.Api.Services.Interfaces;
using System.Threading.Tasks;
using IoTMonitoring.Api.Utilities;

namespace IoTMonitoring.Api.Controllers
{
    [ApiController]
    [Route("api/speakers")]
    public class SpeakersController : ControllerBase
    {
        private readonly ISpeakerService _speakerService;

        public SpeakersController(ISpeakerService speakerService)
        {
            _speakerService = speakerService;
        }

        // 스피커 상태 조회
        [HttpGet("{id}/status")]
        public async Task<ActionResult<SpeakerStatusDto>> GetStatus(int id)
        {
            var status = await _speakerService.GetStatusAsync(id);
            if (status == null)
                return NotFound($"Speaker with ID {id} not found");

            return Ok(status);
        }

        // 스피커 제어
        [HttpPost("{id}/control")]
        public async Task<ActionResult> ControlSpeaker(int id, [FromBody] SpeakerControlDto controlRequest)
        {
            try
            {
                var result = await _speakerService.ControlSpeakerAsync(id, controlRequest);
                return Ok(new { Success = true, Message = $"Speaker {id} control command sent successfully", Details = result });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        // 스피커 제어 이력 조회
        [HttpGet("{id}/history")]
        public async Task<ActionResult<IEnumerable<SpeakerControlHistoryDto>>> GetControlHistory(
            int id,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int limit = 50)
        {
            var start = startDate ?? DateTimeHelper.Now.AddDays(-7);
            var end = endDate ?? DateTimeHelper.Now;

            var history = await _speakerService.GetControlHistoryAsync(id, start, end, limit);
            return Ok(history);
        }
    }
}