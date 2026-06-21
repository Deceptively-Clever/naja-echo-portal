using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NajaEcho.Domain.Items;
using NajaEcho.Domain.Locations;
using NajaEcho.Domain.Warehouse;
using NajaEcho.Infrastructure.Identity;
using NajaEcho.Infrastructure.Persistence;
using Testcontainers.PostgreSql;
using Xunit;

namespace NajaEcho.Infrastructure.Tests.Locations;

public sealed class LocationCatalogIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _pg = new PostgreSqlBuilder()
        .WithDatabase("najaecho_loc_test")
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

    private StarSystem AddStarSystem(int uexId = 1, string name = "Sol")
    {
        var system = new StarSystem
        {
            Id = Guid.NewGuid(),
            UexId = uexId,
            Name = name,
            IsAvailable = true,
            IsVisible = true,
            Status = CatalogStatus.Active,
            RawData = JsonDocument.Parse("{}"),
            ImportedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
        _db.Set<StarSystem>().Add(system);
        return system;
    }

    private SpaceStation AddSpaceStation(Guid starSystemId, int uexId = 100, string name = "ARC-L1")
    {
        var station = new SpaceStation
        {
            Id = Guid.NewGuid(),
            UexId = uexId,
            StarSystemId = starSystemId,
            Name = name,
            IsAvailable = true,
            IsDecommissioned = false,
            IsLandable = true,
            HasRefinery = false,
            HasTradeTerminal = true,
            Status = CatalogStatus.Active,
            RawData = JsonDocument.Parse("{}"),
            ImportedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
        _db.Set<SpaceStation>().Add(station);
        return station;
    }

    [Fact]
    public async Task StarSystem_UexId_UniqueConstraint_Rejected()
    {
        AddStarSystem(uexId: 999, name: "Stanton");
        await _db.SaveChangesAsync();

        AddStarSystem(uexId: 999, name: "Pyro");

        var act = () => _db.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task SpaceStation_FK_RejectsUnknownStarSystem()
    {
        AddSpaceStation(starSystemId: Guid.NewGuid());

        var act = () => _db.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task WarehouseInventory_StationId_NullableFK_Accepted()
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            DisplayName = "Alice",
            DiscordUsername = "alice",
            UserName = "alice",
            NormalizedUserName = "ALICE",
            Email = "alice@test.com",
            NormalizedEmail = "ALICE@TEST.COM",
            SecurityStamp = Guid.NewGuid().ToString(),
        };
        var item = new Item
        {
            Id = Guid.NewGuid(),
            UexId = 1,
            IdCategory = 1,
            Name = "Widget",
            Status = ItemStatus.Active,
            RawData = JsonDocument.Parse("{}"),
            ImportedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
        _db.Set<ApplicationUser>().Add(user);
        _db.Items.Add(item);
        await _db.SaveChangesAsync();

        var entry = new WarehouseInventoryEntry
        {
            Id = Guid.NewGuid(),
            ItemId = item.Id,
            OwnerUserId = user.Id,
            Location = "Bay 1",
            Quantity = 1,
            Quality = 500,
            StationId = null,
        };
        _db.Set<WarehouseInventoryEntry>().Add(entry);
        await _db.SaveChangesAsync();

        var persisted = await _db.Set<WarehouseInventoryEntry>().FindAsync(entry.Id);
        persisted!.StationId.Should().BeNull();

        // Now attach a valid station and verify FK is enforced
        var system = AddStarSystem(uexId: 1, name: "Stanton2");
        await _db.SaveChangesAsync();
        var station = AddSpaceStation(system.Id, uexId: 200, name: "ARC-L2");
        await _db.SaveChangesAsync();

        persisted.StationId = station.Id;
        await _db.SaveChangesAsync();

        var updated = await _db.Set<WarehouseInventoryEntry>().FindAsync(entry.Id);
        updated!.StationId.Should().Be(station.Id);
    }

    [Fact]
    public async Task WarehouseMaterial_StationId_NullableFK_Accepted()
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            DisplayName = "Bob",
            DiscordUsername = "bob",
            UserName = "bob",
            NormalizedUserName = "BOB",
            Email = "bob@test.com",
            NormalizedEmail = "BOB@TEST.COM",
            SecurityStamp = Guid.NewGuid().ToString(),
        };
        var commodity = new NajaEcho.Domain.Commodities.Commodity
        {
            Id = Guid.NewGuid(),
            UexId = 1,
            Name = "Agricium",
            Status = NajaEcho.Domain.Commodities.CommodityStatus.Active,
            RawData = JsonDocument.Parse("{}"),
            ImportedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
        _db.Set<ApplicationUser>().Add(user);
        _db.Set<NajaEcho.Domain.Commodities.Commodity>().Add(commodity);
        await _db.SaveChangesAsync();

        var entry = new WarehouseMaterialEntry
        {
            Id = Guid.NewGuid(),
            CommodityId = commodity.Id,
            OwnerUserId = user.Id,
            Location = "Cargo Bay",
            Quantity = 10m,
            Quality = 500,
            StationId = null,
        };
        _db.Set<WarehouseMaterialEntry>().Add(entry);
        await _db.SaveChangesAsync();

        var persisted = await _db.Set<WarehouseMaterialEntry>().FindAsync(entry.Id);
        persisted!.StationId.Should().BeNull();

        // Attach a valid station
        var system = AddStarSystem(uexId: 2, name: "Odin");
        await _db.SaveChangesAsync();
        var station = AddSpaceStation(system.Id, uexId: 300, name: "CRU-L1");
        await _db.SaveChangesAsync();

        persisted.StationId = station.Id;
        await _db.SaveChangesAsync();

        var updated = await _db.Set<WarehouseMaterialEntry>().FindAsync(entry.Id);
        updated!.StationId.Should().Be(station.Id);
    }
}
