using CofferOS.Application.Abstractions.Persistence;
using CofferOS.Application.Contracts;
using CofferOS.Domain.Common;
using CofferOS.Domain.Wallets;

namespace CofferOS.Application.Wallets;

/// <summary>
/// Manages user-defined metadata for addresses, transactions, UTXOs and wallets.
/// Labels, categories, tags and custom key/value entries are stored locally in SQLite.
/// Notes remain on their own endpoints; this service only reads them when building a
/// full metadata view for an object.
/// </summary>
public sealed class TransactionMetadataService
{
    private readonly IMetadataRepository _metadata;
    private readonly INoteRepository _notes;
    private readonly IUnitOfWork _unitOfWork;

    public TransactionMetadataService(IMetadataRepository metadata, INoteRepository notes, IUnitOfWork unitOfWork)
    {
        _metadata = metadata;
        _notes = notes;
        _unitOfWork = unitOfWork;
    }

    public async Task<ObjectMetadataDto> GetForObjectAsync(
        Guid walletId,
        LabelTarget target,
        string reference,
        CancellationToken cancellationToken = default)
    {
        var labels = await _metadata.GetLabelsAsync(walletId, target, reference, cancellationToken);
        var tags = await _metadata.GetTagsAsync(walletId, target, reference, cancellationToken);
        var category = await _metadata.GetCategoryAsync(walletId, target, reference, cancellationToken);
        var entries = await _metadata.GetEntriesAsync(walletId, target, reference, cancellationToken);
        var notes = await _notes.GetByWalletAsync(walletId, cancellationToken);

        var noteDtos = notes
            .Where(n => n.Target == target && n.Reference == reference)
            .Select(n => new NoteDto(n.Id, n.Target.ToString(), n.Reference, n.Content, n.CreatedAt, n.UpdatedAt))
            .ToList();

        return new ObjectMetadataDto(
            target.ToString(),
            reference,
            labels.FirstOrDefault()?.Text,
            category?.Name,
            tags.Select(t => t.Value).ToList(),
            entries.ToDictionary(e => e.Key, e => e.Value, StringComparer.OrdinalIgnoreCase),
            noteDtos);
    }

    public async Task UpdateForObjectAsync(
        Guid walletId,
        LabelTarget target,
        string reference,
        UpdateMetadataRequest request,
        CancellationToken cancellationToken = default)
    {
        // Label: one per object; null/whitespace clears it.
        var existingLabels = await _metadata.GetLabelsAsync(walletId, target, reference, cancellationToken);
        foreach (var l in existingLabels)
            _metadata.RemoveLabel(l);

        if (!string.IsNullOrWhiteSpace(request.Label))
            await _metadata.AddLabelAsync(new Label(walletId, target, reference, request.Label.Trim()), cancellationToken);

        // Category: one per object; null/whitespace clears it.
        var existingCategory = await _metadata.GetCategoryAsync(walletId, target, reference, cancellationToken);
        if (existingCategory is not null)
            _metadata.RemoveCategory(existingCategory);

        if (!string.IsNullOrWhiteSpace(request.Category))
            await _metadata.AddCategoryAsync(new Category(walletId, target, reference, request.Category.Trim()), cancellationToken);

        // Tags: if supplied, full replace.
        if (request.Tags is not null)
        {
            var existingTags = await _metadata.GetTagsAsync(walletId, target, reference, cancellationToken);
            foreach (var t in existingTags)
                _metadata.RemoveTag(t);

            foreach (var value in request.Tags.Select(Tag.Normalize).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(value))
                    await _metadata.AddTagAsync(new Tag(walletId, target, reference, value), cancellationToken);
            }
        }

        // Custom metadata: if supplied, full replace.
        if (request.Metadata is not null)
        {
            var existingEntries = await _metadata.GetEntriesAsync(walletId, target, reference, cancellationToken);
            foreach (var e in existingEntries)
                _metadata.RemoveEntry(e);

            foreach (var (key, value) in request.Metadata)
            {
                if (!string.IsNullOrWhiteSpace(key))
                    await _metadata.AddEntryAsync(new MetadataEntry(walletId, target, reference, key.Trim(), value), cancellationToken);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
