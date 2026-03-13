using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Exceptions;
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
        private readonly IMapper _mapper;

        public ProductService(
            IUnitOfWork unitOfWork,
            IImageService imageService,
            IBusinessLocationService locationService,
            IImportService importService,
            IStockMovementService stockMovementService,
            IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _imageService = imageService;
            _locationService = locationService;
            _importService = importService;
            _stockMovementService = stockMovementService;
            _mapper = mapper;
        }

        public async Task<PaginatedResponse<ProductListItemDto>> SearchProductsAsync(Guid userId, ProductQueryParams query)
        {
            // Validate access (owner or employee, blocks employee when inactive — RULE-LOC-07)
            await ValidateProductAccessAsync(userId, query.LocationId);

            var (products, totalCount) = await _unitOfWork.Products.SearchAsync(query);

            var items = products.Select(p => _mapper.Map<ProductListItemDto>(p)).ToList();

            // Use values with fallback (should be set by controller)
            var pageNumber = query.PageNumber ?? 1;
            var pageSize = query.PageSize ?? 10;

            return new PaginatedResponse<ProductListItemDto>(items, totalCount, pageNumber, pageSize);
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

        public async Task<(ProductListItemDto Product, List<string>? Warnings)> CreateProductAsync(Guid userId, CreateProductRequest request)
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

            // 4. Record initial stock (creates Import + StockMovement if stock > 0)
            if (request.Stock > 0)
            {
                await RecordStockChangeAsync(product, request.Stock, request.CostPrice);
                await _unitOfWork.SaveChangesAsync();
            }

            return (_mapper.Map<ProductListItemDto>(product), warnings);
        }

        public async Task<(ProductListItemDto Product, List<string>? Warnings)> UpdateProductAsync(Guid userId, long productId, UpdateProductRequest request)
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

            // 6. Handle stock change + Save
            var stockDiff = request.Stock - oldStock;
            if (stockDiff != 0)
                await RecordStockChangeAsync(product, stockDiff, request.CostPrice);

            _unitOfWork.Products.Update(product);
            await _unitOfWork.SaveChangesAsync();

            return (_mapper.Map<ProductListItemDto>(product), warnings);
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

        private static void ReconcileSaleItems(Product product, CreateProductRequest request)
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
        /// Records stock change: creates Import (for increase) + StockMovement.
        /// For increase: 1 internal SaveChanges needed to obtain ImportId.
        /// Caller must call SaveChangesAsync() after to persist remaining entities.
        /// </summary>
        private async Task RecordStockChangeAsync(Product product, int stockDiff, decimal costPrice)
        {
            if (stockDiff > 0)
            {
                var importId = await _importService.CreateInventoryAdjustmentImportAsync(
                    product.BusinessLocationId,
                    product.ProductId,
                    stockDiff,
                    costPrice);

                var movement = _stockMovementService.CreateStockMovement(
                    product,
                    StockMovementType.In,
                    stockDiff,
                    StockMovementReferenceType.Import,
                    importId);
                product.StockMovements.Add(movement);
            }
            else if (stockDiff < 0)
            {
                var movement = _stockMovementService.CreateStockMovement(
                    product,
                    StockMovementType.Out,
                    stockDiff,
                    StockMovementReferenceType.Adjustment,
                    null);
                product.StockMovements.Add(movement);
            }
        }

        #endregion
    }
}
