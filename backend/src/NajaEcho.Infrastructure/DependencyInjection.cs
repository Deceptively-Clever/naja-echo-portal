using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NajaEcho.Application.Abstractions;
using NajaEcho.Application.Features.Auth.GetCurrentUser;
using NajaEcho.Application.Features.Auth.SignInWithDiscord;
using NajaEcho.Infrastructure.Identity;
using NajaEcho.Infrastructure.Persistence;

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

        return services;
    }
}
