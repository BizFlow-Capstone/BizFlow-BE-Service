using AutoMapper;
using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Exceptions;
using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.Import;
using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Application.Interfaces.Services;
using BizFlow.Domain.Entities;
using BizFlow.Domain.Enums;

namespace BizFlow.Application.Services
{
    public class ImportService : IImportService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IImageService _imageService;
        private readonly IStockMovementService _stockMovementService;
        private readonly IMapper _mapper;

        public ImportService(IUnitOfWork unitOfWork, IImageService imageService, IStockMovementService stockMovementService, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _imageService = imageService;
            _stockMovementService = stockMovementService;
            _mapper = mapper;
        }

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
            // Validate location exists
            var location = await _unitOfWork.BusinessLocations.GetByIdAsync(request.BusinessLocationId);
            if (location == null)
                throw new NotFoundException(MessageKeys.ImportLocationNotFound);

            // If not a draft, ReceivedAt is required
            if (!request.SaveAsDraft && !request.ReceivedAt.HasValue)
                throw new BadRequestException(MessageKeys.ImportDateRequiredOnConfirm);

            // Validate and build items
            var (items, totalAmount) = await BuildImportItemsAsync(request.Items);

            // Generate unique import code (no DB query needed)
            var importCode = GenerateImportCode();

            var status = request.SaveAsDraft ? ImportStatus.Draft : ImportStatus.Confirmed;

            var import = await CreateImportRecordAsync(
                importType: request.ImportType,
                status: status,
                businessLocationId: request.BusinessLocationId,
                items: items,
                totalAmount: totalAmount,
                supplier: request.Supplier,
                note: request.Note,
                receivedAt: request.SaveAsDraft ? null : request.ReceivedAt,
                imageStream: request.ImageStream,
                imageFileName: request.ImageFileName,
                applyToStock: !request.SaveAsDraft);

