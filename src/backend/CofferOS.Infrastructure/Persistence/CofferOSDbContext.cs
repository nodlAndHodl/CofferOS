using CofferOS.Application.Abstractions.Events;
using CofferOS.Domain.Common;
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
