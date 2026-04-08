using AutoMapper;
using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Exceptions;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.GeneralLedger;
using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Application.Interfaces.Services;
using BizFlow.Domain.Entities;
using BizFlow.Domain.Enums;
using Microsoft.Extensions.Options;

namespace BizFlow.Application.Services
{
    public class GeneralLedgerService : IGeneralLedgerService
    {
        private readonly IUnitOfWork _uow;
        private readonly IBusinessLocationService _locationService;
        private readonly IMapper _mapper;
        private readonly IMessageService _messageService;
        private readonly GeneralLedgerSettings _settings;

        public GeneralLedgerService(
            IUnitOfWork uow,
            IBusinessLocationService locationService,
            IMapper mapper,
            IMessageService messageService,
            IOptions<GeneralLedgerSettings> settings)
        {
            _uow = uow;
            _locationService = locationService;
            _mapper = mapper;
            _messageService = messageService;
            _settings = settings.Value;
        }

        public async Task<PaginatedResponse<GeneralLedgerEntryDto>> ListAsync(Guid userId, GeneralLedgerQueryParams query)
        {
            await _locationService.ValidateOwnerAsync(userId, query.BusinessLocationId);

            var viewMode = (query.ViewMode ?? GeneralLedgerViewMode.Audit).Trim().ToLowerInvariant();
            if (!GeneralLedgerViewMode.IsValid(viewMode))
                throw new BadRequestException(MessageKeys.LedgerInvalidViewMode);

            if (query.TransactionTypes != null && query.TransactionTypes.Any())
            {
                foreach (var tt in query.TransactionTypes)
                {
                    if (!string.IsNullOrWhiteSpace(tt) && !GeneralLedgerTransactionType.IsValid(tt.Trim()))
                        throw new BadRequestException(MessageKeys.LedgerInvalidTransactionType);
                }
            }

            if (query.ReferenceTypes != null && query.ReferenceTypes.Any())
            {
                foreach (var rt in query.ReferenceTypes)
                {
                    if (!string.IsNullOrWhiteSpace(rt) && !GeneralLedgerReferenceType.IsValid(rt.Trim()))
                        throw new BadRequestException(MessageKeys.LedgerInvalidReferenceType);
                }
            }

            if (query.MoneyChannels != null && query.MoneyChannels.Any())
            {
                foreach (var mc in query.MoneyChannels)
                {
                    if (!string.IsNullOrWhiteSpace(mc) && !MoneyChannelType.IsValid(mc.Trim()))
                        throw new BadRequestException(MessageKeys.LedgerInvalidMoneyChannel);
                }
            }

            query.ViewMode = viewMode;

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var earliestAllowedDate = ResolveEarliestAllowedDate(today);

            if (query.FromDate.HasValue && (query.FromDate.Value < earliestAllowedDate || query.FromDate.Value > today))
                throw new BadRequestException(MessageKeys.LedgerDateOutOfRange);

            if (query.ToDate.HasValue && (query.ToDate.Value < earliestAllowedDate || query.ToDate.Value > today))
                throw new BadRequestException(MessageKeys.LedgerDateOutOfRange);

            query.ToDate ??= today;
            query.FromDate ??= earliestAllowedDate;

            if (query.FromDate > query.ToDate)
                throw new BadRequestException(MessageKeys.LedgerInvalidDateRange);

            var (items, total) = await _uow.GeneralLedgerEntries.SearchAsync(query);
            var dtos = _mapper.Map<List<GeneralLedgerEntryDto>>(items);

            var asOfDate = query.ToDate.Value;
            var pageEntryIds = dtos.Select(d => d.EntryId).ToList();

            var reversalSummary = await _uow.GeneralLedgerEntries.GetReversalSummaryAsOfAsync(pageEntryIds, asOfDate);

            // if it reverses another entry, it is not reversed, it is a reversal entry
            // if it is reversed by another entry, it is reversed
            // if it is not reversed by another entry, it is active
            foreach (var dto in dtos)
            {
                // check if it is a reversal entry
                if (dto.IsReversal)
                {
                    dto.IsReversed = false;
                    dto.ReversalEntryId = null;
                    dto.ReversalCount = 0;
                    dto.EffectiveStatus = "reversal";
                    continue;
                }

                // check if it is reversed by another entry
                if (reversalSummary.TryGetValue(dto.EntryId, out var summary))
                {
                    dto.IsReversed = true;
                    dto.ReversalCount = summary.ReversalCount;
                    dto.ReversalEntryId = summary.LatestReversalEntryId;
                    dto.EffectiveStatus = "reversed";
                }
                else
                {
                    dto.IsReversed = false;
                    dto.ReversalCount = 0;
                    dto.ReversalEntryId = null;
                    dto.EffectiveStatus = "active";
                }
            }

            if (viewMode == GeneralLedgerViewMode.Audit && dtos.Count > 0)
            {
                // Build ancestry links in batches so each row can expose its full audit chain.
                var linkMap = await BuildEntryLinkMapAsync(dtos.Select(d => d.EntryId));

                foreach (var dto in dtos)
                {
                    var chain = new List<long>();
                    var seen = new HashSet<long>();
                    var cursor = dto.EntryId;

                    while (seen.Add(cursor))
                    {
                        chain.Add(cursor);

                        if (!linkMap.TryGetValue(cursor, out var previous) || !previous.HasValue)
                            break;

                        cursor = previous.Value;
                    }

                    chain.Reverse();
                    dto.HistoryChainEntryIds = chain;
                }
            }

            var pageNumber = query.PageNumber ?? 1;
            var pageSize = query.PageSize ?? 20;

            await PopulateSourceLinksAsync(dtos);

            return new PaginatedResponse<GeneralLedgerEntryDto>(dtos, total, pageNumber, pageSize);
        }

