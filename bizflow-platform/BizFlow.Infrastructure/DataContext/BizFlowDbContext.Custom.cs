using BizFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BizFlow.Infrastructure.DataContext;

public partial class BizFlowDbContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        // Global Query Filter: Soft Delete for BusinessLocation
        modelBuilder.Entity<BusinessLocation>().HasQueryFilter(e => !e.IsDeleted);

        // Global Query Filter: Soft Delete for Product (+ Parent Location Check)
        modelBuilder.Entity<Product>().HasQueryFilter(e => !e.IsDeleted && !e.BusinessLocation.IsDeleted);
    }
}
