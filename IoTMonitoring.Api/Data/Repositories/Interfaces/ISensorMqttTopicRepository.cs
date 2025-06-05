using System.Threading.Tasks;
using IoTMonitoring.Api.Data.Models;

namespace IoTMonitoring.Api.Data.Repositories.Interfaces
{
    public interface ISensorMqttTopicRepository
    {
        Task<SensorMqttTopic> GetBySensorIdAsync(int sensorId);
        Task<SensorMqttTopic> GetByIdAsync(int topicId);
        Task<int> CreateAsync(SensorMqttTopic mqttTopic);
        Task<bool> UpdateAsync(SensorMqttTopic mqttTopic);
        Task<bool> DeleteBySensorIdAsync(int sensorId);
        Task<bool> DeleteByIdAsync(int topicId);
    }
}