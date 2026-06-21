using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NajaEcho.Application.Abstractions;
using NajaEcho.Application.Features.Warehouse.GetInventory;
using NajaEcho.Application.Features.Warehouse.GetInventoryFilters;
using NajaEcho.Application.Features.Warehouse.SearchCatalogItems;
using Xunit;

namespace NajaEcho.Application.Tests.Features.Warehouse;

public sealed class SearchCatalogItemsHandlerTests
{
    private static readonly Guid Item1Id = Guid.NewGuid();
    private static readonly Guid Item2Id = Guid.NewGuid();

    private sealed class FakeWarehouseRepo : IWarehouseInventoryRepository
    {
        private readonly List<CatalogItemResultDto> _items;
        public FakeWarehouseRepo(List<CatalogItemResultDto> items) => _items = items;

        public Task<IReadOnlyList<CatalogItemResultDto>> SearchCatalogItemsAsync(string? search, int limit, CancellationToken ct)
        {
            IEnumerable<CatalogItemResultDto> q = _items;
            if (!string.IsNullOrEmpty(search))
                q = q.Where(i => i.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
            IReadOnlyList<CatalogItemResultDto> result = q.Take(limit).ToList();
            return Task.FromResult(result);
        }

        public Task<IReadOnlyList<InventoryRowDto>> GetInventoryAsync(string? name, string? type, string? subtype, Guid? ownerUserId, string? location, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<InventoryRowDto>>([]);
        public Task<InventoryFiltersDto> GetInventoryFiltersAsync(CancellationToken ct) =>
            Task.FromResult(new InventoryFiltersDto([], [], []));
        public Task<(InventoryRowDto Row, bool IsNew)> AddOrIncrementAsync(Guid itemId, Guid ownerUserId, string location, int quantity, int quality, Guid? stationId, CancellationToken ct) =>
            throw new NotImplementedException();
        public Task<InventoryRowDto> UpdateQuantityAsync(Guid id, int quantity, CancellationToken ct) =>
            throw new NotImplementedException();

        public Task<InventoryRowDto> UpdateItemAsync(Guid id, Guid ownerUserId, Guid stationId, int quantity, CancellationToken ct) =>
            throw new NotImplementedException();
        public Task UpdateStationAsync(Guid id, Guid stationId, CancellationToken ct) => Task.CompletedTask;
        public Task<bool> ExistsAsync(Guid id, CancellationToken ct) => Task.FromResult(true);
        public Task RemoveAsync(Guid id, CancellationToken ct) => throw new NotImplementedException();
    }

    [Fact]
    public async Task HandleAsync_NameSearch_ReturnsMatchingItems()
    {
        var items = new List<CatalogItemResultDto>
        {
            new(Item1Id, "Laser Mk1", "Weapons", "Laser"),
            new(Item2Id, "Ballistic Pistol", "Weapons", "Ballistic"),
        };
        var handler = new SearchCatalogItemsHandler(new FakeWarehouseRepo(items), NullLogger<SearchCatalogItemsHandler>.Instance);

        var result = await handler.HandleAsync(new SearchCatalogItemsQuery("laser", 25), default);

        result.Should().ContainSingle().Which.Name.Should().Be("Laser Mk1");
    }

    [Fact]
    public async Task HandleAsync_CaseInsensitiveSearch_Matches()
    {
        var items = new List<CatalogItemResultDto>
        {
            new(Item1Id, "Laser Mk1", "Weapons", "Laser"),
        };
        var handler = new SearchCatalogItemsHandler(new FakeWarehouseRepo(items), NullLogger<SearchCatalogItemsHandler>.Instance);

        var result = await handler.HandleAsync(new SearchCatalogItemsQuery("LASER", 25), default);

        result.Should().ContainSingle();
    }

    [Fact]
    public async Task HandleAsync_NullSearch_ReturnsAll()
    {
        var items = new List<CatalogItemResultDto>
        {
            new(Item1Id, "Laser Mk1", "Weapons", "Laser"),
            new(Item2Id, "Ballistic Pistol", "Weapons", "Ballistic"),
        };
        var handler = new SearchCatalogItemsHandler(new FakeWarehouseRepo(items), NullLogger<SearchCatalogItemsHandler>.Instance);

        var result = await handler.HandleAsync(new SearchCatalogItemsQuery(null, 25), default);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task HandleAsync_LimitCapped_ReturnsAtMostLimit()
    {
        var items = Enumerable.Range(1, 30)
            .Select(i => new CatalogItemResultDto(Guid.NewGuid(), $"Item {i}", null, null))
            .ToList();
        var handler = new SearchCatalogItemsHandler(new FakeWarehouseRepo(items), NullLogger<SearchCatalogItemsHandler>.Instance);

        var result = await handler.HandleAsync(new SearchCatalogItemsQuery(null, 5), default);

        result.Should().HaveCount(5);
    }

    [Fact]
    public async Task HandleAsync_ReturnsTypeAndSubtype()
    {
        var items = new List<CatalogItemResultDto>
        {
            new(Item1Id, "Laser Mk1", "Weapons", "Laser"),
        };
        var handler = new SearchCatalogItemsHandler(new FakeWarehouseRepo(items), NullLogger<SearchCatalogItemsHandler>.Instance);

        var result = await handler.HandleAsync(new SearchCatalogItemsQuery(null, 25), default);

        result.Single().Type.Should().Be("Weapons");
        result.Single().Subtype.Should().Be("Laser");
    }
}
