using AutoMapper;
using BizFlow.Application.DTOs.GeneralLedger;
using BizFlow.Domain.Entities;

namespace BizFlow.Application.Mappers
{
    public class GeneralLedgerProfile : AutoMapper.Profile
    {
        public GeneralLedgerProfile()
        {
            // TransactionType / MoneyChannel / EffectiveStatus and
            // Source.ReferenceType are reference i18n fields — populated by the
            // service layer via IReferenceLabelService after AutoMapper
            // projection to avoid pulling DI into AutoMapper profiles.
            CreateMap<GeneralLedgerEntry, GeneralLedgerEntryDto>()
                .ForMember(d => d.TransactionType, opt => opt.Ignore())
                .ForMember(d => d.MoneyChannel, opt => opt.Ignore())
                .ForMember(d => d.EffectiveStatus, opt => opt.Ignore())
                .ForMember(d => d.Source, opt => opt.MapFrom(s => new SourceLinkDto
                {
                    ReferenceId = s.ReferenceId
                }));
        }
    }
}
