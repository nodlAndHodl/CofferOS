using CofferOS.Domain.Common;

namespace CofferOS.Domain.Wallets;

/// <summary>
/// A longer, free-form note attached to an object. Notes are richer than labels
/// and are intended for human context (e.g. "cold storage vault, keys with lawyer").
/// </summary>
public sealed class Note : Entity
{
    private Note() { }

    public Note(Guid walletId, LabelTarget target, string reference, string content)
    {
        WalletId = walletId;
        Target = target;
        Reference = reference;
        Content = content;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public Guid WalletId { get; private set; }
    public LabelTarget Target { get; private set; }
    public string Reference { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public void Update(string content)
    {
        Content = content;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
