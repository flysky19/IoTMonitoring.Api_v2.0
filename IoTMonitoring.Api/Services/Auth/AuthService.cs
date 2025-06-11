using System;
using System.Security.Authentication;
using System.Threading.Tasks;
using IoTMonitoring.Api.Data.Models;
using IoTMonitoring.Api.Data.Repositories.Interfaces;
using IoTMonitoring.Api.DTOs.Auth;
using IoTMonitoring.Api.Services.Auth.Interfaces;
using IoTMonitoring.Api.Services.Security.Interfaces;
using IoTMonitoring.Api.Services.Security.Models;
using IoTMonitoring.Api.Utilities;

namespace IoTMonitoring.Api.Services.Auth
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtTokenGenerator _tokenGenerator;

        public AuthService(
            IUserRepository userRepository,
            IPasswordHasher passwordHasher,
            IJwtTokenGenerator tokenGenerator)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _tokenGenerator = tokenGenerator;
        }

        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            // 1. 사용자 조회
            var user = await _userRepository.GetByUsernameAsync(request.Username);

            Console.WriteLine($"사용자 조회 결과: {(user == null ? "사용자 없음" : $"사용자 ID: {user.UserID}, 활성 상태: {user.IsActive}")}");

            if (user == null || !user.IsActive)
            {
                throw new AuthenticationException("사용자가 존재하지 않거나 비활성 상태입니다.");
            }

            // 2. 비밀번호 검증
            if (!_passwordHasher.VerifyPassword(request.Password, user.Password))
            {
                throw new AuthenticationException("비밀번호가 일치하지 않습니다.");
            }

            // 3. 마지막 로그인 시간 업데이트
            user.LastLogin = DateTimeHelper.Now;
            await _userRepository.UpdateAsync(user);

            // 4. JWT 토큰 생성
            var tokenResult = _tokenGenerator.GenerateTokens(user);

            // 5. 로그인 응답 반환
            return new LoginResponse
            {
                Token = tokenResult.AccessToken,
                RefreshToken = tokenResult.RefreshToken,
                Expiration = tokenResult.Expiration,
                UserId = user.UserID,
                Username = user.Username,
                FullName = user.FullName,
                LastLogin = user.LastLogin.Value,
                Role = user.Role.Trim(), // 이 부분 확인
            };
        }

        public async Task<LoginResponse> RefreshTokenAsync(string refreshToken)
        {
            // 리프레시 토큰 파싱
            if (!_tokenGenerator.TryParseRefreshToken(refreshToken, out RefreshTokenData refreshTokenData))
            {
                throw new AuthenticationException("유효하지 않은 리프레시 토큰입니다.");
            }

            // 사용자 조회
            var user = await _userRepository.GetByIdAsync(refreshTokenData.UserId);
            if (user == null || !user.IsActive)
            {
                throw new AuthenticationException("사용자가 존재하지 않거나 비활성 상태입니다.");
            }

            // 새로운 토큰 발급
            var tokenResult = _tokenGenerator.GenerateTokens(user);

            return new LoginResponse
            {
                Token = tokenResult.AccessToken,
                RefreshToken = tokenResult.RefreshToken,
                Expiration = tokenResult.Expiration,
                UserId = user.UserID,
                Username = user.Username,
                FullName = user.FullName,
                LastLogin = user.LastLogin ?? DateTimeHelper.Now
            };
        }

        public Task<bool> ValidateTokenAsync(string token)
        {
            var principal = _tokenGenerator.ValidateToken(token);
            return Task.FromResult(principal != null);
        }

        public bool IsAdmin(string username)
        {
            return username.ToLower() == "admin";
        }
    }
}