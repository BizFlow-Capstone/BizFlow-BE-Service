using System.Globalization;
using AutoMapper;
using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Exceptions;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.Accounting;
using BizFlow.Application.DTOs.Cost;
using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Application.Interfaces.Services;
using BizFlow.Domain.Entities;
using BizFlow.Domain.Enums;

namespace BizFlow.Application.Services
{
    public class CostService : ICostService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly IBusinessLocationService _locationService;
        private readonly IImageService _imageService;
        private readonly IGeneralLedgerService _generalLedgerService;
        private readonly IReferenceLabelService _labels;
        private readonly IDocumentNumberRegistryService _documentNumberRegistry;
        private readonly IMessageService _messageService;

        public CostService(
            IUnitOfWork uow,
            IMapper mapper,
            IBusinessLocationService locationService,
            IImageService imageService,
            IGeneralLedgerService generalLedgerService,
            IReferenceLabelService labels,
            IDocumentNumberRegistryService documentNumberRegistry,
            IMessageService messageService)
        {
            _uow = uow;
            _mapper = mapper;
            _locationService = locationService;
            _imageService = imageService;
            _generalLedgerService = generalLedgerService;
            _labels = labels;
            _documentNumberRegistry = documentNumberRegistry;
            _messageService = messageService;
        }

        private CostDto ToDto(Cost cost)
        {
            var dto = _mapper.Map<CostDto>(cost);
            dto.CostType = _labels.ToOption(ReferenceCategory.CostType, cost.CostType);
            dto.PaymentMethod = _labels.ToOptionOrNull(ReferenceCategory.PaymentMethod, cost.PaymentMethod);
            dto.Status = _labels.ToOption(ReferenceCategory.CostStatus, cost.Status);
            return dto;
        }

        public async Task<CostDto> CreateManualAsync(Guid userId, CreateManualCostRequest request)
        {
            await _locationService.ValidateOwnerAsync(userId, request.BusinessLocationId);

            var normalizedType = request.CostType.Trim().ToLower();
            if (!CostType.IsValid(normalizedType))
                throw new BadRequestException(MessageKeys.BadRequest);

            if (normalizedType == CostType.Import)
                throw new BadRequestException(MessageKeys.BadRequest);

            string? normalizedPaymentMethod = null;
            if (!string.IsNullOrWhiteSpace(request.PaymentMethod))
            {
                if (!PaymentMethods.IsValid(request.PaymentMethod))
                    throw new BadRequestException(MessageKeys.BadRequest);

                normalizedPaymentMethod = request.PaymentMethod.Trim().ToLower();
            }

            var docNumberNormalized = _documentNumberRegistry.NormalizeOrNull(request.DocumentNumber);

            string? documentUrl = null;
            string? documentPublicId = null;

            if (request.ImageStream != null)
            {
                var imageInfo = await _imageService.UploadImageAsync(
                    request.ImageStream, request.ImageFileName, ImageUploadTarget.Costs);
                documentUrl = imageInfo.Url;
                documentPublicId = imageInfo.PublicId;
            }

            var ownerId = await ResolveOwnerIdAsync(request.BusinessLocationId);

            var entity = await _uow.ExecuteResilientAsync(async ct =>
            {
                if (docNumberNormalized != null)
                {
                    await _documentNumberRegistry.EnsureLockAndAssertUniqueAsync(
                        ownerId, docNumberNormalized, cancellationToken: ct);
                }

                var cost = new Cost
                {
                    BusinessLocationId = request.BusinessLocationId,
                    BusinessTypeId = request.BusinessTypeId,
                    CostType = normalizedType,
                    Description = request.Description.Trim(),
                    Amount = request.Amount,
                    Status = CostStatus.Posted,
                    CostDate = request.CostDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
                    PaymentMethod = normalizedPaymentMethod,
                    DocumentNumber = docNumberNormalized,
                    DocumentDate = request.DocumentDate,
                    DocumentUrl = documentUrl,
                    DocumentPublicId = documentPublicId,
                    CreatedBy = userId,
                    CreatedAt = DateTime.UtcNow
                };

                await _uow.Costs.AddAsync(cost);
                await _uow.SaveChangesAsync(ct);

                await _generalLedgerService.RecordCostLedgerLineFromRowAsync(cost);
                await _uow.SaveChangesAsync(ct);

                return cost;
            });

            return ToDto(entity);
        }

