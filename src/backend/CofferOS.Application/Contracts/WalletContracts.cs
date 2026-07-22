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
    DateTimeOffset CreatedAt);

public sealed record DashboardDto(
    int WalletCount,
    BalanceDto TotalBalance,
    IReadOnlyList<WalletSummaryDto> Wallets,
    IReadOnlyList<TransactionDto> RecentActivity);

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
