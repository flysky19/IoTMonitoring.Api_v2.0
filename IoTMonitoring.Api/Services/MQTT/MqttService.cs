// Services/MQTT/MqttService.cs
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Client;
using System;
using System.Text;
using System.Threading.Tasks;
using IoTMonitoring.Api.Services.MQTT.Interfaces;
using IoTMonitoring.Api.Services.Interfaces;
using IoTMonitoring.Api.Services.Sensor.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using IoTMonitoring.Api.Data.Repositories.Interfaces;
using System.Text.Json;
using IoTMonitoring.Api.DTOs;
using IoTMonitoring.Api.Services.SignalR.Interfaces;

namespace IoTMonitoring.Api.Services.MQTT
{
    public class MqttService : IMqttService
    {
        private readonly ILogger<MqttService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IServiceScopeFactory _scopeFactory; // 변경
        private IManagedMqttClient _mqttClient;

        public bool IsConnected => _mqttClient?.IsConnected ?? false;
        public event EventHandler<SensorDataReceivedEventArgs> OnSensorDataReceived;

        public MqttService(
            ILogger<MqttService> logger,
            IConfiguration configuration,
            IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _configuration = configuration;
            _scopeFactory = scopeFactory;
        }

        public async Task StartAsync()
        {
            try
            {
                var mqttSettings = _configuration.GetSection("MqttSettings");

                // ClientId 동적 생성
                var clientId = mqttSettings["ClientId"];
                if (clientId.Contains("{MachineName}"))
                {
                    clientId = clientId.Replace("{MachineName}", Environment.MachineName);
                }
                if (clientId.Contains("{Timestamp}"))
                {
                    clientId = clientId.Replace("{Timestamp}", DateTime.UtcNow.Ticks.ToString());
                }

                _logger.LogInformation($"MQTT Client ID: {clientId}");

                var clientOptions = new MqttClientOptionsBuilder()
                    .WithClientId(clientId)
                    .WithTcpServer(mqttSettings["BrokerHost"], int.Parse(mqttSettings["BrokerPort"]))
                    .WithCredentials(mqttSettings["Username"], mqttSettings["Password"])
                    .WithTls()
                    .WithCleanSession()
                    .WithKeepAlivePeriod(TimeSpan.FromSeconds(60))
                    .Build();

                var managedOptions = new ManagedMqttClientOptionsBuilder()
                    .WithAutoReconnectDelay(TimeSpan.FromSeconds(10))
                    .WithClientOptions(clientOptions)
                    .Build();

                _mqttClient = new MqttFactory().CreateManagedMqttClient();

                // 이벤트 핸들러 등록
                _mqttClient.ApplicationMessageReceivedAsync += OnMessageReceived;
                _mqttClient.ConnectedAsync += OnConnected;
                _mqttClient.DisconnectedAsync += OnDisconnected;

                await _mqttClient.StartAsync(managedOptions);

                _logger.LogInformation("MQTT 클라이언트 시작됨");
            }
            catch (Exception ex)
            {
                _logger.LogError($"MQTT 클라이언트 시작 실패: {ex.Message}");
                throw;
            }
        }

        public async Task StopAsync()
        {
            if (_mqttClient != null)
            {
                await _mqttClient.StopAsync();
                _mqttClient.Dispose();
                _logger.LogInformation("MQTT 클라이언트 중지됨");
            }
        }

