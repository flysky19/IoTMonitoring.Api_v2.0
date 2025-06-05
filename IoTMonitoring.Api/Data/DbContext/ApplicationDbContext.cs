using IoTMonitoring.Api.Data.Models;
using Microsoft.EntityFrameworkCore;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace IoTMonitoring.Api.Data.DbContext
{
    public class ApplicationDbContext : Microsoft.EntityFrameworkCore.DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<UserCompany> UserCompanies { get; set; }
        public DbSet<SensorGroup> SensorGroups { get; set; }
        public DbSet<Sensor> Sensors { get; set; }
        public DbSet<ParticleData> ParticleData { get; set; }
        public DbSet<WindData> WindData { get; set; }
        public DbSet<TempHumidityData> TempHumidityData { get; set; }
        public DbSet<SpeakerStatus> SpeakerStatuses { get; set; }
        public DbSet<SpeakerControlHistory> SpeakerControlHistories { get; set; }
        public DbSet<SensorConnectionHistory> SensorConnectionHistories { get; set; }
        public DbSet<SensorMqttTopic> SensorMqttTopics { get; set; }
        public DbSet<MqttSetting> MqttSettings { get; set; }
        public DbSet<MqttMessageLog> MqttMessageLogs { get; set; }
        public DbSet<AlertSetting> AlertSettings { get; set; }
        public DbSet<AlertHistory> AlertHistories { get; set; }
        public DbSet<SensorUptimeStat> SensorUptimeStats { get; set; }
        public DbSet<DailyStatistic> DailyStatistics { get; set; }
        public DbSet<SystemSetting> SystemSettings { get; set; }
        public DbSet<DataPurgeHistory> DataPurgeHistories { get; set; }

        // 기타 DbSet 속성들

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 테이블 이름 매핑
            modelBuilder.Entity<User>().ToTable("Users");
            modelBuilder.Entity<Company>().ToTable("Companies");
            modelBuilder.Entity<UserCompany>().ToTable("UserCompanies")
                .HasKey(uc => new { uc.UserID, uc.CompanyID });
            modelBuilder.Entity<SensorGroup>().ToTable("SensorGroups");
            modelBuilder.Entity<Sensor>().ToTable("Sensors");
            modelBuilder.Entity<ParticleData>().ToTable("ParticleData");
            modelBuilder.Entity<WindData>().ToTable("WindData");
            modelBuilder.Entity<TempHumidityData>().ToTable("TempHumidityData");
            modelBuilder.Entity<SpeakerStatus>().ToTable("SpeakerStatus");
            modelBuilder.Entity<SpeakerControlHistory>().ToTable("SpeakerControlHistory");
            modelBuilder.Entity<SensorConnectionHistory>().ToTable("SensorConnectionHistory");
            modelBuilder.Entity<SensorMqttTopic>().ToTable("SensorMqttTopics");
            modelBuilder.Entity<MqttSetting>().ToTable("MqttSettings");
            modelBuilder.Entity<MqttMessageLog>().ToTable("MqttMessageLogs");
            modelBuilder.Entity<AlertSetting>().ToTable("AlertSettings");
            modelBuilder.Entity<AlertHistory>().ToTable("AlertHistory");
            modelBuilder.Entity<SensorUptimeStat>().ToTable("SensorUptimeStats");
            modelBuilder.Entity<DailyStatistic>().ToTable("DailyStatistics");
            modelBuilder.Entity<SystemSetting>().ToTable("SystemSettings");
            modelBuilder.Entity<DataPurgeHistory>().ToTable("DataPurgeHistory");

            // 관계 설정
            modelBuilder.Entity<UserCompany>()
                .HasOne(uc => uc.User)
                .WithMany()
                .HasForeignKey(uc => uc.UserID);

            modelBuilder.Entity<UserCompany>()
                .HasOne(uc => uc.Company)
                .WithMany(c => c.UserCompanies)
                .HasForeignKey(uc => uc.CompanyID);

            modelBuilder.Entity<SensorGroup>()
                .HasOne(sg => sg.Company)
                .WithMany(c => c.SensorGroups)
                .HasForeignKey(sg => sg.CompanyID);

            modelBuilder.Entity<Sensor>()
                .HasOne(s => s.SensorGroup)
                .WithMany(sg => sg.Sensors)
                .HasForeignKey(s => s.GroupID);

            // 기타 설정
        }
    }
}