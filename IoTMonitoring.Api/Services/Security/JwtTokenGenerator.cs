using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using IoTMonitoring.Api.Data.Models;
using IoTMonitoring.Api.Services.Security.Interfaces;
using IoTMonitoring.Api.Services.Security.Models;
using IoTMonitoring.Api.Utilities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

namespace IoTMonitoring.Api.Services.Security
{
    public class JwtTokenGenerator : IJwtTokenGenerator
    {
        private readonly JwtSettings _jwtSettings;

        public JwtTokenGenerator(IOptions<JwtSettings> jwtSettings)
        {
            _jwtSettings = jwtSettings.Value;
        }

        public TokenResult GenerateTokens(User user)
        {
            // JWT 토큰 생성 로직 구현
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role?.Trim()), // 중요! Role 클레임 추가
                new Claim("fullName", user.FullName ?? string.Empty)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiry = DateTimeHelper.Now.AddMinutes(_jwtSettings.ExpirationMinutes);

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: expiry,
                signingCredentials: creds
            );

            // 리프레시 토큰 생성
            var refreshTokenExpiry = DateTimeHelper.Now.AddDays(_jwtSettings.RefreshTokenExpirationDays);
            var refreshTokenData = new RefreshTokenData
            {
                UserId = user.UserID,
                ExpiryDate = refreshTokenExpiry
            };

            var refreshToken = EncryptRefreshToken(refreshTokenData);

            return new TokenResult
            {
                AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
                RefreshToken = refreshToken,
                Expiration = expiry
            };
        }

        public ClaimsPrincipal ValidateToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidAudience = _jwtSettings.Audience,
                ClockSkew = TimeSpan.Zero,
                RoleClaimType = ClaimTypes.Role // Role 클레임 타입 명시
            };

            try
            {
                return tokenHandler.ValidateToken(token, validationParameters, out _);
            }
            catch
            {
                return null;
            }
        }

        public bool TryParseRefreshToken(string refreshToken, out RefreshTokenData refreshTokenData)
        {
            refreshTokenData = null;

            try
            {
                var decrypted = DecryptRefreshToken(refreshToken);
                refreshTokenData = JsonConvert.DeserializeObject<RefreshTokenData>(decrypted);
                return refreshTokenData != null && refreshTokenData.ExpiryDate > DateTimeHelper.Now;
            }
            catch
            {
                return false;
            }
        }

        private string EncryptRefreshToken(RefreshTokenData data)
        {
            var json = JsonConvert.SerializeObject(data);
            var key = Encoding.UTF8.GetBytes(_jwtSettings.SecretKey.Length >= 32
                ? _jwtSettings.SecretKey.Substring(0, 32)
                : _jwtSettings.SecretKey.PadRight(32, '0'));

            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.GenerateIV();

                using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                {
                    using (var ms = new System.IO.MemoryStream())
                    {
                        ms.Write(aes.IV, 0, aes.IV.Length);

                        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                        using (var sw = new System.IO.StreamWriter(cs))
                        {
                            sw.Write(json);
                        }

                        return Convert.ToBase64String(ms.ToArray());
                    }
                }
            }
        }

        private string DecryptRefreshToken(string encryptedToken)
        {
            var key = Encoding.UTF8.GetBytes(_jwtSettings.SecretKey.Length >= 32
              ? _jwtSettings.SecretKey.Substring(0, 32)
              : _jwtSettings.SecretKey.PadRight(32, '0'));
            var encryptedBytes = Convert.FromBase64String(encryptedToken);

            using (var aes = Aes.Create())
            {
                aes.Key = key;

                var iv = new byte[aes.IV.Length];
                Array.Copy(encryptedBytes, 0, iv, 0, iv.Length);
                aes.IV = iv;

                using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                {
                    using (var cs = new CryptoStream(
                        new System.IO.MemoryStream(
                            encryptedBytes, iv.Length, encryptedBytes.Length - iv.Length),
                        decryptor,
                        CryptoStreamMode.Read))
                    using (var sr = new System.IO.StreamReader(cs))
                    {
                        return sr.ReadToEnd();
                    }
                }
            }
        }
    }
}