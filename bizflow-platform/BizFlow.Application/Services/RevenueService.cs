using AutoMapper;
using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Exceptions;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.Common.Models;
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

        public RevenueService(
            IUnitOfWork uow,
            IMapper mapper,
            IBusinessLocationService locationService,
            IImageService imageService,
            IGeneralLedgerService generalLedgerService,
            IReferenceLabelService labels,
            IBackgroundJobScheduler backgroundJobScheduler)
        {
            _uow = uow;
            _mapper = mapper;
            _locationService = locationService;
            _imageService = imageService;
            _generalLedgerService = generalLedgerService;
            _labels = labels;
            _backgroundJobScheduler = backgroundJobScheduler;
        }

        private RevenueDto ToDto(Revenue revenue)
        {
            var dto = _mapper.Map<RevenueDto>(revenue);
            dto.RevenueType = _labels.ToOption(ReferenceCategory.RevenueType, revenue.RevenueType);
            dto.MoneyChannel = _labels.ToOptionOrNull(ReferenceCategory.MoneyChannelType, revenue.MoneyChannel);
            return dto;
        }

        public async Task<RevenueDto> CreateManualAsync(Guid userId, CreateManualRevenueRequest request)
        {
            await _locationService.ValidateOwnerAsync(userId, request.BusinessLocationId);
            var businessTypeId = EnsureBusinessTypeRequired(request.BusinessTypeId);

            if (!PaymentMethods.IsValid(request.MoneyChannel))
                throw new BadRequestException(MessageKeys.BadRequest);

            var revenue = await _uow.ExecuteResilientAsync(async _ =>
            {
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
                    RevenueDate = request.RevenueDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
                    Description = request.Description.Trim(),
                    MoneyChannel = request.MoneyChannel.Trim().ToLower(),
                    DocumentUrl = documentUrl,
                    DocumentPublicId = documentPublicId,
                    DocumentNumber = NormalizeDocumentNumber(request.DocumentNumber),
                    DocumentDate = request.DocumentDate,
                    CreatedBy = userId,
                    CreatedAt = DateTime.UtcNow
                };

                await _uow.Revenues.AddAsync(entity);
                await _uow.SaveChangesAsync(); // Need RevenueId before GL reference entry.

                await _generalLedgerService.RecordManualRevenueAsync(entity);

                return entity;
            });

            _backgroundJobScheduler.EnqueueAiAnomalyCheck(
                revenue.BusinessLocationId, "revenue", revenue.RevenueId);

            return ToDto(revenue);
        }

        public async Task<RevenueDto> UpdateManualAsync(Guid userId, long revenueId, UpdateManualRevenueRequest request)
        {
            var revenue = await _uow.Revenues.GetByIdAsync(revenueId)
                ?? throw new NotFoundException(MessageKeys.NotFound);

            await _locationService.ValidateOwnerAsync(userId, revenue.BusinessLocationId);

            if (!revenue.RevenueType.Equals(RevenueType.Manual, StringComparison.OrdinalIgnoreCase))
                throw new BadRequestException(MessageKeys.BadRequest);

            if (!PaymentMethods.IsValid(request.MoneyChannel))
                throw new BadRequestException(MessageKeys.BadRequest);

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

            revenue.DocumentNumber = NormalizeDocumentNumber(request.DocumentNumber);
            revenue.DocumentDate = request.DocumentDate;

            _uow.Revenues.Update(revenue);
            await _uow.SaveChangesAsync();

            await _generalLedgerService.ReverseRevenueEntriesAsync(revenue, MessageKeys.ManualRevenueUpdatedReversalReason);
            await _generalLedgerService.RecordManualRevenueAsync(revenue);
            await _uow.SaveChangesAsync();

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

            if (!string.IsNullOrWhiteSpace(query.MoneyChannel)
                && !MoneyChannelType.IsValid(query.MoneyChannel.Trim()))
                throw new BadRequestException(MessageKeys.BadRequest);

            if (!string.IsNullOrWhiteSpace(query.RevenueType))
                query.RevenueType = query.RevenueType.Trim().ToLowerInvariant();

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
                ?? throw new NotFoundException(MessageKeys.NotFound);

            await _locationService.ValidateOwnerAsync(userId, revenue.BusinessLocationId);

            if (!revenue.RevenueType.Equals(RevenueType.Manual, StringComparison.OrdinalIgnoreCase))
                throw new BadRequestException(MessageKeys.BadRequest);

            await _uow.ExecuteResilientAsync(async _ =>
            {
                revenue.DeletedAt = DateTime.UtcNow;
                _uow.Revenues.Update(revenue);

                await _generalLedgerService.ReverseRevenueEntriesAsync(revenue, MessageKeys.ManualRevenueDeletedReversalReason);
            });
        }

        private static string? NormalizeDocumentNumber(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            return value.Trim();
        }

        private static Guid EnsureBusinessTypeRequired(Guid? businessTypeId)
        {
            if (!businessTypeId.HasValue || businessTypeId.Value == Guid.Empty)
                throw new BadRequestException(MessageKeys.BadRequest);
            return businessTypeId.Value;
        }
    }
}
