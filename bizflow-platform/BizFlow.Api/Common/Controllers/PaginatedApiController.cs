using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.Common.Models;
using Microsoft.Extensions.Options;

namespace BizFlow.Api.Common.Controllers
{
    /// <summary>
    /// Base controller for endpoints that support pagination.
    /// Provides ApplyPaginationDefaults with validation (< 1 → default).
    /// </summary>
    public abstract class PaginatedApiController : BaseApiController
    {
        protected readonly PaginationSettings PaginationSettings;

        protected PaginatedApiController(
            IMessageService messageService,
            ILogger logger,
            IOptions<PaginationSettings> paginationSettings)
            : base(messageService, logger)
        {
            PaginationSettings = paginationSettings.Value;
        }

        /// <summary>
        /// Apply default pagination values from settings. Invalid values (less than 1) are replaced with defaults.
        /// </summary>
        protected void ApplyPaginationDefaults(PaginationParams pagination)
        {
            if (pagination.PageNumber == null || pagination.PageNumber < 1)
                pagination.PageNumber = PaginationSettings.DefaultPageNumber;

            if (pagination.PageSize == null || pagination.PageSize < 1)
                pagination.PageSize = PaginationSettings.DefaultPageSize;

            if (pagination.PageSize > PaginationSettings.MaxPageSize)
                pagination.PageSize = PaginationSettings.MaxPageSize;
        }
    }
}