            var result = _mapper.Map<ImportSummaryDto>(import);
            result.BusinessLocationName = location.LocationName;
            return result;
        }

        // =========================================================
        // 3. Update Import
        // =========================================================

        public async Task<ImportSummaryDto> UpdateImportAsync(Guid userId, long importId, UpdateImportRequest request)
        {
            var import = await _unitOfWork.Imports.GetByIdWithItemsAsync(importId);
            if (import == null)
                throw new NotFoundException(MessageKeys.NotFound);

            if (import.Status != ImportStatus.Draft)
                throw new BadRequestException(MessageKeys.ImportOnlyDraftCanBeEdited);

            // Always overwrite all fields (null = clear the value)
            import.ImportType = request.ImportType;
            import.Supplier = request.Supplier;
            import.Note = request.Note;
            import.ReceivedAt = request.ReceivedAt;
            import.UpdatedAt = DateTime.UtcNow;

            // Update Image
            if (request.RemoveImage && !string.IsNullOrEmpty(import.ImagePublicId))
            {
                // Orphan image on Cloudinary will be cleaned up by ImageCleanupJob
                import.ImageUrl = null;
                import.ImagePublicId = null;
            }

            // Handle new image upload (if provided)
            if (request.ImageStream != null)
            {
                var imageInfo = await _imageService.UploadImageAsync(
                    request.ImageStream, request.ImageFileName, ImageUploadTarget.Imports);
                import.ImageUrl = imageInfo.Url;
                import.ImagePublicId = imageInfo.PublicId;
            }

            // Always replace items — null or empty list = remove all
            foreach (var old in import.ProductsImports.ToList())
                _unitOfWork.Imports.DeleteItem(old);

            if (request.Items != null && request.Items.Count > 0)
            {
                var (newItems, totalAmount) = await BuildImportItemsAsync(request.Items);
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

            return _mapper.Map<ImportSummaryDto>(import);
        }

        // =========================================================
        // 4. Patch Import (Confirm DRAFT)
        // =========================================================

        public async Task<ImportPatchResultDto> PatchImportAsync(Guid userId, long importId, PatchImportRequest request)
        {
            var import = await _unitOfWork.Imports.GetByIdWithItemsAsync(importId);
            if (import == null)
                throw new NotFoundException(MessageKeys.NotFound);

            // Must be DRAFT to confirm
            if (import.Status != ImportStatus.Draft)
                throw new BadRequestException(MessageKeys.ImportOnlyDraftCanBeEdited);

            if (!request.ReceivedAt.HasValue)
                throw new BadRequestException(MessageKeys.ImportDateRequiredOnConfirm);

            // Add quantity to product stock and update CostPrice
            await ApplyImportToProductsAsync(import.ProductsImports, import.ImportId);

            import.ReceivedAt = request.ReceivedAt;
            import.Status = ImportStatus.Confirmed;
            import.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Imports.Update(import);
            await _unitOfWork.SaveChangesAsync();

            return new ImportPatchResultDto
            {
                ImportId = import.ImportId,
                ImportCode = import.ImportCode,
                Status = import.Status,
                ReceivedAt = import.ReceivedAt,
                UpdatedAt = import.UpdatedAt
            };
        }

        // =========================================================
        // 5. List Imports (paginated)
        // =========================================================

        public async Task<PaginatedResponse<ImportSummaryDto>> ListImportsAsync(Guid userId, ImportQueryParams query)
        {
            var (imports, totalCount) = await _unitOfWork.Imports.SearchAsync(query);

            var items = _mapper.Map<List<ImportSummaryDto>>(imports);

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

            return _mapper.Map<ImportDetailDto>(import);
        }

        public async Task<long> CreateInventoryAdjustmentImportAsync(int businessLocationId, long productId, int quantity, decimal costPrice)
        {
            if (quantity <= 0)
                throw new BadRequestException(MessageKeys.BadRequest);

            var import = new Import
            {
                ImportCode = GenerateImportCode(),
                ImportType = ImportType.InventoryAdjustment,
                Status = ImportStatus.Confirmed,
                BusinessLocationId = businessLocationId,
                TotalAmount = quantity * costPrice,
                CreatedAt = DateTime.UtcNow,
                ConfirmedAt = DateTime.UtcNow,
                ReceivedAt = DateTime.UtcNow
            };

            var product = await _unitOfWork.Products.GetByIdAsync(productId);
            if (product == null)
                throw new NotFoundException(MessageKeys.ImportProductNotFound);

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

            import = await CreateImportRecordAsync(
                importType: ImportType.InventoryAdjustment,
                status: ImportStatus.Confirmed,
                businessLocationId: businessLocationId,
                items: items,
                totalAmount: quantity * costPrice,
                supplier: null,
                note: null,
                receivedAt: DateTime.UtcNow,
                imageStream: null,
                imageFileName: null,
                applyToStock: false);

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

            if (import.Status == ImportStatus.Cancelled)
                throw new BadRequestException(MessageKeys.ImportAlreadyCancelled);

            if (import.Status == ImportStatus.Confirmed)
            {
                // Soft cancel: reverse stock + CostPrice, mark as CANCELLED
                await RevertImportFromProductsAsync(import.ProductsImports, import.ImportId);

                import.Status = ImportStatus.Cancelled;
                import.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Imports.Update(import);
            }
            else
            {
                // DRAFT → hard delete
                _unitOfWork.Imports.Delete(import);
            }

            await _unitOfWork.SaveChangesAsync();
        }

        // =========================================================
        // Helpers
        // =========================================================

        /// <summary>
        /// Apply confirmed import: add stock + update CostPrice to latest import price
        /// </summary>
        private async Task ApplyImportToProductsAsync(ICollection<ProductImport> items, long importId)
        {
            foreach (var item in items)
            {
                var product = item.Product ?? await _unitOfWork.Products.GetByIdAsync(item.ProductId);
                if (product == null)
                    throw new NotFoundException(MessageKeys.ImportProductNotFound);

                product.Stock += item.Quantity;
                product.CostPrice = item.CostPrice;

                var movement = _stockMovementService.CreateStockMovement(
                    product,
                    StockMovementType.In,
                    item.Quantity,
                    StockMovementReferenceType.Import,
                    importId);
                product.StockMovements.Add(movement);

                _unitOfWork.Products.Update(product);
            }
        }

        /// <summary>
        /// Revert cancelled import: subtract stock + restore CostPrice from previous confirmed import
        /// </summary>
        private async Task RevertImportFromProductsAsync(ICollection<ProductImport> items, long importId)
        {
            foreach (var item in items)
            {
                var product = item.Product ?? await _unitOfWork.Products.GetByIdAsync(item.ProductId);
                if (product == null) continue;

                var previousStock = product.Stock;
                product.Stock = Math.Max(0, product.Stock - item.Quantity);
                var actualOut = previousStock - product.Stock;

                var movement = _stockMovementService.CreateStockMovement(
                    product,
                    StockMovementType.Out,
                    -actualOut,
                    StockMovementReferenceType.Import,
                    importId);
                product.StockMovements.Add(movement);

                // Restore CostPrice to the latest remaining confirmed import's price
                var previousCostPrice = await _unitOfWork.Products
                    .GetLatestCostPriceFromImportsAsync(item.ProductId, importId);
                product.CostPrice = previousCostPrice ?? 0;

                _unitOfWork.Products.Update(product);
            }
        }

        private static string GenerateImportCode()
        {
            return Guid.NewGuid().ToString("N")[..12].ToUpper();
        }

        private async Task<Import> CreateImportRecordAsync(
            string importType,
            string status,
            int businessLocationId,
            List<ProductImport> items,
            decimal totalAmount,
            string? supplier,
            string? note,
            DateTime? receivedAt,
            Stream? imageStream,
            string? imageFileName,
            bool applyToStock)
        {
            var import = new Import
            {
                ImportCode = GenerateImportCode(),
                ImportType = importType,
                Status = status,
                BusinessLocationId = businessLocationId,
                Supplier = supplier,
                Note = note,
                ReceivedAt = receivedAt,
                TotalAmount = totalAmount,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = status == ImportStatus.Draft ? null : DateTime.UtcNow,
                ConfirmedAt = status == ImportStatus.Confirmed ? DateTime.UtcNow : null
            };

            if (imageStream != null)
            {
                var imageInfo = await _imageService.UploadImageAsync(imageStream, imageFileName, ImageUploadTarget.Imports);
                import.ImageUrl = imageInfo.Url;
                import.ImagePublicId = imageInfo.PublicId;
            }

            await _unitOfWork.Imports.AddAsync(import);
            await _unitOfWork.SaveChangesAsync();

            foreach (var item in items)
                item.ImportId = import.ImportId;

            import.ProductsImports = items;

            if (applyToStock)
                await ApplyImportToProductsAsync(items, import.ImportId);

            await _unitOfWork.SaveChangesAsync();

            return import;
        }

        private async Task<(List<ProductImport> items, decimal totalAmount)> BuildImportItemsAsync(
            List<ImportItemRequest> requestItems)
        {
            var items = new List<ProductImport>();
            decimal totalAmount = 0;

            foreach (var req in requestItems)
            {
                var product = await _unitOfWork.Products.GetByIdAsync(req.ProductId);
                if (product == null)
                    throw new NotFoundException(MessageKeys.ImportProductNotFound);

                var totalPrice = req.Quantity * req.CostPrice;

                items.Add(new ProductImport
                {
                    ProductId = req.ProductId,
                    Quantity = req.Quantity,
                    CostPrice = req.CostPrice,
                    TotalPrice = totalPrice,
                    BaseUnit = product.Unit
                });

                totalAmount += totalPrice;
            }

            return (items, totalAmount);
        }
    }
}
