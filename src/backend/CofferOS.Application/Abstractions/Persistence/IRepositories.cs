using CofferOS.Domain.Common;
using CofferOS.Domain.Treasury;
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
    Task<IReadOnlyList<Tag>> GetTagsAsync(Guid walletId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Category>> GetCategoriesAsync(Guid walletId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MetadataEntry>> GetMetadataAsync(Guid walletId, CancellationToken cancellationToken = default);

    /// <summary>Most recent transactions across all wallets, paginated.</summary>
    Task<IReadOnlyList<WalletTransaction>> GetRecentTransactionsAsync(int skip, int take, CancellationToken cancellationToken = default);

    Task<int> GetRecentTransactionCountAsync(CancellationToken cancellationToken = default);
}

/// <summary>Read/write access to user-defined metadata (labels, tags, categories, key/value entries) for a target object.</summary>
public interface IMetadataRepository
{
    Task<IReadOnlyList<Label>> GetLabelsAsync(Guid walletId, LabelTarget target, string reference, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Tag>> GetTagsAsync(Guid walletId, LabelTarget target, string reference, CancellationToken cancellationToken = default);
    Task<Category?> GetCategoryAsync(Guid walletId, LabelTarget target, string reference, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MetadataEntry>> GetEntriesAsync(Guid walletId, LabelTarget target, string reference, CancellationToken cancellationToken = default);

    Task AddLabelAsync(Label label, CancellationToken cancellationToken = default);
    Task AddTagAsync(Tag tag, CancellationToken cancellationToken = default);
    Task AddCategoryAsync(Category category, CancellationToken cancellationToken = default);
    Task AddEntryAsync(MetadataEntry entry, CancellationToken cancellationToken = default);

    void RemoveLabel(Label label);
    void RemoveTag(Tag tag);
    void RemoveCategory(Category category);
    void RemoveEntry(MetadataEntry entry);
}

/// <summary>Read/write access to user-recorded wallet timeline events.</summary>
public interface ITimelineEventRepository
{
    Task<TimelineEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TimelineEvent>> GetByWalletAsync(Guid walletId, CancellationToken cancellationToken = default);
    Task AddAsync(TimelineEvent timelineEvent, CancellationToken cancellationToken = default);
    void Remove(TimelineEvent timelineEvent);
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

/// <summary>Read/write access to loans (Bitcoin-collateralized). Standalone aggregate (Phase 1: manual entry).</summary>
public interface ILoanRepository
{
    Task<Loan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Loan>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Loan>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Loan loan, CancellationToken cancellationToken = default);
    void Remove(Loan loan);
}

/// <summary>Read/write access to loan payments. Payments drive derived balances in the accrual model.</summary>
public interface ILoanPaymentRepository
{
    Task<IReadOnlyList<LoanPayment>> GetByLoanAsync(Guid loanId, CancellationToken cancellationToken = default);
    Task AddAsync(LoanPayment payment, CancellationToken cancellationToken = default);
    void Remove(LoanPayment payment);
}

/// <summary>Read/write access to loan price snapshots for historical LTV analysis.</summary>
public interface ILoanPriceSnapshotRepository
{
    Task<IReadOnlyList<LoanPriceSnapshot>> GetByLoanAsync(Guid loanId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LoanPriceSnapshot>> GetByLoanInRangeAsync(Guid loanId, DateTimeOffset startDate, DateTimeOffset endDate, CancellationToken cancellationToken = default);
    Task<LoanPriceSnapshot?> GetLatestByLoanAsync(Guid loanId, CancellationToken cancellationToken = default);
    Task AddAsync(LoanPriceSnapshot snapshot, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<LoanPriceSnapshot> snapshots, CancellationToken cancellationToken = default);
    void Remove(LoanPriceSnapshot snapshot);
}

/// <summary>Transactional boundary. Committing also dispatches buffered domain events.</summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
