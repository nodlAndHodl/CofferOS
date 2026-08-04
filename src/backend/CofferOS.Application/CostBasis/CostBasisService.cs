using CofferOS.Application.Abstractions.Persistence;
using CofferOS.Domain.Common;

namespace CofferOS.Application.CostBasis;

/// <summary>
/// Manages user-provided cost basis (total fiat paid) for UTXOs and loan collateral.
/// Stored amounts are interpreted in the app-wide display currency (USD by default today).
/// Missing entries default to 0 when queried.
/// </summary>
public sealed class CostBasisService
{
    private readonly ICostBasisRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CostBasisService(ICostBasisRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task SetAsync(CostBasisTarget target, string reference, decimal amount, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reference))
            throw new ArgumentException("Reference is required.", nameof(reference));
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative.", nameof(amount));

        var trimmed = reference.Trim();

        if (amount == 0)
        {
            await ClearAsync(target, trimmed, cancellationToken);
            return;
        }

        var existing = await _repository.GetAsync(target, trimmed, cancellationToken);
        if (existing is not null)
        {
            existing.UpdateAmount(amount);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        else
        {
            var entry = new CostBasisEntry(target, trimmed, amount);
            await _repository.AddAsync(entry, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task ClearAsync(CostBasisTarget target, string reference, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reference))
            throw new ArgumentException("Reference is required.", nameof(reference));

        var existing = await _repository.GetAsync(target, reference.Trim(), cancellationToken);
        if (existing is null)
            return;

        _repository.Remove(existing);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<decimal> GetAsync(CostBasisTarget target, string reference, CancellationToken cancellationToken = default)
    {
        var entry = await _repository.GetAsync(target, reference, cancellationToken);
        return entry?.Amount ?? 0m;
    }

    public async Task<decimal> GetByReferenceAsync(CostBasisTarget target, string reference, CancellationToken cancellationToken = default)
    {
        var entry = await _repository.GetAsync(target, reference, cancellationToken);
        return entry?.Amount ?? 0m;
    }

    public async Task<IReadOnlyDictionary<string, decimal>> GetByReferencesAsync(
        CostBasisTarget target,
        IReadOnlyList<string> references,
        CancellationToken cancellationToken = default)
    {
        var entries = await _repository.GetByReferencesAsync(target, references, cancellationToken);
        var dict = new Dictionary<string, decimal>(StringComparer.Ordinal);
        foreach (var e in entries)
            dict[e.Reference] = e.Amount;

        // Ensure every requested reference has a default of 0.
        foreach (var r in references)
        {
            if (!dict.ContainsKey(r))
                dict[r] = 0m;
        }

        return dict;
    }

    public async Task<IReadOnlyDictionary<string, decimal>> GetByTargetAsync(CostBasisTarget target, CancellationToken cancellationToken = default)
    {
        var entries = await _repository.GetByTargetAsync(target, cancellationToken);
        return entries.ToDictionary(e => e.Reference, e => e.Amount, StringComparer.Ordinal);
    }
}