        public async Task<ManualCostUpdateResponseDto> UpdateManualAsync(Guid userId, long costId, UpdateManualCostRequest request)
        {
            var costPreview = await _uow.Costs.GetByIdAsync(costId)
                ?? throw new NotFoundException(MessageKeys.CostNotFound);

            await _locationService.ValidateOwnerAsync(userId, costPreview.BusinessLocationId);

            if (costPreview.CostType.Equals(CostType.Import, StringComparison.OrdinalIgnoreCase))
                throw new BadRequestException(MessageKeys.BadRequest);

            if (CostStatus.IsTerminal(costPreview.Status))
                throw new BadRequestException(MessageKeys.CostCannotEditCancelledOrReplaced);

            // ── Draft: in-place update (no replacement, no GL on the row) ──
            if (CostStatus.IsEditableInPlace(costPreview.Status))
                return new ManualCostUpdateResponseDto
                {
                    IsReplacement = false,
                    Cost = await UpdateManualDraftInPlaceAsync(userId, costPreview, request)
                };

            // ── Posted: replace-when-posted ──
            if (!string.Equals(costPreview.Status, CostStatus.Posted, StringComparison.OrdinalIgnoreCase))
                throw new BadRequestException(MessageKeys.CostInvalidStatus);

            if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
                throw new BadRequestException(MessageKeys.CostEditPostedRequiresIdempotencyKey);

            var idemKey = request.IdempotencyKey.Trim();
            if (idemKey.Length > 100)
                throw new BadRequestException(MessageKeys.BadRequest);

            var existingReplacement = await _uow.Costs.GetLatestReplacementByRefCostIdAsync(costId, idemKey);
            if (existingReplacement != null)
            {
                return new ManualCostUpdateResponseDto
                {
                    IsReplacement = true,
                    Replacement = new PostedRecordReplacementResultDto
                    {
                        OldRecordId = costId,
                        OldRecordStatus = _labels.ToOption(ReferenceCategory.CostStatus, CostStatus.Replaced),
                        NewRecordId = existingReplacement.CostId,
                        NewRecordStatus = _labels.ToOption(ReferenceCategory.CostStatus, existingReplacement.Status)
                    }
                };
            }

            var ownerId = await ResolveOwnerIdAsync(costPreview.BusinessLocationId);
            var docNumberNormalized = _documentNumberRegistry.NormalizeOrNull(request.DocumentNumber);

            string? normalizedCostType = costPreview.CostType;
            if (!string.IsNullOrWhiteSpace(request.CostType))
            {
                normalizedCostType = request.CostType.Trim().ToLower();
                if (!CostType.IsValid(normalizedCostType))
                    throw new BadRequestException(MessageKeys.BadRequest);

                if (normalizedCostType == CostType.Import)
                    throw new BadRequestException(MessageKeys.BadRequest);
            }

            string? normalizedPaymentMethod = null;
            if (!string.IsNullOrWhiteSpace(request.PaymentMethod))
            {
                if (!PaymentMethods.IsValid(request.PaymentMethod))
                    throw new BadRequestException(MessageKeys.BadRequest);

                normalizedPaymentMethod = request.PaymentMethod.Trim().ToLower();
            }

            var replacementCostDate = request.CostDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

            var newCost = await _uow.ExecuteResilientAsync(async ct =>
            {
                await _uow.Costs.LockCostRowForUpdateAsync(costId, ct);

                var old = await _uow.Costs.GetByIdAsync(costId)
                    ?? throw new NotFoundException(MessageKeys.CostNotFound);

                if (CostStatus.IsTerminal(old.Status))
                    throw new BadRequestException(MessageKeys.CostCannotEditCancelledOrReplaced);

                if (!string.Equals(old.Status, CostStatus.Posted, StringComparison.OrdinalIgnoreCase))
                    throw new BadRequestException(MessageKeys.CostInvalidStatus);

                var dup = await _uow.Costs.GetLatestReplacementByRefCostIdAsync(costId, idemKey, ct);
                if (dup != null)
                    return dup;

                if (docNumberNormalized != null)
                {
                    await _documentNumberRegistry.EnsureLockAndAssertUniqueAsync(
                        ownerId,
                        docNumberNormalized,
                        excludeCostId: old.CostId,
                        cancellationToken: ct);
                }

                var replaceReversalDesc = BuildReplacePostedManualCostReversalDescription(old);
                var costReversal = new Cost
                {
                    BusinessLocationId = old.BusinessLocationId,
                    BusinessTypeId = old.BusinessTypeId,
                    CostType = old.CostType,
                    ImportId = old.ImportId,
                    Description = replaceReversalDesc,
                    Amount = -old.Amount,
                    Status = CostStatus.Posted,
                    CostDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    PaymentMethod = old.PaymentMethod,
                    IsReversal = true,
                    ReversedCostId = old.CostId,
                    CreatedBy = userId,
                    CreatedAt = DateTime.UtcNow
                };
                await _uow.Costs.AddAsync(costReversal);
                await _uow.SaveChangesAsync(ct);
                await _generalLedgerService.RecordCostLedgerLineFromRowAsync(costReversal);
                await _uow.SaveChangesAsync(ct);

                old.Status = CostStatus.Replaced;
                old.CancelledAt = DateTime.UtcNow;
                old.CancelledBy = userId;
                old.UpdatedAt = DateTime.UtcNow;
                _uow.Costs.Update(old);

                string? docUrl = old.DocumentUrl;
                string? docPublicId = old.DocumentPublicId;

                if (request.RemoveDocument && !string.IsNullOrEmpty(old.DocumentPublicId))
                {
                    docUrl = null;
                    docPublicId = null;
                }

                if (request.ImageStream != null)
                {
                    var imageInfo = await _imageService.UploadImageAsync(
                        request.ImageStream, request.ImageFileName, ImageUploadTarget.Costs);
                    docUrl = imageInfo.Url;
                    docPublicId = imageInfo.PublicId;
                }

                var created = new Cost
                {
                    BusinessLocationId = old.BusinessLocationId,
                    BusinessTypeId = request.BusinessTypeId,
                    CostType = normalizedCostType!,
                    Description = request.Description.Trim(),
                    Amount = request.Amount,
                    Status = CostStatus.Posted,
                    CostDate = replacementCostDate,
                    PaymentMethod = normalizedPaymentMethod,
                    DocumentNumber = docNumberNormalized,
                    DocumentDate = request.DocumentDate,
                    DocumentUrl = docUrl,
                    DocumentPublicId = docPublicId,
                    RefCostId = old.CostId,
                    IdempotencyKey = idemKey,
                    CreatedBy = userId,
                    CreatedAt = DateTime.UtcNow
                };

                await _uow.Costs.AddAsync(created);
                await _uow.SaveChangesAsync(ct);

                await _generalLedgerService.RecordCostLedgerLineFromRowAsync(created);
                await _uow.SaveChangesAsync(ct);

                return created;
            });

            return new ManualCostUpdateResponseDto
            {
                IsReplacement = true,
                Replacement = new PostedRecordReplacementResultDto
                {
                    OldRecordId = costId,
                    OldRecordStatus = _labels.ToOption(ReferenceCategory.CostStatus, CostStatus.Replaced),
                    NewRecordId = newCost.CostId,
                    NewRecordStatus = _labels.ToOption(ReferenceCategory.CostStatus, newCost.Status)
                }
            };
        }