        public async Task SubscribeToSensorTopicsAsync()
        {
            if (!IsConnected)
            {
                _logger.LogWarning("MQTT 클라이언트가 연결되지 않음");
                return;
            }

            try
            {
                // 모든 센서 데이터 토픽 구독
                var topics = new[]
                {
                    "iot-monitoring/sensors/+/data",
                    "iot-monitoring/sensors/+/status",
                    "iot-monitoring/sensors/+/heartbeat"
                };

                foreach (var topic in topics)
                {
                    await _mqttClient.SubscribeAsync(topic, MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce);
                    _logger.LogInformation($"토픽 구독: {topic}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"토픽 구독 실패: {ex.Message}");
            }
        }

        public async Task PublishAsync(string topic, string message, int qos = 0, bool retain = false)
        {
            if (!IsConnected)
            {
                _logger.LogWarning("MQTT 클라이언트가 연결되지 않음");
                return;
            }

            try
            {
                var mqttMessage = new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(message)
                    .WithQualityOfServiceLevel((MQTTnet.Protocol.MqttQualityOfServiceLevel)qos)
                    .WithRetainFlag(retain)
                    .Build();

                await _mqttClient.EnqueueAsync(mqttMessage);
                _logger.LogInformation($"메시지 발송: {topic} - {message}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"메시지 발송 실패: {ex.Message}");
            }
        }

        private async Task OnMessageReceived(MqttApplicationMessageReceivedEventArgs e)
        {
            try
            {
                var topic = e.ApplicationMessage.Topic;
                var payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);

                _logger.LogInformation($"MQTT 메시지 수신 - Topic: {topic}, Payload: {payload}");

                // 토픽 파싱: iot-monitoring/sensors/{sensorUuid}/{messageType}
                var topicParts = topic.Split('/');
                if (topicParts.Length >= 4 && topicParts[0] == "iot-monitoring" && topicParts[1] == "sensors")
                {
                    var sensorUuid = topicParts[2];
                    var messageType = topicParts[3];

                    // 센서 데이터 처리
                    await ProcessSensorMessage(sensorUuid, messageType, payload);

                    // 이벤트 발생
                    OnSensorDataReceived?.Invoke(this, new SensorDataReceivedEventArgs
                    {
                        SensorUuid = sensorUuid,
                        MessageType = messageType,
                        Payload = payload,
                        Timestamp = DateTime.UtcNow
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"MQTT 메시지 처리 중 오류: {ex.Message}");
            }
        }

        private async Task ProcessSensorMessage(string sensorUuid, string messageType, string payload)
        {
            try
            {
                switch (messageType.ToLower())
                {
                    case "data":
                        await ProcessSensorData(sensorUuid, payload);
                        break;
                    case "heartbeat":
                        await ProcessHeartbeat(sensorUuid);
                        break;
                    case "status":
                        await ProcessStatusUpdate(sensorUuid, payload);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"센서 메시지 처리 실패 - UUID: {sensorUuid}, Type: {messageType}, Error: {ex.Message}");
            }
        }

        private async Task ProcessSensorData(string sensorUuid, string payload)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope(); // 변경
                var sensorService = scope.ServiceProvider.GetRequiredService<ISensorService>();
                var sensorDataRepository = scope.ServiceProvider.GetRequiredService<ISensorDataRepository>();
                var signalRService = scope.ServiceProvider.GetRequiredService<ISignalRService>(); // 추가

                var sensors = await sensorService.GetAllSensorsAsync(null, null, null);
                var sensor = sensors.FirstOrDefault(s => s.SensorUUID == sensorUuid);

                if (sensor != null)
                {
                    await SaveSensorDataByType(sensorDataRepository, sensor, payload);

                    // SignalR로 실시간 전송
                    var sensorData = new
                    {
                        sensorId = sensor.SensorID,
                        sensorUuid = sensorUuid,
                        sensorType = sensor.SensorType,
                        data = JsonSerializer.Deserialize<JsonElement>(payload),
                        timestamp = DateTime.UtcNow
                    };

                    // 모든 모니터링 클라이언트에게 전송
                    await signalRService.SendSensorDataToAllAsync(sensorData);

                    // 특정 센서 구독자들에게 전송
                    await signalRService.SendSensorDataToGroupAsync(sensor.SensorID, sensorData);

                    _logger.LogInformation($"센서 데이터 저장 및 실시간 전송 완료: {sensorUuid}");
                }
                else
                {
                    _logger.LogWarning($"센서를 찾을 수 없음: {sensorUuid}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"센서 데이터 처리 실패 - UUID: {sensorUuid}, Error: {ex.Message}");
            }
        }

        private async Task SaveSensorDataByType(ISensorDataRepository repository, dynamic sensor, string payload)
        {
            try
            {
                var data = JsonSerializer.Deserialize<JsonElement>(payload);

                switch (sensor.SensorType.ToLower())
                {
                    case "temp_humidity":
                        var tempHumidityDto = new TempHumidityDataCreateDto
                        {
                            SensorID = sensor.SensorID,
                            Timestamp = data.TryGetProperty("timestamp", out var timestampProp)
                                ? DateTime.Parse(timestampProp.GetString())
                                : DateTime.UtcNow,
                            Temperature = data.TryGetProperty("temperature", out var tempProp)
                                ? tempProp.GetSingle()
                                : null,
                            Humidity = data.TryGetProperty("humidity", out var humidityProp)
                                ? humidityProp.GetSingle()
                                : null,
                            RawData = payload
                        };
                        await repository.AddTempHumidityDataAsync(tempHumidityDto);
                        break;

                    case "particle":
                        if (data.TryGetProperty("data", out var sensorData))
                        {
                            var particleDto = new ParticleDataCreateDto
                            {
                                SensorID = sensor.SensorID,
                                Timestamp = data.TryGetProperty("timestamp", out var timestampProp2)
                                ? DateTime.Parse(timestampProp2.GetString())
                                : DateTime.UtcNow,
                                PM0_3 = sensorData.TryGetProperty("pm0_3", out var pm03Prop) ? pm03Prop.GetSingle() : null,
                                PM0_5 = sensorData.TryGetProperty("pm0_5", out var pm05Prop) ? pm05Prop.GetSingle() : null,
                                PM1_0 = sensorData.TryGetProperty("pm1_0", out var pm10Prop) ? pm10Prop.GetSingle() : null,
                                PM2_5 = sensorData.TryGetProperty("pm2_5", out var pm25Prop) ? pm25Prop.GetSingle() : null,
                                PM5_0 = sensorData.TryGetProperty("pm5_0", out var pm50Prop) ? pm50Prop.GetSingle() : null,
                                PM10 = sensorData.TryGetProperty("pm10", out var pm100Prop) ? pm100Prop.GetSingle() : null,
                                RawData = payload
                            };

                            await repository.AddParticleDataAsync(particleDto);
                        }

                        break;

                    case "wind":
                        var windDto = new WindDataCreateDto
                        {
                            SensorID = sensor.SensorID,
                            Timestamp = data.TryGetProperty("timestamp", out var timestampProp3)
                                ? DateTime.Parse(timestampProp3.GetString())
                                : DateTime.UtcNow,
                            WindSpeed = data.TryGetProperty("windSpeed", out var windSpeedProp)
                                ? windSpeedProp.GetSingle()
                                : null,
                            RawData = payload
                        };
                        await repository.AddWindDataAsync(windDto);
                        break;

                    default:
                        _logger.LogWarning($"지원하지 않는 센서 타입: {sensor.SensorType}");
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"센서 데이터 저장 실패: {ex.Message}");
                throw;
            }
        }

        private async Task ProcessHeartbeat(string sensorUuid)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope(); // 변경
                var sensorService = scope.ServiceProvider.GetRequiredService<ISensorService>();
                var signalRService = scope.ServiceProvider.GetRequiredService<ISignalRService>(); // 추가


                var sensors = await sensorService.GetAllSensorsAsync(null, null, null);
                var sensor = sensors.FirstOrDefault(s => s.SensorUUID == sensorUuid);

                if (sensor != null)
                {
                    await sensorService.UpdateSensorHeartbeatAsync(sensor.SensorID);

                    // SignalR로 하트비트 업데이트 전송
                    await signalRService.SendHeartbeatUpdateAsync(sensor.SensorID, DateTime.UtcNow);

                    _logger.LogInformation($"하트비트 업데이트: {sensorUuid}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"하트비트 처리 실패: {sensorUuid} - {ex.Message}");
            }
        }

        private async Task ProcessStatusUpdate(string sensorUuid, string payload)
        {
            try
            {
                _logger.LogInformation($"상태 업데이트: {sensorUuid} - {payload}");

                using var scope = _scopeFactory.CreateScope();
                var sensorService = scope.ServiceProvider.GetRequiredService<ISensorService>();
                var signalRService = scope.ServiceProvider.GetRequiredService<ISignalRService>();

                // 센서 UUID로 센서 조회
                var sensors = await sensorService.GetAllSensorsAsync(null, null, null);
                var sensor = sensors.FirstOrDefault(s => s.SensorUUID == sensorUuid);

                if (sensor == null)
                {
                    _logger.LogWarning($"상태 업데이트 실패: 센서를 찾을 수 없음 - UUID: {sensorUuid}");
                    return;
                }

                // JSON 형식으로 페이로드 파싱
                var statusData = JsonSerializer.Deserialize<JsonElement>(payload);

                // 연결 상태와 상태 메시지 추출
                string connectionStatus = "online";
                string statusMessage = null;

                if (statusData.TryGetProperty("status", out var statusProp))
                {
                    connectionStatus = statusProp.GetString();
                }

                if (statusData.TryGetProperty("message", out var messageProp))
                {
                    statusMessage = messageProp.GetString();
                }

                // 센서 연결 상태 업데이트
                await sensorService.UpdateSensorConnectionStatusAsync(sensor.SensorID, connectionStatus, statusMessage);

                // SignalR을 통해 상태 변경을 클라이언트에 알림
                await signalRService.SendSensorStatusUpdateAsync(sensor.SensorID, statusMessage);

                _logger.LogInformation($"센서 상태 업데이트 처리 완료 - UUID: {sensorUuid}, Status: {connectionStatus}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"센서 상태 업데이트 처리 중 오류: {sensorUuid} - {ex.Message}");
            }
        }

        private async Task OnConnected(MqttClientConnectedEventArgs e)
        {
            _logger.LogInformation("MQTT 브로커에 연결됨");

            // 연결되면 자동으로 센서 토픽 구독
            await SubscribeToSensorTopicsAsync();
        }

        private Task OnDisconnected(MqttClientDisconnectedEventArgs e)
        {
            _logger.LogWarning($"MQTT 브로커 연결 끊김: {e.Reason}");
            return Task.CompletedTask;
        }
    }
}