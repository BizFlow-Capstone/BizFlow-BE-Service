using AutoMapper;
using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Exceptions;
using BizFlow.Application.Common.Models;
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

        public CostService(
            IUnitOfWork uow,
            IMapper mapper,
            IBusinessLocationService locationService,
            IImageService imageService,
            IGeneralLedgerService generalLedgerService)
        {
            _uow = uow;
            _mapper = mapper;
            _locationService = locationService;
            _imageService = imageService;
            _generalLedgerService = generalLedgerService;
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

            string? documentUrl = null;
            string? documentPublicId = null;

            // Handle document image upload
            if (request.ImageStream != null)
            {
                var imageInfo = await _imageService.UploadImageAsync(
                    request.ImageStream, request.ImageFileName, ImageUploadTarget.Costs);
                documentUrl = imageInfo.Url;
                documentPublicId = imageInfo.PublicId;
            }

            var entity = new Cost
            {
                BusinessLocationId = request.BusinessLocationId,
                CostType = normalizedType,
                Description = request.Description.Trim(),
                Amount = request.Amount,
                CostDate = request.CostDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
                PaymentMethod = normalizedPaymentMethod,
                DocumentNumber = NormalizeDocumentNumber(request.DocumentNumber),
                DocumentDate = request.DocumentDate,
                DocumentUrl = documentUrl,
                DocumentPublicId = documentPublicId,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow
            };

            await _uow.Costs.AddAsync(entity);
            await _uow.SaveChangesAsync();

            await _generalLedgerService.RecordManualCostAsync(entity);
            await _uow.SaveChangesAsync();

            return _mapper.Map<CostDto>(entity);
        }

        public async Task<CostDto> UpdateManualAsync(Guid userId, long costId, UpdateManualCostRequest request)
        {
            var cost = await _uow.Costs.GetByIdAsync(costId)
                ?? throw new NotFoundException(MessageKeys.NotFound);

            await _locationService.ValidateOwnerAsync(userId, cost.BusinessLocationId);

            if (cost.CostType.Equals(CostType.Import, StringComparison.OrdinalIgnoreCase))
                throw new BadRequestException(MessageKeys.BadRequest);

            string? normalizedPaymentMethod = null;
            if (!string.IsNullOrWhiteSpace(request.PaymentMethod))
            {
                if (!PaymentMethods.IsValid(request.PaymentMethod))
                    throw new BadRequestException(MessageKeys.BadRequest);

                normalizedPaymentMethod = request.PaymentMethod.Trim().ToLower();
            }

            cost.Description = request.Description.Trim();
            cost.Amount = request.Amount;
            cost.CostDate = request.CostDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
            cost.PaymentMethod = normalizedPaymentMethod;
            cost.DocumentNumber = NormalizeDocumentNumber(request.DocumentNumber);
            cost.DocumentDate = request.DocumentDate;
            cost.UpdatedAt = DateTime.UtcNow;

            // Handle document image removal
            if (request.RemoveDocument && !string.IsNullOrEmpty(cost.DocumentPublicId))
            {
                // Orphan image on Cloudinary will be cleaned up by ImageCleanupJob
                cost.DocumentUrl = null;
                cost.DocumentPublicId = null;
            }

            // Handle new document image upload
            if (request.ImageStream != null)
            {
                var imageInfo = await _imageService.UploadImageAsync(
                    request.ImageStream, request.ImageFileName, ImageUploadTarget.Costs);
                cost.DocumentUrl = imageInfo.Url;
                cost.DocumentPublicId = imageInfo.PublicId;
            }

            _uow.Costs.Update(cost);
            await _uow.SaveChangesAsync();

            await _generalLedgerService.ReverseCostEntriesAsync(cost, MessageKeys.ManualCostUpdatedReversalReason);
            await _generalLedgerService.RecordManualCostAsync(cost);
            await _uow.SaveChangesAsync();

            return _mapper.Map<CostDto>(cost);
        }

        public async Task<PaginatedResponse<CostDto>> ListAsync(Guid userId, CostQueryParams query)
        {
            await _locationService.ValidateOwnerAsync(userId, query.BusinessLocationId);

            if (!string.IsNullOrWhiteSpace(query.CostType)
                && !CostType.IsValid(query.CostType.Trim()))
                throw new BadRequestException(MessageKeys.BadRequest);

            if (!string.IsNullOrWhiteSpace(query.PaymentMethod)
                && !PaymentMethods.IsValid(query.PaymentMethod.Trim()))
                throw new BadRequestException(MessageKeys.BadRequest);

            if (!string.IsNullOrWhiteSpace(query.CostType))
                query.CostType = query.CostType.Trim().ToLowerInvariant();

            if (!string.IsNullOrWhiteSpace(query.PaymentMethod))
                query.PaymentMethod = query.PaymentMethod.Trim().ToLowerInvariant();

            var (items, total) = await _uow.Costs.SearchAsync(query);
            var dtos = _mapper.Map<List<CostDto>>(items);

            var pageNumber = query.PageNumber ?? 1;
            var pageSize = query.PageSize ?? 20;
            return new PaginatedResponse<CostDto>(dtos, total, pageNumber, pageSize);
        }

        public async Task DeleteManualAsync(Guid userId, long costId)
        {
            var cost = await _uow.Costs.GetByIdAsync(costId)
                ?? throw new NotFoundException(MessageKeys.NotFound);

            await _locationService.ValidateOwnerAsync(userId, cost.BusinessLocationId);

            if (cost.CostType.Equals(CostType.Import, StringComparison.OrdinalIgnoreCase))
                throw new BadRequestException(MessageKeys.BadRequest);

            cost.DeletedAt = DateTime.UtcNow;
            cost.UpdatedAt = DateTime.UtcNow;
            _uow.Costs.Update(cost);

            await _uow.SaveChangesAsync();

            await _generalLedgerService.ReverseCostEntriesAsync(cost, MessageKeys.ManualCostDeletedReversalReason);
            await _uow.SaveChangesAsync();
        }

        public async Task<Cost> CreateImportCostAsync(Guid userId, Import import, string? documentNumber = null, DateOnly? documentDate = null)
        {
            return await _uow.ExecuteResilientAsync(async _ =>
            {
                var existing = await _uow.Costs.GetByImportIdAsync(import.ImportId);
                if (existing != null)
                {
                    // If previously soft-deleted and import is re-confirmed in future flows,
                    // revive the same row to keep one source-of-truth cost per import.
                    if (existing.DeletedAt != null)
                    {
                        existing.DeletedAt = null;
                        existing.UpdatedAt = DateTime.UtcNow;
                        existing.DocumentNumber = NormalizeDocumentNumber(documentNumber);
                        existing.DocumentDate = documentDate;
                        _uow.Costs.Update(existing);

                        await _generalLedgerService.RecordImportCostAsync(existing);
                    }

                    return existing;
                }

                var cost = new Cost
                {
                    BusinessLocationId = import.BusinessLocationId,
                    CostType = CostType.Import,
                    ImportId = import.ImportId,
                    Description = $"Import {import.ImportCode ?? import.ImportId.ToString()}",
                    Amount = import.TotalAmount,
                    CostDate = DateOnly.FromDateTime(import.ReceivedAt ?? import.ConfirmedAt ?? import.CreatedAt),
                    PaymentMethod = null,
                    DocumentNumber = NormalizeDocumentNumber(documentNumber),
                    DocumentDate = documentDate,
                    CreatedBy = userId,
                    CreatedAt = DateTime.UtcNow
                };

                await _uow.Costs.AddAsync(cost);
                await _uow.SaveChangesAsync(); // Need CostId before creating GL reference entry.

                await _generalLedgerService.RecordImportCostAsync(cost);

                return cost;
            });
        }

        public async Task ReverseImportCostAsync(Guid userId, Import import, string? reason = null)
        {
            var cost = await _uow.Costs.GetByImportIdAsync(import.ImportId);
            if (cost == null)
                return;

            if (cost.DeletedAt == null)
            {
                cost.DeletedAt = DateTime.UtcNow;
                cost.UpdatedAt = DateTime.UtcNow;
                _uow.Costs.Update(cost);
            }

            await _generalLedgerService.ReverseCostEntriesAsync(cost, reason ?? MessageKeys.ImportCancelledReversalReason);
            await _uow.SaveChangesAsync();
        }

        private static string? NormalizeDocumentNumber(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            return value.Trim();
        }
    }
}
