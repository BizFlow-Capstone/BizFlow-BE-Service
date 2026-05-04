using System.Globalization;
using AutoMapper;
using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Exceptions;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.Accounting;
using BizFlow.Application.DTOs.Revenue;
using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Application.Interfaces.Services;
using BizFlow.Domain.Entities;
using BizFlow.Domain.Enums;

namespace BizFlow.Application.Services
{
    public class RevenueService : IRevenueService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly IBusinessLocationService _locationService;
        private readonly IImageService _imageService;
        private readonly IGeneralLedgerService _generalLedgerService;
        private readonly IReferenceLabelService _labels;
        private readonly IBackgroundJobScheduler _backgroundJobScheduler;
        private readonly IDocumentNumberRegistryService _documentNumberRegistry;
        private readonly IMessageService _messageService;

        public RevenueService(
            IUnitOfWork uow,
            IMapper mapper,
            IBusinessLocationService locationService,
            IImageService imageService,
            IGeneralLedgerService generalLedgerService,
            IReferenceLabelService labels,
            IBackgroundJobScheduler backgroundJobScheduler,
            IDocumentNumberRegistryService documentNumberRegistry,
            IMessageService messageService)
        {
            _uow = uow;
            _mapper = mapper;
            _locationService = locationService;
            _imageService = imageService;
            _generalLedgerService = generalLedgerService;
            _labels = labels;
            _backgroundJobScheduler = backgroundJobScheduler;
            _documentNumberRegistry = documentNumberRegistry;
            _messageService = messageService;
        }

        private RevenueDto ToDto(Revenue revenue)
        {
            var dto = _mapper.Map<RevenueDto>(revenue);
            dto.RevenueType = _labels.ToOption(ReferenceCategory.RevenueType, revenue.RevenueType);
            dto.MoneyChannel = _labels.ToOptionOrNull(ReferenceCategory.MoneyChannelType, revenue.MoneyChannel);
            dto.Status = _labels.ToOption(ReferenceCategory.RevenueStatus, revenue.Status);
            return dto;
        }

        public async Task<RevenueDto> CreateManualAsync(Guid userId, CreateManualRevenueRequest request)
        {
            await _locationService.ValidateOwnerAsync(userId, request.BusinessLocationId);
            var businessTypeId = EnsureBusinessTypeRequired(request.BusinessTypeId);

            if (!PaymentMethods.IsValid(request.MoneyChannel))
                throw new BadRequestException(MessageKeys.BadRequest);

            var docNumberNormalized = _documentNumberRegistry.NormalizeOrNull(request.DocumentNumber);
            var ownerId = await ResolveOwnerIdAsync(request.BusinessLocationId);

            var revenue = await _uow.ExecuteResilientAsync(async ct =>
            {
                if (docNumberNormalized != null)
                {
                    await _documentNumberRegistry.EnsureLockAndAssertUniqueAsync(
                        ownerId, docNumberNormalized, cancellationToken: ct);
                }

                string? documentUrl = null;
                string? documentPublicId = null;
                if (request.ImageStream != null)
                {
                    var imageInfo = await _imageService.UploadImageAsync(
                        request.ImageStream, request.ImageFileName, ImageUploadTarget.Costs);
                    documentUrl = imageInfo.Url;
                    documentPublicId = imageInfo.PublicId;
                }

                var entity = new Revenue
                {
                    BusinessLocationId = request.BusinessLocationId,
                    BusinessTypeId = businessTypeId,
                    RevenueType = RevenueType.Manual,
                    Amount = request.Amount,
                    Status = RevenueStatus.Posted,
                    RevenueDate = request.RevenueDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
                    Description = request.Description.Trim(),
                    MoneyChannel = request.MoneyChannel.Trim().ToLower(),
                    DocumentUrl = documentUrl,
                    DocumentPublicId = documentPublicId,
                    DocumentNumber = docNumberNormalized,
                    DocumentDate = request.DocumentDate,
                    CreatedBy = userId,
                    CreatedAt = DateTime.UtcNow
                };

                await _uow.Revenues.AddAsync(entity);
                await _uow.SaveChangesAsync(ct);

                await _generalLedgerService.RecordRevenueLedgerLineFromRowAsync(entity);
                await _uow.SaveChangesAsync(ct);

                return entity;
            });

            _backgroundJobScheduler.EnqueueAiAnomalyCheck(
                revenue.BusinessLocationId, "revenue", revenue.RevenueId);

            return ToDto(revenue);
        }

