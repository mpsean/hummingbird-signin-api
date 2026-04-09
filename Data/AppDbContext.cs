using Microsoft.EntityFrameworkCore;
using LoginApi.Models;

namespace LoginApi.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User>   Users   => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.HasIndex(t => t.Slug).IsUnique();
            entity.Property(t => t.Slug).IsRequired().HasMaxLength(50);
            entity.Property(t => t.Name).IsRequired().HasMaxLength(150);
            entity.Property(t => t.FrontendUrl).IsRequired().HasMaxLength(500);
            entity.Property(t => t.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<User>(entity =>
        {
            // Email unique per tenant (same email can exist in different tenants)
            entity.HasIndex(u => new { u.TenantId, u.Email }).IsUnique();
            // Username unique per tenant
            entity.HasIndex(u => new { u.TenantId, u.Username }).IsUnique();

            entity.Property(u => u.Email).IsRequired().HasMaxLength(100);
            entity.Property(u => u.Username).IsRequired().HasMaxLength(100);
            entity.Property(u => u.Role).HasDefaultValue("User");
            entity.Property(u => u.IsActive).HasDefaultValue(true);

            entity.HasOne(u => u.Tenant)
                  .WithMany(t => t.Users)
                  .HasForeignKey(u => u.TenantId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
