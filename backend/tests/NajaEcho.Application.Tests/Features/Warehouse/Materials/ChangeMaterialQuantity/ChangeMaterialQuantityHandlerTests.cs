using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NajaEcho.Application.Abstractions;
using NajaEcho.Application.Features.Warehouse.Materials.ChangeMaterialQuantity;
using NajaEcho.Application.Features.Warehouse.Materials.GetMaterialFilters;
using NajaEcho.Application.Features.Warehouse.Materials.GetMaterials;
using NajaEcho.Application.Features.Warehouse.Materials.SearchCommodities;
using Xunit;

namespace NajaEcho.Application.Tests.Features.Warehouse.Materials.ChangeMaterialQuantity;

public sealed class ChangeMaterialQuantityHandlerTests
{
    private static readonly Guid KnownRowId = Guid.NewGuid();
    private static readonly Guid KnownCommodityId = Guid.NewGuid();
    private static readonly Guid KnownOwnerId = Guid.NewGuid();

    private sealed class FakeMaterialRepo : IMaterialInventoryRepository
    {
        public bool RowExists { get; set; } = true;
        public decimal? CapturedQuantity { get; private set; }

        public Task<MaterialRowDto> UpdateQuantityAsync(Guid id, decimal quantity, CancellationToken ct)
        {
            if (!RowExists || id != KnownRowId)
                throw new MaterialRowNotFoundException(id);
            CapturedQuantity = quantity;
            return Task.FromResult(new MaterialRowDto(id, KnownCommodityId, "Titanium", "TTAM", quantity, 500, KnownOwnerId, "Alice", "Bay 1"));
        }

        public Task<IReadOnlyList<MaterialRowDto>> GetMaterialsAsync(
            string? material, Guid? ownerUserId, string? location, int? qualityMin, int? qualityMax, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<MaterialRowDto>>([]);

        public Task<MaterialFiltersDto> GetMaterialFiltersAsync(CancellationToken ct) =>
            Task.FromResult(new MaterialFiltersDto([], []));

        public Task<IReadOnlyList<CommodityResultDto>> SearchCommoditiesAsync(string? search, int limit, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<CommodityResultDto>>([]);

        public Task<(MaterialRowDto Row, bool IsNew)> AddOrIncrementAsync(
            Guid commodityId, Guid ownerUserId, string location, decimal quantity, int quality, Guid? locationId, string? locationType, CancellationToken ct) =>
            throw new NotImplementedException();


        public Task<MaterialRowDto> UpdateMaterialAsync(Guid id, Guid ownerUserId, Guid locationId, string locationType, decimal quantity, CancellationToken ct) => throw new NotImplementedException();
        public Task UpdateLocationAsync(Guid id, Guid locationId, string locationType, CancellationToken ct) => Task.CompletedTask;
        public Task<bool> ExistsAsync(Guid id, CancellationToken ct) => Task.FromResult(true);
        public Task RemoveAsync(Guid id, CancellationToken ct) => throw new NotImplementedException();
    }

    private static ChangeMaterialQuantityHandler MakeHandler(FakeMaterialRepo? repo = null) =>
        new(repo ?? new FakeMaterialRepo(), NullLogger<ChangeMaterialQuantityHandler>.Instance);

    [Fact]
    public async Task HandleAsync_UnknownRow_ThrowsMaterialRowNotFoundException()
    {
        var repo = new FakeMaterialRepo { RowExists = false };
        var act = () => MakeHandler(repo).HandleAsync(new ChangeMaterialQuantityCommand(KnownRowId, 5m), default);
        await act.Should().ThrowAsync<MaterialRowNotFoundException>();
    }

    [Fact]
    public async Task HandleAsync_QuantityRoundedHalfUp_BeforeValidation()
    {
        var repo = new FakeMaterialRepo();
        await MakeHandler(repo).HandleAsync(new ChangeMaterialQuantityCommand(KnownRowId, 1.0005m), default);
        repo.CapturedQuantity.Should().Be(1.001m);
    }

    [Fact]
    public async Task HandleAsync_QuantityZeroOrLess_ThrowsArgumentOutOfRangeException_WithoutCallingRepository()
    {
        var repo = new FakeMaterialRepo();
        var act = () => MakeHandler(repo).HandleAsync(new ChangeMaterialQuantityCommand(KnownRowId, 0m), default);
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
        repo.CapturedQuantity.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_NegativeQuantity_ThrowsArgumentOutOfRangeException()
    {
        var act = () => MakeHandler().HandleAsync(new ChangeMaterialQuantityCommand(KnownRowId, -5m), default);
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task HandleAsync_ValidRequest_CallsUpdateQuantityAsync_WithRoundedAbsoluteValue()
    {
        var repo = new FakeMaterialRepo();
        var result = await MakeHandler(repo).HandleAsync(new ChangeMaterialQuantityCommand(KnownRowId, 7.5m), default);
        repo.CapturedQuantity.Should().Be(7.50m);
        result.Quantity.Should().Be(7.50m);
    }
}
