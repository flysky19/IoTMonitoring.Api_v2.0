using System;
using IoTMonitoring.Api.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using IoTMonitoring.Api.Services.MQTT.Interfaces;
using IoTMonitoring.Api.Data.DbContext;
using Microsoft.EntityFrameworkCore;
using IoTMonitoring.Api.Services.SignalR.Interfaces;
using IoTMonitoring.Api.Services.SignalR;
using IoTMonitoring.Api.Hubs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Mvc;
using IoTMonitoring.Api.Utilities;
using IoTMonitoring.Api.Data.RateLimit;
using IoTMonitoring.Api.Middlewares;
using IoTMonitoring.Api.Services.RateLimit;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddConsole();
var logger = LoggerFactory.Create(config => config.AddConsole()).CreateLogger("Program");
logger.LogInformation($"Current Environment: {builder.Environment.EnvironmentName}");


// 설정 로드 순서
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddJsonFile("appsettings.Local.json", optional: true) // Git에서 제외됨
    .AddEnvironmentVariables();


try
{
    ConfigurationValidator.ValidateConfiguration(builder.Configuration);
    Console.WriteLine("✅ 설정 검증 완료");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ 설정 오류: {ex.Message}");
    Environment.Exit(1); // 잘못된 설정이면 앱 시작 중단
}

// Rate limiting 서비스 추가
builder.Services.AddRateLimiting(builder.Configuration);


// 서비스 등록
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger 설정
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "IoT Monitoring API",
        Version = "v1",
        Description = "IoT 센서 모니터링 시스템 API"
    });

    // JWT 인증 설정
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});


builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin",
        builder => builder
            .WithOrigins("http://localhost:3000", "https://yourdomain.com")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials());
});

// 사용자 정의 서비스 등록 (순서 중요!)
try
{
    Console.WriteLine("데이터베이스 설정 중...");
    builder.Services.ConfigureDatabase(builder.Configuration);
    //builder.Services.AddDbContext<ApplicationDbContext>(options =>
    //options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


    Console.WriteLine("로깅 설정 중...");
    builder.Services.ConfigureLogging();

    Console.WriteLine("매퍼 설정 중...");
    builder.Services.ConfigureMappers();

    Console.WriteLine("리포지토리 설정 중...");
    builder.Services.ConfigureRepositories();

    Console.WriteLine("서비스 설정 중...");
    builder.Services.ConfigureServices();

    Console.WriteLine("인증 설정 중...");
    builder.Services.ConfigureAuthentication(builder.Configuration);
    builder.Services.AddAuthorization();

    Console.WriteLine("모든 서비스 등록 완료!");
}
catch (Exception ex)
{
    Console.WriteLine($"서비스 등록 중 오류: {ex.Message}");
    Console.WriteLine($"스택 트레이스: {ex.StackTrace}");
    throw;
}

// SignalR 서비스 등록
builder.Services.AddSignalR();
builder.Services.AddScoped<ISignalRService, SignalRService>();

var app = builder.Build();

Console.WriteLine($"현재 환경: {app.Environment.EnvironmentName}");
Console.WriteLine($"IsDevelopment: {app.Environment.IsDevelopment()}");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "IoT Monitoring API V1");
        c.RoutePrefix = "swagger"; // Swagger를 /swagger 경로로 제한
    });
}

app.UseStaticFiles();
app.UseDefaultFiles();
app.UseDefaultFiles(new DefaultFilesOptions
{
    DefaultFileNames = new List<string> { "login.html" }
});
app.UseRouting();

// 또는 특정 폴더 지정
//app.UseStaticFiles(new StaticFileOptions
//{
//    FileProvider = new PhysicalFileProvider(
//        Path.Combine(builder.Environment.ContentRootPath, "public")),
//    RequestPath = "/public"
//});


//app.UseHttpsRedirection();
app.UseCors("AllowSpecificOrigin");


app.MapGet("/debug", () => "API is running!");



using (var scope = app.Services.CreateScope())
{
    var mqttService = scope.ServiceProvider.GetRequiredService<IMqttService>();
    _ = Task.Run(async () => await mqttService.StartAsync());
}

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseRateLimiting(); // ⭐ 인증 전에 위치해야 함


Console.WriteLine("애플리케이션이 시작되었습니다.");
Console.WriteLine($"Swagger URL: https://localhost:7051/swagger");
Console.WriteLine($"Debug URL: https://localhost:7051/debug");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<SensorHub>("/sensorHub");// SignalR Hub 엔드포인트 매핑
app.MapFallbackToFile("login.html");  // 404 시 로그인 페이지로 리디렉션

app.Run();




public static class RateLimitingExtensions
{
    public static IServiceCollection AddRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        // 설정 바인딩
        services.Configure<RateLimitOptions>(configuration.GetSection("RateLimit"));

        // Rate limit 서비스 등록
        services.AddSingleton<IRateLimitService, MemoryRateLimitService>();

        return services;
    }

    public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RateLimitingMiddleware>();
    }
}