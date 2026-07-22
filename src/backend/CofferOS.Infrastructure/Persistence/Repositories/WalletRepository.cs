using CofferOS.Application.Abstractions.Persistence;
using CofferOS.Domain.Wallets;
using Microsoft.EntityFrameworkCore;

namespace CofferOS.Infrastructure.Persistence.Repositories;

public sealed class WalletRepository : IWalletRepository
{
    private readonly CofferOSDbContext _db;

    public WalletRepository(CofferOSDbContext db) => _db = db;

    public Task<Wallet?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.Wallets.FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

    public Task<Wallet?> GetByIdWithDescriptorsAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.Wallets
            .Include(w => w.Descriptors)
            .ThenInclude(d => d.Addresses)
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Wallet>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _db.Wallets
            .Include(w => w.Descriptors)
            .OrderBy(w => w.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Wallet wallet, CancellationToken cancellationToken = default) =>
        await _db.Wallets.AddAsync(wallet, cancellationToken);

    public void Remove(Wallet wallet)
    {
        var labels = _db.Labels.Where(l => l.WalletId == wallet.Id);
        var notes = _db.Notes.Where(n => n.WalletId == wallet.Id);
        _db.Labels.RemoveRange(labels);
        _db.Notes.RemoveRange(notes);
        _db.Wallets.Remove(wallet);
    }

    public async Task ReplaceUtxosAsync(Guid walletId, IReadOnlyList<Utxo> utxos, CancellationToken cancellationToken = default)
    {
        var existing = await _db.Utxos.Where(u => u.WalletId == walletId).ToListAsync(cancellationToken);
        _db.Utxos.RemoveRange(existing);
        await _db.Utxos.AddRangeAsync(utxos, cancellationToken);
    }

    public async Task ReplaceTransactionsAsync(Guid walletId, IReadOnlyList<WalletTransaction> transactions, CancellationToken cancellationToken = default)
    {
        var existing = await _db.Transactions.Where(t => t.WalletId == walletId).ToListAsync(cancellationToken);
        _db.Transactions.RemoveRange(existing);
        await _db.Transactions.AddRangeAsync(transactions, cancellationToken);
    }
}
