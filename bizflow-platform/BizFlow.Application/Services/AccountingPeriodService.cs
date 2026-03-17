using System.Text.Json;
using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Exceptions;
using BizFlow.Application.DTOs.Accounting;
using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Application.Interfaces.Services;
using BizFlow.Domain.Entities;

namespace BizFlow.Application.Services;

public class AccountingPeriodService : IAccountingPeriodService
{
    private readonly IUnitOfWork _unitOfWork;

    public AccountingPeriodService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
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
        var openingCash = request.OpeningCashBalance;
        var openingBank = request.OpeningBankBalance;

        if (previousPeriod == null)
        {
            if (!openingCash.HasValue || !openingBank.HasValue)
            {
                throw new BadRequestException(MessageKeys.PeriodOpeningBalanceRequired);
            }
        }
        else if (!openingCash.HasValue || !openingBank.HasValue)
        {
            var (netCash, netBank) = await _unitOfWork.AccountingPeriods.CalculateNetCashAndBankAsync(
                locationId,
                previousPeriod.StartDate,
                previousPeriod.EndDate);

            openingCash ??= (previousPeriod.OpeningCashBalance ?? 0) + netCash;
            openingBank ??= (previousPeriod.OpeningBankBalance ?? 0) + netBank;
        }

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
            Action = x.Action,
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

    private static AccountingPeriodDto MapPeriod(AccountingPeriod period)
    {
        return new AccountingPeriodDto
        {
            PeriodId = period.PeriodId,
            BusinessLocationId = period.BusinessLocationId,
            PeriodType = period.PeriodType,
            Year = period.Year,
            Quarter = period.Quarter,
            StartDate = period.StartDate,
            EndDate = period.EndDate,
            OpeningCashBalance = period.OpeningCashBalance,
            OpeningBankBalance = period.OpeningBankBalance,
            Status = period.Status,
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