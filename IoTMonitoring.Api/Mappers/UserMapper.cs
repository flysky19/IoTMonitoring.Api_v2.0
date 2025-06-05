using System;
using System.Linq;
using IoTMonitoring.Api.Data.Models;
using IoTMonitoring.Api.DTOs;
using IoTMonitoring.Api.Mappers.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace IoTMonitoring.Api.Mappers
{
    public class UserMapper : IUserMapper
    {
        public UserDto ToDto(User user)
        {
            if (user == null) return null;

            return new UserDto
            {
                UserID = user.UserID,
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                Phone = user.Phone,
                Role = user.Role,
                CompanyIDs = user.UserCompanies?.Select(uc => uc.CompanyID).ToList() ?? new List<int>(),
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt
            };
        }

        public UserDto ToDetailDto(User user)
        {
            if (user == null) return null;

            return new UserDto
            {
                UserID = user.UserID,
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                Phone = user.Phone,
                Role = user.Role,  // 단일 역할
                CompanyIDs = user.UserCompanies.Select(uc => uc.CompanyID).ToList(),
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt
            };
        }

        public User ToEntity(UserCreateDto userDto)
        {
            if (userDto == null) return null;

            return new User
            {
                //Username = dto.Username,
                //Email = dto.Email,
                //FullName = dto.FullName,
                //Phone = dto.Phone,
                ////Role = dto.Roles?.FirstOrDefault() ?? "User",
                ////CompanyID = dto.CompanyIDs?.FirstOrDefault(),
                //IsActive = dto.Active,
                //CreatedAt = DateTime.UtcNow
                Username = userDto.Username,
                Password = userDto.Password,
                Email = userDto.Email,
                FullName = userDto.FullName,
                Phone = userDto.Phone,
                Role = userDto.Role,  // 단일 역할
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        public void UpdateEntity(User user, UserUpdateDto dto)
        {
            if (user == null || dto == null) return;

            user.Email = dto.Email ?? user.Email;
            user.FullName = dto.FullName ?? user.FullName;
            user.Phone = dto.Phone ?? user.Phone;
            user.IsActive = dto.IsActive;
            user.Role = dto.Role == null ? user.Role : "User";
            //user.com = dto.CompanyIDs?.FirstOrDefault();
            user.UpdatedAt = DateTime.UtcNow;
        }

        public void UpdateEntity(User user, UserProfileUpdateDto dto)
        {
            if (user == null || dto == null) return;

            user.Email = dto.Email ?? user.Email;
            user.FullName = dto.FullName ?? user.FullName;
            user.Phone = dto.Phone ?? user.Phone;
            user.UpdatedAt = DateTime.UtcNow;
        }
    }
}