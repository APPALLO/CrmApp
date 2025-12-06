using CrmApp.Models;
using Microsoft.EntityFrameworkCore;

namespace CrmApp.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Customer> Customers { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Index for performance
        modelBuilder.Entity<Customer>()
            .HasIndex(c => c.LastPurchaseDate);
            
        modelBuilder.Entity<Customer>()
            .HasIndex(c => c.Email)
            .IsUnique();
    }
}
