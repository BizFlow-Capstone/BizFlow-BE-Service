using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Exceptions;
using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.Product;
using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Application.Interfaces.Services;
using BizFlow.Domain.Entities;

namespace BizFlow.Application.Services
{
    public class ProductService : IProductService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICloudinaryService _cloudinaryService;

        public ProductService(IUnitOfWork unitOfWork, ICloudinaryService cloudinaryService)
        {
            _unitOfWork = unitOfWork;
            _cloudinaryService = cloudinaryService;
        }

        public async Task<PaginatedResponse<ProductListItemDto>> SearchProductsAsync(Guid userId, ProductQueryParams query)
        {
            // Validate ownership
            var isOwner = await _unitOfWork.BusinessLocations.IsOwnerOfLocationAsync(userId, query.LocationId);
            if (!isOwner)
            {
                throw new ForbiddenException(MessageKeys.LocationAccessDenied);
            }

            var (products, totalCount) = await _unitOfWork.Products.SearchAsync(query);

            var items = products.Select(MapToListItemDto);

            // Use values with fallback (should be set by controller)
            var pageNumber = query.PageNumber ?? 1;
            var pageSize = query.PageSize ?? 10;

            return new PaginatedResponse<ProductListItemDto>(items, totalCount, pageNumber, pageSize);
        }

        public async Task<ProductSaleItemsResponseDto?> GetProductSaleItemsAsync(Guid userId, long productId)
        {
            var product = await _unitOfWork.Products.GetByIdWithSaleItemsAsync(productId);
            if (product == null)
            {
                throw new NotFoundException(MessageKeys.ProductNotFound);
            }

            // Validate ownership
            var isOwner = await _unitOfWork.BusinessLocations.IsOwnerOfLocationAsync(userId, product.BusinessLocationId);
            if (!isOwner)
            {
                throw new ForbiddenException(MessageKeys.LocationAccessDenied);
            }

            return new ProductSaleItemsResponseDto
            {
                ProductId = product.ProductId,
                SaleItems = product.SaleItems.Select(s => new SaleItemDto
                {
                    SaleItemId = s.SaleItemId,
                    Unit = s.Unit,
                    Quantity = s.Quantity,
                    Price = s.ProductPricePolicies.FirstOrDefault(pp => pp.IsDefault)?.Price ?? 0
                }).ToList()
            };
        }

        public async Task<ProductListItemDto> CreateProductAsync(Guid userId, CreateProductRequest request)
        {
            // 1. Validate ownership
            var isOwner = await _unitOfWork.BusinessLocations.IsOwnerOfLocationAsync(userId, request.LocationId);
            if (!isOwner)
            {
                throw new ForbiddenException(MessageKeys.LocationAccessDenied);
            }

            // 2. Validate PriceTiers (fail fast)
            if (request.PriceTiers != null && request.PriceTiers.Any())
            {
                var duplicateUnitTier = request.PriceTiers.FirstOrDefault(t => 
                    string.Equals(t.Unit?.Trim(), request.Unit?.Trim(), StringComparison.OrdinalIgnoreCase));
                
                if (duplicateUnitTier != null)
                {
                    // Pass null for errors, and the unit name as a message argument
                    throw new BadRequestException(MessageKeys.ProductDuplicateUnitInPriceTiers, null, duplicateUnitTier.Unit ?? "");
                }
            }

            // 3. Build Product Entity Graph
            var product = new Product
            {
                BusinessLocationId = request.LocationId,
                BusinessTypeId = request.BusinessTypeId,
                ProductName = request.ProductName,
                Sku = request.Sku,
                TrackInventory = request.TrackInventory,
                Unit = request.Unit,
                CostPrice = request.CostPrice,
                Stock = request.Stock,
                Manufacturer = request.Manufacturer,
                Status = "active",
                DeletedAt = null
            };

            // 3.1 Create Default Sale Item & Price Policy
            var defaultSaleItem = new SaleItem
            {
                Unit = request.Unit,
                Quantity = 1,
                Product = product // Navigation property check
            };
            
            var defaultPricePolicy = new ProductPricePolicy
            {
                Price = request.CostPrice,
                IsDefault = true,
                StartAt = DateTime.UtcNow,
                SaleItem = defaultSaleItem
            };
            
            defaultSaleItem.ProductPricePolicies.Add(defaultPricePolicy);
            product.SaleItems.Add(defaultSaleItem);

            // 3.2 Create Additional Sale Items from PriceTiers
            if (request.PriceTiers != null)
            {
                foreach (var tier in request.PriceTiers)
                {
                    var saleItem = new SaleItem
                    {
                        Unit = tier.Unit,
                        Quantity = tier.Quantity,
                        Product = product
                    };

                    var pricePolicy = new ProductPricePolicy
                    {
                        Price = tier.Price,
                        IsDefault = true,
                        StartAt = DateTime.UtcNow,
                        SaleItem = saleItem
                    };

                    saleItem.ProductPricePolicies.Add(pricePolicy);
                    product.SaleItems.Add(saleItem);
                }
            }

            if (request.ImageStream != null)
            {
                var uploadResult = await _cloudinaryService.UploadImageAsync(request.ImageStream, request.ImageFileName ?? "image", "Products");
                
                if (!uploadResult.Success)
                {
                    throw new BadRequestException(MessageKeys.ProductImageUploadFailed, null, uploadResult.Error ?? "Unknown error");
                }

                product.ImageUrl = uploadResult.Url;
                product.ImagePublicId = uploadResult.PublicId;
            }

            // 5. Save to Database (Single Transaction)
            try 
            {
                await _unitOfWork.Products.AddAsync(product);
                await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception)
            {
                // Rollback: Delete image from Cloudinary if DB save fails
                if (!string.IsNullOrEmpty(product.ImagePublicId))
                {
                    await _cloudinaryService.DeleteImageAsync(product.ImagePublicId);
                }
                throw; 
            }

            return MapToListItemDto(product);
        }

