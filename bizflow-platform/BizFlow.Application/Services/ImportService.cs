using AutoMapper;
using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Exceptions;
using BizFlow.Application.Common.Helpers;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.DTOs.Accounting;
using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.Import;
using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Application.Interfaces.Services;
using BizFlow.Domain.Entities;
using BizFlow.Domain.Enums;
using System.Text.Json.Nodes;

namespace BizFlow.Application.Services
{
    public class ImportService : IImportService
    {
        private const string DraftPaymentMethodMetadataKey = "paymentMethod";
        private readonly IUnitOfWork _unitOfWork;
        private readonly IImageService _imageService;
        private readonly IStockMovementService _stockMovementService;
        private readonly ICostService _costService;
        private readonly IBackgroundJobScheduler _backgroundJobScheduler;
        private readonly IMapper _mapper;
        private readonly IReferenceLabelService _labels;
        private readonly IMessageService _messageService;

        public ImportService(
            IUnitOfWork unitOfWork,
            IImageService imageService,
            IStockMovementService stockMovementService,
            ICostService costService,
            IBackgroundJobScheduler backgroundJobScheduler,
            IMapper mapper,
            IReferenceLabelService labels,
            IMessageService messageService)
        {
            _unitOfWork = unitOfWork;
            _imageService = imageService;
            _stockMovementService = stockMovementService;
            _costService = costService;
            _backgroundJobScheduler = backgroundJobScheduler;
            _mapper = mapper;
            _labels = labels;
            _messageService = messageService;
        }

        private ImportSummaryDto ToDto(Import import)
        {
            var dto = _mapper.Map<ImportSummaryDto>(import);
            dto.ImportType = _labels.ToOption(ReferenceCategory.ImportType, import.ImportType);
            dto.Status = _labels.ToOption(ReferenceCategory.ImportStatus, import.Status);
            return dto;
        }

        private async Task<ImportDetailDto> ToDetailDtoAsync(Import import)
        {
            var dto = _mapper.Map<ImportDetailDto>(import);
            dto.ImportType = _labels.ToOption(ReferenceCategory.ImportType, import.ImportType);
            dto.Status = _labels.ToOption(ReferenceCategory.ImportStatus, import.Status);
            var importCost = await _unitOfWork.Costs.GetByImportIdAsync(import.ImportId);
            var paymentMethodCode = importCost?.PaymentMethod ?? GetDraftPaymentMethodFromMetadata(import.SchemaDataJson);
            dto.PaymentMethod = _labels.ToOptionOrNull(
                ReferenceCategory.PaymentMethod,
                paymentMethodCode);
            return dto;
        }

        #region Query Methods

        // =========================================================
        // 1. Get Template
        // =========================================================

        public async Task<ImportSchemaDto> GetTemplateAsync()
        {
            var activeSchema = await _unitOfWork.ImportSchemas.GetActiveAsync();
            if (activeSchema == null)
                throw new NotFoundException(MessageKeys.NotFound);

            var activeVersion = activeSchema.ImportSchemaVersions
                .FirstOrDefault(v => v.IsActive);
            if (activeVersion == null)
                throw new NotFoundException(MessageKeys.NotFound);

            return new ImportSchemaDto { SchemaJson = activeVersion.SchemaJson };
        }

        // =========================================================
        // 2. Create Import
        // =========================================================

