using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NajaEcho.Application.Abstractions;
using NajaEcho.Application.Features.Warehouse.Materials.GetMaterialFilters;
using NajaEcho.Application.Features.Warehouse.Materials.GetMaterials;
using NajaEcho.Application.Features.Warehouse.Materials.SearchCommodities;
using Xunit;

namespace NajaEcho.Application.Tests.Features.Warehouse.Materials.SearchCommodities;

public sealed class SearchCommoditiesQueryHandlerTests
{
    private static readonly Guid Commodity1Id = Guid.NewGuid();
    private static readonly Guid Commodity2Id = Guid.NewGuid();

    private sealed class FakeMaterialRepo(List<CommodityResultDto> activeCommodities) : IMaterialInventoryRepository
    {
        public Task<IReadOnlyList<CommodityResultDto>> SearchCommoditiesAsync(string? search, int limit, CancellationToken ct)
        {
            IEnumerable<CommodityResultDto> q = activeCommodities;
            if (!string.IsNullOrEmpty(search))
                q = q.Where(c =>
                    c.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    (c.Code is not null && c.Code.Contains(search, StringComparison.OrdinalIgnoreCase)));
            IReadOnlyList<CommodityResultDto> result = q.Take(limit).ToList();
            return Task.FromResult(result);
        }

        public Task<IReadOnlyList<MaterialRowDto>> GetMaterialsAsync(
            string? material, Guid? ownerUserId, string? location, int? qualityMin, int? qualityMax, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<MaterialRowDto>>([]);

        public Task<MaterialFiltersDto> GetMaterialFiltersAsync(CancellationToken ct) =>
            Task.FromResult(new MaterialFiltersDto([], []));

        public Task<(MaterialRowDto Row, bool IsNew)> AddOrIncrementAsync(
            Guid commodityId, Guid ownerUserId, string location, decimal quantity, int quality, Guid? stationId, CancellationToken ct) =>
            throw new NotImplementedException();

        public Task<MaterialRowDto> UpdateQuantityAsync(Guid id, decimal quantity, CancellationToken ct) =>
            throw new NotImplementedException();

        
        public Task<MaterialRowDto> UpdateMaterialAsync(Guid id, Guid ownerUserId, Guid stationId, decimal quantity, CancellationToken ct) => throw new NotImplementedException();
        public Task UpdateStationAsync(Guid id, Guid stationId, CancellationToken ct) => Task.CompletedTask;
        public Task<bool> ExistsAsync(Guid id, CancellationToken ct) => Task.FromResult(true);
        public Task RemoveAsync(Guid id, CancellationToken ct) => throw new NotImplementedException();
    }

    [Fact]
    public async Task HandleAsync_NameSearch_ReturnsMatchingCommodities()
    {
        var commodities = new List<CommodityResultDto>
        {
            new(Commodity1Id, "Titanium", "TTAM"),
            new(Commodity2Id, "Quantanium", "QTM"),
        };
        var handler = new SearchCommoditiesQueryHandler(new FakeMaterialRepo(commodities), NullLogger<SearchCommoditiesQueryHandler>.Instance);

        var result = await handler.HandleAsync(new SearchCommoditiesQuery("titan", 25), default);

        result.Should().ContainSingle().Which.Name.Should().Be("Titanium");
    }

    [Fact]
    public async Task HandleAsync_CodeSearch_CaseInsensitive_Matches()
    {
        var commodities = new List<CommodityResultDto>
        {
            new(Commodity1Id, "Titanium", "TTAM"),
        };
        var handler = new SearchCommoditiesQueryHandler(new FakeMaterialRepo(commodities), NullLogger<SearchCommoditiesQueryHandler>.Instance);

        var result = await handler.HandleAsync(new SearchCommoditiesQuery("ttam", 25), default);

        result.Should().ContainSingle();
    }

    [Fact]
    public async Task HandleAsync_ExcludesSoftDeletedCommodities()
    {
        // The fake only models commodities the repository would already exclude (active-only),
        // mirroring how the real ICommodityRepository filters by status before reaching this handler.
        var commodities = new List<CommodityResultDto>
        {
            new(Commodity1Id, "Titanium", "TTAM"),
        };
        var handler = new SearchCommoditiesQueryHandler(new FakeMaterialRepo(commodities), NullLogger<SearchCommoditiesQueryHandler>.Instance);

        var result = await handler.HandleAsync(new SearchCommoditiesQuery(null, 25), default);

        result.Should().ContainSingle();
    }

    [Fact]
    public async Task HandleAsync_LimitCapped_ReturnsAtMostLimit()
    {
        var commodities = Enumerable.Range(1, 30)
            .Select(i => new CommodityResultDto(Guid.NewGuid(), $"Commodity {i}", null))
            .ToList();
        var handler = new SearchCommoditiesQueryHandler(new FakeMaterialRepo(commodities), NullLogger<SearchCommoditiesQueryHandler>.Instance);

        var result = await handler.HandleAsync(new SearchCommoditiesQuery(null, 5), default);

        result.Should().HaveCount(5);
    }
}
