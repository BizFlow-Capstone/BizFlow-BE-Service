using AutoMapper;
using BizFlow.Application.DTOs.Hire;
using BizFlow.Domain.Entities;

namespace BizFlow.Application.Mappers
{
    public class HireProfile : AutoMapper.Profile
    {
        public HireProfile()
        {
            CreateMap<BizFlow.Domain.Entities.Profile, EmployeeSummaryDto>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.FullName));

            CreateMap<(Hire hire, string fullName, string email, string phone), HiredEmployeeDto>()
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.hire.Status))
                .ForMember(dest => dest.EmployeeId, opt => opt.MapFrom(src => src.hire.EmployeeId));

            CreateMap<(Hire hire, string fullName, string email, string phone), EmployeeSummaryDto>()
                .ForMember(dest => dest.ProfileId, opt => opt.MapFrom(src => src.hire.EmployeeId.ToString()))
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.fullName))
                .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.phone))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.email))
                .ForMember(dest => dest.IsAlreadyHired, opt => opt.MapFrom(src => src.hire.Status == "accepted"));
        }
    }
}
