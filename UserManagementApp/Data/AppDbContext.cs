using Microsoft.EntityFrameworkCore;
using UserManagementApp.Models;

namespace UserManagementApp.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // important: THIS is the unique index requirement from the task.
        // It's enforced at the database level, not by application-side "check-then-insert" logic,
        // so it stays consistent even under concurrent inserts from multiple sources.
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        // store enum as string in DB for readability (optional, but nicer than raw ints)
        modelBuilder.Entity<User>()
            .Property(u => u.Status)
            .HasConversion<string>()
            .HasMaxLength(20);
    }
}