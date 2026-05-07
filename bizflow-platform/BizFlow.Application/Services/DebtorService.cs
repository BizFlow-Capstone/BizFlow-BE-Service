using AutoMapper;
using BizFlow.Application.Common.Helpers;
using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Exceptions;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.Debtor;
using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Application.Interfaces.Services;
using BizFlow.Application.Mappers;
using BizFlow.Domain.Entities;
using BizFlow.Domain.Enums;

namespace BizFlow.Application.Services
{
    public class DebtorService : IDebtorService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly IBusinessLocationService _locationService;
        private readonly IGeneralLedgerService _generalLedgerService;
        private readonly IReferenceLabelService _labels;

        public DebtorService(
            IUnitOfWork uow,
            IMapper mapper,
            IBusinessLocationService locationService,
            IGeneralLedgerService generalLedgerService,
            IReferenceLabelService labels)
        {
            _uow = uow;
            _mapper = mapper;
            _locationService = locationService;
            _generalLedgerService = generalLedgerService;
            _labels = labels;
        }

        private DebtorPaymentDto ToDto(DebtorPaymentTransaction transaction)
        {
            var dto = _mapper.Map<DebtorPaymentDto>(transaction);
            dto.PaymentMethod = _labels.ToOption(ReferenceCategory.PaymentMethod, transaction.PaymentMethod);
            dto.BalanceDelta = transaction.BalanceAfter - transaction.BalanceBefore;
            var debtDirectionCode = DebtBalanceSemanticHelper.ResolveDebtDirection(
                transaction.BalanceBefore,
                transaction.BalanceAfter);
            dto.DebtDirection = _labels.ToOption(ReferenceCategory.DebtDirection, debtDirectionCode);
            dto.DebtAction = ResolveDebtAction(transaction, debtDirectionCode);
            dto.OutstandingDebtAfter = DebtBalanceSemanticHelper.CalculateOutstandingDebt(transaction.BalanceAfter);
            return dto;
        }

        public async Task<PaginatedResponse<DebtorSummaryDto>> ListAsync(Guid userId, DebtorQueryParams query)
        {
            var ownedLocations = await _locationService.GetOwnedLocationsAsync(userId);
            var ownedLocationIds = ownedLocations.Select(l => l.Id).ToList();
            IEnumerable<int> allowedLocationIds;

            if (query.BusinessLocationIds != null && query.BusinessLocationIds.Any())
            {
                var unauthorizedIds = query.BusinessLocationIds.Except(ownedLocationIds).ToList();
                if (unauthorizedIds.Any())
                {
                    throw new ForbiddenException(MessageKeys.Forbidden);
                }
                
                allowedLocationIds = query.BusinessLocationIds;
            }
            else
            {
                allowedLocationIds = ownedLocationIds;

                if (!allowedLocationIds.Any())
                {
                    return new PaginatedResponse<DebtorSummaryDto>(new List<DebtorSummaryDto>(), 0, query.PageNumber, query.PageSize);
                }
            }

            var (items, total) = await _uow.Debtors.SearchAsync(query, allowedLocationIds);
            var dtos = _mapper.Map<List<DebtorSummaryDto>>(items);
            return new PaginatedResponse<DebtorSummaryDto>(dtos, total, query.PageNumber, query.PageSize);
        }

        public async Task<DebtorDetailDto> GetDetailAsync(Guid userId, long debtorId)
        {
            var debtor = await GetDebtorAndVerifyOwnerAsync(userId, debtorId);

            return _mapper.Map<DebtorDetailDto>(debtor);
        }

        public async Task<DebtorDetailDto> CreateAsync(Guid userId, CreateDebtorRequest request)
        {
            await VerifyLocationOwnerAsync(userId, request.BusinessLocationId);

            if (!string.IsNullOrWhiteSpace(request.Phone))
            {
                var dup = await _uow.Debtors.PhoneExistsInLocationAsync(request.BusinessLocationId, request.Phone);
                if (dup) throw new ConflictException(MessageKeys.DebtorPhoneDuplicate);
            }

            var debtor = DebtorProfile.ToEntity(request, userId);
            await _uow.Debtors.AddAsync(debtor);
            await _uow.SaveChangesAsync();
            return _mapper.Map<DebtorDetailDto>(debtor);
        }

