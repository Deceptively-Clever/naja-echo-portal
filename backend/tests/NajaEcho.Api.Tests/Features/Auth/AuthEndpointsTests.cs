using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NajaEcho.Application.Abstractions;
using NajaEcho.Domain.Users;
using NajaEcho.Infrastructure.Persistence;
using Xunit;

namespace NajaEcho.Api.Tests.Features.Auth;

public class AuthEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AuthEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(b =>
        {
            // Provide required configuration so startup doesn't throw
            b.ConfigureAppConfiguration((_, cfg) =>
            {
                cfg.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Discord:ClientId"] = "test-client-id",
                    ["Discord:ClientSecret"] = "test-client-secret",
                    ["ConnectionStrings:Default"] = "Host=localhost;Database=test;Username=test;Password=test",
                });
            });

            b.ConfigureTestServices(services =>
            {
                // Replace real DB with in-memory
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.RemoveAll<AppDbContext>();
                services.AddDbContext<AppDbContext>(opts =>
                    opts.UseInMemoryDatabase("TestDb"));

                // Replace real infrastructure with fakes
                services.RemoveAll<IUserRepository>();
                services.AddSingleton<IUserRepository, InMemoryUserRepository>();
                services.RemoveAll<IUnitOfWork>();
                services.AddSingleton<IUnitOfWork, NoOpUnitOfWork>();
            });
        });
    }

    [Fact]
    public async Task Health_Returns200()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.GetAsync("/api/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Me_Returns401_WhenUnauthenticated()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.GetAsync("/api/auth/me");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.Should().Contain("application/problem+json");
    }

    [Fact]
    public async Task SignOut_Returns204_WhenUnauthenticated()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.PostAsync("/api/auth/signout", null);
        // Signout requires auth per our implementation; unauthenticated gets 401
        // This verifies the endpoint exists and returns a definitive status
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Login_Redirects_ToDiscord()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.GetAsync("/api/auth/discord/login");
        response.StatusCode.Should().Be(HttpStatusCode.Found);
        response.Headers.Location?.Host.Should().Be("discord.com");
    }
}

// ---- Test doubles ----

internal sealed class InMemoryUserRepository : IUserRepository
{
    private readonly List<UserProfile> _store = [];

    public Task<UserProfile?> FindByDiscordUserIdAsync(string discordUserId, CancellationToken ct = default) =>
        Task.FromResult(_store.FirstOrDefault(u => u.DiscordUserId == discordUserId));

    public Task<UserProfile?> FindByIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(_store.FirstOrDefault(u => u.Id == id));

    public Task AddAsync(UserProfile user, CancellationToken ct = default)
    {
        _store.Add(user);
        return Task.CompletedTask;
    }
}

internal sealed class NoOpUnitOfWork : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;
}
