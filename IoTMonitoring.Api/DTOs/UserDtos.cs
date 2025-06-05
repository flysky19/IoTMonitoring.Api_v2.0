// DTOs/UserDtos.cs
namespace IoTMonitoring.Api.DTOs
{
    // 사용자 기본 정보 DTO
    public class UserDto
    {
        public int UserID { get; set; }

        public string Username { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }

        public string FullName { get; set; }  // 전체 이름

        public string Phone { get; set; }

        public List<int> CompanyIDs { get; set; }    // ✅ 다중 회사

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? LastLogin { get; set; }
    }

    //// 사용자 상세 정보 DTO
    //public class UserDetailDto : UserDto
    //{
    //    public string Phone { get; set; }
    //    public DateTime CreatedAt { get; set; }
    //    public DateTime? LastLogin { get; set; }
    //    public List<CompanyDto> AssignedCompanies { get; set; }
    //}

    // 사용자 생성 DTO
    public class UserCreateDto
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string Phone { get; set; }
        public bool Active { get; set; } = true;
        public string Role { get; set; }
        public List<int> CompanyIDs { get; set; }
    }

    // 사용자 업데이트 DTO
    public class UserUpdateDto
    {
        public string Email { get; set; }
        public string FullName { get; set; }
        public string Phone { get; set; }
        public bool IsActive { get; set; }
        public string Role { get; set; }
        public int[] CompanyIDs { get; set; }
    }

    // 비밀번호 변경 DTO
    public class PasswordChangeDto
    {
        public string CurrentPassword { get; set; }
        public string NewPassword { get; set; }
        public string ConfirmPassword { get; set; }
    }

    // 사용자 프로필 업데이트 DTO (일반 사용자가 자신의 정보만 수정할 때 사용)
    public class UserProfileUpdateDto
    {
        public string Email { get; set; }
        public string FullName { get; set; }
        public string Phone { get; set; }
    }

    // 관리자의 비밀번호 리셋 DTO
    public class PasswordResetDto
    {
        public string NewPassword { get; set; }
    }

    // 회사 할당 변경 DTO
    public class UserCompanyAssignmentDto
    {
        public int[] CompanyIDs { get; set; }
    }

    // 역할 변경 DTO
    public class UserRoleAssignmentDto
    {
        public string[] Roles { get; set; }
    }
}