using AutoMapper;
using BizFlow.Application.DTOs.Hire;
using BizFlow.Application.DTOs.Location;
using BizFlow.Domain.Entities;

namespace BizFlow.Application.Mappers
{
    public class LocationProfile : AutoMapper.Profile
    {
        public LocationProfile()
        {
            // CreateLocationRequest → BusinessLocation
            CreateMap<CreateLocationRequest, BusinessLocation>()
                .ForMember(dest => dest.LocationName, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.BusinessLocationId, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.Ignore())
                .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Imports, opt => opt.Ignore())
                .ForMember(dest => dest.Products, opt => opt.Ignore())
                .ForMember(dest => dest.UserLocationAssignments, opt => opt.Ignore());

            // UpdateLocationRequest → BusinessLocation
            CreateMap<UpdateLocationRequest, BusinessLocation>()
                .ForMember(dest => dest.LocationName, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.BusinessLocationId, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.Ignore())
                .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Imports, opt => opt.Ignore())
                .ForMember(dest => dest.Products, opt => opt.Ignore())
                .ForMember(dest => dest.UserLocationAssignments, opt => opt.Ignore());

            // BusinessLocation → BusinessLocationDto (kept for potential reuse)
            CreateMap<BusinessLocation, BusinessLocationDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.BusinessLocationId))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.LocationName));

            // Tuple from Repository → EmployeeSummaryDto
            CreateMap<(Guid UserId, string FullName, string Email, string Phone), EmployeeSummaryDto>()
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId.ToString()))
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.FullName))
                .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.Phone));
        }
    }
}
