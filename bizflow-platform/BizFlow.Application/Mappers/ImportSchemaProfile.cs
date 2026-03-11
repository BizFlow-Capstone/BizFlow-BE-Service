using AutoMapper;
using BizFlow.Application.DTOs.ImportSchema;
using BizFlow.Domain.Entities;

namespace BizFlow.Application.Mappers
{
    public class ImportSchemaProfile : AutoMapper.Profile
    {
        public ImportSchemaProfile()
        {
            // Base mapping: ImportSchema → ImportSchemaListItemDto (list endpoint)
            CreateMap<ImportSchema, ImportSchemaListItemDto>()
                .ForMember(dest => dest.IsActive,
                    opt => opt.MapFrom(src => src.IsActive == true));

            // ImportSchema → ImportSchemaResponse (detail endpoint)
            // IncludeBase<> inherits base member config (including IsActive bool? → bool)
            CreateMap<ImportSchema, ImportSchemaResponse>()
                .IncludeBase<ImportSchema, ImportSchemaListItemDto>()
                .ForMember(dest => dest.SchemaJson,
                    opt => opt.MapFrom(src => src.ImportSchemaVersions
                        .FirstOrDefault(v => v.IsActive) != null
                            ? src.ImportSchemaVersions.FirstOrDefault(v => v.IsActive)!.SchemaJson
                            : null));
        }
    }
}
