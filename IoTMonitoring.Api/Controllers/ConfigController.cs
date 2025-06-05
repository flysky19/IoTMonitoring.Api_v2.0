using IoTMonitoring.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace IoTMonitoring.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ConfigController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ConfigController> _logger;

        public ConfigController(
          IConfiguration configuration,
          ILogger<ConfigController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// 클라이언트 라이센스 키 조회
        /// </summary>
        [HttpGet("license")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult GetLicenseKey()
        {
            try
            {
                // appsettings에서 라이센스 키 읽기
                var licenseKey = _configuration["License:SecretKey"];

                if (string.IsNullOrEmpty(licenseKey))
                {
                    _logger.LogWarning("라이센스 키가 설정되지 않았습니다.");
                    return Ok(new { licenseKey = "" });
                }

                // 선택사항: 라이센스 키 난독화
                // var obfuscatedKey = ObfuscateLicenseKey(licenseKey);

                return Ok(new
                {
                    licenseKey = licenseKey,
                    // 추가 정보 (필요시)
                    expiryDate = (DateTime?)null,
                    type = "production"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "라이센스 키 조회 중 오류 발생");
                return StatusCode(500, new { error = "라이센스 키를 불러올 수 없습니다." });
            }
        }
        /// <summary>
        /// 시스템 설정 정보 조회
        /// </summary>
        [HttpGet("settings")]
        public IActionResult GetSystemSettings()
        {
            try
            {
                var settings = new
                {
                    version = "1.0.0",
                    environment = _configuration["Environment"] ?? "Production",
                    features = new
                    {
                        realtimeMonitoring = true,
                        dataExport = true,
                        alertSystem = true,
                        maxSensorsPerGroup = 100,
                        dataRetentionDays = _configuration.GetValue<int>("DataRetention:Days", 90)
                    }
                };

                return Ok(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "시스템 설정 조회 중 오류 발생");
                return StatusCode(500, new { error = "설정을 불러올 수 없습니다." });
            }
        }

        /// <summary>
        /// SyncFusion 라이센스 키 조회 (더 안전한 방법)
        /// </summary>
        [HttpGet("syncfusion-license")]
        [Authorize(Roles = "Admin,User")]  // 역할 기반 인증
        public IActionResult GetSyncFusionLicense()
        {
            try
            {
                var userId = User.FindFirst("UserId")?.Value;
                _logger.LogInformation($"사용자 {userId}가 SyncFusion 라이센스 키를 요청했습니다.");

                var licenseKey = _configuration["License:SecretKey"];

                if (string.IsNullOrEmpty(licenseKey))
                {
                    return Ok(new { licenseKey = "" });
                }

                // 선택사항: 사용자별 라이센스 검증
                // if (!IsUserLicensed(userId)) 
                // {
                //     return Forbid("라이센스가 없는 사용자입니다.");
                // }

                return Ok(new
                {
                    licenseKey = licenseKey,
                    isValid = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SyncFusion 라이센스 키 조회 중 오류 발생");
                return StatusCode(500, new { error = "라이센스 키를 불러올 수 없습니다." });
            }
        }

        // 선택사항: 라이센스 키 난독화 메서드
        private string ObfuscateLicenseKey(string key)
        {
            // Base64 인코딩이나 다른 난독화 방법 사용
            var bytes = Encoding.UTF8.GetBytes(key);
            return Convert.ToBase64String(bytes);
        }

        private string DeobfuscateLicenseKey(string obfuscatedKey)
        {
            var bytes = Convert.FromBase64String(obfuscatedKey);
            return Encoding.UTF8.GetString(bytes);
        }


    }
}