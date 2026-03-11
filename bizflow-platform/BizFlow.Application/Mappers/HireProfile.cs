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
                .ForMember(dest => dest.EmployeeId, opt => opt.MapFrom(src => src.hire.EmployeeId));

            CreateMap<(Hire hire, string fullName, string email, string phone), EmployeeSummaryDto>()
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.hire.EmployeeId.ToString()))
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.fullName))
                .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.phone));
        }
    }
}
