using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IoTMonitoring.Api.DTOs;

namespace IoTMonitoring.Api.Data.Repositories.Interfaces
{
    public interface ISensorConnectionHistoryRepository
    {
        Task<IEnumerable<SensorConnectionHistoryDto>> GetConnectionHistoryAsync(int sensorId, DateTime startDate, DateTime endDate, int limit);
        Task<IEnumerable<SensorConnectionHistoryDto>> GetRecentConnectionHistoryAsync(int sensorId, int limit = 10);
        Task AddConnectionHistoryAsync(int sensorId, string oldStatus, string newStatus, string reason);
        Task<int> GetConnectionCountAsync(int sensorId, DateTime startDate, DateTime endDate);
    }
}