        private async Task<CostDto> UpdateManualDraftInPlaceAsync(Guid userId, Cost cost, UpdateManualCostRequest request)
        {
            if (cost.CostType.Equals(CostType.Import, StringComparison.OrdinalIgnoreCase))
                throw new BadRequestException(MessageKeys.BadRequest);

            string? normalizedCostType = cost.CostType;
            if (!string.IsNullOrWhiteSpace(request.CostType))
            {
                normalizedCostType = request.CostType.Trim().ToLower();
                if (!CostType.IsValid(normalizedCostType))
                    throw new BadRequestException(MessageKeys.BadRequest);

                if (normalizedCostType == CostType.Import)
                    throw new BadRequestException(MessageKeys.BadRequest);
            }

            string? normalizedPaymentMethod = null;
            if (!string.IsNullOrWhiteSpace(request.PaymentMethod))
            {
                if (!PaymentMethods.IsValid(request.PaymentMethod))
                    throw new BadRequestException(MessageKeys.BadRequest);

                normalizedPaymentMethod = request.PaymentMethod.Trim().ToLower();
            }

            var docNumberNormalized = _documentNumberRegistry.NormalizeOrNull(request.DocumentNumber);
            var ownerId = await ResolveOwnerIdAsync(cost.BusinessLocationId);

            await _uow.ExecuteResilientAsync(async ct =>
            {
                if (docNumberNormalized != null
                    && !string.Equals(docNumberNormalized, cost.DocumentNumberNormalized, StringComparison.Ordinal))
                {
                    await _documentNumberRegistry.EnsureLockAndAssertUniqueAsync(
                        ownerId,
                        docNumberNormalized,
                        excludeCostId: cost.CostId,
                        cancellationToken: ct);
                }

                cost.CostType = normalizedCostType!;
                cost.BusinessTypeId = request.BusinessTypeId;
                cost.Description = request.Description.Trim();
                cost.Amount = request.Amount;
                cost.CostDate = request.CostDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
                cost.PaymentMethod = normalizedPaymentMethod;
                cost.DocumentNumber = docNumberNormalized;
                cost.DocumentDate = request.DocumentDate;
                cost.UpdatedAt = DateTime.UtcNow;

                if (request.RemoveDocument && !string.IsNullOrEmpty(cost.DocumentPublicId))
                {
                    cost.DocumentUrl = null;
                    cost.DocumentPublicId = null;
                }

                if (request.ImageStream != null)
                {
                    var imageInfo = await _imageService.UploadImageAsync(
                        request.ImageStream, request.ImageFileName, ImageUploadTarget.Costs);
                    cost.DocumentUrl = imageInfo.Url;
                    cost.DocumentPublicId = imageInfo.PublicId;
                }

                _uow.Costs.Update(cost);
                await _uow.SaveChangesAsync(ct);
            });

            return ToDto(cost);
        }

