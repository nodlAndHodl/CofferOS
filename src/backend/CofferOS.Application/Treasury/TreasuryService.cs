using CofferOS.Application.Abstractions.Persistence;
using CofferOS.Application.Abstractions.Providers;
using CofferOS.Application.Contracts;
using CofferOS.Domain.Common;
using CofferOS.Domain.Treasury;

namespace CofferOS.Application.Treasury;

/// <summary>
/// Use-case service for managing Bitcoin-collateralized loans (Phase 1: manual).
/// All calculations are performed via LoanCalculator to keep logic centralized and testable.
/// </summary>
public sealed class TreasuryService
{
    private readonly ILoanRepository _loans;
    private readonly ILoanPaymentRepository _payments;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBitcoinPriceProvider _priceProvider;
    private readonly IMutableBitcoinPriceSource? _mutablePriceSource;
    private readonly ILoanAccrualService _accrual;

    public TreasuryService(
        ILoanRepository loans,
        ILoanPaymentRepository payments,
        IUnitOfWork unitOfWork,
        IBitcoinPriceProvider priceProvider,
        ILoanAccrualService accrual,
        IMutableBitcoinPriceSource? mutablePriceSource = null)
    {
        _loans = loans;
        _payments = payments;
        _unitOfWork = unitOfWork;
        _priceProvider = priceProvider;
        _accrual = accrual;
        _mutablePriceSource = mutablePriceSource;
    }

    public async Task<IReadOnlyList<LoanSummaryDto>> GetSummariesAsync(CancellationToken cancellationToken = default)
    {
        var loans = await _loans.GetAllAsync(cancellationToken);
        var results = new List<LoanSummaryDto>(loans.Count);
        foreach (var loan in loans)
        {
            var pays = await _payments.GetByLoanAsync(loan.Id, cancellationToken);
            var snap = await _accrual.CalculateAsync(loan, pays, null, cancellationToken);
            results.Add(ToSummary(loan, snap));
        }
        return results;
    }

