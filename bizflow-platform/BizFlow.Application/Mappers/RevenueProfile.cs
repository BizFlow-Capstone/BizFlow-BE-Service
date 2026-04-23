using AutoMapper;
using BizFlow.Application.DTOs.Revenue;
using BizFlow.Domain.Entities;

namespace BizFlow.Application.Mappers
{
    public class RevenueProfile : AutoMapper.Profile
    {
        public RevenueProfile()
        {
            // RevenueType / MoneyChannel are reference i18n fields — populated by
            // the service layer via IReferenceLabelService after AutoMapper
            // projection to avoid pulling DI into AutoMapper profiles.
            CreateMap<Revenue, RevenueDto>()
                .ForMember(dest => dest.BusinessTypeName,
                    opt => opt.MapFrom(src => src.BusinessType != null ? src.BusinessType.Name : null))
                .ForMember(dest => dest.RevenueType, opt => opt.Ignore())
                .ForMember(dest => dest.MoneyChannel, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore());
        }
    }
}
