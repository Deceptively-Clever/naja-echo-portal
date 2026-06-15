using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NajaEcho.Application.Abstractions;
using NajaEcho.Application.Features.Warehouse.ShipComponents.GetShipComponentFilters;
using NajaEcho.Application.Features.Warehouse.ShipComponents.GetShipComponents;
using NajaEcho.Application.Features.Warehouse.ShipComponents.SearchSystemsCatalog;
using NajaEcho.Domain.Warehouse;

namespace NajaEcho.Application.Tests.Features.Warehouse.ShipComponents.SearchSystemsCatalog;

public sealed class SearchSystemsCatalogQueryHandlerTests
{
    private sealed class FakeRepo : IShipComponentRepository
    {
        private readonly IReadOnlyList<SystemsCatalogItemDto> _items;
        public FakeRepo(IReadOnlyList<SystemsCatalogItemDto>? items = null) => _items = items ?? [];

        public Task<IReadOnlyList<SystemsCatalogItemDto>> SearchSystemsCatalogAsync(string? search, int limit, CancellationToken ct) =>
            Task.FromResult(_items.Take(limit).ToList() as IReadOnlyList<SystemsCatalogItemDto>);

        public Task<IReadOnlyList<ShipComponentRowDto>> GetShipComponentsAsync(GetShipComponentsQuery q, CancellationToken ct) => Task.FromResult<IReadOnlyList<ShipComponentRowDto>>([]);
        public Task<ShipComponentFiltersDto> GetShipComponentFiltersAsync(CancellationToken ct) => Task.FromResult(new ShipComponentFiltersDto([], [], [], [], [], [], false, false, false));
        public Task<bool> HasCachedAttributesAsync(Guid id, CancellationToken ct) => Task.FromResult(false);
        public Task SaveItemAttributesAsync(IReadOnlyList<ItemAttribute> attrs, CancellationToken ct) => Task.CompletedTask;
        public Task UpsertShipComponentAttributesAsync(Guid id, DateTimeOffset at, CancellationToken ct) => Task.CompletedTask;
    }

    private static SearchSystemsCatalogQueryHandler MakeHandler(IReadOnlyList<SystemsCatalogItemDto>? items = null)
        => new(new FakeRepo(items), NullLogger<SearchSystemsCatalogQueryHandler>.Instance);

    [Fact]
    public async Task HandleAsync_ReturnsOnlySystemsResults()
    {
        var items = new List<SystemsCatalogItemDto>
        {
            new(Guid.NewGuid(), "Shield Mk1", "Shield"),
            new(Guid.NewGuid(), "Radar Pro", "Radar"),
        };
        var handler = MakeHandler(items);

        var result = await handler.HandleAsync(new SearchSystemsCatalogQuery(null, 25), default);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task HandleAsync_LimitRespected()
    {
        var items = Enumerable.Range(0, 10)
            .Select(i => new SystemsCatalogItemDto(Guid.NewGuid(), $"Item {i}", "Shield"))
            .ToList();
        var handler = MakeHandler(items);

        var result = await handler.HandleAsync(new SearchSystemsCatalogQuery(null, 3), default);

        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task HandleAsync_EmptySearch_ReturnsAllItems()
    {
        var items = new List<SystemsCatalogItemDto>
        {
            new(Guid.NewGuid(), "Alpha", "Shield"),
            new(Guid.NewGuid(), "Beta", "Gun"),
        };
        var handler = MakeHandler(items);

        var result = await handler.HandleAsync(new SearchSystemsCatalogQuery(null, 25), default);

        result.Should().HaveCount(2);
    }
}
