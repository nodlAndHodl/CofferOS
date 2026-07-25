using CofferOS.Application.Treasury;
using CofferOS.Domain.Common;
using CofferOS.Domain.Treasury;
using Xunit;

namespace CofferOS.Application.Tests.Treasury;

public class LoanCalculatorTests
{
    [Fact]
    public void CalculateCollateralValue_MultipliesCorrectly()
    {
        var value = LoanCalculator.CalculateCollateralValue(2.5m, 100000m);
        Assert.Equal(250000m, value);
    }

    [Fact]
    public void CalculateCollateralValue_ThrowsOnNegativeInputs()
    {
        Assert.Throws<ArgumentException>(() => LoanCalculator.CalculateCollateralValue(-1, 100000));
        Assert.Throws<ArgumentException>(() => LoanCalculator.CalculateCollateralValue(1, -1));
    }

    [Fact]
    public void CalculateCurrentLtv_ComputesRatio()
    {
        var ltv = LoanCalculator.CalculateCurrentLtv(70000m, 100000m);
        Assert.Equal(0.7m, ltv);
    }

    [Fact]
    public void CalculateCurrentLtv_ReturnsZeroWhenCollateralIsZero()
    {
        var ltv = LoanCalculator.CalculateCurrentLtv(10000m, 0m);
        Assert.Equal(0m, ltv);
    }

    [Fact]
    public void CalculateCurrentLtv_ThrowsOnNegativeBalance()
    {
        Assert.Throws<ArgumentException>(() => LoanCalculator.CalculateCurrentLtv(-1, 100000));
    }

    [Fact]
    public void CalculateCurrentLtv_FromLoanEntity()
    {
        var loan = Loan.Create(
            "Test Loan", "Lender", 100000m, 70000m, 0.05m,
            InterestType.Fixed, DateTimeOffset.UtcNow, 12, PaymentFrequency.Monthly,
            2m, 50000m, 0.8m, 0.9m, null);

        var ltv = LoanCalculator.CalculateCurrentLtv(loan);
        // 70000 / (2 * 50000) = 0.7
        Assert.Equal(0.7m, ltv);
    }

    [Fact]
    public void CalculateLiquidationPrice_ComputesCorrectly()
    {
        // balance 70000, collateral 2 BTC, liq LTV 0.9 => price = 70000 / (2 * 0.9)
        var price = LoanCalculator.CalculateLiquidationPrice(70000m, 2m, 0.9m);
        Assert.Equal(38888.888888888888888888888889m, price, 10);
    }

    [Fact]
    public void CalculateWarningPrice_ComputesCorrectly()
    {
        var price = LoanCalculator.CalculateWarningPrice(70000m, 2m, 0.8m);
        Assert.Equal(43750m, price);
    }

    [Fact]
    public void CalculateDistanceToWarning_PositiveWhenBufferExists()
    {
        var dist = LoanCalculator.CalculateDistanceToWarning(0.65m, 0.8m);
        Assert.Equal(0.15m, dist);
    }

    [Fact]
    public void CalculateDistanceToLiquidation_PositiveWhenBufferExists()
    {
        var dist = LoanCalculator.CalculateDistanceToLiquidation(0.65m, 0.9m);
        Assert.Equal(0.25m, dist);
    }

    [Fact]
    public void CalculateRemainingCollateralBuffer_ReturnsBufferBtc()
    {
        // balance 70000, collateral 1 BTC, price 50000, warning 0.8
        // needed = 70000 / 0.8 / 50000 = 1.75 BTC
        // buffer = 1.75 - 1 = 0.75
        var buffer = LoanCalculator.CalculateRemainingCollateralBuffer(70000m, 1m, 50000m, 0.8m);
        Assert.Equal(0.75m, buffer);
    }

    [Fact]
    public void CalculateRemainingCollateralBuffer_ReturnsZeroIfAlreadyBreached()
    {
        var buffer = LoanCalculator.CalculateRemainingCollateralBuffer(70000m, 2m, 50000m, 0.8m);
        Assert.Equal(0m, buffer);
    }

    [Fact]
    public void CalculateSimpleInterest_Monthly()
    {
        var interest = LoanCalculator.CalculateSimpleInterest(100000m, 0.12m, 1, PaymentFrequency.Monthly);
        Assert.Equal(1000m, interest); // 12% / 12 = 1%
    }

    [Fact]
    public void CalculateSimpleInterest_Weekly()
    {
        var interest = LoanCalculator.CalculateSimpleInterest(100000m, 0.052m, 1, PaymentFrequency.Weekly);
        Assert.Equal(100m, interest); // 5.2% / 52 ≈ 0.1%
    }
}
