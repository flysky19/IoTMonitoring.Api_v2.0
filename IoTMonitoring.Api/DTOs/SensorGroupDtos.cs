// DTOs/SensorGroupDtos.cs
namespace IoTMonitoring.Api.DTOs
{
    // 센서 그룹 기본 정보 DTO
    public class SensorGroupDto
    {
        public int GroupID { get; set; }
        public int? CompanyID { get; set; }
        public string CompanyName { get; set; }

        public string GroupName { get; set; }
        public string Location { get; set; }
        public bool IsActive { get; set; }
        public int SensorCount { get; set; }
    }

    // 센서 그룹 상세 정보 DTO
    public class SensorGroupDetailDto : SensorGroupDto
    {
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public Dictionary<string, int> SensorCountByType { get; set; }
        public Dictionary<string, int> SensorCountByStatus { get; set; }
    }

    // 센서 그룹 생성 DTO
    public class SensorGroupCreateDto
    {
        public int CompanyID { get; set; }
        public string GroupName { get; set; }
        public string Location { get; set; }
        public string Description { get; set; }
        public bool Active { get; set; } = true;
    }

    // 센서 그룹 업데이트 DTO
    public class SensorGroupUpdateDto
    {
        public string GroupName { get; set; }
        public string Location { get; set; }
        public string Description { get; set; }
        public bool Active { get; set; }
    }

    public class UpdateGroupMembersDto
    {
        public int GroupID { get; set; }
        public List<int> AddSensorIds { get; set; } = new List<int>();
        public List<int> RemoveSensorIds { get; set; } = new List<int>();
    }
}