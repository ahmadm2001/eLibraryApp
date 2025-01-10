using Microsoft.EntityFrameworkCore;

namespace eLibraryApp.Models
{
    public class LibraryDbContext : DbContext
    {
        public LibraryDbContext(DbContextOptions<LibraryDbContext> options) : base(options) { }

        // DbSets for each model
        public DbSet<User> Users { get; set; }
        public DbSet<Book> Books { get; set; }
        public DbSet<BorrowedBook> BorrowedBooks { get; set; }
        public DbSet<PurchasedBook> PurchasedBooks { get; set; }
        public DbSet<Rating> Ratings { get; set; }
        public DbSet<WaitingListEntry> WaitingListEntries { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // User-BorrowedBook Relationship
            modelBuilder.Entity<BorrowedBook>()
                .HasOne(bb => bb.User)
                .WithMany(u => u.BorrowedBooks)
                .HasForeignKey(bb => bb.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // User-PurchasedBook Relationship
            modelBuilder.Entity<PurchasedBook>()
                .HasOne(pb => pb.User)
                .WithMany(u => u.PurchasedBooks)
                .HasForeignKey(pb => pb.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // User-Rating Relationship (Cascade Delete)
            modelBuilder.Entity<Rating>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Book-BorrowedBook Relationship
            modelBuilder.Entity<BorrowedBook>()
                .HasOne(bb => bb.Book)
                .WithMany()
                .HasForeignKey(bb => bb.BookId)
                .OnDelete(DeleteBehavior.Cascade);

            // Book-PurchasedBook Relationship
            modelBuilder.Entity<PurchasedBook>()
                .HasOne(pb => pb.Book)
                .WithMany()
                .HasForeignKey(pb => pb.BookId)
                .OnDelete(DeleteBehavior.Cascade);

            // Book-Rating Relationship
            modelBuilder.Entity<Rating>()
                .HasOne(r => r.Book)
                .WithMany(b => b.Ratings)
                .HasForeignKey(r => r.BookId)
                .OnDelete(DeleteBehavior.Cascade);

            // Waiting List Entry Relationships
            modelBuilder.Entity<WaitingListEntry>()
                .HasOne(wl => wl.User)
                .WithMany()
                .HasForeignKey(wl => wl.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WaitingListEntry>()
                .HasOne(wl => wl.Book)
                .WithMany()
                .HasForeignKey(wl => wl.BookId)
                .OnDelete(DeleteBehavior.Restrict);

            // Default Values and Field Configurations
            modelBuilder.Entity<User>()
                .Property(u => u.MaxBorrowableBooks)
                .HasDefaultValue(3);

            modelBuilder.Entity<WaitingListEntry>()
                .Property(wl => wl.NotificationSent)
                .HasDefaultValue(false);
        }
    }
}
