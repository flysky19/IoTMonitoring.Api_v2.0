// DTOs/SensorDataDtos.cs
namespace IoTMonitoring.Api.DTOs
{
    // 파티클 데이터 DTO
    public class ParticleDataDto
    {
        public long DataID { get; set; }
        public int SensorID { get; set; }
        public DateTime Timestamp { get; set; }
        public float? PM0_3 { get; set; }
        public float? PM0_5 { get; set; }
        public float? PM1_0 { get; set; }
        public float? PM2_5 { get; set; }
        public float? PM5_0 { get; set; }
        public float? PM10 { get; set; }
        public string RawData { get; set; }
    }

    // 풍향 데이터 DTO
    public class WindDataDto
    {
        public long DataID { get; set; }
        public int SensorID { get; set; }
        public DateTime Timestamp { get; set; }
        public float? WindSpeed { get; set; }
        public string RawData { get; set; }
    }

    // 온습도 데이터 DTO
    public class TempHumidityDataDto
    {
        public long DataID { get; set; }
        public int SensorID { get; set; }
        public DateTime Timestamp { get; set; }
        public float? Temperature { get; set; }
        public float? Humidity { get; set; }
        public string RawData { get; set; }
    }

    // 파티클 데이터 생성 DTO
    public class ParticleDataCreateDto
    {
        public int SensorID { get; set; }

        public DateTime Timestamp { get; set; }

        public float? PM0_3 { get; set; }
        public float? PM0_5 { get; set; }
        public float? PM1_0 { get; set; }
        public float? PM2_5 { get; set; }
        public float? PM5_0 { get; set; }
        public float? PM10 { get; set; }
        public string RawData { get; set; }
    }

    // 풍향 데이터 생성 DTO
    public class WindDataCreateDto
    {
        public int SensorID { get; set; }
        public float? WindSpeed { get; set; }
        public string RawData { get; set; }

        public DateTime Timestamp { get; set; }
    }

    // 온습도 데이터 생성 DTO
    public class TempHumidityDataCreateDto
    {
        public int SensorID { get; set; }
        public DateTime Timestamp { get; set; }
        public float? Temperature { get; set; }
        public float? Humidity { get; set; }
        public string RawData { get; set; }
    }

    // 센서 연결 이력 DTO
    public class SensorConnectionHistoryDto
    {
        public long HistoryID { get; set; }
        public int SensorID { get; set; }
        public DateTime StatusChangeTime { get; set; }
        public string OldStatus { get; set; }
        public string NewStatus { get; set; }
        public string ChangeReason { get; set; }
    }

    // 집계된 데이터 DTO
    public class AggregatedDataDto
    {
        public DateTime TimePeriod { get; set; }
        public string AggregationType { get; set; } // Hourly, Daily, Monthly
        public Dictionary<string, float?> MinValues { get; set; }
        public Dictionary<string, float?> MaxValues { get; set; }
        public Dictionary<string, float?> AvgValues { get; set; }
        public int DataPointCount { get; set; }
    }
}