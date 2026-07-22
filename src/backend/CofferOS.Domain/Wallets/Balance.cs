using CofferOS.Domain.ValueObjects;

namespace CofferOS.Domain.Wallets;

/// <summary>
/// A point-in-time balance snapshot for a wallet. This is a value object computed
/// from UTXOs; it is not persisted as an aggregate but returned by queries.
/// </summary>
public readonly record struct Balance(BitcoinAmount Confirmed, BitcoinAmount Unconfirmed)
{
    public BitcoinAmount Total => Confirmed + Unconfirmed;

    public static Balance Empty => new(BitcoinAmount.Zero, BitcoinAmount.Zero);
}
