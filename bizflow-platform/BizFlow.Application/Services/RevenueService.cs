using AutoMapper;
using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Exceptions;
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
        private readonly IGeneralLedgerService _generalLedgerService;

        public RevenueService(
            IUnitOfWork uow,
            IMapper mapper,
            IBusinessLocationService locationService,
            IGeneralLedgerService generalLedgerService)
        {
            _uow = uow;
            _mapper = mapper;
            _locationService = locationService;
            _generalLedgerService = generalLedgerService;
        }

        public async Task<RevenueDto> CreateManualAsync(Guid userId, CreateManualRevenueRequest request)
        {
            await _locationService.ValidateOwnerAsync(userId, request.BusinessLocationId);

            if (!PaymentMethods.IsValid(request.MoneyChannel))
                throw new BadRequestException(MessageKeys.BadRequest);

            var revenue = await _uow.ExecuteResilientAsync(async _ =>
            {
                var entity = new Revenue
                {
                    BusinessLocationId = request.BusinessLocationId,
                    RevenueType = RevenueType.Manual,
                    Amount = request.Amount,
                    RevenueDate = request.RevenueDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
                    Description = request.Description.Trim(),
                    MoneyChannel = request.MoneyChannel.Trim().ToLower(),
                    CreatedBy = userId,
                    CreatedAt = DateTime.UtcNow
                };

                await _uow.Revenues.AddAsync(entity);
                await _uow.SaveChangesAsync(); // Need RevenueId before GL reference entry.

                await _generalLedgerService.RecordManualRevenueAsync(entity);

                return entity;
            });

            return _mapper.Map<RevenueDto>(revenue);
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
            var dtos = _mapper.Map<List<RevenueDto>>(items);

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
    }
}
