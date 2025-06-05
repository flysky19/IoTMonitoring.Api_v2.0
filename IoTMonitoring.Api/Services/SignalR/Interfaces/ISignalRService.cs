namespace IoTMonitoring.Api.Services.SignalR.Interfaces
{
    public interface ISignalRService
    {
        Task SendSensorDataToAllAsync(object sensorData);
        Task SendSensorDataToGroupAsync(int sensorId, object sensorData);
        Task SendSensorStatusUpdateAsync(int sensorId, string status);
        Task SendHeartbeatUpdateAsync(int sensorId, DateTime lastHeartbeat);
        Task SendAlertToAllAsync(object alert);
        Task SendDashboardUpdateAsync(object dashboardData);
    }
}