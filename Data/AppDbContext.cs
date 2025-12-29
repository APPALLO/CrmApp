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

        // Yeni Performans İndeksleri
        modelBuilder.Entity<Customer>()
            .HasIndex(c => c.CreatedAt)
            .HasDatabaseName("IX_Customers_CreatedAt"); // Sıralama performansı için

        modelBuilder.Entity<Customer>()
            .HasIndex(c => c.FirstName)
            .HasDatabaseName("IX_Customers_FirstName"); // Arama performansı için

        modelBuilder.Entity<Customer>()
            .HasIndex(c => c.LastName)
            .HasDatabaseName("IX_Customers_LastName"); // Arama performansı için
            
        // Bileşik indeks (Ad + Soyad aramaları için)
        modelBuilder.Entity<Customer>()
            .HasIndex(c => new { c.FirstName, c.LastName })
            .HasDatabaseName("IX_Customers_Name");
    }
}