        public async Task<DebtorDetailDto> UpdateAsync(Guid userId, long debtorId, UpdateDebtorRequest request)
        {
            var debtor = await GetDebtorAndVerifyOwnerAsync(userId, debtorId);

            if (!string.IsNullOrWhiteSpace(request.Phone))
            {
                var dup = await _uow.Debtors.PhoneExistsInLocationAsync(
                    debtor.BusinessLocationId, request.Phone, debtorId);
                if (dup) throw new ConflictException(MessageKeys.DebtorPhoneDuplicate);
            }

            DebtorProfile.ApplyUpdate(request, debtor);
            _uow.Debtors.Update(debtor);
            await _uow.SaveChangesAsync();
            return _mapper.Map<DebtorDetailDto>(debtor);
        }

        public async Task<DebtorDetailDto> UpdateStatusAsync(Guid userId, long debtorId, bool isActive)
        {
            var debtor = await GetDebtorAndVerifyOwnerAsync(userId, debtorId);

            debtor.IsActive = isActive;
            debtor.UpdatedAt = DateTime.UtcNow;

            _uow.Debtors.Update(debtor);
            await _uow.SaveChangesAsync();

            return _mapper.Map<DebtorDetailDto>(debtor);
        }

        public async Task DeleteAsync(Guid userId, long debtorId, bool forceDelete = false)
        {
            var debtor = await GetDebtorAndVerifyOwnerAsync(userId, debtorId);

            if (debtor.CurrentBalance != 0 && !forceDelete)
                throw new BadRequestException(MessageKeys.DebtorHasOutstandingBalance);

            var hasActivity = await _uow.Debtors.HasAnyActivityAsync(debtorId);

            if (hasActivity)
            {
                // Soft delete
                debtor.DeletedAt = DateTime.UtcNow;
                debtor.IsActive = false;
                _uow.Debtors.Update(debtor);
            }
            else
            {
                // Hard delete
                _uow.Debtors.Remove(debtor);
            }

            await _uow.SaveChangesAsync();
        }

        public async Task<IEnumerable<DebtorMinimalDto>> GetActiveDebtorsByLocationAsync(Guid userId, int locationId)
        {
            await _locationService.ValidateLocationAccessAsync(userId, locationId);
            var debtors = await _uow.Debtors.GetActiveByLocationAsync(locationId);
            return _mapper.Map<IEnumerable<DebtorMinimalDto>>(debtors);
        }

        public async Task<DebtorPaymentDto> RecordPaymentAsync(Guid userId, long debtorId, RecordDebtPaymentRequest request)
        {
            if (request.Amount <= 0)
                throw new BadRequestException(MessageKeys.DebtorPaymentAmountZero);

            if (!PaymentMethods.IsValid(request.PaymentMethod))
                throw new BadRequestException(MessageKeys.DebtorPaymentMethodInvalid);

            var debtor = await GetDebtorAndVerifyOwnerAsync(userId, debtorId);

            if (debtor.IsActive != true)
                throw new BadRequestException(MessageKeys.DebtorNotActive);

            if (!DebtPaymentActions.IsValidUserAction(request.Action))
                throw new BadRequestException(MessageKeys.DebtorPaymentActionInvalid);

            var transaction = DebtorProfile.ToEntity(request, debtorId, userId, debtor.CurrentBalance);
            await ApplyDebtTransactionAsync(debtor, transaction);
            await _uow.SaveChangesAsync();

            // Log debt payment transaction to GL.
            await _generalLedgerService.RecordDebtPaymentAsync(
                transaction,
                debtor.BusinessLocationId,
                request.Action);
            await _uow.SaveChangesAsync();

            return ToDto(transaction);
        }

        public async Task<DebtorPaymentTransaction> RecordSystemDebtIncreaseAsync(
            Guid userId,
            long debtorId,
            decimal amount,
            string note)
        {
            if (amount <= 0)
                throw new BadRequestException(MessageKeys.DebtorPaymentAmountZero);

            var debtor = await _uow.Debtors.GetByIdAsync(debtorId)
                ?? throw new NotFoundException(MessageKeys.DebtorNotFound);

            var transaction = new DebtorPaymentTransaction
            {
                DebtorId = debtor.DebtorId,
                Amount = amount,
                PaymentMethod = PaymentMethods.System,
                Notes = note,
                BalanceBefore = debtor.CurrentBalance,
                BalanceAfter = DebtBalanceSemanticHelper.ApplyOrderDebtIncrease(debtor.CurrentBalance, amount),
                CreatedByUserId = userId,
                PaidAt = DateTime.UtcNow
            };

            await ApplyDebtTransactionAsync(debtor, transaction);
            return transaction;
        }

