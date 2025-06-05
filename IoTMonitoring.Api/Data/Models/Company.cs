namespace IoTMonitoring.Api.Data.Models
{
    public class Company
    {
        public int CompanyID { get; set; }
        public string CompanyName { get; set; }
        public string Address { get; set; }
        public string ContactPerson { get; set; }
        public string ContactPhone { get; set; }
        public string ContactEmail { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool Active { get; set; }

        // 탐색 속성
        public List<SensorGroup> SensorGroups { get; set; } = new List<SensorGroup>();
        public ICollection<UserCompany> UserCompanies { get; set; }
    }
}