using CofferOS.Application.Abstractions.Persistence;
using CofferOS.Domain.Wallets;
using Microsoft.EntityFrameworkCore;

namespace CofferOS.Infrastructure.Persistence.Repositories;

public sealed class WalletReadStore : IWalletReadStore
{
    private readonly CofferOSDbContext _db;

    public WalletReadStore(CofferOSDbContext db) => _db = db;

    public async Task<IReadOnlyList<Address>> GetAddressesAsync(Guid walletId, CancellationToken cancellationToken = default) =>
        await _db.Addresses.AsNoTracking()
            .Where(a => a.WalletId == walletId)
            .OrderBy(a => a.IsChange).ThenBy(a => a.DerivationIndex)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<WalletTransaction>> GetTransactionsAsync(Guid walletId, CancellationToken cancellationToken = default) =>
        await _db.Transactions.AsNoTracking()
            .Where(t => t.WalletId == walletId)
            .OrderByDescending(t => t.Timestamp)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Utxo>> GetUtxosAsync(Guid walletId, CancellationToken cancellationToken = default) =>
        await _db.Utxos.AsNoTracking()
            .Where(u => u.WalletId == walletId && !u.IsSpent)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Label>> GetLabelsAsync(Guid walletId, CancellationToken cancellationToken = default) =>
        await _db.Labels.AsNoTracking()
            .Where(l => l.WalletId == walletId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Note>> GetNotesAsync(Guid walletId, CancellationToken cancellationToken = default) =>
        await _db.Notes.AsNoTracking()
            .Where(n => n.WalletId == walletId)
            .OrderByDescending(n => n.UpdatedAt)
            .ToListAsync(cancellationToken);
}
