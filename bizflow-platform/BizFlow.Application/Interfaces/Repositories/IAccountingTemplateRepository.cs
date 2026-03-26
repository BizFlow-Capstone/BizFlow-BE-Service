using BizFlow.Domain.Entities;

namespace BizFlow.Application.Interfaces.Repositories;

public interface IAccountingTemplateRepository
{
    Task<AccountingTemplate?> GetByCodeAsync(string templateCode);
    Task<List<AccountingTemplate>> GetActiveTemplatesAsync();
    Task<AccountingTemplateVersion?> GetActiveVersionByTemplateIdAsync(int templateId);
    Task<AccountingTemplateVersion?> GetVersionWithMappingsAsync(int templateVersionId);
}
