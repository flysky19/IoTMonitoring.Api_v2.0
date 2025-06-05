using System;
using System.Text;
using IoTMonitoring.Api.Data.Connection;
using IoTMonitoring.Api.Data.Repositories;
using IoTMonitoring.Api.Data.Repositories.Interfaces;
using IoTMonitoring.Api.Services.Auth;
using IoTMonitoring.Api.Services.Auth.Interfaces;
using IoTMonitoring.Api.Services.Logging.Interfaces;
using IoTMonitoring.Api.Services.Logging;
using IoTMonitoring.Api.Services.Security;
using IoTMonitoring.Api.Services.Security.Interfaces;
using IoTMonitoring.Api.Services.Security.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using IoTMonitoring.Api.Services.Sensor.Interfaces;
using IoTMonitoring.Api.Services.Sensor;
using IoTMonitoring.Api.Mappers.Interfaces;
using IoTMonitoring.Api.Mappers;
using IoTMonitoring.Api.Services.MQTT.Interfaces;
using IoTMonitoring.Api.Services.MQTT;
using IoTMonitoring.Api.Services.SensorGroup.Interfaces;
using IoTMonitoring.Api.Services.SensorGroup;
using Microsoft.EntityFrameworkCore;
using IoTMonitoring.Api.Services.UserSvr;
using IoTMonitoring.Api.Services.UserSvr.Interfaces;
using IoTMonitoring.Api.Services.Interfaces;
using IoTMonitoring.Api.Services.CompanySvc.Interfaces;
using IoTMonitoring.Api.Services.CompanySvc;
using System.Security.Claims;

namespace IoTMonitoring.Api.Extensions
{
    public static class ServiceExtensions
    {
        public static void ConfigureAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            // JWT 설정 추가
            var jwtSettingsSection = configuration.GetSection("JwtSettings");
            services.Configure<JwtSettings>(jwtSettingsSection);
            var jwtSettings = jwtSettingsSection.Get<JwtSettings>();
            var key = Encoding.ASCII.GetBytes(jwtSettings.SecretKey);

            // JWT 인증 추가
            services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(x =>
            {
                x.RequireHttpsMetadata = false;
                x.SaveToken = true;
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    ClockSkew = TimeSpan.Zero,

                    // Role 클레임 타입 명시 - 이 부분이 중요!
                    RoleClaimType = ClaimTypes.Role,
                    NameClaimType = ClaimTypes.Name
                };
                // JWT 토큰 이벤트 핸들러 (디버깅용)
                x.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        Console.WriteLine($"Authentication failed: {context.Exception.Message}");
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        var claimsIdentity = context.Principal.Identity as ClaimsIdentity;
                        if (claimsIdentity != null)
                        {
                            Console.WriteLine($"Token validated for user: {claimsIdentity.Name}");
                            foreach (var claim in claimsIdentity.Claims)
                            {
                                Console.WriteLine($"Claim: {claim.Type} = {claim.Value}");
                            }
                        }
                        return Task.CompletedTask;
                    }
                };

            });
        }

        public static void ConfigureRepositories(this IServiceCollection services)
        {
            try
            {
                Console.WriteLine("리포지토리 서비스 등록 중...");

                // 기존 리포지토리
                services.AddScoped<IUserRepository, UserRepository>();

                // 센서 관련 리포지토리
                services.AddScoped<ISensorRepository, SensorRepository>();
                services.AddScoped<ISensorDataRepository, SensorDataRepository>();
                services.AddScoped<ISensorGroupRepository, SensorGroupRepository>();
                services.AddScoped<ISensorMqttTopicRepository, SensorMqttTopicRepository>();
                services.AddScoped<ISensorConnectionHistoryRepository, SensorConnectionHistoryRepository>();
                services.AddScoped<ICompanyRepository, CompanyRepository>();

                Console.WriteLine("모든 리포지토리 등록 등록 완료");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"리포지토리 설정 오류: {ex.Message}");
                throw;
            }

           
        }

        public static void ConfigureServices(this IServiceCollection services)
        {
            try
            {
                Console.WriteLine("비즈니스 서비스 등록 중...");

                // 인증 관련 서비스
                services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
                services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
                services.AddScoped<IAuthService, AuthService>();

                services.AddScoped<IUserService, UserService>();

                // 센서 관련 서비스 (간단한 구현 사용)
                services.AddScoped<ISensorService, SensorService>();
                services.AddScoped<ISensorGroupService, SensorGroupService>();

                // MQTT 서비스
                services.AddScoped<IMqttService, MqttService>();

                //services.AddSingleton<ISensorDataService, SensorDataService>();
                services.AddScoped<ICompanyService, CompanyService>();

                Console.WriteLine("모든 비즈니스 서비스 등록 완료");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"서비스 설정 오류: {ex.Message}");
                throw;
            }

        }

        // 매퍼 설정 (선택사항)
        public static void ConfigureMappers(this IServiceCollection services)
        {
            try
            {
                Console.WriteLine("매퍼 서비스 등록 중...");
                // 매퍼가 구현되면 주석 해제
                services.AddScoped<IUserMapper, UserMapper>();

                services.AddScoped<ISensorMapper, SensorMapper>();
                services.AddScoped<ISensorGroupMapper, SensorGroupMapper>();

                Console.WriteLine("매퍼 서비스 등록 완료 (스킵)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"매퍼 설정 오류: {ex.Message}");
                throw;
            }
        }

        public static void ConfigureDatabase(this IServiceCollection services, IConfiguration configuration)
        {
            try
            {
                Console.WriteLine("데이터베이스 연결 팩토리 등록 중...");

                services.AddSingleton<IDbConnectionFactory>(sp =>
                new SqlConnectionFactory(configuration.GetConnectionString("DefaultConnection")));

                
                //services.AddDbContext<ApplicationDbContext>(options =>
                //options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

                Console.WriteLine("데이터베이스 연결 팩토리 등록 완료");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"데이터베이스 설정 오류: {ex.Message}");
                throw;
            }
        }

        public static void ConfigureLogging(this IServiceCollection services)
        {
            try
            {
                Console.WriteLine("로깅 서비스 등록 중...");
                services.AddSingleton<IAppLogger, AppLogger>();
                Console.WriteLine("로깅 서비스 등록 완료");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"로깅 설정 오류: {ex.Message}");
                throw;
            }
        }
    }
}