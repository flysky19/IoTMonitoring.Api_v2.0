using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using IoTMonitoring.Api.Utilities;

namespace IoTMonitoring.Api.Data.Models
{
    [Table("ParticleData")]
    public class ParticleData
    {
        [Key]
        public long DataID { get; set; }

        [Required]
        public int SensorID { get; set; }

        [Required]
        public DateTime Timestamp { get; set; } = DateTimeHelper.Now;

        public float? PM1_0 { get; set; }
        public float? PM2_5 { get; set; }
        public float? PM4_0 { get; set; }
        public float? PM10_0 { get; set; }
        public float? PM_0_5 { get; set; }
        public float? PM_5_0 { get; set; }

        public string? RawData { get; set; }

        [ForeignKey("SensorID")]
        public virtual Sensor Sensor { get; set; }
    }
}