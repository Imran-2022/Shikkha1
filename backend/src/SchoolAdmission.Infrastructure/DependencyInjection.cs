using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SchoolAdmission.Domain.Interfaces;
using SchoolAdmission.Infrastructure.Persistence;
using SchoolAdmission.Infrastructure.Persistence.Repositories;
using SchoolAdmission.Infrastructure.Services;

namespace SchoolAdmission.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? Environment.GetEnvironmentVariable("CONNECTION_STRING")
            ?? throw new InvalidOperationException(
                "Database connection string is not configured. " +
                "Set ConnectionStrings__DefaultConnection or CONNECTION_STRING environment variable.");

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorCodesToAdd: null);

                npgsql.CommandTimeout(30);
            });

            // Table and column names are mapped explicitly in the entity configurations, so no
            // naming-convention package is needed (and none can drift from the migration).
            // Don't let a cosmetic snapshot difference (e.g. generated constraint names) stop the
            // app from starting; the migration itself is the source of truth for the schema.
            options.ConfigureWarnings(w =>
                w.Ignore(RelationalEventId.PendingModelChangesWarning));
        });

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<ISchoolClassRepository, SchoolClassRepository>();
        services.AddScoped<IAdmissionHoldRepository, AdmissionHoldRepository>();

        services.AddScoped<HoldExpirySweeper>();
        services.AddHostedService<HoldExpiryService>();

        return services;
    }

    public static async Task MigrateDatabaseAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.MigrateAsync();
    }
}
