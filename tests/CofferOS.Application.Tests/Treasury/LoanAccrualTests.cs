using CofferOS.Application.Treasury;
using CofferOS.Domain.Common;
using CofferOS.Domain.Treasury;
using Xunit;

namespace CofferOS.Application.Tests.Treasury;

public class LoanAccrualTests
{
    private static Loan CreateLoan(decimal principal, decimal rate, DateTimeOffset startDate)
    {
        return Loan.Create(
            "Test Loan",
            null,
            principal,
            principal,
            rate,
            InterestType.Fixed,
            startDate,
            12,
            PaymentFrequency.Monthly,
            1m,
            50000m,
            0.8m,
            0.9m,
            null);
    }

    [Fact]
    public void Loan_Create_InitializesAccruedInterestToZero()
    {
        var loan = CreateLoan(2500m, 0.1149m, new DateTimeOffset(2026, 5, 7, 0, 0, 0, TimeSpan.Zero));

        Assert.Equal(0m, loan.AccruedInterest);
        Assert.Equal(loan.LoanStartDate, loan.LastAccruedOn);
        Assert.Equal(2500m, loan.CurrentBalance);
    }

    [Fact]
    public async Task LoanAccrualService_CalculateAsync_AccruesDailyInterestOverSeveralDays()
    {
        var service = new LoanAccrualService(null!, null!, null!);
        var startDate = new DateTimeOffset(2026, 5, 7, 0, 0, 0, TimeSpan.Zero);
        var loan = CreateLoan(2500m, 0.1149m, startDate);
        var asOf = startDate.AddDays(79);

        var snapshot = await service.CalculateAsync(loan, Array.Empty<LoanPayment>(), asOf);

        // Use the same arithmetic as the accrual engine.
        var expectedAccrued = 2500m * (0.1149m / 365m) * 79m;
        var expectedDaily = 2500m * (0.1149m / 365m);

        Assert.Equal(expectedAccrued, snapshot.AccruedInterest);
        Assert.Equal(2500m + expectedAccrued, snapshot.CurrentBalance);
        Assert.Equal(Math.Round(expectedDaily, 10), Math.Round(snapshot.DailyInterest, 10));
    }

    [Fact]
    public async Task LoanAccrualService_CalculateAsync_NoAccrualOnStartDate()
    {
        var service = new LoanAccrualService(null!, null!, null!);
        var startDate = new DateTimeOffset(2026, 5, 7, 0, 0, 0, TimeSpan.Zero);
        var loan = CreateLoan(2500m, 0.1149m, startDate);

        var snapshot = await service.CalculateAsync(loan, Array.Empty<LoanPayment>(), startDate);

        Assert.Equal(0m, snapshot.AccruedInterest);
        Assert.Equal(2500m, snapshot.CurrentBalance);
    }

    [Fact]
    public async Task LoanAccrualService_CalculateAsync_HonorsPrincipalPayments()
    {
        var service = new LoanAccrualService(null!, null!, null!);
        var startDate = new DateTimeOffset(2026, 5, 7, 0, 0, 0, TimeSpan.Zero);
        var loan = CreateLoan(2500m, 0.1149m, startDate);
        var payment = new LoanPayment(loan.Id, startDate.AddDays(10), 500m, 500m, 0m, null);

        var asOf = startDate.AddDays(20);
        var snapshot = await service.CalculateAsync(loan, new[] { payment }, asOf);

        // Outstanding principal 2000, 20 days from start at 0.1149/365
        var expectedAccrued = 2000m * (0.1149m / 365m) * 20m;
        Assert.Equal(2000m, snapshot.OutstandingPrincipal);
        Assert.Equal(expectedAccrued, snapshot.AccruedInterest);
        Assert.Equal(snapshot.OutstandingPrincipal + snapshot.AccruedInterest, snapshot.CurrentBalance);
    }

    [Fact]
    public void Loan_ResetAccrual_ClearsAccruedInterestAndResetsBalance()
    {
        var startDate = new DateTimeOffset(2026, 5, 7, 0, 0, 0, TimeSpan.Zero);
        var loan = CreateLoan(2500m, 0.1149m, startDate);
        loan.AddAccruedInterest(100m, startDate.AddDays(10));

        var newStart = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);
        loan.ResetAccrual(newStart);

        Assert.Equal(0m, loan.AccruedInterest);
        Assert.Equal(newStart, loan.LastAccruedOn);
        Assert.Equal(2500m, loan.CurrentBalance);
    }

    [Fact]
    public void Loan_AddAccruedInterest_UpdatesCurrentBalance()
    {
        var startDate = new DateTimeOffset(2026, 5, 7, 0, 0, 0, TimeSpan.Zero);
        var loan = CreateLoan(2500m, 0.1149m, startDate);

        var accrued = 2500m * (0.1149m / 365m) * 79m;
        loan.AddAccruedInterest(accrued, startDate.AddDays(79));

        Assert.Equal(accrued, loan.AccruedInterest);
        Assert.Equal(2500m + accrued, loan.CurrentBalance);
    }

    [Fact]
    public void Loan_RefreshCurrentBalance_RecomputesFromPrincipalAndAccrued()
    {
        var startDate = new DateTimeOffset(2026, 5, 7, 0, 0, 0, TimeSpan.Zero);
        var loan = CreateLoan(2500m, 0.1149m, startDate);

        // Simulate a payment reducing principal with no new accrual
        loan.ReducePrincipal(500m, startDate);
        loan.RefreshCurrentBalance();

        Assert.Equal(2000m, loan.CurrentBalance);
    }
}
