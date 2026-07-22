using CofferOS.Application.Abstractions.Persistence;
using CofferOS.Domain.Wallets;
using Microsoft.EntityFrameworkCore;

namespace CofferOS.Infrastructure.Persistence.Repositories;

public sealed class NoteRepository : INoteRepository
{
    private readonly CofferOSDbContext _db;

    public NoteRepository(CofferOSDbContext db) => _db = db;

    public Task<Note?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.Notes.FirstOrDefaultAsync(n => n.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Note>> GetByWalletAsync(Guid walletId, CancellationToken cancellationToken = default) =>
        await _db.Notes
            .Where(n => n.WalletId == walletId)
            .OrderByDescending(n => n.UpdatedAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Note note, CancellationToken cancellationToken = default) =>
        await _db.Notes.AddAsync(note, cancellationToken);

    public void Remove(Note note) => _db.Notes.Remove(note);
}
