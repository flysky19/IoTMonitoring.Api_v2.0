using IoTMonitoring.Api.Services.MQTT.Interfaces;

namespace IoTMonitoring.Api.Services.MQTT
{
    public class MqttBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MqttBackgroundService> _logger;

        public MqttBackgroundService(IServiceProvider serviceProvider, ILogger<MqttBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var mqttService = scope.ServiceProvider.GetRequiredService<IMqttService>();

                await mqttService.StartAsync();
                _logger.LogInformation("MQTT 백그라운드 서비스 시작됨");

                // 서비스가 중지될 때까지 대기
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("MQTT 백그라운드 서비스 중지됨");
            }
            catch (Exception ex)
            {
                _logger.LogError($"MQTT 백그라운드 서비스 오류: {ex.Message}");
            }
        }
    }
}
