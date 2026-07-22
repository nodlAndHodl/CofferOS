using CofferOS.Application.Abstractions.Persistence;
using CofferOS.Domain.Wallets;
using Microsoft.EntityFrameworkCore;

namespace CofferOS.Infrastructure.Persistence.Repositories;

public sealed class AddressRepository : IAddressRepository
{
    private readonly CofferOSDbContext _db;

    public AddressRepository(CofferOSDbContext db) => _db = db;

    public async Task<IReadOnlyList<Address>> GetByWalletAsync(Guid walletId, CancellationToken cancellationToken = default) =>
        await _db.Addresses.AsNoTracking()
            .Where(a => a.WalletId == walletId)
            .OrderBy(a => a.IsChange).ThenBy(a => a.DerivationIndex)
            .ToListAsync(cancellationToken);

    public async Task ReplaceAddressesAsync(Guid walletId, IReadOnlyList<Address> addresses, CancellationToken cancellationToken = default)
    {
        var existing = await _db.Addresses.Where(a => a.WalletId == walletId).ToListAsync(cancellationToken);
        _db.Addresses.RemoveRange(existing);
        await _db.Addresses.AddRangeAsync(addresses, cancellationToken);
    }
}
