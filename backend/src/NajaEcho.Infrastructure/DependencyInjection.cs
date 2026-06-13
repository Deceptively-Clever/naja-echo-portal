using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NajaEcho.Application.Abstractions;
using NajaEcho.Application.Features.Auth.GetCurrentUser;
using NajaEcho.Application.Features.Auth.SignInWithDiscord;
using NajaEcho.Application.Features.Ships.GetShipById;
using NajaEcho.Application.Features.Ships.GetShips;
using NajaEcho.Application.Features.Ships.ImportShips;
using NajaEcho.Infrastructure.Identity;
using NajaEcho.Infrastructure.Persistence;
using NajaEcho.Infrastructure.Ships;

namespace NajaEcho.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(opts =>
            opts.UseNpgsql(configuration.GetConnectionString("Default"))
                .UseSnakeCaseNamingConvention());

        services.AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<AppDbContext>();

        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<IExternalLoginService, DiscordExternalLoginService>();

        services.AddScoped<SignInWithDiscordHandler>();
        services.AddScoped<GetCurrentUserHandler>();

        // Ships
        services.AddScoped<IShipRepository, ShipRepository>();
        services.AddHttpClient<IUexVehicleClient, UexVehicleClient>(client =>
        {
            var baseUrl = configuration["UexVehicleClient:BaseUrl"] ?? "https://api.uexcorp.uk/2.0/";
            client.BaseAddress = new Uri(baseUrl);
        });
        services.AddSingleton<IImportCoordinator, ImportCoordinator>();
        services.AddScoped<ImportShipsHandler>();
        services.AddScoped<GetShipsHandler>();
        services.AddScoped<GetShipByIdHandler>();

        // Admin role seeder
        services.AddScoped<AdminRoleSeeder>();

        return services;
    }
}