        public async Task<ImportSummaryDto> CreateImportAsync(Guid userId, CreateImportRequest request)
        {
            if (!ImportType.IsValid(request.ImportType.Trim()))
                throw new BadRequestException(MessageKeys.BadRequest);

            var normalizedImportType = request.ImportType.Trim().ToUpperInvariant();

            // Validate location exists
            var location = await _unitOfWork.BusinessLocations.GetByIdAsync(request.BusinessLocationId);
            if (location == null)
                throw new NotFoundException(MessageKeys.ImportLocationNotFound);

            await EnsureOwnerOfLocationAsync(userId, request.BusinessLocationId);

            // If not a draft, ReceivedAt is required
            if (!request.SaveAsDraft && !request.ReceivedAt.HasValue)
                throw new BadRequestException(MessageKeys.ImportDateRequiredOnConfirm);

            if (!request.SaveAsDraft && (request.Items == null || request.Items.Count == 0))
                throw new BadRequestException(MessageKeys.ImportItemsRequiredOnConfirm);

            var normalizedPaymentMethod = NormalizePaymentMethodOrThrow(request.PaymentMethod);

            // Validate and build items
            var (items, totalAmount) = await BuildImportItemsAsync(request.BusinessLocationId, request.Items);

            var status = request.SaveAsDraft ? ImportStatus.Draft : ImportStatus.Confirmed;

            var import = await CreateImportRecordAsync(
                importType: normalizedImportType,
                status: status,
                businessLocationId: request.BusinessLocationId,
                items: items,
                totalAmount: totalAmount,
                supplier: request.Supplier,
                memo: request.Note,
                receivedAt: request.SaveAsDraft ? null : request.ReceivedAt,
                imageStream: request.ImageStream,
                imageFileName: request.ImageFileName,
                applyToStock: !request.SaveAsDraft,
                draftPaymentMethod: request.SaveAsDraft ? normalizedPaymentMethod : null);

            // Auto create import cost + GL when created directly as CONFIRMED.
            if (status == ImportStatus.Confirmed)
            {
                await _costService.CreateImportCostAsync(
                    userId,
                    import,
                    request.DocumentNumber,
                    request.DocumentDate,
                    normalizedPaymentMethod);
            }

            var result = ToDto(import);
            result.BusinessLocationName = location.LocationName;
            return result;
        }

        // =========================================================
        // 3. Update Import
        // =========================================================

        public async Task<ImportUpdateResultDto> UpdateImportAsync(Guid userId, long importId, UpdateImportRequest request)
        {
            var import = await _unitOfWork.Imports.GetByIdWithItemsAsync(importId);
            if (import == null)
                throw new NotFoundException(MessageKeys.NotFound);

            await EnsureOwnerOfLocationAsync(userId, import.BusinessLocationId);

            if (string.Equals(import.Status, ImportStatus.Cancelled, StringComparison.OrdinalIgnoreCase))
                throw new BadRequestException(MessageKeys.ImportCannotEditCancelledOrReplaced);

            if (string.Equals(import.Status, ImportStatus.Draft, StringComparison.OrdinalIgnoreCase))
                return new ImportUpdateResultDto { IsReplacement = false, Import = await UpdateDraftImportAsync(import, request) };

            if (string.Equals(import.Status, ImportStatus.Confirmed, StringComparison.OrdinalIgnoreCase))
                return await ReplaceConfirmedImportAsync(userId, importId, request);

            throw new BadRequestException(MessageKeys.BadRequest);
        }

        private async Task<ImportSummaryDto> UpdateDraftImportAsync(Import import, UpdateImportRequest request)
        {
            // ImportType cannot be null on entity; keep current when request omits it.
            if (!string.IsNullOrWhiteSpace(request.ImportType)
                && !ImportType.IsValid(request.ImportType.Trim()))
                throw new BadRequestException(MessageKeys.BadRequest);

            import.ImportType = string.IsNullOrWhiteSpace(request.ImportType)
                ? import.ImportType
                : request.ImportType.Trim().ToUpperInvariant();
            import.Supplier = request.Supplier;
            import.Note = request.Note;
            import.ReceivedAt = request.ReceivedAt;
            import.UpdatedAt = DateTime.UtcNow;

            if (request.RemoveImage && !string.IsNullOrEmpty(import.ImagePublicId))
            {
                import.ImageUrl = null;
                import.ImagePublicId = null;
            }

            if (request.ImageStream != null)
            {
                var imageInfo = await _imageService.UploadImageAsync(
                    request.ImageStream, request.ImageFileName, ImageUploadTarget.Costs);
                import.ImageUrl = imageInfo.Url;
                import.ImagePublicId = imageInfo.PublicId;
            }

            if (request.PaymentMethod != null)
            {
                var normalizedPaymentMethod = NormalizePaymentMethodOrThrow(request.PaymentMethod);
                import.SchemaDataJson = UpsertDraftPaymentMethodMetadata(
                    import.SchemaDataJson,
                    normalizedPaymentMethod,
                    removeWhenNull: true);
            }

            foreach (var old in import.ProductsImports.ToList())
                _unitOfWork.Imports.DeleteItem(old);

            if (request.Items != null && request.Items.Count > 0)
            {
                var (newItems, totalAmount) = await BuildImportItemsAsync(import.BusinessLocationId, request.Items);
                foreach (var item in newItems)
                    item.ImportId = import.ImportId;

                import.ProductsImports = newItems;
                import.TotalAmount = totalAmount;
            }
            else
            {
                import.ProductsImports = new List<ProductImport>();
                import.TotalAmount = 0;
            }

            _unitOfWork.Imports.Update(import);
            await _unitOfWork.SaveChangesAsync();

            var location = await _unitOfWork.BusinessLocations.GetByIdAsync(import.BusinessLocationId);
            var dto = ToDto(import);
            if (location != null)
                dto.BusinessLocationName = location.LocationName;
            return dto;
        }

