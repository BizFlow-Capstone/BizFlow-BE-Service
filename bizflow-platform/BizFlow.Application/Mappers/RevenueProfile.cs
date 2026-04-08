using AutoMapper;
using BizFlow.Application.DTOs.Revenue;
using BizFlow.Domain.Entities;

namespace BizFlow.Application.Mappers
{
    public class RevenueProfile : AutoMapper.Profile
    {
        public RevenueProfile()
        {
            CreateMap<Revenue, RevenueDto>()
                .ForMember(dest => dest.BusinessTypeName,
                    opt => opt.MapFrom(src => src.BusinessType != null ? src.BusinessType.Name : null));
        }
    }
}
