using Microsoft.EntityFrameworkCore;
using KickRateServer.Models;

namespace KickRateServer.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Game> Games { get; set; } = null!;
        public DbSet<Rating> Ratings { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<Game>()
                .HasOne(g => g.CreatedByUser)
                .WithMany(u => u.GamesCreated)
                .HasForeignKey(g => g.CreatedByUserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
