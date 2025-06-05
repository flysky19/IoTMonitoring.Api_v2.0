using System;
using System.Security.Authentication;
using System.Threading.Tasks;
using IoTMonitoring.Api.DTOs.Auth;
using IoTMonitoring.Api.Services.Auth.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace IoTMonitoring.Api.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {

            _authService = authService;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            try
            {
                var result = await _authService.LoginAsync(request);
                return Ok(result);
            }
            catch (AuthenticationException ex)
            {
                _logger.LogWarning($"로그인 실패: {ex.Message}");
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"로그인 처리 중 오류: {ex.Message}");
                return StatusCode(500, new { message = "로그인 처리 중 오류가 발생했습니다." });
            }
        }

        [HttpPost("refresh-token")]
        public async Task<ActionResult<LoginResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                var result = await _authService.RefreshTokenAsync(request.RefreshToken);
                return Ok(result);
            }
            catch (AuthenticationException ex)
            {
                _logger.LogWarning($"토큰 갱신 실패: {ex.Message}");
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"토큰 갱신 중 오류: {ex.Message}");
                return StatusCode(500, new { message = "토큰 갱신 중 오류가 발생했습니다." });
            }
        }

        /// <summary>
        /// 로그아웃
        /// </summary>
        [HttpPost("logout")]
        [Authorize]
        public ActionResult Logout()
        {
            // 실제로는 refresh token을 블랙리스트에 추가하거나 무효화
            return Ok(new { message = "로그아웃이 완료되었습니다." });
        }


        [Authorize]
        [HttpGet("validate")]
        public async Task<ActionResult> ValidateToken()
        {
            // 이 엔드포인트는 Authorize 속성으로 인해 토큰이 유효한 경우에만 접근 가능
            return Ok(new { isValid = true });
        }
    }
}