using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NajaEcho.Application.Abstractions;
using NajaEcho.Application.Features.Warehouse.ShipComponents.GetShipComponentFilters;
using NajaEcho.Application.Features.Warehouse.ShipComponents.GetShipComponents;
using NajaEcho.Application.Features.Warehouse.ShipComponents.SearchSystemsCatalog;
using NajaEcho.Domain.Warehouse;

namespace NajaEcho.Application.Tests.Features.Warehouse.ShipComponents.GetShipComponentFilters;

public sealed class GetShipComponentFiltersQueryHandlerTests
{
    private sealed class FakeRepo : IShipComponentRepository
    {
        private readonly ShipComponentFiltersDto _dto;

        public FakeRepo(ShipComponentFiltersDto dto) => _dto = dto;

        public Task<ShipComponentFiltersDto> GetShipComponentFiltersAsync(CancellationToken ct) => Task.FromResult(_dto);

        public Task<IReadOnlyList<ShipComponentRowDto>> GetShipComponentsAsync(GetShipComponentsQuery q, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<ShipComponentRowDto>>([]);
        public Task<IReadOnlyList<SystemsCatalogItemDto>> SearchSystemsCatalogAsync(string? s, int l, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<SystemsCatalogItemDto>>([]);
        public Task<bool> HasCachedAttributesAsync(Guid id, CancellationToken ct) => Task.FromResult(false);
        public Task SaveItemAttributesAsync(IReadOnlyList<ItemAttribute> attrs, CancellationToken ct) => Task.CompletedTask;
        public Task UpsertShipComponentAttributesAsync(Guid id, DateTimeOffset at, CancellationToken ct) => Task.CompletedTask;
    }

    private static GetShipComponentFiltersQueryHandler MakeHandler(ShipComponentFiltersDto dto)
        => new(new FakeRepo(dto), NullLogger<GetShipComponentFiltersQueryHandler>.Instance);

    [Fact]
    public async Task HandleAsync_PopulatedInventory_ReturnsDistinctOptionLists()
    {
        var dto = new ShipComponentFiltersDto(
            ["Shield", "Gun"],
            ["Military", "Civilian"],
            [1, 2, 3],
            ["A", "B"],
            [new OwnerFilterOption(Guid.NewGuid(), "Alice")],
            ["Bay 1"],
            false, false, false);

        var result = await MakeHandler(dto).HandleAsync(new GetShipComponentFiltersQuery(), default);

        result.Types.Should().BeEquivalentTo(["Shield", "Gun"]);
        result.Classes.Should().BeEquivalentTo(["Military", "Civilian"]);
        result.Sizes.Should().BeEquivalentTo([1, 2, 3]);
        result.Grades.Should().BeEquivalentTo(["A", "B"]);
        result.Owners.Should().HaveCount(1);
        result.Locations.Should().BeEquivalentTo(["Bay 1"]);
    }

    [Fact]
    public async Task HandleAsync_UnknownClassTrue_SetsFlag()
    {
        var dto = new ShipComponentFiltersDto([], [], [], [], [], [], true, false, false);

        var result = await MakeHandler(dto).HandleAsync(new GetShipComponentFiltersQuery(), default);

        result.UnknownClass.Should().BeTrue();
        result.UnknownSize.Should().BeFalse();
        result.UnknownGrade.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_EmptyInventory_AllListsEmptyAndFlagsFalse()
    {
        var dto = new ShipComponentFiltersDto([], [], [], [], [], [], false, false, false);

        var result = await MakeHandler(dto).HandleAsync(new GetShipComponentFiltersQuery(), default);

        result.Types.Should().BeEmpty();
        result.Classes.Should().BeEmpty();
        result.Sizes.Should().BeEmpty();
        result.Grades.Should().BeEmpty();
        result.Owners.Should().BeEmpty();
        result.Locations.Should().BeEmpty();
        result.UnknownClass.Should().BeFalse();
        result.UnknownSize.Should().BeFalse();
        result.UnknownGrade.Should().BeFalse();
    }
}
