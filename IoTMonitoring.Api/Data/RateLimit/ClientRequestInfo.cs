using IoTMonitoring.Api.Utilities;

namespace IoTMonitoring.Api.Data.RateLimit
{
    public class ClientRequestInfo
    {
        public string ClientId { get; set; }
        public List<DateTime> RequestTimes { get; set; } = new();
        public DateTime? BlockedUntil { get; set; }
        public int ViolationCount { get; set; } = 0;

        // 만료된 요청 기록 정리
        public void CleanupOldRequests(TimeSpan maxAge)
        {
            var cutoff = DateTimeHelper.Now - maxAge;
            RequestTimes.RemoveAll(time => time < cutoff);
        }

        // 현재 차단 상태인지 확인
        public bool IsBlocked => BlockedUntil.HasValue && BlockedUntil > DateTimeHelper.Now;

        // 특정 시간 내 요청 수 계산
        public int GetRequestCountInWindow(TimeSpan window)
        {
            var cutoff = DateTimeHelper.Now - window;
            return RequestTimes.Count(time => time >= cutoff);
        }
    }
}
