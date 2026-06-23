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
    private static readonly Guid KnownLocationId = Guid.NewGuid();
    private const string KnownLocationType = "Station";

    private sealed class FakeMaterialRepo : IMaterialInventoryRepository
    {
        public bool RowExists { get; set; } = true;
        public Guid? LastUpdatedLocationId { get; private set; }
        public string? LastUpdatedLocationType { get; private set; }
        public Guid? LastUpdatedRowId { get; private set; }

        public Task<(MaterialRowDto Row, bool IsNew)> AddOrIncrementAsync(
            Guid commodityId, Guid ownerUserId, string location, decimal quantity, int quality, Guid? locationId, string? locationType, CancellationToken ct)
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

        public Task<MaterialRowDto> UpdateMaterialAsync(Guid id, Guid ownerUserId, Guid locationId, string locationType, decimal quantity, CancellationToken ct)
            => throw new NotImplementedException();

        public Task UpdateLocationAsync(Guid id, Guid locationId, string locationType, CancellationToken ct)
        {
            LastUpdatedRowId = id;
            LastUpdatedLocationId = locationId;
            LastUpdatedLocationType = locationType;
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(Guid id, CancellationToken ct)
            => Task.FromResult(RowExists);

        public Task RemoveAsync(Guid id, CancellationToken ct)
            => throw new NotImplementedException();
    }

    private static TransferMaterialHandler CreateHandler(FakeMaterialRepo? repo = null) =>
        new(repo ?? new FakeMaterialRepo(), NullLogger<TransferMaterialHandler>.Instance);

    [Fact]
    public async Task Transfer_WithValidRow_UpdatesLocation()
    {
        var repo = new FakeMaterialRepo { RowExists = true };

        await CreateHandler(repo).HandleAsync(new TransferMaterialCommand(KnownRowId, KnownLocationId, KnownLocationType), default);

        repo.LastUpdatedRowId.Should().Be(KnownRowId);
        repo.LastUpdatedLocationId.Should().Be(KnownLocationId);
        repo.LastUpdatedLocationType.Should().Be(KnownLocationType);
    }

    [Fact]
    public async Task Transfer_WithUnknownRow_ThrowsMaterialRowNotFoundException()
    {
        var repo = new FakeMaterialRepo { RowExists = false };

        var act = () => CreateHandler(repo).HandleAsync(new TransferMaterialCommand(KnownRowId, KnownLocationId, KnownLocationType), default);

        await act.Should().ThrowAsync<MaterialRowNotFoundException>();
    }

    [Fact]
    public async Task Transfer_WithCityLocationType_UpdatesLocation()
    {
        var repo = new FakeMaterialRepo { RowExists = true };

        await CreateHandler(repo).HandleAsync(new TransferMaterialCommand(KnownRowId, KnownLocationId, "City"), default);

        repo.LastUpdatedLocationType.Should().Be("City");
        repo.LastUpdatedRowId.Should().Be(KnownRowId);
    }
}
