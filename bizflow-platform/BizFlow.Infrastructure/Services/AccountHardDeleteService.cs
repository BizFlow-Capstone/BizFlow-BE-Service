using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Application.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace BizFlow.Infrastructure.Services;

public sealed class AccountHardDeleteService : IAccountHardDeleteService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAccountPurgeRepository _purgeRepository;
    private readonly ILogger<AccountHardDeleteService> _logger;

    public AccountHardDeleteService(
        IUnitOfWork unitOfWork,
        IAccountPurgeRepository purgeRepository,
        ILogger<AccountHardDeleteService> logger)
    {
        _unitOfWork = unitOfWork;
        _purgeRepository = purgeRepository;
        _logger = logger;
    }

    public async Task<bool> HardDeleteAccountNowAsync(
        Guid accountId,
        CancellationToken cancellationToken = default
    )
    {
        var eligibilityCutoffUtc = DateTime.UtcNow;
        var committed = false;
        Guid? purgedProfileId = null;

        await _unitOfWork.ExecuteResilientPurgeAsync(
            async ct =>
            {
                var account = await _unitOfWork.Accounts.GetTrackedByIdAsync(accountId);
                if (account == null)
                {
                    return false;
                }

                if (account.DeletedAt == null)
                {
                    account.IsActive = false;
                    account.DeletedAt = eligibilityCutoffUtc;
                    account.UpdatedAt = eligibilityCutoffUtc;
                    await _unitOfWork.SaveChangesAsync(ct);
                }

                var (isCommitted, profileId) =
                    await _purgeRepository.TryPurgeOneAccountWithinTransactionAsync(
                        accountId,
                        eligibilityCutoffUtc,
                        ct
                    );
                committed = isCommitted;
                purgedProfileId = profileId;
                return isCommitted;
            },
            cancellationToken
        );

        if (committed)
        {
            _logger.LogInformation(
                "Immediate hard-delete completed. AccountId={AccountId}, ProfileId={ProfileId}",
                accountId,
                purgedProfileId);
        }

        return committed;
    }
}
