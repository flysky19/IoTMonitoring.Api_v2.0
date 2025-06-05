// DTOs/CompanyDtos.cs
namespace IoTMonitoring.Api.DTOs
{
    // 업체 기본 정보 DTO
    public class CompanyDto
    {
        public int CompanyID { get; set; }
        public string CompanyName { get; set; }
        public string Address { get; set; }
        public string ContactPerson { get; set; }
        public string ContactPhone { get; set; }
        public string ContactEmail { get; set; }
        public bool Active { get; set; }

        public int UserCount { get; set; }
        public int SensorCount { get; set; }
        public int GroupCount { get; set; }
    }

    // 업체 상세 정보 DTO
    public class CompanyDetailDto : CompanyDto
    {
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    // 업체 생성 DTO
    public class CompanyCreateDto
    {
        public string CompanyName { get; set; }
        public string Address { get; set; }
        public string ContactPerson { get; set; }
        public string ContactPhone { get; set; }
        public string ContactEmail { get; set; }
        public bool Active { get; set; }
    }

    // 업체 업데이트 DTO
    public class CompanyUpdateDto
    {
        public string CompanyName { get; set; }
        public string Address { get; set; }
        public string ContactPerson { get; set; }
        public string ContactPhone { get; set; }
        public string ContactEmail { get; set; }
        public bool Active { get; set; }
    }

    public class CompanySimpleDto
    {
        public int CompanyID { get; set; }
        public string CompanyName { get; set; }
        public bool Active { get; set; }
    }
}