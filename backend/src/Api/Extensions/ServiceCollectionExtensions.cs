using Api.Data;
using Api.Repositories;
using Api.Services;
using Microsoft.EntityFrameworkCore;

namespace Api.Extensions;

/// <summary>
/// Extension methods for configuring application services in the DI container.
/// Centralizes service registration following organization standards.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all application services, repositories, and dependencies.
    /// Call this method in Program.cs to configure the service container.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register Entity Framework DbContext
        services.AddDbContext<AppDbContext>(options =>
        {
            var sp = services.BuildServiceProvider();
            var config = sp.GetRequiredService<IConfiguration>();
            var connectionString = config.GetConnectionString("DefaultConnection");

            if (!string.IsNullOrWhiteSpace(connectionString) && connectionString.TrimStart().StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
            {
                options.UseSqlite(connectionString);
            }
            else
            {
                options.UseSqlServer(connectionString ?? "Server=.;Database=dotnet_react_starter;Trusted_Connection=true;TrustServerCertificate=true;");
            }
        });

        // Register repositories using AddScoped
        services.AddScoped<IUserRepository, UserRepository>();

        // Register services using AddScoped
        services.AddScoped<IAuthService, AuthService>();

        // Register authentication
        services.AddAuthentication("Bearer")
            .AddJwtBearer();

        services.AddAuthorization();

        return services;
    }
}