        public async Task<ManualRevenueUpdateResponseDto> UpdateManualAsync(Guid userId, long revenueId, UpdateManualRevenueRequest request)
        {
            var revenuePreview = await _uow.Revenues.GetByIdAsync(revenueId)
                ?? throw new NotFoundException(MessageKeys.RevenueNotFound);

            await _locationService.ValidateOwnerAsync(userId, revenuePreview.BusinessLocationId);

            if (!revenuePreview.RevenueType.Equals(RevenueType.Manual, StringComparison.OrdinalIgnoreCase))
                throw new BadRequestException(MessageKeys.BadRequest);

            if (RevenueStatus.IsTerminal(revenuePreview.Status))
                throw new BadRequestException(MessageKeys.RevenueCannotEditCancelledOrReplaced);

            if (RevenueStatus.IsEditableInPlace(revenuePreview.Status))
                return new ManualRevenueUpdateResponseDto
                {
                    IsReplacement = false,
                    Revenue = await UpdateManualDraftInPlaceAsync(userId, revenuePreview, request)
                };

            if (!string.Equals(revenuePreview.Status, RevenueStatus.Posted, StringComparison.OrdinalIgnoreCase))
                throw new BadRequestException(MessageKeys.RevenueInvalidStatus);

            if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
                throw new BadRequestException(MessageKeys.RevenueEditPostedRequiresIdempotencyKey);

            var idemKey = request.IdempotencyKey.Trim();
            if (idemKey.Length > 100)
                throw new BadRequestException(MessageKeys.BadRequest);

            var existingReplacement = await _uow.Revenues.GetLatestReplacementByRefRevenueIdAsync(revenueId, idemKey);
            if (existingReplacement != null)
            {
                return new ManualRevenueUpdateResponseDto
                {
                    IsReplacement = true,
                    Revenue = ToDto(existingReplacement),
                    Replacement = new PostedRecordReplacementResultDto
                    {
                        OldRecordId = revenueId,
                        OldRecordStatus = _labels.ToOption(ReferenceCategory.RevenueStatus, RevenueStatus.Replaced),
                        NewRecordId = existingReplacement.RevenueId,
                        NewRecordStatus = _labels.ToOption(ReferenceCategory.RevenueStatus, existingReplacement.Status)
                    }
                };
            }

            if (!PaymentMethods.IsValid(request.MoneyChannel))
                throw new BadRequestException(MessageKeys.BadRequest);

            var docNumberNormalized = _documentNumberRegistry.NormalizeOrNull(request.DocumentNumber);
            var ownerId = await ResolveOwnerIdAsync(revenuePreview.BusinessLocationId);

            var newRevenue = await _uow.ExecuteResilientAsync(async ct =>
            {
                await _uow.Revenues.LockRevenueRowForUpdateAsync(revenueId, ct);

                var old = await _uow.Revenues.GetByIdAsync(revenueId)
                    ?? throw new NotFoundException(MessageKeys.RevenueNotFound);

                if (RevenueStatus.IsTerminal(old.Status))
                    throw new BadRequestException(MessageKeys.RevenueCannotEditCancelledOrReplaced);

                if (!string.Equals(old.Status, RevenueStatus.Posted, StringComparison.OrdinalIgnoreCase))
                    throw new BadRequestException(MessageKeys.RevenueInvalidStatus);

                var dup = await _uow.Revenues.GetLatestReplacementByRefRevenueIdAsync(revenueId, idemKey, ct);
                if (dup != null)
                    return dup;

                if (docNumberNormalized != null)
                {
                    await _documentNumberRegistry.EnsureLockAndAssertUniqueAsync(
                        ownerId,
                        docNumberNormalized,
                        excludeRevenueId: old.RevenueId,
                        cancellationToken: ct);
                }

                var replacementRevenueDate = request.RevenueDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
                var replaceReversalDesc = BuildReplacePostedManualRevenueReversalDescription(old);
                var revenueReversal = new Revenue
                {
                    BusinessLocationId = old.BusinessLocationId,
                    BusinessTypeId = old.BusinessTypeId,
                    OrderId = old.OrderId,
                    RevenueType = old.RevenueType,
                    Amount = -old.Amount,
                    Status = RevenueStatus.Posted,
                    RevenueDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    Description = replaceReversalDesc,
                    MoneyChannel = old.MoneyChannel,
                    IsReversal = true,
                    ReversedRevenueId = old.RevenueId,
                    CreatedBy = userId,
                    CreatedAt = DateTime.UtcNow
                };
                await _uow.Revenues.AddAsync(revenueReversal);
                await _uow.SaveChangesAsync(ct);
                await _generalLedgerService.RecordRevenueLedgerLineFromRowAsync(revenueReversal);
                await _uow.SaveChangesAsync(ct);

                old.Status = RevenueStatus.Replaced;
                old.CancelledAt = DateTime.UtcNow;
                old.CancelledBy = userId;
                _uow.Revenues.Update(old);

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

                var businessTypeId = EnsureBusinessTypeRequired(request.BusinessTypeId);

                var created = new Revenue
                {
                    BusinessLocationId = old.BusinessLocationId,
                    BusinessTypeId = businessTypeId,
                    RevenueType = RevenueType.Manual,
                    Amount = request.Amount,
                    Status = RevenueStatus.Posted,
                    RevenueDate = replacementRevenueDate,
                    Description = request.Description.Trim(),
                    MoneyChannel = request.MoneyChannel.Trim().ToLower(),
                    DocumentUrl = docUrl,
                    DocumentPublicId = docPublicId,
                    DocumentNumber = docNumberNormalized,
                    DocumentDate = request.DocumentDate,
                    RefRevenueId = old.RevenueId,
                    IdempotencyKey = idemKey,
                    CreatedBy = userId,
                    CreatedAt = DateTime.UtcNow
                };

                await _uow.Revenues.AddAsync(created);
                await _uow.SaveChangesAsync(ct);

                await _generalLedgerService.RecordRevenueLedgerLineFromRowAsync(created);
                await _uow.SaveChangesAsync(ct);

                return created;
            });

            _backgroundJobScheduler.EnqueueAiAnomalyCheck(
                newRevenue.BusinessLocationId, "revenue", newRevenue.RevenueId);

            return new ManualRevenueUpdateResponseDto
            {
                IsReplacement = true,
                Revenue = ToDto(newRevenue),
                Replacement = new PostedRecordReplacementResultDto
                {
                    OldRecordId = revenueId,
                    OldRecordStatus = _labels.ToOption(ReferenceCategory.RevenueStatus, RevenueStatus.Replaced),
                    NewRecordId = newRevenue.RevenueId,
                    NewRecordStatus = _labels.ToOption(ReferenceCategory.RevenueStatus, newRevenue.Status)
                }
            };
        }

