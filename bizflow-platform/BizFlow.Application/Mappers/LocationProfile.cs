using AutoMapper;
using BizFlow.Application.DTOs.Location;
using BizFlow.Domain.Entities;

namespace BizFlow.Application.Mappers
{
    public class LocationProfile : Profile
    {
        public LocationProfile()
        {
            CreateMap<CreateLocationRequest, BusinessLocation>();
            
            CreateMap<BusinessLocation, BusinessLocationDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.BusinessLocationId))
                .ForMember(dest => dest.OwnerName, opt => opt.MapFrom((src, dest, destMember, context) => 
                    context.Items.ContainsKey("OwnerName") ? context.Items["OwnerName"] : null));
        }
    }
}