        public async Task<ProductListItemDto> UpdateProductAsync(Guid userId, long productId, UpdateProductRequest request)
        {
            var product = await _unitOfWork.Products.GetByIdWithSaleItemsAsync(productId);
            if (product == null)
            {
                throw new NotFoundException(MessageKeys.ProductNotFound);
            }

            // 1. Validate ownership
            var isOwner = await _unitOfWork.BusinessLocations.IsOwnerOfLocationAsync(userId, product.BusinessLocationId);
            if (!isOwner)
            {
                throw new ForbiddenException(MessageKeys.LocationAccessDenied);
            }
            
            // 2. Prevent changing product location
            if (product.BusinessLocationId != request.LocationId)
            {
                throw new BadRequestException(MessageKeys.ProductCannotChangeLocation);
            }

            // 2. Validate PriceTiers (fail fast)
            if (request.PriceTiers != null && request.PriceTiers.Any())
            {
                var duplicateUnitTier = request.PriceTiers.FirstOrDefault(t => 
                    string.Equals(t.Unit?.Trim(), request.Unit?.Trim(), StringComparison.OrdinalIgnoreCase));
                
                if (duplicateUnitTier != null)
                {
                    throw new BadRequestException(MessageKeys.ProductDuplicateUnitInPriceTiers, null, duplicateUnitTier.Unit ?? "");
                }
            }

            // 4. Update Product Properties
            product.BusinessTypeId = request.BusinessTypeId;
            product.ProductName = request.ProductName;
            product.Sku = request.Sku;
            product.TrackInventory = request.TrackInventory;
            product.Unit = request.Unit;
            product.CostPrice = request.CostPrice;
            product.Stock = request.Stock;
            product.Manufacturer = request.Manufacturer;
            // Status not updated here, use dedicated API

            // 5. Update Image
            // Handle image removal
            if (request.RemoveImage && !string.IsNullOrEmpty(product.ImagePublicId))
            {
                await _cloudinaryService.DeleteImageAsync(product.ImagePublicId);
                product.ImageUrl = null;
                product.ImagePublicId = null;
            }
            
            // Handle new image upload (if provided)
            if (request.ImageStream != null)
            {
                // Delete old image if exists
                if (!string.IsNullOrEmpty(product.ImagePublicId))
                {
                    await _cloudinaryService.DeleteImageAsync(product.ImagePublicId);
                }

                var uploadResult = await _cloudinaryService.UploadImageAsync(request.ImageStream, request.ImageFileName ?? "image", "Products");
                
                if (!uploadResult.Success)
                {
                    throw new BadRequestException(MessageKeys.ProductImageUploadFailed, null, uploadResult.Error ?? "Unknown error");
                }

                product.ImageUrl = uploadResult.Url;
                product.ImagePublicId = uploadResult.PublicId;
            }

            // 6. Smart Update for SaleItems
            // Build expected sale items from request
            var expectedSaleItems = new List<(string Unit, int Quantity, decimal Price)>
            {
                // Default sale item (base unit)
                (request.Unit, 1, request.CostPrice)
            };
            
            // Add price tiers
            if (request.PriceTiers != null)
            {
                expectedSaleItems.AddRange(request.PriceTiers.Select(t => (t.Unit, t.Quantity, t.Price)));
            }

            // Get existing sale items
            var existingSaleItems = product.SaleItems.ToList();

            // Track which existing items to keep
            var processedExistingItemIds = new HashSet<long>();

            // Update or add sale items
            foreach (var (unit, quantity, price) in expectedSaleItems)
            {
                // Try to find matching existing sale item (same unit AND quantity)
                var existingItem = existingSaleItems.FirstOrDefault(s => 
                    string.Equals(s.Unit, unit, StringComparison.OrdinalIgnoreCase) && 
                    s.Quantity == quantity &&
                    !processedExistingItemIds.Contains(s.SaleItemId));

                if (existingItem != null)
                {
                    // UPDATE: Found matching sale item, update its price policy
                    processedExistingItemIds.Add(existingItem.SaleItemId);
                    
                    var defaultPolicy = existingItem.ProductPricePolicies.FirstOrDefault(pp => pp.IsDefault);
                    if (defaultPolicy != null)
                    {
                        // Update existing policy price
                        defaultPolicy.Price = price;
                    }
                    else
                    {
                        // Create new default policy if somehow missing
                        existingItem.ProductPricePolicies.Add(new ProductPricePolicy
                        {
                            Price = price,
                            IsDefault = true,
                            StartAt = DateTime.UtcNow
                        });
                    }
                }
                else
                {
                    // ADD: No matching sale item found, create new one
                    var newSaleItem = new SaleItem
                    {
                        Unit = unit,
                        Quantity = quantity,
                    };

                    var pricePolicy = new ProductPricePolicy
                    {
                        Price = price,
                        IsDefault = true,
                        StartAt = DateTime.UtcNow,
                        SaleItem = newSaleItem
                    };

                    newSaleItem.ProductPricePolicies.Add(pricePolicy);
                    product.SaleItems.Add(newSaleItem);
                }
            }

            // REMOVE: Delete sale items that are no longer in the request
            var itemsToRemove = existingSaleItems
                .Where(s => !processedExistingItemIds.Contains(s.SaleItemId))
                .ToList();

            foreach (var itemToRemove in itemsToRemove)
            {
                product.SaleItems.Remove(itemToRemove);
            }

            // 7. Save
            // Note: If validation fails DB constraint (foreign keys), it will throw.
            try 
            {
                _unitOfWork.Products.Update(product);
                await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception)
            {
                // If update fails, and we uploaded a new image, we should probably delete it?
                // But we don't track the "newly uploaded" id easily here if we overwrote the property.
                // For now, standard transaction rollback by UnitOfWork will happen for DB.
                // Image cleanup is harder.
                throw; 
            }

            return MapToListItemDto(product);
        }
        public async Task<bool> UpdateProductStatusAsync(Guid userId, long productId, string status)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(productId);
            if (product == null)
            {
                throw new NotFoundException(MessageKeys.ProductNotFound);
            }

