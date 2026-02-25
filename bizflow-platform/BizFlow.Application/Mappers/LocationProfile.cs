using AutoMapper;
using BizFlow.Application.DTOs.Hire;
using BizFlow.Application.DTOs.Location;
using BizFlow.Domain.Entities;

namespace BizFlow.Application.Mappers
{
    public class LocationProfile : Profile
    {
        public LocationProfile()
        {
            CreateMap<CreateLocationRequest, BusinessLocation>();
            CreateMap<UpdateLocationRequest, BusinessLocation>()
                .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));
            
            CreateMap<BusinessLocation, BusinessLocationDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.BusinessLocationId));

            // Map tuple from Repository to DTO
            CreateMap<(Guid UserId, string FullName, string Email, string Phone), EmployeeSummaryDto>()
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId.ToString()))
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.FullName))
                .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.Phone));
        }
    }
}
