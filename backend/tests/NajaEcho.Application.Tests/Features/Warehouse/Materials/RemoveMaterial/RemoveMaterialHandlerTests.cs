using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NajaEcho.Application.Abstractions;
using NajaEcho.Application.Features.Warehouse.Materials.ChangeMaterialQuantity;
using NajaEcho.Application.Features.Warehouse.Materials.GetMaterialFilters;
using NajaEcho.Application.Features.Warehouse.Materials.GetMaterials;
using NajaEcho.Application.Features.Warehouse.Materials.RemoveMaterial;
using NajaEcho.Application.Features.Warehouse.Materials.SearchCommodities;
using Xunit;

namespace NajaEcho.Application.Tests.Features.Warehouse.Materials.RemoveMaterial;

public sealed class RemoveMaterialHandlerTests
{
    private static readonly Guid KnownRowId = Guid.NewGuid();
    private static readonly Guid UnknownRowId = Guid.NewGuid();

    private sealed class FakeMaterialRepo : IMaterialInventoryRepository
    {
        private readonly HashSet<Guid> _rows = [KnownRowId];
        public bool Contains(Guid id) => _rows.Contains(id);

        public Task RemoveAsync(Guid id, CancellationToken ct)
        {
            if (!_rows.Contains(id))
                throw new MaterialRowNotFoundException(id);
            _rows.Remove(id);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<MaterialRowDto>> GetMaterialsAsync(
            string? material, Guid? ownerUserId, string? location, int? qualityMin, int? qualityMax, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<MaterialRowDto>>([]);

        public Task<MaterialFiltersDto> GetMaterialFiltersAsync(CancellationToken ct) =>
            Task.FromResult(new MaterialFiltersDto([], []));

        public Task<IReadOnlyList<CommodityResultDto>> SearchCommoditiesAsync(string? search, int limit, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<CommodityResultDto>>([]);

        public Task<(MaterialRowDto Row, bool IsNew)> AddOrIncrementAsync(
            Guid commodityId, Guid ownerUserId, string location, decimal quantity, int quality, Guid? stationId, CancellationToken ct) =>
            throw new NotImplementedException();

        public Task<MaterialRowDto> UpdateQuantityAsync(Guid id, decimal quantity, CancellationToken ct) =>
            throw new NotImplementedException();
    }

    [Fact]
    public async Task HandleAsync_KnownRow_CallsRemoveAsync()
    {
        var repo = new FakeMaterialRepo();
        var handler = new RemoveMaterialHandler(repo, NullLogger<RemoveMaterialHandler>.Instance);

        await handler.HandleAsync(new RemoveMaterialCommand(KnownRowId), default);

        repo.Contains(KnownRowId).Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_UnknownRow_PropagatesMaterialRowNotFoundException()
    {
        var repo = new FakeMaterialRepo();
        var handler = new RemoveMaterialHandler(repo, NullLogger<RemoveMaterialHandler>.Instance);

        Func<Task> act = () => handler.HandleAsync(new RemoveMaterialCommand(UnknownRowId), default);

        await act.Should().ThrowAsync<MaterialRowNotFoundException>();
    }
}
