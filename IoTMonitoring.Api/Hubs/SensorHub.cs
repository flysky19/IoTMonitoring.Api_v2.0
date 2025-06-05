using Microsoft.AspNetCore.SignalR;

namespace IoTMonitoring.Api.Hubs
{
    public class SensorHub : Hub
    {
        private readonly ILogger<SensorHub> _logger;

        public SensorHub(ILogger<SensorHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation($"클라이언트 연결됨: {Context.ConnectionId}");

            // 연결된 클라이언트를 "SensorMonitors" 그룹에 추가
            await Groups.AddToGroupAsync(Context.ConnectionId, "SensorMonitors");

            // 클라이언트에게 연결 확인 메시지 전송
            await Clients.Caller.SendAsync("Connected", new
            {
                connectionId = Context.ConnectionId,
                message = "SignalR에 연결되었습니다!"
            });

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            _logger.LogInformation($"클라이언트 연결 해제됨: {Context.ConnectionId}");

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "SensorMonitors");
            await base.OnDisconnectedAsync(exception);
        }

        // 클라이언트가 특정 센서 구독
        public async Task SubscribeToSensor(int sensorId)
        {
            var groupName = $"Sensor_{sensorId}";
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

            _logger.LogInformation($"클라이언트 {Context.ConnectionId}가 센서 {sensorId} 구독");

            await Clients.Caller.SendAsync("SubscriptionConfirmed", new
            {
                sensorId = sensorId,
                message = $"센서 {sensorId} 구독이 완료되었습니다."
            });
        }

        // 클라이언트가 센서 구독 해제
        public async Task UnsubscribeFromSensor(int sensorId)
        {
            var groupName = $"Sensor_{sensorId}";
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

            _logger.LogInformation($"클라이언트 {Context.ConnectionId}가 센서 {sensorId} 구독 해제");
        }

        // 대시보드 그룹 참여
        public async Task JoinDashboard()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "Dashboard");
            _logger.LogInformation($"클라이언트 {Context.ConnectionId}가 대시보드 그룹 참여");
        }
    }
}