        private async Task<ImportUpdateResultDto> ReplaceConfirmedImportAsync(Guid userId, long oldImportId, UpdateImportRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
                throw new BadRequestException(MessageKeys.ImportEditConfirmedRequiresIdempotencyKey);

            var idemKey = request.IdempotencyKey.Trim();
            if (idemKey.Length > 100)
                throw new BadRequestException(MessageKeys.BadRequest);

            var existing = await _unitOfWork.Imports.GetLatestReplacementByRefImportIdAsync(oldImportId, idemKey);
            if (existing != null)
            {
                return new ImportUpdateResultDto
                {
                    IsReplacement = true,
                    Replacement = new PostedRecordReplacementResultDto
                    {
                        OldRecordId = oldImportId,
                        OldRecordStatus = _labels.ToOption(ReferenceCategory.ImportStatus, ImportStatus.Cancelled),
                        NewRecordId = existing.ImportId,
                        NewRecordStatus = _labels.ToOption(ReferenceCategory.ImportStatus, existing.Status)
                    }
                };
            }

            if (request.Items == null || request.Items.Count == 0)
                throw new BadRequestException(MessageKeys.ImportItemsRequiredOnConfirm);

            var oldPreview = await _unitOfWork.Imports.GetByIdWithItemsAsync(oldImportId)
                ?? throw new NotFoundException(MessageKeys.NotFound);

            var receivedAt = request.ReceivedAt ?? oldPreview.ReceivedAt;
            if (!receivedAt.HasValue)
                throw new BadRequestException(MessageKeys.ImportDateRequiredOnConfirm);
            var normalizedPaymentMethod = NormalizePaymentMethodOrThrow(request.PaymentMethod);

            Import newImportResult = null!;

            await EntityCodeGenerator.ExecuteWithDuplicateKeyRetryAsync(() => _unitOfWork.ExecuteResilientAsync(async ct =>
            {
                await _unitOfWork.Imports.LockImportRowForUpdateAsync(oldImportId, ct);

                var old = await _unitOfWork.Imports.GetByIdWithItemsAsync(oldImportId)
                    ?? throw new NotFoundException(MessageKeys.NotFound);

                if (!string.Equals(old.Status, ImportStatus.Confirmed, StringComparison.OrdinalIgnoreCase))
                    throw new BadRequestException(MessageKeys.BadRequest);

                var dup = await _unitOfWork.Imports.GetLatestReplacementByRefImportIdAsync(oldImportId, idemKey, ct);
                if (dup != null)
                {
                    newImportResult = dup;
                    return;
                }

                var newCode = EntityCodeGenerator.Generate("IMP", DateTime.UtcNow, old.BusinessLocationId, 3);

                if (!string.IsNullOrWhiteSpace(request.ImportType)
                    && !ImportType.IsValid(request.ImportType.Trim()))
                    throw new BadRequestException(MessageKeys.BadRequest);

                var importType = string.IsNullOrWhiteSpace(request.ImportType)
                    ? old.ImportType
                    : request.ImportType.Trim().ToUpperInvariant();

                var (newItems, totalAmount) = await BuildImportItemsAsync(old.BusinessLocationId, request.Items!);

                await RevertImportFromProductsAsync(
                    old.ProductsImports,
                    old.ImportId,
                    old.CancelledAt?.ToString("O") ?? DateTime.UtcNow.ToString("O"));

                await _costService.ReverseImportCostAsync(userId, old, MessageKeys.ImportCancelledReversalReason);

                old.Status = ImportStatus.Cancelled;
                old.CancelledAt = DateTime.UtcNow;
                old.CancelledBy = userId;
                old.CancelReason = _messageService.GetMessage(MessageKeys.ImportReplacedReason, newCode);
                old.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Imports.Update(old);

                var newImport = new Import
                {
                    ImportCode = newCode,
                    ImportType = importType,
                    Status = ImportStatus.Confirmed,
                    BusinessLocationId = old.BusinessLocationId,
                    Supplier = request.Supplier ?? old.Supplier,
                    Note = request.Note ?? old.Note,
                    ReceivedAt = receivedAt,
                    TotalAmount = totalAmount,
                    RefImportId = old.ImportId,
                    IdempotencyKey = idemKey,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    ConfirmedAt = DateTime.UtcNow
                };

                if (request.RemoveImage && !string.IsNullOrEmpty(old.ImagePublicId))
                {
                    newImport.ImageUrl = null;
                    newImport.ImagePublicId = null;
                }
                else if (request.ImageStream != null)
                {
                    var imageInfo = await _imageService.UploadImageAsync(
                        request.ImageStream, request.ImageFileName, ImageUploadTarget.Costs);
                    newImport.ImageUrl = imageInfo.Url;
                    newImport.ImagePublicId = imageInfo.PublicId;
                }
                else
                {
                    newImport.ImageUrl = old.ImageUrl;
                    newImport.ImagePublicId = old.ImagePublicId;
                }

                await _unitOfWork.Imports.AddAsync(newImport);
                await _unitOfWork.SaveChangesAsync(ct);

                foreach (var item in newItems)
                    item.ImportId = newImport.ImportId;

                newImport.ProductsImports = newItems;

                await ApplyImportToProductsAsync(newItems, newImport.ImportId, newImport.Note);
                await _unitOfWork.SaveChangesAsync(ct);

                await _costService.CreateImportCostInCurrentTransactionAsync(
                    userId,
                    newImport,
                    request.DocumentNumber,
                    request.DocumentDate,
                    normalizedPaymentMethod,
                    cancellationToken: ct);

                newImportResult = newImport;
            }));

            _backgroundJobScheduler.EnqueueAiAnomalyCheck(newImportResult.BusinessLocationId, "import", newImportResult.ImportId);

            return new ImportUpdateResultDto
            {
                IsReplacement = true,
                Replacement = new PostedRecordReplacementResultDto
                {
                    OldRecordId = oldImportId,
                    OldRecordStatus = _labels.ToOption(ReferenceCategory.ImportStatus, ImportStatus.Cancelled),
                    NewRecordId = newImportResult.ImportId,
                    NewRecordStatus = _labels.ToOption(ReferenceCategory.ImportStatus, newImportResult.Status)
                }
            };
        }

