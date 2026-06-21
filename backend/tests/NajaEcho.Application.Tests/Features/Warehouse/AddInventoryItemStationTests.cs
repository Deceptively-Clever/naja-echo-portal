using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NajaEcho.Application.Abstractions;
using NajaEcho.Application.Features.Warehouse.AddInventoryItem;
using NajaEcho.Application.Features.Warehouse.GetInventory;
using NajaEcho.Application.Features.Warehouse.GetInventoryFilters;
using NajaEcho.Application.Features.Warehouse.SearchCatalogItems;
using NajaEcho.Application.Features.Warehouse.ShipComponents.GetShipComponentFilters;
using NajaEcho.Application.Features.Warehouse.ShipComponents.GetShipComponents;
using NajaEcho.Application.Features.Warehouse.ShipComponents.SearchSystemsCatalog;
using NajaEcho.Domain.Items;
using NajaEcho.Domain.Warehouse;
using Xunit;

namespace NajaEcho.Application.Tests.Features.Warehouse;

public sealed class AddInventoryItemStationTests
{
    private static readonly Guid KnownItemId = Guid.NewGuid();
    private static readonly Guid KnownOwnerId = Guid.NewGuid();
    private static readonly Guid KnownRowId = Guid.NewGuid();
    private static readonly Guid KnownStationId = Guid.NewGuid();

    private sealed class CapturingWarehouseRepo : IWarehouseInventoryRepository
    {
        public Guid? CapturedStationId { get; private set; } = Guid.Empty;

        public Task<(InventoryRowDto Row, bool IsNew)> AddOrIncrementAsync(
            Guid itemId, Guid ownerUserId, string location, int quantity, int quality, Guid? stationId, CancellationToken ct)
        {
            CapturedStationId = stationId;
            var row = new InventoryRowDto(KnownRowId, itemId, "Test Item", null, null, quantity, quality, ownerUserId, "Alice", location);
            return Task.FromResult((row, true));
        }

