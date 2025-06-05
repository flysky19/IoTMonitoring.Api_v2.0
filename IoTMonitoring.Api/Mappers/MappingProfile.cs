// Mappers/MappingProfile.cs
using AutoMapper;
using IoTMonitoring.Api.Data.Models;
using IoTMonitoring.Api.DTOs;

namespace IoTMonitoring.Api.Mappers
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Sensor -> SensorDto
            CreateMap<Sensor, SensorDto>()
                .ForMember(dest => dest.GroupName, opt => opt.MapFrom(src => src.SensorGroup != null ? src.SensorGroup.GroupName : null))
                .ForMember(dest => dest.Location, opt => opt.MapFrom(src => src.SensorGroup != null ? src.SensorGroup.Location : null));

            // Sensor -> SensorDetailDto
            CreateMap<Sensor, SensorDetailDto>()
                .ForMember(dest => dest.GroupName, opt => opt.MapFrom(src => src.SensorGroup != null ? src.SensorGroup.GroupName : null))
                .ForMember(dest => dest.Location, opt => opt.MapFrom(src => src.SensorGroup != null ? src.SensorGroup.Location : null));

            // 필요한 추가 매핑 구성
        }
    }
}