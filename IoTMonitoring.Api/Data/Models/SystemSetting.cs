namespace IoTMonitoring.Api.Data.Models
{
    public class SystemSetting
    {
        public int SettingID { get; set; }
        public string SettingKey { get; set; }
        public string SettingValue { get; set; }
        public string DataType { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }

        // 탐색 속성
        public User UpdatedByUser { get; set; }
    }
}