        public async Task<PaginatedResponse<CostDto>> ListAsync(Guid userId, CostQueryParams query)
        {
            await _locationService.ValidateOwnerAsync(userId, query.BusinessLocationId);

            if (!string.IsNullOrWhiteSpace(query.CostType)
                && !CostType.IsValid(query.CostType.Trim()))
                throw new BadRequestException(MessageKeys.BadRequest);

            if (!string.IsNullOrWhiteSpace(query.Status)
                && !CostStatus.IsValid(query.Status.Trim()))
                throw new BadRequestException(MessageKeys.BadRequest);

            if (!string.IsNullOrWhiteSpace(query.PaymentMethod)
                && !PaymentMethods.IsValid(query.PaymentMethod.Trim()))
                throw new BadRequestException(MessageKeys.BadRequest);

            if (!string.IsNullOrWhiteSpace(query.CostType))
                query.CostType = query.CostType.Trim().ToLowerInvariant();

            if (!string.IsNullOrWhiteSpace(query.Status))
                query.Status = query.Status.Trim().ToLowerInvariant();

            if (!string.IsNullOrWhiteSpace(query.PaymentMethod))
                query.PaymentMethod = query.PaymentMethod.Trim().ToLowerInvariant();

            var (items, total) = await _uow.Costs.SearchAsync(query);
            var dtos = items.Select(ToDto).ToList();

            var pageNumber = query.PageNumber ?? 1;
            var pageSize = query.PageSize ?? 20;
            return new PaginatedResponse<CostDto>(dtos, total, pageNumber, pageSize);
        }

