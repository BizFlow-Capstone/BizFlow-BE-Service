using AutoMapper;
using BizFlow.Application.DTOs.Revenue;
using BizFlow.Domain.Entities;

namespace BizFlow.Application.Mappers
{
    public class RevenueProfile : AutoMapper.Profile
    {
        public RevenueProfile()
        {
            CreateMap<Revenue, RevenueDto>();
        }
    }
}
