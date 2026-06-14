using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NajaEcho.Application.Abstractions;
using NajaEcho.Application.Features.ItemCategories.RefreshCategories;
using NajaEcho.Application.Features.Ships.ImportShips;
using NajaEcho.Domain.ItemCategories;

namespace NajaEcho.Application.Tests.Features.ItemCategories;

public sealed class RefreshCategoriesHandlerTests
{
    private sealed class FakeCategoryRepository : IItemCategoryRepository
    {
        public List<ItemCategory> Categories { get; } = [];
        public bool BulkUpsertThrows { get; set; }
        public int BulkUpsertCallCount { get; private set; }

        public Task<(int Inserted, int Updated, int Unchanged)> BulkUpsertAsync(
            IReadOnlyList<ItemCategory> incoming, CancellationToken ct)
        {
            BulkUpsertCallCount++;
            if (BulkUpsertThrows) throw new InvalidOperationException("DB error");
            return Task.FromResult((incoming.Count, 0, 0));
        }

        public Task<IReadOnlyList<ItemCategory>> GetAllAsync(CancellationToken ct)
            => Task.FromResult<IReadOnlyList<ItemCategory>>(Categories);

        public Task<IReadOnlyList<ItemCategory>> GetEligibleAsync(CancellationToken ct)
            => Task.FromResult<IReadOnlyList<ItemCategory>>(Categories.Where(c => c.Type == "item").ToList());

        public Task<DateTimeOffset?> GetLastRefreshedAtAsync(CancellationToken ct)
            => Task.FromResult<DateTimeOffset?>(null);

        public Task<int> GetActiveItemCountAsync(int categoryUexId, CancellationToken ct)
            => Task.FromResult(0);

        public Task<DateTimeOffset?> GetLastImportedAtAsync(int categoryUexId, CancellationToken ct)
            => Task.FromResult<DateTimeOffset?>(null);
    }

    private sealed class FakeCategoryClient : IUexCategoryClient
    {
        public IReadOnlyList<JsonDocument> Records { get; set; } = [];
        public bool Throws { get; set; }

        public Task<IReadOnlyList<JsonDocument>> FetchAllCategoriesAsync(CancellationToken ct)
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

    private static JsonDocument MakeCategoryRecord(int id, string type, string name) =>
        JsonDocument.Parse($$"""{"id":{{id}},"type":"{{type}}","name":"{{name}}","section":null,"is_game_related":1,"is_mining":0,"date_added":null,"date_modified":null}""");

    private RefreshCategoriesHandler CreateHandler(
        FakeCategoryRepository repo, FakeCategoryClient client, FakeCoordinator coordinator) =>
        new(repo, client, coordinator, NullLogger<RefreshCategoriesHandler>.Instance);

    [Fact]
    public async Task HandleAsync_NewCategories_ReturnsInsertedCount()
    {
        var repo = new FakeCategoryRepository();
        var client = new FakeCategoryClient
        {
            Records = [MakeCategoryRecord(1, "item", "Armor"), MakeCategoryRecord(2, "item", "Weapons")]
        };
        var handler = CreateHandler(repo, client, new FakeCoordinator());

        var result = await handler.HandleAsync(new RefreshCategoriesCommand());

        result.Inserted.Should().Be(2);
        result.Fetched.Should().Be(2);
        result.DurationMs.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task HandleAsync_FeedFails_ThrowsAndDoesNotCallRepository()
    {
        var repo = new FakeCategoryRepository();
        var client = new FakeCategoryClient { Throws = true };
        var handler = CreateHandler(repo, client, new FakeCoordinator());

        await handler.Invoking(h => h.HandleAsync(new RefreshCategoriesCommand()))
            .Should().ThrowAsync<HttpRequestException>();

        repo.BulkUpsertCallCount.Should().Be(0);
    }

    [Fact]
    public async Task HandleAsync_CoordinatorAlreadyHeld_ThrowsImportAlreadyInProgress()
    {
        var coordinator = new FakeCoordinator();
        coordinator.TryAcquire();

        var repo = new FakeCategoryRepository();
        var client = new FakeCategoryClient { Records = [MakeCategoryRecord(1, "item", "Armor")] };
        var handler = CreateHandler(repo, client, coordinator);

        await handler.Invoking(h => h.HandleAsync(new RefreshCategoriesCommand()))
            .Should().ThrowAsync<ImportAlreadyInProgressException>();
    }

    [Fact]
    public async Task HandleAsync_ReleasesLockAfterSuccess()
    {
        var coordinator = new FakeCoordinator();
        var repo = new FakeCategoryRepository();
        var client = new FakeCategoryClient { Records = [MakeCategoryRecord(1, "item", "Armor")] };
        var handler = CreateHandler(repo, client, coordinator);

        await handler.HandleAsync(new RefreshCategoriesCommand());

        coordinator.IsHeld.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_ReleasesLockAfterFailure()
    {
        var coordinator = new FakeCoordinator();
        var repo = new FakeCategoryRepository { BulkUpsertThrows = true };
        var client = new FakeCategoryClient { Records = [MakeCategoryRecord(1, "item", "Armor")] };
        var handler = CreateHandler(repo, client, coordinator);

        await handler.Invoking(h => h.HandleAsync(new RefreshCategoriesCommand()))
            .Should().ThrowAsync<InvalidOperationException>();

        coordinator.IsHeld.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_EmptyFeed_CallsRepositoryWithEmptyList()
    {
        var repo = new FakeCategoryRepository();
        var client = new FakeCategoryClient { Records = [] };
        var handler = CreateHandler(repo, client, new FakeCoordinator());

        var result = await handler.HandleAsync(new RefreshCategoriesCommand());

        result.Fetched.Should().Be(0);
        result.Inserted.Should().Be(0);
        repo.BulkUpsertCallCount.Should().Be(1);
    }
}
