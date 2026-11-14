using Microsoft.EntityFrameworkCore;

namespace CinemaBD.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<LegacyInvoice>()
            .HasIndex(x => x.GatewayTxnRef)
            .IsUnique()
            .HasFilter("[GatewayTxnRef] IS NOT NULL AND [GatewayTxnRef] <> ''");
        modelBuilder.Entity<LegacyInvoice>()
            .Ignore(x => x.DaCheckIn)
            .Ignore(x => x.ThoiGianCheckIn);
        modelBuilder.Entity<LegacyTicket>()
            .Ignore(x => x.DaCheckIn)
            .Ignore(x => x.ThoiGianCheckIn);

        modelBuilder.Entity<RevokedToken>()
            .ToTable("RevokedTokens")
            .HasKey(x => x.Jti);
        modelBuilder.Entity<RevokedToken>()
            .HasIndex(x => x.ExpiresAtUtc);

        modelBuilder.Entity<IdentitySession>(entity =>
        {
            entity.ToTable("IdentitySessions");
            entity.HasKey(x => x.SessionId);
            entity.Property(x => x.AccountType).HasMaxLength(32).IsRequired();
            entity.Property(x => x.AccountId).HasMaxLength(128).IsRequired();
            entity.Property(x => x.RevocationReason).HasMaxLength(128);
            entity.Property(x => x.DeviceName).HasMaxLength(100);
            entity.Property(x => x.UserAgent).HasMaxLength(512);
            entity.Property(x => x.LastSeenIp).HasMaxLength(45);
            entity.HasIndex(x => new { x.AccountType, x.AccountId, x.RevokedAtUtc });
            entity.HasIndex(x => x.ExpiresAtUtc);
        });

        modelBuilder.Entity<RefreshTokenRecord>(entity =>
        {
            entity.ToTable("RefreshTokens");
            entity.HasKey(x => x.RefreshTokenId);
            entity.Property(x => x.TokenHash).HasColumnType("binary(32)").IsRequired();
            entity.HasIndex(x => x.TokenHash).IsUnique();
            entity.HasIndex(x => new { x.SessionId, x.ExpiresAtUtc });
            entity.HasOne(x => x.Session)
                .WithMany()
                .HasForeignKey(x => x.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<RefreshTokenRecord>()
                .WithMany()
                .HasForeignKey(x => x.ReplacedByRefreshTokenId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<SecurityAuditRecord>(entity =>
        {
            entity.ToTable("SecurityAudit");
            entity.HasKey(x => x.AuditId);
            entity.Property(x => x.EventType).HasMaxLength(64).IsRequired();
            entity.Property(x => x.AccountType).HasMaxLength(32);
            entity.Property(x => x.AccountId).HasMaxLength(128);
            entity.Property(x => x.SourceIp).HasMaxLength(45);
            entity.Property(x => x.CorrelationId).HasMaxLength(128);
            entity.Property(x => x.Details).HasMaxLength(1000);
            entity.HasIndex(x => x.OccurredAtUtc);
            entity.HasIndex(x => new { x.EventType, x.OccurredAtUtc });
        });

        modelBuilder.Entity<LegacySeat>()
            .HasKey(x => new { x.MaPhong, x.MaGhe });

        modelBuilder.Entity<LegacyRoom>()
            .Ignore(x => x.MaRap);

        modelBuilder.Entity<LegacyInvoice>()
            .Ignore(x => x.DaCheckIn)
            .Ignore(x => x.ThoiGianCheckIn);

        modelBuilder.Entity<LegacyReview>()
            .Ignore(x => x.Rating)
            .Ignore(x => x.IsHidden);
    }

    public DbSet<LegacyMovie> Movies => Set<LegacyMovie>();
    public DbSet<LegacyCinema> Cinemas => Set<LegacyCinema>();
    public DbSet<LegacyRoom> Rooms => Set<LegacyRoom>();
    public DbSet<LegacySeat> Seats => Set<LegacySeat>();
    public DbSet<LegacyShowtime> Showtimes => Set<LegacyShowtime>();
    public DbSet<LegacyTicket> Tickets => Set<LegacyTicket>();
    public DbSet<LegacyPayment> Payments => Set<LegacyPayment>();
    public DbSet<LegacyCombo> Combos => Set<LegacyCombo>();
    public DbSet<LegacyBookedCombo> BookedCombos => Set<LegacyBookedCombo>();
    public DbSet<LegacyCustomer> Customers => Set<LegacyCustomer>();
    public DbSet<LegacyAdmin> Admins => Set<LegacyAdmin>();
    public DbSet<LegacyRole> Roles => Set<LegacyRole>();
    public DbSet<LegacyPermission> Permissions => Set<LegacyPermission>();
    public DbSet<LegacyRolePermission> RolePermissions => Set<LegacyRolePermission>();
    public DbSet<LegacyEmployee> Employees => Set<LegacyEmployee>();

    // Content & Business entities
    public DbSet<LegacyGenre> Genres => Set<LegacyGenre>();
    public DbSet<LegacyArticle> Articles => Set<LegacyArticle>();
    public DbSet<LegacyEvent> Events => Set<LegacyEvent>();
    public DbSet<LegacyReview> Reviews => Set<LegacyReview>();
    public DbSet<LegacyInvoice> Invoices => Set<LegacyInvoice>();
    public DbSet<LegacyInvoiceLineItem> InvoiceLineItems => Set<LegacyInvoiceLineItem>();
    public DbSet<LegacyInvoiceHistory> InvoiceHistories => Set<LegacyInvoiceHistory>();
    public DbSet<LegacyVoucher> Vouchers => Set<LegacyVoucher>();
    public DbSet<LegacyLoyaltyPoints> LoyaltyPoints => Set<LegacyLoyaltyPoints>();
    public DbSet<LegacyRefundRequest> RefundRequests => Set<LegacyRefundRequest>();
    public DbSet<RevokedToken> RevokedTokens => Set<RevokedToken>();
    public DbSet<IdentitySession> IdentitySessions => Set<IdentitySession>();
    public DbSet<RefreshTokenRecord> RefreshTokens => Set<RefreshTokenRecord>();
    public DbSet<SecurityAuditRecord> SecurityAudit => Set<SecurityAuditRecord>();
}
