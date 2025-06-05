using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IoTMonitoring.Api.Data.Models
{
    [Table("SensorGroups")]
    public class SensorGroup
    {
        [Key]
        public int GroupID { get; set; }

        public int? CompanyID { get; set; }

        [Required]
        [StringLength(100)]
        public string GroupName { get; set; }

        [StringLength(255)]
        public string Location { get; set; }

        public string Description { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        [Required]
        public bool Active { get; set; } = true;

        // Navigation Properties
        [ForeignKey("CompanyID")]
        public virtual Company Company { get; set; }

        public virtual ICollection<Sensor> Sensors { get; set; } = new List<Sensor>();
    }
}