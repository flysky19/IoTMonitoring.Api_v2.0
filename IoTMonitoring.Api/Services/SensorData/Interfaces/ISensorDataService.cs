namespace IoTMonitoring.Api.Services.SensorData.Interfaces
{
    public interface ISensorDataService
    {
        Task SaveSensorDataAsync(int sensorId, string sensorType, string jsonData);
        Task<IEnumerable<dynamic>> GetSensorDataAsync(int sensorId, DateTime startDate, DateTime endDate, int limit);
    }
}