using System;
using System.Threading.Tasks;
using Dapper;
using IoTMonitoring.Api.Data.Connection;
using IoTMonitoring.Api.Data.Models;
using IoTMonitoring.Api.Data.Repositories.Interfaces;

namespace IoTMonitoring.Api.Data.Repositories
{
    public class SensorMqttTopicRepository : ISensorMqttTopicRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public SensorMqttTopicRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<SensorMqttTopic> GetBySensorIdAsync(int sensorId)
        {
            using (var connection = await _connectionFactory.CreateConnectionAsync())
            {
                return await connection.QuerySingleOrDefaultAsync<SensorMqttTopic>(
                    "SELECT * FROM SensorMqttTopics WHERE SensorID = @SensorId",
                    new { SensorId = sensorId });
            }
        }

        public async Task<SensorMqttTopic> GetByIdAsync(int topicId)
        {
            using (var connection = await _connectionFactory.CreateConnectionAsync())
            {
                return await connection.QuerySingleOrDefaultAsync<SensorMqttTopic>(
                    "SELECT * FROM SensorMqttTopics WHERE TopicID = @TopicId",
                    new { TopicId = topicId });
            }
        }

        public async Task<int> CreateAsync(SensorMqttTopic mqttTopic)
        {
            using (var connection = await _connectionFactory.CreateConnectionAsync())
            {
                const string sql = @"
                    INSERT INTO SensorMqttTopics (
                        SensorID, DataTopic, ControlTopic, StatusTopic, HeartbeatTopic, 
                        QoS, Retained, CreatedAt
                    ) 
                    OUTPUT INSERTED.TopicID
                    VALUES (
                        @SensorId, @DataTopic, @ControlTopic, @StatusTopic, @HeartbeatTopic,
                        @QoS, @Retained, @CreatedAt
                    )";

                return await connection.QuerySingleAsync<int>(sql, mqttTopic);
            }
        }

        public async Task<bool> UpdateAsync(SensorMqttTopic mqttTopic)
        {
            using (var connection = await _connectionFactory.CreateConnectionAsync())
            {
                const string sql = @"
                    UPDATE SensorMqttTopics SET
                        DataTopic = @DataTopic,
                        ControlTopic = @ControlTopic,
                        StatusTopic = @StatusTopic,
                        HeartbeatTopic = @HeartbeatTopic,
                        QoS = @QoS,
                        Retained = @Retained,
                        UpdatedAt = @UpdatedAt
                    WHERE TopicID = @TopicId";

                var rowsAffected = await connection.ExecuteAsync(sql, mqttTopic);
                return rowsAffected > 0;
            }
        }

        public async Task<bool> DeleteBySensorIdAsync(int sensorId)
        {
            using (var connection = await _connectionFactory.CreateConnectionAsync())
            {
                var rowsAffected = await connection.ExecuteAsync(
                    "DELETE FROM SensorMqttTopics WHERE SensorID = @SensorId",
                    new { SensorId = sensorId });

                return rowsAffected > 0;
            }
        }

        public async Task<bool> DeleteByIdAsync(int topicId)
        {
            using (var connection = await _connectionFactory.CreateConnectionAsync())
            {
                var rowsAffected = await connection.ExecuteAsync(
                    "DELETE FROM SensorMqttTopics WHERE TopicID = @TopicId",
                    new { TopicId = topicId });

                return rowsAffected > 0;
            }
        }
    }
}