using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using NajaEcho.Infrastructure.Persistence;
using Xunit;

namespace NajaEcho.Api.Tests;

// Runs once before any test in this assembly. Each WebApplicationFactory creates OS file
// watchers via PhysicalFilesWatcher; on WSL/Linux the inotify limit (128) is exhausted when
// many factories exist in the same process. Polling never creates inotify descriptors.
internal static class TestAssemblySetup
{
    [ModuleInitializer]
    public static void Initialize() =>
        Environment.SetEnvironmentVariable("DOTNET_USE_POLLING_FILE_WATCHER", "1");
}

[CollectionDefinition("ApiTests", DisableParallelization = true)]
public sealed class ApiTestCollection;

internal static class TestServiceCollectionExtensions
{
    internal static IServiceCollection ReplaceWithInMemoryDb(this IServiceCollection services, string dbName)
    {
        // Remove the pre-built options object
        services.RemoveAll<DbContextOptions<AppDbContext>>();
        services.RemoveAll<AppDbContext>();

        // Also remove the IConfigureOptions callbacks that register Npgsql — these accumulate
        // and cause EF Core to see both Npgsql and InMemory providers on the same options builder
        var configDescriptors = services
            .Where(d => d.ServiceType == typeof(IConfigureOptions<DbContextOptions<AppDbContext>>))
            .ToList();
        foreach (var d in configDescriptors)
            services.Remove(d);

        services.AddDbContext<AppDbContext>(opts =>
            opts.UseInMemoryDatabase(dbName)
                .EnableServiceProviderCaching(false));

        return services;
    }
}
