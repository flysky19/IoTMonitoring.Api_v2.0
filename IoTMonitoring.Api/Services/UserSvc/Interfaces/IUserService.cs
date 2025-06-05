using IoTMonitoring.Api.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IoTMonitoring.Api.Services.UserSvr.Interfaces
{
    public interface IUserService
    {
        // 기본 CRUD 작업
        Task<IEnumerable<UserDto>> GetAllUsersAsync(bool includeInactive = false);
        Task<UserDto> GetUserByIdAsync(int id);
        Task<UserDto> CreateUserAsync(UserCreateDto userDto);
        Task UpdateUserAsync(int id, UserUpdateDto userDto);
        Task DeactivateUserAsync(int id);
        Task ActivateUserAsync(int id);
        
        // 프로필 관련
        Task UpdateUserProfileAsync(int userId, UserProfileUpdateDto profileDto);
        
        // 비밀번호 관련
        Task ChangePasswordAsync(int userId, PasswordChangeDto passwordDto);
        Task ResetPasswordAsync(int userId, string newPassword);
        
        // 역할 관리
        Task UpdateUserRolesAsync(int userId, string[] roles);
        
        // 회사 할당 관리
        Task UpdateUserCompaniesAsync(int userId, int[] companyIds);
        
        // 인증 관련
        Task<UserDto> AuthenticateAsync(string username, string password);
        Task<bool> ValidatePasswordAsync(int userId, string password);
        
        // 유효성 검사
        Task<bool> CheckUsernameExistsAsync(string username, int? excludeUserId = null);
        Task<bool> CheckEmailExistsAsync(string email, int? excludeUserId = null);

        Task DeleteUserAsync(int id);
        Task<bool> CanDeleteUserAsync(int id);
    }
}