        public async Task<DebtorPaymentTransaction> RecordSystemDebtRollbackAsync(
            Guid userId,
            long debtorId,
            decimal amount,
            string note)
        {
            if (amount <= 0)
                throw new BadRequestException(MessageKeys.DebtorPaymentAmountZero);

            var debtor = await _uow.Debtors.GetByIdAsync(debtorId)
                ?? throw new NotFoundException(MessageKeys.DebtorNotFound);

            var transaction = new DebtorPaymentTransaction
            {
                DebtorId = debtor.DebtorId,
                Amount = amount,
                PaymentMethod = PaymentMethods.System,
                Notes = note,
                BalanceBefore = debtor.CurrentBalance,
                BalanceAfter = DebtBalanceSemanticHelper.ApplyOrderDebtRollback(debtor.CurrentBalance, amount),
                CreatedByUserId = userId,
                PaidAt = DateTime.UtcNow
            };

            await ApplyDebtTransactionAsync(debtor, transaction);
            return transaction;
        }

        public Task RecordSystemDebtRollbackLedgerEntryAsync(
            DebtorPaymentTransaction persistedRollback,
            int businessLocationId,
            CancellationToken cancellationToken = default)
            => _generalLedgerService.RecordDebtPaymentAsync(
                persistedRollback,
                businessLocationId,
                DebtPaymentActions.SystemRollback);

        public async Task<IEnumerable<DebtorPaymentDto>> GetPaymentsAsync(Guid userId, long debtorId)
        {
            var debtor = await GetDebtorAndVerifyOwnerAsync(userId, debtorId);

            var transactions = await _uow.Debtors.GetPaymentsAsync(debtorId);
            return transactions
                .Select(transaction =>
                {
                    var dto = ToDto(transaction);
                    dto.DebtorName = debtor.Name;
                    dto.DebtorPhone = debtor.Phone;
                    return dto;
                })
                .ToList();
        }

        public async Task<DebtorBalanceSyncResultDto> SyncCurrentBalancesAsync(
            long? debtorId = null,
            CancellationToken cancellationToken = default)
        {
            var (matchedCount, updatedCount) = await _uow.Debtors.SyncCurrentBalancesAsync(debtorId, cancellationToken);
            return new DebtorBalanceSyncResultDto
            {
                MatchedDebtorCount = matchedCount,
                UpdatedDebtorCount = updatedCount
            };
        }

        #region Private Helpers

        private async Task VerifyLocationOwnerAsync(Guid userId, int locationId)
        {
            await _locationService.ValidateOwnerAsync(userId, locationId);
        }

        private static string ResolveDebtAction(DebtorPaymentTransaction transaction, string debtDirection)
        {
            if (transaction.PaymentMethod.Equals(PaymentMethods.System, StringComparison.OrdinalIgnoreCase))
                return DebtPaymentActions.SystemRollback;

            return debtDirection switch
            {
                DebtDirection.Decrease => DebtPaymentActions.DecreaseDebt,
                DebtDirection.Increase => DebtPaymentActions.IncreaseDebt,
                _ => DebtPaymentActions.DecreaseDebt
            };
        }

        private async Task ApplyDebtTransactionAsync(Debtor debtor, DebtorPaymentTransaction transaction)
        {
            debtor.CurrentBalance = transaction.BalanceAfter;
            debtor.UpdatedAt = DateTime.UtcNow;

            await _uow.Debtors.AddPaymentAsync(transaction);
            _uow.Debtors.Update(debtor);
        }

        private async Task<Debtor> GetDebtorAndVerifyOwnerAsync(Guid userId, long debtorId)
        {
            var debtor = await _uow.Debtors.GetByIdAsync(debtorId)
                ?? throw new NotFoundException(MessageKeys.DebtorNotFound);

            await VerifyLocationOwnerAsync(userId, debtor.BusinessLocationId);

            return debtor;
        }

        #endregion
    }
}

