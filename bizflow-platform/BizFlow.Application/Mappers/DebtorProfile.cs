using AutoMapper;
using BizFlow.Application.DTOs.Debtor;
using BizFlow.Domain.Entities;

namespace BizFlow.Application.Mappers
{
    public class DebtorProfile : AutoMapper.Profile
    {
        public DebtorProfile()
        {
            CreateMap<Debtor, DebtorSummaryDto>()
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive ?? true));

            CreateMap<Debtor, DebtorDetailDto>()
                .IncludeBase<Debtor, DebtorSummaryDto>();

            CreateMap<Debtor, DebtorMinimalDto>();

            // PaymentMethod is a reference i18n field — populated by the
            // service layer via IReferenceLabelService after AutoMapper
            // projection to avoid pulling DI into AutoMapper profiles.
            CreateMap<DebtorPaymentTransaction, DebtorPaymentDto>()
                .ForMember(dest => dest.TransactionId, opt => opt.MapFrom(src => src.DebtorPaymentTransactionId))
                .ForMember(dest => dest.PaymentMethod, opt => opt.Ignore());
        }

        #region Static mapping methods — used when audit fields must be set at call time

        public static Debtor ToEntity(CreateDebtorRequest request, Guid createdByUserId) => new()
        {
            BusinessLocationId = request.BusinessLocationId,
            Name = request.Name.Trim(),
            Phone = request.Phone?.Trim(),
            Address = request.Address?.Trim(),
            Notes = request.Notes?.Trim(),
            CreditLimit = request.CreditLimit,
            CurrentBalance = 0,
            IsActive = true,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        public static void ApplyUpdate(UpdateDebtorRequest request, Debtor debtor)
        {
            debtor.Name = request.Name.Trim();
            debtor.Phone = request.Phone?.Trim();
            debtor.Address = request.Address?.Trim();
            debtor.Notes = request.Notes?.Trim();
            debtor.CreditLimit = request.CreditLimit;
            debtor.UpdatedAt = DateTime.UtcNow;
        }

        public static DebtorPaymentTransaction ToEntity(
            RecordDebtPaymentRequest request,
            long debtorId,
            Guid createdByUserId,
            decimal balanceBefore) => new()
        {
            DebtorId = debtorId,
            Amount = request.Amount,
            PaymentMethod = request.PaymentMethod.ToLower(),
            Notes = request.Notes?.Trim(),
            BalanceBefore = balanceBefore,
            // Positive amount → balance increases → debt decreases. Negative amount → balance decreases → debt increases.
            BalanceAfter = balanceBefore + request.Amount,
            CreatedByUserId = createdByUserId,
            PaidAt = DateTime.UtcNow
        };

        #endregion
    }
}

