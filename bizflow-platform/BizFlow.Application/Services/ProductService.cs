using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Exceptions;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.Product;
using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Application.Interfaces.Services;
using BizFlow.Domain.Entities;
using BizFlow.Domain.Enums;
using AutoMapper;

namespace BizFlow.Application.Services
{
    public class ProductService : IProductService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IImageService _imageService;
        private readonly IBusinessLocationService _locationService;
        private readonly IImportService _importService;
        private readonly IStockMovementService _stockMovementService;
        private readonly IMessageService _messageService;
        private readonly IMapper _mapper;

        public ProductService(
            IUnitOfWork unitOfWork,
            IImageService imageService,
            IBusinessLocationService locationService,
            IImportService importService,
            IStockMovementService stockMovementService,
            IMessageService messageService,
            IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _imageService = imageService;
            _locationService = locationService;
            _importService = importService;
            _stockMovementService = stockMovementService;
            _messageService = messageService;
            _mapper = mapper;
        }

        #region Query Methods

        public async Task<PaginatedResponse<ProductSummaryDto>> SearchProductsAsync(Guid userId, ProductQueryParams query)
        {
            // Validate access (owner or employee, blocks employee when inactive — RULE-LOC-07)
            await ValidateProductAccessAsync(userId, query.LocationId);

            var (products, totalCount) = await _unitOfWork.Products.SearchAsync(query);

            var items = products.Select(p => _mapper.Map<ProductSummaryDto>(p)).ToList();

            // Use values with fallback (should be set by controller)
            var pageNumber = query.PageNumber ?? 1;
            var pageSize = query.PageSize ?? 10;

            return new PaginatedResponse<ProductSummaryDto>(items, totalCount, pageNumber, pageSize);
        }

        public async Task<List<ProductQuickSearchDto>> SearchQuickProductsAsync(Guid userId, int locationId, string? search)
        {
            await ValidateProductAccessAsync(userId, locationId);

            var products = await _unitOfWork.Products.QuickSearchByLocationAsync(locationId, search);
            return products.Select(p => _mapper.Map<ProductQuickSearchDto>(p)).ToList();
        }

        public async Task<ProductDetailDto?> GetProductDetailAsync(Guid userId, long productId)
        {
            var product = await _unitOfWork.Products.GetByIdWithDetailsAsync(productId);
            if (product == null)
            {
                throw new NotFoundException(MessageKeys.NotFound);
            }

            await ValidateProductAccessAsync(userId, product.BusinessLocationId);

            return _mapper.Map<ProductDetailDto>(product);
        }

        public async Task<ProductSaleItemsResponseDto?> GetProductSaleItemsAsync(Guid userId, long productId)
        {
            var product = await _unitOfWork.Products.GetByIdWithSaleItemsAsync(productId);
            if (product == null)
            {
                throw new NotFoundException(MessageKeys.NotFound);
            }

            await ValidateProductAccessAsync(userId, product.BusinessLocationId);

            return _mapper.Map<ProductSaleItemsResponseDto>(product);
        }

        public async Task<CostPriceHistoryDto> GetCostPriceHistoryAsync(Guid userId, long productId)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(productId);
            if (product == null)
            {
                throw new NotFoundException(MessageKeys.NotFound);
            }

            // Owner only
            var isOwner = await _unitOfWork.BusinessLocations.IsOwnerOfLocationAsync(userId, product.BusinessLocationId);
            if (!isOwner)
            {
                throw new ForbiddenException(MessageKeys.Forbidden);
            }

            var history = await _unitOfWork.Products.GetCostPriceHistoryAsync(productId);

