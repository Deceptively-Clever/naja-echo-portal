using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NajaEcho.Domain.Ships;
using NajaEcho.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace NajaEcho.Infrastructure.Tests.Ships;

public sealed class ShipRepositoryTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _pg = new PostgreSqlBuilder()
        .WithDatabase("najaecho_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    private AppDbContext _db = null!;

    public async Task InitializeAsync()
    {
        await _pg.StartAsync();
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_pg.GetConnectionString())
            .UseSnakeCaseNamingConvention()
            .Options;
        _db = new AppDbContext(opts);
        await _db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await _pg.DisposeAsync();
    }

    private static JsonDocument MakeRaw(int id, string name) =>
        JsonDocument.Parse($$"""{"id":{{id}},"name":"{{name}}","uuid":null,"name_full":null,"company_name":null}""");

    private static Ship MakeShip(int uexId, string name, ShipStatus status = ShipStatus.Active)
    {
        var now = DateTimeOffset.UtcNow;
        return new Ship
        {
            Id = Guid.NewGuid(), UexId = uexId, Name = name, Status = status,
            RawData = MakeRaw(uexId, name), ImportedAt = now, UpdatedAt = now,
        };
    }

    [Fact]
    public async Task GetPagedAsync_ReturnsPaginatedResultsOrderedByName()
    {
        var repo = new NajaEcho.Infrastructure.Ships.ShipRepository(_db);
        _db.Ships.AddRange(MakeShip(3, "Zebra"), MakeShip(1, "Alpha"), MakeShip(2, "Bravo"));
        await _db.SaveChangesAsync();

        var (items, total) = await repo.GetPagedAsync(1, 2);

        total.Should().Be(3);
        items.Should().HaveCount(2);
        items[0].Name.Should().Be("Alpha");
        items[1].Name.Should().Be("Bravo");
    }

    [Fact]
    public async Task BulkUpsertAsync_JsonbRoundTrip_PreservesAllFields()
    {
        var repo = new NajaEcho.Infrastructure.Ships.ShipRepository(_db);
        var raw = JsonDocument.Parse("""{"id":42,"name":"Gladius","company_name":"Aegis","custom_field":"some_value","nested":{"key":"value"}}""");
        var ship = new Ship { UexId = 42, Name = "Gladius", RawData = raw, Status = ShipStatus.Active, ImportedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow };

        await repo.BulkUpsertAsync([ship]);
        _db.ChangeTracker.Clear();

        var loaded = await _db.Ships.FirstAsync(s => s.UexId == 42);
        loaded.RawData.RootElement.GetProperty("custom_field").GetString().Should().Be("some_value");
    }

    [Fact]
    public async Task BulkUpsertAsync_TransactionalRollback_LeavesDataUnchanged()
    {
        var existing = MakeShip(1, "Existing");
        _db.Ships.Add(existing);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        // Create a repo that will trigger a unique constraint violation by adding duplicate uex_ids
        var duplicates = new List<Ship>
        {
            new() { UexId = 2, Name = "New", RawData = MakeRaw(2, "New"), Status = ShipStatus.Active, ImportedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow },
            new() { UexId = 2, Name = "Duplicate", RawData = MakeRaw(2, "Duplicate"), Status = ShipStatus.Active, ImportedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow },
        };

        // Since our BulkUpsertAsync processes them as new, the second will cause a unique constraint on uex_id
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_pg.GetConnectionString())
            .UseSnakeCaseNamingConvention()
            .Options;
        using var freshDb = new AppDbContext(opts);
        var repo2 = new NajaEcho.Infrastructure.Ships.ShipRepository(freshDb);

        await repo2.Invoking(r => r.BulkUpsertAsync(duplicates)).Should().ThrowAsync<Exception>();

        _db.ChangeTracker.Clear();
        var count = await _db.Ships.CountAsync();
        count.Should().Be(1);
    }

    [Fact]
    public async Task BulkUpsertAsync_SoftDeleteAndReactivate_WorksCorrectly()
    {
        var repo = new NajaEcho.Infrastructure.Ships.ShipRepository(_db);
        var ship = MakeShip(1, "Gladius");
        await repo.BulkUpsertAsync([ship]);
        _db.ChangeTracker.Clear();

        // Remove from feed → soft-delete
        await repo.BulkUpsertAsync([MakeShip(2, "Avenger")]);
        _db.ChangeTracker.Clear();

        var softDeleted = await _db.Ships.FirstAsync(s => s.UexId == 1);
        softDeleted.Status.Should().Be(ShipStatus.SoftDeleted);
        softDeleted.SoftDeletedAt.Should().NotBeNull();

        // Reappear in feed → reactivate
        await repo.BulkUpsertAsync([MakeShip(1, "Gladius"), MakeShip(2, "Avenger")]);
        _db.ChangeTracker.Clear();

        var reactivated = await _db.Ships.FirstAsync(s => s.UexId == 1);
        reactivated.Status.Should().Be(ShipStatus.Active);
        reactivated.SoftDeletedAt.Should().BeNull();
    }
}
