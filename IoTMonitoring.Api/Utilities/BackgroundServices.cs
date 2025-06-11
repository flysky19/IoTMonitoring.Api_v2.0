using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using IoTMonitoring.Api.Data.Connection;
using Microsoft.AspNetCore.Connections;


namespace IoTMonitoring.Api.Utilities
{
    // 설정 클래스들
    public class BackgroundJobSettings
    {
        public CheckSensorConnectionsSettings CheckSensorConnections { get; set; }
        public DailyJobSettings CalculateDailySensorUptime { get; set; }
        public DailyJobSettings PurgeOldData { get; set; }
    }

    public class CheckSensorConnectionsSettings
    {
        public bool Enabled { get; set; } = true;
        public int IntervalMinutes { get; set; } = 1;
    }

    public class DailyJobSettings
    {
        public bool Enabled { get; set; } = true;
        public TimeSpan ExecutionTime { get; set; } = new TimeSpan(1, 0, 0); // 기본값 01:00
    }

    // 데이터베이스 작업 인터페이스
    public interface IMaintenanceRepository
    {
        Task CheckSensorConnectionsAsync();
        Task CalculateDailySensorUptimeAsync();
        Task PurgeOldDataAsync();
    }

    // 데이터베이스 작업 구현
    public class MaintenanceRepository : IMaintenanceRepository
    {
        private readonly ILogger<MaintenanceRepository> _logger;
        private readonly IDbConnectionFactory _connectionFactory;

        public MaintenanceRepository(IDbConnectionFactory connectionFactory, ILogger<MaintenanceRepository> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        public async Task CheckSensorConnectionsAsync()
        {
            try
            {
                using (var connection = await _connectionFactory.CreateConnectionAsync())
                {
                    _logger.LogInformation("센서 연결 상태 확인 시작");

                    await connection.ExecuteAsync(
                        "CheckSensorConnections",
                        commandType: CommandType.StoredProcedure,
                        commandTimeout: 300); // 5분 타임아웃

                    _logger.LogInformation("센서 연결 상태 확인 완료");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "센서 연결 상태 확인 중 오류 발생");
                throw;
            }
        }

        public async Task CalculateDailySensorUptimeAsync()
        {
            try
            {
                using (var connection = await _connectionFactory.CreateConnectionAsync())
                {
                    _logger.LogInformation("일일 센서 가동 시간 계산 시작");

                    await connection.ExecuteAsync(
                        "CalculateDailySensorUptime",
                        commandType: CommandType.StoredProcedure,
                        commandTimeout: 600); // 10분 타임아웃

                    _logger.LogInformation("일일 센서 가동 시간 계산 완료");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "일일 센서 가동 시간 계산 중 오류 발생");
                throw;
            }
        }

        public async Task PurgeOldDataAsync()
        {
            try
            {
                using (var connection = await _connectionFactory.CreateConnectionAsync())
                {
                    _logger.LogInformation("오래된 데이터 삭제 시작");

                    // 데이터가 많을 수 있으므로 여러 번 실행
                    int totalDeleted = 0;
                    int batchDeleted;

                    do
                    {
                        var result = await connection.QueryAsync<dynamic>(
                            "PurgeOldData",
                            commandType: CommandType.StoredProcedure,
                            commandTimeout: 1200); // 20분 타임아웃

                        // DataPurgeHistory에서 방금 삭제된 행 수 확인
                        batchDeleted = await connection.QuerySingleOrDefaultAsync<int>(
                            @"SELECT ISNULL(SUM(RowsDeleted), 0) 
                          FROM DataPurgeHistory 
                          WHERE ExecutionTime >= DATEADD(MINUTE, -1, GETDATE())");

                        totalDeleted += batchDeleted;

                        if (batchDeleted > 0)
                        {
                            _logger.LogInformation($"배치 삭제 완료: {batchDeleted}행");
                            await Task.Delay(1000); // 1초 대기
                        }

                    } while (batchDeleted >= 10000); // 10000건 미만이면 종료

                    _logger.LogInformation($"오래된 데이터 삭제 완료. 총 {totalDeleted}행 삭제됨");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "오래된 데이터 삭제 중 오류 발생");
                throw;
            }
        }
    }

    // 센서 연결 확인 백그라운드 서비스 (주기적 실행)
    public class SensorConnectionCheckerService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SensorConnectionCheckerService> _logger;
        private readonly BackgroundJobSettings _settings;

        public SensorConnectionCheckerService(
            IServiceProvider serviceProvider,
            ILogger<SensorConnectionCheckerService> logger,
            IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _settings = configuration.GetSection("BackgroundJobs").Get<BackgroundJobSettings>();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_settings?.CheckSensorConnections?.Enabled ?? false)
            {
                _logger.LogInformation("센서 연결 확인 서비스가 비활성화되어 있습니다.");
                return;
            }

            var interval = TimeSpan.FromMinutes(_settings.CheckSensorConnections.IntervalMinutes);
            _logger.LogInformation($"센서 연결 확인 서비스 시작. 실행 간격: {interval}");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var repository = scope.ServiceProvider.GetRequiredService<IMaintenanceRepository>();

                    await repository.CheckSensorConnectionsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "센서 연결 확인 중 오류 발생");
                }

