using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using IoTMonitoring.Api.Data.Models;
using IoTMonitoring.Api.Data.Repositories.Interfaces;
using IoTMonitoring.Api.DTOs;
using IoTMonitoring.Api.Services.Logging.Interfaces;
using IoTMonitoring.Api.Mappers.Interfaces;
using IoTMonitoring.Api.Services.Security.Interfaces;
using IoTMonitoring.Api.Services.UserSvr.Interfaces;

namespace IoTMonitoring.Api.Services.UserSvr
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IUserMapper _userMapper;
        private readonly IAppLogger _logger;
        private readonly IPasswordHasher _passwordHasher;

        public UserService(
            IUserRepository userRepository,
            IUserMapper userMapper,
            IPasswordHasher passwordHasher,
            IAppLogger logger)
        {
            _userRepository = userRepository;
            _userMapper = userMapper;
            _passwordHasher = passwordHasher;
            _logger = logger;
        }

        #region 기본 CRUD 작업

        public async Task<IEnumerable<UserDto>> GetAllUsersAsync(bool includeInactive = false)
        {
            try
            {
                _logger.LogInformation($"사용자 목록 조회 시작 - includeInactive: {includeInactive}");

                var users = await _userRepository.GetUsersWithCompanyAsync(includeInactive);

                var userDtos = users.Select(u => new UserDto
                {
                    UserID = u.UserID,
                    Username = u.Username,
                    FullName = u.FullName,  // Name → FullName으로 수정
                    Email = u.Email,
                    Phone = u.Phone,
                    Role = u.Role,
                    CompanyIDs = u.UserCompanies?.Select(uc => uc.CompanyID).ToList() ?? new List<int>(),
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt,

                }).ToList();

                var count = users.Select(_userMapper.ToDto).ToList();

                _logger.LogInformation($"사용자 목록 조회 완료 - 조회된 사용자 수: {count.Count}");
                return userDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError($"사용자 목록 조회 중 오류: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<UserDto> GetUserByIdAsync(int id)
        {
            try
            {
                _logger.LogInformation($"사용자 상세 조회 시작 - ID: {id}");

                //var user = await _userRepository.GetUserWithCompanyAsync(id);
                var user = await _userRepository.GetByIdAsync(id);
                
                if (user == null)
                {
                    throw new KeyNotFoundException($"사용자 ID {id}를 찾을 수 없습니다.");
                }

                var result = _userMapper.ToDetailDto(user);

                // 사용자가 속한 회사 정보 추가
                //if (user.company != null)
                //{
                //    result.AssignedCompanies = new List<CompanyDto>
                //    {
                //        new CompanyDto
                //        {
                //            CompanyID = user.company.CompanyID,
                //            CompanyName = user.company.CompanyName,
                //            Active = user.company.Active
                //        }
                //    };
                //}

                _logger.LogInformation($"사용자 상세 조회 완료 - ID: {id}, Username: {result.Username}");
                return result;
            }
            catch (KeyNotFoundException)
            {
                _logger.LogWarning($"사용자를 찾을 수 없음 - ID: {id}");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError($"사용자 상세 조회 중 오류 (ID: {id}): {ex.Message}", ex);
                throw;
            }
        }

        public async Task<UserDto> CreateUserAsync(UserCreateDto userDto)
        {
            try
            {
                _logger.LogInformation($"사용자 생성 시작 - Username: {userDto.Username}");

                // 사용자명 중복 확인
                if (await _userRepository.ExistsByUsernameAsync(userDto.Username))
                {
                    throw new InvalidOperationException($"사용자명 {userDto.Username}이(가) 이미 존재합니다.");
                }

                // 이메일 중복 확인
                if (await _userRepository.ExistsByEmailAsync(userDto.Email))
                {
                    throw new InvalidOperationException($"이메일 {userDto.Email}이(가) 이미 사용 중입니다.");
                }

                // 엔티티 생성
                var user = _userMapper.ToEntity(userDto);

                // DB에 저장
                //var userId = await _userRepository.CreateAsync(user);
                var userId = await _userRepository.CreateAsync(user, userDto.CompanyIDs);
                user.UserID = userId;

                _logger.LogInformation($"사용자 생성 완료 - ID: {userId}, Username: {userDto.Username}");
                return _userMapper.ToDto(user);
            }
            catch (InvalidOperationException)
            {
                _logger.LogWarning($"사용자 생성 실패 - 중복된 사용자명 또는 이메일: {userDto.Username}");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError($"사용자 생성 중 오류: {ex.Message}", ex);
                throw;
            }
        }

        public async Task UpdateUserAsync(int id, UserUpdateDto userDto)
        {
            try
            {
                _logger.LogInformation($"사용자 수정 시작 - ID: {id}");

                var user = await _userRepository.GetByIdAsync(id);
                if (user == null)
                {
                    throw new KeyNotFoundException($"사용자 ID {id}를 찾을 수 없습니다.");
                }

                // 이메일 중복 확인 (자신 제외)
                if (!string.IsNullOrEmpty(userDto.Email) && user.Email != userDto.Email)
                {
                    if (await _userRepository.ExistsByEmailAsync(userDto.Email, id))
                    {
                        throw new InvalidOperationException("이미 사용 중인 이메일입니다.");
                    }
                }

                // 엔티티 업데이트
                _userMapper.UpdateEntity(user, userDto);
                await _userRepository.UpdateAsync(user, userDto.CompanyIDs.ToList());

                _logger.LogInformation($"사용자 수정 완료 - ID: {id}");
            }
            catch (KeyNotFoundException)
            {
                _logger.LogWarning($"사용자를 찾을 수 없음 - ID: {id}");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError($"사용자 수정 중 오류 (ID: {id}): {ex.Message}", ex);
                throw;
            }
        }

        public async Task DeactivateUserAsync(int id)
        {
            try
            {
                _logger.LogInformation($"사용자 비활성화 시작 - ID: {id}");

                var user = await _userRepository.GetByIdAsync(id);
                if (user == null)
                {
                    throw new KeyNotFoundException($"사용자 ID {id}를 찾을 수 없습니다.");
                }

                user.IsActive = false;
                user.UpdatedAt = DateTime.UtcNow;
                await _userRepository.UpdateAsync(user);

                _logger.LogInformation($"사용자 비활성화 완료 - ID: {id}");
            }
            catch (KeyNotFoundException)
            {
                _logger.LogWarning($"사용자를 찾을 수 없음 - ID: {id}");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError($"사용자 비활성화 중 오류 (ID: {id}): {ex.Message}", ex);
                throw;
            }
        }

        public async Task ActivateUserAsync(int id)
        {
            try
            {
                _logger.LogInformation($"사용자 활성화 시작 - ID: {id}");

                var user = await _userRepository.GetByIdAsync(id);
                if (user == null)
                {
                    throw new KeyNotFoundException($"사용자 ID {id}를 찾을 수 없습니다.");
                }

                user.IsActive = true;
                user.UpdatedAt = DateTime.UtcNow;
                await _userRepository.UpdateAsync(user);

                _logger.LogInformation($"사용자 활성화 완료 - ID: {id}");
            }
            catch (KeyNotFoundException)
            {
                _logger.LogWarning($"사용자를 찾을 수 없음 - ID: {id}");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError($"사용자 활성화 중 오류 (ID: {id}): {ex.Message}", ex);
                throw;
            }
        }

        #endregion

        #region 프로필 관련

        public async Task UpdateUserProfileAsync(int userId, UserProfileUpdateDto profileDto)
        {
            try
            {
                _logger.LogInformation($"사용자 프로필 수정 시작 - ID: {userId}");

                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    throw new KeyNotFoundException($"사용자 ID {userId}를 찾을 수 없습니다.");
                }

                // 이메일 중복 확인 (자신 제외)
                if (!string.IsNullOrEmpty(profileDto.Email) && user.Email != profileDto.Email)
                {
                    if (await _userRepository.ExistsByEmailAsync(profileDto.Email, userId))
                    {
                        throw new InvalidOperationException("이미 사용 중인 이메일입니다.");
                    }
                }

                _userMapper.UpdateEntity(user, profileDto);
                await _userRepository.UpdateAsync(user);

                _logger.LogInformation($"사용자 프로필 수정 완료 - ID: {userId}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"사용자 프로필 수정 중 오류 (ID: {userId}): {ex.Message}", ex);
                throw;
            }
        }

        #endregion

        #region 비밀번호 관련

        public async Task ChangePasswordAsync(int userId, PasswordChangeDto passwordDto)
        {
            try
            {
                _logger.LogInformation($"비밀번호 변경 시작 - UserID: {userId}");

                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    throw new KeyNotFoundException($"사용자 ID {userId}를 찾을 수 없습니다.");
                }

                // 현재 비밀번호 확인
                if (!await ValidatePasswordAsync(userId, passwordDto.CurrentPassword))
                {
                    throw new UnauthorizedAccessException("현재 비밀번호가 올바르지 않습니다.");
                }

                // 새 비밀번호 확인
                if (passwordDto.NewPassword != passwordDto.ConfirmPassword)
                {
                    throw new ArgumentException("새 비밀번호가 일치하지 않습니다.");
                }

                // 비밀번호 업데이트
                var newPasswordHash = _passwordHasher.HashPassword(passwordDto.NewPassword);
                await _userRepository.UpdatePasswordAsync(userId, newPasswordHash);

                _logger.LogInformation($"비밀번호 변경 완료 - UserID: {userId}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"비밀번호 변경 중 오류 (UserID: {userId}): {ex.Message}", ex);
                throw;
            }
        }

        public async Task ResetPasswordAsync(int userId, string newPassword)
        {
            try
            {
                _logger.LogInformation($"비밀번호 리셋 시작 - UserID: {userId}");

                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    throw new KeyNotFoundException($"사용자 ID {userId}를 찾을 수 없습니다.");
                }

                var passwordHash = _passwordHasher.HashPassword(newPassword);
                await _userRepository.UpdatePasswordAsync(userId, passwordHash);

                _logger.LogInformation($"비밀번호 리셋 완료 - UserID: {userId}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"비밀번호 리셋 중 오류 (UserID: {userId}): {ex.Message}", ex);
                throw;
            }
        }

        #endregion

        #region 역할 및 회사 관리

        public async Task UpdateUserRolesAsync(int userId, string[] roles)
        {
            try
            {
                _logger.LogInformation($"사용자 역할 변경 시작 - UserID: {userId}");

                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    throw new KeyNotFoundException($"사용자 ID {userId}를 찾을 수 없습니다.");
                }

                //user.Role = roles?.FirstOrDefault() ?? "User";
                user.UpdatedAt = DateTime.UtcNow;
                await _userRepository.UpdateAsync(user);

                _logger.LogInformation($"사용자 역할 변경 완료 - UserID: {userId}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"사용자 역할 변경 중 오류 (UserID: {userId}): {ex.Message}", ex);
                throw;
            }
        }

        public async Task UpdateUserCompaniesAsync(int userId, int[] companyIds)
        {
            try
            {
                _logger.LogInformation($"사용자 회사 할당 변경 시작 - UserID: {userId}");

                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    throw new KeyNotFoundException($"사용자 ID {userId}를 찾을 수 없습니다.");
                }

                //user.CompanyID = companyIds?.FirstOrDefault();
                user.UpdatedAt = DateTime.UtcNow;
                await _userRepository.UpdateAsync(user);

                _logger.LogInformation($"사용자 회사 할당 변경 완료 - UserID: {userId}, CompanyID: {user.company}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"사용자 회사 할당 변경 중 오류 (UserID: {userId}): {ex.Message}", ex);
                throw;
            }
        }

        #endregion

        #region 인증 관련

        public async Task<UserDto> AuthenticateAsync(string username, string password)
        {
            try
            {
                _logger.LogInformation($"사용자 인증 시작 - Username: {username}");

                var user = await _userRepository.GetByUsernameAsync(username);
                if (user == null || !user.IsActive)
                {
                    _logger.LogWarning($"인증 실패 - 사용자를 찾을 수 없거나 비활성 상태: {username}");
                    return null;
                }

                if (!_passwordHasher.VerifyPassword(password, user.Password))
                {
                    _logger.LogWarning($"인증 실패 - 잘못된 비밀번호: {username}");
                    return null;
                }

                // 마지막 로그인 시간 업데이트
                await _userRepository.UpdateLastLoginAsync(user.UserID);

                _logger.LogInformation($"사용자 인증 성공 - Username: {username}");
                return _userMapper.ToDto(user);
            }
            catch (Exception ex)
            {
                _logger.LogError($"사용자 인증 중 오류 (Username: {username}): {ex.Message}", ex);
                throw;
            }
        }

        public async Task<bool> ValidatePasswordAsync(int userId, string password)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null) return false;

                return _passwordHasher.VerifyPassword(password, user.Password);
            }
            catch (Exception ex)
            {
                _logger.LogError($"비밀번호 검증 중 오류 (UserID: {userId}): {ex.Message}", ex);
                return false;
            }
        }

        public async Task<bool> CheckUsernameExistsAsync(string username, int? excludeUserId = null)
        {
            return await _userRepository.ExistsByUsernameAsync(username, excludeUserId);
        }

        public async Task<bool> CheckEmailExistsAsync(string email, int? excludeUserId = null)
        {
            return await _userRepository.ExistsByEmailAsync(email, excludeUserId);
        }

        public async Task DeleteUserAsync(int id)
        {
            try
            {
                _logger.LogInformation($"사용자 삭제 시작 - ID: {id}");

                // 사용자 존재 확인
                var user = await _userRepository.GetByIdAsync(id);
                if (user == null)
                {
                    throw new KeyNotFoundException($"사용자 ID {id}를 찾을 수 없습니다.");
                }

                // 삭제 전 검증
                // 1. 시스템 관리자 계정은 삭제 불가
                if (user.Username?.ToLower() == "admin" || user.Username?.ToLower() == "system")
                {
                    throw new InvalidOperationException("시스템 관리자 계정은 삭제할 수 없습니다.");
                }

                // 2. 활성 세션이 있는지 확인 (선택사항)
                // if (await _sessionService.HasActiveSessionAsync(id))
                // {
                //     throw new InvalidOperationException("현재 로그인 중인 사용자는 삭제할 수 없습니다.");
                // }

                // 3. 관련 데이터 처리 (회사 연결 등)
                // UserCompanies 테이블의 레코드는 CASCADE DELETE로 자동 삭제되거나
                // 명시적으로 삭제해야 할 수도 있습니다.

                // 사용자 삭제
                await _userRepository.DeleteAsync(id);

                _logger.LogInformation($"사용자 삭제 완료 - ID: {id}, Username: {user.Username}");
            }
            catch (KeyNotFoundException)
            {
                _logger.LogWarning($"삭제할 사용자를 찾을 수 없음 - ID: {id}");
                throw;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning($"사용자 삭제 불가 - ID: {id}, 사유: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError($"사용자 삭제 중 오류 (ID: {id}): {ex.Message}", ex);
                throw new InvalidOperationException("사용자 삭제 중 오류가 발생했습니다.", ex);
            }
        }

        public async Task<bool> CanDeleteUserAsync(int id)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(id);
                if (user == null) return false;

                // 삭제 가능 여부 체크
                if (user.Username?.ToLower() == "admin" || user.Username?.ToLower() == "system")
                {
                    return false;
                }

                // 추가 비즈니스 규칙 체크...

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"사용자 삭제 가능 여부 확인 중 오류 (ID: {id}): {ex.Message}", ex);
                return false;
            }
        }

        #endregion

    }
}