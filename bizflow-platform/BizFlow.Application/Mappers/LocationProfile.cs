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
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Address))
                .ForMember(dest => dest.District, opt => opt.MapFrom(src => src.District))
                .ForMember(dest => dest.City, opt =>  opt.MapFrom(src => src.City))
                .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.Phone))
                .ForMember(dest => dest.TaxCode, opt => opt.MapFrom(src => src.TaxCode))
                .ForMember(dest => dest.BusinessLocationId, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.Ignore())
                .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Imports, opt => opt.Ignore())
                .ForMember(dest => dest.Products, opt => opt.Ignore())
                .ForMember(dest => dest.UserLocationAssignments, opt => opt.Ignore());
            
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