        private async Task PopulateSourceLinksAsync(List<GeneralLedgerEntryDto> dtos)
        {
            var costIds = dtos
                .Where(d => d.Source != null && d.Source.ReferenceType == GeneralLedgerReferenceType.Cost && d.Source.ReferenceId.HasValue)
                .Select(d => d.Source.ReferenceId!.Value)
                .Distinct()
                .ToList();

            var costs = await _uow.Costs.GetByIdsAsync(costIds);
            var costDict = costs.ToDictionary(c => c.CostId);

            var revenueIds = dtos
                .Where(d => d.Source != null && d.Source.ReferenceType == GeneralLedgerReferenceType.Revenue && d.Source.ReferenceId.HasValue)
                .Select(d => d.Source.ReferenceId!.Value)
                .Distinct()
                .ToList();

            var revenues = await _uow.Revenues.GetByIdsAsync(revenueIds);
            var revenueDict = revenues.ToDictionary(r => r.RevenueId);

            foreach (var dto in dtos)
            {
                var source = dto.Source;
                if (source == null) continue;

                if (source.ReferenceType == GeneralLedgerReferenceType.Revenue)
                {
                    if (source.ReferenceId.HasValue && revenueDict.TryGetValue(source.ReferenceId.Value, out var revenue) && revenue.OrderId.HasValue)
                    {
                        source.EntityType = "order";
                        source.EntityId = revenue.OrderId.Value;
                    }
                    else
                    {
                        source.EntityType = "revenue";
                        source.EntityId = source.ReferenceId;
                    }
                }
                else if (source.ReferenceType == GeneralLedgerReferenceType.Cost)
                {
                    if (source.ReferenceId.HasValue && costDict.TryGetValue(source.ReferenceId.Value, out var cost) && cost.ImportId.HasValue)
                    {
                        source.EntityType = "import";
                        source.EntityId = cost.ImportId;
                    }
                    else
                    {
                        source.EntityType = "cost";
                        source.EntityId = source.ReferenceId;
                    }
                }
                else if (source.ReferenceType == GeneralLedgerReferenceType.DebtorPayment)
                {
                    source.EntityType = "debtor_payment";
                    source.EntityId = source.ReferenceId;
                }
                else
                {
                    source.EntityType = source.ReferenceType;
                    source.EntityId = source.ReferenceId;
                }
            }
        }

