using Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Api.Tests.Integration;

/// <summary>
/// Custom web application factory for integration testing.
/// Replaces the SQL Server DbContext with an in-memory database so auth flows can run locally.
/// </summary>
public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder webHostBuilder)
    {
        webHostBuilder.UseEnvironment("Testing");

        webHostBuilder.ConfigureServices(serviceCollection =>
        {
            serviceCollection.RemoveAll(typeof(DbContextOptions<AppDbContext>));
            serviceCollection.RemoveAll(typeof(AppDbContext));

            serviceCollection.AddDbContext<AppDbContext>(options =>
            {
                options.UseInMemoryDatabase("AuthIntegrationTestsDb");
            });
        });
    }
}
