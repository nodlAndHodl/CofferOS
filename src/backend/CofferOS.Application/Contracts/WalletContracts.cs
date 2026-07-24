namespace CofferOS.Application.Contracts;

/// <summary>Request to import a new watch-only wallet from an xpub or descriptor.</summary>
public sealed record ImportWalletRequest(
    string Name,
    string? Description,
    string Descriptor,
    string Network = "Mainnet",
    int InitialAddressCount = 20);

public sealed record BalanceDto(long ConfirmedSats, long UnconfirmedSats, long TotalSats, decimal TotalBtc);

public sealed record WalletSummaryDto(
    Guid Id,
    string Name,
    string? Description,
    string Network,
    bool WatchOnly,
    int DescriptorCount,
    int TransactionCount,
    BalanceDto Balance,
    DateTimeOffset CreatedAt);

public sealed record DescriptorDto(
    Guid Id,
    string Source,
    string ScriptType,
    string Raw,
    string? MasterFingerprint,
    string? DerivationPath,
    string? Checksum,
    int AddressCount);

public sealed record AddressDto(
    Guid Id,
    int DerivationIndex,
    bool IsChange,
    string Value,
    bool IsUsed,
    int UseCount,
    string? FirstTxId,
    string? LastTxId,
    long CurrentSats);

public sealed record TransactionDto(
    string TxId,
    long NetAmountSats,
    long FeeSats,
    string Direction,
    int Confirmations,
    long? BlockHeight,
    DateTimeOffset? Timestamp);

public sealed record UtxoDto(
    string TxId,
    int Vout,
    long ValueSats,
    string? Address,
    int Confirmations,
    DateTimeOffset? Timestamp,
    bool IsSpent);

public sealed record RescanResultDto(int UtxoCount, BalanceDto Balance);

public sealed record NoteDto(
    Guid Id,
    string Target,
    string Reference,
    string Content,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record CreateNoteRequest(
    string Target,
    string Reference,
    string Content);

public sealed record UpdateNoteRequest(string Content);

public sealed record LabelDto(string Target, string Reference, string Text);

public sealed record TagDto(string Target, string Reference, string Value);

public sealed record CategoryDto(string Target, string Reference, string Name);

public sealed record MetadataEntryDto(string Target, string Reference, string Key, string Value);

/// <summary>The full user-defined metadata attached to a single object (usually a transaction).</summary>
public sealed record ObjectMetadataDto(
    string Target,
    string Reference,
    string? Label,
    string? Category,
    IReadOnlyList<string> Tags,
    IReadOnlyDictionary<string, string> Metadata,
    IReadOnlyList<NoteDto> Notes);

/// <summary>
/// Replaces the metadata for a target object. A null label/category clears it;
/// the tag list and metadata dictionary fully replace what is stored. Notes are
/// managed through the existing note endpoints.
/// </summary>
public sealed record UpdateMetadataRequest(
    string Target,
    string Reference,
    string? Label,
    string? Category,
    IReadOnlyList<string>? Tags,
    IReadOnlyDictionary<string, string>? Metadata);

public sealed record WalletDetailDto(
    Guid Id,
    string Name,
    string? Description,
    string Network,
    bool WatchOnly,
    BalanceDto Balance,
    IReadOnlyList<DescriptorDto> Descriptors,
    IReadOnlyList<AddressDto> Addresses,
    IReadOnlyList<TransactionDto> Transactions,
    IReadOnlyList<UtxoDto> Utxos,
    IReadOnlyList<LabelDto> Labels,
    IReadOnlyList<NoteDto> Notes,
    IReadOnlyList<TagDto> Tags,
    IReadOnlyList<CategoryDto> Categories,
    IReadOnlyList<MetadataEntryDto> Metadata,
    DateTimeOffset CreatedAt);

/// <summary>A single entry on a wallet timeline (stored annotation or generated from history).</summary>
public sealed record TimelineEntryDto(
    Guid? Id,
    string Type,
    DateTimeOffset OccurredAt,
    string Title,
    string? Description,
    string? Reference,
    long? AmountSats,
    long? RunningBalanceSats,
    bool IsUserEvent);

public sealed record WalletTimelineDto(
    Guid WalletId,
    string WalletName,
    BalanceDto CurrentBalance,
    IReadOnlyList<TimelineEntryDto> Entries);

public sealed record CreateTimelineEventRequest(
    DateTimeOffset OccurredAt,
    string Title,
    string? Description,
    string? Reference,
    string? Type);

public sealed record UpdateTimelineEventRequest(
    DateTimeOffset OccurredAt,
    string Title,
    string? Description,
    string? Reference);

public sealed record TimelineEventDto(
    Guid Id,
    string Type,
    DateTimeOffset OccurredAt,
    string Title,
    string? Description,
    string? Reference,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record RecentActivityItemDto(
    string TxId,
    long NetAmountSats,
    long? BlockHeight,
    DateTimeOffset? Timestamp,
    string WalletName,
    string? Label,
    IReadOnlyList<string> Tags);

public sealed record RecentActivityPageDto(
    int Skip,
    int Take,
    int Total,
    IReadOnlyList<RecentActivityItemDto> Items);

public sealed record DashboardDto(
    int WalletCount,
    BalanceDto TotalBalance,
    IReadOnlyList<WalletSummaryDto> Wallets,
    RecentActivityPageDto RecentActivity);

public sealed record ElectrumStatusDto(
    bool Connected,
    string ProviderId,
    string Host,
    int Port,
    string? Socks5Proxy,
    long? BlockHeight,
    string? Error);

public sealed record NodeStatusDto(
    bool Connected,
    string ProviderId,
    string? Chain,
    long? Blocks,
    double? VerificationProgress,
    string? Error);
