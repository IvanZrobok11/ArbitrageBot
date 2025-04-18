using DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace DAL;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<UserConfiguration> UserConfigurations { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<BlackAsset> BlackAssets { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        //optionsBuilder.EnableSensitiveDataLogging();
    }
}
