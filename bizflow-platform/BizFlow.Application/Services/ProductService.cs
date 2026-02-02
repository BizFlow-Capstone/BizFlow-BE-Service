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

        public ProductService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<PaginatedResponse<ProductListItemDto>> GetProductsAsync(
            Guid userId, int locationId, int pageNumber, int pageSize)
        {
            // Validate ownership
            var isOwner = await _unitOfWork.BusinessLocations.IsOwnerOfLocationAsync(userId, locationId);
            if (!isOwner)
            {
                throw new ForbiddenException(MessageKeys.LocationAccessDenied);
            }

            var (products, totalCount) = await _unitOfWork.Products.GetByLocationIdAsync(
                locationId, pageNumber, pageSize);

            var items = products.Select(p => new ProductListItemDto
            {
                ProductId = p.ProductId,
                Name = p.ProductName,
                Sku = p.Sku,
                Price = GetDefaultPrice(p),
                TrackInventory = p.TrackInventory ?? true,
                Stock = (p.TrackInventory ?? true) ? p.Stock : null,
                Status = p.Status
            });

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
            // Validate ownership
            var isOwner = await _unitOfWork.BusinessLocations.IsOwnerOfLocationAsync(userId, request.LocationId);
            if (!isOwner)
            {
                throw new ForbiddenException(MessageKeys.LocationAccessDenied);
            }

            var product = new Product
            {
                BusinessLocationId = request.LocationId,
                BusinessTypeId = request.BusinessTypeId,
                ProductName = request.Name,
                Sku = request.Sku,
                TrackInventory = request.TrackInventory,
                Unit = request.Unit,
                CostPrice = request.CostPrice,
                Stock = request.Stock,
                ImageUrl = request.ImageUrl,
                Manufacturer = request.Manufacturer,
                Status = "active",
                IsDeleted = false
            };

            await _unitOfWork.Products.AddAsync(product);
            await _unitOfWork.SaveChangesAsync();

            // Add sale items (price tiers)
            decimal? defaultPrice = null;
            foreach (var tier in request.PriceTiers)
            {
                var saleItem = new SaleItem
                {
                    ProductId = product.ProductId,
                    Unit = tier.Unit,
                    Quantity = tier.Quantity
                };
                await _unitOfWork.Products.AddSaleItemAsync(saleItem);
                await _unitOfWork.SaveChangesAsync();

                // Each sale item has its own price policy, IsDefault = true means this is the current active price
                var pricePolicy = new ProductPricePolicy
                {
                    SaleItemId = saleItem.SaleItemId,
                    Price = tier.Price,
                    IsDefault = true, 
                    StartAt = DateTime.UtcNow
                };
                await _unitOfWork.Products.AddPricePolicyAsync(pricePolicy);

                // Default price is the price of sale item matching product's base unit
                if (!defaultPrice.HasValue && string.Equals(tier.Unit, request.Unit, StringComparison.OrdinalIgnoreCase))
                {
                    defaultPrice = tier.Price;
                }
            }
            await _unitOfWork.SaveChangesAsync();

            return new ProductListItemDto
            {
                ProductId = product.ProductId,
                Name = product.ProductName,
                Sku = product.Sku,
                Price = defaultPrice.HasValue ? defaultPrice.Value : 0,
                TrackInventory = product.TrackInventory ?? true,
                Stock = (product.TrackInventory ?? true) ? product.Stock : null,
                Status = product.Status
            };
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

        #region Private Helpers

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