        public Task<IReadOnlyList<InventoryRowDto>> GetInventoryAsync(string? name, string? type, string? subtype, Guid? ownerUserId, string? location, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<InventoryRowDto>>([]);
        public Task<InventoryFiltersDto> GetInventoryFiltersAsync(CancellationToken ct) =>
            Task.FromResult(new InventoryFiltersDto([], [], []));
        public Task<IReadOnlyList<CatalogItemResultDto>> SearchCatalogItemsAsync(string? search, int limit, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<CatalogItemResultDto>>([]);
        public Task<InventoryRowDto> UpdateQuantityAsync(Guid id, int quantity, CancellationToken ct) =>
            throw new NotImplementedException();
        public Task<InventoryRowDto> UpdateItemAsync(Guid id, Guid ownerUserId, Guid stationId, int quantity, CancellationToken ct) =>
            throw new NotImplementedException();
        public Task UpdateStationAsync(Guid id, Guid stationId, CancellationToken ct) => Task.CompletedTask;
        public Task<bool> ExistsAsync(Guid id, CancellationToken ct) => Task.FromResult(true);
        public Task RemoveAsync(Guid id, CancellationToken ct) => throw new NotImplementedException();
    }

    private sealed class TrackingStationRepo : ISpaceStationRepository
    {
        public bool StationExists { get; set; } = true;
        public int ExistsCallCount { get; private set; }

        public Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
        {
            ExistsCallCount++;
            return Task.FromResult(StationExists);
        }

        public Task<(int, int, int, int, int)> BulkUpsertAsync(IReadOnlyList<JsonDocument> records, IReadOnlyDictionary<int, Guid> starSystemMap, CancellationToken ct = default) =>
            Task.FromResult((0, 0, 0, 0, 0));
        public Task<IReadOnlyList<StationDto>> SearchActiveStationsAsync(string? search, int limit, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<StationDto>>([]);
    }

    private sealed class FakeItemRepo : IItemRepository
    {
        public Task<Item?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult<Item?>(new Item
            {
                Id = id, Name = "Test Item", Status = ItemStatus.Active,
                Uuid = id.ToString(), UexId = 42, IdCategory = 1,
                RawData = JsonDocument.Parse("{}"),
                ImportedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow,
            });
        public Task<(int, int, int, int, int)> BulkUpsertForCategoryAsync(int idCategory, IReadOnlyList<Item> incoming, CancellationToken ct) =>
            throw new NotImplementedException();
    }

    private sealed class FakeUserRepo : IUserRepository
    {
        public Task<bool> ExistsAsync(Guid userId, CancellationToken ct) => Task.FromResult(true);
        public Task<IReadOnlyList<(Guid Id, string DisplayName)>> GetAllAsync(CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<(Guid, string)>>([]);
    }

    private sealed class FakeScRepo : IShipComponentRepository
    {
        public Task<bool> HasCachedAttributesAsync(Guid id, CancellationToken ct) => Task.FromResult(true);
        public Task SaveItemAttributesAsync(IReadOnlyList<ItemAttribute> attrs, CancellationToken ct) => Task.CompletedTask;
        public Task<IReadOnlyList<ShipComponentRowDto>> GetShipComponentsAsync(GetShipComponentsQuery q, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<ShipComponentRowDto>>([]);
        public Task<ShipComponentFiltersDto> GetShipComponentFiltersAsync(CancellationToken ct) =>
            Task.FromResult(new ShipComponentFiltersDto([], [], [], [], [], [], false, false, false));
        public Task<IReadOnlyList<SystemsCatalogItemDto>> SearchSystemsCatalogAsync(string? s, int l, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<SystemsCatalogItemDto>>([]);
    }

    private sealed class FakeAttrClient : IUexItemAttributeClient
    {
        public Task<IReadOnlyList<JsonDocument>> FetchItemAttributesAsync(int uexItemId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<JsonDocument>>([]);
    }

    private static AddInventoryItemHandler MakeHandler(
        CapturingWarehouseRepo? repo = null,
        TrackingStationRepo? stationRepo = null) =>
        new(
            repo ?? new CapturingWarehouseRepo(),
            new FakeItemRepo(),
            new FakeUserRepo(),
            new FakeScRepo(),
            new FakeAttrClient(),
            stationRepo ?? new TrackingStationRepo(),
            NullLogger<AddInventoryItemHandler>.Instance);

    [Fact]
    public async Task AddItem_WithValidStationId_PersistsStationId()
    {
        var repo = new CapturingWarehouseRepo();
        var stationRepo = new TrackingStationRepo { StationExists = true };

        await MakeHandler(repo, stationRepo).HandleAsync(
            new AddInventoryItemCommand(KnownItemId, KnownOwnerId, "Bay 1", 1, StationId: KnownStationId), default);

        repo.CapturedStationId.Should().Be(KnownStationId);
        stationRepo.ExistsCallCount.Should().Be(1);
    }

    [Fact]
    public async Task AddItem_WithInvalidStationId_ThrowsException()
    {
        var stationRepo = new TrackingStationRepo { StationExists = false };

        var act = () => MakeHandler(stationRepo: stationRepo).HandleAsync(
            new AddInventoryItemCommand(KnownItemId, KnownOwnerId, "Bay 1", 1, StationId: Guid.NewGuid()), default);

        await act.Should().ThrowAsync<Exception>("station does not exist");
    }

    [Fact]
    public async Task AddItem_WithNullStationId_PersistsNullAndSkipsExistsCheck()
    {
        var repo = new CapturingWarehouseRepo();
        var stationRepo = new TrackingStationRepo();

        await MakeHandler(repo, stationRepo).HandleAsync(
            new AddInventoryItemCommand(KnownItemId, KnownOwnerId, "Bay 1", 1, StationId: null), default);

        repo.CapturedStationId.Should().BeNull();
        stationRepo.ExistsCallCount.Should().Be(0);
    }
}