                await Task.Delay(interval, stoppingToken);
            }
        }
    }

    // 일일 작업 백그라운드 서비스 (특정 시간 실행)
    public class DailyMaintenanceService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DailyMaintenanceService> _logger;
        private readonly BackgroundJobSettings _settings;

        public DailyMaintenanceService(
            IServiceProvider serviceProvider,
            ILogger<DailyMaintenanceService> logger,
            IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _settings = configuration.GetSection("BackgroundJobs").Get<BackgroundJobSettings>();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("일일 유지보수 서비스 시작");

            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.Now;
                var nextRun = GetNextRunTime(now);
                var delay = nextRun - now;

                _logger.LogInformation($"다음 실행 시간: {nextRun:yyyy-MM-dd HH:mm:ss}");

                await Task.Delay(delay, stoppingToken);

                if (!stoppingToken.IsCancellationRequested)
                {
                    await RunDailyJobs();
                }
            }
        }

        private DateTime GetNextRunTime(DateTime currentTime)
        {
            var times = new[]
            {
                (_settings?.CalculateDailySensorUptime?.Enabled ?? false) ?
                    _settings.CalculateDailySensorUptime.ExecutionTime : (TimeSpan?)null,
                (_settings?.PurgeOldData?.Enabled ?? false) ?
                    _settings.PurgeOldData.ExecutionTime : (TimeSpan?)null
            }.Where(t => t.HasValue).Select(t => t.Value).ToArray();

            if (!times.Any())
            {
                // 활성화된 작업이 없으면 다음날 자정
                return currentTime.Date.AddDays(1);
            }

            var nextRunTimes = times.Select(time =>
            {
                var runTime = currentTime.Date.Add(time);
                return runTime > currentTime ? runTime : runTime.AddDays(1);
            });

            return nextRunTimes.Min();
        }

        private async Task RunDailyJobs()
        {
            var now = DateTime.Now;
            var currentTime = now.TimeOfDay;

            using var scope = _serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IMaintenanceRepository>();

            // 가동 시간 계산 (보통 새벽 1시)
            if (_settings?.CalculateDailySensorUptime?.Enabled ?? false)
            {
                var uptimeExecutionTime = _settings.CalculateDailySensorUptime.ExecutionTime;
                if (IsTimeToRun(currentTime, uptimeExecutionTime))
                {
                    try
                    {
                        _logger.LogInformation("일일 센서 가동 시간 계산 작업 시작");
                        await repository.CalculateDailySensorUptimeAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "일일 센서 가동 시간 계산 중 오류 발생");
                    }
                }
            }

            // 데이터 삭제 (보통 새벽 3시)
            if (_settings?.PurgeOldData?.Enabled ?? false)
            {
                var purgeExecutionTime = _settings.PurgeOldData.ExecutionTime;
                if (IsTimeToRun(currentTime, purgeExecutionTime))
                {
                    try
                    {
                        _logger.LogInformation("오래된 데이터 삭제 작업 시작");
                        await repository.PurgeOldDataAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "오래된 데이터 삭제 중 오류 발생");
                    }
                }
            }
        }

        private bool IsTimeToRun(TimeSpan currentTime, TimeSpan scheduledTime)
        {
            // 현재 시간이 예정 시간의 ±30초 이내인지 확인
            var difference = Math.Abs((currentTime - scheduledTime).TotalSeconds);
            return difference <= 30;
        }
    }

    // Startup.cs 또는 Program.cs에서 서비스 등록
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddMaintenanceServices(this IServiceCollection services)
        {
            // Repository 등록
            services.AddScoped<IMaintenanceRepository, MaintenanceRepository>();

            // Background Services 등록
            services.AddHostedService<SensorConnectionCheckerService>();
            services.AddHostedService<DailyMaintenanceService>();

            return services;
        }
    }
}
