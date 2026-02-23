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
        private readonly IMapper _mapper;

        public ImportService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        // =========================================================
        // 1. Get Template
        // =========================================================

        public async Task<ImportSchemaDto> GetTemplateAsync()
        {
            var version = await _unitOfWork.Imports.GetActiveSchemaVersionAsync();
            if (version == null)
                throw new NotFoundException(MessageKeys.ImportNotFound);

            return new ImportSchemaDto { SchemaJson = version.SchemaJson };
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

            var import = new Import
            {
                ImportCode = importCode,
                ImportType = request.ImportType,
                Status = status,
                BusinessLocationId = request.BusinessLocationId,
                Supplier = request.Supplier,
                Note = request.Note,
                ReceivedAt = request.SaveAsDraft ? null : request.ReceivedAt,
                TotalAmount = totalAmount,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = request.SaveAsDraft ? null : DateTime.UtcNow
            };

            await _unitOfWork.Imports.AddAsync(import);
            await _unitOfWork.SaveChangesAsync();

            // Add items after import is saved (FK ImportId)
            foreach (var item in items)
                item.ImportId = import.ImportId;

            import.ProductsImports = items;

            // If CONFIRMED → update stock immediately
            if (!request.SaveAsDraft)
            {
                foreach (var item in items)
                {
                    var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId);
                    if (product == null)
                        throw new NotFoundException(MessageKeys.ImportProductNotFound);

                    product.Stock = product.Stock + item.Quantity;
                    _unitOfWork.Products.Update(product);
                }
            }

            await _unitOfWork.SaveChangesAsync();

            var result = _mapper.Map<ImportSummaryDto>(import);
            result.BusinessLocationName = location.Name;
            return result;
        }

        // =========================================================
        // 3. Update Import
        // =========================================================

        public async Task<ImportSummaryDto> UpdateImportAsync(Guid userId, long importId, UpdateImportRequest request)
        {
            var import = await _unitOfWork.Imports.GetByIdWithItemsAsync(importId);
            if (import == null)
                throw new NotFoundException(MessageKeys.ImportNotFound);

            if (import.Status != ImportStatus.Draft)
                throw new BadRequestException(MessageKeys.ImportOnlyDraftCanBeEdited);

            // Always overwrite all fields (null = clear the value)
            import.ImportType = request.ImportType;
            import.Supplier = request.Supplier;
            import.Note = request.Note;
            import.ReceivedAt = request.ReceivedAt;
            import.UpdatedAt = DateTime.UtcNow;

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
                throw new NotFoundException(MessageKeys.ImportNotFound);

            // Must be DRAFT to confirm
            if (import.Status != ImportStatus.Draft)
                throw new BadRequestException(MessageKeys.ImportOnlyDraftCanBeEdited);

            if (!request.ReceivedAt.HasValue)
                throw new BadRequestException(MessageKeys.ImportDateRequiredOnConfirm);

            // Add quantity to product stock
            foreach (var item in import.ProductsImports)
            {
                var product = item.Product;
                if (product == null)
                    throw new NotFoundException(MessageKeys.ImportProductNotFound);

                product.Stock = product.Stock + item.Quantity;
                _unitOfWork.Products.Update(product);
            }

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
                throw new NotFoundException(MessageKeys.ImportNotFound);

            return _mapper.Map<ImportDetailDto>(import);
        }

        // =========================================================
        // 7. Delete Import
        // =========================================================

        public async Task DeleteImportAsync(Guid userId, long importId)
        {
            var import = await _unitOfWork.Imports.GetByIdWithItemsAsync(importId);
            if (import == null)
                throw new NotFoundException(MessageKeys.ImportNotFound);

            if (import.Status == ImportStatus.Cancelled)
                throw new BadRequestException(MessageKeys.ImportAlreadyCancelled);

            if (import.Status == ImportStatus.Confirmed)
            {
                // Soft cancel: reverse stock, mark as CANCELLED (keep the record)
                foreach (var item in import.ProductsImports)
                {
                    var product = item.Product;
                    if (product != null)
                    {
                        product.Stock = Math.Max(0, product.Stock - item.Quantity);
                        _unitOfWork.Products.Update(product);
                    }
                }

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

        private static string GenerateImportCode()
        {
            return Guid.NewGuid().ToString("N")[..12].ToUpper();
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
