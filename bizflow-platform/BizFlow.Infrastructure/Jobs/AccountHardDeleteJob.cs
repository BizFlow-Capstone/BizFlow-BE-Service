using BizFlow.Application.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace BizFlow.Infrastructure.Jobs;

public sealed class AccountHardDeleteJob
{
    private readonly IAccountHardDeleteService _hardDeleteService;
    private readonly ILogger<AccountHardDeleteJob> _logger;

    public AccountHardDeleteJob(
        IAccountHardDeleteService hardDeleteService,
        ILogger<AccountHardDeleteJob> logger)
    {
        _hardDeleteService = hardDeleteService;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        try
        {
            var n = await _hardDeleteService.ProcessPendingHardDeletesAsync();
            if (n > 0)
                _logger.LogInformation("Account hard-delete job processed {Count} account(s).", n);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Account hard-delete job failed.");
        }
    }
}
