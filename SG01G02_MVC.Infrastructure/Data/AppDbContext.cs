using Microsoft.EntityFrameworkCore;
using SG01G02_MVC.Domain.Entities;

namespace SG01G02_MVC.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Product> Products { get; set; }
        public DbSet<AppUser> AppUsers { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            modelBuilder.Entity<Product>(entity =>
            {
                entity.Property(p => p.Name).IsRequired();
                entity.Property(p => p.Price).HasColumnType("decimal(10,2)");
                entity.Property(p => p.ExternalReviewApiProductId).IsRequired(false).HasMaxLength(10);
            });

            modelBuilder.Entity<AppUser>(entity =>
            {
                entity.Property(u => u.Username).IsRequired();
                entity.Property(u => u.PasswordHash).IsRequired();
                entity.Property(u => u.Role).IsRequired();
            });
        }
    }
}