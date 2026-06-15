using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NajaEcho.Application.Abstractions;
using NajaEcho.Application.Features.Warehouse.ShipComponents.GetShipComponentFilters;
using NajaEcho.Application.Features.Warehouse.ShipComponents.GetShipComponents;
using NajaEcho.Application.Features.Warehouse.ShipComponents.SearchSystemsCatalog;
using NajaEcho.Domain.Warehouse;

namespace NajaEcho.Application.Tests.Features.Warehouse.ShipComponents.GetShipComponents;

public sealed class GetShipComponentsQueryHandlerTests
{
    private static readonly Guid ItemId1 = Guid.NewGuid();
    private static readonly Guid ItemId2 = Guid.NewGuid();
    private static readonly Guid OwnerId = Guid.NewGuid();
    private static readonly Guid RowId1 = Guid.NewGuid();
    private static readonly Guid RowId2 = Guid.NewGuid();

    private sealed class FakeShipComponentRepo : IShipComponentRepository
    {
        private readonly IReadOnlyList<ShipComponentRowDto> _rows;

        public FakeShipComponentRepo(IReadOnlyList<ShipComponentRowDto>? rows = null)
            => _rows = rows ?? [];

        public Task<IReadOnlyList<ShipComponentRowDto>> GetShipComponentsAsync(
            GetShipComponentsQuery query, CancellationToken ct)
            => Task.FromResult(_rows);

        public Task<ShipComponentFiltersDto> GetShipComponentFiltersAsync(CancellationToken ct)
            => Task.FromResult(new ShipComponentFiltersDto([], [], [], [], [], [], false, false, false));

        public Task<IReadOnlyList<SystemsCatalogItemDto>> SearchSystemsCatalogAsync(string? search, int limit, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<SystemsCatalogItemDto>>([]);

        public Task<bool> HasCachedAttributesAsync(Guid itemId, CancellationToken ct) => Task.FromResult(false);
        public Task SaveItemAttributesAsync(IReadOnlyList<ItemAttribute> attributes, CancellationToken ct) => Task.CompletedTask;
        public Task UpsertShipComponentAttributesAsync(Guid itemId, DateTimeOffset fetchedAt, CancellationToken ct) => Task.CompletedTask;
    }

    private static GetShipComponentsQueryHandler MakeHandler(IReadOnlyList<ShipComponentRowDto>? rows = null)
        => new(new FakeShipComponentRepo(rows), NullLogger<GetShipComponentsQueryHandler>.Instance);

    [Fact]
    public async Task HandleAsync_ReturnsRowsFromRepository()
    {
        var rows = new List<ShipComponentRowDto>
        {
            new(RowId1, ItemId1, "Alpha Shield", "Shield", "A", 1, "A", 5, OwnerId, "Alice", "Bay 1"),
            new(RowId2, ItemId2, "Beta Shield", "Shield", null, null, null, 2, OwnerId, "Alice", "Bay 2"),
        };
        var handler = MakeHandler(rows);

        var result = await handler.HandleAsync(
            new GetShipComponentsQuery(null, null, null, null, null, null, null, false, false, false), default);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task HandleAsync_NullClassInRow_ReturnedAsNullInDto()
    {
        var rows = new List<ShipComponentRowDto>
        {
            new(RowId1, ItemId1, "Gamma Gun", "Gun", null, null, null, 3, OwnerId, "Bob", "Dock 1"),
        };
        var handler = MakeHandler(rows);

        var result = await handler.HandleAsync(
            new GetShipComponentsQuery(null, null, null, null, null, null, null, false, false, false), default);

        var row = result.Single();
        row.Class.Should().BeNull();
        row.Size.Should().BeNull();
        row.Grade.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_PopulatedAttributes_ReturnedCorrectly()
    {
        var rows = new List<ShipComponentRowDto>
        {
            new(RowId1, ItemId1, "Delta Missile", "Missile", "Military", 2, "A", 10, OwnerId, "Charlie", "Bay 5"),
        };
        var handler = MakeHandler(rows);

        var result = await handler.HandleAsync(
            new GetShipComponentsQuery(null, null, null, null, null, null, null, false, false, false), default);

        var row = result.Single();
        row.Class.Should().Be("Military");
        row.Size.Should().Be(2);
        row.Grade.Should().Be("A");
    }

    [Fact]
    public async Task HandleAsync_EmptyRepository_ReturnsEmptyList()
    {
        var handler = MakeHandler([]);

        var result = await handler.HandleAsync(
            new GetShipComponentsQuery(null, null, null, null, null, null, null, false, false, false), default);

        result.Should().BeEmpty();
    }
}
