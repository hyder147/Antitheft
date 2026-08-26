using AntiTheftApp.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AntiTheftApp.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<Command> Commands => Set<Command>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<Device>()
            .HasOne(d => d.User)
            .WithMany(u => u.Devices)
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Command>()
            .HasOne(c => c.Device)
            .WithMany(d => d.Commands)
            .HasForeignKey(c => c.DeviceId)
            .OnDelete(DeleteBehavior.Cascade);

        // Store enums as readable strings instead of raw ints in the DB
        modelBuilder.Entity<Command>()
            .Property(c => c.Type)
            .HasConversion<string>();

        modelBuilder.Entity<Command>()
            .Property(c => c.Status)
            .HasConversion<string>();

        base.OnModelCreating(modelBuilder);
    }
}
