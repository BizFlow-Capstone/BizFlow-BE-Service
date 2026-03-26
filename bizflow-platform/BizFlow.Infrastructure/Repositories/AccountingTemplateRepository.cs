using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Domain.Entities;
using BizFlow.Infrastructure.DataContext;
using Microsoft.EntityFrameworkCore;

namespace BizFlow.Infrastructure.Repositories;

public class AccountingTemplateRepository : IAccountingTemplateRepository
{
    private readonly BizFlowDbContext _context;

    public AccountingTemplateRepository(BizFlowDbContext context)
    {
        _context = context;
    }

    public async Task<AccountingTemplate?> GetByCodeAsync(string templateCode)
    {
        return await _context.Set<AccountingTemplate>()
            .Include(x => x.Versions.Where(v => v.IsActive))
            .FirstOrDefaultAsync(x => x.TemplateCode == templateCode && x.IsActive);
    }

    public async Task<List<AccountingTemplate>> GetActiveTemplatesAsync()
    {
        return await _context.Set<AccountingTemplate>()
            .Where(x => x.IsActive)
            .OrderBy(x => x.TemplateCode)
            .ToListAsync();
    }

    public async Task<AccountingTemplateVersion?> GetActiveVersionByTemplateIdAsync(int templateId)
    {
        return await _context.Set<AccountingTemplateVersion>()
            .FirstOrDefaultAsync(x => x.TemplateId == templateId && x.IsActive);
    }

    public async Task<AccountingTemplateVersion?> GetVersionWithMappingsAsync(int templateVersionId)
    {
        return await _context.Set<AccountingTemplateVersion>()
            .Include(x => x.Template)
            .Include(x => x.FieldMappings.OrderBy(m => m.SortOrder))
                .ThenInclude(m => m.Formula)
            .FirstOrDefaultAsync(x => x.TemplateVersionId == templateVersionId);
    }
}