        private async Task<Dictionary<long, long?>> BuildEntryLinkMapAsync(IEnumerable<long> seedEntryIds)
        {
            var linkMap = new Dictionary<long, long?>();
            var pending = new HashSet<long>(seedEntryIds.Where(id => id > 0));

            while (pending.Count > 0)
            {
                var batchIds = pending.Where(id => !linkMap.ContainsKey(id)).ToList();
                if (batchIds.Count == 0)
                    break;

                var links = await _uow.GeneralLedgerEntries.GetEntryLinksByIdsAsync(batchIds);
                if (links.Count == 0)
                    break;

                pending.Clear();

                foreach (var (entryId, reversedEntryId) in links)
                {
                    linkMap[entryId] = reversedEntryId;

                    if (reversedEntryId.HasValue && !linkMap.ContainsKey(reversedEntryId.Value))
                        pending.Add(reversedEntryId.Value);
                }
            }

            return linkMap;
        }

        private DateOnly ResolveEarliestAllowedDate(DateOnly today)
        {
            var lookbackValue = _settings.LookbackValue <= 0 ? 1 : _settings.LookbackValue;
            var lookbackUnit = (_settings.LookbackUnit ?? "year").Trim().ToLowerInvariant();

            return lookbackUnit switch
            {
                "day" => today.AddDays(-lookbackValue),
                "month" => today.AddMonths(-lookbackValue),
                "year" => today.AddYears(-lookbackValue),
                _ => throw new InvalidOperationException(_messageService.GetMessage(MessageKeys.InvalidLookbackUnit))
            };
        }

        public async Task<GeneralLedgerEntry> RecordImportCostAsync(Cost cost)
        {
            var entry = BuildCostEntry(cost, GeneralLedgerTransactionType.ImportCost);
            await _uow.GeneralLedgerEntries.AddAsync(entry);
            return entry;
        }

        public async Task<GeneralLedgerEntry> RecordManualCostAsync(Cost cost)
        {
            var entry = BuildCostEntry(cost, GeneralLedgerTransactionType.ManualCost);
            await _uow.GeneralLedgerEntries.AddAsync(entry);
            return entry;
        }

        public async Task<int> ReverseCostEntriesAsync(Cost cost, string reversalReason)
        {
            var reasonMessage = _messageService.GetMessage(reversalReason);
            var reversalDescription = _messageService.GetMessage(MessageKeys.ReversalDescriptionFormat, reasonMessage);

            var originals = (await _uow.GeneralLedgerEntries
                .GetNonReversalByReferenceAsync(cost.BusinessLocationId, GeneralLedgerReferenceType.Cost, cost.CostId))
                .ToList();

            if (!originals.Any())
                return 0;

            var reversedEntryIds = await _uow.GeneralLedgerEntries.GetReversedEntryIdsAsync(originals.Select(o => o.EntryId));
            var candidates = originals.Where(o => !reversedEntryIds.Contains(o.EntryId)).ToList();

            foreach (var original in candidates)
            {
                var reversal = new GeneralLedgerEntry
                {
                    BusinessLocationId = original.BusinessLocationId,
                    TransactionType = original.TransactionType,
                    ReferenceType = original.ReferenceType,
                    ReferenceId = original.ReferenceId,
                    EntryDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    Description = reversalDescription,
                    DebitAmount = original.CreditAmount,
                    CreditAmount = original.DebitAmount,
                    MoneyChannel = original.MoneyChannel,
                    IsReversal = true,
                    ReversedEntryId = original.EntryId,
                    CreatedAt = DateTime.UtcNow
                };

                await _uow.GeneralLedgerEntries.AddAsync(reversal);
            }

            return candidates.Count;
        }

        public async Task<GeneralLedgerEntry> RecordDebtPaymentAsync(DebtorPaymentTransaction transaction, int businessLocationId)
        {
            var entry = new GeneralLedgerEntry
            {
                BusinessLocationId = businessLocationId,
                TransactionType = GeneralLedgerTransactionType.DebtPayment,
                ReferenceType = GeneralLedgerReferenceType.DebtorPayment,
                ReferenceId = transaction.DebtorPaymentTransactionId,
                EntryDate = DateOnly.FromDateTime(transaction.PaidAt),
                Description = transaction.Notes ?? _messageService.GetMessage(MessageKeys.LedgerDebtPaymentDescription),
                DebitAmount = Math.Abs(transaction.Amount),
                CreditAmount = 0,
                MoneyChannel = transaction.PaymentMethod,
                IsReversal = false,
                ReversedEntryId = null,
                CreatedAt = DateTime.UtcNow
            };

            await _uow.GeneralLedgerEntries.AddAsync(entry);
            return entry;
        }

