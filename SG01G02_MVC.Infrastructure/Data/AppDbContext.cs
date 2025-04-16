using Microsoft.EntityFrameworkCore;
using SG01G02_MVC.Domain.Entities;

namespace SG01G02_MVC.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Product> Products { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        // Optional: Fluent API config
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Example: enforce required fields
            modelBuilder.Entity<Product>(entity =>
            {
                entity.Property(p => p.Name).IsRequired();
                entity.Property(p => p.Price).HasColumnType("decimal(10,2)");
            });
        }
    }
}