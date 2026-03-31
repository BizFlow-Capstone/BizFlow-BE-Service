using BizFlow.Domain.Entities;

namespace BizFlow.Application.Interfaces.Repositories;

public interface IAccountingTemplateRepository
{
    Task<AccountingTemplate?> GetByCodeAsync(string templateCode);
    Task<List<AccountingTemplate>> GetActiveTemplatesAsync();
    Task<List<AccountingTemplate>> GetAllWithVersionsAsync();
    Task<AccountingTemplateVersion?> GetActiveVersionByTemplateIdAsync(int templateId);
    Task<AccountingTemplateVersion?> GetVersionWithMappingsAsync(int templateVersionId);
    Task<AccountingTemplateVersion?> GetVersionWithMappingsAndBooksAsync(int templateVersionId);
    Task<List<TemplateRowDefinition>> GetRowDefinitionsAsync(int templateVersionId);
    Task<AccountingTemplateVersion> AddVersionAsync(AccountingTemplateVersion version);
    void UpdateVersion(AccountingTemplateVersion version);
    void RemoveVersion(AccountingTemplateVersion version);
    Task<TemplateFieldMapping?> GetMappingByIdAsync(int mappingId);
    void UpdateMapping(TemplateFieldMapping mapping);
}
