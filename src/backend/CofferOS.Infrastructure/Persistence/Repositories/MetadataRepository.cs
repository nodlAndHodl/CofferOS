using CofferOS.Application.Abstractions.Persistence;
using CofferOS.Domain.Common;
using CofferOS.Domain.Wallets;
using Microsoft.EntityFrameworkCore;

namespace CofferOS.Infrastructure.Persistence.Repositories;

public sealed class MetadataRepository : IMetadataRepository
{
    private readonly CofferOSDbContext _db;

    public MetadataRepository(CofferOSDbContext db) => _db = db;

    public async Task<IReadOnlyList<Label>> GetLabelsAsync(Guid walletId, LabelTarget target, string reference, CancellationToken cancellationToken = default) =>
        await _db.Labels
            .Where(l => l.WalletId == walletId && l.Target == target && l.Reference == reference)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Tag>> GetTagsAsync(Guid walletId, LabelTarget target, string reference, CancellationToken cancellationToken = default) =>
        await _db.Tags
            .Where(t => t.WalletId == walletId && t.Target == target && t.Reference == reference)
            .OrderBy(t => t.Value)
            .ToListAsync(cancellationToken);

    public Task<Category?> GetCategoryAsync(Guid walletId, LabelTarget target, string reference, CancellationToken cancellationToken = default) =>
        _db.Categories
            .FirstOrDefaultAsync(c => c.WalletId == walletId && c.Target == target && c.Reference == reference, cancellationToken);

    public async Task<IReadOnlyList<MetadataEntry>> GetEntriesAsync(Guid walletId, LabelTarget target, string reference, CancellationToken cancellationToken = default) =>
        await _db.MetadataEntries
            .Where(m => m.WalletId == walletId && m.Target == target && m.Reference == reference)
            .OrderBy(m => m.Key)
            .ToListAsync(cancellationToken);

    public async Task AddLabelAsync(Label label, CancellationToken cancellationToken = default) =>
        await _db.Labels.AddAsync(label, cancellationToken);

    public async Task AddTagAsync(Tag tag, CancellationToken cancellationToken = default) =>
        await _db.Tags.AddAsync(tag, cancellationToken);

    public async Task AddCategoryAsync(Category category, CancellationToken cancellationToken = default) =>
        await _db.Categories.AddAsync(category, cancellationToken);

    public async Task AddEntryAsync(MetadataEntry entry, CancellationToken cancellationToken = default) =>
        await _db.MetadataEntries.AddAsync(entry, cancellationToken);

    public void RemoveLabel(Label label) => _db.Labels.Remove(label);
    public void RemoveTag(Tag tag) => _db.Tags.Remove(tag);
    public void RemoveCategory(Category category) => _db.Categories.Remove(category);
    public void RemoveEntry(MetadataEntry entry) => _db.MetadataEntries.Remove(entry);
}