        public async Task<GeneralLedgerEntry> RecordManualRevenueAsync(Revenue revenue)
        {
            var entry = new GeneralLedgerEntry
            {
                BusinessLocationId = revenue.BusinessLocationId,
                TransactionType = GeneralLedgerTransactionType.ManualRevenue,
                ReferenceType = GeneralLedgerReferenceType.Revenue,
                ReferenceId = revenue.RevenueId,
                EntryDate = revenue.RevenueDate,
                Description = revenue.Description,
                DebitAmount = revenue.Amount,
                CreditAmount = 0,
                MoneyChannel = revenue.MoneyChannel,
                IsReversal = false,
                ReversedEntryId = null,
                CreatedAt = DateTime.UtcNow
            };

            await _uow.GeneralLedgerEntries.AddAsync(entry);
            return entry;
        }

        public async Task<GeneralLedgerEntry> RecordSaleRevenueAsync(Revenue revenue)
        {
            var entry = new GeneralLedgerEntry
            {
                BusinessLocationId = revenue.BusinessLocationId,
                TransactionType = GeneralLedgerTransactionType.Sale,
                ReferenceType = GeneralLedgerReferenceType.Revenue,
                ReferenceId = revenue.RevenueId,
                EntryDate = revenue.RevenueDate,
                Description = revenue.Description,
                DebitAmount = revenue.Amount,
                CreditAmount = 0,
                MoneyChannel = revenue.MoneyChannel,
                IsReversal = false,
                ReversedEntryId = null,
                CreatedAt = DateTime.UtcNow
            };

            await _uow.GeneralLedgerEntries.AddAsync(entry);
            return entry;
        }

        public async Task<int> ReverseRevenueEntriesAsync(Revenue revenue, string reversalReason)
        {
            var reasonMessage = _messageService.GetMessage(reversalReason);
            var reversalDescription = _messageService.GetMessage(MessageKeys.ReversalDescriptionFormat, reasonMessage);

            var originals = (await _uow.GeneralLedgerEntries
                .GetNonReversalByReferenceAsync(revenue.BusinessLocationId, GeneralLedgerReferenceType.Revenue, revenue.RevenueId))
                .ToList();

            if (!originals.Any())
                return 0;

            var reversedEntryIds = await _uow.GeneralLedgerEntries.GetReversedEntryIdsAsync(originals.Select(o => o.EntryId));
            var candidates = originals.Where(o => !reversedEntryIds.Contains(o.EntryId)).ToList();

            foreach (var original in candidates)
            {
                var reversal = new GeneralLedgerEntry
                {
                    BusinessLocationId = original.BusinessLocationId,
                    TransactionType = original.TransactionType,
                    ReferenceType = original.ReferenceType,
                    ReferenceId = original.ReferenceId,
                    EntryDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    Description = reversalDescription,
                    DebitAmount = original.CreditAmount,
                    CreditAmount = original.DebitAmount,
                    MoneyChannel = original.MoneyChannel,
                    IsReversal = true,
                    ReversedEntryId = original.EntryId,
                    CreatedAt = DateTime.UtcNow
                };

                await _uow.GeneralLedgerEntries.AddAsync(reversal);
            }

            return candidates.Count;
        }

        private static GeneralLedgerEntry BuildCostEntry(Cost cost, string transactionType)
        {
            return new GeneralLedgerEntry
            {
                BusinessLocationId = cost.BusinessLocationId,
                TransactionType = transactionType,
                ReferenceType = GeneralLedgerReferenceType.Cost,
                ReferenceId = cost.CostId,
                EntryDate = cost.CostDate,
                Description = cost.Description,
                DebitAmount = 0,
                CreditAmount = cost.Amount,
                MoneyChannel = cost.PaymentMethod,
                IsReversal = false,
                ReversedEntryId = null,
                CreatedAt = DateTime.UtcNow
            };
        }
    }
}
