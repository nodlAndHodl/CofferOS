using CofferOS.Application.Abstractions.Persistence;
using CofferOS.Application.Contracts;
using CofferOS.Domain.Common;
using CofferOS.Domain.Retirement;

namespace CofferOS.Application.Retirement;

/// <summary>
/// Service for managing retirement account holdings.
/// </summary>
public sealed class RetirementAccountService
{
    private readonly IRetirementAccountRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public RetirementAccountService(
        IRetirementAccountRepository repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<RetirementAccountDto> CreateAsync(
        CreateRetirementAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        var account = RetirementAccount.Create(
            request.Name,
            request.AccountType,
            request.Provider,
            request.BitcoinAmount,
            request.Currency,
            request.Notes);

        foreach (var entry in request.CostBasisEntries)
        {
            account.AddCostBasisEntry(entry.CostBasis, entry.AcquisitionDate);
        }

        await _repository.AddAsync(account, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(account);
    }

    public async Task<RetirementAccountDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var account = await _repository.GetByIdAsync(id, cancellationToken);
        return account is null ? null : MapToDto(account);
    }

    public async Task<IReadOnlyList<RetirementAccountDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var accounts = await _repository.GetAllAsync(cancellationToken);
        return accounts.Select(MapToDto).ToList();
    }

    public async Task<RetirementAccountDto> UpdateAsync(
        Guid id,
        UpdateRetirementAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        var account = await _repository.GetByIdAsync(id, cancellationToken);
        if (account is null)
            throw new InvalidOperationException($"Retirement account with ID {id} not found.");

        account.UpdateBasicInfo(
            request.Name,
            request.Provider,
            request.BitcoinAmount,
            request.Currency,
            request.Notes);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapToDto(account);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var account = await _repository.GetByIdAsync(id, cancellationToken);
        if (account is null)
            return false;

        _repository.Remove(account);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<RetirementAccountDto> AddCostBasisEntryAsync(
        Guid accountId,
        CostBasisEntryInput entry,
        CancellationToken cancellationToken = default)
    {
        var account = await _repository.GetByIdAsync(accountId, cancellationToken);
        if (account is null)
            throw new InvalidOperationException($"Retirement account with ID {accountId} not found.");

        account.AddCostBasisEntry(entry.CostBasis, entry.AcquisitionDate);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapToDto(account);
    }

    public async Task<RetirementAccountDto> RemoveCostBasisEntryAsync(
        Guid accountId,
        Guid entryId,
        CancellationToken cancellationToken = default)
    {
        var account = await _repository.GetByIdAsync(accountId, cancellationToken);
        if (account is null)
            throw new InvalidOperationException($"Retirement account with ID {accountId} not found.");

        account.RemoveCostBasisEntry(entryId);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapToDto(account);
    }

    private static RetirementAccountDto MapToDto(RetirementAccount account)
    {
        return new RetirementAccountDto
        {
            Id = account.Id,
            Name = account.Name,
            AccountType = account.AccountType,
            Provider = account.Provider,
            BitcoinAmount = account.BitcoinAmount,
            Currency = account.Currency,
            Notes = account.Notes,
            TotalCostBasis = account.GetTotalCostBasis(),
            CostBasisEntries = account.CostBasisEntries
                .Select(e => new RetirementAccountCostBasisDto
                {
                    Id = e.Id,
                    CostBasis = e.CostBasis,
                    AcquisitionDate = e.AcquisitionDate,
                    CreatedAt = e.CreatedAt
                })
                .ToList(),
            CreatedAt = account.CreatedAt,
            UpdatedAt = account.UpdatedAt
        };
    }
}