        public async Task DeleteManualAsync(Guid userId, long costId)
        {
            var cost = await _uow.Costs.GetByIdAsync(costId)
                ?? throw new NotFoundException(MessageKeys.CostNotFound);

            await _locationService.ValidateOwnerAsync(userId, cost.BusinessLocationId);

            if (cost.CostType.Equals(CostType.Import, StringComparison.OrdinalIgnoreCase))
                throw new BadRequestException(MessageKeys.BadRequest);

            if (CostStatus.IsTerminal(cost.Status))
                throw new BadRequestException(MessageKeys.CostCannotEditCancelledOrReplaced);

            if (string.Equals(cost.Status, CostStatus.Draft, StringComparison.OrdinalIgnoreCase))
            {
                cost.Status = CostStatus.Cancelled;
                cost.CancelledAt = DateTime.UtcNow;
                cost.CancelledBy = userId;
                cost.UpdatedAt = DateTime.UtcNow;
                _uow.Costs.Update(cost);
                await _uow.SaveChangesAsync();
                return;
            }

            await _uow.ExecuteResilientAsync(async ct =>
            {
                await _uow.Costs.LockCostRowForUpdateAsync(costId, ct);

                var current = await _uow.Costs.GetByIdAsync(costId)
                    ?? throw new NotFoundException(MessageKeys.CostNotFound);

                if (await _uow.Costs.HasActiveReversalForCostAsync(current.CostId, ct))
                    return;

                var deleteReversalDesc = BuildReplacePostedManualCostReversalDescription(current);
                var reversal = new Cost
                {
                    BusinessLocationId = current.BusinessLocationId,
                    BusinessTypeId = current.BusinessTypeId,
                    CostType = current.CostType,
                    ImportId = current.ImportId,
                    Description = deleteReversalDesc,
                    Amount = -current.Amount,
                    Status = CostStatus.Posted,
                    CostDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    PaymentMethod = current.PaymentMethod,
                    IsReversal = true,
                    ReversedCostId = current.CostId,
                    CreatedBy = userId,
                    CreatedAt = DateTime.UtcNow
                };

                await _uow.Costs.AddAsync(reversal);
                await _uow.SaveChangesAsync(ct);
                await _generalLedgerService.RecordCostLedgerLineFromRowAsync(reversal);

                current.Status = CostStatus.Replaced;
                current.CancelledAt = DateTime.UtcNow;
                current.CancelledBy = userId;
                current.UpdatedAt = DateTime.UtcNow;
                _uow.Costs.Update(current);
                await _uow.SaveChangesAsync(ct);
            });
        }

        public Task<Cost> CreateImportCostAsync(
            Guid userId,
            Import import,
            string? documentNumber = null,
            DateOnly? documentDate = null,
            string? paymentMethod = null)
            => _uow.ExecuteResilientAsync(ct => CreateImportCostCoreAsync(userId, import, documentNumber, documentDate, paymentMethod, ct));

        public Task<Cost> CreateImportCostInCurrentTransactionAsync(
            Guid userId,
            Import import,
            string? documentNumber = null,
            DateOnly? documentDate = null,
            string? paymentMethod = null,
            CancellationToken cancellationToken = default)
            => CreateImportCostCoreAsync(userId, import, documentNumber, documentDate, paymentMethod, cancellationToken);

