using AutoMapper;
using BizFlow.Application.DTOs.Hire;
using BizFlow.Domain.Entities;

namespace BizFlow.Application.Mappers
{
    public class HireProfile : Profile
    {
        public HireProfile()
        {
            // Map User -> EmployeeSummaryDto
            // UserId (Guid -> String) is handled automatically by AutoMapper
            CreateMap<User, EmployeeSummaryDto>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.FullName));

            // Custom mapping for HiredEmployeeDto from Tuple (returned by Repo)
            // fullName -> FullName, email -> Email, phone -> Phone handled by naming convention
            CreateMap<(UserLocationAssignment hire, string fullName, string email, string phone), HiredEmployeeDto>()
                .ForMember(dest => dest.EmployeeId, opt => opt.MapFrom(src => src.hire.UserId));

            // Custom mapping for EmployeeSummaryDto from Tuple
            CreateMap<(UserLocationAssignment hire, string fullName, string email, string phone), EmployeeSummaryDto>()
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.hire.UserId.ToString()))
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.fullName));
        }
    }
}
