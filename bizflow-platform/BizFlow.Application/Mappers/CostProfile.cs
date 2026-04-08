using AutoMapper;
using BizFlow.Application.DTOs.Cost;
using BizFlow.Domain.Entities;

namespace BizFlow.Application.Mappers
{
    public class CostProfile : AutoMapper.Profile
    {
        public CostProfile()
        {
            CreateMap<Cost, CostDto>();
        }
    }
}
