using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Exceptions;
using BizFlow.Application.DTOs.Order;
using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Application.Interfaces.Services;
using BizFlow.Application.Services;
using BizFlow.Domain.Entities;
using BizFlow.Domain.Enums;
using AutoMapper;
using BizFlow.Application.Mappers;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BizFlow.Application.Tests;

public class OrderServiceTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IBusinessLocationService> _locationService = new();
    private readonly Mock<IBusinessLocationRepository> _locationRepo = new();
    private readonly Mock<IOrderRepository> _orderRepo = new();
    private readonly Mock<IProfileRepository> _profileRepo = new();
    private readonly Mock<IStockMovementService> _stockMovementService = new();
    private readonly Mock<IGeneralLedgerService> _glService = new();
    private readonly Mock<IBackgroundJobScheduler> _bgScheduler = new();
    private readonly Mock<BizFlow.Application.Common.Interfaces.IMessageService> _messageService = new();
    private readonly IMapper _mapper;

    public OrderServiceTests()
    {
        var cfg = new MapperConfiguration(c => c.AddProfile<OrderProfile>(), NullLoggerFactory.Instance);
        _mapper = cfg.CreateMapper();

        _uow.SetupGet(x => x.Orders).Returns(_orderRepo.Object);
        _uow.SetupGet(x => x.BusinessLocations).Returns(_locationRepo.Object);
        _uow.SetupGet(x => x.Profiles).Returns(_profileRepo.Object);

        _messageService.Setup(m => m.GetMessage(It.IsAny<string>(), It.IsAny<object[]>()))
            .Returns((string key, object[] _) => key);
        _messageService.Setup(m => m.GetMessage(It.IsAny<string>()))
            .Returns((string key) => key);
    }

    private OrderService BuildSut() => new(
        _uow.Object,
        _mapper,
        _locationService.Object,
        _stockMovementService.Object,
        _glService.Object,
        _messageService.Object,
        _bgScheduler.Object);

    // ═══════════════════════════════════════════════════
    // COMPLETE ORDER
    // ═══════════════════════════════════════════════════

    [Fact]
    public async Task CompleteAsync_WhenOrderNotFound_ShouldThrowNotFound()
    {
        _orderRepo.Setup(r => r.GetByIdWithDetailsAsNoTrackingAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Order?)null);

        var sut = BuildSut();

        await Assert.ThrowsAsync<NotFoundException>(() => sut.CompleteAsync(Guid.NewGuid(), 99,
            new CompleteOrderRequest()));
    }

    [Fact]
    public async Task CompleteAsync_WhenAlreadyCompleted_ShouldThrowBadRequest()
    {
        var userId = Guid.NewGuid();
        var order = new Order
        {
            OrderId = 1,
            Status = OrderStatus.Completed,
            OrderDetails = new List<OrderDetail>()
        };
        _orderRepo.Setup(r => r.GetByIdWithDetailsAsNoTrackingAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _locationService.Setup(s => s.ValidateLocationAccessAsync(userId, It.IsAny<int>()))
            .Returns(Task.CompletedTask);
        _locationRepo.Setup(r => r.IsOwnerOfLocationAsync(userId, It.IsAny<int>())).ReturnsAsync(true);

        var sut = BuildSut();

        await Assert.ThrowsAsync<BadRequestException>(() => sut.CompleteAsync(userId, 1,
            new CompleteOrderRequest()));
    }

    [Fact]
    public async Task CompleteAsync_WhenCancelled_ShouldThrowBadRequest()
    {
        var userId = Guid.NewGuid();
        var order = new Order
        {
            OrderId = 2,
            Status = OrderStatus.Cancelled,
            OrderDetails = new List<OrderDetail>()
        };
        _orderRepo.Setup(r => r.GetByIdWithDetailsAsNoTrackingAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _locationService.Setup(s => s.ValidateLocationAccessAsync(userId, It.IsAny<int>()))
            .Returns(Task.CompletedTask);
        _locationRepo.Setup(r => r.IsOwnerOfLocationAsync(userId, It.IsAny<int>())).ReturnsAsync(true);

        var sut = BuildSut();

        await Assert.ThrowsAsync<BadRequestException>(() => sut.CompleteAsync(userId, 2,
            new CompleteOrderRequest()));
    }

    // ═══════════════════════════════════════════════════
    // CANCEL ORDER
    // ═══════════════════════════════════════════════════

    [Fact]
    public async Task CancelAsync_WhenOrderNotFound_ShouldThrowNotFound()
    {
        _orderRepo.Setup(r => r.GetByIdWithDetailsAsync(99)).ReturnsAsync((Order?)null);

        var sut = BuildSut();

        await Assert.ThrowsAsync<NotFoundException>(() => sut.CancelAsync(Guid.NewGuid(), 99,
            new CancelOrderRequest { CancelReason = "test" }));
    }

    [Fact]
    public async Task CancelAsync_WhenAlreadyCancelled_ShouldThrowBadRequest()
    {
        var userId = Guid.NewGuid();
        var order = new Order
        {
            OrderId = 3,
            Status = OrderStatus.Cancelled,
            OrderDetails = new List<OrderDetail>()
        };
        _orderRepo.Setup(r => r.GetByIdWithDetailsAsync(3)).ReturnsAsync(order);
        _locationService.Setup(s => s.ValidateOwnerAsync(userId, It.IsAny<int>())).Returns(Task.CompletedTask);

        var sut = BuildSut();

        await Assert.ThrowsAsync<BadRequestException>(() => sut.CancelAsync(userId, 3,
            new CancelOrderRequest { CancelReason = "test" }));
    }

    // ═══════════════════════════════════════════════════
    // GET ORDER DETAIL
    // ═══════════════════════════════════════════════════

    [Fact]
    public async Task GetDetailAsync_WhenOrderNotFound_ShouldThrowNotFound()
    {
        _orderRepo.Setup(r => r.GetByIdWithDetailsAsync(999)).ReturnsAsync((Order?)null);

        var sut = BuildSut();

        await Assert.ThrowsAsync<NotFoundException>(() => sut.GetDetailAsync(Guid.NewGuid(), 999));
    }

    // ═══════════════════════════════════════════════════
    // LIST ORDERS
    // ═══════════════════════════════════════════════════

    [Fact]
    public async Task ListAsync_WhenInvalidStatus_ShouldThrowBadRequest()
    {
        var userId = Guid.NewGuid();
        _locationService.Setup(s => s.ValidateLocationAccessAsync(userId, 1)).Returns(Task.CompletedTask);
        _locationRepo.Setup(r => r.IsOwnerOfLocationAsync(userId, 1)).ReturnsAsync(true);

        var sut = BuildSut();

        await Assert.ThrowsAsync<BadRequestException>(() => sut.ListAsync(userId,
            new OrderQueryParams { BusinessLocationId = 1, Status = "invalid_status" }));
    }

    [Fact]
    public async Task ListAsync_WhenValidNoOrders_ShouldReturnEmptyPage()
    {
        var userId = Guid.NewGuid();
        _locationService.Setup(s => s.ValidateLocationAccessAsync(userId, 1)).Returns(Task.CompletedTask);
        _locationRepo.Setup(r => r.IsOwnerOfLocationAsync(userId, 1)).ReturnsAsync(true);
        _orderRepo.Setup(r => r.SearchAsync(It.IsAny<OrderQueryParams>()))
            .ReturnsAsync((new List<Order>(), 0));
        _profileRepo.Setup(r => r.GetByIdsAsync(It.IsAny<List<Guid>>()))
            .ReturnsAsync(new List<BizFlow.Domain.Entities.Profile>());

        var sut = BuildSut();
        var result = await sut.ListAsync(userId, new OrderQueryParams
        {
            BusinessLocationId = 1, PageNumber = 1, PageSize = 10
        });

        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
    }

    // ═══════════════════════════════════════════════════
    // UPDATE ORDER
    // ═══════════════════════════════════════════════════

    [Fact]
    public async Task UpdateAsync_WhenOrderNotFound_ShouldThrowNotFound()
    {
        _orderRepo.Setup(r => r.GetByIdWithDetailsAsync(99)).ReturnsAsync((Order?)null);

        var sut = BuildSut();

        await Assert.ThrowsAsync<NotFoundException>(() => sut.UpdateAsync(Guid.NewGuid(), 99,
            new UpdateOrderRequest { BusinessLocationId = 1, Items = new List<OrderItemRequest>() }));
    }

    [Fact]
    public async Task UpdateAsync_WhenStatusNotPendingOrCompleted_ShouldThrowBadRequest()
    {
        var userId = Guid.NewGuid();
        var order = new Order
        {
            OrderId = 5,
            Status = OrderStatus.Cancelled,
            OrderDetails = new List<OrderDetail>
            {
                new OrderDetail { SaleItem = new SaleItem { Product = new Product { BusinessLocationId = 1 } } }
            }
        };
        _orderRepo.Setup(r => r.GetByIdWithDetailsAsync(5)).ReturnsAsync(order);
        _locationService.Setup(s => s.ValidateLocationAccessAsync(userId, 1)).Returns(Task.CompletedTask);
        _locationRepo.Setup(r => r.IsOwnerOfLocationAsync(userId, 1)).ReturnsAsync(true);

        var sut = BuildSut();

        await Assert.ThrowsAsync<BadRequestException>(() => sut.UpdateAsync(userId, 5,
            new UpdateOrderRequest
            {
                BusinessLocationId = 1,
                Items = new List<OrderItemRequest> { new OrderItemRequest { SaleItemId = 1, Quantity = 1 } }
            }));
    }

    // ═══════════════════════════════════════════════════
    // EDGE CASES: INVENTORY & PRICING
    // ═══════════════════════════════════════════════════

    [Fact]
    public async Task CreateAsync_WithStalePriceFromFE_ShouldUseCurrentPolicyPrice()
    {
        var userId = Guid.NewGuid();
        var request = new CreateOrderRequest
        {
            BusinessLocationId = 1,
            Items = new List<OrderItemRequest>
            {
                new OrderItemRequest { SaleItemId = 1, Quantity = 2, Discount = 0 } // FE thinking price is something else, but actually we just pass Quantity
            },
            CashAmount = 24, // Expecting 2 * 12 = 24
            BankAmount = 0,
            DebtAmount = 0
        };

        var saleItem = new SaleItem
        {
            SaleItemId = 1,
            Quantity = 1,
            Product = new Product { BusinessLocationId = 1, TrackInventory = true, Stock = 10 },
            ProductPricePolicies = new List<ProductPricePolicy>
            {
                new ProductPricePolicy { IsDefault = true, Price = 12 } // Backend price is 12
            }
        };

        var mockProductRepo = new Mock<IProductRepository>();
        mockProductRepo.Setup(r => r.GetSaleItemsForPriceAdjustAsync(It.IsAny<IEnumerable<long>>()))
            .ReturnsAsync(new List<SaleItem> { saleItem });
        _uow.SetupGet(u => u.Products).Returns(mockProductRepo.Object);

        _locationService.Setup(s => s.ValidateLocationAccessAsync(userId, 1)).Returns(Task.CompletedTask);
        _uow.Setup(u => u.ExecuteResilientAsync(It.IsAny<Func<CancellationToken, Task<Order>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Order { OrderId = 1 });

        var sut = BuildSut();
        var result = await sut.CreateAsync(userId, request);

        // FE sends cashAmount = 24. Since 2 * 12 = 24, it perfectly matches the BE calculated total.
        // If FE sent cashAmount = 20 (stale price 10), then it would throw BadRequestException due to payment mismatch!
        // So the fact that it passes means the backend enforced the 12 price!
        Assert.False(result.RequiresConfirmation);

        // Prove that it throws when mismatch
        request.CashAmount = 20; // Stale price from FE scenario
        await Assert.ThrowsAsync<BadRequestException>(() => sut.CreateAsync(userId, request));
    }

    [Fact]
    public async Task CompleteAsync_WithNegativeInventory_ShouldNotBlock_WhenTrackInventoryTrue()
    {
        var userId = Guid.NewGuid();
        var order = new Order
        {
            OrderId = 10,
            Status = OrderStatus.Pending,
            OrderDetails = new List<OrderDetail>
            {
                new OrderDetail
                {
                    Quantity = 5, // Order 5
                    Amount = 100, // Important for BuildBusinessTypeAllocations
                    SaleItem = new SaleItem
                    {
                        Product = new Product 
                        { 
                            BusinessLocationId = 1, TrackInventory = true, Stock = -10, 
                            BusinessTypeId = Guid.NewGuid(),
                            StockMovements = new List<StockMovement>()
                        } // Already negative!
                    }
                }
            }
        };

        _orderRepo.Setup(r => r.GetByIdWithDetailsAsNoTrackingAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _orderRepo.Setup(r => r.GetByIdWithDetailsAsync(10)).ReturnsAsync(order);
        _locationService.Setup(s => s.ValidateLocationAccessAsync(userId, It.IsAny<int>())).Returns(Task.CompletedTask);
        _locationRepo.Setup(r => r.IsOwnerOfLocationAsync(userId, It.IsAny<int>())).ReturnsAsync(true);
        _uow.Setup(u => u.ExecuteResilientAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Callback<Func<CancellationToken, Task>, CancellationToken>((func, ct) => func(ct).GetAwaiter().GetResult())
            .Returns(Task.CompletedTask);

        // Mock revenue/GL to avoid null refs
        var mockRevRepo = new Mock<IRevenueRepository>();
        _uow.SetupGet(u => u.Revenues).Returns(mockRevRepo.Object);
        var mockProductRepo = new Mock<IProductRepository>();
        _uow.SetupGet(u => u.Products).Returns(mockProductRepo.Object);

        var sut = BuildSut();
        
        // It should complete successfully without throwing
        await sut.CompleteAsync(userId, 10, new CompleteOrderRequest { ConfirmLowStock = true });

        Assert.Equal(-15, order.OrderDetails.First().SaleItem.Product.Stock); // Did not block, went from -10 to -15
    }

    // ═══════════════════════════════════════════════════
    // EDGE CASES: DEBT RULES
    // ═══════════════════════════════════════════════════

    [Fact]
    public async Task CreateAsync_WhenDebtExceedsLimit_ShouldReturnWarning_InsteadOfBlock()
    {
        var userId = Guid.NewGuid();
        var request = new CreateOrderRequest
        {
            BusinessLocationId = 1,
            DebtorId = 2,
            Items = new List<OrderItemRequest> { new OrderItemRequest { SaleItemId = 1, Quantity = 1 } },
            DebtAmount = 1000 // Wants to debt 1000
        };

        var saleItem = new SaleItem
        {
            SaleItemId = 1, Quantity = 1,
            Product = new Product { BusinessLocationId = 1, TrackInventory = false },
            ProductPricePolicies = new List<ProductPricePolicy> { new ProductPricePolicy { IsDefault = true, Price = 1000 } }
        };

        var debtor = new Debtor
        {
            DebtorId = 2, BusinessLocationId = 1,
            CurrentBalance = -500, // Owes 500
            CreditLimit = 1000 // Limit is 1000
        };

        var mockProductRepo = new Mock<IProductRepository>();
        mockProductRepo.Setup(r => r.GetSaleItemsForPriceAdjustAsync(It.IsAny<IEnumerable<long>>())).ReturnsAsync(new List<SaleItem> { saleItem });
        _uow.SetupGet(u => u.Products).Returns(mockProductRepo.Object);

        var mockDebtorRepo = new Mock<IDebtorRepository>();
        mockDebtorRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(debtor);
        _uow.SetupGet(u => u.Debtors).Returns(mockDebtorRepo.Object);

        _locationService.Setup(s => s.ValidateLocationAccessAsync(userId, 1)).Returns(Task.CompletedTask);

        var sut = BuildSut();
        
        // Debt will be 500 + 1000 = 1500 > 1000. So it should throw warning, NOT block completely.
        var result = await sut.CreateAsync(userId, request);

        Assert.True(result.RequiresConfirmation);
        Assert.Contains(MessageKeys.DebtorCreditLimitExceededConfirmRequired, result.Warnings!);
    }

    [Fact]
    public async Task CompleteAsync_WhenCurrentBalanceIsPositive_ShouldDeductUsingDebtAmount()
    {
        var userId = Guid.NewGuid();
        var order = new Order
        {
            OrderId = 11, Status = OrderStatus.Pending,
            DebtorId = 5, DebtAmount = 200, // Wants to use 200 debt
            OrderDetails = new List<OrderDetail>
            {
                new OrderDetail 
                { 
                    Quantity = 1, Amount = 200, 
                    SaleItem = new SaleItem { Product = new Product { BusinessLocationId = 1, TrackInventory = false, BusinessTypeId = Guid.NewGuid(), StockMovements = new List<StockMovement>() } } 
                }
            }
        };

        var debtor = new Debtor
        {
            DebtorId = 5, CurrentBalance = 500 // Has POSITIVE 500 balance!
        };

        _orderRepo.Setup(r => r.GetByIdWithDetailsAsNoTrackingAsync(11, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _orderRepo.Setup(r => r.GetByIdWithDetailsAsync(11)).ReturnsAsync(order);
        _locationService.Setup(s => s.ValidateLocationAccessAsync(userId, It.IsAny<int>())).Returns(Task.CompletedTask);
        _locationRepo.Setup(r => r.IsOwnerOfLocationAsync(userId, It.IsAny<int>())).ReturnsAsync(true);
        _uow.Setup(u => u.ExecuteResilientAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Callback<Func<CancellationToken, Task>, CancellationToken>((func, ct) => func(ct).GetAwaiter().GetResult())
            .Returns(Task.CompletedTask);

        var mockDebtorRepo = new Mock<IDebtorRepository>();
        mockDebtorRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(debtor);
        _uow.SetupGet(u => u.Debtors).Returns(mockDebtorRepo.Object);

        var mockRevRepo = new Mock<IRevenueRepository>();
        _uow.SetupGet(u => u.Revenues).Returns(mockRevRepo.Object);
        var mockProductRepo = new Mock<IProductRepository>();
        _uow.SetupGet(u => u.Products).Returns(mockProductRepo.Object);

        var sut = BuildSut();
        await sut.CompleteAsync(userId, 11, new CompleteOrderRequest());

        // Balance went from 500 to 300! It correctly consumed the positive balance.
        Assert.Equal(300, debtor.CurrentBalance); 
    }
}
