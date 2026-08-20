using CofferOS.Domain.Common;

namespace CofferOS.Domain.Retirement;

/// <summary>
/// A Bitcoin holding in a retirement account (IRA, 401k, etc.).
/// All values are entered manually; no exchange integration.
/// </summary>
public sealed class RetirementAccount : Entity
{
    private RetirementAccount() { }

    private RetirementAccount(
        string name,
        RetirementAccountType accountType,
        string provider,
        decimal bitcoinAmount,
        string currency,
        string? notes)
    {
        Name = name;
        AccountType = accountType;
        Provider = provider;
        BitcoinAmount = bitcoinAmount;
        Currency = currency;
        Notes = notes;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public string Name { get; private set; } = string.Empty;
    public RetirementAccountType AccountType { get; private set; }
    public string Provider { get; private set; } = string.Empty;
    public decimal BitcoinAmount { get; private set; }
    public string Currency { get; private set; } = "USD";
    public string? Notes { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private readonly List<RetirementAccountCostBasis> _costBasisEntries = new();
    public IReadOnlyCollection<RetirementAccountCostBasis> CostBasisEntries => _costBasisEntries.AsReadOnly();

    /// <summary>Factory for creating a new retirement account.</summary>
    public static RetirementAccount Create(
        string name,
        RetirementAccountType accountType,
        string provider,
        decimal bitcoinAmount,
        string currency = "USD",
        string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Account name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(provider))
            throw new ArgumentException("Provider is required.", nameof(provider));
        if (bitcoinAmount < 0)
            throw new ArgumentException("Bitcoin amount cannot be negative.", nameof(bitcoinAmount));

        return new RetirementAccount(
            name.Trim(),
            accountType,
            provider.Trim(),
            bitcoinAmount,
            string.IsNullOrWhiteSpace(currency) ? "USD" : currency.Trim().ToUpperInvariant(),
            notes?.Trim());
    }

    public void UpdateBasicInfo(string name, string provider, decimal bitcoinAmount, string currency, string? notes)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Account name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(provider))
            throw new ArgumentException("Provider is required.", nameof(provider));
        if (bitcoinAmount < 0)
            throw new ArgumentException("Bitcoin amount cannot be negative.", nameof(bitcoinAmount));

        Name = name.Trim();
        Provider = provider.Trim();
        BitcoinAmount = bitcoinAmount;
        Currency = string.IsNullOrWhiteSpace(currency) ? "USD" : currency.Trim().ToUpperInvariant();
        Notes = notes?.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void AddCostBasisEntry(decimal costBasis, DateTimeOffset acquisitionDate)
    {
        if (costBasis < 0)
            throw new ArgumentException("Cost basis cannot be negative.", nameof(costBasis));

        var entry = new RetirementAccountCostBasis(Id, costBasis, acquisitionDate);
        _costBasisEntries.Add(entry);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void RemoveCostBasisEntry(Guid entryId)
    {
        var entry = _costBasisEntries.FirstOrDefault(e => e.Id == entryId);
        if (entry is not null)
        {
            _costBasisEntries.Remove(entry);
            UpdatedAt = DateTimeOffset.UtcNow;
        }
    }

    public decimal GetTotalCostBasis() => _costBasisEntries.Sum(e => e.CostBasis);
}
