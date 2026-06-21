using FluentAssertions;
using NajaEcho.Application.Abstractions;
using NajaEcho.Application.Features.Warehouse.GetInventory;
using NajaEcho.Application.Features.Warehouse.GetInventoryFilters;
using NajaEcho.Application.Features.Warehouse.SearchCatalogItems;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace NajaEcho.Application.Tests.Features.Warehouse;

public sealed class GetInventoryFiltersHandlerTests
{
    private sealed class FakeWarehouseRepo : IWarehouseInventoryRepository
    {
        private readonly InventoryFiltersDto _filters;

        public FakeWarehouseRepo(InventoryFiltersDto filters) => _filters = filters;

        public Task<IReadOnlyList<InventoryRowDto>> GetInventoryAsync(
            string? name, string? type, string? subtype, Guid? ownerUserId, string? location, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<InventoryRowDto>>([]);

        public Task<InventoryFiltersDto> GetInventoryFiltersAsync(CancellationToken ct) =>
            Task.FromResult(_filters);

        public Task<IReadOnlyList<CatalogItemResultDto>> SearchCatalogItemsAsync(string? search, int limit, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<CatalogItemResultDto>>([]);

        public Task<(InventoryRowDto Row, bool IsNew)> AddOrIncrementAsync(Guid itemId, Guid ownerUserId, string location, int quantity, int quality, Guid? stationId, CancellationToken ct) =>
            throw new NotImplementedException();

        public Task<InventoryRowDto> UpdateQuantityAsync(Guid id, int quantity, CancellationToken ct) =>
            throw new NotImplementedException();

        public Task RemoveAsync(Guid id, CancellationToken ct) => throw new NotImplementedException();
    }

    [Fact]
    public async Task HandleAsync_ReturnsTypesFromItemCategories()
    {
        var filters = new InventoryFiltersDto(["Armor", "Weapons"], [], []);
        var handler = new GetInventoryFiltersHandler(new FakeWarehouseRepo(filters), NullLogger<GetInventoryFiltersHandler>.Instance);

        var result = await handler.HandleAsync(new GetInventoryFiltersQuery(), default);

        result.Types.Should().BeEquivalentTo(["Armor", "Weapons"]);
    }

    [Fact]
    public async Task HandleAsync_ReturnsSubtypesFromItemCategories()
    {
        var filters = new InventoryFiltersDto([], ["Laser", "Ballistic"], []);
        var handler = new GetInventoryFiltersHandler(new FakeWarehouseRepo(filters), NullLogger<GetInventoryFiltersHandler>.Instance);

        var result = await handler.HandleAsync(new GetInventoryFiltersQuery(), default);

        result.Subtypes.Should().BeEquivalentTo(["Laser", "Ballistic"]);
    }

    [Fact]
    public async Task HandleAsync_ReturnsOwnersFromInventory()
    {
        var ownerId = Guid.NewGuid();
        var filters = new InventoryFiltersDto([], [], [new OwnerOption(ownerId, "Alice")]);
        var handler = new GetInventoryFiltersHandler(new FakeWarehouseRepo(filters), NullLogger<GetInventoryFiltersHandler>.Instance);

        var result = await handler.HandleAsync(new GetInventoryFiltersQuery(), default);

        result.Owners.Should().ContainSingle().Which.UserId.Should().Be(ownerId);
    }

    [Fact]
    public async Task HandleAsync_EmptyInventory_ReturnsEmptyOwners()
    {
        var filters = new InventoryFiltersDto(["Weapons"], ["Laser"], []);
        var handler = new GetInventoryFiltersHandler(new FakeWarehouseRepo(filters), NullLogger<GetInventoryFiltersHandler>.Instance);

        var result = await handler.HandleAsync(new GetInventoryFiltersQuery(), default);

        result.Owners.Should().BeEmpty();
    }
}
