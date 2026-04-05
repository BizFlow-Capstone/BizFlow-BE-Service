using BizFlow.Application.Common.Models;
using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Application.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BizFlow.Infrastructure.Services;

public sealed class AccountHardDeleteService : IAccountHardDeleteService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAccountPurgeRepository _purgeRepository;
    private readonly IOptions<AccountPurgeOptions> _options;
    private readonly ILogger<AccountHardDeleteService> _logger;

    public AccountHardDeleteService(
        IUnitOfWork unitOfWork,
        IAccountPurgeRepository purgeRepository,
        IOptions<AccountPurgeOptions> options,
        ILogger<AccountHardDeleteService> logger)
    {
        _unitOfWork = unitOfWork;
        _purgeRepository = purgeRepository;
        _options = options;
        _logger = logger;
    }

    public async Task<int> ProcessPendingHardDeletesAsync(CancellationToken cancellationToken = default)
    {
        var opts = _options.Value;
        var batch = Math.Max(1, opts.BatchSize);
        var retentionDays = Math.Max(0, opts.RetentionDays);
        var cutoff = DateTime.UtcNow.AddDays(-retentionDays);

        var accountIds = await _purgeRepository.GetPendingPurgeAccountIdsAsync(cutoff, batch, cancellationToken);

        var processed = 0;
        foreach (var accountId in accountIds)
        {
            try
            {
                var committed = false;
                Guid? purgedProfileId = null;
                await _unitOfWork.ExecuteResilientPurgeAsync(async ct =>
                {
                    var (c, pid) = await _purgeRepository.TryPurgeOneAccountWithinTransactionAsync(
                        accountId,
                        cutoff,
                        ct);
                    committed = c;
                    purgedProfileId = pid;
                    return c;
                }, cancellationToken);

                if (!committed)
                    continue;

                processed++;
                if (purgedProfileId == null)
                    _logger.LogInformation("Hard-deleted account without profile. AccountId={AccountId}", accountId);
                else
                    _logger.LogInformation(
                        "Hard-deleted account and owned data. AccountId={AccountId}, ProfileId={ProfileId}",
                        accountId,
                        purgedProfileId.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Account hard-delete failed. AccountId={AccountId}", accountId);
            }
        }

        return processed;
    }
}
