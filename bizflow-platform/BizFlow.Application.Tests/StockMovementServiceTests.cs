using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Exceptions;
using BizFlow.Application.Services;
using BizFlow.Domain.Entities;
using BizFlow.Domain.Enums;
using Xunit;

namespace BizFlow.Application.Tests;

public class StockMovementServiceTests
{
    private readonly StockMovementService _sut = new();

    [Fact]
    public void CreateStockMovement_WhenQuantityPositive_ShouldCreateInMovement()
    {
        var product = new Product { ProductId = 10, Stock = 150 };

        var movement = _sut.CreateStockMovement(product, 20, StockMovementReferenceType.Import, 99, "restock");

        Assert.Equal(10, movement.ProductId);
        Assert.Equal(20, movement.Quantity);
        Assert.Equal(StockMovementType.In, movement.MovementType);
        Assert.Equal(150, movement.BalanceAfter);
        Assert.Equal(99, movement.ReferenceId);
        Assert.Equal("restock", movement.Memo);
    }

    [Fact]
    public void CreateStockMovement_WhenQuantityNegative_ShouldCreateOutMovement()
    {
        var product = new Product { ProductId = 7, Stock = 45 };

        var movement = _sut.CreateStockMovement(product, -5, StockMovementReferenceType.Order, 123, "sale");

        Assert.Equal(StockMovementType.Out, movement.MovementType);
        Assert.Equal(-5, movement.Quantity);
        Assert.Equal(45, movement.BalanceAfter);
    }

    [Fact]
    public void CreateStockMovement_WhenReferenceTypeNull_ShouldAllowAndCreate()
    {
        var product = new Product { ProductId = 3, Stock = 10 };

        var movement = _sut.CreateStockMovement(product, 1, null, null, null);

        Assert.Equal(StockMovementType.In, movement.MovementType);
        Assert.Null(movement.ReferenceType);
        Assert.Null(movement.ReferenceId);
    }

    [Fact]
    public void CreateStockMovement_WhenQuantityIsZero_ShouldThrowBadRequest()
    {
        var product = new Product { ProductId = 1, Stock = 5 };

        var ex = Assert.Throws<BadRequestException>(() => _sut.CreateStockMovement(product, 0, null, null));
        Assert.Equal(MessageKeys.BadRequest, ex.MessageKey);
    }

    [Fact]
    public void CreateStockMovement_WhenReferenceTypeInvalid_ShouldThrowBadRequest()
    {
        var product = new Product { ProductId = 2, Stock = 5 };

        var ex = Assert.Throws<BadRequestException>(() => _sut.CreateStockMovement(product, 5, "invalid_ref", 1));
        Assert.Equal(MessageKeys.BadRequest, ex.MessageKey);
    }
}