        private async Task<Cost> CreateImportCostCoreAsync(
            Guid userId,
            Import import,
            string? documentNumber,
            DateOnly? documentDate,
            string? paymentMethod,
            CancellationToken cancellationToken)
        {
            var docNumberNormalized = _documentNumberRegistry.NormalizeOrNull(documentNumber);
            string? normalizedPaymentMethod = null;
            if (!string.IsNullOrWhiteSpace(paymentMethod))
            {
                if (!PaymentMethods.IsValid(paymentMethod))
                    throw new BadRequestException(MessageKeys.BadRequest);

                normalizedPaymentMethod = paymentMethod.Trim().ToLowerInvariant();
            }

            var ownerId = await ResolveOwnerIdAsync(import.BusinessLocationId);

            var existing = await _uow.Costs.GetByImportIdAsync(import.ImportId);
            if (existing != null)
            {
                if (string.Equals(existing.Status, CostStatus.Cancelled, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(existing.Status, CostStatus.Replaced, StringComparison.OrdinalIgnoreCase))
                {
                    if (docNumberNormalized != null
                        && !string.Equals(docNumberNormalized, existing.DocumentNumberNormalized, StringComparison.Ordinal))
                    {
                        await _documentNumberRegistry.EnsureLockAndAssertUniqueAsync(
                            ownerId, docNumberNormalized, excludeCostId: existing.CostId, cancellationToken: cancellationToken);
                    }

                    existing.Status = CostStatus.Posted;
                    existing.UpdatedAt = DateTime.UtcNow;
                    existing.Description = ResolveImportCostDescription(import);
                    existing.DocumentNumber = docNumberNormalized;
                    existing.DocumentDate = documentDate;
                    existing.PaymentMethod = normalizedPaymentMethod;
                    _uow.Costs.Update(existing);

                    await _generalLedgerService.RecordCostLedgerLineFromRowAsync(existing);
                }

                return existing;
            }

            if (docNumberNormalized != null)
            {
                await _documentNumberRegistry.EnsureLockAndAssertUniqueAsync(
                    ownerId, docNumberNormalized, cancellationToken: cancellationToken);
            }

            var cost = new Cost
            {
                BusinessLocationId = import.BusinessLocationId,
                CostType = CostType.Import,
                ImportId = import.ImportId,
                Description = ResolveImportCostDescription(import),
                Amount = import.TotalAmount,
                Status = CostStatus.Posted,
                CostDate = DateOnly.FromDateTime(import.ReceivedAt ?? import.ConfirmedAt ?? import.CreatedAt),
                PaymentMethod = normalizedPaymentMethod,
                DocumentNumber = docNumberNormalized,
                DocumentDate = documentDate,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow
            };

            await _uow.Costs.AddAsync(cost);
            await _uow.SaveChangesAsync(cancellationToken);

            await _generalLedgerService.RecordCostLedgerLineFromRowAsync(cost);

            return cost;
        }

        private static string ResolveImportCostDescription(Import import)
        {
            if (!string.IsNullOrWhiteSpace(import.Note))
                return import.Note.Trim();

            return $"Nhập hàng {import.ImportCode ?? import.ImportId.ToString()}";
        }

        public async Task ReverseImportCostAsync(Guid userId, Import import, string? reason = null)
        {
            var cost = await _uow.Costs.GetByImportIdAsync(import.ImportId);
            if (cost == null)
                return;

            if (cost.IsReversal)
                return;

            if (await _uow.Costs.HasActiveReversalForCostAsync(cost.CostId))
                return;

            var reversalDesc = BuildReplacePostedManualCostReversalDescription(cost);
            var reversal = new Cost
            {
                BusinessLocationId = cost.BusinessLocationId,
                BusinessTypeId = cost.BusinessTypeId,
                CostType = CostType.Import,
                ImportId = null,
                Description = reversalDesc,
                Amount = -cost.Amount,
                Status = CostStatus.Posted,
                CostDate = DateOnly.FromDateTime(DateTime.UtcNow),
                PaymentMethod = cost.PaymentMethod,
                IsReversal = true,
                ReversedCostId = cost.CostId,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow
            };

            await _uow.Costs.AddAsync(reversal);
            await _uow.SaveChangesAsync();
            await _generalLedgerService.RecordCostLedgerLineFromRowAsync(reversal);

            cost.Status = CostStatus.Replaced;
            cost.CancelledAt = DateTime.UtcNow;
            cost.CancelledBy = userId;
            cost.UpdatedAt = DateTime.UtcNow;
            _uow.Costs.Update(cost);
            await _uow.SaveChangesAsync();
        }

        private string BuildLedgerReversalDescription(string reversalReasonMessageKey)
        {
            var reasonMessage = _messageService.GetMessage(reversalReasonMessageKey);
            return _messageService.GetMessage(MessageKeys.ReversalDescriptionFormat, reasonMessage);
        }

        /// <summary>Same pattern as revenue: prefix + source cost date + document (replace or delete posted).</summary>
        private string BuildReplacePostedManualCostReversalDescription(Cost supersededCost)
        {
            var prefix = _messageService.GetMessage(MessageKeys.LedgerCostReplaceReversalPrefix);
            var datePart = supersededCost.CostDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var doc = !string.IsNullOrWhiteSpace(supersededCost.DocumentNumber)
                ? supersededCost.DocumentNumber.Trim()
                : supersededCost.DocumentNumberNormalized?.Trim();
            if (string.IsNullOrWhiteSpace(doc))
                return $"{prefix} {datePart}";
            return $"{prefix} {datePart} {doc}";
        }

        private async Task<Guid> ResolveOwnerIdAsync(int locationId)
        {
            var ownerId = await _uow.BusinessLocations.GetOwnerIdByLocationAsync(locationId)
                ?? throw new NotFoundException(MessageKeys.NotFound);
            return ownerId;
        }
    }
}
