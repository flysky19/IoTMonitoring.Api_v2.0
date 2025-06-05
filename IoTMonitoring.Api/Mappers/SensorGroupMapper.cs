using IoTMonitoring.Api.Data.Models;
using IoTMonitoring.Api.DTOs;
using IoTMonitoring.Api.Mappers.Interfaces;

namespace IoTMonitoring.Api.Mappers
{
    public class SensorGroupMapper : ISensorGroupMapper
    {
        public SensorGroupDto ToDto(SensorGroup entity)
        {
            if (entity == null) return null;

            return new SensorGroupDto
            {
                GroupID = entity.GroupID,
                CompanyID = entity.CompanyID,
                CompanyName = entity.Company?.CompanyName, // Navigation property 사용
                GroupName = entity.GroupName,
                Location = entity.Location,
                IsActive = entity.Active,
                SensorCount = 0 // Service에서 별도 계산
            };
        }

        public SensorGroupDetailDto ToDetailDto(SensorGroup entity)
        {
            if (entity == null) return null;

            return new SensorGroupDetailDto
            {
                GroupID = entity.GroupID,
                CompanyID = entity.CompanyID,
                CompanyName = entity.Company?.CompanyName,
                GroupName = entity.GroupName,
                Location = entity.Location,
                Description = entity.Description,
                IsActive = entity.Active,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                SensorCount = 0, // Service에서 별도 계산
                SensorCountByType = new Dictionary<string, int>(), // Service에서 별도 계산
                SensorCountByStatus = new Dictionary<string, int>() // Service에서 별도 계산
            };
        }

        public SensorGroup ToEntity(SensorGroupCreateDto createDto)
        {
            if (createDto == null) return null;

            return new SensorGroup
            {
                CompanyID = createDto.CompanyID,
                GroupName = createDto.GroupName,
                Location = createDto.Location,
                Description = createDto.Description,
                Active = createDto.Active,
                CreatedAt = DateTime.UtcNow
            };
        }

        public void UpdateEntity(SensorGroup entity, SensorGroupUpdateDto updateDto)
        {
            if (entity == null || updateDto == null) return;

            if (!string.IsNullOrEmpty(updateDto.GroupName))
                entity.GroupName = updateDto.GroupName;

            entity.Location = updateDto.Location;
            entity.Description = updateDto.Description;
            entity.Active = updateDto.Active;
            entity.UpdatedAt = DateTime.UtcNow;
        }
    }
}