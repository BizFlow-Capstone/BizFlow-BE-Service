using System.Text.Json;
using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Exceptions;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.DTOs.Accounting;
using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Application.Interfaces.Services;
using BizFlow.Domain.Entities;

namespace BizFlow.Application.Services;

public class AccountingPeriodService : IAccountingPeriodService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IReferenceLabelService _labels;

    public AccountingPeriodService(IUnitOfWork unitOfWork, IReferenceLabelService labels)
    {
        _unitOfWork = unitOfWork;
        _labels = labels;
    }

    public async Task<AccountingPeriodDto> CreatePeriodAsync(int locationId, Guid userId, CreateAccountingPeriodRequest request)
    {
        await EnsureOwnerAccessAsync(userId, locationId);

        var normalizedPeriodType = NormalizePeriodType(request.PeriodType);
        var quarter = ValidateAndNormalizeQuarter(normalizedPeriodType, request.Quarter);
        var (startDate, endDate) = CalculatePeriodRange(normalizedPeriodType, request.Year, quarter);

        var exists = await _unitOfWork.AccountingPeriods.ExistsAsync(locationId, normalizedPeriodType, request.Year, quarter);
        if (exists)
        {
            throw new ConflictException(MessageKeys.PeriodAlreadyExists);
        }

        var previousPeriod = await _unitOfWork.AccountingPeriods.GetPreviousPeriodAsync(locationId, normalizedPeriodType, startDate);
        var (openingCash, openingBank) = await ResolveOpeningBalancesAsync(
            locationId,
            previousPeriod,
            request.OpeningCashBalance,
            request.OpeningBankBalance,
            request.UseSuggestedOpeningBalances);

        var period = new AccountingPeriod
        {
            BusinessLocationId = locationId,
            PeriodType = normalizedPeriodType,
            Year = request.Year,
            Quarter = quarter,
            StartDate = startDate,
            EndDate = endDate,
            OpeningCashBalance = openingCash,
            OpeningBankBalance = openingBank,
            Status = AccountingPeriodConstants.PeriodStatuses.Open
        };

        await _unitOfWork.AccountingPeriods.AddAsync(period);
        await _unitOfWork.SaveChangesAsync();

        await _unitOfWork.AccountingPeriods.AddAuditLogAsync(new AccountingPeriodAuditLog
        {
            PeriodId = period.PeriodId,
            Action = AccountingPeriodConstants.AuditActions.PeriodCreated,
            NewValue = SerializeJson(new
            {
                period.PeriodType,
                period.Year,
                period.Quarter,
                period.OpeningCashBalance,
                period.OpeningBankBalance
            }),
            CreatedByUserId = userId
        });

        await _unitOfWork.SaveChangesAsync();
        return MapPeriod(period);
    }

    public async Task<AccountingPeriodDto> CreateCustomPeriodAsync(int locationId, Guid userId, CreateCustomAccountingPeriodRequest request)
    {
        await EnsureOwnerAccessAsync(userId, locationId);

        if (request.EndDate < request.StartDate)
        {
            throw new BadRequestException(MessageKeys.BadRequest, new { dateRange = "EndDate must be greater than or equal to StartDate" });
        }

        var previousPeriod = await _unitOfWork.AccountingPeriods.GetPreviousPeriodAsync(
            locationId,
            AccountingPeriodConstants.PeriodTypes.Custom,
            request.StartDate);

        var (openingCash, openingBank) = await ResolveOpeningBalancesAsync(
            locationId,
            previousPeriod,
            request.OpeningCashBalance,
            request.OpeningBankBalance,
            request.UseSuggestedOpeningBalances);

        var period = new AccountingPeriod
        {
            BusinessLocationId = locationId,
            PeriodType = AccountingPeriodConstants.PeriodTypes.Custom,
            Year = (short)request.StartDate.Year,
            Quarter = null,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            OpeningCashBalance = openingCash,
            OpeningBankBalance = openingBank,
            Status = AccountingPeriodConstants.PeriodStatuses.Open
        };

        await _unitOfWork.AccountingPeriods.AddAsync(period);
        await _unitOfWork.SaveChangesAsync();

        await _unitOfWork.AccountingPeriods.AddAuditLogAsync(new AccountingPeriodAuditLog
        {
            PeriodId = period.PeriodId,
            Action = AccountingPeriodConstants.AuditActions.PeriodCreated,
            NewValue = SerializeJson(new
            {
                period.PeriodType,
                period.StartDate,
                period.EndDate,
                period.OpeningCashBalance,
                period.OpeningBankBalance
            }),
            CreatedByUserId = userId
        });

        await _unitOfWork.SaveChangesAsync();
        return MapPeriod(period);
    }

    public async Task<OpeningBalanceSuggestionDto> GetOpeningBalanceSuggestionAsync(int locationId, Guid userId, OpeningBalanceSuggestionRequest request)
    {
        await EnsureOwnerAccessAsync(userId, locationId);

        var normalizedPeriodType = NormalizeSuggestionPeriodType(request.PeriodType);
        var startDate = normalizedPeriodType == AccountingPeriodConstants.PeriodTypes.Custom
            ? ValidateAndResolveCustomStartDate(request.StartDate)
            : CalculatePeriodRange(
                normalizedPeriodType,
                ValidateSuggestionYear(request.Year),
                ValidateAndNormalizeQuarter(normalizedPeriodType, request.Quarter)).StartDate;

        var previousPeriod = await _unitOfWork.AccountingPeriods.GetPreviousPeriodAsync(locationId, normalizedPeriodType, startDate);
        if (previousPeriod == null)
        {
            return new OpeningBalanceSuggestionDto
            {
                HasSuggestion = false,
                SuggestionReasonCode = MessageKeys.PeriodSuggestionNoSource
            };
        }

        var (openingCash, openingBank, previousOpeningCash, previousOpeningBank, netCash, netBank) =
            await CalculateCarryFromPreviousPeriodAsync(locationId, previousPeriod);

        return new OpeningBalanceSuggestionDto
        {
            HasSuggestion = true,
            SuggestionReasonCode = MessageKeys.PeriodSuggestionFromPrevious,
            CalculationExplanationCode = MessageKeys.PeriodSuggestionFormula,
            OpeningCashBalance = openingCash,
            OpeningBankBalance = openingBank,
            SourcePeriodId = previousPeriod.PeriodId,
            SourceStartDate = previousPeriod.StartDate,
            SourceEndDate = previousPeriod.EndDate,
            CalculationBreakdown = new OpeningBalanceCalculationBreakdownDto
            {
                PreviousOpeningCashBalance = previousOpeningCash,
                PreviousOpeningBankBalance = previousOpeningBank,
                NetCashInSourcePeriod = netCash,
                NetBankInSourcePeriod = netBank,
                SuggestedOpeningCashBalance = openingCash,
                SuggestedOpeningBankBalance = openingBank
            }
        };
    }

    public async Task<List<AccountingPeriodDto>> GetPeriodsAsync(int locationId, Guid userId)
    {
        await EnsureOwnerAccessAsync(userId, locationId);
        var periods = await _unitOfWork.AccountingPeriods.GetByLocationAsync(locationId);
        return periods.Select(MapPeriod).ToList();
    }

    public async Task<AccountingPeriodDto> GetPeriodDetailAsync(int locationId, long periodId, Guid userId)
    {
        await EnsureOwnerAccessAsync(userId, locationId);

        var period = await _unitOfWork.AccountingPeriods.GetByLocationAndIdAsync(locationId, periodId);
        if (period == null)
        {
            throw new NotFoundException(MessageKeys.PeriodNotFound);
        }

        return MapPeriod(period);
    }

    public async Task<AccountingPeriodDto> FinalizePeriodAsync(int locationId, long periodId, Guid userId)
    {
        await EnsureOwnerAccessAsync(userId, locationId);

        var period = await _unitOfWork.AccountingPeriods.GetByLocationAndIdAsync(locationId, periodId);
        if (period == null)
        {
            throw new NotFoundException(MessageKeys.PeriodNotFound);
        }

        if (period.Status == AccountingPeriodConstants.PeriodStatuses.Finalized)
        {
            throw new BadRequestException(MessageKeys.PeriodAlreadyFinalized);
        }

        if (period.Status != AccountingPeriodConstants.PeriodStatuses.Open &&
            period.Status != AccountingPeriodConstants.PeriodStatuses.Reopened)
        {
            throw new BadRequestException(MessageKeys.PeriodNotOpen);
        }

        var activeBooks = await _unitOfWork.AccountingPeriods.CountActiveBooksAsync(periodId);
        if (activeBooks <= 0)
        {
            throw new BadRequestException(MessageKeys.PeriodNoBooks);
        }

        period.Status = AccountingPeriodConstants.PeriodStatuses.Finalized;
        period.FinalizedAt = DateTime.UtcNow;
        period.FinalizedByUserId = userId;

        _unitOfWork.AccountingPeriods.Update(period);

        await _unitOfWork.AccountingPeriods.AddAuditLogAsync(new AccountingPeriodAuditLog
        {
            PeriodId = period.PeriodId,
            Action = AccountingPeriodConstants.AuditActions.PeriodFinalized,
            NewValue = SerializeJson(new
            {
                period.Status,
                period.FinalizedAt,
                period.FinalizedByUserId
            }),
            CreatedByUserId = userId
        });

        await _unitOfWork.SaveChangesAsync();
        return MapPeriod(period);
    }

    public async Task<AccountingPeriodDto> ReopenPeriodAsync(int locationId, long periodId, Guid userId, string reason)
    {
        await EnsureOwnerAccessAsync(userId, locationId);

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new BadRequestException(MessageKeys.PeriodReopenReasonRequired);
        }

        var period = await _unitOfWork.AccountingPeriods.GetByLocationAndIdAsync(locationId, periodId);
        if (period == null)
        {
            throw new NotFoundException(MessageKeys.PeriodNotFound);
        }

        if (period.Status != AccountingPeriodConstants.PeriodStatuses.Finalized)
        {
            throw new BadRequestException(MessageKeys.PeriodNotFinalized);
        }

        var oldValue = SerializeJson(new { status = period.Status });

        period.Status = AccountingPeriodConstants.PeriodStatuses.Reopened;
        _unitOfWork.AccountingPeriods.Update(period);

        await _unitOfWork.AccountingPeriods.AddAuditLogAsync(new AccountingPeriodAuditLog
        {
            PeriodId = period.PeriodId,
            Action = AccountingPeriodConstants.AuditActions.PeriodReopened,
            OldValue = oldValue,
            NewValue = SerializeJson(new { status = period.Status }),
            Reason = reason.Trim(),
            CreatedByUserId = userId
        });

        await _unitOfWork.SaveChangesAsync();
        return MapPeriod(period);
    }

    public async Task DeletePeriodAsync(int locationId, long periodId, Guid userId)
    {
        await EnsureOwnerAccessAsync(userId, locationId);

        var period = await _unitOfWork.AccountingPeriods.GetByLocationAndIdAsync(locationId, periodId)
            ?? throw new NotFoundException(MessageKeys.PeriodNotFound);

        if (period.Status != AccountingPeriodConstants.PeriodStatuses.Open)
            throw new BadRequestException(MessageKeys.BadRequest,
                "Only open periods can be deleted. Finalized or reopened periods cannot be removed.");

        var bookCount = await _unitOfWork.AccountingPeriods.CountActiveBooksAsync(periodId);
        if (bookCount > 0)
            throw new BadRequestException(MessageKeys.BadRequest,
                "Cannot delete a period that has accounting books. Remove all books first.");

        var hasTaxPayments = await _unitOfWork.AccountingPeriods.HasTaxPaymentsAsync(periodId);
        if (hasTaxPayments)
            throw new BadRequestException(MessageKeys.BadRequest,
                "Cannot delete a period that has tax payment records.");

        await _unitOfWork.AccountingPeriods.RemoveAuditLogsAsync(periodId);
        _unitOfWork.AccountingPeriods.Remove(period);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<List<AccountingPeriodAuditLogDto>> GetAuditLogsAsync(int locationId, long periodId, Guid userId)
    {
        await EnsureOwnerAccessAsync(userId, locationId);

        var period = await _unitOfWork.AccountingPeriods.GetByLocationAndIdAsync(locationId, periodId);
        if (period == null)
        {
            throw new NotFoundException(MessageKeys.PeriodNotFound);
        }

        var logs = await _unitOfWork.AccountingPeriods.GetAuditLogsAsync(periodId);
        return logs.Select(x => new AccountingPeriodAuditLogDto
        {
            LogId = x.LogId,
            PeriodId = x.PeriodId,
            Action = _labels.ToOption(ReferenceCategory.AccountingPeriodAuditAction, x.Action),
            OldValue = x.OldValue,
            NewValue = x.NewValue,
            Reason = x.Reason,
            CreatedByUserId = x.CreatedByUserId,
            CreatedAt = x.CreatedAt
        }).ToList();
    }

    private async Task EnsureOwnerAccessAsync(Guid userId, int locationId)
    {
        var location = await _unitOfWork.BusinessLocations.GetByIdAsync(locationId);
        if (location == null)
        {
            throw new NotFoundException(MessageKeys.NotFound);
        }

        var isOwner = await _unitOfWork.BusinessLocations.IsOwnerOfLocationAsync(userId, locationId);
        if (!isOwner)
        {
            throw new ForbiddenException(MessageKeys.Forbidden);
        }
    }

    private static string NormalizePeriodType(string periodType)
    {
        var normalized = (periodType ?? string.Empty).Trim().ToLowerInvariant();
        if (normalized != AccountingPeriodConstants.PeriodTypes.Quarter &&
            normalized != AccountingPeriodConstants.PeriodTypes.Year)
        {
            throw new BadRequestException(MessageKeys.BadRequest, new { periodType = "quarter|year" });
        }

        return normalized;
    }

    private static string NormalizeSuggestionPeriodType(string periodType)
    {
        var normalized = (periodType ?? string.Empty).Trim().ToLowerInvariant();
        if (normalized != AccountingPeriodConstants.PeriodTypes.Quarter &&
            normalized != AccountingPeriodConstants.PeriodTypes.Year &&
            normalized != AccountingPeriodConstants.PeriodTypes.Custom)
        {
            throw new BadRequestException(MessageKeys.BadRequest, new { periodType = "quarter|year|custom" });
        }

        return normalized;
    }

    private static short ValidateSuggestionYear(short? year)
    {
        if (!year.HasValue)
        {
            throw new BadRequestException(MessageKeys.BadRequest, new { year = "Year is required for periodType=quarter|year" });
        }

        return year.Value;
    }

    private static DateOnly ValidateAndResolveCustomStartDate(DateOnly? startDate)
    {
        if (!startDate.HasValue)
        {
            throw new BadRequestException(MessageKeys.BadRequest, new { startDate = "StartDate is required for periodType=custom" });
        }

        return startDate.Value;
    }

    private static int? ValidateAndNormalizeQuarter(string periodType, int? quarter)
    {
        if (periodType == AccountingPeriodConstants.PeriodTypes.Year)
        {
            if (quarter.HasValue)
            {
                throw new BadRequestException(MessageKeys.BadRequest, new { quarter = "Quarter must be null when periodType=year" });
            }

            return null;
        }

        if (!quarter.HasValue || quarter is < 1 or > 4)
        {
            throw new BadRequestException(MessageKeys.BadRequest, new { quarter = "Quarter is required and must be between 1 and 4" });
        }

        return quarter;
    }

    private static (DateOnly StartDate, DateOnly EndDate) CalculatePeriodRange(string periodType, short year, int? quarter)
    {
        if (periodType == AccountingPeriodConstants.PeriodTypes.Year)
        {
            return (new DateOnly(year, 1, 1), new DateOnly(year, 12, 31));
        }

        var q = quarter!.Value;
        var startMonth = ((q - 1) * 3) + 1;
        var endMonth = startMonth + 2;

        var start = new DateOnly(year, startMonth, 1);
        var end = new DateOnly(year, endMonth, DateTime.DaysInMonth(year, endMonth));
        return (start, end);
    }

    private async Task<(decimal? OpeningCash, decimal? OpeningBank)> ResolveOpeningBalancesAsync(
        int locationId,
        AccountingPeriod? previousPeriod,
        decimal? requestedOpeningCash,
        decimal? requestedOpeningBank,
        bool useSuggestedOpeningBalances)
    {
        var openingCash = requestedOpeningCash;
        var openingBank = requestedOpeningBank;

        if (openingCash.HasValue && openingBank.HasValue)
        {
            return (openingCash, openingBank);
        }

        if (!useSuggestedOpeningBalances)
        {
            throw new BadRequestException(MessageKeys.PeriodOpeningBalanceRequired);
        }

        if (previousPeriod == null)
        {
            throw new BadRequestException(MessageKeys.PeriodOpeningBalanceRequired);
        }

        var (suggestedOpeningCash, suggestedOpeningBank, _, _, _, _) = await CalculateCarryFromPreviousPeriodAsync(locationId, previousPeriod);
        return (suggestedOpeningCash, suggestedOpeningBank);
    }

    private async Task<(
        decimal OpeningCash,
        decimal OpeningBank,
        decimal PreviousOpeningCash,
        decimal PreviousOpeningBank,
        decimal NetCash,
        decimal NetBank)> CalculateCarryFromPreviousPeriodAsync(
        int locationId,
        AccountingPeriod previousPeriod)
    {
        var (netCash, netBank) = await _unitOfWork.AccountingPeriods.CalculateNetCashAndBankAsync(
            locationId,
            previousPeriod.StartDate,
            previousPeriod.EndDate);

        var previousOpeningCash = previousPeriod.OpeningCashBalance ?? 0;
        var previousOpeningBank = previousPeriod.OpeningBankBalance ?? 0;
        var openingCash = previousOpeningCash + netCash;
        var openingBank = previousOpeningBank + netBank;

        return (
            openingCash,
            openingBank,
            previousOpeningCash,
            previousOpeningBank,
            netCash,
            netBank);
    }

    private AccountingPeriodDto MapPeriod(AccountingPeriod period)
    {
        return new AccountingPeriodDto
        {
            PeriodId = period.PeriodId,
            BusinessLocationId = period.BusinessLocationId,
            PeriodType = _labels.ToOption(ReferenceCategory.AccountingPeriodType, period.PeriodType),
            Year = period.Year,
            Quarter = period.Quarter,
            StartDate = period.StartDate,
            EndDate = period.EndDate,
            OpeningCashBalance = period.OpeningCashBalance,
            OpeningBankBalance = period.OpeningBankBalance,
            Status = _labels.ToOption(ReferenceCategory.AccountingPeriodStatus, period.Status),
            FinalizedAt = period.FinalizedAt,
            FinalizedByUserId = period.FinalizedByUserId,
            CreatedAt = period.CreatedAt,
            UpdatedAt = period.UpdatedAt
        };
    }

    private static string SerializeJson(object value)
    {
        return JsonSerializer.Serialize(value);
    }
}