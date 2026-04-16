using AutoMapper;
using BizFlow.Application.DTOs.Import;
using BizFlow.Domain.Entities;

namespace BizFlow.Application.Mappers
{
    public class ImportProfile : AutoMapper.Profile
    {
        public ImportProfile()
        {
            // Import → ImportSummaryDto
            CreateMap<Import, ImportSummaryDto>()
                .ForMember(dest => dest.BusinessLocationName,
                    opt => opt.MapFrom(src => src.BusinessLocation != null ? src.BusinessLocation.LocationName : null));

            // Import → ImportDetailDto  
            CreateMap<Import, ImportDetailDto>()
                .ForMember(dest => dest.BusinessLocationName,
                    opt => opt.MapFrom(src => src.BusinessLocation != null ? src.BusinessLocation.LocationName : null))
                .ForMember(dest => dest.Items,
                    opt => opt.MapFrom(src => src.ProductsImports));

            // ProductImport → ImportItemDetailDto
            CreateMap<ProductImport, ImportItemDetailDto>()
                .ForMember(dest => dest.ProductName,
                    opt => opt.MapFrom(src => src.Product != null ? src.Product.ProductName : null))
                .ForMember(dest => dest.Sku,
                    opt => opt.MapFrom(src => src.Product != null ? src.Product.Sku : null))
                .ForMember(dest => dest.CurrentStock,
                    opt => opt.MapFrom(src => src.Product != null ? src.Product.Stock : (decimal?)null));
        }
    }
}
