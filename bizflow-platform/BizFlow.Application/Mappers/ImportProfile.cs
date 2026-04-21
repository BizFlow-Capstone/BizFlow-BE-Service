using AutoMapper;
using BizFlow.Application.DTOs.Import;
using BizFlow.Domain.Entities;

namespace BizFlow.Application.Mappers
{
    public class ImportProfile : AutoMapper.Profile
    {
        public ImportProfile()
        {
            // ImportType / Status are reference i18n fields — populated by the
            // service layer via IReferenceLabelService after AutoMapper
            // projection to avoid pulling DI into AutoMapper profiles.

            // Import → ImportSummaryDto
            CreateMap<Import, ImportSummaryDto>()
                .ForMember(dest => dest.BusinessLocationName,
                    opt => opt.MapFrom(src => src.BusinessLocation != null ? src.BusinessLocation.LocationName : null))
                .ForMember(dest => dest.ImportType, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore());

            // Import → ImportDetailDto
            CreateMap<Import, ImportDetailDto>()
                .IncludeBase<Import, ImportSummaryDto>()
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
