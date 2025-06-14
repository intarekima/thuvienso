using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace thuvienso.Data;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        // Load cấu hình từ appsettings.json
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

        var connectionString = configuration.GetConnectionString("DefaultConnection");

        optionsBuilder.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 36)));

        return new AppDbContext(optionsBuilder.Options);
    }
}
