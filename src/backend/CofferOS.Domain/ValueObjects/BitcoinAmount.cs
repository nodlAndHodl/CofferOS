namespace CofferOS.Domain.ValueObjects;

/// <summary>
/// A value object representing a Bitcoin amount. Stored and reasoned about in
/// satoshis (the smallest unit) to avoid floating point rounding errors.
/// </summary>
public readonly record struct BitcoinAmount(long Satoshis) : IComparable<BitcoinAmount>
{
    public const long SatoshisPerBitcoin = 100_000_000L;

    public static readonly BitcoinAmount Zero = new(0);

    public decimal ToBtc() => (decimal)Satoshis / SatoshisPerBitcoin;

    public static BitcoinAmount FromBtc(decimal btc) => new((long)(btc * SatoshisPerBitcoin));

    public static BitcoinAmount operator +(BitcoinAmount a, BitcoinAmount b) => new(a.Satoshis + b.Satoshis);

    public static BitcoinAmount operator -(BitcoinAmount a, BitcoinAmount b) => new(a.Satoshis - b.Satoshis);

    public int CompareTo(BitcoinAmount other) => Satoshis.CompareTo(other.Satoshis);

    public override string ToString() => $"{ToBtc():0.########} BTC";
}
