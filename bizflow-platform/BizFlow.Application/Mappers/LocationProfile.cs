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
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.LocationName))
                .ForMember(dest => dest.OwnerProfileId, opt => opt.Ignore())
                .ForMember(dest => dest.OwnerName, opt => opt.Ignore());

            // Tuple from Repository → EmployeeSummaryDto
            CreateMap<(Guid UserId, string FullName, string Email, string Phone), EmployeeSummaryDto>()
                .ForMember(dest => dest.ProfileId, opt => opt.MapFrom(src => src.UserId.ToString()))
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.FullName))
                .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.Phone))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.IsAlreadyHired, opt => opt.MapFrom(src => true))
                .ForMember(dest => dest.Status, opt => opt.Ignore());
        }
    }
}
