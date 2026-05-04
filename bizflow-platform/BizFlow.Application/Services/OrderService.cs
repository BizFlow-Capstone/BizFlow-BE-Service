using AutoMapper;
using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Exceptions;
using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.Order;
using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Application.Interfaces.Services;
using BizFlow.Domain.Entities;
using BizFlow.Domain.Enums;
using System.Text.Json;
using System.Text.Json.Nodes;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.Common.Helpers;

namespace BizFlow.Application.Services
{
    public class OrderService : IOrderService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly IBusinessLocationService _locationService;
        private readonly IStockMovementService _stockMovementService;
        private readonly IRevenueService _revenueService;
        private readonly IDebtorService _debtorService;
        private readonly IMessageService _messageService;
        private readonly IBackgroundJobScheduler _backgroundJobScheduler;
        private readonly IReferenceLabelService _labels;
        private readonly IDocumentNumberRegistryService _documentNumberRegistry;

        public OrderService(
            IUnitOfWork uow,
            IMapper mapper,
            IBusinessLocationService locationService,
            IStockMovementService stockMovementService,
            IRevenueService revenueService,
            IDebtorService debtorService,
            IMessageService messageService,
            IBackgroundJobScheduler backgroundJobScheduler,
            IReferenceLabelService labels,
            IDocumentNumberRegistryService documentNumberRegistry)
        {
            _uow = uow;
            _mapper = mapper;
            _locationService = locationService;
            _stockMovementService = stockMovementService;
            _revenueService = revenueService;
            _debtorService = debtorService;
            _messageService = messageService;
            _backgroundJobScheduler = backgroundJobScheduler;
            _labels = labels;
            _documentNumberRegistry = documentNumberRegistry;
        }

        public async Task<OrderActionResultDto> CreateAsync(Guid userId, CreateOrderRequest request)
        {
            await _locationService.ValidateLocationAccessAsync(userId, request.BusinessLocationId);

            var prepared = await PrepareOrderDraftAsync(
                userId,
                request.BusinessLocationId,
                request.DebtorId,
                request.Items,
                request.CashAmount,
                request.BankAmount,
                request.DebtAmount,
                request.ConfirmLowStock,
                request.ConfirmCreditLimitExceeded);

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
                    BillMetadata = NormalizeBillMetadataWithDocumentInfo(request.BillMetadata, request.DocumentNumber, request.DocumentDate),
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
            var createdDto = await MapOrderWithCreatorAsync(created);
            return new OrderActionResultDto { Order = createdDto };
        }

        public async Task<OrderActionResultDto> UpdateAsync(Guid userId, long orderId, UpdateOrderRequest request)
        {
            var order = await _uow.Orders.GetByIdWithDetailsAsync(orderId)
                ?? throw new NotFoundException(MessageKeys.NotFound);

            var locationId = ResolveOrderLocationId(order, request.BusinessLocationId);
            await _locationService.ValidateLocationAccessAsync(userId, locationId);

            var isOwner = await _uow.BusinessLocations.IsOwnerOfLocationAsync(userId, locationId);
            if (!isOwner && (order.CreatedBy == null || order.CreatedBy != userId))
                throw new ForbiddenException(MessageKeys.Forbidden);

            if (order.Status.Equals(OrderStatus.Pending, StringComparison.OrdinalIgnoreCase))
            {
                var prepared = await PrepareOrderDraftAsync(
                    userId,
                    locationId,
                    request.DebtorId,
                    request.Items,
                    request.CashAmount,
                    request.BankAmount,
                    request.DebtAmount,
                    request.ConfirmLowStock,
                    request.ConfirmCreditLimitExceeded);

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
                    order.BillMetadata = NormalizeBillMetadataWithDocumentInfo(request.BillMetadata, request.DocumentNumber, request.DocumentDate);
                    order.Note = request.Note?.Trim();
                    order.UpdatedBy = userId;
                    order.UpdatedAt = DateTime.UtcNow;

                    _uow.OrderDetails.RemoveRange(order.OrderDetails);

                    foreach (var detail in prepared.Details)
                        detail.OrderId = order.OrderId;

                    await _uow.OrderDetails.AddRangeAsync(prepared.Details);

                    _uow.Orders.Update(order);
                });

