using AutoMapper;
using BizFlow.Application.DTOs.Product;
using BizFlow.Domain.Entities;

namespace BizFlow.Application.Mappers
{
    public class ProductProfile : Profile
    {
        public ProductProfile()
        {
            // Product entity to ProductDetailDto
            CreateMap<Product, ProductDetailDto>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.ProductName))
                .ForMember(dest => dest.BusinessLocationName, opt => opt.MapFrom(src => src.BusinessLocation != null ? src.BusinessLocation.Name : ""))
                .ForMember(dest => dest.TrackInventory, opt => opt.MapFrom(src => src.TrackInventory ?? true))
                .ForMember(dest => dest.SaleItems, opt => opt.MapFrom(src => src.SaleItems));

            // Product entity to ProductListItemDto
            CreateMap<Product, ProductListItemDto>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.ProductName))
                .ForMember(dest => dest.TrackInventory, opt => opt.MapFrom(src => src.TrackInventory ?? true))
                .ForMember(dest => dest.Stock, opt => opt.MapFrom(src => 
                    (src.TrackInventory ?? true) ? src.Stock : (int?)null));

            // Product entity to ProductSaleItemsResponseDto
            CreateMap<Product, ProductSaleItemsResponseDto>()
                .ForMember(dest => dest.SaleItems, opt => opt.MapFrom(src => src.SaleItems));

            // SaleItem entity to SaleItemDto
            CreateMap<SaleItem, SaleItemDto>()
                .ForMember(dest => dest.Price, opt => opt.MapFrom(src => 
                    src.ProductPricePolicies.FirstOrDefault(pp => pp.IsDefault) != null 
                        ? src.ProductPricePolicies.First(pp => pp.IsDefault).Price 
                        : 0));
        }
    }
}
