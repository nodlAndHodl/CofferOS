using CofferOS.Domain.Wallets;

namespace CofferOS.Application.Abstractions.Persistence;

/// <summary>Read/write access to the Wallet aggregate.</summary>
public interface IWalletRepository
{
    Task<Wallet?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Loads a wallet with its descriptors and derived addresses.</summary>
    Task<Wallet?> GetByIdWithDescriptorsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Wallet>> GetAllAsync(CancellationToken cancellationToken = default);

    Task AddAsync(Wallet wallet, CancellationToken cancellationToken = default);

    void Remove(Wallet wallet);

    /// <summary>Replaces all UTXOs for a wallet with a new scan result.</summary>
    Task ReplaceUtxosAsync(Guid walletId, IReadOnlyList<Utxo> utxos, CancellationToken cancellationToken = default);

    /// <summary>Replaces all transactions for a wallet with a new history result.</summary>
    Task ReplaceTransactionsAsync(Guid walletId, IReadOnlyList<WalletTransaction> transactions, CancellationToken cancellationToken = default);
}

/// <summary>Read access to derived / observed wallet data used by query services.</summary>
public interface IWalletReadStore
{
    Task<IReadOnlyList<Address>> GetAddressesAsync(Guid walletId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WalletTransaction>> GetTransactionsAsync(Guid walletId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Utxo>> GetUtxosAsync(Guid walletId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Label>> GetLabelsAsync(Guid walletId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Note>> GetNotesAsync(Guid walletId, CancellationToken cancellationToken = default);
}

/// <summary>Read/write access to derived wallet addresses.</summary>
public interface IAddressRepository
{
    Task<IReadOnlyList<Address>> GetByWalletAsync(Guid walletId, CancellationToken cancellationToken = default);
    Task ReplaceAddressesAsync(Guid walletId, IReadOnlyList<Address> addresses, CancellationToken cancellationToken = default);
}

/// <summary>Read/write access to wallet notes.</summary>
public interface INoteRepository
{
    Task<Note?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Note>> GetByWalletAsync(Guid walletId, CancellationToken cancellationToken = default);
    Task AddAsync(Note note, CancellationToken cancellationToken = default);
    void Remove(Note note);
}

/// <summary>Transactional boundary. Committing also dispatches buffered domain events.</summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
