using AutoMapper;
using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Exceptions;
using BizFlow.Application.DTOs.ImportSchema;
using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Application.Interfaces.Services;
using BizFlow.Domain.Entities;

namespace BizFlow.Application.Services
{
    public class ImportSchemaService : IImportSchemaService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public ImportSchemaService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        // =========================================================
        // 1. Get All
        // =========================================================

        public async Task<List<ImportSchemaListItemDto>> GetAllAsync()
        {
            var schemas = await _unitOfWork.ImportSchemas.GetAllAsync();
            return _mapper.Map<List<ImportSchemaListItemDto>>(schemas);
        }

        // =========================================================
        // 2. Get By Id
        // =========================================================

        public async Task<ImportSchemaResponse> GetByIdAsync(int id)
        {
            var schema = await _unitOfWork.ImportSchemas.GetByIdWithVersionsAsync(id);
            if (schema == null)
                throw new NotFoundException(MessageKeys.ImportSchemaNotFound);

            return _mapper.Map<ImportSchemaResponse>(schema);
        }

        // =========================================================
        // 3. Create
        // =========================================================

        public async Task<ImportSchemaResponse> CreateAsync(CreateImportSchemaRequest request)
        {
            if (await _unitOfWork.ImportSchemas.ExistsWithTemplateCodeAsync(request.TemplateCode))
                throw new BadRequestException(MessageKeys.ImportSchemaTemplateCodeConflict);

            var schema = new ImportSchema
            {
                TemplateCode = request.TemplateCode,
                Name = request.Name,
                IsActive = false,
                EverActivated = false,
                CreatedAt = DateTime.UtcNow
            };

            // First version — active by default within this schema
            var version = new ImportSchemaVersion
            {
                SchemaJson = request.SchemaJson,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            schema.ImportSchemaVersions.Add(version);

            await _unitOfWork.ImportSchemas.AddAsync(schema);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<ImportSchemaResponse>(schema);
        }

        // =========================================================
        // 4. Update
        // =========================================================

        public async Task<ImportSchemaResponse> UpdateAsync(int id, UpdateImportSchemaRequest request)
        {
            var schema = await _unitOfWork.ImportSchemas.GetByIdWithVersionsAsync(id);
            if (schema == null)
                throw new NotFoundException(MessageKeys.ImportSchemaNotFound);

            // Update basic fields if provided
            if (!string.IsNullOrWhiteSpace(request.Name))
                schema.Name = request.Name;

            if (!string.IsNullOrWhiteSpace(request.TemplateCode) && request.TemplateCode != schema.TemplateCode)
            {
                if (await _unitOfWork.ImportSchemas.ExistsWithTemplateCodeAsync(request.TemplateCode, excludeId: id))
                    throw new BadRequestException(MessageKeys.ImportSchemaTemplateCodeConflict);

                schema.TemplateCode = request.TemplateCode;
            }

            // Only create a new version if SchemaJson is provided AND different
            if (!string.IsNullOrWhiteSpace(request.SchemaJson))
            {
                var currentActiveVersion = schema.ImportSchemaVersions
                    .FirstOrDefault(v => v.IsActive);

                var schemaJsonChanged = currentActiveVersion == null
                    || currentActiveVersion.SchemaJson != request.SchemaJson;

                if (schemaJsonChanged)
                {
                    // Deactivate old version
                    if (currentActiveVersion != null)
                        currentActiveVersion.IsActive = false;

                    // Create new active version
                    var newVersion = new ImportSchemaVersion
                    {
                        ImportSchemaId = schema.ImportSchemaId,
                        SchemaJson = request.SchemaJson,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    schema.ImportSchemaVersions.Add(newVersion);
                }
            }

            _unitOfWork.ImportSchemas.Update(schema);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<ImportSchemaResponse>(schema);
        }

        // =========================================================
        // 5. Activate
        // =========================================================

        public async Task ActivateAsync(int id)
        {
            var schema = await _unitOfWork.ImportSchemas.GetByIdAsync(id);
            if (schema == null)
                throw new NotFoundException(MessageKeys.ImportSchemaNotFound);

            // Deactivate all schemas first
            await _unitOfWork.ImportSchemas.DeactivateAllAsync();

            // Activate target
            schema.IsActive = true;
            schema.EverActivated = true;

            _unitOfWork.ImportSchemas.Update(schema);
            await _unitOfWork.SaveChangesAsync();
        }

        // =========================================================
        // 6. Delete
        // =========================================================

        public async Task DeleteAsync(int id)
        {
            var schema = await _unitOfWork.ImportSchemas.GetByIdAsync(id);
            if (schema == null)
                throw new NotFoundException(MessageKeys.ImportSchemaNotFound);

            // Cannot delete active schema
            if (schema.IsActive == true)
                throw new BadRequestException(MessageKeys.ImportSchemaCannotDeleteActive);

            if (schema.EverActivated)
            {
                // Soft delete
                schema.DeletedAt = DateTime.UtcNow;
                _unitOfWork.ImportSchemas.Update(schema);
            }
            else
            {
                // Hard delete
                _unitOfWork.ImportSchemas.Delete(schema);
            }

            await _unitOfWork.SaveChangesAsync();
        }
    }
}
