using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NajaEcho.Application.Abstractions;
using NajaEcho.Application.Features.Warehouse.Materials.ChangeMaterialQuantity;
using NajaEcho.Application.Features.Warehouse.Materials.GetMaterialFilters;
using NajaEcho.Application.Features.Warehouse.Materials.GetMaterials;
using NajaEcho.Application.Features.Warehouse.Materials.SearchCommodities;
using NajaEcho.Application.Features.Warehouse.Materials.TransferMaterial;
using Xunit;

namespace NajaEcho.Application.Tests.Features.Warehouse;

public sealed class TransferMaterialHandlerTests
{
    private static readonly Guid KnownRowId = Guid.NewGuid();
    private static readonly Guid KnownStationId = Guid.NewGuid();

    private sealed class FakeMaterialRepo : IMaterialInventoryRepository
    {
        public bool RowExists { get; set; } = true;
        public Guid? LastUpdatedStationId { get; private set; }
        public Guid? LastUpdatedRowId { get; private set; }

        public Task<(MaterialRowDto Row, bool IsNew)> AddOrIncrementAsync(
            Guid commodityId, Guid ownerUserId, string location, decimal quantity, int quality, Guid? stationId, CancellationToken ct)
            => throw new NotImplementedException();

        public Task<IReadOnlyList<MaterialRowDto>> GetMaterialsAsync(
            string? material, Guid? ownerUserId, string? location, int? qualityMin, int? qualityMax, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<MaterialRowDto>>([]);

        public Task<MaterialFiltersDto> GetMaterialFiltersAsync(CancellationToken ct)
            => Task.FromResult(new MaterialFiltersDto([], []));

        public Task<IReadOnlyList<CommodityResultDto>> SearchCommoditiesAsync(
            string? search, int limit, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<CommodityResultDto>>([]);

        public Task<MaterialRowDto> UpdateQuantityAsync(Guid id, decimal quantity, CancellationToken ct)
            => throw new NotImplementedException();

        public Task<MaterialRowDto> UpdateMaterialAsync(Guid id, Guid ownerUserId, Guid stationId, decimal quantity, CancellationToken ct)
            => throw new NotImplementedException();

        public Task UpdateStationAsync(Guid id, Guid stationId, CancellationToken ct)
        {
            LastUpdatedRowId = id;
            LastUpdatedStationId = stationId;
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(Guid id, CancellationToken ct)
            => Task.FromResult(RowExists);

        public Task RemoveAsync(Guid id, CancellationToken ct)
            => throw new NotImplementedException();
    }

    private sealed class FakeStationRepo : ISpaceStationRepository
    {
        public bool StationExists { get; set; } = true;

        public Task<(int, int, int, int, int)> BulkUpsertAsync(
            IReadOnlyList<JsonDocument> records, IReadOnlyDictionary<int, Guid> starSystemMap, CancellationToken ct)
            => Task.FromResult((0, 0, 0, 0, 0));

        public Task<IReadOnlyList<StationDto>> SearchActiveStationsAsync(string? search, int limit, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<StationDto>>([]);

        public Task<bool> ExistsAsync(Guid id, CancellationToken ct)
            => Task.FromResult(StationExists);
    }

    private static TransferMaterialHandler CreateHandler(
        FakeMaterialRepo? repo = null,
        FakeStationRepo? stationRepo = null) =>
        new(repo ?? new FakeMaterialRepo(),
            stationRepo ?? new FakeStationRepo(),
            NullLogger<TransferMaterialHandler>.Instance);

    [Fact]
    public async Task Transfer_WithValidRowAndStation_SetsStationId()
    {
        var repo = new FakeMaterialRepo { RowExists = true };

        await CreateHandler(repo).HandleAsync(new TransferMaterialCommand(KnownRowId, KnownStationId), default);

        repo.LastUpdatedRowId.Should().Be(KnownRowId);
        repo.LastUpdatedStationId.Should().Be(KnownStationId);
    }

    [Fact]
    public async Task Transfer_WithUnknownRow_ThrowsMaterialRowNotFoundException()
    {
        var repo = new FakeMaterialRepo { RowExists = false };

        var act = () => CreateHandler(repo).HandleAsync(new TransferMaterialCommand(KnownRowId, KnownStationId), default);

        await act.Should().ThrowAsync<MaterialRowNotFoundException>();
    }

    [Fact]
    public async Task Transfer_WithInvalidStationId_ThrowsInvalidOperationException()
    {
        var stationRepo = new FakeStationRepo { StationExists = false };

        var act = () => CreateHandler(stationRepo: stationRepo)
            .HandleAsync(new TransferMaterialCommand(KnownRowId, KnownStationId), default);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Transfer_DoesNotModifyLocationField()
    {
        var repo = new FakeMaterialRepo();

        await CreateHandler(repo).HandleAsync(new TransferMaterialCommand(KnownRowId, KnownStationId), default);

        repo.LastUpdatedStationId.Should().Be(KnownStationId);
        repo.LastUpdatedRowId.Should().Be(KnownRowId);
    }
}
