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
    private static readonly Guid KnownLocationId = Guid.NewGuid();
    private const string KnownLocationType = "Station";

    private sealed class FakeWarehouseRepo : IWarehouseInventoryRepository
    {
        public bool RowExists { get; set; } = true;
        public Guid? LastUpdatedLocationId { get; private set; }
        public string? LastUpdatedLocationType { get; private set; }
        public Guid? LastUpdatedRowId { get; private set; }

        public Task<(InventoryRowDto Row, bool IsNew)> AddOrIncrementAsync(
            Guid itemId, Guid ownerUserId, string location, int quantity, int quality, Guid? locationId, string? locationType, CancellationToken ct)
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

        public Task<InventoryRowDto> UpdateItemAsync(Guid id, Guid ownerUserId, Guid locationId, string locationType, int quantity, CancellationToken ct) =>
            throw new NotImplementedException();

        public Task UpdateLocationAsync(Guid id, Guid locationId, string locationType, CancellationToken ct)
        {
            LastUpdatedRowId = id;
            LastUpdatedLocationId = locationId;
            LastUpdatedLocationType = locationType;
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(Guid id, CancellationToken ct)
            => Task.FromResult(RowExists);

        public Task RemoveAsync(Guid id, CancellationToken ct)
            => throw new NotImplementedException();
    }

    private static TransferInventoryItemHandler CreateHandler(FakeWarehouseRepo? repo = null) =>
        new(repo ?? new FakeWarehouseRepo(), NullLogger<TransferInventoryItemHandler>.Instance);

    [Fact]
    public async Task Transfer_WithValidRow_UpdatesLocation()
    {
        var repo = new FakeWarehouseRepo { RowExists = true };

        await CreateHandler(repo).HandleAsync(new TransferInventoryItemCommand(KnownRowId, KnownLocationId, KnownLocationType), default);

        repo.LastUpdatedRowId.Should().Be(KnownRowId);
        repo.LastUpdatedLocationId.Should().Be(KnownLocationId);
        repo.LastUpdatedLocationType.Should().Be(KnownLocationType);
    }

    [Fact]
    public async Task Transfer_WithUnknownRow_ThrowsInventoryRowNotFoundException()
    {
        var repo = new FakeWarehouseRepo { RowExists = false };

        var act = () => CreateHandler(repo).HandleAsync(new TransferInventoryItemCommand(KnownRowId, KnownLocationId, KnownLocationType), default);

        await act.Should().ThrowAsync<InventoryRowNotFoundException>();
    }

    [Fact]
    public async Task Transfer_WithCityLocationType_UpdatesLocation()
    {
        var repo = new FakeWarehouseRepo { RowExists = true };

        await CreateHandler(repo).HandleAsync(new TransferInventoryItemCommand(KnownRowId, KnownLocationId, "City"), default);

        repo.LastUpdatedLocationType.Should().Be("City");
        repo.LastUpdatedRowId.Should().Be(KnownRowId);
    }
}