        // =========================================================
        // 4. Patch Import (Confirm DRAFT)
        // =========================================================

        public async Task<ImportPatchResultDto> PatchImportAsync(Guid userId, long importId, PatchImportRequest request)
        {
            var import = await _unitOfWork.Imports.GetByIdWithItemsAsync(importId);
            if (import == null)
                throw new NotFoundException(MessageKeys.NotFound);

            await EnsureOwnerOfLocationAsync(userId, import.BusinessLocationId);

            // Must be DRAFT to confirm
            if (import.Status != ImportStatus.Draft)
                throw new BadRequestException(MessageKeys.ImportOnlyDraftCanBeEdited);

            if (!request.ReceivedAt.HasValue)
                throw new BadRequestException(MessageKeys.ImportDateRequiredOnConfirm);

            if (import.ProductsImports == null || import.ProductsImports.Count == 0)
                throw new BadRequestException(MessageKeys.ImportItemsRequiredOnConfirm);

            var normalizedPaymentMethod = NormalizePaymentMethodOrThrow(request.PaymentMethod);
            var paymentMethodForCost = normalizedPaymentMethod
                ?? GetDraftPaymentMethodFromMetadata(import.SchemaDataJson);

            // Add quantity to product stock and update CostPrice
            await ApplyImportToProductsAsync(import.ProductsImports, import.ImportId, import.Note);

            import.ReceivedAt = request.ReceivedAt;
            import.Status = ImportStatus.Confirmed;
            import.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Imports.Update(import);
            await _unitOfWork.SaveChangesAsync();

            await _costService.CreateImportCostAsync(
                userId,
                import,
                request.DocumentNumber,
                request.DocumentDate,
                paymentMethodForCost);

            import.SchemaDataJson = UpsertDraftPaymentMethodMetadata(
                import.SchemaDataJson,
                null,
                removeWhenNull: true);
            _unitOfWork.Imports.Update(import);
            await _unitOfWork.SaveChangesAsync();

            // Fire-and-forget: enqueue AI anomaly check via Hangfire.
            // If AI Service is down, the job will retry — does not block user.
            _backgroundJobScheduler.EnqueueAiAnomalyCheck(import.BusinessLocationId, "import", import.ImportId);

            return new ImportPatchResultDto
            {
                ImportId = import.ImportId,
                ImportCode = import.ImportCode,
                Status = _labels.ToOption(ReferenceCategory.ImportStatus, import.Status),
                ReceivedAt = import.ReceivedAt,
                UpdatedAt = import.UpdatedAt
            };
        }

