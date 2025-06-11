using IoTMonitoring.Api.Utilities;

namespace IoTMonitoring.Api.Middlewares
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var startTime = DateTimeHelper.Now;

            // 요청 정보 로깅
            _logger.LogInformation($"🚀 {context.Request.Method} {context.Request.Path} 시작");

            await _next(context);

            var duration = DateTimeHelper.Now - startTime;

            // 응답 정보 로깅  
            _logger.LogInformation($"✅ {context.Request.Method} {context.Request.Path} " +
                                  $"완료 - {context.Response.StatusCode} ({duration.TotalMilliseconds}ms)");
        }
    }
}
