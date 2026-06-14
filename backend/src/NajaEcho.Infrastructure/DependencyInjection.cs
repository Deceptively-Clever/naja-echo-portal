using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NajaEcho.Application.Abstractions;
using NajaEcho.Application.Features.Auth.GetCurrentUser;
using NajaEcho.Application.Features.Auth.SignInWithDiscord;
using NajaEcho.Application.Features.Hangar.AddShipToHangar;
using NajaEcho.Application.Features.Hangar.GetMyHangar;
using NajaEcho.Application.Features.Hangar.GetOrgHangar;
using NajaEcho.Application.Features.Hangar.GetOwningMembers;
using NajaEcho.Application.Features.Hangar.ImportHangar;
using NajaEcho.Application.Features.Hangar.RemoveShipFromHangar;
using NajaEcho.Application.Features.Hangar.SearchCatalogShips;
using NajaEcho.Application.Features.Ships.GetShipById;
using NajaEcho.Application.Features.Ships.GetShips;
using NajaEcho.Application.Features.Ships.ImportShips;
using NajaEcho.Application.Features.Commodities.GetCommodities;
using NajaEcho.Application.Features.Commodities.ImportCommodities;
using NajaEcho.Application.Features.ItemCategories.GetCategories;
using NajaEcho.Application.Features.ItemCategories.RefreshCategories;
using NajaEcho.Application.Features.Items.ImportItems;
using NajaEcho.Infrastructure.Commodities;
using NajaEcho.Infrastructure.Hangar;
using NajaEcho.Infrastructure.Identity;
using NajaEcho.Infrastructure.ItemCategories;
using NajaEcho.Infrastructure.Items;
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

        // Hangar
        services.AddScoped<IHangarRepository, HangarRepository>();
        services.AddScoped<GetMyHangarHandler>();
        services.AddScoped<GetOrgHangarHandler>();
        services.AddScoped<GetOwningMembersHandler>();
        services.AddScoped<SearchCatalogShipsHandler>();
        services.AddScoped<AddShipToHangarHandler>();
        services.AddScoped<RemoveShipFromHangarHandler>();
        services.AddScoped<ImportHangarHandler>();

        // Item Categories & Items
        services.AddHttpClient<IUexCategoryClient, UexCategoryClient>(client =>
        {
            var baseUrl = configuration["UexVehicleClient:BaseUrl"] ?? "https://api.uexcorp.uk/2.0/";
            client.BaseAddress = new Uri(baseUrl);
        });
        services.AddHttpClient<IUexItemClient, UexItemClient>(client =>
        {
            var baseUrl = configuration["UexVehicleClient:BaseUrl"] ?? "https://api.uexcorp.uk/2.0/";
            client.BaseAddress = new Uri(baseUrl);
        });
        services.AddScoped<IItemCategoryRepository, ItemCategoryRepository>();
        services.AddScoped<IItemRepository, ItemRepository>();
        services.AddScoped<RefreshCategoriesHandler>();
        services.AddScoped<GetCategoriesHandler>();
        services.AddScoped<ImportItemsHandler>();

        // Commodities
        services.AddHttpClient<IUexCommodityClient, UexCommodityClient>(client =>
        {
            var baseUrl = configuration["UexVehicleClient:BaseUrl"] ?? "https://api.uexcorp.uk/2.0/";
            client.BaseAddress = new Uri(baseUrl);
        });
        services.AddScoped<ICommodityRepository, CommodityRepository>();
        services.AddScoped<GetCommoditiesHandler>();
        services.AddScoped<ImportCommoditiesHandler>();

        // Admin role seeder
        services.AddScoped<AdminRoleSeeder>();

        return services;
    }
}
