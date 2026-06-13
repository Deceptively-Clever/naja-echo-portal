using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NajaEcho.Application.Abstractions;
using NajaEcho.Application.Features.Ships.ImportShips;
using NajaEcho.Domain.Ships;

namespace NajaEcho.Application.Tests.Features.Ships;

public sealed class ImportShipsHandlerTests
{
    // Fakes
    private sealed class FakeRepository : IShipRepository
    {
        public List<Ship> Ships { get; } = [];
        public bool BulkUpsertThrows { get; set; }
        public int BulkUpsertCallCount { get; private set; }

        public Task<(IReadOnlyList<Ship>, int)> GetPagedAsync(int page, int pageSize, CancellationToken ct)
        {
            IReadOnlyList<Ship> items = [..Ships];
            return Task.FromResult((items, Ships.Count));
        }

        public Task<Ship?> GetByIdAsync(Guid id, CancellationToken ct)
            => Task.FromResult(Ships.FirstOrDefault(s => s.Id == id));

        public Task<Ship?> GetByUexIdAsync(int uexId, CancellationToken ct)
            => Task.FromResult(Ships.FirstOrDefault(s => s.UexId == uexId));

        public Task<(int, int, int, int)> BulkUpsertAsync(IReadOnlyList<Ship> incoming, CancellationToken ct)
        {
            BulkUpsertCallCount++;
            if (BulkUpsertThrows) throw new InvalidOperationException("DB error");

            var added = 0; var updated = 0; var reactivated = 0; var softDeleted = 0;
            var incomingIds = incoming.Select(s => s.UexId).ToHashSet();
            var now = DateTimeOffset.UtcNow;

            foreach (var ship in incoming)
            {
                var existing = Ships.FirstOrDefault(s => s.UexId == ship.UexId);
                if (existing is null)
                {
                    ship.Id = Guid.NewGuid(); ship.Status = ShipStatus.Active;
                    ship.ImportedAt = now; ship.UpdatedAt = now;
                    Ships.Add(ship); added++;
                }
                else if (existing.Status == ShipStatus.SoftDeleted)
                {
                    existing.Status = ShipStatus.Active; existing.SoftDeletedAt = null; reactivated++;
                }
                else { updated++; }
            }

            foreach (var s in Ships.Where(s => s.Status == ShipStatus.Active && !incomingIds.Contains(s.UexId)).ToList())
            {
                s.Status = ShipStatus.SoftDeleted; s.SoftDeletedAt = now; softDeleted++;
            }

            return Task.FromResult((added, updated, reactivated, softDeleted));
        }

        public Task<IReadOnlyList<int>> GetAllActiveUexIdsAsync(CancellationToken ct)
            => Task.FromResult<IReadOnlyList<int>>(Ships.Where(s => s.Status == ShipStatus.Active).Select(s => s.UexId).ToList());
    }

    private sealed class FakeVehicleClient : IUexVehicleClient
    {
        public IReadOnlyList<JsonDocument> Records { get; set; } = [];
        public bool Throws { get; set; }

        public Task<IReadOnlyList<JsonDocument>> FetchAllVehiclesAsync(CancellationToken ct)
        {
            if (Throws) throw new HttpRequestException("Feed unavailable");
            return Task.FromResult(Records);
        }
    }

    private sealed class FakeCoordinator : IImportCoordinator
    {
        public bool IsHeld { get; private set; }
        public bool TryAcquire() { if (IsHeld) return false; IsHeld = true; return true; }
        public void Release() => IsHeld = false;
    }

    private static JsonDocument MakeRecord(int id, string name, string? company = null) =>
        JsonDocument.Parse($$"""{"id":{{id}},"name":"{{name}}","company_name":{{(company is null ? "null" : $"\"{company}\"")}},"name_full":null,"uuid":null}""");

    private ImportShipsHandler CreateHandler(FakeRepository repo, FakeVehicleClient client, FakeCoordinator coordinator) =>
        new(repo, client, coordinator, NullLogger<ImportShipsHandler>.Instance);

    [Fact]
    public async Task HandleAsync_NewRecords_ReturnsAddedCount()
    {
        var repo = new FakeRepository();
        var client = new FakeVehicleClient { Records = [MakeRecord(1, "100i"), MakeRecord(2, "Avenger")] };
        var handler = CreateHandler(repo, client, new FakeCoordinator());

        var result = await handler.HandleAsync(new ImportShipsCommand());

        result.Added.Should().Be(2);
        result.Updated.Should().Be(0);
        result.Reactivated.Should().Be(0);
        result.SoftDeleted.Should().Be(0);
        result.Total.Should().Be(2);
    }

