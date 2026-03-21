using BizFlow.Application.Common.Specifications;
using BizFlow.Application.DTOs.GeneralLedger;
using BizFlow.Domain.Entities;
using BizFlow.Domain.Enums;

namespace BizFlow.Application.Specifications.GeneralLedger
{
    public class GeneralLedgerSearchSpec : BaseSpecification<GeneralLedgerEntry>
    {
        public GeneralLedgerSearchSpec(GeneralLedgerQueryParams query, bool isCount, bool filterOnly = false)
            : base(e => e.BusinessLocationId == query.BusinessLocationId)
        {
            if (query.TransactionTypes != null && query.TransactionTypes.Any())
            {
                var types = query.TransactionTypes.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim().ToLower()).ToList();
                if (types.Any())
                {
                    AddCriteria(e => types.Contains(e.TransactionType.ToLower()));
                }
            }

            if (query.ReferenceTypes != null && query.ReferenceTypes.Any())
            {
                var refs = query.ReferenceTypes.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim().ToLower()).ToList();
                if (refs.Any())
                {
                    AddCriteria(e => refs.Contains(e.ReferenceType.ToLower()));
                }
            }

            if (query.MoneyChannels != null && query.MoneyChannels.Any())
            {
                var channels = query.MoneyChannels.Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => c.Trim().ToLower()).ToList();
                if (channels.Any())
                {
                    AddCriteria(e => e.MoneyChannel != null && channels.Contains(e.MoneyChannel.ToLower()));
                }
            }

            if (query.FromDate.HasValue)
                AddCriteria(e => e.EntryDate >= query.FromDate.Value);

            if (query.ToDate.HasValue)
                AddCriteria(e => e.EntryDate <= query.ToDate.Value);

            var viewMode = (query.ViewMode ?? GeneralLedgerViewMode.Audit).Trim().ToLowerInvariant();
            if (viewMode == GeneralLedgerViewMode.Effective)
            {
                if (!query.ToDate.HasValue)
                    throw new ArgumentException("ToDate is required for effective view mode.", nameof(query.ToDate));

                var asOfDate = query.ToDate.Value;

                AddCriteria(e => !e.IsReversal);
                AddCriteria(e => !e.InverseReversedEntry.Any(r => r.IsReversal && r.EntryDate <= asOfDate));
            }

            if (!filterOnly)
            {
                // No complex includes for GL yet, but applying sort rules
            }

            if (!isCount)
            {
                AddOrderByDescending(e => e.EntryDate);
                // Cannot easily chain Multiple ThenBy in generic spec using BaseSpecification as it is typical.
                // We'll let Repository add secondary sorts if needed, or rely on primary here.
            }
        }
    }
}
