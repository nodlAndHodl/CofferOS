using System.Text.Json.Serialization;
using CofferOS.Domain.Common;

namespace CofferOS.Application.Contracts;

/// <summary>DTO for creating a new retirement account.</summary>
public sealed class CreateRetirementAccountRequest
{
    public string Name { get; init; } = string.Empty;
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public RetirementAccountType AccountType { get; init; }
    public string Provider { get; init; } = string.Empty;
    public decimal BitcoinAmount { get; init; }
    public string Currency { get; init; } = "USD";
    public string? Notes { get; init; }
    public List<CostBasisEntryInput> CostBasisEntries { get; init; } = [];
}

/// <summary>DTO for updating a retirement account.</summary>
public sealed class UpdateRetirementAccountRequest
{
    public string Name { get; init; } = string.Empty;
    public string Provider { get; init; } = string.Empty;
    public decimal BitcoinAmount { get; init; }
    public string Currency { get; init; } = "USD";
    public string? Notes { get; init; }
}

/// <summary>Input for a cost basis entry.</summary>
public sealed class CostBasisEntryInput
{
    public decimal CostBasis { get; init; }
    public DateTimeOffset AcquisitionDate { get; init; }
}

/// <summary>DTO for a cost basis entry in a retirement account.</summary>
public sealed class RetirementAccountCostBasisDto
{
    public Guid Id { get; init; }
    public decimal CostBasis { get; init; }
    public DateTimeOffset AcquisitionDate { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

/// <summary>DTO for a retirement account.</summary>
public sealed class RetirementAccountDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public RetirementAccountType AccountType { get; init; }
    public string Provider { get; init; } = string.Empty;
    public decimal BitcoinAmount { get; init; }
    public string Currency { get; init; } = "USD";
    public string? Notes { get; init; }
    public decimal TotalCostBasis { get; init; }
    public IReadOnlyList<RetirementAccountCostBasisDto> CostBasisEntries { get; init; } = [];
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}