    [Fact]
    public async Task HandleAsync_ExistingRecord_CountsAsUpdated()
    {
        var repo = new FakeRepository();
        repo.Ships.Add(new Ship { Id = Guid.NewGuid(), UexId = 1, Name = "100i", Status = ShipStatus.Active, ImportedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow, RawData = MakeRecord(1, "100i") });
        var client = new FakeVehicleClient { Records = [MakeRecord(1, "100i Updated")] };
        var handler = CreateHandler(repo, client, new FakeCoordinator());

        var result = await handler.HandleAsync(new ImportShipsCommand());

        result.Updated.Should().Be(1);
        result.Added.Should().Be(0);
    }

    [Fact]
    public async Task HandleAsync_SoftDeletedRecordReappears_CountsAsReactivated()
    {
        var repo = new FakeRepository();
        repo.Ships.Add(new Ship { Id = Guid.NewGuid(), UexId = 1, Name = "100i", Status = ShipStatus.SoftDeleted, SoftDeletedAt = DateTimeOffset.UtcNow, ImportedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow, RawData = MakeRecord(1, "100i") });
        var client = new FakeVehicleClient { Records = [MakeRecord(1, "100i")] };
        var handler = CreateHandler(repo, client, new FakeCoordinator());

        var result = await handler.HandleAsync(new ImportShipsCommand());

        result.Reactivated.Should().Be(1);
    }

    [Fact]
    public async Task HandleAsync_ActiveRecordMissingFromFeed_CountsAsSoftDeleted()
    {
        var repo = new FakeRepository();
        repo.Ships.Add(new Ship { Id = Guid.NewGuid(), UexId = 99, Name = "Gone", Status = ShipStatus.Active, ImportedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow, RawData = MakeRecord(99, "Gone") });
        var client = new FakeVehicleClient { Records = [MakeRecord(1, "100i")] };
        var handler = CreateHandler(repo, client, new FakeCoordinator());

        var result = await handler.HandleAsync(new ImportShipsCommand());

        result.SoftDeleted.Should().Be(1);
    }

    [Fact]
    public async Task HandleAsync_ZeroRecordFeed_LeavesDataUnchangedAndReturnsWarning()
    {
        var repo = new FakeRepository();
        repo.Ships.Add(new Ship { Id = Guid.NewGuid(), UexId = 1, Name = "100i", Status = ShipStatus.Active, ImportedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow, RawData = MakeRecord(1, "100i") });
        var client = new FakeVehicleClient { Records = [] };
        var handler = CreateHandler(repo, client, new FakeCoordinator());

        var result = await handler.HandleAsync(new ImportShipsCommand());

        repo.BulkUpsertCallCount.Should().Be(0);
        result.Warning.Should().NotBeNull();
        result.Total.Should().Be(0);
    }

    [Fact]
    public async Task HandleAsync_FeedFails_ThrowsAndDoesNotCallRepository()
    {
        var repo = new FakeRepository();
        var client = new FakeVehicleClient { Throws = true };
        var handler = CreateHandler(repo, client, new FakeCoordinator());

        await handler.Invoking(h => h.HandleAsync(new ImportShipsCommand()))
            .Should().ThrowAsync<HttpRequestException>();

        repo.BulkUpsertCallCount.Should().Be(0);
    }

    [Fact]
    public async Task HandleAsync_CoordinatorAlreadyHeld_ThrowsImportAlreadyInProgress()
    {
        var coordinator = new FakeCoordinator();
        coordinator.TryAcquire();

        var repo = new FakeRepository();
        var client = new FakeVehicleClient { Records = [MakeRecord(1, "100i")] };
        var handler = CreateHandler(repo, client, coordinator);

        await handler.Invoking(h => h.HandleAsync(new ImportShipsCommand()))
            .Should().ThrowAsync<ImportAlreadyInProgressException>();
    }

    [Fact]
    public async Task HandleAsync_ReleasesLockAfterSuccess()
    {
        var coordinator = new FakeCoordinator();
        var repo = new FakeRepository();
        var client = new FakeVehicleClient { Records = [MakeRecord(1, "100i")] };
        var handler = CreateHandler(repo, client, coordinator);

        await handler.HandleAsync(new ImportShipsCommand());

        coordinator.IsHeld.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_ReleasesLockAfterFailure()
    {
        var coordinator = new FakeCoordinator();
        var repo = new FakeRepository { BulkUpsertThrows = true };
        var client = new FakeVehicleClient { Records = [MakeRecord(1, "100i")] };
        var handler = CreateHandler(repo, client, coordinator);

        await handler.Invoking(h => h.HandleAsync(new ImportShipsCommand()))
            .Should().ThrowAsync<InvalidOperationException>();

        coordinator.IsHeld.Should().BeFalse();
    }
}
