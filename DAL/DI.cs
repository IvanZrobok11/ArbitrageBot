using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DAL;

public static class DI
{
    public static void AddDAL(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));
    }

    public static void AddDAL(this IServiceCollection services, string databaseName)
    {
        var dbFolder = GetDBDirectory();

        var dbPath = Path.Combine(dbFolder, databaseName);
        services.AddDbContext<AppDbContext>(options => options.UseSqlite($"Data Source={dbPath}"));
    }

    private static string GetDBDirectory()
    {
        var solutionFolder = Directory.GetParent(Directory.GetCurrentDirectory())?.FullName ?? Directory.GetCurrentDirectory();
        var dbFolder = Path.Combine(solutionFolder, "data");
        Directory.CreateDirectory(dbFolder);
        return dbFolder;
    }
}
