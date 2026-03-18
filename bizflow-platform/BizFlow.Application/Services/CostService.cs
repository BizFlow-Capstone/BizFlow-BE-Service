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

        public CostService(
            IUnitOfWork uow,
            IMapper mapper,
            IBusinessLocationService locationService,
            IImageService imageService)
        {
            _uow = uow;
            _mapper = mapper;
            _locationService = locationService;
            _imageService = imageService;
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
                DocumentUrl = documentUrl,
                DocumentPublicId = documentPublicId,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow
            };

            await _uow.Costs.AddAsync(entity);
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

            return _mapper.Map<CostDto>(cost);
        }

        public async Task<PaginatedResponse<CostDto>> ListAsync(Guid userId, CostQueryParams query)
        {
            await _locationService.ValidateOwnerAsync(userId, query.BusinessLocationId);

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
        }
    }
}