        private async Task<RevenueDto> UpdateManualDraftInPlaceAsync(Guid userId, Revenue revenue, UpdateManualRevenueRequest request)
        {
            if (!PaymentMethods.IsValid(request.MoneyChannel))
                throw new BadRequestException(MessageKeys.BadRequest);

            var docNumberNormalized = _documentNumberRegistry.NormalizeOrNull(request.DocumentNumber);
            var ownerId = await ResolveOwnerIdAsync(revenue.BusinessLocationId);

            await _uow.ExecuteResilientAsync(async ct =>
            {
                if (docNumberNormalized != null
                    && !string.Equals(docNumberNormalized, revenue.DocumentNumberNormalized, StringComparison.Ordinal))
                {
                    await _documentNumberRegistry.EnsureLockAndAssertUniqueAsync(
                        ownerId,
                        docNumberNormalized,
                        excludeRevenueId: revenue.RevenueId,
                        cancellationToken: ct);
                }

                revenue.BusinessTypeId = EnsureBusinessTypeRequired(request.BusinessTypeId);
                revenue.Amount = request.Amount;
                revenue.RevenueDate = request.RevenueDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
                revenue.Description = request.Description.Trim();
                revenue.MoneyChannel = request.MoneyChannel.Trim().ToLower();

                if (request.RemoveDocument && !string.IsNullOrEmpty(revenue.DocumentPublicId))
                {
                    revenue.DocumentUrl = null;
                    revenue.DocumentPublicId = null;
                }

                if (request.ImageStream != null)
                {
                    var imageInfo = await _imageService.UploadImageAsync(
                        request.ImageStream, request.ImageFileName, ImageUploadTarget.Costs);
                    revenue.DocumentUrl = imageInfo.Url;
                    revenue.DocumentPublicId = imageInfo.PublicId;
                }

                revenue.DocumentNumber = docNumberNormalized;
                revenue.DocumentDate = request.DocumentDate;

                _uow.Revenues.Update(revenue);
                await _uow.SaveChangesAsync(ct);
            });

            _backgroundJobScheduler.EnqueueAiAnomalyCheck(
                revenue.BusinessLocationId, "revenue", revenue.RevenueId);

            return ToDto(revenue);
        }

