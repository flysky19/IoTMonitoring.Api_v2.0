using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace IoTMonitoring.Api.Data.Models
{
    [Table("TempHumidityData")]
    public class TempHumidityData
    {
        [Key]
        public long DataID { get; set; }

        [Required]
        public int SensorID { get; set; }

        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public float? Temperature { get; set; }

        public float? Humidity { get; set; }

        public string? RawData { get; set; }

        // 외래키 관계
        [ForeignKey("SensorID")]
        public virtual Sensor Sensor { get; set; }
    }
}