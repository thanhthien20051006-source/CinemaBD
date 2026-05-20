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
}