    public async Task<LoanDetailDto?> GetDetailAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var loan = await _loans.GetByIdAsync(id, cancellationToken);
        if (loan is null) return null;
        var pays = await _payments.GetByLoanAsync(loan.Id, cancellationToken);
        var snap = await _accrual.CalculateAsync(loan, pays, null, cancellationToken);
        return ToDetail(loan, snap);
    }

    public async Task<LoanSummaryDto> CreateAsync(CreateLoanRequest request, CancellationToken cancellationToken = default)
    {
        var interestType = ParseInterestType(request.InterestType);
        var paymentFreq = ParsePaymentFrequency(request.PaymentFrequency);

        var loan = Loan.Create(
            request.Name,
            request.Lender,
            request.PrincipalAmount,
            request.CurrentBalance,
            request.InterestRate,
            interestType,
            request.LoanStartDate,
            request.LoanTermMonths,
            paymentFreq,
            request.CollateralAmountBtc,
            request.CurrentBtcPrice,
            request.WarningLtv,
            request.LiquidationLtv,
            request.Notes);

        await _loans.AddAsync(loan, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToSummary(loan);
    }

    public async Task<LoanDetailDto?> UpdateAsync(Guid id, UpdateLoanRequest request, CancellationToken cancellationToken = default)
    {
        var loan = await _loans.GetByIdAsync(id, cancellationToken);
        if (loan is null) return null;

        var interestType = ParseInterestType(request.InterestType);
        var paymentFreq = ParsePaymentFrequency(request.PaymentFrequency);

        loan.UpdateDetails(
            request.Name,
            request.Lender,
            request.PrincipalAmount,
            request.CurrentBalance,
            request.InterestRate,
            interestType,
            request.LoanStartDate,
            request.LoanTermMonths,
            paymentFreq,
            request.CollateralAmountBtc,
            request.CurrentBtcPrice,
            request.WarningLtv,
            request.LiquidationLtv,
            request.Notes);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDetail(loan);
    }

    public async Task<LoanDetailDto?> UpdateBalanceAsync(Guid id, UpdateLoanBalanceRequest request, CancellationToken cancellationToken = default)
    {
        var loan = await _loans.GetByIdAsync(id, cancellationToken);
        if (loan is null) return null;

        loan.UpdateBalance(request.CurrentBalance);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDetail(loan);
    }

    public async Task<LoanDetailDto?> UpdateCollateralAsync(Guid id, UpdateLoanCollateralRequest request, CancellationToken cancellationToken = default)
    {
        var loan = await _loans.GetByIdAsync(id, cancellationToken);
        if (loan is null) return null;

        loan.UpdateCollateral(request.CollateralAmountBtc, request.CurrentBtcPrice);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDetail(loan);
    }

    public async Task<bool> SetBtcPriceAsync(SetBtcPriceRequest request, CancellationToken cancellationToken = default)
    {
        if (_mutablePriceSource is null)
        {
            // If no mutable source, we can still update all active loans' cached price directly.
            // For Phase 1 with Manual provider, mutable source should exist.
            // Fall back to updating active loans' price snapshots.
        }
        else
        {
            _mutablePriceSource.SetPrice(request.Price);
        }

        // Also update the cached price on active loans so their calculations reflect the new price.
        var active = await _loans.GetActiveAsync(cancellationToken);
        foreach (var loan in active)
        {
            loan.UpdatePrice(request.Price);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var loan = await _loans.GetByIdAsync(id, cancellationToken);
        if (loan is null) return false;

        _loans.Remove(loan);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<TreasurySummaryDto> GetTreasurySummaryAsync(CancellationToken cancellationToken = default)
    {
        var active = await _loans.GetActiveAsync(cancellationToken);

        decimal totalBalance = 0m;
        decimal totalCollateralBtc = 0m;
        decimal totalCollateralValue = 0m;
        decimal sumLtvWeighted = 0m; // for simple avg we will average the LTVs directly

        LoanSummaryDto? highestRisk = null;
        decimal? highestLtv = null;

        foreach (var loan in active)
        {
            var pays = await _payments.GetByLoanAsync(loan.Id, cancellationToken);
            var snap = await _accrual.CalculateAsync(loan, pays, null, cancellationToken);

            totalBalance += snap.CurrentBalance;
            totalCollateralBtc += loan.CollateralAmountBtc;
            var cv = LoanCalculator.CalculateCollateralValue(loan.CollateralAmountBtc, loan.CurrentBtcPrice);
            totalCollateralValue += cv;

            if (cv > 0)
            {
                // accumulate for avg LTV
                // we will compute average after the loop using count
            }

            var ltv = snap.CurrentLtv;
            if (highestLtv is null || ltv > highestLtv)
            {
                highestLtv = ltv;
                highestRisk = ToSummary(loan, snap);
            }
        }

        var activeCount = active.Count;
        decimal avgLtv = 0m;
        if (activeCount > 0 && totalCollateralValue > 0)
        {
            avgLtv = totalBalance / totalCollateralValue;
        }

        var price = await _priceProvider.GetCurrentPriceAsync(cancellationToken);

        return new TreasurySummaryDto(
            activeCount,
            totalBalance,
            totalCollateralBtc,
            totalCollateralValue,
            avgLtv,
            highestRisk,
            price,
            _priceProvider.ProviderId);
    }

    private static LoanSummaryDto ToSummary(Loan loan, LoanAccrualSnapshot? snap = null)
    {
        decimal balance = snap?.CurrentBalance ?? loan.CurrentBalance;
        var collateralValue = LoanCalculator.CalculateCollateralValue(loan.CollateralAmountBtc, loan.CurrentBtcPrice);
        var ltv = LoanCalculator.CalculateCurrentLtv(balance, collateralValue);
        var distWarn = LoanCalculator.CalculateDistanceToWarning(ltv, loan.WarningLtv);
        var distLiq = LoanCalculator.CalculateDistanceToLiquidation(ltv, loan.LiquidationLtv);

        return new LoanSummaryDto(
            loan.Id,
            loan.Name,
            loan.Lender,
            loan.Status.ToString(),
            loan.PrincipalAmount,
            balance,
            loan.InterestRate,
            loan.InterestType.ToString(),
            loan.CollateralAmountBtc,
            loan.CurrentBtcPrice,
            collateralValue,
            ltv,
            loan.WarningLtv,
            loan.LiquidationLtv,
            distWarn,
            distLiq,
            loan.CreatedAt,
            loan.UpdatedAt);
    }

    private static LoanDetailDto ToDetail(Loan loan, LoanAccrualSnapshot? snap = null)
    {
        decimal balance = snap?.CurrentBalance ?? loan.CurrentBalance;
        var collateralValue = LoanCalculator.CalculateCollateralValue(loan.CollateralAmountBtc, loan.CurrentBtcPrice);
        var ltv = LoanCalculator.CalculateCurrentLtv(balance, collateralValue);
        var warnPrice = LoanCalculator.CalculateWarningPrice(balance, loan.CollateralAmountBtc, loan.WarningLtv);
        var liqPrice = LoanCalculator.CalculateLiquidationPrice(balance, loan.CollateralAmountBtc, loan.LiquidationLtv);
        var distWarn = LoanCalculator.CalculateDistanceToWarning(ltv, loan.WarningLtv);
        var distLiq = LoanCalculator.CalculateDistanceToLiquidation(ltv, loan.LiquidationLtv);
        var buffer = LoanCalculator.CalculateRemainingCollateralBuffer(balance, loan.CollateralAmountBtc, loan.CurrentBtcPrice, loan.WarningLtv);

        return new LoanDetailDto(
            loan.Id,
            loan.Name,
            loan.Lender,
            loan.Status.ToString(),
            loan.Notes,
            loan.PrincipalAmount,
            balance,
            loan.InterestRate,
            loan.InterestType.ToString(),
            loan.LoanStartDate,
            loan.LoanTermMonths,
            loan.PaymentFrequency.ToString(),
            loan.CollateralAmountBtc,
            loan.CurrentBtcPrice,
            collateralValue,
            ltv,
            loan.WarningLtv,
            loan.LiquidationLtv,
            warnPrice,
            liqPrice,
            distWarn,
            distLiq,
            buffer,
            loan.CreatedAt,
            loan.UpdatedAt);
    }

    private static InterestType ParseInterestType(string value)
    {
        if (Enum.TryParse<InterestType>(value, true, out var t)) return t;
        throw new ArgumentException($"Invalid interest type: {value}");
    }

    private static PaymentFrequency ParsePaymentFrequency(string value)
    {
        if (Enum.TryParse<PaymentFrequency>(value, true, out var f)) return f;
        throw new ArgumentException($"Invalid payment frequency: {value}");
    }
}
