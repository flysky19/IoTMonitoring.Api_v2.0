namespace IoTMonitoring.Api.Utilities
{
    public class ConfigurationValidator
    {
        public static void ValidateConfiguration(IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
                throw new InvalidOperationException("DefaultConnection is not configured");

            var jwtSecret = configuration["JwtSettings:SecretKey"];
            if (string.IsNullOrEmpty(jwtSecret) || jwtSecret.Length < 32)
                throw new InvalidOperationException("JWT SecretKey must be at least 32 characters");

            var mqttHost = configuration["MqttSettings:BrokerHost"];
            if (string.IsNullOrEmpty(mqttHost))
                throw new InvalidOperationException("MQTT BrokerHost is not configured");

            var configLic = configuration["License:SecretKey"];
            if (string.IsNullOrEmpty(configLic))
                throw new InvalidOperationException("SyncFusion License is not configured");
        }
    }
}
