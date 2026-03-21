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

            CreateMap<(Hire hire, string fullName, string email, string? phone, string? avatarUrl), HiredEmployeeDto>()
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.fullName))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.email))
                .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.phone))
                .ForMember(dest => dest.AvatarUrl, opt => opt.MapFrom(src => src.avatarUrl))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.hire.Status))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.hire.IsActive == true))
                .ForMember(dest => dest.StartAt, opt => opt.MapFrom(src =>
                    src.hire.Status == "pending" || src.hire.Status == "rejected"
                        ? (DateTime?)null
                        : src.hire.StartAt))
                .ForMember(dest => dest.EndAt, opt => opt.MapFrom(src =>
                    src.hire.Status == "pending" || src.hire.Status == "rejected"
                        ? (DateTime?)null
                        : src.hire.EndAt))
                .ForMember(dest => dest.EmployeeId, opt => opt.MapFrom(src => src.hire.EmployeeId));

            CreateMap<(Hire hire, string fullName, string email, string? phone, string? avatarUrl), EmployeeSummaryDto>()
                .ForMember(dest => dest.ProfileId, opt => opt.MapFrom(src => src.hire.EmployeeId.ToString()))
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.fullName))
                .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.phone))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.email))
                .ForMember(dest => dest.AvatarUrl, opt => opt.MapFrom(src => src.avatarUrl))
                .ForMember(dest => dest.IsAlreadyHired, opt => opt.MapFrom(src => src.hire.Status == "accepted" && src.hire.IsActive == true))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.hire.IsActive == true))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.hire.Status))
                .ForMember(dest => dest.StartAt, opt => opt.MapFrom(src =>
                    src.hire.Status == "pending" || src.hire.Status == "rejected"
                        ? (DateTime?)null
                        : src.hire.StartAt))
                .ForMember(dest => dest.EndAt, opt => opt.MapFrom(src =>
                    src.hire.Status == "pending" || src.hire.Status == "rejected"
                        ? (DateTime?)null
                        : src.hire.EndAt));
        }
    }
}
