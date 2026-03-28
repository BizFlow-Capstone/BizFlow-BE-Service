using System.Text.Json;
using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Application.Interfaces.Services;
using BizFlow.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BizFlow.Application.Tests;

public class FormulaEngineTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ILogger<Infrastructure.Services.FormulaEngine.FormulaEngine>> _logger = new();

    private Infrastructure.Services.FormulaEngine.FormulaEngine BuildSut() => new(_uow.Object, _logger.Object);

    private FormulaEvaluationContext BuildContext() => new()
    {
        BookId = 1,
        BusinessLocationId = 6,
        PeriodId = 1,
        PeriodStart = new DateOnly(2025, 1, 1),
        PeriodEnd = new DateOnly(2025, 3, 31),
        RulesetId = 1,
        BusinessTypeIds = new List<Guid> { Guid.NewGuid() }
    };

    // ═══════════════════════════════════════════════════
    // LITERAL
    // ═══════════════════════════════════════════════════

    [Fact]
    public async Task Evaluate_Literal_ShouldReturnValue()
    {
        var formula = new FormulaDefinition
        {
            FormulaId = 1,
            Code = "TEST_LITERAL",
            ExpressionJson = """{"literal": 42.5}""",
            FormulaType = "literal"
        };

        var sut = BuildSut();
        var results = await sut.EvaluateFormulasAsync(BuildContext(), new[] { formula });

        Assert.Equal(42.5m, results["TEST_LITERAL"]);
    }

    // ═══════════════════════════════════════════════════
    // REF (reference to another formula)
    // ═══════════════════════════════════════════════════

    [Fact]
    public async Task Evaluate_Ref_ShouldResolveFromPreviousFormula()
    {
        var f1 = new FormulaDefinition
        {
            FormulaId = 1,
            Code = "BASE",
            ExpressionJson = """{"literal": 100}""",
            FormulaType = "literal"
        };
        var f2 = new FormulaDefinition
        {
            FormulaId = 2,
            Code = "DOUBLE_BASE",
            ExpressionJson = """{"op": "MULTIPLY", "left": {"ref": "BASE"}, "right": {"literal": 2}}""",
            FormulaType = "computed"
        };

        var sut = BuildSut();
        var results = await sut.EvaluateFormulasAsync(BuildContext(), new[] { f1, f2 });

        Assert.Equal(100m, results["BASE"]);
        Assert.Equal(200m, results["DOUBLE_BASE"]);
    }

    // ═══════════════════════════════════════════════════
    // BINARY OPS
    // ═══════════════════════════════════════════════════

    [Theory]
    [InlineData("ADD", 10, 3, 13)]
    [InlineData("SUBTRACT", 10, 3, 7)]
    [InlineData("MULTIPLY", 10, 3, 30)]
    [InlineData("DIVIDE", 10, 4, 2.5)]
    public async Task Evaluate_Op_ShouldComputeCorrectly(string op, decimal left, decimal right, decimal expected)
    {
        var formula = new FormulaDefinition
        {
            FormulaId = 1,
            Code = "OP_TEST",
            ExpressionJson = JsonSerializer.Serialize(new
            {
                op,
                left = new { literal = left },
                right = new { literal = right }
            }),
            FormulaType = "computed"
        };

        var sut = BuildSut();
        var results = await sut.EvaluateFormulasAsync(BuildContext(), new[] { formula });

        Assert.Equal(expected, results["OP_TEST"]);
    }

    [Fact]
    public async Task Evaluate_DivideByZero_ShouldReturnZero()
    {
        var formula = new FormulaDefinition
        {
            FormulaId = 1,
            Code = "DIV_ZERO",
            ExpressionJson = """{"op": "DIVIDE", "left": {"literal": 100}, "right": {"literal": 0}}""",
            FormulaType = "computed"
        };

        var sut = BuildSut();
        var results = await sut.EvaluateFormulasAsync(BuildContext(), new[] { formula });

        Assert.Equal(0m, results["DIV_ZERO"]);
    }

    // ═══════════════════════════════════════════════════
    // FUNCTIONS: MAX, MIN, ABS
    // ═══════════════════════════════════════════════════

    [Fact]
    public async Task Evaluate_Max_ShouldReturnLargest()
    {
        var formula = new FormulaDefinition
        {
            FormulaId = 1,
            Code = "MAX_TEST",
            ExpressionJson = """{"fn": "MAX", "args": [{"literal": 5}, {"literal": 12}, {"literal": 8}]}""",
            FormulaType = "computed"
        };

        var sut = BuildSut();
        var results = await sut.EvaluateFormulasAsync(BuildContext(), new[] { formula });

        Assert.Equal(12m, results["MAX_TEST"]);
    }

    [Fact]
    public async Task Evaluate_Min_ShouldReturnSmallest()
    {
        var formula = new FormulaDefinition
        {
            FormulaId = 1,
            Code = "MIN_TEST",
            ExpressionJson = """{"fn": "MIN", "args": [{"literal": 5}, {"literal": 12}, {"literal": 3}]}""",
            FormulaType = "computed"
        };

        var sut = BuildSut();
        var results = await sut.EvaluateFormulasAsync(BuildContext(), new[] { formula });

        Assert.Equal(3m, results["MIN_TEST"]);
    }

    [Fact]
    public async Task Evaluate_Abs_ShouldReturnAbsoluteValue()
    {
        var formula = new FormulaDefinition
        {
            FormulaId = 1,
            Code = "ABS_TEST",
            ExpressionJson = """{"fn": "ABS", "args": [{"literal": -42}]}""",
            FormulaType = "computed"
        };

        var sut = BuildSut();
        var results = await sut.EvaluateFormulasAsync(BuildContext(), new[] { formula });

        Assert.Equal(42m, results["ABS_TEST"]);
    }

    // ═══════════════════════════════════════════════════
    // ROUNDING
    // ═══════════════════════════════════════════════════

    [Fact]
    public async Task Evaluate_RoundHalfUp_ShouldRoundCorrectly()
    {
        var formula = new FormulaDefinition
        {
            FormulaId = 1,
            Code = "ROUND_TEST",
            ExpressionJson = """{"op": "DIVIDE", "left": {"literal": 10}, "right": {"literal": 3}}""",
            FormulaType = "computed",
            RoundingMode = "round_half_up",
            RoundingPrecision = 2
        };

        var sut = BuildSut();
        var results = await sut.EvaluateFormulasAsync(BuildContext(), new[] { formula });

        Assert.Equal(3.33m, results["ROUND_TEST"]);
    }

    [Fact]
    public async Task Evaluate_Floor_ShouldRoundDown()
    {
        var formula = new FormulaDefinition
        {
            FormulaId = 1,
            Code = "FLOOR_TEST",
            ExpressionJson = """{"literal": 3.99}""",
            FormulaType = "literal",
            RoundingMode = "floor",
            RoundingPrecision = 0
        };

        var sut = BuildSut();
        var results = await sut.EvaluateFormulasAsync(BuildContext(), new[] { formula });

        Assert.Equal(3m, results["FLOOR_TEST"]);
    }

    // ═══════════════════════════════════════════════════
    // COMPLEX CHAIN
    // ═══════════════════════════════════════════════════

    [Fact]
    public async Task Evaluate_ComplexChain_ShouldResolveInOrder()
    {
        // revenue = 1000, taxRate = 0.01 (1%), tax = revenue * taxRate = 10
        var formulas = new[]
        {
            new FormulaDefinition
            {
                FormulaId = 1, Code = "REVENUE",
                ExpressionJson = """{"literal": 1000}""",
                FormulaType = "literal"
            },
            new FormulaDefinition
            {
                FormulaId = 2, Code = "TAX_RATE",
                ExpressionJson = """{"literal": 0.01}""",
                FormulaType = "literal"
            },
            new FormulaDefinition
            {
                FormulaId = 3, Code = "TAX_AMOUNT",
                ExpressionJson = """{"op": "MULTIPLY", "left": {"ref": "REVENUE"}, "right": {"ref": "TAX_RATE"}}""",
                FormulaType = "computed"
            }
        };

        var sut = BuildSut();
        var results = await sut.EvaluateFormulasAsync(BuildContext(), formulas);

        Assert.Equal(1000m, results["REVENUE"]);
        Assert.Equal(0.01m, results["TAX_RATE"]);
        Assert.Equal(10m, results["TAX_AMOUNT"]);
    }
}
