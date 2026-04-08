using BizFlow.Application.Common.Specifications;
using BizFlow.Application.DTOs.Import;
using BizFlow.Domain.Entities;

namespace BizFlow.Application.Specifications.Imports
{
    public class ImportSearchSpec : BaseSpecification<Import>
    {
        public ImportSearchSpec(ImportQueryParams query, bool isCount, bool filterOnly = false)
            : base(x => true) // Base criteria can be anything, will be combined
        {
            if (query.BusinessLocationId.HasValue)
                AddCriteria(i => i.BusinessLocationId == query.BusinessLocationId.Value);

            if (!string.IsNullOrWhiteSpace(query.Status))
                AddCriteria(i => i.Status == query.Status);

            if (!string.IsNullOrWhiteSpace(query.ImportType))
                AddCriteria(i => i.ImportType == query.ImportType);

            if (query.FromDate.HasValue)
                AddCriteria(i => i.CreatedAt >= query.FromDate.Value);

            if (query.ToDate.HasValue)
                AddCriteria(i => i.CreatedAt <= query.ToDate.Value);

            if (!filterOnly)
            {
                AddInclude(i => i.BusinessLocation);
            }

            if (!isCount)
            {
                AddOrderByDescending(i => i.CreatedAt);
            }
        }
    }
}
