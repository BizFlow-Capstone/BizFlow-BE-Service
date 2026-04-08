using BizFlow.Application.Services;
using Xunit;

namespace BizFlow.Application.Tests;

public class ReferenceServiceTests
{
    private readonly ReferenceService _sut = new();

    [Fact]
    public void AllReferenceMethods_ShouldReturnNonEmptyValues_WithoutNulls()
    {
        var all = new List<IReadOnlyList<string>>
        {
            _sut.GetPaymentMethods(),
            _sut.GetBusinessTypeStatuses(),
            _sut.GetCostTypes(),
            _sut.GetGeneralLedgerReferenceTypes(),
            _sut.GetGeneralLedgerTransactionTypes(),
            _sut.GetGeneralLedgerViewModes(),
            _sut.GetImportStatuses(),
            _sut.GetImportTypes(),
            _sut.GetMoneyChannelTypes(),
            _sut.GetOrderStatuses(),
            _sut.GetProductStatuses(),
            _sut.GetRevenueTypes(),
            _sut.GetSubscriptionStatuses(),
            _sut.GetSubscriptionTransactionTypes(),
            _sut.GetStockMovementTypes(),
            _sut.GetStockMovementReferenceTypes()
        };

        Assert.All(all, values =>
        {
            Assert.NotNull(values);
            Assert.NotEmpty(values);
            Assert.DoesNotContain(values, string.IsNullOrWhiteSpace);
        });
    }

    [Fact]
    public void ReferenceLists_ShouldNotContainDuplicates_CaseInsensitive()
    {
        var all = new List<IReadOnlyList<string>>
        {
            _sut.GetPaymentMethods(),
            _sut.GetBusinessTypeStatuses(),
            _sut.GetCostTypes(),
            _sut.GetGeneralLedgerReferenceTypes(),
            _sut.GetGeneralLedgerTransactionTypes(),
            _sut.GetGeneralLedgerViewModes(),
            _sut.GetImportStatuses(),
            _sut.GetImportTypes(),
            _sut.GetMoneyChannelTypes(),
            _sut.GetOrderStatuses(),
            _sut.GetProductStatuses(),
            _sut.GetRevenueTypes(),
            _sut.GetSubscriptionStatuses(),
            _sut.GetSubscriptionTransactionTypes(),
            _sut.GetStockMovementTypes(),
            _sut.GetStockMovementReferenceTypes()
        };

        foreach (var list in all)
        {
            var distinct = list.Distinct(StringComparer.OrdinalIgnoreCase).Count();
            Assert.Equal(list.Count, distinct);
        }
    }

    [Fact]
    public void ReferenceMethods_ShouldBeDeterministic()
    {
        Assert.Equal(_sut.GetPaymentMethods(), _sut.GetPaymentMethods());
        Assert.Equal(_sut.GetImportStatuses(), _sut.GetImportStatuses());
        Assert.Equal(_sut.GetOrderStatuses(), _sut.GetOrderStatuses());
        Assert.Equal(_sut.GetRevenueTypes(), _sut.GetRevenueTypes());
    }
}
