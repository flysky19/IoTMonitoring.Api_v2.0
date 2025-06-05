using IoTMonitoring.Api.Data.RateLimit;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace IoTMonitoring.Api.Services.RateLimit
{
    public class MemoryRateLimitService : IRateLimitService
    {
        private readonly RateLimitOptions _options;
        private readonly ILogger<MemoryRateLimitService> _logger;
        private readonly ConcurrentDictionary<string, ClientRequestInfo> _clients = new();
        private readonly Timer _cleanupTimer;

        public MemoryRateLimitService(IOptions<RateLimitOptions> options, ILogger<MemoryRateLimitService> logger)
        {
            _options = options.Value;
            _logger = logger;

            // 5분마다 오래된 데이터 정리
            _cleanupTimer = new Timer(CleanupOldData, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
        }

        public async Task<bool> IsRequestAllowedAsync(string clientId, string path)
        {
            // 화이트리스트 IP 체크
            if (_options.WhitelistedIPs.Contains(clientId))
                return true;

            // 화이트리스트 경로 체크
            if (_options.WhitelistedPaths.Any(whitePath => path.StartsWith(whitePath, StringComparison.OrdinalIgnoreCase)))
                return true;

            var clientInfo = _clients.GetOrAdd(clientId, new ClientRequestInfo { ClientId = clientId });

            // 현재 차단 상태 확인
            if (clientInfo.IsBlocked)
            {
                _logger.LogWarning($"차단된 클라이언트 요청 시도: {clientId}, 차단 해제: {clientInfo.BlockedUntil}");
                return false;
            }

            // 요청 제한 체크
            var minuteRequests = clientInfo.GetRequestCountInWindow(TimeSpan.FromMinutes(1));
            var hourRequests = clientInfo.GetRequestCountInWindow(TimeSpan.FromHours(1));
            var dayRequests = clientInfo.GetRequestCountInWindow(TimeSpan.FromDays(1));

            // 제한 초과 체크
            if (minuteRequests >= _options.RequestsPerMinute)
            {
                await BlockClientAsync(clientInfo, "분당 요청 수 초과");
                return false;
            }

            if (hourRequests >= _options.RequestsPerHour)
            {
                await BlockClientAsync(clientInfo, "시간당 요청 수 초과");
                return false;
            }

            if (dayRequests >= _options.RequestsPerDay)
            {
                await BlockClientAsync(clientInfo, "일당 요청 수 초과");
                return false;
            }

            return true;
        }

        public async Task RecordRequestAsync(string clientId)
        {
            var clientInfo = _clients.GetOrAdd(clientId, new ClientRequestInfo { ClientId = clientId });
            clientInfo.RequestTimes.Add(DateTime.UtcNow);

            // 메모리 사용량 제한을 위해 오래된 요청 기록 정리
            clientInfo.CleanupOldRequests(TimeSpan.FromDays(1));
        }

        public async Task<ClientRequestInfo> GetClientInfoAsync(string clientId)
        {
            return _clients.GetValueOrDefault(clientId);
        }

        private async Task BlockClientAsync(ClientRequestInfo clientInfo, string reason)
        {
            clientInfo.BlockedUntil = DateTime.UtcNow.Add(_options.BlockDuration);
            clientInfo.ViolationCount++;

            _logger.LogWarning($"클라이언트 차단: {clientInfo.ClientId}, 사유: {reason}, " +
                              $"차단 시간: {_options.BlockDuration}, 위반 횟수: {clientInfo.ViolationCount}");
        }

        private void CleanupOldData(object state)
        {
            var cutoff = DateTime.UtcNow - TimeSpan.FromDays(1);
            var toRemove = new List<string>();

            foreach (var kvp in _clients)
            {
                var clientInfo = kvp.Value;
                clientInfo.CleanupOldRequests(TimeSpan.FromDays(1));

                // 1일 동안 요청이 없고 차단도 해제된 클라이언트는 제거
                if (!clientInfo.RequestTimes.Any() && !clientInfo.IsBlocked)
                {
                    toRemove.Add(kvp.Key);
                }
            }

            foreach (var clientId in toRemove)
            {
                _clients.TryRemove(clientId, out _);
            }

            _logger.LogDebug($"Rate limit 데이터 정리 완료: {toRemove.Count}개 클라이언트 제거, " +
                            $"현재 추적 중인 클라이언트: {_clients.Count}개");
        }

        public void Dispose()
        {
            _cleanupTimer?.Dispose();
        }
    }
}