        public async Task<PaginatedResponse<RevenueDto>> ListAsync(Guid userId, RevenueQueryParams query)
        {
            await _locationService.ValidateOwnerAsync(userId, query.BusinessLocationId);

            if (!string.IsNullOrWhiteSpace(query.RevenueType)
                && !RevenueType.IsValid(query.RevenueType.Trim()))
                throw new BadRequestException(MessageKeys.BadRequest);

            if (!string.IsNullOrWhiteSpace(query.Status)
                && !RevenueStatus.IsValid(query.Status.Trim()))
                throw new BadRequestException(MessageKeys.BadRequest);

            if (!string.IsNullOrWhiteSpace(query.MoneyChannel)
                && !MoneyChannelType.IsValid(query.MoneyChannel.Trim()))
                throw new BadRequestException(MessageKeys.BadRequest);

            if (!string.IsNullOrWhiteSpace(query.RevenueType))
                query.RevenueType = query.RevenueType.Trim().ToLowerInvariant();

            if (!string.IsNullOrWhiteSpace(query.Status))
                query.Status = query.Status.Trim().ToLowerInvariant();

            if (!string.IsNullOrWhiteSpace(query.MoneyChannel))
                query.MoneyChannel = query.MoneyChannel.Trim().ToLowerInvariant();

            var (items, total) = await _uow.Revenues.SearchAsync(query);
            var dtos = items.Select(ToDto).ToList();

            var pageNumber = query.PageNumber ?? 1;
            var pageSize = query.PageSize ?? 20;
            return new PaginatedResponse<RevenueDto>(dtos, total, pageNumber, pageSize);
        }

