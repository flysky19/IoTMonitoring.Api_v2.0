using Microsoft.AspNetCore.SignalR;
using IoTMonitoring.Api.Hubs;
using IoTMonitoring.Api.Services.SignalR.Interfaces;

namespace IoTMonitoring.Api.Services.SignalR
{
    public class SignalRService : ISignalRService
    {
        private readonly IHubContext<SensorHub> _hubContext;
        private readonly ILogger<SignalRService> _logger;

        public SignalRService(IHubContext<SensorHub> hubContext, ILogger<SignalRService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task SendSensorDataToAllAsync(object sensorData)
        {
            try
            {
                await _hubContext.Clients.Group("SensorMonitors").SendAsync("SensorDataReceived", sensorData);
                _logger.LogDebug("모든 클라이언트에게 센서 데이터 전송 완료");
            }
            catch (Exception ex)
            {
                _logger.LogError($"센서 데이터 전송 실패: {ex.Message}");
            }
        }

        public async Task SendSensorDataToGroupAsync(int sensorId, object sensorData)
        {
            try
            {
                var groupName = $"Sensor_{sensorId}";
                await _hubContext.Clients.Group(groupName).SendAsync("SensorDataUpdated", new
                {
                    sensorId = sensorId,
                    data = sensorData,
                    timestamp = DateTime.UtcNow
                });

                _logger.LogDebug($"센서 {sensorId} 구독자들에게 데이터 전송 완료");
            }
            catch (Exception ex)
            {
                _logger.LogError($"센서 {sensorId} 데이터 전송 실패: {ex.Message}");
            }
        }

        public async Task SendSensorStatusUpdateAsync(int sensorId, string status)
        {
            try
            {
                await _hubContext.Clients.Group("SensorMonitors").SendAsync("SensorStatusChanged", new
                {
                    sensorId = sensorId,
                    status = status,
                    timestamp = DateTime.UtcNow
                });

                _logger.LogInformation($"센서 {sensorId} 상태 변경 알림: {status}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"센서 상태 업데이트 전송 실패: {ex.Message}");
            }
        }

        public async Task SendHeartbeatUpdateAsync(int sensorId, DateTime lastHeartbeat)
        {
            try
            {
                await _hubContext.Clients.Group("SensorMonitors").SendAsync("HeartbeatReceived", new
                {
                    sensorId = sensorId,
                    lastHeartbeat = lastHeartbeat,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"하트비트 업데이트 전송 실패: {ex.Message}");
            }
        }

        public async Task SendAlertToAllAsync(object alert)
        {
            try
            {
                await _hubContext.Clients.Group("SensorMonitors").SendAsync("AlertReceived", alert);
                _logger.LogWarning($"알림 전송: {alert}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"알림 전송 실패: {ex.Message}");
            }
        }

        public async Task SendDashboardUpdateAsync(object dashboardData)
        {
            try
            {
                await _hubContext.Clients.Group("Dashboard").SendAsync("DashboardUpdated", dashboardData);
                _logger.LogDebug("대시보드 데이터 업데이트 전송 완료");
            }
            catch (Exception ex)
            {
                _logger.LogError($"대시보드 업데이트 전송 실패: {ex.Message}");
            }
        }
    }
}