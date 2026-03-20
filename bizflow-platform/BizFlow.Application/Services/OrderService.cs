using AutoMapper;
using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Exceptions;
using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.Order;
using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Application.Interfaces.Services;
using BizFlow.Domain.Entities;
using BizFlow.Domain.Enums;

namespace BizFlow.Application.Services
{
    public class OrderService : IOrderService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly IBusinessLocationService _locationService;
        private readonly IStockMovementService _stockMovementService;
        private readonly IGeneralLedgerService _generalLedgerService;

        public OrderService(
            IUnitOfWork uow,
            IMapper mapper,
            IBusinessLocationService locationService,
            IStockMovementService stockMovementService,
            IGeneralLedgerService generalLedgerService)
        {
            _uow = uow;
            _mapper = mapper;
            _locationService = locationService;
            _stockMovementService = stockMovementService;
            _generalLedgerService = generalLedgerService;
        }

        public async Task<OrderActionResultDto> CreateAsync(Guid userId, CreateOrderRequest request)
        {
            await _locationService.ValidateOwnerAsync(userId, request.BusinessLocationId);

            var prepared = await PrepareOrderDraftAsync(
                userId,
                request.BusinessLocationId,
                request.DebtorId,
                request.Items,
                request.CashAmount,
                request.BankAmount,
                request.DebtAmount,
                request.ConfirmLowStock);

            if (prepared.RequiresConfirmation)
                return new OrderActionResultDto { RequiresConfirmation = true, Warnings = prepared.Warnings };

            var order = await _uow.ExecuteResilientAsync(async _ =>
            {
                var now = DateTime.UtcNow;
                var created = new Order
                {
                    OrderCode = GenerateOrderCode(),
                    DebtorId = request.DebtorId,
                    CustomerName = request.CustomerName?.Trim(),
                    CustomerPhone = request.CustomerPhone?.Trim(),
                    SubTotal = prepared.SubTotal,
                    Discount = prepared.Discount,
                    TotalAmount = prepared.TotalAmount,
                    CashAmount = request.CashAmount,
                    BankAmount = request.BankAmount,
                    DebtAmount = request.DebtAmount,
                    Status = OrderStatus.Pending,
                    BillMetadata = request.BillMetadata,
                    Note = request.Note?.Trim(),
                    CreatedBy = userId,
                    UpdatedBy = userId,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                await _uow.Orders.AddAsync(created);
                await _uow.SaveChangesAsync(); // Need OrderId before adding detail rows.

                foreach (var detail in prepared.Details)
                    detail.OrderId = created.OrderId;

                await _uow.OrderDetails.AddRangeAsync(prepared.Details);

                return created;
            });

            var created = await _uow.Orders.GetByIdWithDetailsAsync(order.OrderId) ?? order;
            return new OrderActionResultDto { Order = _mapper.Map<OrderDto>(created) };
        }

        public async Task<OrderActionResultDto> UpdateAsync(Guid userId, long orderId, UpdateOrderRequest request)
        {
            var order = await _uow.Orders.GetByIdWithDetailsAsync(orderId)
                ?? throw new NotFoundException(MessageKeys.NotFound);

            var locationId = ResolveOrderLocationId(order, request.BusinessLocationId);
            await _locationService.ValidateLocationAccessAsync(userId, locationId);

            var isOwner = await _uow.BusinessLocations.IsOwnerOfLocationAsync(userId, locationId);
            if (!isOwner && order.CreatedBy != userId)
                throw new ForbiddenException(MessageKeys.Forbidden);

            if (!order.Status.Equals(OrderStatus.Pending, StringComparison.OrdinalIgnoreCase))
                throw new BadRequestException(MessageKeys.BadRequest);

            var prepared = await PrepareOrderDraftAsync(
                userId,
                locationId,
                request.DebtorId,
                request.Items,
                request.CashAmount,
                request.BankAmount,
                request.DebtAmount,
                request.ConfirmLowStock);

            if (prepared.RequiresConfirmation)
                return new OrderActionResultDto { RequiresConfirmation = true, Warnings = prepared.Warnings };

            await _uow.ExecuteResilientAsync(async _ =>
            {
                order.DebtorId = request.DebtorId;
                order.CustomerName = request.CustomerName?.Trim();
                order.CustomerPhone = request.CustomerPhone?.Trim();
                order.SubTotal = prepared.SubTotal;
                order.Discount = prepared.Discount;
                order.TotalAmount = prepared.TotalAmount;
                order.CashAmount = request.CashAmount;
                order.BankAmount = request.BankAmount;
                order.DebtAmount = request.DebtAmount;
                order.BillMetadata = request.BillMetadata;
                order.Note = request.Note?.Trim();
                order.UpdatedBy = userId;
                order.UpdatedAt = DateTime.UtcNow;

                _uow.OrderDetails.RemoveRange(order.OrderDetails);

                foreach (var detail in prepared.Details)
                    detail.OrderId = order.OrderId;

                await _uow.OrderDetails.AddRangeAsync(prepared.Details);

                _uow.Orders.Update(order);
            });

            var updated = await _uow.Orders.GetByIdWithDetailsAsync(order.OrderId) ?? order;
            return new OrderActionResultDto { Order = _mapper.Map<OrderDto>(updated) };
        }

        public async Task<OrderActionResultDto> CompleteAsync(Guid userId, long orderId, CompleteOrderRequest request)
        {
            var order = await _uow.Orders.GetByIdWithDetailsAsync(orderId)
                ?? throw new NotFoundException(MessageKeys.NotFound);

            var locationId = ResolveOrderLocationId(order, null);
            await _locationService.ValidateOwnerAsync(userId, locationId);

            if (!order.Status.Equals(OrderStatus.Pending, StringComparison.OrdinalIgnoreCase))
                throw new BadRequestException(MessageKeys.BadRequest);

            var warnings = GetLowStockWarnings(order.OrderDetails);
            if (warnings.Any() && !request.ConfirmLowStock)
            {
                return new OrderActionResultDto
                {
                    RequiresConfirmation = true,
                    Warnings = warnings
                };
            }

            await _uow.ExecuteResilientAsync(async _ =>
            {
                foreach (var detail in order.OrderDetails)
                {
                    var product = detail.SaleItem.Product;
                    if (product.TrackInventory != true)
                        continue;

                    product.Stock -= detail.Quantity;

                    var movement = _stockMovementService.CreateStockMovement(
                        product,
                        -detail.Quantity,
                        StockMovementReferenceType.Order,
                        order.OrderId,
                        order.Note);

                    product.StockMovements.Add(movement);
                    _uow.Products.Update(product);
                }

                if (order.DebtorId.HasValue && order.DebtAmount > 0)
                {
                    var debtor = await _uow.Debtors.GetByIdAsync(order.DebtorId.Value)
                        ?? throw new NotFoundException(MessageKeys.DebtorNotFound);

                    debtor.CurrentBalance += order.DebtAmount;
                    debtor.UpdatedAt = DateTime.UtcNow;
                    _uow.Debtors.Update(debtor);
                }

                order.Status = OrderStatus.Completed;
                order.CompletedAt = DateTime.UtcNow;
                order.CompletedBy = userId;
                order.UpdatedAt = DateTime.UtcNow;
                order.UpdatedBy = userId;
                _uow.Orders.Update(order);

                var revenues = BuildSaleRevenuesFromOrder(order, locationId, userId);
                foreach (var revenue in revenues)
                    await _uow.Revenues.AddAsync(revenue);

                await _uow.SaveChangesAsync();

                foreach (var revenue in revenues)
                    await _generalLedgerService.RecordSaleRevenueAsync(revenue);

                await _uow.SaveChangesAsync();
            });

            var completed = await _uow.Orders.GetByIdWithDetailsAsync(order.OrderId) ?? order;
            return new OrderActionResultDto { Order = _mapper.Map<OrderDto>(completed) };
        }

        public async Task<OrderDto> CancelAsync(Guid userId, long orderId, CancelOrderRequest request)
        {
            var order = await _uow.Orders.GetByIdWithDetailsAsync(orderId)
                ?? throw new NotFoundException(MessageKeys.NotFound);

            var locationId = ResolveOrderLocationId(order, null);
            await _locationService.ValidateOwnerAsync(userId, locationId);

            if (order.Status.Equals(OrderStatus.Cancelled, StringComparison.OrdinalIgnoreCase))
                throw new BadRequestException(MessageKeys.BadRequest);

            await _uow.ExecuteResilientAsync(async _ =>
            {
                if (order.Status.Equals(OrderStatus.Completed, StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var detail in order.OrderDetails)
                    {
                        var product = detail.SaleItem.Product;
                        if (product.TrackInventory != true)
                            continue;

                        product.Stock += detail.Quantity;

                        var movement = _stockMovementService.CreateStockMovement(
                            product,
                            detail.Quantity,
                            StockMovementReferenceType.Order,
                            order.OrderId,
                            request.CancelReason);

                        product.StockMovements.Add(movement);
                        _uow.Products.Update(product);
                    }

                    if (order.DebtorId.HasValue && order.DebtAmount > 0)
                    {
                        var debtor = await _uow.Debtors.GetByIdAsync(order.DebtorId.Value)
                            ?? throw new NotFoundException(MessageKeys.DebtorNotFound);

                        debtor.CurrentBalance -= order.DebtAmount;
                        debtor.UpdatedAt = DateTime.UtcNow;
                        _uow.Debtors.Update(debtor);
                    }

                    var saleRevenues = await _uow.Revenues.GetSaleByOrderIdAsync(locationId, order.OrderId);
                    foreach (var revenue in saleRevenues)
                    {
                        revenue.DeletedAt = DateTime.UtcNow;
                        _uow.Revenues.Update(revenue);
                    }

                    foreach (var revenue in saleRevenues)
                        await _generalLedgerService.ReverseRevenueEntriesAsync(revenue, MessageKeys.OrderCancelledReversalReason);
                }

                order.Status = OrderStatus.Cancelled;
                order.CancelledAt = DateTime.UtcNow;
                order.CancelledBy = userId;
                order.CancelReason = request.CancelReason?.Trim();
                order.UpdatedAt = DateTime.UtcNow;
                order.UpdatedBy = userId;
                _uow.Orders.Update(order);

                await _uow.SaveChangesAsync();
            });

            var cancelled = await _uow.Orders.GetByIdWithDetailsAsync(order.OrderId) ?? order;
            return _mapper.Map<OrderDto>(cancelled);
        }

        public async Task<EditCompletedSaveResultDto> EditCompletedSaveAsync(Guid userId, long oldOrderId, UpdateOrderRequest request)
        {
            var oldOrder = await _uow.Orders.GetByIdWithDetailsAsync(oldOrderId)
                ?? throw new NotFoundException(MessageKeys.NotFound);

            var locationId = ResolveOrderLocationId(oldOrder, request.BusinessLocationId);
            await _locationService.ValidateOwnerAsync(userId, locationId);

            var idempotencyMarker = BuildEditCompletedIdempotencyMarker(request.IdempotencyKey);
            Order? replacement = null;
            if (idempotencyMarker != null)
            {
                replacement = await _uow.Orders.GetLatestReplacementByRefOrderIdAsync(oldOrderId, idempotencyMarker);
            }
            else
            {
                replacement = await _uow.Orders.GetLatestReplacementByRefOrderIdAsync(oldOrderId);
            }

            if (replacement != null)
            {
                if (!oldOrder.Status.Equals(OrderStatus.Cancelled, StringComparison.OrdinalIgnoreCase))
                {
                    await CancelAsync(userId, oldOrderId, new CancelOrderRequest
                    {
                        CancelReason = $"replaced by ORDER#{replacement.OrderId}"
                    });
                }

                return new EditCompletedSaveResultDto
                {
                    OldOrderId = oldOrderId,
                    OldOrderStatus = OrderStatus.Cancelled,
                    NewOrderId = replacement.OrderId,
                    NewOrderStatus = replacement.Status
                };
            }

            if (!oldOrder.Status.Equals(OrderStatus.Completed, StringComparison.OrdinalIgnoreCase))
                throw new BadRequestException(MessageKeys.BadRequest);

            var prepared = await PrepareOrderDraftAsync(
                userId,
                locationId,
                request.DebtorId,
                request.Items,
                request.CashAmount,
                request.BankAmount,
                request.DebtAmount,
                request.ConfirmLowStock);

            if (prepared.RequiresConfirmation)
                throw new BadRequestException(MessageKeys.BadRequest);

            var newOrderId = await _uow.ExecuteResilientAsync(async _ =>
            {
                var now = DateTime.UtcNow;
                var newOrder = new Order
                {
                    OrderCode = GenerateOrderCode(),
                    RefOrderId = oldOrderId,
                    DebtorId = request.DebtorId,
                    CustomerName = request.CustomerName?.Trim(),
                    CustomerPhone = request.CustomerPhone?.Trim(),
                    SubTotal = prepared.SubTotal,
                    Discount = prepared.Discount,
                    TotalAmount = prepared.TotalAmount,
                    CashAmount = request.CashAmount,
                    BankAmount = request.BankAmount,
                    DebtAmount = request.DebtAmount,
                    Status = OrderStatus.Pending,
                    BillMetadata = request.BillMetadata,
                    Note = request.Note?.Trim(),
                    CreatedBy = userId,
                    UpdatedBy = userId,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                if (!string.IsNullOrWhiteSpace(idempotencyMarker))
                {
                    newOrder.BillMetadata = string.IsNullOrWhiteSpace(newOrder.BillMetadata)
                        ? idempotencyMarker
                        : $"{newOrder.BillMetadata};{idempotencyMarker}";
                }

                await _uow.Orders.AddAsync(newOrder);
                await _uow.SaveChangesAsync(); // Need OrderId for detail rows and replacement link.

                foreach (var detail in prepared.Details)
                    detail.OrderId = newOrder.OrderId;

                await _uow.OrderDetails.AddRangeAsync(prepared.Details);

                // Complete new replacement order.
                foreach (var detail in prepared.Details)
                {
                    var product = detail.SaleItem.Product;
                    if (product.TrackInventory != true)
                        continue;

                    product.Stock -= detail.Quantity;
                    var movement = _stockMovementService.CreateStockMovement(
                        product,
                        -detail.Quantity,
                        StockMovementReferenceType.Order,
                        newOrder.OrderId,
                        newOrder.Note);
                    product.StockMovements.Add(movement);
                    _uow.Products.Update(product);
                }

                if (newOrder.DebtorId.HasValue && newOrder.DebtAmount > 0)
                {
                    var newDebtor = await _uow.Debtors.GetByIdAsync(newOrder.DebtorId.Value)
                        ?? throw new NotFoundException(MessageKeys.DebtorNotFound);

                    newDebtor.CurrentBalance += newOrder.DebtAmount;
                    newDebtor.UpdatedAt = DateTime.UtcNow;
                    _uow.Debtors.Update(newDebtor);
                }

                newOrder.Status = OrderStatus.Completed;
                newOrder.CompletedAt = DateTime.UtcNow;
                newOrder.CompletedBy = userId;
                newOrder.UpdatedAt = DateTime.UtcNow;
                newOrder.UpdatedBy = userId;
                _uow.Orders.Update(newOrder);

                var newOrderRevenues = BuildSaleRevenuesFromOrder(newOrder, locationId, userId);
                foreach (var revenue in newOrderRevenues)
                    await _uow.Revenues.AddAsync(revenue);

                await _uow.SaveChangesAsync(); // Need RevenueId before GL reference entries.

                foreach (var revenue in newOrderRevenues)
                    await _generalLedgerService.RecordSaleRevenueAsync(revenue);

                // Cancel old completed order after replacement order is completed.
                foreach (var detail in oldOrder.OrderDetails)
                {
                    var product = detail.SaleItem.Product;
                    if (product.TrackInventory != true)
                        continue;

                    product.Stock += detail.Quantity;
                    var movement = _stockMovementService.CreateStockMovement(
                        product,
                        detail.Quantity,
                        StockMovementReferenceType.Order,
                        oldOrder.OrderId,
                        $"replaced by ORDER#{newOrder.OrderId}");
                    product.StockMovements.Add(movement);
                    _uow.Products.Update(product);
                }

                if (oldOrder.DebtorId.HasValue && oldOrder.DebtAmount > 0)
                {
                    var oldDebtor = await _uow.Debtors.GetByIdAsync(oldOrder.DebtorId.Value)
                        ?? throw new NotFoundException(MessageKeys.DebtorNotFound);

                    oldDebtor.CurrentBalance -= oldOrder.DebtAmount;
                    oldDebtor.UpdatedAt = DateTime.UtcNow;
                    _uow.Debtors.Update(oldDebtor);
                }

                var oldSaleRevenues = await _uow.Revenues.GetSaleByOrderIdAsync(locationId, oldOrder.OrderId);
                foreach (var revenue in oldSaleRevenues)
                {
                    revenue.DeletedAt = DateTime.UtcNow;
                    _uow.Revenues.Update(revenue);
                }

                foreach (var revenue in oldSaleRevenues)
                    await _generalLedgerService.ReverseRevenueEntriesAsync(revenue, MessageKeys.OrderCancelledReversalReason);

                oldOrder.Status = OrderStatus.Cancelled;
                oldOrder.CancelledAt = DateTime.UtcNow;
                oldOrder.CancelledBy = userId;
                oldOrder.CancelReason = $"replaced by ORDER#{newOrder.OrderId}";
                oldOrder.UpdatedAt = DateTime.UtcNow;
                oldOrder.UpdatedBy = userId;
                _uow.Orders.Update(oldOrder);

                return newOrder.OrderId;
            });

            return new EditCompletedSaveResultDto
            {
                OldOrderId = oldOrderId,
                OldOrderStatus = OrderStatus.Cancelled,
                NewOrderId = newOrderId,
                NewOrderStatus = OrderStatus.Completed
            };
        }

        public async Task<OrderDto> GetDetailAsync(Guid userId, long orderId)
        {
            var order = await _uow.Orders.GetByIdWithDetailsAsync(orderId)
                ?? throw new NotFoundException(MessageKeys.NotFound);

            var locationId = ResolveOrderLocationId(order, null);
            await _locationService.ValidateOwnerAsync(userId, locationId);

            return _mapper.Map<OrderDto>(order);
        }

        public async Task<PaginatedResponse<OrderDto>> ListAsync(Guid userId, OrderQueryParams query)
        {
            await _locationService.ValidateOwnerAsync(userId, query.BusinessLocationId);

            if (!string.IsNullOrWhiteSpace(query.Status)
                && !OrderStatus.IsValid(query.Status.Trim()))
                throw new BadRequestException(MessageKeys.BadRequest);

            if (!string.IsNullOrWhiteSpace(query.Status))
                query.Status = query.Status.Trim().ToLowerInvariant();

            var (items, totalCount) = await _uow.Orders.SearchAsync(query);
            var dtos = _mapper.Map<List<OrderDto>>(items);

            var pageNumber = query.PageNumber ?? 1;
            var pageSize = query.PageSize ?? 20;
            return new PaginatedResponse<OrderDto>(dtos, totalCount, pageNumber, pageSize);
        }

        private static string GenerateOrderCode()
            => $"ORD-{DateTime.UtcNow:yyyyMMddHHmmssfff}";

        private int ResolveOrderLocationId(Order order, int? preferredLocationId)
        {
            if (preferredLocationId.HasValue)
                return preferredLocationId.Value;

            var locationIds = order.OrderDetails
                .Select(od => od.SaleItem.Product.BusinessLocationId)
                .Distinct()
                .ToList();

            if (locationIds.Count == 1)
                return locationIds[0];

            if (order.Debtor != null)
                return order.Debtor.BusinessLocationId;

            throw new BadRequestException(MessageKeys.BadRequest);
        }

        private static List<string> GetLowStockWarnings(IEnumerable<OrderDetail> orderDetails)
        {
            var warnings = new List<string>();
            foreach (var detail in orderDetails)
            {
                var product = detail.SaleItem.Product;
                if (product.TrackInventory == true && detail.Quantity > product.Stock)
                {
                    warnings.Add(MessageKeys.LowStockConfirmRequired);
                }
            }

            return warnings;
        }

        private async Task<(bool RequiresConfirmation, List<string> Warnings, List<OrderDetail> Details, decimal SubTotal, decimal Discount, decimal TotalAmount)>
            PrepareOrderDraftAsync(
                Guid userId,
                int businessLocationId,
                long? debtorId,
                List<OrderItemRequest> itemRequests,
                decimal cashAmount,
                decimal bankAmount,
                decimal debtAmount,
                bool confirmLowStock)
        {
            if (itemRequests == null || itemRequests.Count == 0)
                throw new BadRequestException(MessageKeys.BadRequest);

            var saleItemIds = itemRequests.Select(i => i.SaleItemId).Distinct().ToList();
            var saleItems = await _uow.Products.GetSaleItemsForPriceAdjustAsync(saleItemIds);
            if (saleItems.Count != saleItemIds.Count)
                throw new NotFoundException(MessageKeys.NotFound);

            if (saleItems.Any(si => si.Product.BusinessLocationId != businessLocationId))
                throw new ForbiddenException(MessageKeys.Forbidden);

            if (debtorId.HasValue)
            {
                var debtor = await _uow.Debtors.GetByIdAsync(debtorId.Value)
                    ?? throw new NotFoundException(MessageKeys.DebtorNotFound);

                if (debtor.BusinessLocationId != businessLocationId)
                    throw new ForbiddenException(MessageKeys.Forbidden);
            }

            if (debtAmount > 0 && !debtorId.HasValue)
                throw new BadRequestException(MessageKeys.BadRequest);

            var details = new List<OrderDetail>();
            decimal subTotal = 0;
            decimal discount = 0;
            decimal total = 0;
            var warnings = new List<string>();

            foreach (var item in itemRequests)
            {
                var saleItem = saleItems.First(si => si.SaleItemId == item.SaleItemId);

                var lineSubTotal = item.Quantity * item.UnitPrice;
                var lineAmount = lineSubTotal - item.Discount;
                if (lineAmount < 0)
                    throw new BadRequestException(MessageKeys.BadRequest);

                subTotal += lineSubTotal;
                discount += item.Discount;
                total += lineAmount;

                if (saleItem.Product.TrackInventory == true && item.Quantity > saleItem.Product.Stock)
                {
                    warnings.Add(MessageKeys.LowStockConfirmRequired);
                }

                details.Add(new OrderDetail
                {
                    SaleItemId = item.SaleItemId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    Discount = item.Discount,
                    Amount = lineAmount,
                    CreatedAt = DateTime.UtcNow,
                    SaleItem = saleItem
                });
            }

            if (warnings.Any() && !confirmLowStock)
            {
                return (true, warnings, new List<OrderDetail>(), 0, 0, 0);
            }

            if (cashAmount + bankAmount + debtAmount != total)
                throw new BadRequestException(MessageKeys.BadRequest);

            return (false, warnings, details, subTotal, discount, total);
        }

        private static List<Revenue> BuildSaleRevenuesFromOrder(Order order, int businessLocationId, Guid userId)
        {
            var revenues = new List<Revenue>();
            var date = DateOnly.FromDateTime(order.CompletedAt ?? DateTime.UtcNow);
            var marker = $"ORDER#{order.OrderId}";

            if (order.CashAmount > 0)
            {
                revenues.Add(new Revenue
                {
                    BusinessLocationId = businessLocationId,
                    RevenueType = RevenueType.Sale,
                    Amount = order.CashAmount,
                    RevenueDate = date,
                    Description = $"{marker} cash",
                    MoneyChannel = PaymentMethods.Cash,
                    CreatedBy = userId,
                    CreatedAt = DateTime.UtcNow
                });
            }

            if (order.BankAmount > 0)
            {
                revenues.Add(new Revenue
                {
                    BusinessLocationId = businessLocationId,
                    RevenueType = RevenueType.Sale,
                    Amount = order.BankAmount,
                    RevenueDate = date,
                    Description = $"{marker} bank",
                    MoneyChannel = PaymentMethods.Bank,
                    CreatedBy = userId,
                    CreatedAt = DateTime.UtcNow
                });
            }

            if (order.DebtAmount > 0)
            {
                revenues.Add(new Revenue
                {
                    BusinessLocationId = businessLocationId,
                    RevenueType = RevenueType.Sale,
                    Amount = order.DebtAmount,
                    RevenueDate = date,
                    Description = $"{marker} debt",
                    MoneyChannel = "debt",
                    CreatedBy = userId,
                    CreatedAt = DateTime.UtcNow
                });
            }

            return revenues;
        }

        private static string? BuildEditCompletedIdempotencyMarker(string? idempotencyKey)
        {
            if (string.IsNullOrWhiteSpace(idempotencyKey))
                return null;

            return $"IDEMPOTENCY#{idempotencyKey.Trim()}";
        }
    }
}
