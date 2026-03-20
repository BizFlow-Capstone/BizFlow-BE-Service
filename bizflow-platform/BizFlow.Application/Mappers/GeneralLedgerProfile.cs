using AutoMapper;
using BizFlow.Application.DTOs.GeneralLedger;
using BizFlow.Domain.Entities;

namespace BizFlow.Application.Mappers
{
    public class GeneralLedgerProfile : AutoMapper.Profile
    {
        public GeneralLedgerProfile()
        {
            CreateMap<GeneralLedgerEntry, GeneralLedgerEntryDto>();
        }
    }
}
