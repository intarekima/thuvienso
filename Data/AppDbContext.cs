using Microsoft.EntityFrameworkCore;
using thuvienso.Models;

namespace thuvienso.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        // ==== CORE ====
        public DbSet<User> Users { get; set; }
        public DbSet<Author> Authors { get; set; }
        public DbSet<Publisher> Publishers { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<DocumentAuthor> DocumentAuthors { get; set; }

        // ==== ACTIONS ====
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Download> Downloads { get; set; }
        public DbSet<QRCode> QRCodes { get; set; }

        public DbSet<Contact> Contacts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ==== ENUMS ====
            modelBuilder.Entity<User>()
                .Property(u => u.Role)
                .HasColumnType("ENUM('admin','user')")
                .HasDefaultValue("user");

            modelBuilder.Entity<Payment>()
                .Property(p => p.PaymentStatus)
                .HasColumnType("ENUM('pending','paid','failed')");

            modelBuilder.Entity<QRCode>()
                .Property(q => q.Type)
                .HasColumnType("ENUM('view','download')");

            // ==== CATEGORY → CATEGORY (cha – con) ====
            modelBuilder.Entity<Category>()
                .HasOne(c => c.Parent)
                .WithMany(c => c.Children)
                .HasForeignKey(c => c.ParentId)
                .OnDelete(DeleteBehavior.SetNull);

            // ==== DOCUMENT → CATEGORY ====
            modelBuilder.Entity<Document>()
                .HasOne(d => d.Category)
                .WithMany(c => c.Documents)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            // ==== DOCUMENT → PUBLISHER ====
            modelBuilder.Entity<Document>()
                .HasOne(d => d.Publisher)
                .WithMany(p => p.Documents)
                .HasForeignKey(d => d.PublisherId)
                .OnDelete(DeleteBehavior.SetNull);

            // ==== DOCUMENTAUTHOR (nhiều–nhiều) ====
            modelBuilder.Entity<DocumentAuthor>()
                .HasOne(da => da.Document)
                .WithMany(d => d.DocumentAuthors)
                .HasForeignKey(da => da.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DocumentAuthor>()
                .HasOne(da => da.Author)
                .WithMany(a => a.DocumentAuthors)
                .HasForeignKey(da => da.AuthorId)
                .OnDelete(DeleteBehavior.Cascade);
        }
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            foreach (var entry in ChangeTracker.Entries<Document>())
            {
                if (entry.State == EntityState.Added)
                {
                    // Nếu chưa được set từ constructor thì gán luôn
                    if (entry.Entity.CreatedAt == default)
                        entry.Entity.CreatedAt = now;

                    entry.Entity.UpdatedAt = now;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdatedAt = now;
                }
            }

            return await base.SaveChangesAsync(cancellationToken);
        }

    }
}
