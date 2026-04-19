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
            .Include(x => x.RowDefinitions.OrderBy(r => r.Position).ThenBy(r => r.SortOrder))
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
            .Include(x => x.RowDefinitions.OrderBy(r => r.Position).ThenBy(r => r.SortOrder))
            .FirstOrDefaultAsync(x => x.TemplateVersionId == templateVersionId);
    }

    public async Task<List<TemplateRowDefinition>> GetRowDefinitionsAsync(int templateVersionId)
    {
        return await _context.Set<TemplateRowDefinition>()
            .Include(x => x.Formula)
            .Where(x => x.TemplateVersionId == templateVersionId)
            .OrderBy(x => x.Position)
            .ThenBy(x => x.SortOrder)
            .ToListAsync();
    }

    public async Task<bool> ExistsByCodeAsync(string templateCode)
        => await _context.Set<AccountingTemplate>().AnyAsync(x => x.TemplateCode == templateCode);

    public async Task<AccountingTemplate?> GetByIdWithVersionsAsync(int templateId)
        => await _context.Set<AccountingTemplate>()
            .Include(x => x.Versions.OrderByDescending(v => v.CreatedAt))
                .ThenInclude(v => v.AccountingBooks)
            .Include(x => x.Versions)
                .ThenInclude(v => v.FieldMappings)
            .FirstOrDefaultAsync(x => x.TemplateId == templateId);

    public async Task AddTemplateAsync(AccountingTemplate template)
        => await _context.Set<AccountingTemplate>().AddAsync(template);

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

    public async Task AddMappingAsync(TemplateFieldMapping mapping)
    {
        await _context.Set<TemplateFieldMapping>().AddAsync(mapping);
    }

    public void RemoveMapping(TemplateFieldMapping mapping)
    {
        _context.Set<TemplateFieldMapping>().Remove(mapping);
    }

    // ── RowDefinitions ──

    public async Task<TemplateRowDefinition?> GetRowDefinitionByIdAsync(int rowDefId)
    {
        return await _context.Set<TemplateRowDefinition>()
            .Include(x => x.Formula)
            .Include(x => x.TemplateVersion).ThenInclude(v => v.Template)
            .FirstOrDefaultAsync(x => x.RowDefId == rowDefId);
    }

    public async Task AddRowDefinitionAsync(TemplateRowDefinition rowDef)
    {
        await _context.Set<TemplateRowDefinition>().AddAsync(rowDef);
    }

    public void UpdateRowDefinition(TemplateRowDefinition rowDef)
    {
        _context.Set<TemplateRowDefinition>().Update(rowDef);
    }

    public void RemoveRowDefinition(TemplateRowDefinition rowDef)
    {
        _context.Set<TemplateRowDefinition>().Remove(rowDef);
    }

    // ── MappableEntities ──

    public async Task<List<MappableEntity>> GetAllMappableEntitiesAsync(bool? active)
    {
        var query = _context.Set<MappableEntity>()
            .Include(e => e.Fields)
            .AsQueryable();

        if (active.HasValue)
            query = query.Where(e => e.IsActive == active.Value);

        return await query.OrderBy(e => e.Category).ThenBy(e => e.EntityCode).ToListAsync();
    }

    public async Task<MappableEntity?> GetMappableEntityWithFieldsAsync(int entityId)
    {
        return await _context.Set<MappableEntity>()
            .Include(e => e.Fields.OrderBy(f => f.FieldCode))
            .FirstOrDefaultAsync(e => e.EntityId == entityId);
    }

    public async Task<MappableEntity?> GetMappableEntityByCodeAsync(string entityCode)
    {
        return await _context.Set<MappableEntity>()
            .FirstOrDefaultAsync(e => e.EntityCode == entityCode);
    }

    public async Task AddMappableEntityAsync(MappableEntity entity)
    {
        await _context.Set<MappableEntity>().AddAsync(entity);
    }

    public void UpdateMappableEntity(MappableEntity entity)
    {
        _context.Set<MappableEntity>().Update(entity);
    }

    public void RemoveMappableEntity(MappableEntity entity)
    {
        _context.Set<MappableEntity>().Remove(entity);
    }

    // ── MappableFields ──

    public async Task<MappableField?> GetMappableFieldByIdAsync(int fieldId)
    {
        return await _context.Set<MappableField>()
            .Include(f => f.Entity)
            .FirstOrDefaultAsync(f => f.FieldId == fieldId);
    }

    public async Task AddMappableFieldAsync(MappableField field)
    {
        await _context.Set<MappableField>().AddAsync(field);
    }

    public void UpdateMappableField(MappableField field)
    {
        _context.Set<MappableField>().Update(field);
    }
}
