namespace BizFlow.Application.Common.Models;

/// <summary>Background hard-delete of soft-deleted accounts (Hangfire).</summary>
public sealed class AccountPurgeOptions
{
    public const string SectionName = "AccountPurge";

    /// <summary>Days after <see cref="Domain.Entities.Account.DeletedAt"/> before physical purge runs.</summary>
    public int RetentionDays { get; set; } = 30;

    /// <summary>Max accounts to process per job run.</summary>
    public int BatchSize { get; set; } = 10;
}
