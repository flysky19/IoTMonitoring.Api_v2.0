using IoTMonitoring.Api.Data.RateLimit;

namespace IoTMonitoring.Api.Services.RateLimit
{
    public interface IRateLimitService
    {
        Task<bool> IsRequestAllowedAsync(string clientId, string path);
        Task RecordRequestAsync(string clientId);
        Task<ClientRequestInfo> GetClientInfoAsync(string clientId);
    }

}
