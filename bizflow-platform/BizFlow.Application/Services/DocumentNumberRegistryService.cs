using System.Text.RegularExpressions;
using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Exceptions;
using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Application.Interfaces.Services;

namespace BizFlow.Application.Services
{
    /// <inheritdoc cref="IDocumentNumberRegistryService"/>
    public class DocumentNumberRegistryService : IDocumentNumberRegistryService
    {
        private const int MaxDocumentNumberLength = 100;

        private static readonly Regex WhitespaceCollapseRegex = new(@"\s+", RegexOptions.Compiled);

        private readonly IUnitOfWork _uow;

        public DocumentNumberRegistryService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public string? NormalizeOrNull(string? documentNumber)
        {
            if (string.IsNullOrWhiteSpace(documentNumber))
                return null;

            var trimmed = WhitespaceCollapseRegex.Replace(documentNumber.Trim(), " ").ToUpperInvariant();

            if (trimmed.Length > MaxDocumentNumberLength)
                throw new BadRequestException(MessageKeys.DocumentNumberTooLong);

            return trimmed.Length == 0 ? null : trimmed;
        }

        public async Task EnsureLockAndAssertUniqueAsync(
            Guid ownerId,
            string documentNumberNormalized,
            long? excludeCostId = null,
            long? excludeRevenueId = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(documentNumberNormalized))
                throw new ArgumentException("documentNumberNormalized must be non-empty. Call NormalizeOrNull first and skip the check when it returns null.", nameof(documentNumberNormalized));

            // 1) Ensure the (OwnerId, DocumentNumberNormalized) lock row exists.
            //    Under concurrency, INSERT IGNORE makes the ensure-step safe.
            await _uow.AccountingDocumentLocks.EnsureExistsAsync(
                ownerId, documentNumberNormalized, cancellationToken);

            // 2) Acquire the row-level write lock. This serializes all concurrent
            //    peers that touch the same (OwnerId, DocumentNumberNormalized).
            await _uow.AccountingDocumentLocks.LockForUpdateAsync(
                ownerId, documentNumberNormalized, cancellationToken);

            // 3) Duplicate-check Costs and Revenues (including cancelled / soft-deleted rows).
            var costConflict = await _uow.Costs.ExistsByDocumentNumberForOwnerAsync(
                ownerId, documentNumberNormalized, excludeCostId, cancellationToken);
            if (costConflict)
                throw new ConflictException(MessageKeys.DocumentNumberDuplicated, documentNumberNormalized);

            var revenueConflict = await _uow.Revenues.ExistsByDocumentNumberForOwnerAsync(
                ownerId, documentNumberNormalized, excludeRevenueId, cancellationToken);
            if (revenueConflict)
                throw new ConflictException(MessageKeys.DocumentNumberDuplicated, documentNumberNormalized);
        }
    }
}