        // =========================================================
        // 5. List Imports (paginated)
        // =========================================================

        public async Task<PaginatedResponse<ImportSummaryDto>> ListImportsAsync(Guid userId, ImportQueryParams query)
        {
            if (!query.BusinessLocationId.HasValue)
                throw new BadRequestException(MessageKeys.BadRequest);

            if (!string.IsNullOrWhiteSpace(query.Status)
                && !ImportStatus.IsValid(query.Status.Trim()))
                throw new BadRequestException(MessageKeys.BadRequest);

            if (!string.IsNullOrWhiteSpace(query.ImportType)
                && !ImportType.IsValid(query.ImportType.Trim()))
                throw new BadRequestException(MessageKeys.BadRequest);

            if (!string.IsNullOrWhiteSpace(query.Status))
                query.Status = query.Status.Trim().ToUpperInvariant();

            if (!string.IsNullOrWhiteSpace(query.ImportType))
                query.ImportType = query.ImportType.Trim().ToUpperInvariant();

            await EnsureAccessToLocationAsync(userId, query.BusinessLocationId.Value);

            var (imports, totalCount) = await _unitOfWork.Imports.SearchAsync(query);

            var items = imports.Select(ToDto).ToList();

            var pageNumber = query.PageNumber ?? 1;
            var pageSize = query.PageSize ?? 10;

            return new PaginatedResponse<ImportSummaryDto>(items, totalCount, pageNumber, pageSize);
        }

        // =========================================================
        // 6. Get Import Detail
        // =========================================================

        public async Task<ImportDetailDto> GetImportDetailAsync(Guid userId, long importId)
        {
            var import = await _unitOfWork.Imports.GetByIdWithItemsAsync(importId);
            if (import == null)
                throw new NotFoundException(MessageKeys.NotFound);

            await EnsureAccessToLocationAsync(userId, import.BusinessLocationId);

            return await ToDetailDtoAsync(import);
        }

        #endregion

        #region Command Methods

        public async Task<long> CreateInventoryAdjustmentImportAsync(Guid userId, int businessLocationId, long productId, decimal quantity, decimal costPrice, string? memo = null)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(productId);
            if (product == null)
                throw new NotFoundException(MessageKeys.ImportProductNotFound);

            ValidateProductLocationAndCostPrice(product, businessLocationId, costPrice);

            var items = new List<ProductImport>
            {
                new ProductImport
                {
                    ProductId = productId,
                    Quantity = quantity,
                    CostPrice = costPrice,
                    TotalPrice = quantity * costPrice,
                    BaseUnit = product.Unit,
                    CreatedAt = DateTime.UtcNow
                }
            };

            var import = await CreateImportRecordAsync(
                importType: ImportType.InventoryAdjustment,
                status: ImportStatus.Confirmed,
                businessLocationId: businessLocationId,
                items: items,
                totalAmount: quantity * costPrice,
                supplier: null,
                memo: memo,
                receivedAt: DateTime.UtcNow,
                imageStream: null,
                imageFileName: null,
                applyToStock: true,
                draftPaymentMethod: null);

            await _costService.CreateImportCostAsync(userId, import);

            return import.ImportId;
        }

        // =========================================================
        // 7. Delete Import
        // =========================================================

        public async Task DeleteImportAsync(Guid userId, long importId)
        {
            var import = await _unitOfWork.Imports.GetByIdWithItemsAsync(importId);
            if (import == null)
                throw new NotFoundException(MessageKeys.NotFound);

            await EnsureOwnerOfLocationAsync(userId, import.BusinessLocationId);

            if (import.Status == ImportStatus.Cancelled)
                throw new BadRequestException(MessageKeys.ImportAlreadyCancelled);

            if (import.Status == ImportStatus.Confirmed)
            {
                // Soft cancel: reverse stock + CostPrice, mark as CANCELLED.
                // For cancel flows, StockMovement memo follows CancelledAt.
                import.CancelledAt = DateTime.UtcNow;
                await RevertImportFromProductsAsync(import.ProductsImports, import.ImportId, import.CancelledAt.Value.ToString("O"));

                import.Status = ImportStatus.Cancelled;
                import.CancelledBy = userId;
                import.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Imports.Update(import);

                await _costService.ReverseImportCostAsync(userId, import, MessageKeys.ImportCancelledReversalReason);
            }
            else
            {
                // DRAFT → hard delete
                _unitOfWork.Imports.Delete(import);
            }

            await _unitOfWork.SaveChangesAsync();
        }

