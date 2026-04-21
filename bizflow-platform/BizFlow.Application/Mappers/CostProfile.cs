using AutoMapper;
using BizFlow.Application.DTOs.Cost;
using BizFlow.Domain.Entities;

namespace BizFlow.Application.Mappers
{
    public class CostProfile : AutoMapper.Profile
    {
        public CostProfile()
        {
            // CostType / PaymentMethod are reference i18n fields — populated by
            // the service layer via IReferenceLabelService after AutoMapper
            // projection to avoid pulling DI into AutoMapper profiles.
            CreateMap<Cost, CostDto>()
                .ForMember(dest => dest.CostType, opt => opt.Ignore())
                .ForMember(dest => dest.PaymentMethod, opt => opt.Ignore());
        }
    }
}
