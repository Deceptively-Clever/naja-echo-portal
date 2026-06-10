using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NajaEcho.Domain.Users;
using NajaEcho.Infrastructure.Persistence;
using NajaEcho.Infrastructure.Persistence.Repositories;
using Testcontainers.PostgreSql;
using Xunit;

namespace NajaEcho.Infrastructure.Tests.Persistence;

public class UserRepositoryTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private AppDbContext _db = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .UseSnakeCaseNamingConvention()
            .Options;

        _db = new AppDbContext(opts);
        await _db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    private static readonly DateTimeOffset Now = new(2026, 6, 8, 12, 0, 0, TimeSpan.Zero);

    private static UserProfile MakeUser(string discordId = "disc123") =>
        UserProfile.CreateFromDiscord(new DiscordProfile
        {
            Id = discordId,
            Username = "user",
            GlobalName = "User",
            Avatar = null,
        }, Now);

    [Fact]
    public async Task FindByDiscordUserIdAsync_ReturnsNull_WhenNotFound()
    {
        var repo = new UserRepository(_db);
        var result = await repo.FindByDiscordUserIdAsync("unknown", CancellationToken.None);
        result.Should().BeNull();
    }

    [Fact]
    public async Task AddAsync_PersistsUser_RetrievableByDiscordId()
    {
        var repo = new UserRepository(_db);
        var user = MakeUser("disc999");

        await repo.AddAsync(user, CancellationToken.None);
        await _db.SaveChangesAsync();

        var found = await repo.FindByDiscordUserIdAsync("disc999", CancellationToken.None);
        found.Should().NotBeNull();
        found!.Id.Should().Be(user.Id);
        found.DisplayName.Should().Be("User");
    }

    [Fact]
    public async Task AddAsync_TwiceWithSameDiscordId_ThrowsOnSave()
    {
        var repo = new UserRepository(_db);
        await repo.AddAsync(MakeUser("dup123"), CancellationToken.None);
        await _db.SaveChangesAsync();

        var db2 = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .UseSnakeCaseNamingConvention()
            .Options);
        var repo2 = new UserRepository(db2);

        await repo2.AddAsync(MakeUser("dup123"), CancellationToken.None);
        var act = async () => await db2.SaveChangesAsync();

        await act.Should().ThrowAsync<Exception>(); // unique constraint violation
    }
}
