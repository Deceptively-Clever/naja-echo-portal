using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NajaEcho.Application.Abstractions;
using NajaEcho.Application.Features.Warehouse.ChangeInventoryQuantity;
using NajaEcho.Application.Features.Warehouse.GetInventory;
using NajaEcho.Application.Features.Warehouse.GetInventoryFilters;
using NajaEcho.Application.Features.Warehouse.SearchCatalogItems;
using NajaEcho.Application.Features.Warehouse.TransferInventoryItem;
using Xunit;

namespace NajaEcho.Application.Tests.Features.Warehouse;

public sealed class TransferInventoryItemHandlerTests
{
    private static readonly Guid KnownRowId = Guid.NewGuid();
    private static readonly Guid KnownStationId = Guid.NewGuid();

    private sealed class FakeWarehouseRepo : IWarehouseInventoryRepository
    {
        public bool RowExists { get; set; } = true;
        public Guid? LastUpdatedStationId { get; private set; }
        public Guid? LastUpdatedRowId { get; private set; }

        public Task<(InventoryRowDto Row, bool IsNew)> AddOrIncrementAsync(
            Guid itemId, Guid ownerUserId, string location, int quantity, int quality, Guid? stationId, CancellationToken ct)
            => throw new NotImplementedException();

        public Task<IReadOnlyList<InventoryRowDto>> GetInventoryAsync(
            string? name, string? type, string? subtype, Guid? ownerUserId, string? location, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<InventoryRowDto>>([]);

        public Task<InventoryFiltersDto> GetInventoryFiltersAsync(CancellationToken ct)
            => Task.FromResult(new InventoryFiltersDto([], [], []));

        public Task<IReadOnlyList<CatalogItemResultDto>> SearchCatalogItemsAsync(
            string? search, int limit, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<CatalogItemResultDto>>([]);

        public Task<InventoryRowDto> UpdateQuantityAsync(Guid id, int quantity, CancellationToken ct)
            => throw new NotImplementedException();

        public Task<InventoryRowDto> UpdateItemAsync(Guid id, Guid ownerUserId, Guid stationId, int quantity, CancellationToken ct) =>
            throw new NotImplementedException();
        public Task UpdateStationAsync(Guid id, Guid stationId, CancellationToken ct)
        {
            LastUpdatedRowId = id;
            LastUpdatedStationId = stationId;
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(Guid id, CancellationToken ct)
            => Task.FromResult(RowExists);

        public Task RemoveAsync(Guid id, CancellationToken ct)
            => throw new NotImplementedException();
    }

    private sealed class FakeStationRepo : ISpaceStationRepository
    {
        public bool StationExists { get; set; } = true;

        public Task<(int, int, int, int, int)> BulkUpsertAsync(
            IReadOnlyList<JsonDocument> records, IReadOnlyDictionary<int, Guid> starSystemMap, CancellationToken ct)
            => Task.FromResult((0, 0, 0, 0, 0));

        public Task<IReadOnlyList<StationDto>> SearchActiveStationsAsync(string? search, int limit, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<StationDto>>([]);

        public Task<bool> ExistsAsync(Guid id, CancellationToken ct)
            => Task.FromResult(StationExists);
    }

    private static TransferInventoryItemHandler CreateHandler(
        FakeWarehouseRepo? repo = null,
        FakeStationRepo? stationRepo = null) =>
        new(repo ?? new FakeWarehouseRepo(),
            stationRepo ?? new FakeStationRepo(),
            NullLogger<TransferInventoryItemHandler>.Instance);

    [Fact]
    public async Task Transfer_WithValidRowAndStation_SetsStationId()
    {
        var repo = new FakeWarehouseRepo { RowExists = true };

        await CreateHandler(repo).HandleAsync(new TransferInventoryItemCommand(KnownRowId, KnownStationId), default);

        repo.LastUpdatedRowId.Should().Be(KnownRowId);
        repo.LastUpdatedStationId.Should().Be(KnownStationId);
    }

    [Fact]
    public async Task Transfer_WithUnknownRow_ThrowsInventoryRowNotFoundException()
    {
        var repo = new FakeWarehouseRepo { RowExists = false };

        var act = () => CreateHandler(repo).HandleAsync(new TransferInventoryItemCommand(KnownRowId, KnownStationId), default);

        await act.Should().ThrowAsync<InventoryRowNotFoundException>();
    }

    [Fact]
    public async Task Transfer_WithInvalidStationId_ThrowsInvalidOperationException()
    {
        var stationRepo = new FakeStationRepo { StationExists = false };

        var act = () => CreateHandler(stationRepo: stationRepo)
            .HandleAsync(new TransferInventoryItemCommand(KnownRowId, KnownStationId), default);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Transfer_DoesNotModifyLocationField()
    {
        var repo = new FakeWarehouseRepo();

        await CreateHandler(repo).HandleAsync(new TransferInventoryItemCommand(KnownRowId, KnownStationId), default);

        repo.LastUpdatedStationId.Should().Be(KnownStationId);
        repo.LastUpdatedRowId.Should().Be(KnownRowId);
    }
}
