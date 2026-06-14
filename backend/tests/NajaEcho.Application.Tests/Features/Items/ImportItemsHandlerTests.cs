using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NajaEcho.Application.Abstractions;
using NajaEcho.Application.Features.Items.ImportItems;
using NajaEcho.Application.Features.Ships.ImportShips;
using NajaEcho.Domain.ItemCategories;
using NajaEcho.Domain.Items;

namespace NajaEcho.Application.Tests.Features.Items;

public sealed class ImportItemsHandlerTests
{
    private sealed class FakeCategoryRepo : IItemCategoryRepository
    {
        public List<ItemCategory> Categories { get; } = [];

        public Task<(int Inserted, int Updated, int Unchanged)> BulkUpsertAsync(
            IReadOnlyList<ItemCategory> incoming, CancellationToken ct) =>
            Task.FromResult((0, 0, 0));

        public Task<IReadOnlyList<ItemCategory>> GetAllAsync(CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<ItemCategory>>(Categories);

        public Task<IReadOnlyList<ItemCategory>> GetEligibleAsync(CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<ItemCategory>>(Categories.Where(c => c.Type == "item").ToList());

        public Task<DateTimeOffset?> GetLastRefreshedAtAsync(CancellationToken ct) =>
            Task.FromResult<DateTimeOffset?>(null);

        public Task<int> GetActiveItemCountAsync(int categoryUexId, CancellationToken ct) =>
            Task.FromResult(0);

        public Task<DateTimeOffset?> GetLastImportedAtAsync(int categoryUexId, CancellationToken ct) =>
            Task.FromResult<DateTimeOffset?>(null);
    }

    private sealed class FakeItemRepo : IItemRepository
    {
        public int CallCount { get; private set; }
        public bool Throws { get; set; }
        public (int, int, int, int, int) ReturnValue { get; set; } = (0, 0, 0, 0, 0);

        public Task<(int Inserted, int Updated, int Unchanged, int SoftDeleted, int Restored)> BulkUpsertForCategoryAsync(
            int idCategory, IReadOnlyList<Item> incoming, CancellationToken ct)
        {
            CallCount++;
            if (Throws) throw new InvalidOperationException("DB error");
            return Task.FromResult(ReturnValue);
        }
    }

    private sealed class FakeItemClient : IUexItemClient
    {
        public IReadOnlyList<JsonDocument> Records { get; set; } = [];
        public bool Throws { get; set; }

        public Task<IReadOnlyList<JsonDocument>> FetchItemsByCategoryAsync(int categoryId, CancellationToken ct)
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

    private static ItemCategory MakeCategory(int uexId, string type = "item") =>
        new() { UexId = uexId, Type = type, Name = $"Cat{uexId}", ImportedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow, RawData = JsonDocument.Parse("{}") };

    private static JsonDocument MakeItemRecord(string uuid, int id = 1) =>
        JsonDocument.Parse($$"""{"id":{{id}},"uuid":"{{uuid}}","name":"Item{{id}}","id_category":1}""");

    private static JsonDocument MakeItemNoUuid(int id = 99) =>
        JsonDocument.Parse($$"""{"id":{{id}},"name":"NoUUID"}""");

    private ImportItemsHandler CreateHandler(
        FakeCategoryRepo catRepo, IItemRepository itemRepo, FakeItemClient client, FakeCoordinator coordinator) =>
        new(catRepo, itemRepo, client, coordinator, NullLogger<ImportItemsHandler>.Instance);

    // Single-category path: UUID items call repository
    [Fact]
    public async Task HandleAsync_SingleCategory_CallsRepositoryWithMappedItems()
    {
        var catRepo = new FakeCategoryRepo();
        catRepo.Categories.Add(MakeCategory(1));
        var itemRepo = new FakeItemRepo { ReturnValue = (2, 0, 0, 0, 0) };
        var client = new FakeItemClient
        {
            Records = [MakeItemRecord("a1b2c3d4-e5f6-7890-abcd-ef1234567890"), MakeItemRecord("b2c3d4e5-f6a7-8901-bcde-f12345678901", 2)]
        };
        var handler = CreateHandler(catRepo, itemRepo, client, new FakeCoordinator());

        var result = await handler.HandleAsync(new ImportItemsCommand(1));

        result.ItemsInserted.Should().Be(2);
        result.CategoriesProcessed.Should().Be(1);
        result.CategoriesSucceeded.Should().Be(1);
        result.Status.Should().Be(ImportStatus.Success);
        itemRepo.CallCount.Should().Be(1);
    }

    // Records without uuid field get uuid="" and are still passed to the repo (identified by uex_id)
    [Fact]
    public async Task HandleAsync_NullUuidRecords_PassedToRepoWithEmptyUuid()
    {
        var catRepo = new FakeCategoryRepo();
        catRepo.Categories.Add(MakeCategory(1));
        var itemRepo = new FakeItemRepo { ReturnValue = (2, 0, 0, 0, 0) };
        var client = new FakeItemClient
        {
            Records = [MakeItemRecord("a1b2c3d4-e5f6-7890-abcd-ef1234567890"), MakeItemNoUuid()]
        };
        var handler = CreateHandler(catRepo, itemRepo, client, new FakeCoordinator());

        var result = await handler.HandleAsync(new ImportItemsCommand(1));

        result.ItemsSkippedNoUuid.Should().Be(0);
        result.ItemsFetched.Should().Be(2);
        result.ItemsInserted.Should().Be(2);
        itemRepo.CallCount.Should().Be(1);
    }

    // Lock not available → ImportAlreadyInProgressException
    [Fact]
    public async Task HandleAsync_LockUnavailable_ThrowsImportAlreadyInProgressException()
    {
        var coordinator = new FakeCoordinator();
        coordinator.TryAcquire();

        var catRepo = new FakeCategoryRepo();
        catRepo.Categories.Add(MakeCategory(1));
        var handler = CreateHandler(catRepo, new FakeItemRepo(), new FakeItemClient(), coordinator);

        await handler.Invoking(h => h.HandleAsync(new ImportItemsCommand(1)))
            .Should().ThrowAsync<ImportAlreadyInProgressException>();
    }

    // Unknown categoryUexId → result with Failed status
    [Fact]
    public async Task HandleAsync_UnknownCategory_ReturnsFailedResult()
    {
        var catRepo = new FakeCategoryRepo();
        var handler = CreateHandler(catRepo, new FakeItemRepo(), new FakeItemClient(), new FakeCoordinator());

        var result = await handler.HandleAsync(new ImportItemsCommand(999));

        result.Status.Should().Be(ImportStatus.Failed);
        result.Errors.Should().HaveCount(1);
        result.Errors[0].CategoryUexId.Should().Be(999);
    }

    // UEX client throws → returns Failed status with error detail, releases lock
    [Fact]
    public async Task HandleAsync_ClientFails_ReturnsFailedStatusAndReleasesLock()
    {
        var catRepo = new FakeCategoryRepo();
        catRepo.Categories.Add(MakeCategory(1));
        var coordinator = new FakeCoordinator();
        var client = new FakeItemClient { Throws = true };
        var handler = CreateHandler(catRepo, new FakeItemRepo(), client, coordinator);

        var result = await handler.HandleAsync(new ImportItemsCommand(1));

        result.Status.Should().Be(ImportStatus.Failed);
        result.CategoriesFailed.Should().Be(1);
        result.Errors.Should().HaveCount(1);
        coordinator.IsHeld.Should().BeFalse();
    }

    // Lock released after success
    [Fact]
    public async Task HandleAsync_ReleasesLockAfterSuccess()
    {
        var coordinator = new FakeCoordinator();
        var catRepo = new FakeCategoryRepo();
        catRepo.Categories.Add(MakeCategory(1));
        var handler = CreateHandler(catRepo, new FakeItemRepo(), new FakeItemClient(), coordinator);

        await handler.HandleAsync(new ImportItemsCommand(1));

        coordinator.IsHeld.Should().BeFalse();
    }

    // All-category path: all eligible categories processed
    [Fact]
    public async Task HandleAsync_AllCategories_ProcessesAllEligibleCategories()
    {
        var catRepo = new FakeCategoryRepo();
        catRepo.Categories.Add(MakeCategory(1));
        catRepo.Categories.Add(MakeCategory(2));
        catRepo.Categories.Add(MakeCategory(3, "vehicle")); // not eligible
        var itemRepo = new FakeItemRepo { ReturnValue = (5, 0, 0, 0, 0) };
        var client = new FakeItemClient
        {
            Records = [MakeItemRecord("a1b2c3d4-e5f6-7890-abcd-ef1234567890")]
        };
        var handler = CreateHandler(catRepo, itemRepo, client, new FakeCoordinator());

        var result = await handler.HandleAsync(new ImportItemsCommand(null));

        result.CategoriesProcessed.Should().Be(2);
        result.CategoriesSucceeded.Should().Be(2);
        result.CategoriesFailed.Should().Be(0);
        result.Status.Should().Be(ImportStatus.Success);
        itemRepo.CallCount.Should().Be(2);
    }

    // Per-category failure: remaining categories continue
    [Fact]
    public async Task HandleAsync_AllCategories_PerCategoryFailureDoesNotStopOthers()
    {
        var catRepo = new FakeCategoryRepo();
        catRepo.Categories.Add(MakeCategory(1));
        catRepo.Categories.Add(MakeCategory(2));
        var itemRepo = new FakeItemRepo();

        // Use a client that fails on all calls to observe per-category failure isolation
        var client = new FakeItemClient();
        var handler = CreateHandler(catRepo, itemRepo, client, new FakeCoordinator());

        // Patch: make itemRepo throw on first call only
        itemRepo.Throws = false;
        // We can't easily do per-call failure with the simple fake, so test with
        // a failing client for all categories and confirm CompletedWithErrors
        client.Throws = true;

        var result = await handler.HandleAsync(new ImportItemsCommand(null));

        result.CategoriesFailed.Should().Be(2);
        result.CategoriesSucceeded.Should().Be(0);
        result.Status.Should().Be(ImportStatus.Failed);
        result.Errors.Should().HaveCount(2);
    }

    // All-category path: status = CompletedWithErrors when some categories fail
    [Fact]
    public async Task HandleAsync_AllCategories_PartialFailure_StatusIsCompletedWithErrors()
    {
        var catRepo = new FakeCategoryRepo();
        catRepo.Categories.Add(MakeCategory(1));
        catRepo.Categories.Add(MakeCategory(2));

        // Create a handler variant where cat 1 succeeds, cat 2 fails
        // We need itemRepo to fail on second call
        var itemRepo = new PerCallFailItemRepo(failOnCall: 2);
        var client = new FakeItemClient { Records = [MakeItemRecord("a1b2c3d4-e5f6-7890-abcd-ef1234567890")] };
        var handler = CreateHandler(catRepo, itemRepo, client, new FakeCoordinator());

        var result = await handler.HandleAsync(new ImportItemsCommand(null));

        result.CategoriesSucceeded.Should().Be(1);
        result.CategoriesFailed.Should().Be(1);
        result.Status.Should().Be(ImportStatus.CompletedWithErrors);
    }

    private sealed class PerCallFailItemRepo : IItemRepository
    {
        private readonly int _failOnCall;
        private int _callCount;

        public PerCallFailItemRepo(int failOnCall) => _failOnCall = failOnCall;

        public Task<(int Inserted, int Updated, int Unchanged, int SoftDeleted, int Restored)> BulkUpsertForCategoryAsync(
            int idCategory, IReadOnlyList<Item> incoming, CancellationToken ct)
        {
            _callCount++;
            if (_callCount == _failOnCall) throw new InvalidOperationException($"Failure on category call {_callCount}");
            return Task.FromResult((incoming.Count, 0, 0, 0, 0));
        }
    }
}