            return new CostPriceHistoryDto
            {
                ProductId = product.ProductId,
                ProductName = product.ProductName,
                CurrentCostPrice = product.CostPrice,
                History = history
            };
        }

        #endregion

        #region Command Methods

        public async Task<(ProductSummaryDto Product, List<string>? Warnings)> CreateProductAsync(Guid userId, CreateProductRequest request)
        {
            // 1. Validate
            var isOwner = await _unitOfWork.BusinessLocations.IsOwnerOfLocationAsync(userId, request.LocationId);
            if (!isOwner)
                throw new ForbiddenException(MessageKeys.Forbidden);

            ValidatePriceTiers(request);
            var warnings = await CheckSkuDuplicateWarningAsync(request.LocationId, request.Sku);

            // 2. Build Product entity graph
            var product = _mapper.Map<Product>(request);
            product.Status = ProductStatus.Active;
            var initialStock = request.Stock;

            BuildSaleItems(product, request);

            if (request.ImageStream != null)
            {
                var imageInfo = await _imageService.UploadImageAsync(request.ImageStream, request.ImageFileName, ImageUploadTarget.Products);
                product.ImageUrl = imageInfo.Url;
                product.ImagePublicId = imageInfo.PublicId;
            }

            // 3. Save product
            await _unitOfWork.Products.AddAsync(product);
            await _unitOfWork.SaveChangesAsync();

            // 4. Record initial stock atomically; if import creation fails, compensate by deleting the product.
            if (initialStock > 0)
            {
                try
                {
                    var initialStockMemo = _messageService.GetMessage(MessageKeys.ProductInitialStockMemo);
                    await ApplyStockToTargetAsync(product, initialStock, request.CostPrice, initialStockMemo);
                    await _unitOfWork.SaveChangesAsync();
                }
                catch
                {
                    _unitOfWork.Products.Delete(product);
                    await _unitOfWork.SaveChangesAsync();
                    throw;
                }
            }

            var created = await _unitOfWork.Products.GetByIdWithDetailsAsync(product.ProductId) ?? product;

            return (_mapper.Map<ProductSummaryDto>(created), warnings);
        }

        public async Task<(ProductSummaryDto Product, List<string>? Warnings)> UpdateProductAsync(Guid userId, long productId, UpdateProductRequest request)
        {
            var product = await _unitOfWork.Products.GetByIdWithSaleItemsAsync(productId);
            if (product == null)
                throw new NotFoundException(MessageKeys.NotFound);

            // 1. Validate
            var isOwner = await _unitOfWork.BusinessLocations.IsOwnerOfLocationAsync(userId, product.BusinessLocationId);
            if (!isOwner)
                throw new ForbiddenException(MessageKeys.Forbidden);

            if (product.BusinessLocationId != request.LocationId)
                throw new BadRequestException(MessageKeys.ProductCannotChangeLocation);

            ValidatePriceTiers(request);
            var warnings = await CheckSkuDuplicateWarningAsync(request.LocationId, request.Sku, productId);

            // 2. Capture old stock before updating
            var oldStock = product.Stock;
            var oldCostPrice = product.CostPrice;

            // 3. Update properties from request
            _mapper.Map(request, product);

            // 4. Update image
            if (request.RemoveImage && !string.IsNullOrEmpty(product.ImagePublicId))
            {
                product.ImageUrl = null;
                product.ImagePublicId = null;
            }
            if (request.ImageStream != null)
            {
                var imageInfo = await _imageService.UploadImageAsync(
                    request.ImageStream, request.ImageFileName, ImageUploadTarget.Products);
                product.ImageUrl = imageInfo.Url;
                product.ImagePublicId = imageInfo.PublicId;
            }

            // 5. Reconcile SaleItems
            ReconcileSaleItems(product, request);

            // 6. Handle stock change — Stock is excluded from the mapper, so product.Stock is still oldStock here.
            var stockDiff = request.Stock - oldStock;
            var costPriceChanged = request.CostPrice != oldCostPrice;
            if (stockDiff != 0 || costPriceChanged)
            {
                var updateStockMemo = stockDiff != 0
                    ? _messageService.GetMessage(MessageKeys.ProductStockUpdatedOnUpdateMemo)
                    : null;

                await _importService.CreateInventoryAdjustmentImportAsync(
                    product.BusinessLocationId,
                    product.ProductId,
                    stockDiff,
                    request.CostPrice,
                    updateStockMemo);
            }

            _unitOfWork.Products.Update(product);
            await _unitOfWork.SaveChangesAsync();

            var updated = await _unitOfWork.Products.GetByIdWithDetailsAsync(product.ProductId) ?? product;

            return (_mapper.Map<ProductSummaryDto>(updated), warnings);
        }

        public async Task UpdateProductStatusAsync(Guid userId, long productId, string status)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(productId);
            if (product == null)
            {
                throw new NotFoundException(MessageKeys.NotFound);
            }

            // Validate ownership
            var isOwner = await _unitOfWork.BusinessLocations.IsOwnerOfLocationAsync(userId, product.BusinessLocationId);
            if (!isOwner)
            {
                throw new ForbiddenException(MessageKeys.Forbidden);
            }

            // Validate status value
            var normalizedStatus = status.ToLower();
            if (normalizedStatus != ProductStatus.Active && normalizedStatus != ProductStatus.Inactive)
            {
                throw new BadRequestException(MessageKeys.ProductInvalidStatus);
            }

            product.Status = normalizedStatus;
            _unitOfWork.Products.Update(product);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<ProductSummaryDto> AdjustProductStockAsync(Guid userId, long productId, AdjustProductStockRequest request)
        {
            var product = await _unitOfWork.Products.GetByIdWithDetailsAsync(productId);
            if (product == null)
                throw new NotFoundException(MessageKeys.NotFound);

            var isOwner = await _unitOfWork.BusinessLocations.IsOwnerOfLocationAsync(userId, product.BusinessLocationId);
            if (!isOwner)
                throw new ForbiddenException(MessageKeys.Forbidden);

            if (request.Stock == product.Stock)
                return _mapper.Map<ProductSummaryDto>(product);

            var costPriceForIncrease = request.CostPrice ?? product.CostPrice;
            var memo = string.IsNullOrWhiteSpace(request.Memo) ? null : request.Memo.Trim();

            await ApplyStockToTargetAsync(product, request.Stock, costPriceForIncrease, memo);

            _unitOfWork.Products.Update(product);
            await _unitOfWork.SaveChangesAsync();

            var updated = await _unitOfWork.Products.GetByIdWithDetailsAsync(product.ProductId) ?? product;
            return _mapper.Map<ProductSummaryDto>(updated);
        }

        public async Task BulkAdjustSellingPriceAsync(Guid userId, BulkAdjustSellingPriceRequest request)
        {
            if (request.SaleItemIds == null || request.SaleItemIds.Count == 0 || request.DeltaAmount == 0)
                throw new BadRequestException(MessageKeys.BadRequest);

            var requestedSaleItemIds = request.SaleItemIds.Distinct().ToList();
            var saleItems = await _unitOfWork.Products.GetSaleItemsForPriceAdjustAsync(requestedSaleItemIds);

            // Missing/deleted SaleItems are treated as invalid targets.
            if (saleItems.Count != requestedSaleItemIds.Count)
                throw new NotFoundException(MessageKeys.NotFound);

            var ownedLocationIds = (await _unitOfWork.BusinessLocations.GetLocationsByUserAsync(userId, isOwner: true))
                .Select(l => l.Id)
                .ToHashSet();

            if (ownedLocationIds.Count == 0)
                throw new ForbiddenException(MessageKeys.Forbidden);

            // Owner must manage every selected SaleItem's product location.
            if (saleItems.Any(si => !ownedLocationIds.Contains(si.Product.BusinessLocationId)))
                throw new ForbiddenException(MessageKeys.Forbidden);

            await _unitOfWork.ExecuteResilientAsync(async _ =>
            {
                foreach (var saleItem in saleItems)
                {
                    var defaultPolicy = saleItem.ProductPricePolicies.FirstOrDefault(pp => pp.IsDefault);
                    if (defaultPolicy == null)
                        throw new BadRequestException(MessageKeys.BadRequest);

                    var newPrice = defaultPolicy.Price + request.DeltaAmount;
                    if (newPrice < 0)
                        throw new BadRequestException(MessageKeys.BadRequest);

                    defaultPolicy.IsDefault = false;
                    defaultPolicy.EndAt = DateTime.UtcNow;

                    await _unitOfWork.Products.AddPricePolicyAsync(new ProductPricePolicy
                    {
                        SaleItemId = saleItem.SaleItemId,
                        Price = newPrice,
                        IsDefault = true,
                        StartAt = DateTime.UtcNow
                    });

                    // Keep Product.SellingPrice in sync with the base-unit sale item price.
                    if (string.Equals(saleItem.Unit?.Trim(), saleItem.Product.Unit?.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        saleItem.Product.SellingPrice = newPrice;
                        _unitOfWork.Products.Update(saleItem.Product);
                    }
                }
            });
        }

        public async Task DeleteProductAsync(Guid userId, long productId)
        {
            var product = await _unitOfWork.Products.GetByIdWithSaleItemsAsync(productId);
            if (product == null)
            {
                throw new NotFoundException(MessageKeys.NotFound);
            }

            // Validate ownership
            var isOwner = await _unitOfWork.BusinessLocations.IsOwnerOfLocationAsync(userId, product.BusinessLocationId);
            if (!isOwner)
            {
                throw new ForbiddenException(MessageKeys.Forbidden);
            }

            // Validate business history (imports, orders)
            var hasHistory = await _unitOfWork.Products.HasHistoryAsync(productId);
            
            if (hasHistory)
            {
                // Soft Delete product + cascade to SaleItems (RULE-PROD-10)
                product.DeletedAt = DateTime.UtcNow;
                foreach (var saleItem in product.SaleItems)
                {
                    saleItem.DeletedAt = DateTime.UtcNow;
                }
                _unitOfWork.Products.Update(product);
            }
            else
            {
                // Hard Delete if unused
                _unitOfWork.Products.Delete(product);
            }

            await _unitOfWork.SaveChangesAsync();
        }

        #endregion

        #region Private Helpers

        private static void ValidatePriceTiers(CreateProductRequest request)
        {
            if (request.PriceTiers == null || !request.PriceTiers.Any()) return;

            var duplicateUnitTier = request.PriceTiers.FirstOrDefault(t =>
                string.Equals(t.Unit?.Trim(), request.Unit?.Trim(), StringComparison.OrdinalIgnoreCase));

            if (duplicateUnitTier != null)
                throw new BadRequestException(MessageKeys.ProductDuplicateUnitInPriceTiers, null, duplicateUnitTier.Unit ?? "");
        }

        private async Task<List<string>?> CheckSkuDuplicateWarningAsync(int locationId, string? sku, long? excludeProductId = null)
        {
            if (string.IsNullOrWhiteSpace(sku)) return null;

            var existing = await _unitOfWork.Products.FindBySkuInLocationAsync(locationId, sku, excludeProductId);
            return existing == null ? null : new List<string> { MessageKeys.ProductDuplicateSku };
        }

        private async Task ValidateProductAccessAsync(Guid userId, int businessLocationId)
        {
            // Reusable access guard for Product endpoints.
            // Current rule: Owner/Employee can access, and employee is blocked when location is inactive (RULE-LOC-07).
            await _locationService.ValidateLocationAccessAsync(userId, businessLocationId);
        }

        private static void BuildSaleItems(Product product, CreateProductRequest request)
        {
            // Default SaleItem (base unit)
            var defaultSaleItem = new SaleItem { Unit = request.Unit, Quantity = 1, Product = product };
            defaultSaleItem.ProductPricePolicies.Add(new ProductPricePolicy
            {
                Price = request.SellingPrice, IsDefault = true, StartAt = DateTime.UtcNow, SaleItem = defaultSaleItem
            });
            product.SaleItems.Add(defaultSaleItem);

            // Additional PriceTiers
            if (request.PriceTiers == null) return;
            foreach (var tier in request.PriceTiers)
            {
                var saleItem = new SaleItem { Unit = tier.Unit, Quantity = tier.Quantity, Product = product };
                saleItem.ProductPricePolicies.Add(new ProductPricePolicy
                {
                    Price = tier.Price, IsDefault = true, StartAt = DateTime.UtcNow, SaleItem = saleItem
                });
                product.SaleItems.Add(saleItem);
            }
        }

        private static void ReconcileSaleItems(Product product, UpdateProductRequest request)
        {
            var expectedSaleItems = new List<(string Unit, int Quantity, decimal Price)>
            {
                (request.Unit, 1, request.SellingPrice)
            };
            if (request.PriceTiers != null)
                expectedSaleItems.AddRange(request.PriceTiers.Select(t => (t.Unit, t.Quantity, t.Price)));

            var existingSaleItems = product.SaleItems.ToList();
            var processedIds = new HashSet<long>();

            foreach (var (unit, quantity, price) in expectedSaleItems)
            {
                var existing = existingSaleItems.FirstOrDefault(s =>
                    string.Equals(s.Unit, unit, StringComparison.OrdinalIgnoreCase)
                    && s.Quantity == quantity
                    && !processedIds.Contains(s.SaleItemId));

                if (existing != null)
                {
                    processedIds.Add(existing.SaleItemId);
                    var defaultPolicy = existing.ProductPricePolicies.FirstOrDefault(pp => pp.IsDefault);
                    if (defaultPolicy == null)
                    {
                        existing.ProductPricePolicies.Add(new ProductPricePolicy
                            { Price = price, IsDefault = true, StartAt = DateTime.UtcNow });
                    }
                    else if (defaultPolicy.Price != price)
                    {
                        // Never update ProductPricePolicy directly: close old version and create a new one.
                        defaultPolicy.IsDefault = false;
                        defaultPolicy.EndAt = DateTime.UtcNow;

                        existing.ProductPricePolicies.Add(new ProductPricePolicy
                        {
                            Price = price,
                            IsDefault = true,
                            StartAt = DateTime.UtcNow
                        });
                    }
                }
                else
                {
                    var newItem = new SaleItem { Unit = unit, Quantity = quantity };
                    newItem.ProductPricePolicies.Add(new ProductPricePolicy
                        { Price = price, IsDefault = true, StartAt = DateTime.UtcNow, SaleItem = newItem });
                    product.SaleItems.Add(newItem);
                }
            }

            // Soft-delete removed items
            foreach (var item in existingSaleItems.Where(s => !processedIds.Contains(s.SaleItemId)))
                item.DeletedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Applies stock changes from current stock to target stock.
        /// Shared by Create/Update/Manual Adjust to keep stock behavior consistent.
        /// </summary>
        private async Task ApplyStockToTargetAsync(Product product, int targetStock, decimal costPriceForIncrease, string? note = null)
        {
            var stockDiff = targetStock - product.Stock;
            if (stockDiff == 0)
                return;

            // For decrease, ProductService updates stock directly.
            // For increase, ImportService applies increase + stock movement.
            if (stockDiff < 0)
                product.Stock = targetStock;

            await ApplyStockAdjustmentAsync(product, stockDiff, costPriceForIncrease, note);
        }

        /// <summary>
        /// Records stock change using one orchestrator:
        /// - Increase: ImportService owns stock apply + stock movement creation.
        /// - Decrease: ProductService creates stock movement directly.
        /// </summary>
        private async Task ApplyStockAdjustmentAsync(Product product, int stockDiff, decimal costPrice, string? note = null)
        {
            if (stockDiff > 0)
            {
                await _importService.CreateInventoryAdjustmentImportAsync(
                    product.BusinessLocationId,
                    product.ProductId,
                    stockDiff,
                    costPrice,
                    note);
            }
            else if (stockDiff < 0)
            {
                var movement = _stockMovementService.CreateStockMovement(
                    product,
                    stockDiff,
                    StockMovementReferenceType.Adjustment,
                    null,
                    note);
                product.StockMovements.Add(movement);
            }
        }

        #endregion
    }
}
