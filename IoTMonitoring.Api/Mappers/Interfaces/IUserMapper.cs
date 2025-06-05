using IoTMonitoring.Api.Data.Models;
using IoTMonitoring.Api.DTOs;

namespace IoTMonitoring.Api.Mappers.Interfaces
{
    public interface IUserMapper
    {
        UserDto ToDto(User user);
        UserDto ToDetailDto(User user);
        User ToEntity(UserCreateDto dto);
        void UpdateEntity(User user, UserUpdateDto dto);
        void UpdateEntity(User user, UserProfileUpdateDto dto);
    }
}
