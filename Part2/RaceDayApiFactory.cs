using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;

namespace RaceDay.Api.Tests;

/// <summary>
/// Spins up the real RaceDay API in-process, swapping the SQL Server DbContext for a fresh
/// EF Core InMemory database per factory instance so tests never touch a real database.
/// </summary>
public class RaceDayApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<RaceDayContext>));
            if (descriptor is not null) services.Remove(descriptor);

            services.AddDbContext<RaceDayContext>(options =>
            {
                options.UseInMemoryDatabase(_dbName);
            });
        });
    }
}
