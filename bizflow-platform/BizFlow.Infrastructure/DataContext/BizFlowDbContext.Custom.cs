using BizFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BizFlow.Infrastructure.DataContext;

public partial class BizFlowDbContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        // Global Query Filter: Soft Delete for BusinessLocation
        modelBuilder.Entity<BusinessLocation>().HasQueryFilter(e => e.DeletedAt == null);

        // Global Query Filter: Soft Delete for Product (+ Parent Location Check)
        modelBuilder.Entity<Product>().HasQueryFilter(e => e.DeletedAt == null && e.BusinessLocation.DeletedAt == null);

        // Global Query Filter: Soft Delete for SaleItem (+ Parent Product Check)
        modelBuilder.Entity<SaleItem>().HasQueryFilter(s => s.DeletedAt == null && s.Product.DeletedAt == null);

        // Global Query Filter: Soft Delete for ImportSchema
        modelBuilder.Entity<ImportSchema>().HasQueryFilter(e => e.DeletedAt == null);
    }
}
