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
        private readonly IReferenceLabelService _labels;
        private readonly GeneralLedgerSettings _settings;

        public GeneralLedgerService(
            IUnitOfWork uow,
            IBusinessLocationService locationService,
            IMapper mapper,
            IMessageService messageService,
            IReferenceLabelService labels,
            IOptions<GeneralLedgerSettings> settings)
        {
            _uow = uow;
            _locationService = locationService;
            _mapper = mapper;
            _messageService = messageService;
            _labels = labels;
            _settings = settings.Value;
        }

        public async Task<PaginatedResponse<GeneralLedgerEntryDto>> ListAsync(Guid userId, GeneralLedgerQueryParams query)
        {
            await ValidateAndNormalizeLedgerQueryAsync(userId, query);

            var (items, total) = await _uow.GeneralLedgerEntries.SearchAsync(query);
            var itemList = items.ToList();
            var dtos = _mapper.Map<List<GeneralLedgerEntryDto>>(itemList);

            // Enrich i18n reference fields (dto fields ignored by AutoMapper profile)
            // plus track the raw reference_type code for downstream processing.
            var rawReferenceTypes = new string[dtos.Count];
            for (var i = 0; i < dtos.Count; i++)
            {
                var entity = itemList[i];
                var dto = dtos[i];

                dto.TransactionType = _labels.ToOption(ReferenceCategory.GeneralLedgerTransactionType, entity.TransactionType);
                dto.MoneyChannel = entity.MoneyChannel != null
                    && entity.MoneyChannel.Equals(PaymentMethods.System, StringComparison.OrdinalIgnoreCase)
                    ? null
                    : _labels.ToOptionOrNull(ReferenceCategory.MoneyChannelType, entity.MoneyChannel);

                if (dto.Source != null)
                {
                    dto.Source.ReferenceType = _labels.ToOption(ReferenceCategory.GeneralLedgerReferenceType, entity.ReferenceType);
                    rawReferenceTypes[i] = entity.ReferenceType;
                }
            }

            var asOfDate = query.ToDate!.Value;
            var pageEntryIds = dtos.Select(d => d.EntryId).ToList();

            var reversalSummary = await _uow.GeneralLedgerEntries.GetReversalSummaryAsOfAsync(pageEntryIds, asOfDate);

            // if it reverses another entry, it is not reversed, it is a reversal entry
            // if it is reversed by another entry, it is reversed
            // if it is not reversed by another entry, it is active
            foreach (var dto in dtos)
            {
                string effectiveStatusCode;

                // check if it is a reversal entry
                if (dto.IsReversal)
                {
                    dto.IsReversed = false;
                    dto.ReversalEntryId = null;
                    dto.ReversalCount = 0;
                    effectiveStatusCode = "reversal";
                }
                else if (reversalSummary.TryGetValue(dto.EntryId, out var summary))
                {
                    // check if it is reversed by another entry
                    dto.IsReversed = true;
                    dto.ReversalCount = summary.ReversalCount;
                    dto.ReversalEntryId = summary.LatestReversalEntryId;
                    effectiveStatusCode = "reversed";
                }
                else
                {
                    dto.IsReversed = false;
                    dto.ReversalCount = 0;
                    dto.ReversalEntryId = null;
                    effectiveStatusCode = "active";
                }

                dto.EffectiveStatus = _labels.ToOption(ReferenceCategory.LedgerEffectiveStatus, effectiveStatusCode);
            }

            if (query.ViewMode == GeneralLedgerViewMode.Audit && dtos.Count > 0)
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

            await PopulateSourceLinksAsync(dtos, rawReferenceTypes);

            return new PaginatedResponse<GeneralLedgerEntryDto>(dtos, total, pageNumber, pageSize);
        }

        public async Task<GeneralLedgerTotalsDto> GetTotalsAsync(Guid userId, GeneralLedgerTotalsQueryParams query)
        {
            await ValidateAndNormalizeTotalsQueryAsync(userId, query);
            var (totalRevenue, totalCost) = await _uow.GeneralLedgerEntries.SumRevenueAndCostAsync(query);
            return new GeneralLedgerTotalsDto
            {
                TotalRevenue = totalRevenue,
                TotalCost = totalCost
            };
        }

        private async Task ValidateAndNormalizeTotalsQueryAsync(Guid userId, GeneralLedgerTotalsQueryParams query)
        {
            await _locationService.ValidateOwnerAsync(userId, query.BusinessLocationId);

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
        }

        private async Task ValidateAndNormalizeLedgerQueryAsync(Guid userId, GeneralLedgerQueryParams query)
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
        }

        private async Task PopulateSourceLinksAsync(List<GeneralLedgerEntryDto> dtos, string[] rawReferenceTypes)
        {
            var costIds = new List<long>();
            var revenueIds = new List<long>();

            for (var i = 0; i < dtos.Count; i++)
            {
                var dto = dtos[i];
                if (dto.Source == null) continue;
                var refType = rawReferenceTypes[i];

                if (refType == GeneralLedgerReferenceType.Cost && dto.Source.ReferenceId.HasValue)
                    costIds.Add(dto.Source.ReferenceId.Value);
                else if (refType == GeneralLedgerReferenceType.Revenue && dto.Source.ReferenceId.HasValue)
                    revenueIds.Add(dto.Source.ReferenceId.Value);
            }

            var costs = await _uow.Costs.GetByIdsAsync(costIds.Distinct().ToList());
            var costDict = costs.ToDictionary(c => c.CostId);

            var revenues = await _uow.Revenues.GetByIdsAsync(revenueIds.Distinct().ToList());
            var revenueDict = revenues.ToDictionary(r => r.RevenueId);

            for (var i = 0; i < dtos.Count; i++)
            {
                var source = dtos[i].Source;
                if (source == null) continue;
                var refType = rawReferenceTypes[i];

                if (refType == GeneralLedgerReferenceType.Revenue)
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
                else if (refType == GeneralLedgerReferenceType.Cost)
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
                else if (refType == GeneralLedgerReferenceType.DebtorPayment)
                {
                    source.EntityType = "debtor_payment";
                    source.EntityId = source.ReferenceId;
                }
                else
                {
                    source.EntityType = refType;
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

        public async Task<GeneralLedgerEntry> RecordDebtPaymentAsync(
            DebtorPaymentTransaction transaction,
            int businessLocationId,
            string? debtAction = null)
        {
            var normalizedAction = NormalizeDebtAction(debtAction, transaction);
            var description = ResolveDebtDescription(normalizedAction, transaction.Notes);
            var entry = new GeneralLedgerEntry
            {
                BusinessLocationId = businessLocationId,
                TransactionType = GeneralLedgerTransactionType.DebtPayment,
                ReferenceType = GeneralLedgerReferenceType.DebtorPayment,
                ReferenceId = transaction.DebtorPaymentTransactionId,
                EntryDate = DateOnly.FromDateTime(transaction.PaidAt),
                Description = description,
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

        private string ResolveDebtDescription(string debtAction, string? notes)
        {
            if (!string.IsNullOrWhiteSpace(notes))
                return notes;

            return debtAction switch
            {
                DebtPaymentActions.DecreaseDebt => _messageService.GetMessage(MessageKeys.LedgerDebtDecreaseDescription),
                DebtPaymentActions.IncreaseDebt => _messageService.GetMessage(MessageKeys.LedgerDebtIncreaseDescription),
                DebtPaymentActions.SystemRollback => _messageService.GetMessage(MessageKeys.LedgerDebtSystemRollbackDescription),
                _ => _messageService.GetMessage(MessageKeys.LedgerDebtPaymentDescription)
            };
        }

        private static string NormalizeDebtAction(string? debtAction, DebtorPaymentTransaction transaction)
        {
            if (!string.IsNullOrWhiteSpace(debtAction))
                return debtAction.Trim().ToLowerInvariant();

            if (transaction.PaymentMethod.Equals(PaymentMethods.System, StringComparison.OrdinalIgnoreCase))
                return DebtPaymentActions.SystemRollback;

            return DebtPaymentActions.DecreaseDebt;
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

        public async Task<GeneralLedgerEntry> RecordRevenueLedgerLineFromRowAsync(Revenue revenue)
        {
            var transactionType = revenue.RevenueType.Equals(RevenueType.Sale, StringComparison.OrdinalIgnoreCase)
                ? GeneralLedgerTransactionType.Sale
                : GeneralLedgerTransactionType.ManualRevenue;

            var abs = Math.Abs(revenue.Amount);
            var debit = revenue.Amount >= 0 ? abs : 0;
            var credit = revenue.Amount >= 0 ? 0 : abs;

            var reversedGlId = await ResolveReversedGlEntryIdAsync(
                revenue.BusinessLocationId,
                GeneralLedgerReferenceType.Revenue,
                revenue.ReversedRevenueId);

            var entry = new GeneralLedgerEntry
            {
                BusinessLocationId = revenue.BusinessLocationId,
                TransactionType = transactionType,
                ReferenceType = GeneralLedgerReferenceType.Revenue,
                ReferenceId = revenue.RevenueId,
                EntryDate = revenue.RevenueDate,
                Description = revenue.Description,
                DebitAmount = debit,
                CreditAmount = credit,
                MoneyChannel = revenue.MoneyChannel,
                IsReversal = revenue.IsReversal,
                ReversedEntryId = reversedGlId,
                CreatedAt = DateTime.UtcNow
            };

            await _uow.GeneralLedgerEntries.AddAsync(entry);
            return entry;
        }

        public async Task<GeneralLedgerEntry> RecordCostLedgerLineFromRowAsync(Cost cost)
        {
            var transactionType = cost.CostType.Equals(CostType.Import, StringComparison.OrdinalIgnoreCase)
                ? GeneralLedgerTransactionType.ImportCost
                : GeneralLedgerTransactionType.ManualCost;

            var abs = Math.Abs(cost.Amount);
            var debit = cost.Amount >= 0 ? 0 : abs;
            var credit = cost.Amount >= 0 ? abs : 0;

            var reversedGlId = await ResolveReversedGlEntryIdAsync(
                cost.BusinessLocationId,
                GeneralLedgerReferenceType.Cost,
                cost.ReversedCostId);

            var entry = new GeneralLedgerEntry
            {
                BusinessLocationId = cost.BusinessLocationId,
                TransactionType = transactionType,
                ReferenceType = GeneralLedgerReferenceType.Cost,
                ReferenceId = cost.CostId,
                EntryDate = cost.CostDate,
                Description = cost.Description,
                DebitAmount = debit,
                CreditAmount = credit,
                MoneyChannel = cost.PaymentMethod,
                IsReversal = cost.IsReversal,
                ReversedEntryId = reversedGlId,
                CreatedAt = DateTime.UtcNow
            };

            await _uow.GeneralLedgerEntries.AddAsync(entry);
            return entry;
        }

        private async Task<long?> ResolveReversedGlEntryIdAsync(
            int businessLocationId,
            string referenceType,
            long? reversedSourceEntityId)
        {
            if (!reversedSourceEntityId.HasValue)
                return null;

            var originals = (await _uow.GeneralLedgerEntries.GetNonReversalByReferenceAsync(
                    businessLocationId,
                    referenceType,
                    reversedSourceEntityId.Value))
                .ToList();

            if (!originals.Any())
                return null;

            var reversedEntryIds = await _uow.GeneralLedgerEntries.GetReversedEntryIdsAsync(originals.Select(o => o.EntryId));
            var candidate = originals.FirstOrDefault(o => !reversedEntryIds.Contains(o.EntryId));
            return candidate?.EntryId;
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
