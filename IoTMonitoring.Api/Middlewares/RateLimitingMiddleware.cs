using IoTMonitoring.Api.Data.RateLimit;
using IoTMonitoring.Api.Services.RateLimit;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace IoTMonitoring.Api.Middlewares
{
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IRateLimitService _rateLimitService;
        private readonly ILogger<RateLimitingMiddleware> _logger;
        private readonly RateLimitOptions _options;

        public RateLimitingMiddleware(
        RequestDelegate next,
            IRateLimitService rateLimitService,
            IOptions<RateLimitOptions> options,
            ILogger<RateLimitingMiddleware> logger)
        {
            _next = next;
            _rateLimitService = rateLimitService;
            _options = options.Value;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var clientId = GetClientIdentifier(context);
            var path = context.Request.Path.Value;

            // Rate limit 체크
            if (!await _rateLimitService.IsRequestAllowedAsync(clientId, path))
            {
                await HandleRateLimitExceeded(context, clientId);
                return;
            }

            // 요청 기록
            await _rateLimitService.RecordRequestAsync(clientId);

            // Rate limit 헤더 추가
            await AddRateLimitHeaders(context, clientId);

            // 다음 미들웨어로 진행
            await _next(context);
        }

        private string GetClientIdentifier(HttpContext context)
        {
            // 1. 인증된 사용자는 UserID 사용
            var userId = context.User?.FindFirst("userId")?.Value;
            if (!string.IsNullOrEmpty(userId))
                return $"user:{userId}";

            // 2. API 키가 있다면 API 키 사용
            var apiKey = context.Request.Headers["X-API-Key"].FirstOrDefault();
            if (!string.IsNullOrEmpty(apiKey))
                return $"api:{apiKey}";

            // 3. 기본적으로 IP 주소 사용
            var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault() ??
                        context.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim() ??
                        context.Connection.RemoteIpAddress?.ToString();

            return $"ip:{realIp ?? "unknown"}";
        }

        private async Task HandleRateLimitExceeded(HttpContext context, string clientId)
        {
            var clientInfo = await _rateLimitService.GetClientInfoAsync(clientId);

            context.Response.StatusCode = 429; // Too Many Requests
            context.Response.ContentType = "application/json";

            // Rate limit 정보 헤더 추가
            context.Response.Headers.Add("Retry-After", "60");
            context.Response.Headers.Add("X-RateLimit-Limit", _options.RequestsPerMinute.ToString());
            context.Response.Headers.Add("X-RateLimit-Remaining", "0");
            context.Response.Headers.Add("X-RateLimit-Reset", DateTimeOffset.UtcNow.AddMinutes(1).ToUnixTimeSeconds().ToString());

            if (clientInfo?.BlockedUntil.HasValue == true)
            {
                context.Response.Headers.Add("X-RateLimit-BlockedUntil",
                    ((DateTimeOffset)clientInfo.BlockedUntil.Value).ToUnixTimeSeconds().ToString());
            }

            var response = new
            {
                error = "Rate limit exceeded",
                message = "요청 횟수가 제한을 초과했습니다. 잠시 후 다시 시도해주세요.",
                retryAfter = 60,
                clientId = clientId.Split(':').LastOrDefault(), // IP만 노출
                blockedUntil = clientInfo?.BlockedUntil
            };

            var json = System.Text.Json.JsonSerializer.Serialize(response);
            await context.Response.WriteAsync(json);

            _logger.LogWarning($"Rate limit 초과: {clientId}, Path: {context.Request.Path}");
        }

        private async Task AddRateLimitHeaders(HttpContext context, string clientId)
        {
            var clientInfo = await _rateLimitService.GetClientInfoAsync(clientId);
            if (clientInfo == null) return;

            var minuteRequests = clientInfo.GetRequestCountInWindow(TimeSpan.FromMinutes(1));
            var remaining = Math.Max(0, _options.RequestsPerMinute - minuteRequests);

            context.Response.Headers.Add("X-RateLimit-Limit", _options.RequestsPerMinute.ToString());
            context.Response.Headers.Add("X-RateLimit-Remaining", remaining.ToString());
            context.Response.Headers.Add("X-RateLimit-Reset",
                DateTimeOffset.UtcNow.AddMinutes(1).ToUnixTimeSeconds().ToString());
        }
    }
}
