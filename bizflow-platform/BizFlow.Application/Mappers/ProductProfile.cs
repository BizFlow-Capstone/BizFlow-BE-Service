using AutoMapper;
using BizFlow.Application.DTOs.Product;
using BizFlow.Domain.Entities;

namespace BizFlow.Application.Mappers
{
    public class ProductProfile : AutoMapper.Profile
    {
        public ProductProfile()
        {
            // ============ Entity → DTO ============

            // Product → ProductDetailDto
            CreateMap<Product, ProductDetailDto>()
                .IncludeBase<Product, ProductSummaryDto>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.ProductName))
                .ForMember(dest => dest.BusinessLocationName, opt => opt.MapFrom(src => src.BusinessLocation != null ? src.BusinessLocation.LocationName : ""))
                .ForMember(dest => dest.TrackInventory, opt => opt.MapFrom(src => src.TrackInventory ?? true))
                .ForMember(dest => dest.ImageUrl, opt => opt.MapFrom(src => src.ImageUrl))
                .ForMember(dest => dest.SaleItems, opt => opt.MapFrom(src => src.SaleItems));

            // Product → ProductSummaryDto
            CreateMap<Product, ProductSummaryDto>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.ProductName))
                .ForMember(dest => dest.TrackInventory, opt => opt.MapFrom(src => src.TrackInventory ?? true))
                .ForMember(dest => dest.ImageUrl, opt => opt.MapFrom(src => src.ImageUrl))
                .ForMember(dest => dest.BusinessTypeId, opt => opt.MapFrom(src => src.BusinessTypeId))
                .ForMember(dest => dest.BusinessTypeName, opt => opt.MapFrom(src => src.BusinessType != null ? src.BusinessType.Name : ""))
                .ForMember(dest => dest.Stock, opt => opt.MapFrom(src =>
                    (src.TrackInventory ?? true) ? src.Stock : (int?)null))
                .ForMember(dest => dest.Price, opt => opt.MapFrom(src =>
                    src.SaleItems
                        .Where(s => s.Unit.ToLower() == src.Unit.ToLower())
                        .SelectMany(s => s.ProductPricePolicies)
                        .Where(pp => pp.IsDefault)
                        .Select(pp => pp.Price)
                        .FirstOrDefault()));

            // Product -> ProductQuickSearchDto
            CreateMap<Product, ProductQuickSearchDto>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.ProductName))
                .ForMember(dest => dest.SellingPrice, opt => opt.MapFrom(src =>
                    src.SaleItems
                        .Where(s => s.Unit.ToLower() == src.Unit.ToLower())
                        .SelectMany(s => s.ProductPricePolicies)
                        .Where(pp => pp.IsDefault)
                        .Select(pp => pp.Price)
                        .FirstOrDefault()))
                .ForMember(dest => dest.SaleItems, opt => opt.MapFrom(src => src.SaleItems));

            // Product → ProductSaleItemsResponseDto
            CreateMap<Product, ProductSaleItemsResponseDto>()
                .ForMember(dest => dest.SaleItems, opt => opt.MapFrom(src => src.SaleItems));

            // SaleItem → SaleItemDto
            CreateMap<SaleItem, SaleItemDto>()
                .ForMember(dest => dest.Price, opt => opt.MapFrom(src =>
                    src.ProductPricePolicies.FirstOrDefault(pp => pp.IsDefault) != null
                        ? src.ProductPricePolicies.First(pp => pp.IsDefault).Price
                        : 0));

            // ============ Request → Entity ============

            // CreateProductRequest → Product (used in Create + Update)
            CreateMap<CreateProductRequest, Product>()
                .ForMember(dest => dest.BusinessLocationId, opt => opt.MapFrom(src => src.LocationId))
                .ForMember(dest => dest.ProductId, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.Stock, opt => opt.Ignore())
                .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
                .ForMember(dest => dest.ImageUrl, opt => opt.Ignore())
                .ForMember(dest => dest.ImagePublicId, opt => opt.Ignore())
                .ForMember(dest => dest.SaleItems, opt => opt.Ignore())
                .ForMember(dest => dest.ProductsImports, opt => opt.Ignore())
                .ForMember(dest => dest.StockMovements, opt => opt.Ignore())
                .ForMember(dest => dest.BusinessLocation, opt => opt.Ignore())
                .ForMember(dest => dest.BusinessType, opt => opt.Ignore());
        }
    }
}
