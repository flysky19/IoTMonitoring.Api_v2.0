namespace IoTMonitoring.Api.Data.Models
{
    public class UserCompany
    {
        public int UserID { get; set; }
        public int CompanyID { get; set; }
        public DateTime CreatedAt { get; set; }

        // 탐색 속성
        public User User { get; set; }
        public Company Company { get; set; }
    }
}