            // Validate ownership
            var isOwner = await _unitOfWork.BusinessLocations.IsOwnerOfLocationAsync(userId, product.BusinessLocationId);
            if (!isOwner)
            {
                return false;
            }

            product.Status = status.ToLower();
            _unitOfWork.Products.Update(product);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteProductAsync(Guid userId, long productId)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(productId);
            if (product == null)
            {
                throw new NotFoundException(MessageKeys.ProductNotFound);
            }

            // Validate ownership
            var isOwner = await _unitOfWork.BusinessLocations.IsOwnerOfLocationAsync(userId, product.BusinessLocationId);
            if (!isOwner)
            {
                return false;
            }

            // TODO: Check if product can be deleted (no sale history, no imports, etc.)
            // For now, soft delete the product
            product.DeletedAt = DateTime.UtcNow;
            _unitOfWork.Products.Update(product);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        #region Private Helpers

        private ProductListItemDto MapToListItemDto(Product product)
        {
            return new ProductListItemDto
            {
                ProductId = product.ProductId,
                Name = product.ProductName,
                Sku = product.Sku,
                Price = GetDefaultPrice(product),
                TrackInventory = product.TrackInventory ?? true,
                Stock = (product.TrackInventory ?? true) ? product.Stock : null,
                Status = product.Status
            };
        }

        /// <summary>
        /// Gets default price from sale item with unit matching product's base unit
        /// </summary>
        private static decimal GetDefaultPrice(Product product)
        {
            // Find sale item where unit matches product's base unit
            var matchingSaleItem = product.SaleItems
                .FirstOrDefault(s => string.Equals(s.Unit, product.Unit, StringComparison.OrdinalIgnoreCase));

            if (matchingSaleItem == null) return 0;

            // Get the default price policy for this sale item
            var defaultPolicy = matchingSaleItem.ProductPricePolicies
                .FirstOrDefault(pp => pp.IsDefault);

            return defaultPolicy?.Price ?? 0;
        }

        #endregion
    }
}

