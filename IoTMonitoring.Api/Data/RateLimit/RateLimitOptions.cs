namespace IoTMonitoring.Api.Data.RateLimit
{
    public class RateLimitOptions
    {
        public int RequestsPerMinute { get; set; } = 60;      // 분당 요청 수
        public int RequestsPerHour { get; set; } = 1000;      // 시간당 요청 수  
        public int RequestsPerDay { get; set; } = 10000;      // 일당 요청 수
        public TimeSpan BlockDuration { get; set; } = TimeSpan.FromMinutes(15); // 차단 시간
        public List<string> WhitelistedIPs { get; set; } = new(); // 화이트리스트 IP
        public List<string> WhitelistedPaths { get; set; } = new() { "/api/health" }; // 제외할 경로
    }
}
