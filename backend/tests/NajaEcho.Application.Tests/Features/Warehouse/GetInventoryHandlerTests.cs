using FluentAssertions;
using NajaEcho.Application.Abstractions;
using NajaEcho.Application.Features.Warehouse.GetInventory;
using NajaEcho.Application.Features.Warehouse.GetInventoryFilters;
using NajaEcho.Application.Features.Warehouse.SearchCatalogItems;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace NajaEcho.Application.Tests.Features.Warehouse;

public sealed class GetInventoryHandlerTests
{
    private static readonly Guid RowId1 = Guid.NewGuid();
    private static readonly Guid RowId2 = Guid.NewGuid();
    private static readonly Guid ItemId1 = Guid.NewGuid();
    private static readonly Guid ItemId2 = Guid.NewGuid();
    private static readonly Guid OwnerId1 = Guid.NewGuid();
    private static readonly Guid OwnerId2 = Guid.NewGuid();

    private sealed class FakeWarehouseRepo : IWarehouseInventoryRepository
    {
        private readonly List<InventoryRowDto> _rows;

        public FakeWarehouseRepo(List<InventoryRowDto> rows) => _rows = rows;

        public Task<IReadOnlyList<InventoryRowDto>> GetInventoryAsync(
            string? name, string? type, string? subtype, Guid? ownerUserId, string? location, CancellationToken ct)
        {
            IEnumerable<InventoryRowDto> q = _rows;

            if (!string.IsNullOrEmpty(name))
                q = q.Where(r => r.Name.Contains(name, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(type))
                q = q.Where(r => r.Type == type);
            if (!string.IsNullOrEmpty(subtype))
                q = q.Where(r => r.Subtype == subtype);
            if (ownerUserId.HasValue)
                q = q.Where(r => r.OwnerUserId == ownerUserId.Value);
            if (!string.IsNullOrEmpty(location))
                q = q.Where(r => r.Location.Contains(location, StringComparison.OrdinalIgnoreCase));

            IReadOnlyList<InventoryRowDto> result = q.OrderBy(r => r.Name).ToList();
            return Task.FromResult(result);
        }

        public Task<InventoryFiltersDto> GetInventoryFiltersAsync(CancellationToken ct) =>
            Task.FromResult(new InventoryFiltersDto([], [], []));

        public Task<IReadOnlyList<CatalogItemResultDto>> SearchCatalogItemsAsync(string? search, int limit, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<CatalogItemResultDto>>([]);

        public Task<(InventoryRowDto Row, bool IsNew)> AddOrIncrementAsync(Guid itemId, Guid ownerUserId, string location, int quantity, int quality, CancellationToken ct) =>
            throw new NotImplementedException();

        public Task<InventoryRowDto> UpdateQuantityAsync(Guid id, int quantity, CancellationToken ct) =>
            throw new NotImplementedException();

        public Task RemoveAsync(Guid id, CancellationToken ct) => throw new NotImplementedException();
    }

    private static GetInventoryHandler MakeHandler(List<InventoryRowDto> rows) =>
        new(new FakeWarehouseRepo(rows), NullLogger<GetInventoryHandler>.Instance);

    private static InventoryRowDto MakeRow(Guid id, Guid itemId, string name, string? type, string? subtype,
        Guid ownerId, string ownerName, string location, int quantity = 1) =>
        new(id, itemId, name, type, subtype, quantity, 500, ownerId, ownerName, location);

    [Fact]
    public async Task HandleAsync_NoFilters_ReturnsAllRows()
    {
        var rows = new List<InventoryRowDto>
        {
            MakeRow(RowId1, ItemId1, "Zeta Widget", "Gear", "Tools", OwnerId1, "Alice", "Bay 1"),
            MakeRow(RowId2, ItemId2, "Alpha Gadget", "Gear", "Tools", OwnerId1, "Alice", "Bay 2"),
        };
        var handler = MakeHandler(rows);

        var result = await handler.HandleAsync(new GetInventoryQuery(null, null, null, null, null), default);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task HandleAsync_NoFilters_SortsByNameAscending()
    {
        var rows = new List<InventoryRowDto>
        {
            MakeRow(RowId1, ItemId1, "Zeta Widget", null, null, OwnerId1, "Alice", "Bay 1"),
            MakeRow(RowId2, ItemId2, "Alpha Gadget", null, null, OwnerId1, "Alice", "Bay 2"),
        };
        var handler = MakeHandler(rows);

        var result = await handler.HandleAsync(new GetInventoryQuery(null, null, null, null, null), default);

        result[0].Name.Should().Be("Alpha Gadget");
        result[1].Name.Should().Be("Zeta Widget");
    }

    [Fact]
    public async Task HandleAsync_NameFilter_PartialCaseInsensitiveMatch()
    {
        var rows = new List<InventoryRowDto>
        {
            MakeRow(RowId1, ItemId1, "Gladius Blade", null, null, OwnerId1, "Alice", "Bay 1"),
            MakeRow(RowId2, ItemId2, "Hornet Shield", null, null, OwnerId1, "Alice", "Bay 2"),
        };
        var handler = MakeHandler(rows);

        var result = await handler.HandleAsync(new GetInventoryQuery("glad", null, null, null, null), default);

        result.Should().ContainSingle().Which.Name.Should().Be("Gladius Blade");
    }

    [Fact]
    public async Task HandleAsync_TypeFilter_ExactMatch()
    {
        var rows = new List<InventoryRowDto>
        {
            MakeRow(RowId1, ItemId1, "Item A", "Weapons", "Laser", OwnerId1, "Alice", "Bay 1"),
            MakeRow(RowId2, ItemId2, "Item B", "Armor", "Shield", OwnerId1, "Alice", "Bay 2"),
        };
        var handler = MakeHandler(rows);

        var result = await handler.HandleAsync(new GetInventoryQuery(null, "Weapons", null, null, null), default);

        result.Should().ContainSingle().Which.Type.Should().Be("Weapons");
    }

    [Fact]
    public async Task HandleAsync_SubtypeFilter_ExactMatch()
    {
        var rows = new List<InventoryRowDto>
        {
            MakeRow(RowId1, ItemId1, "Item A", "Weapons", "Laser", OwnerId1, "Alice", "Bay 1"),
            MakeRow(RowId2, ItemId2, "Item B", "Weapons", "Ballistic", OwnerId1, "Alice", "Bay 2"),
        };
        var handler = MakeHandler(rows);

        var result = await handler.HandleAsync(new GetInventoryQuery(null, null, "Laser", null, null), default);

        result.Should().ContainSingle().Which.Subtype.Should().Be("Laser");
    }

    [Fact]
    public async Task HandleAsync_OwnerFilter_ExactMatch()
    {
        var rows = new List<InventoryRowDto>
        {
            MakeRow(RowId1, ItemId1, "Item A", null, null, OwnerId1, "Alice", "Bay 1"),
            MakeRow(RowId2, ItemId2, "Item B", null, null, OwnerId2, "Bob", "Bay 2"),
        };
        var handler = MakeHandler(rows);

        var result = await handler.HandleAsync(new GetInventoryQuery(null, null, null, OwnerId1, null), default);

        result.Should().ContainSingle().Which.OwnerUserId.Should().Be(OwnerId1);
    }

    [Fact]
    public async Task HandleAsync_LocationFilter_PartialCaseInsensitiveMatch()
    {
        var rows = new List<InventoryRowDto>
        {
            MakeRow(RowId1, ItemId1, "Item A", null, null, OwnerId1, "Alice", "Bay 1 Alpha"),
            MakeRow(RowId2, ItemId2, "Item B", null, null, OwnerId1, "Alice", "Dock 3"),
        };
        var handler = MakeHandler(rows);

        var result = await handler.HandleAsync(new GetInventoryQuery(null, null, null, null, "bay"), default);

        result.Should().ContainSingle().Which.Location.Should().Be("Bay 1 Alpha");
    }

    [Fact]
    public async Task HandleAsync_MultipleFilters_AndLogic()
    {
        var rows = new List<InventoryRowDto>
        {
            MakeRow(RowId1, ItemId1, "Laser Mk1", "Weapons", "Laser", OwnerId1, "Alice", "Bay 1"),
            MakeRow(RowId2, ItemId2, "Laser Mk2", "Weapons", "Laser", OwnerId2, "Bob", "Bay 2"),
            MakeRow(Guid.NewGuid(), ItemId1, "Ballistic", "Weapons", "Ballistic", OwnerId1, "Alice", "Bay 1"),
        };
        var handler = MakeHandler(rows);

        var result = await handler.HandleAsync(new GetInventoryQuery(null, "Weapons", "Laser", OwnerId1, null), default);

        result.Should().ContainSingle().Which.Name.Should().Be("Laser Mk1");
    }

    [Fact]
    public async Task HandleAsync_EmptyInventory_ReturnsEmptyList()
    {
        var handler = MakeHandler([]);

        var result = await handler.HandleAsync(new GetInventoryQuery(null, null, null, null, null), default);

        result.Should().BeEmpty();
    }
}
