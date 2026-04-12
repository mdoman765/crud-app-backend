using crud_app_backend.Models;
using Microsoft.EntityFrameworkCore;

namespace crud_app_backend
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // ── existing tables ──────────────────────────────────────────────
        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<SubCategory> SubCategories { get; set; } = null!;
        public DbSet<Product> Products { get; set; } = null!;
        public DbSet<Order> Orders { get; set; } = null!;
        public DbSet<Feedback> Feedbacks { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<WhatsAppMessage> WhatsAppMessages { get; set; } = null!;

        // ── WhatsApp tables ──────────────────────────────────────────────
        public DbSet<WhatsAppSession> WhatsAppSessions { get; set; } = null!;
        public DbSet<WhatsAppSessionHistory> WhatsAppSessionHistories { get; set; } = null!;
        public DbSet<WhatsAppComplaint> WhatsAppComplaints { get; set; } = null!;
        public DbSet<WhatsAppComplaintMedia> WhatsAppComplaintMediaItems { get; set; } = null!;



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // 2.  In OnModelCreating (after the WhatsAppSessionHistory config block):
            modelBuilder.Entity<WhatsAppMessage>(entity =>
            {
                entity.ToTable("WhatsAppMessages", "dbo");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                      .HasDefaultValueSql("NEWSEQUENTIALID()");   // clustered-friendly GUID

                entity.Property(e => e.MessageId)
                      .HasMaxLength(255)
                      .IsRequired();

                entity.HasIndex(e => e.MessageId)
                      .IsUnique()
                      .HasDatabaseName("UX_WhatsAppMessages_MessageId");  // duplicate guard

                entity.Property(e => e.FromNumber)
                      .HasMaxLength(30)
                      .IsRequired();

                entity.HasIndex(e => new { e.FromNumber, e.ReceivedAt })
                      .HasDatabaseName("IX_WhatsAppMessages_FromNumber_ReceivedAt");

                entity.Property(e => e.SenderName).HasMaxLength(255);

                entity.Property(e => e.MessageType)
                      .HasMaxLength(20)
                      .IsRequired();

                entity.Property(e => e.TextBody)
                      .HasColumnType("nvarchar(max)");

                entity.Property(e => e.MediaId).HasMaxLength(255);
                entity.Property(e => e.FileUrl).HasMaxLength(2048);
                entity.Property(e => e.MimeType).HasMaxLength(100);
                entity.Property(e => e.Caption).HasMaxLength(1000);

                entity.Property(e => e.Status)
                      .HasMaxLength(30)
                      .IsRequired()
                      .HasDefaultValue("received");

                entity.Property(e => e.ErrorMessage).HasMaxLength(1000);

                entity.Property(e => e.ReceivedAt)
                      .IsRequired()
                      .HasDefaultValueSql("SYSUTCDATETIME()");
            });
            // ── existing config (unchanged) ──────────────────────────────
            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            });

            modelBuilder.Entity<Category>();
            modelBuilder.Entity<SubCategory>();
            modelBuilder.Entity<Product>();

            modelBuilder.Entity<Product>()
                .HasOne(p => p.SubCategory)
                .WithMany(sc => sc.Products)
                .HasForeignKey(p => p.SubcategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SubCategory>()
                .HasOne(sc => sc.Category)
                .WithMany(c => c.SubCategories)
                .HasForeignKey(sc => sc.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── WhatsAppSessions ─────────────────────────────────────────
            modelBuilder.Entity<WhatsAppSession>(entity =>
            {
                entity.ToTable("WhatsAppSessions", "dbo");

                entity.HasKey(e => e.Phone);

                entity.Property(e => e.Phone)
                      .HasMaxLength(30)
                      .IsRequired();

                entity.Property(e => e.CurrentStep)
                      .HasMaxLength(50)
                      .IsRequired()
                      .HasDefaultValue("INIT");

                entity.Property(e => e.PreviousStep)
                      .HasMaxLength(50)
                      .IsRequired()
                      .HasDefaultValue("INIT");

                entity.Property(e => e.TempData)
                      .HasColumnType("nvarchar(max)")
                      .IsRequired()
                      .HasDefaultValue("{}");

                entity.Property(e => e.PendingReport)
                      .IsRequired()
                      .HasDefaultValue(false);

                entity.Property(e => e.PendingShopReg)
                      .IsRequired()
                      .HasDefaultValue(false);

                entity.Property(e => e.CreatedAt)
                      .IsRequired()
                      .HasDefaultValueSql("SYSUTCDATETIME()");

                entity.Property(e => e.UpdatedAt)
                      .IsRequired()
                      .HasDefaultValueSql("SYSUTCDATETIME()");

                entity.HasMany(e => e.History)
                      .WithOne(h => h.Session)
                      .HasForeignKey(h => h.Phone)
                      .HasConstraintName("FK_SessionHistory_Phone")
                      .OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<WhatsAppComplaint>(entity =>
            {
                entity.ToTable("WhatsAppComplaints", "dbo");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).UseIdentityColumn();

                entity.Property(e => e.ComplaintNumber).HasMaxLength(20);
                entity.HasIndex(e => e.ComplaintNumber)
                      .IsUnique()
                      .HasFilter("[ComplaintNumber] IS NOT NULL")
                      .HasDatabaseName("UX_WhatsAppComplaints_ComplaintNumber");

                entity.Property(e => e.WhatsappPhone).HasMaxLength(30).IsRequired();
                entity.HasIndex(e => e.WhatsappPhone)
                      .HasDatabaseName("IX_WhatsAppComplaints_WhatsappPhone");

                entity.Property(e => e.StaffId).HasMaxLength(50).IsRequired();
                entity.Property(e => e.StaffName).HasMaxLength(255);
                entity.Property(e => e.OfficialPhone).HasMaxLength(30);
                entity.Property(e => e.Designation).HasMaxLength(100);
                entity.Property(e => e.Dept).HasMaxLength(100);
                entity.Property(e => e.GroupName).HasMaxLength(100);
                entity.Property(e => e.Company).HasMaxLength(100);
                entity.Property(e => e.LocationName).HasMaxLength(200);
                entity.Property(e => e.Email).HasMaxLength(255);
                entity.Property(e => e.Description).HasColumnType("nvarchar(max)");
                entity.Property(e => e.CrmTicketId).HasMaxLength(50);
                entity.Property(e => e.ComplaintType).HasMaxLength(30).IsRequired().HasDefaultValue("complaint");
                entity.Property(e => e.Status).HasMaxLength(30).IsRequired().HasDefaultValue("open");
                entity.Property(e => e.CreatedAt).IsRequired().HasDefaultValueSql("SYSUTCDATETIME()");
                entity.Property(e => e.UpdatedAt).IsRequired().HasDefaultValueSql("SYSUTCDATETIME()");

                entity.HasMany(e => e.MediaItems)
                      .WithOne(m => m.Complaint)
                      .HasForeignKey(m => m.ComplaintId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<WhatsAppComplaintMedia>(entity =>
            {
                entity.ToTable("WhatsAppComplaintMedia", "dbo");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).UseIdentityColumn();

                entity.Property(e => e.MessageId).HasMaxLength(255).IsRequired();
                entity.Property(e => e.MediaType).HasMaxLength(10).IsRequired();
                entity.Property(e => e.FileUrl).HasMaxLength(2048);
                entity.Property(e => e.MimeType).HasMaxLength(100);
                entity.Property(e => e.Caption).HasMaxLength(1000);
                entity.Property(e => e.CreatedAt).IsRequired().HasDefaultValueSql("SYSUTCDATETIME()");

                entity.HasIndex(e => e.MessageId)
                      .HasDatabaseName("IX_WhatsAppComplaintMedia_MessageId");
                entity.HasIndex(e => e.ComplaintId)
                      .HasDatabaseName("IX_WhatsAppComplaintMedia_ComplaintId");
            });


            // ── WhatsAppSessionHistory ───────────────────────────────────
            modelBuilder.Entity<WhatsAppSessionHistory>(entity =>
            {
                entity.ToTable("WhatsAppSessionHistory", "dbo");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                      .UseIdentityColumn();

                entity.Property(e => e.Phone)
                      .HasMaxLength(30)
                      .IsRequired();

                entity.Property(e => e.FromStep)
                      .HasMaxLength(50)
                      .IsRequired();

                entity.Property(e => e.ToStep)
                      .HasMaxLength(50)
                      .IsRequired();

                entity.Property(e => e.RawMessage)
                      .HasMaxLength(1000);

                entity.Property(e => e.TempDataSnapshot)
                      .HasColumnType("nvarchar(max)");

                entity.Property(e => e.CreatedAt)
                      .IsRequired()
                      .HasDefaultValueSql("SYSUTCDATETIME()");

                entity.HasIndex(e => new { e.Phone, e.CreatedAt })
                      .HasDatabaseName("IX_WhatsAppSessionHistory_Phone_CreatedAt");
            });
        }
    }
}
