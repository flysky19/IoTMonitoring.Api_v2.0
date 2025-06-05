namespace IoTMonitoring.Api.Data.Models
{
    public class User
    {
        public int UserID { get; set; }
        public string Username { get; set; }
        public string Password { get; set; } // 해시된 비밀번호
        public string Email { get; set; }
        public string FullName { get; set; }
        public string Phone { get; set; }
        public string Role { get; set; } // 사용자 역할 (예: Admin, User 등)
        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? LastLogin { get; set; }
        public DateTime? UpdatedAt { get; set; }


        public ICollection<UserCompany> UserCompanies { get; set; } = new List<UserCompany>();

        public List<Company> AssignedCompanies { get; set; } = new List<Company>();

        public Company company => AssignedCompanies?.FirstOrDefault();
    }
}