        #endregion

        #region Private Helpers

        /// <summary>
        /// Apply confirmed import: add stock + update CostPrice to latest import price
        /// </summary>
        private async Task ApplyImportToProductsAsync(ICollection<ProductImport> items, long importId, string? memo = null)
        {
            foreach (var item in items)
            {
                var product = item.Product ?? await _unitOfWork.Products.GetByIdAsync(item.ProductId);
                if (product == null)
                    throw new NotFoundException(MessageKeys.ImportProductNotFound);

                product.Stock += item.Quantity;
                product.CostPrice = item.CostPrice;

                if (item.Quantity != 0)
                {
                    var movement = _stockMovementService.CreateStockMovement(
                        product,
                        item.Quantity,
                        StockMovementReferenceType.Import,
                        importId,
                        memo);
                    product.StockMovements.Add(movement);
                }

                _unitOfWork.Products.Update(product);
            }
        }

        /// <summary>
        /// Revert cancelled import: subtract stock + restore CostPrice from previous confirmed import
        /// </summary>
        private async Task RevertImportFromProductsAsync(ICollection<ProductImport> items, long importId, string? memo = null)
        {
            foreach (var item in items)
            {
                var product = item.Product ?? await _unitOfWork.Products.GetByIdAsync(item.ProductId);
                if (product == null) continue;

                var previousStock = product.Stock;
                product.Stock = Math.Max(0, product.Stock - item.Quantity);
                var actualOut = previousStock - product.Stock;

                if (actualOut != 0)
                {
                    var movement = _stockMovementService.CreateStockMovement(
                        product,
                        -actualOut,
                        StockMovementReferenceType.Import,
                        importId,
                        memo);
                    product.StockMovements.Add(movement);
                }

                // Restore CostPrice to the latest remaining confirmed import's price
                var previousCostPrice = await _unitOfWork.Products
                    .GetLatestCostPriceFromImportsAsync(item.ProductId, importId);
                product.CostPrice = previousCostPrice ?? 0;

                _unitOfWork.Products.Update(product);
            }
        }

        private async Task<Import> CreateImportRecordAsync(
            string importType,
            string status,
            int businessLocationId,
            List<ProductImport> items,
            decimal totalAmount,
            string? supplier,
            string? memo,
            DateTime? receivedAt,
            Stream? imageStream,
            string? imageFileName,
            bool applyToStock,
            string? draftPaymentMethod = null)
        {
            return await EntityCodeGenerator.ExecuteWithDuplicateKeyRetryAsync(() => _unitOfWork.ExecuteResilientAsync(async _ =>
            {
                var now = DateTime.UtcNow;
                var import = new Import
                {
                    ImportCode = EntityCodeGenerator.Generate("IMP", now, businessLocationId, 3),
                    ImportType = importType,
                    Status = status,
                    BusinessLocationId = businessLocationId,
                    Supplier = supplier,
                    Note = memo,
                    ReceivedAt = receivedAt,
                    SchemaDataJson = UpsertDraftPaymentMethodMetadata(
                        schemaDataJson: null,
                        paymentMethod: draftPaymentMethod,
                        removeWhenNull: false),
                    TotalAmount = totalAmount,
                    CreatedAt = now,
                    UpdatedAt = status == ImportStatus.Draft ? null : now,
                    ConfirmedAt = status == ImportStatus.Confirmed ? now : null
                };

                if (imageStream != null)
                {
                    var imageInfo = await _imageService.UploadImageAsync(imageStream, imageFileName, ImageUploadTarget.Costs);
                    import.ImageUrl = imageInfo.Url;
                    import.ImagePublicId = imageInfo.PublicId;
                }

                await _unitOfWork.Imports.AddAsync(import);
                await _unitOfWork.SaveChangesAsync();

                foreach (var item in items)
                    item.ImportId = import.ImportId;

                import.ProductsImports = items;

                if (applyToStock)
                    await ApplyImportToProductsAsync(items, import.ImportId, import.Note);

                await _unitOfWork.SaveChangesAsync();
                return import;
            }));
        }

