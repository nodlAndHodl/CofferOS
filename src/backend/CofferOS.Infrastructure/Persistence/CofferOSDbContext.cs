using CofferOS.Application.Abstractions.Events;
using CofferOS.Domain.Common;
using CofferOS.Domain.Treasury;
using CofferOS.Domain.Wallets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CofferOS.Infrastructure.Persistence;

/// <summary>
/// The single EF Core DbContext for the modular monolith. Modules own their
/// entities but share one SQLite database (and one connection lifetime) which
/// keeps the deployment simple and privacy-friendly: one local file, no server.
/// </summary>
public sealed class CofferOSDbContext : DbContext
{
    private readonly IDomainEventDispatcher? _dispatcher;

    public CofferOSDbContext(DbContextOptions<CofferOSDbContext> options, IDomainEventDispatcher? dispatcher = null)
        : base(options)
    {
        _dispatcher = dispatcher;
    }

    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<Descriptor> Descriptors => Set<Descriptor>();
    public DbSet<Address> Addresses => Set<Address>();
    public DbSet<WalletTransaction> Transactions => Set<WalletTransaction>();
    public DbSet<Utxo> Utxos => Set<Utxo>();
    public DbSet<Label> Labels => Set<Label>();
    public DbSet<Note> Notes => Set<Note>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<MetadataEntry> MetadataEntries => Set<MetadataEntry>();
    public DbSet<TimelineEvent> TimelineEvents => Set<TimelineEvent>();
    public DbSet<Loan> Loans => Set<Loan>();
    public DbSet<LoanPayment> LoanPayments => Set<LoanPayment>();
    public DbSet<LoanPriceSnapshot> LoanPriceSnapshots => Set<LoanPriceSnapshot>();
    public DbSet<CofferOS.Domain.Prices.BitcoinPriceHistory> BitcoinPriceHistory => Set<CofferOS.Domain.Prices.BitcoinPriceHistory>();
    public DbSet<CostBasisEntry> CostBasisEntries => Set<CostBasisEntry>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // SQLite cannot ORDER BY / compare DateTimeOffset stored as text. Persist as a
        // long (order-preserving for UTC values, which is what CofferOS uses everywhere).
        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<DateTimeOffsetToBinaryConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Wallet>(b =>
        {
            b.ToTable("wallets");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).IsRequired().HasMaxLength(200);
            b.Property(x => x.Description).HasMaxLength(2000);
            b.Property(x => x.Network).HasConversion<string>().HasMaxLength(20);
            b.Property(x => x.WatchOnly);
            b.Property(x => x.CreatedAt);

            b.HasMany(x => x.Descriptors).WithOne().HasForeignKey(d => d.WalletId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(x => x.Transactions).WithOne().HasForeignKey(t => t.WalletId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(x => x.Utxos).WithOne().HasForeignKey(u => u.WalletId).OnDelete(DeleteBehavior.Cascade);

            b.Navigation(x => x.Descriptors).UsePropertyAccessMode(PropertyAccessMode.Field);
            b.Navigation(x => x.Transactions).UsePropertyAccessMode(PropertyAccessMode.Field);
            b.Navigation(x => x.Utxos).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<Descriptor>(b =>
        {
            b.ToTable("descriptors");
            b.HasKey(x => x.Id);
            b.Property(x => x.WalletId);
            b.Property(x => x.Source).HasConversion<string>().HasMaxLength(30);
            b.Property(x => x.ScriptType).HasConversion<string>().HasMaxLength(20);
            b.Property(x => x.Raw).IsRequired();
            b.Property(x => x.MasterFingerprint).HasMaxLength(16);
            b.Property(x => x.DerivationPath).HasMaxLength(100);
            b.Property(x => x.Checksum).HasMaxLength(16);
            b.Property(x => x.Threshold);
            b.Property(x => x.IsSortedMulti);
            b.Property(x => x.CreatedAt);

            b.HasMany(x => x.Addresses).WithOne().HasForeignKey(a => a.DescriptorId).OnDelete(DeleteBehavior.Cascade);
            b.Navigation(x => x.Addresses).UsePropertyAccessMode(PropertyAccessMode.Field);
            b.OwnsMany(x => x.Cosigners, c =>
            {
                c.ToTable("cosigners");
                c.Property(p => p.OrderIndex);
                c.Property(p => p.MasterFingerprint).HasMaxLength(16);
                c.Property(p => p.OriginPath).HasMaxLength(100);
                c.Property(p => p.KeyExpression).IsRequired();
                c.HasKey("Id");
            });
            b.HasIndex(x => x.WalletId);
        });

        modelBuilder.Entity<Address>(b =>
        {
            b.ToTable("addresses");
            b.HasKey(x => x.Id);
            b.Property(x => x.Value).IsRequired();
            b.Property(x => x.ScriptPubKeyHex).IsRequired();
            b.Property(x => x.IsUsed);
            b.Property(x => x.UseCount);
            b.Property(x => x.FirstTxId).HasMaxLength(64);
            b.Property(x => x.LastTxId).HasMaxLength(64);
            b.Property(x => x.CurrentSats);
            b.HasIndex(x => x.WalletId);
            b.HasIndex(x => x.Value);
        });

        modelBuilder.Entity<WalletTransaction>(b =>
        {
            b.ToTable("transactions");
            b.HasKey(x => x.Id);
            b.Property(x => x.TxId).IsRequired().HasMaxLength(64);
            b.Property(x => x.Direction).HasConversion<string>().HasMaxLength(20);
            b.HasIndex(x => x.WalletId);
            b.HasIndex(x => new { x.WalletId, x.TxId }).IsUnique();
        });

        modelBuilder.Entity<Utxo>(b =>
        {
            b.ToTable("utxos");
            b.HasKey(x => x.Id);
            b.Property(x => x.TxId).IsRequired().HasMaxLength(64);
            b.Property(x => x.ScriptPubKeyHex).IsRequired();
            b.HasIndex(x => x.WalletId);
            b.HasIndex(x => new { x.WalletId, x.TxId, x.Vout }).IsUnique();
        });

        modelBuilder.Entity<Label>(b =>
        {
            b.ToTable("labels");
            b.HasKey(x => x.Id);
            b.Property(x => x.Target).HasConversion<string>().HasMaxLength(20);
            b.Property(x => x.Reference).IsRequired().HasMaxLength(200);
            b.Property(x => x.Text).IsRequired().HasMaxLength(500);
            b.HasIndex(x => x.WalletId);
        });

        modelBuilder.Entity<Note>(b =>
        {
            b.ToTable("notes");
            b.HasKey(x => x.Id);
            b.Property(x => x.Target).HasConversion<string>().HasMaxLength(20);
            b.Property(x => x.Reference).IsRequired().HasMaxLength(200);
            b.Property(x => x.Content).IsRequired();
            b.HasIndex(x => x.WalletId);
        });

        modelBuilder.Entity<Tag>(b =>
        {
            b.ToTable("tags");
            b.HasKey(x => x.Id);
            b.Property(x => x.Target).HasConversion<string>().HasMaxLength(20);
            b.Property(x => x.Reference).IsRequired().HasMaxLength(200);
            b.Property(x => x.Value).IsRequired().HasMaxLength(100);
            b.HasIndex(x => x.WalletId);
            b.HasIndex(x => new { x.WalletId, x.Target, x.Reference, x.Value }).IsUnique();
        });

        modelBuilder.Entity<Category>(b =>
        {
            b.ToTable("categories");
            b.HasKey(x => x.Id);
            b.Property(x => x.Target).HasConversion<string>().HasMaxLength(20);
            b.Property(x => x.Reference).IsRequired().HasMaxLength(200);
            b.Property(x => x.Name).IsRequired().HasMaxLength(200);
            b.HasIndex(x => x.WalletId);
            b.HasIndex(x => new { x.WalletId, x.Target, x.Reference }).IsUnique();
        });

        modelBuilder.Entity<MetadataEntry>(b =>
        {
            b.ToTable("metadata_entries");
            b.HasKey(x => x.Id);
            b.Property(x => x.Target).HasConversion<string>().HasMaxLength(20);
            b.Property(x => x.Reference).IsRequired().HasMaxLength(200);
            b.Property(x => x.Key).IsRequired().HasMaxLength(100);
            b.Property(x => x.Value).IsRequired().HasMaxLength(2000);
            b.HasIndex(x => x.WalletId);
            b.HasIndex(x => new { x.WalletId, x.Target, x.Reference, x.Key }).IsUnique();
        });

        modelBuilder.Entity<TimelineEvent>(b =>
        {
            b.ToTable("timeline_events");
            b.HasKey(x => x.Id);
            b.Property(x => x.Type).HasConversion<string>().HasMaxLength(30);
            b.Property(x => x.Title).IsRequired().HasMaxLength(200);
            b.Property(x => x.Description).HasMaxLength(2000);
            b.Property(x => x.Reference).HasMaxLength(200);
            b.HasIndex(x => x.WalletId);
            b.HasIndex(x => new { x.WalletId, x.OccurredAt });
        });

        modelBuilder.Entity<Loan>(b =>
        {
            b.ToTable("loans");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).IsRequired().HasMaxLength(200);
            b.Property(x => x.Lender).HasMaxLength(200);
            b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            b.Property(x => x.Notes).HasMaxLength(4000);
            b.Property(x => x.PrincipalAmount).HasPrecision(18, 8);
            b.Property(x => x.CurrentBalance).HasPrecision(18, 8);
            b.Property(x => x.InterestRate).HasPrecision(18, 8);
            b.Property(x => x.InterestType).HasConversion<string>().HasMaxLength(20);
            b.Property(x => x.PaymentFrequency).HasConversion<string>().HasMaxLength(20);
            b.Property(x => x.InterestPaymentSchedule).HasConversion<int>();
            b.Property(x => x.CollateralAmountBtc).HasPrecision(18, 8);
            b.Property(x => x.CurrentBtcPrice).HasPrecision(18, 8);
            b.Property(x => x.WarningLtv).HasPrecision(18, 8);
            b.Property(x => x.LiquidationLtv).HasPrecision(18, 8);
            b.Property(x => x.LoanTermMonths);
            b.Property(x => x.AccruedInterest).HasPrecision(18, 8);
            b.Property(x => x.LastAccruedOn);
            b.HasIndex(x => x.Status);
            b.HasIndex(x => x.CreatedAt);
        });

        modelBuilder.Entity<LoanPayment>(b =>
        {
            b.ToTable("loan_payments");
            b.HasKey(x => x.Id);
            b.Property(x => x.TotalAmount).HasPrecision(18, 8);
            b.Property(x => x.PrincipalAmount).HasPrecision(18, 8);
            b.Property(x => x.InterestAmount).HasPrecision(18, 8);
            b.Property(x => x.Notes).HasMaxLength(2000);
            b.HasIndex(x => x.LoanId);
            b.HasIndex(x => x.PaymentDate);
        });

        modelBuilder.Entity<LoanPriceSnapshot>(b =>
        {
            b.ToTable("loan_price_snapshots");
            b.HasKey(x => x.Id);
            b.Property(x => x.LoanId);
            b.Property(x => x.PriceUsd).HasPrecision(18, 8);
            b.Property(x => x.Source).IsRequired().HasMaxLength(50);
            b.HasIndex(x => x.LoanId);
            b.HasIndex(x => x.SnapshotDate);
            b.HasIndex(x => new { x.LoanId, x.SnapshotDate });
        });

        modelBuilder.Entity<CostBasisEntry>(b =>
        {
            b.ToTable("cost_basis_entries");
            b.HasKey(x => x.Id);
            b.Property(x => x.Target).HasConversion<string>().HasMaxLength(20);
            b.Property(x => x.Reference).IsRequired().HasMaxLength(200);
            b.Property(x => x.Amount).HasPrecision(18, 2);
            b.Property(x => x.CreatedAt);
            b.Property(x => x.UpdatedAt);
            b.HasIndex(x => x.Reference);
            b.HasIndex(x => new { x.Target, x.Reference }).IsUnique();
        });

        modelBuilder.Entity<CofferOS.Domain.Prices.BitcoinPriceHistory>(b =>
        {
            b.ToTable("bitcoin_price_history");
            b.HasKey(x => x.Id);
            b.Property(x => x.PriceUsd).HasPrecision(18, 8);
            b.Property(x => x.Provider).IsRequired().HasMaxLength(50);
            b.HasIndex(x => x.Timestamp);
            b.HasIndex(x => x.Provider);
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Collect domain events before saving so ids are populated.
        var entitiesWithEvents = ChangeTracker
            .Entries<Entity>()
            .Select(e => e.Entity)
            .Where(e => e.DomainEvents.Count > 0)
            .ToList();

        var domainEvents = entitiesWithEvents.SelectMany(e => e.DomainEvents).ToList();

        var result = await base.SaveChangesAsync(cancellationToken);

        if (_dispatcher is not null && domainEvents.Count > 0)
        {
            await _dispatcher.DispatchAsync(domainEvents, cancellationToken);
            foreach (var entity in entitiesWithEvents)
                entity.ClearDomainEvents();
        }

        return result;
    }
}
