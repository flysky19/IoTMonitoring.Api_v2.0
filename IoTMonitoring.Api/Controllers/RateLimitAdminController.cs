using IoTMonitoring.Api.Services.RateLimit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IoTMonitoring.Api.Controllers
{
    [ApiController]
    [Route("api/admin/rate-limit")]
    [Authorize(Roles = "Admin")]
    public class RateLimitAdminController : ControllerBase
    {
        private readonly IRateLimitService _rateLimitService;

        public RateLimitAdminController(IRateLimitService rateLimitService)
        {
            _rateLimitService = rateLimitService;
        }

        [HttpGet("status/{clientId}")]
        public async Task<ActionResult> GetClientStatus(string clientId)
        {
            var info = await _rateLimitService.GetClientInfoAsync(clientId);
            if (info == null)
                return NotFound();

            return Ok(new
            {
                clientId = info.ClientId,
                isBlocked = info.IsBlocked,
                blockedUntil = info.BlockedUntil,
                violationCount = info.ViolationCount,
                recentRequests = info.RequestTimes.TakeLast(10)
            });
        }
    }
}