                var updatedPending = await _uow.Orders.GetByIdWithDetailsAsync(order.OrderId) ?? order;
                var updatedPendingDto = await MapOrderWithCreatorAsync(updatedPending);
                return new OrderActionResultDto { Order = updatedPendingDto };
            }

            if (order.Status.Equals(OrderStatus.Completed, StringComparison.OrdinalIgnoreCase))
            {
                var editResult = await EditCompletedSaveAsync(userId, orderId, request);
                var replacement = await _uow.Orders.GetByIdWithDetailsAsync(editResult.NewOrderId)
                    ?? throw new NotFoundException(MessageKeys.NotFound);

                var replacementDto = await MapOrderWithCreatorAsync(replacement);
                return new OrderActionResultDto { Order = replacementDto };
            }

            throw new BadRequestException(MessageKeys.BadRequest);
        }

        public async Task<OrderActionResultDto> CompleteAsync(Guid userId, long orderId, CompleteOrderRequest request)
        {
            // No-tracking: avoids EF returning a stale tracked instance inside the transaction after FOR UPDATE.
            var orderPreview = await _uow.Orders.GetByIdWithDetailsAsNoTrackingAsync(orderId)
                ?? throw new NotFoundException(MessageKeys.NotFound);

            var locationId = ResolveOrderLocationId(orderPreview, null);
            await _locationService.ValidateLocationAccessAsync(userId, locationId);
            var isOwner = await _uow.BusinessLocations.IsOwnerOfLocationAsync(userId, locationId);
            if (!isOwner && (orderPreview.CreatedBy == null || orderPreview.CreatedBy != userId))
                throw new ForbiddenException(MessageKeys.Forbidden);

            if (!orderPreview.Status.Equals(OrderStatus.Pending, StringComparison.OrdinalIgnoreCase))
                throw new BadRequestException(MessageKeys.BadRequest);

            var warnings = GetLowStockWarnings(orderPreview.OrderDetails);
            if (warnings.Any() && !request.ConfirmLowStock)
            {
                return new OrderActionResultDto
                {
                    RequiresConfirmation = true,
                    Warnings = warnings
                };
            }

            await _uow.ExecuteResilientAsync(async ct =>
            {
                await _uow.Orders.LockOrderRowForUpdateAsync(orderId, ct);

                var order = await _uow.Orders.GetByIdWithDetailsAsync(orderId)
                    ?? throw new NotFoundException(MessageKeys.NotFound);

                if (!order.Status.Equals(OrderStatus.Pending, StringComparison.OrdinalIgnoreCase))
                    throw new BadRequestException(MessageKeys.OrderCompleteNotPending);

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

                    debtor.CurrentBalance = DebtBalanceSemanticHelper.ApplyOrderDebtIncrease(
                        debtor.CurrentBalance,
                        order.DebtAmount);
                    debtor.UpdatedAt = DateTime.UtcNow;
                    _uow.Debtors.Update(debtor);
                }

                order.Status = OrderStatus.Completed;
                order.CompletedAt = DateTime.UtcNow;
                order.CompletedBy = userId;
                order.UpdatedAt = DateTime.UtcNow;
                order.UpdatedBy = userId;
                _uow.Orders.Update(order);

                var (rawDoc, docDate) = ExtractDocumentInfoFromBillMetadata(order.BillMetadata);
                var docNorm = _documentNumberRegistry.NormalizeOrNull(rawDoc);
                if (docNorm != null)
                {
                    var ownerId = await _uow.BusinessLocations.GetOwnerIdByLocationAsync(locationId)
                        ?? throw new NotFoundException(MessageKeys.NotFound);
                    await _documentNumberRegistry.EnsureLockAndAssertUniqueAsync(ownerId, docNorm, cancellationToken: ct);
                }

                var revenues = BuildSaleRevenuesFromOrder(order, locationId, userId, docNorm, docDate);
                foreach (var revenue in revenues)
                    await _uow.Revenues.AddAsync(revenue);

                await _uow.SaveChangesAsync();

                await _revenueService.RecordPostedSaleRevenuesToLedgerAsync(revenues, ct);

                await _uow.SaveChangesAsync();
            });

            var completed = await _uow.Orders.GetByIdWithDetailsAsync(orderId) ?? throw new NotFoundException(MessageKeys.NotFound);
            var completedDto = await MapOrderWithCreatorAsync(completed);

            // Fire-and-forget: enqueue AI anomaly check via Hangfire.
            // If AI Service is down, the job will retry — does not block user.
            _backgroundJobScheduler.EnqueueAiAnomalyCheck(locationId, "order", orderId);

            return new OrderActionResultDto { Order = completedDto };
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
                    var saleRevenues = await _uow.Revenues.GetSaleByOrderIdAsync(locationId, order.OrderId);
                    var debtAmountToRollback = order.DebtAmount > 0
                        ? order.DebtAmount
                        : saleRevenues
                            .Where(r => r.MoneyChannel == MoneyChannelType.Debt)
                            .Sum(r => r.Amount);

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

                    if (order.DebtorId.HasValue && debtAmountToRollback > 0)
                    {
                        var debtor = await _uow.Debtors.GetByIdAsync(order.DebtorId.Value)
                            ?? throw new NotFoundException(MessageKeys.DebtorNotFound);

                        var rollbackTx = new DebtorPaymentTransaction
                        {
                            DebtorId = debtor.DebtorId,
                            Amount = debtAmountToRollback,
                            PaymentMethod = PaymentMethods.System,
                            Notes = _messageService.GetMessage(MessageKeys.OrderAutoRollbackNote, order.OrderCode),
                            BalanceBefore = debtor.CurrentBalance,
                            BalanceAfter = DebtBalanceSemanticHelper.ApplyOrderDebtRollback(
                                debtor.CurrentBalance,
                                debtAmountToRollback),
                            CreatedByUserId = userId,
                            PaidAt = DateTime.UtcNow
                        };

                        debtor.CurrentBalance = rollbackTx.BalanceAfter;
                        debtor.UpdatedAt = DateTime.UtcNow;

                        await _uow.Debtors.AddPaymentAsync(rollbackTx);
                        _uow.Debtors.Update(debtor);
                        await _uow.SaveChangesAsync();

                        await _debtorService.RecordSystemDebtRollbackLedgerEntryAsync(rollbackTx, locationId);
                    }

                    await _revenueService.ReversePostedSaleRevenuesLedgerForOrderCancelAsync(
                        saleRevenues,
                        MessageKeys.OrderCancelledReversalReason,
                        RevenueStatus.Replaced,
                        userId);
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
            return await MapOrderWithCreatorAsync(cancelled);
        }

        public async Task<EditCompletedSaveResultDto> EditCompletedSaveAsync(Guid userId, long oldOrderId, UpdateOrderRequest request)
        {
            var oldOrder = await _uow.Orders.GetByIdWithDetailsAsync(oldOrderId)
                ?? throw new NotFoundException(MessageKeys.NotFound);

            var locationId = ResolveOrderLocationId(oldOrder, null);

            if (request.BusinessLocationId != locationId)
                throw new BadRequestException(MessageKeys.OrderLocationChangeNotAllowed);

            await _locationService.ValidateLocationAccessAsync(userId, locationId);
            var isOwner = await _uow.BusinessLocations.IsOwnerOfLocationAsync(userId, locationId);
            if (!isOwner && (oldOrder.CreatedBy == null || oldOrder.CreatedBy != userId))
                throw new ForbiddenException(MessageKeys.Forbidden);

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
                        CancelReason = _messageService.GetMessage(MessageKeys.OrderReplacedReason, replacement.OrderCode)
                    });
                }

                return new EditCompletedSaveResultDto
                {
                    OldOrderId = oldOrderId,
                    OldOrderStatus = _labels.ToOption(ReferenceCategory.OrderStatus, OrderStatus.Cancelled),
                    NewOrderId = replacement.OrderId,
                    NewOrderStatus = _labels.ToOption(ReferenceCategory.OrderStatus, replacement.Status)
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
                request.ConfirmLowStock,
                request.ConfirmCreditLimitExceeded);

            if (prepared.RequiresConfirmation)
                throw new BadRequestException(MessageKeys.BadRequest);

            var newOrderId = await _uow.ExecuteResilientAsync(async ct =>
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
                    BillMetadata = NormalizeBillMetadataWithDocumentInfo(request.BillMetadata, request.DocumentNumber, request.DocumentDate),
                    Note = request.Note?.Trim(),
                    CreatedBy = userId,
                    UpdatedBy = userId,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                if (!string.IsNullOrWhiteSpace(idempotencyMarker))
                {
                    newOrder.BillMetadata = AddIdempotencyMarkerToBillMetadata(newOrder.BillMetadata, idempotencyMarker);
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

                    newDebtor.CurrentBalance = DebtBalanceSemanticHelper.ApplyOrderDebtIncrease(
                        newDebtor.CurrentBalance,
                        newOrder.DebtAmount);
                    newDebtor.UpdatedAt = DateTime.UtcNow;
                    _uow.Debtors.Update(newDebtor);
                }

                newOrder.Status = OrderStatus.Completed;
                newOrder.CompletedAt = DateTime.UtcNow;
                newOrder.CompletedBy = userId;
                newOrder.UpdatedAt = DateTime.UtcNow;
                newOrder.UpdatedBy = userId;
                _uow.Orders.Update(newOrder);

                var (rawDoc, docDate) = ExtractDocumentInfoFromBillMetadata(newOrder.BillMetadata);
                var docNorm = _documentNumberRegistry.NormalizeOrNull(rawDoc);
                if (docNorm != null)
                {
                    var ownerId = await _uow.BusinessLocations.GetOwnerIdByLocationAsync(locationId)
                        ?? throw new NotFoundException(MessageKeys.NotFound);
                    await _documentNumberRegistry.EnsureLockAndAssertUniqueAsync(ownerId, docNorm, cancellationToken: ct);
                }

                var newOrderRevenues = BuildSaleRevenuesFromOrder(newOrder, locationId, userId, docNorm, docDate);
                foreach (var revenue in newOrderRevenues)
                    await _uow.Revenues.AddAsync(revenue);

                await _uow.SaveChangesAsync(); // Need RevenueId before GL reference entries.

                await _revenueService.RecordPostedSaleRevenuesToLedgerAsync(newOrderRevenues, ct);

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
                        newOrder.OrderId,
                        _messageService.GetMessage(MessageKeys.OrderReplacedReason, newOrder.OrderCode));
                    product.StockMovements.Add(movement);
                    _uow.Products.Update(product);
                }

                var oldSaleRevenues = await _uow.Revenues.GetSaleByOrderIdAsync(locationId, oldOrder.OrderId);
                var oldDebtAmountToRollback = oldOrder.DebtAmount > 0
                    ? oldOrder.DebtAmount
                    : oldSaleRevenues
                        .Where(r => r.MoneyChannel == MoneyChannelType.Debt)
                        .Sum(r => r.Amount);

                if (oldOrder.DebtorId.HasValue && oldDebtAmountToRollback > 0)
                {
                    var oldDebtor = await _uow.Debtors.GetByIdAsync(oldOrder.DebtorId.Value)
                        ?? throw new NotFoundException(MessageKeys.DebtorNotFound);

                    var rollbackTx = new DebtorPaymentTransaction
                    {
                        DebtorId = oldDebtor.DebtorId,
                        Amount = oldDebtAmountToRollback,
                        PaymentMethod = PaymentMethods.System,
                        Notes = _messageService.GetMessage(MessageKeys.OrderAutoRollbackNote, oldOrder.OrderCode),
                        BalanceBefore = oldDebtor.CurrentBalance,
                        BalanceAfter = DebtBalanceSemanticHelper.ApplyOrderDebtRollback(
                            oldDebtor.CurrentBalance,
                            oldDebtAmountToRollback),
                        CreatedByUserId = userId,
                        PaidAt = DateTime.UtcNow
                    };

                    oldDebtor.CurrentBalance = rollbackTx.BalanceAfter;
                    oldDebtor.UpdatedAt = DateTime.UtcNow;

                    await _uow.Debtors.AddPaymentAsync(rollbackTx);
                    _uow.Debtors.Update(oldDebtor);
                    await _uow.SaveChangesAsync();

                    await _debtorService.RecordSystemDebtRollbackLedgerEntryAsync(rollbackTx, locationId);
                }

                await _revenueService.ReversePostedSaleRevenuesLedgerForOrderCancelAsync(
                    oldSaleRevenues,
                    MessageKeys.OrderCancelledReversalReason,
                    RevenueStatus.Replaced,
                    userId);

                oldOrder.Status = OrderStatus.Cancelled;
                oldOrder.CancelledAt = DateTime.UtcNow;
                oldOrder.CancelledBy = userId;
                oldOrder.CancelReason = _messageService.GetMessage(MessageKeys.OrderReplacedReason, newOrder.OrderCode);
                oldOrder.UpdatedAt = DateTime.UtcNow;
                oldOrder.UpdatedBy = userId;
                _uow.Orders.Update(oldOrder);

                return newOrder.OrderId;
            });

            return new EditCompletedSaveResultDto
            {
                OldOrderId = oldOrderId,
                OldOrderStatus = _labels.ToOption(ReferenceCategory.OrderStatus, OrderStatus.Cancelled),
                NewOrderId = newOrderId,
                NewOrderStatus = _labels.ToOption(ReferenceCategory.OrderStatus, OrderStatus.Completed)
            };
        }

        public async Task<OrderDto> GetDetailAsync(Guid profileId, long orderId)
        {
            var order = await _uow.Orders.GetByIdWithDetailsAsync(orderId)
                ?? throw new NotFoundException(MessageKeys.NotFound);

            var locationId = ResolveOrderLocationId(order, null);
            await _locationService.ValidateLocationAccessAsync(profileId, locationId);

            var isOwner = await _uow.BusinessLocations.IsOwnerOfLocationAsync(profileId, locationId);
            if (!isOwner && (order.CreatedBy == null || order.CreatedBy != profileId))
                throw new ForbiddenException(MessageKeys.Forbidden);

            return await MapOrderWithCreatorAsync(order);
        }

        public async Task<PaginatedResponse<OrderDto>> ListAsync(Guid profileId, OrderQueryParams query)
        {
            await _locationService.ValidateLocationAccessAsync(profileId, query.BusinessLocationId);
            var isOwner = await _uow.BusinessLocations.IsOwnerOfLocationAsync(profileId, query.BusinessLocationId);

            if (!string.IsNullOrWhiteSpace(query.Status)
                && !OrderStatus.IsValid(query.Status.Trim()))
                throw new BadRequestException(MessageKeys.BadRequest);

            if (!string.IsNullOrWhiteSpace(query.Status))
                query.Status = query.Status.Trim().ToLowerInvariant();

            if (!isOwner)
                query.CreatedByProfileId = profileId;

            var (items, totalCount) = await _uow.Orders.SearchAsync(query);
            var dtos = await MapOrdersWithCreatorAsync(items);

            var pageNumber = query.PageNumber ?? 1;
            var pageSize = query.PageSize ?? 20;
            return new PaginatedResponse<OrderDto>(dtos, totalCount, pageNumber, pageSize);
        }

        private static string GenerateOrderCode()
            => $"ORD-{DateTime.UtcNow:yyyyMMddHHmmssfff}";

        private async Task<OrderDto> MapOrderWithCreatorAsync(Order order)
        {
            var dto = _mapper.Map<OrderDto>(order);
            dto.Status = _labels.ToOption(ReferenceCategory.OrderStatus, order.Status);
            if (order.CreatedBy is { } creatorId)
            {
                var profile = await _uow.Profiles.GetByIdAsync(creatorId);
                dto.CreatedByProfileFullName = profile?.FullName;
            }

            return dto;
        }

        private async Task<List<OrderDto>> MapOrdersWithCreatorAsync(IEnumerable<Order> orders)
        {
            var orderList = orders.ToList();
            var dtos = _mapper.Map<List<OrderDto>>(orderList);
            if (dtos.Count == 0)
                return dtos;

            for (var i = 0; i < orderList.Count; i++)
            {
                dtos[i].Status = _labels.ToOption(ReferenceCategory.OrderStatus, orderList[i].Status);
            }

            var creatorIds = orderList.Select(o => o.CreatedBy).Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
            var profiles = creatorIds.Count > 0
                ? await _uow.Profiles.GetByIdsAsync(creatorIds)
                : [];
            var fullNameByProfileId = profiles.ToDictionary(p => p.ProfileId, p => p.FullName);

            foreach (var dto in dtos)
            {
                if (dto.CreatedByProfileId is { } pid && fullNameByProfileId.TryGetValue(pid, out var fullName))
                    dto.CreatedByProfileFullName = fullName;
            }

            return dtos;
        }

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
                bool confirmLowStock,
                bool confirmCreditLimitExceeded)
        {
            if (itemRequests == null || itemRequests.Count == 0)
                throw new BadRequestException(MessageKeys.BadRequest);

            var saleItemIds = itemRequests.Select(i => i.SaleItemId).Distinct().ToList();
            var saleItems = await _uow.Products.GetSaleItemsForPriceAdjustAsync(saleItemIds);
            if (saleItems.Count != saleItemIds.Count)
                throw new NotFoundException(MessageKeys.NotFound);

            if (saleItems.Any(si => si.Product.BusinessLocationId != businessLocationId))
                throw new ForbiddenException(MessageKeys.Forbidden);

            if (debtAmount > 0 && !debtorId.HasValue)
                throw new BadRequestException(MessageKeys.BadRequest);

            Debtor? debtor = null;
            if (debtorId.HasValue)
            {
                debtor = await _uow.Debtors.GetByIdAsync(debtorId.Value)
                    ?? throw new NotFoundException(MessageKeys.DebtorNotFound);

                if (debtor.BusinessLocationId != businessLocationId)
                    throw new ForbiddenException(MessageKeys.Forbidden);
            }

            var details = new List<OrderDetail>();
            decimal subTotal = 0;
            decimal discount = 0;
            decimal total = 0;
            var warnings = new List<string>();

            foreach (var item in itemRequests)
            {
                var saleItem = saleItems.First(si => si.SaleItemId == item.SaleItemId);

                var defaultPolicy = saleItem.ProductPricePolicies.FirstOrDefault(pp => pp.IsDefault);
                if (defaultPolicy == null)
                    throw new BadRequestException(MessageKeys.BadRequest);

                var unitPrice = defaultPolicy.Price;
                var lineSubTotal = item.Quantity * unitPrice;
                var lineAmount = lineSubTotal - item.Discount;
                lineAmount = lineAmount < 0 ? 0 : lineAmount;

                subTotal += lineSubTotal;
                discount += item.Discount;
                total += lineAmount;

                if (saleItem.Product.TrackInventory == true && item.Quantity > saleItem.Product.Stock/saleItem.Quantity)
                {
                    warnings.Add(MessageKeys.LowStockConfirmRequired);
                }

                details.Add(new OrderDetail
                {
                    SaleItemId = item.SaleItemId,
                    Quantity = item.Quantity,
                    UnitPrice = unitPrice,
                    Discount = item.Discount,
                    Amount = lineAmount,
                    CreatedAt = DateTime.UtcNow,
                    SaleItem = saleItem
                });
            }

            var hasLowStockWarning = warnings.Contains(MessageKeys.LowStockConfirmRequired);
            var hasCreditLimitWarning = false;

            if (debtAmount > 0 && debtor != null && debtor.CreditLimit.HasValue)
            {
                var projectedBalance = DebtBalanceSemanticHelper.ApplyOrderDebtIncrease(
                    debtor.CurrentBalance,
                    debtAmount);
                var projectedOutstanding = DebtBalanceSemanticHelper.CalculateOutstandingDebt(projectedBalance);
                if (projectedOutstanding > debtor.CreditLimit.Value)
                {
                    warnings.Add(MessageKeys.DebtorCreditLimitExceededConfirmRequired);
                    hasCreditLimitWarning = true;
                }
            }

            var requiresLowStockConfirm = hasLowStockWarning && !confirmLowStock;
            var requiresCreditLimitConfirm = hasCreditLimitWarning && !confirmCreditLimitExceeded;

            if (requiresLowStockConfirm || requiresCreditLimitConfirm)
            {
                return (true, warnings, new List<OrderDetail>(), 0, 0, 0);
            }

            if (cashAmount + bankAmount + debtAmount != total)
                throw new BadRequestException(MessageKeys.OrderPaymentAmountMismatch);

            return (false, warnings, details, subTotal, discount, total);
        }

        /// <summary>
        /// <paramref name="documentNumberNormalized"/> is produced by
        /// <see cref="IDocumentNumberRegistryService.NormalizeOrNull"/> (already uniqueness-checked at complete-time).
        /// The document is stamped on the <b>first</b> sale revenue row only so owner-wide uniqueness (Costs ∪ Revenues) is not violated by split rows.
        /// </summary>
        private List<Revenue> BuildSaleRevenuesFromOrder(
            Order order,
            int businessLocationId,
            Guid userId,
            string? documentNumberNormalized,
            DateOnly? documentDate)
        {
            var revenues = new List<Revenue>();
            var date = DateOnly.FromDateTime(order.CompletedAt ?? DateTime.UtcNow);
            var businessTypeAllocations = BuildBusinessTypeAllocations(order);
            var orderCode = order.OrderCode ?? order.OrderId.ToString();

            revenues.AddRange(BuildRevenuesByChannel(
                order.CashAmount,
                MoneyChannelType.Cash,
                businessTypeAllocations,
                businessLocationId,
                order.OrderId,
                date,
                userId,
                orderCode));

            revenues.AddRange(BuildRevenuesByChannel(
                order.BankAmount,
                MoneyChannelType.Bank,
                businessTypeAllocations,
                businessLocationId,
                order.OrderId,
                date,
                userId,
                orderCode));

            revenues.AddRange(BuildRevenuesByChannel(
                order.DebtAmount,
                MoneyChannelType.Debt,
                businessTypeAllocations,
                businessLocationId,
                order.OrderId,
                date,
                userId,
                orderCode));

            var first = revenues.FirstOrDefault(r => r.Amount > 0);
            if (first != null)
            {
                first.DocumentNumber = documentNumberNormalized;
                first.DocumentDate = documentDate;
            }

            return revenues;
        }

        private static List<(Guid BusinessTypeId, decimal Weight)> BuildBusinessTypeAllocations(Order order)
        {
            var grouped = order.OrderDetails
                .GroupBy(detail => detail.SaleItem.Product.BusinessTypeId)
                .Select(group => (BusinessTypeId: group.Key, Total: group.Sum(detail => detail.Amount)))
                .Where(x => x.Total > 0)
                .OrderBy(x => x.BusinessTypeId)
                .ToList();

            if (grouped.Count == 0)
                throw new BadRequestException(MessageKeys.BadRequest);

            var total = grouped.Sum(x => x.Total);
            if (total <= 0)
                throw new BadRequestException(MessageKeys.BadRequest);

            return grouped.Select(x => (x.BusinessTypeId, x.Total / total)).ToList();
        }

        private IEnumerable<Revenue> BuildRevenuesByChannel(
            decimal channelAmount,
            string moneyChannel,
            List<(Guid BusinessTypeId, decimal Weight)> allocations,
            int businessLocationId,
            long orderId,
            DateOnly revenueDate,
            Guid userId,
            string orderCode)
        {
            if (channelAmount <= 0)
                return Enumerable.Empty<Revenue>();

            var splitAmounts = SplitAmountByWeights(channelAmount, allocations.Select(x => x.Weight).ToList());
            var now = DateTime.UtcNow;
            var result = new List<Revenue>(allocations.Count);

            for (var i = 0; i < allocations.Count; i++)
            {
                if (splitAmounts[i] <= 0)
                    continue;

                result.Add(new Revenue
                {
                    BusinessLocationId = businessLocationId,
                    BusinessTypeId = allocations[i].BusinessTypeId,
                    OrderId = orderId,
                    RevenueType = RevenueType.Sale,
                    Status = RevenueStatus.Posted,
                    Amount = splitAmounts[i],
                    RevenueDate = revenueDate,
                    Description = _messageService.GetMessage(MessageKeys.OrderRevenueDescriptionFormat, orderCode, moneyChannel),
                    MoneyChannel = moneyChannel,
                    CreatedBy = userId,
                    CreatedAt = now
                });
            }

            return result;
        }

        private static List<decimal> SplitAmountByWeights(decimal amount, List<decimal> weights)
        {
            var results = new List<decimal>(weights.Count);
            var allocated = 0m;

            for (var i = 0; i < weights.Count; i++)
            {
                if (i == weights.Count - 1)
                {
                    results.Add(amount - allocated);
                    break;
                }

                var part = Math.Round(amount * weights[i], 2, MidpointRounding.AwayFromZero);
                results.Add(part);
                allocated += part;
            }

            return results;
        }

        private static string? BuildEditCompletedIdempotencyMarker(string? idempotencyKey)
        {
            if (string.IsNullOrWhiteSpace(idempotencyKey))
                return null;

            return $"IDEMPOTENCY#{idempotencyKey.Trim()}";
        }

        private static string? NormalizeBillMetadata(string? billMetadata)
        {
            if (string.IsNullOrWhiteSpace(billMetadata))
                return null;

            var trimmed = billMetadata.Trim();

            try
            {
                using var _ = JsonDocument.Parse(trimmed);
                return trimmed;
            }
            catch (JsonException)
            {
                // Treat non-JSON input as plain text and store it as JSON string.
                return JsonSerializer.Serialize(trimmed);
            }
        }

        private static string? NormalizeBillMetadataWithDocumentInfo(string? billMetadata, string? documentNumber, DateOnly? documentDate)
        {
            var normalizedNumber = NormalizeDocumentNumber(documentNumber);
            var normalizedMetadata = NormalizeBillMetadata(billMetadata);

            if (normalizedMetadata == null && normalizedNumber == null && !documentDate.HasValue)
                return null;

            JsonNode? parsed = null;
            if (!string.IsNullOrWhiteSpace(normalizedMetadata))
            {
                parsed = JsonNode.Parse(normalizedMetadata);
            }

            JsonObject obj = parsed as JsonObject ?? new JsonObject
            {
                ["metadata"] = parsed
            };

            if (normalizedNumber != null)
                obj["documentNumber"] = normalizedNumber;
            else
                obj.Remove("documentNumber");

            if (documentDate.HasValue)
                obj["documentDate"] = documentDate.Value.ToString("yyyy-MM-dd");
            else
                obj.Remove("documentDate");

            return obj.Count == 0 ? null : obj.ToJsonString();
        }

        private static (string? DocumentNumber, DateOnly? DocumentDate) ExtractDocumentInfoFromBillMetadata(string? billMetadata)
        {
            if (string.IsNullOrWhiteSpace(billMetadata))
                return (null, null);

            try
            {
                var node = JsonNode.Parse(billMetadata) as JsonObject;
                if (node == null)
                    return (null, null);

                var number = NormalizeDocumentNumber(node["documentNumber"]?.GetValue<string>());
                var dateRaw = node["documentDate"]?.GetValue<string>();
                var hasDate = DateOnly.TryParse(dateRaw, out var parsedDate);

                return (number, hasDate ? parsedDate : null);
            }
            catch
            {
                return (null, null);
            }
        }

        private static string? NormalizeDocumentNumber(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            return value.Trim();
        }

        private static string AddIdempotencyMarkerToBillMetadata(string? normalizedBillMetadata, string marker)
        {
            const string markerField = "idempotencyMarker";

            if (string.IsNullOrWhiteSpace(normalizedBillMetadata))
            {
                var obj = new JsonObject
                {
                    [markerField] = marker
                };
                return obj.ToJsonString();
            }

            var node = JsonNode.Parse(normalizedBillMetadata);

            if (node is JsonObject jsonObject)
            {
                jsonObject[markerField] = marker;
                return jsonObject.ToJsonString();
            }

            var wrapped = new JsonObject
            {
                ["metadata"] = node,
                [markerField] = marker
            };

            return wrapped.ToJsonString();
        }

    }
}
