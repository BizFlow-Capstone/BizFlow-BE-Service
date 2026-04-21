using AutoMapper;
using BizFlow.Application.DTOs.Order;
using BizFlow.Domain.Entities;

namespace BizFlow.Application.Mappers
{
    public class OrderProfile : AutoMapper.Profile
    {
        public OrderProfile()
        {
            // Status is a reference i18n field — populated by the service layer
            // via IReferenceLabelService after AutoMapper projection.
            CreateMap<Order, OrderDto>()
                .ForMember(d => d.CreatedByProfileId, o => o.MapFrom(s => s.CreatedBy))
                .ForMember(d => d.CreatedByProfileFullName, o => o.Ignore())
                .ForMember(d => d.Items, o => o.MapFrom(s => s.OrderDetails))
                .ForMember(d => d.Status, o => o.Ignore());

            CreateMap<OrderDetail, OrderDetailDto>()
                .ForMember(d => d.ProductId, o => o.MapFrom(s => s.SaleItem.ProductId))
                .ForMember(d => d.ProductName, o => o.MapFrom(s => s.SaleItem.Product.ProductName))
                .ForMember(d => d.Unit, o => o.MapFrom(s => s.SaleItem.Unit));
        }
    }
}
