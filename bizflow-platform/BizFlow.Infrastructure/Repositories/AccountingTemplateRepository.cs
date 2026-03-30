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

    public async Task<List<AccountingTemplate>> GetAllWithVersionsAsync()
    {
        return await _context.Set<AccountingTemplate>()
            .Include(x => x.Versions.OrderByDescending(v => v.CreatedAt))
                .ThenInclude(v => v.AccountingBooks)
            .Include(x => x.Versions)
                .ThenInclude(v => v.FieldMappings)
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
            .Include(x => x.FieldMappings.OrderBy(m => m.SortOrder))
                .ThenInclude(m => m.SourceField)
            .Include(x => x.FieldMappings.OrderBy(m => m.SortOrder))
                .ThenInclude(m => m.SourceEntity)
            .FirstOrDefaultAsync(x => x.TemplateVersionId == templateVersionId);
    }

    public async Task<AccountingTemplateVersion?> GetVersionWithMappingsAndBooksAsync(int templateVersionId)
    {
        return await _context.Set<AccountingTemplateVersion>()
            .Include(x => x.Template)
            .Include(x => x.AccountingBooks)
            .Include(x => x.FieldMappings.OrderBy(m => m.SortOrder))
                .ThenInclude(m => m.Formula)
            .Include(x => x.FieldMappings.OrderBy(m => m.SortOrder))
                .ThenInclude(m => m.SourceField)
            .Include(x => x.FieldMappings.OrderBy(m => m.SortOrder))
                .ThenInclude(m => m.SourceEntity)
            .FirstOrDefaultAsync(x => x.TemplateVersionId == templateVersionId);
    }

    public async Task<AccountingTemplateVersion> AddVersionAsync(AccountingTemplateVersion version)
    {
        await _context.Set<AccountingTemplateVersion>().AddAsync(version);
        return version;
    }

    public void UpdateVersion(AccountingTemplateVersion version)
    {
        _context.Set<AccountingTemplateVersion>().Update(version);
    }

    public void RemoveVersion(AccountingTemplateVersion version)
    {
        _context.Set<AccountingTemplateVersion>().Remove(version);
    }

    public async Task<TemplateFieldMapping?> GetMappingByIdAsync(int mappingId)
    {
        return await _context.Set<TemplateFieldMapping>()
            .Include(x => x.TemplateVersion)
            .ThenInclude(v => v.Template)
            .FirstOrDefaultAsync(x => x.MappingId == mappingId);
    }

    public void UpdateMapping(TemplateFieldMapping mapping)
    {
        _context.Set<TemplateFieldMapping>().Update(mapping);
    }
}