        public async Task DeleteManualAsync(Guid userId, long revenueId)
        {
            var revenue = await _uow.Revenues.GetByIdAsync(revenueId)
                ?? throw new NotFoundException(MessageKeys.RevenueNotFound);

            await _locationService.ValidateOwnerAsync(userId, revenue.BusinessLocationId);

            if (!revenue.RevenueType.Equals(RevenueType.Manual, StringComparison.OrdinalIgnoreCase))
                throw new BadRequestException(MessageKeys.BadRequest);

            if (RevenueStatus.IsTerminal(revenue.Status))
                throw new BadRequestException(MessageKeys.RevenueCannotEditCancelledOrReplaced);

            if (string.Equals(revenue.Status, RevenueStatus.Draft, StringComparison.OrdinalIgnoreCase))
            {
                await _uow.ExecuteResilientAsync(async ct =>
                {
                    revenue.Status = RevenueStatus.Cancelled;
                    revenue.CancelledAt = DateTime.UtcNow;
                    revenue.CancelledBy = userId;
                    _uow.Revenues.Update(revenue);
                    await _uow.SaveChangesAsync(ct);
                });
                return;
            }

            await _uow.ExecuteResilientAsync(async ct =>
            {
                await _uow.Revenues.LockRevenueRowForUpdateAsync(revenueId, ct);
                var current = await _uow.Revenues.GetByIdAsync(revenueId)
                    ?? throw new NotFoundException(MessageKeys.RevenueNotFound);

                if (await _uow.Revenues.HasActiveReversalForRevenueAsync(current.RevenueId, ct))
                    return;

                var deleteReversalDesc = BuildReplacePostedManualRevenueReversalDescription(current);
                var reversal = new Revenue
                {
                    BusinessLocationId = current.BusinessLocationId,
                    BusinessTypeId = current.BusinessTypeId,
                    OrderId = current.OrderId,
                    RevenueType = current.RevenueType,
                    Amount = -current.Amount,
                    Status = RevenueStatus.Posted,
                    RevenueDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    Description = deleteReversalDesc,
                    MoneyChannel = current.MoneyChannel,
                    IsReversal = true,
                    ReversedRevenueId = current.RevenueId,
                    CreatedBy = userId,
                    CreatedAt = DateTime.UtcNow
                };

                await _uow.Revenues.AddAsync(reversal);
                await _uow.SaveChangesAsync(ct);
                await _generalLedgerService.RecordRevenueLedgerLineFromRowAsync(reversal);

                current.Status = RevenueStatus.Replaced;
                current.CancelledAt = DateTime.UtcNow;
                current.CancelledBy = userId;
                _uow.Revenues.Update(current);
                await _uow.SaveChangesAsync(ct);
            });
        }
        public async Task RecordPostedSaleRevenuesToLedgerAsync(
            IEnumerable<Revenue> revenues,
            CancellationToken cancellationToken = default)
        {
            foreach (var revenue in revenues)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await _generalLedgerService.RecordRevenueLedgerLineFromRowAsync(revenue);
            }
        }

        public async Task ReversePostedSaleRevenuesLedgerForOrderCancelAsync(
            IEnumerable<Revenue> revenues,
            string reversalMessageKey,
            string supersededStatus,
            Guid? reversalCreatedBy = null,
            CancellationToken cancellationToken = default)
        {
            if (!string.Equals(supersededStatus, RevenueStatus.Cancelled, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(supersededStatus, RevenueStatus.Replaced, StringComparison.OrdinalIgnoreCase))
                throw new BadRequestException(MessageKeys.BadRequest);

            foreach (var revenue in revenues)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!string.Equals(revenue.Status, RevenueStatus.Posted, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (await _uow.Revenues.HasActiveReversalForRevenueAsync(revenue.RevenueId, cancellationToken))
                    continue;

                var desc = BuildReplacePostedManualRevenueReversalDescription(revenue);
                var createdBy = reversalCreatedBy ?? revenue.CreatedBy;
                var reversal = new Revenue
                {
                    BusinessLocationId = revenue.BusinessLocationId,
                    BusinessTypeId = revenue.BusinessTypeId,
                    OrderId = revenue.OrderId,
                    RevenueType = revenue.RevenueType,
                    Amount = -revenue.Amount,
                    Status = RevenueStatus.Posted,
                    RevenueDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    Description = desc,
                    MoneyChannel = revenue.MoneyChannel,
                    IsReversal = true,
                    ReversedRevenueId = revenue.RevenueId,
                    CreatedBy = createdBy,
                    CreatedAt = DateTime.UtcNow
                };

                await _uow.Revenues.AddAsync(reversal);
                await _uow.SaveChangesAsync(cancellationToken);
                await _generalLedgerService.RecordRevenueLedgerLineFromRowAsync(reversal);

                revenue.Status = supersededStatus.Trim().ToLowerInvariant();
                revenue.CancelledAt = DateTime.UtcNow;
                revenue.CancelledBy = createdBy;
                _uow.Revenues.Update(revenue);
                await _uow.SaveChangesAsync(cancellationToken);
            }
        }

        private string BuildLedgerReversalDescription(string reversalReasonMessageKey)
        {
            var reasonMessage = _messageService.GetMessage(reversalReasonMessageKey);
            return _messageService.GetMessage(MessageKeys.ReversalDescriptionFormat, reasonMessage);
        }

        /// <summary>
        /// Reversal row description: prefix + source manual revenue date + document number (replace or delete posted).
        /// </summary>
        private string BuildReplacePostedManualRevenueReversalDescription(Revenue supersededRevenue)
        {
            var prefix = _messageService.GetMessage(MessageKeys.LedgerRevenueReplaceReversalPrefix);
            var datePart = supersededRevenue.RevenueDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var doc = !string.IsNullOrWhiteSpace(supersededRevenue.DocumentNumber)
                ? supersededRevenue.DocumentNumber.Trim()
                : supersededRevenue.DocumentNumberNormalized?.Trim();
            if (string.IsNullOrWhiteSpace(doc))
                return $"{prefix} {datePart}";
            return $"{prefix} {datePart} {doc}";
        }

        private static Guid EnsureBusinessTypeRequired(Guid? businessTypeId)
        {
            if (!businessTypeId.HasValue || businessTypeId.Value == Guid.Empty)
                throw new BadRequestException(MessageKeys.BadRequest);
            return businessTypeId.Value;
        }

        private async Task<Guid> ResolveOwnerIdAsync(int locationId)
        {
            var ownerId = await _uow.BusinessLocations.GetOwnerIdByLocationAsync(locationId)
                ?? throw new NotFoundException(MessageKeys.NotFound);
            return ownerId;
        }
    }
}
