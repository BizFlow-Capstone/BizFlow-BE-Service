using BizFlow.Domain.Entities;

namespace BizFlow.Application.Interfaces.Repositories;

public interface IAccountingTemplateRepository
{
    Task<AccountingTemplate?> GetByCodeAsync(string templateCode);
    Task<List<AccountingTemplate>> GetActiveTemplatesAsync();
    Task<List<AccountingTemplate>> GetAllWithVersionsAsync();
    Task<AccountingTemplate?> GetByIdWithVersionsAsync(int templateId);
    Task<bool> ExistsByCodeAsync(string templateCode);
    Task AddTemplateAsync(AccountingTemplate template);
    Task<AccountingTemplateVersion?> GetActiveVersionByTemplateIdAsync(int templateId);
    Task<AccountingTemplateVersion?> GetVersionWithMappingsAsync(int templateVersionId);
    Task<AccountingTemplateVersion?> GetVersionWithMappingsAndBooksAsync(int templateVersionId);
    Task<List<TemplateRowDefinition>> GetRowDefinitionsAsync(int templateVersionId);
    Task<AccountingTemplateVersion> AddVersionAsync(AccountingTemplateVersion version);
    void UpdateVersion(AccountingTemplateVersion version);
    void RemoveVersion(AccountingTemplateVersion version);
    Task<TemplateFieldMapping?> GetMappingByIdAsync(int mappingId);
    void UpdateMapping(TemplateFieldMapping mapping);
    Task AddMappingAsync(TemplateFieldMapping mapping);
    void RemoveMapping(TemplateFieldMapping mapping);

    // ── RowDefinitions ──
    Task<TemplateRowDefinition?> GetRowDefinitionByIdAsync(int rowDefId);
    Task AddRowDefinitionAsync(TemplateRowDefinition rowDef);
    void UpdateRowDefinition(TemplateRowDefinition rowDef);
    void RemoveRowDefinition(TemplateRowDefinition rowDef);

    // ── MappableEntities ──
    Task<List<MappableEntity>> GetAllMappableEntitiesAsync(bool? active);
    Task<MappableEntity?> GetMappableEntityWithFieldsAsync(int entityId);
    Task<MappableEntity?> GetMappableEntityByCodeAsync(string entityCode);
    Task AddMappableEntityAsync(MappableEntity entity);
    void UpdateMappableEntity(MappableEntity entity);
    void RemoveMappableEntity(MappableEntity entity);

    // ── MappableFields ──
    Task<MappableField?> GetMappableFieldByIdAsync(int fieldId);
    Task AddMappableFieldAsync(MappableField field);
    void UpdateMappableField(MappableField field);
}
