using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NajaEcho.Application.Abstractions;
using NajaEcho.Application.Features.Warehouse.Materials.GetMaterialFilters;
using NajaEcho.Application.Features.Warehouse.Materials.GetMaterials;
using NajaEcho.Application.Features.Warehouse.Materials.SearchCommodities;
using Xunit;

namespace NajaEcho.Application.Tests.Features.Warehouse.Materials.GetMaterials;

public sealed class GetMaterialsQueryHandlerTests
{
    private static readonly Guid RowId1 = Guid.NewGuid();
    private static readonly Guid CommodityId1 = Guid.NewGuid();
    private static readonly Guid OwnerId1 = Guid.NewGuid();

    private sealed class FakeMaterialRepo : IMaterialInventoryRepository
    {
        public string? CapturedMaterial;
        public Guid? CapturedOwnerUserId;
        public string? CapturedLocation;
        public int? CapturedQualityMin;
        public int? CapturedQualityMax;
        private readonly List<MaterialRowDto> _rows;

        public FakeMaterialRepo(List<MaterialRowDto> rows) => _rows = rows;

        public Task<IReadOnlyList<MaterialRowDto>> GetMaterialsAsync(
            string? material, Guid? ownerUserId, string? location, int? qualityMin, int? qualityMax, CancellationToken ct)
        {
            CapturedMaterial = material;
            CapturedOwnerUserId = ownerUserId;
            CapturedLocation = location;
            CapturedQualityMin = qualityMin;
            CapturedQualityMax = qualityMax;
            return Task.FromResult<IReadOnlyList<MaterialRowDto>>(_rows);
        }

        public Task<MaterialFiltersDto> GetMaterialFiltersAsync(CancellationToken ct) =>
            Task.FromResult(new MaterialFiltersDto([], []));

        public Task<IReadOnlyList<CommodityResultDto>> SearchCommoditiesAsync(string? search, int limit, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<CommodityResultDto>>([]);

        public Task<(MaterialRowDto Row, bool IsNew)> AddOrIncrementAsync(
            Guid commodityId, Guid ownerUserId, string location, decimal quantity, int quality, Guid? stationId, CancellationToken ct) =>
            throw new NotImplementedException();

        public Task<MaterialRowDto> UpdateQuantityAsync(Guid id, decimal quantity, CancellationToken ct) =>
            throw new NotImplementedException();

        public Task RemoveAsync(Guid id, CancellationToken ct) => throw new NotImplementedException();
    }

    private static GetMaterialsQueryHandler MakeHandler(List<MaterialRowDto> rows, out FakeMaterialRepo repo)
    {
        repo = new FakeMaterialRepo(rows);
        return new GetMaterialsQueryHandler(repo, NullLogger<GetMaterialsQueryHandler>.Instance);
    }

    private static MaterialRowDto MakeRow(Guid id, Guid commodityId, string name, string? code,
        decimal quantity, int quality, Guid ownerId, string ownerName, string location) =>
        new(id, commodityId, name, code, quantity, quality, ownerId, ownerName, location);

    [Fact]
    public async Task HandleAsync_NoFilters_DelegatesToRepositoryAndReturnsRows()
    {
        var rows = new List<MaterialRowDto>
        {
            MakeRow(RowId1, CommodityId1, "Titanium", "TTAM", 12.50m, 600, OwnerId1, "Alice", "Bay 1"),
        };
        var handler = MakeHandler(rows, out var repo);

        var result = await handler.HandleAsync(new GetMaterialsQuery(null, null, null, null, null), default);

        result.Should().HaveCount(1);
        result[0].Quantity.Should().Be(12.50m);
        result[0].Quality.Should().Be(600);
        repo.CapturedMaterial.Should().BeNull();
        repo.CapturedOwnerUserId.Should().BeNull();
        repo.CapturedLocation.Should().BeNull();
        repo.CapturedQualityMin.Should().BeNull();
        repo.CapturedQualityMax.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_EmptyInventory_ReturnsEmptyList()
    {
        var handler = MakeHandler([], out _);

        var result = await handler.HandleAsync(new GetMaterialsQuery(null, null, null, null, null), default);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_WithFilters_PassesFilterValuesThroughToRepository()
    {
        var handler = MakeHandler([], out var repo);

        await handler.HandleAsync(
            new GetMaterialsQuery("Titanium", OwnerId1, "Bay 1", 100, 900), default);

        repo.CapturedMaterial.Should().Be("Titanium");
        repo.CapturedOwnerUserId.Should().Be(OwnerId1);
        repo.CapturedLocation.Should().Be("Bay 1");
        repo.CapturedQualityMin.Should().Be(100);
        repo.CapturedQualityMax.Should().Be(900);
    }
}