        private async Task<(List<ProductImport> items, decimal totalAmount)> BuildImportItemsAsync(
            int businessLocationId,
            List<ImportItemRequest> requestItems)
        {
            var items = new List<ProductImport>();
            decimal totalAmount = 0;

            // Allow import creation/update without products (note/image-only import metadata updates).
            if (requestItems == null || requestItems.Count == 0)
                return (items, totalAmount);

            foreach (var req in requestItems)
            {
                var product = await _unitOfWork.Products.GetByIdAsync(req.ProductId);
                if (product == null)
                    throw new NotFoundException(MessageKeys.ImportProductNotFound);

                ValidateProductLocationAndCostPrice(product, businessLocationId, req.CostPrice);

                var totalPrice = req.Quantity * req.CostPrice;

                items.Add(new ProductImport
                {
                    ProductId = req.ProductId,
                    Quantity = req.Quantity,
                    CostPrice = req.CostPrice,
                    TotalPrice = totalPrice,
                    BaseUnit = product.Unit,
                    CreatedAt = DateTime.UtcNow
                });

                totalAmount += totalPrice;
            }

            return (items, totalAmount);
        }

        private static void ValidateProductLocationAndCostPrice(Product product, int businessLocationId, decimal costPrice)
        {
            if (product.BusinessLocationId != businessLocationId)
                throw new BadRequestException(MessageKeys.BadRequest);

            if (costPrice < 0)
                throw new BadRequestException(MessageKeys.BadRequest);
        }

        private static string? NormalizePaymentMethodOrThrow(string? paymentMethod)
        {
            if (string.IsNullOrWhiteSpace(paymentMethod))
                return null;

            if (!PaymentMethods.IsValid(paymentMethod))
                throw new BadRequestException(MessageKeys.BadRequest);

            return paymentMethod.Trim().ToLowerInvariant();
        }

        private static string? GetDraftPaymentMethodFromMetadata(string? schemaDataJson)
        {
            if (string.IsNullOrWhiteSpace(schemaDataJson))
                return null;

            try
            {
                var node = JsonNode.Parse(schemaDataJson);
                if (node is not JsonObject jsonObject
                    || !jsonObject.TryGetPropertyValue(DraftPaymentMethodMetadataKey, out var paymentMethodNode))
                    return null;

                var paymentMethod = paymentMethodNode?.GetValue<string>();
                if (string.IsNullOrWhiteSpace(paymentMethod) || !PaymentMethods.IsValidInternal(paymentMethod))
                    return null;

                return paymentMethod.Trim().ToLowerInvariant();
            }
            catch
            {
                return null;
            }
        }

        private static string? UpsertDraftPaymentMethodMetadata(
            string? schemaDataJson,
            string? paymentMethod,
            bool removeWhenNull)
        {
            JsonObject jsonObject;

            try
            {
                jsonObject = string.IsNullOrWhiteSpace(schemaDataJson)
                    ? new JsonObject()
                    : JsonNode.Parse(schemaDataJson) as JsonObject ?? new JsonObject();
            }
            catch
            {
                jsonObject = new JsonObject();
            }

            if (string.IsNullOrWhiteSpace(paymentMethod))
            {
                if (removeWhenNull)
                    jsonObject.Remove(DraftPaymentMethodMetadataKey);
            }
            else
            {
                jsonObject[DraftPaymentMethodMetadataKey] = paymentMethod;
            }

            return jsonObject.Count == 0 ? null : jsonObject.ToJsonString();
        }

        private async Task EnsureOwnerOfLocationAsync(Guid userId, int locationId)
        {
            var isOwner = await _unitOfWork.BusinessLocations.IsOwnerOfLocationAsync(userId, locationId);
            if (!isOwner)
                throw new ForbiddenException(MessageKeys.Forbidden);
        }

        private async Task EnsureAccessToLocationAsync(Guid userId, int locationId)
        {
            var hasAccess = await _unitOfWork.BusinessLocations.HasAccessToLocationAsync(userId, locationId);
            if (!hasAccess)
                throw new ForbiddenException(MessageKeys.Forbidden);
        }

        #endregion
    }
}
