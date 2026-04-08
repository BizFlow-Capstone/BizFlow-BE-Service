using AutoMapper;
using BizFlow.Application.DTOs.Order;
using BizFlow.Application.Mappers;
using BizFlow.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BizFlow.Application.Tests;

public class OrderProfileAnonymizeTests
{
    [Fact]
    public void Map_OrderWithNullCreatedBy_MapsNullCreatedByProfileId()
    {
        var cfg = new MapperConfiguration(c => c.AddProfile<OrderProfile>(), NullLoggerFactory.Instance);
        var mapper = cfg.CreateMapper();

        var order = new Order
        {
            OrderId = 1,
            OrderCode = "ORD-TEST-001",
            SubTotal = 0,
            Discount = 0,
            TotalAmount = 0,
            CashAmount = 0,
            BankAmount = 0,
            DebtAmount = 0,
            Status = "pending",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = null,
            OrderDetails = new List<OrderDetail>()
        };

        var dto = mapper.Map<OrderDto>(order);

        Assert.Null(dto.CreatedByProfileId);
    }
}
