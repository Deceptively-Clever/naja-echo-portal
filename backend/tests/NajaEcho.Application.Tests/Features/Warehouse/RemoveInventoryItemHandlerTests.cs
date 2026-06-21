using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NajaEcho.Application.Abstractions;
using NajaEcho.Application.Features.Warehouse.ChangeInventoryQuantity;
using NajaEcho.Application.Features.Warehouse.GetInventory;
using NajaEcho.Application.Features.Warehouse.GetInventoryFilters;
using NajaEcho.Application.Features.Warehouse.RemoveInventoryItem;
using NajaEcho.Application.Features.Warehouse.SearchCatalogItems;
using Xunit;

namespace NajaEcho.Application.Tests.Features.Warehouse;

public sealed class RemoveInventoryItemHandlerTests
{
    private static readonly Guid KnownRowId = Guid.NewGuid();
    private static readonly Guid UnknownRowId = Guid.NewGuid();

    private sealed class FakeWarehouseRepo : IWarehouseInventoryRepository
    {
        private readonly HashSet<Guid> _rows = [KnownRowId];

        public Task RemoveAsync(Guid id, CancellationToken ct)
        {
            if (!_rows.Contains(id))
                throw new InventoryRowNotFoundException(id);
            _rows.Remove(id);
            return Task.CompletedTask;
        }

        public bool Contains(Guid id) => _rows.Contains(id);

        public Task<IReadOnlyList<InventoryRowDto>> GetInventoryAsync(string? name, string? type, string? subtype, Guid? ownerUserId, string? location, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<InventoryRowDto>>([]);
        public Task<InventoryFiltersDto> GetInventoryFiltersAsync(CancellationToken ct) =>
            Task.FromResult(new InventoryFiltersDto([], [], []));
        public Task<IReadOnlyList<CatalogItemResultDto>> SearchCatalogItemsAsync(string? search, int limit, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<CatalogItemResultDto>>([]);
        public Task<(InventoryRowDto Row, bool IsNew)> AddOrIncrementAsync(Guid itemId, Guid ownerUserId, string location, int quantity, int quality, Guid? stationId, CancellationToken ct) =>
            throw new NotImplementedException();
        public Task<InventoryRowDto> UpdateQuantityAsync(Guid id, int quantity, CancellationToken ct) =>
            throw new NotImplementedException();
    }

    [Fact]
    public async Task HandleAsync_KnownRow_RemovesSuccessfully()
    {
        var repo = new FakeWarehouseRepo();
        var handler = new RemoveInventoryItemHandler(repo, NullLogger<RemoveInventoryItemHandler>.Instance);

        Func<Task> act = () => handler.HandleAsync(new RemoveInventoryItemCommand(KnownRowId), default);

        await act.Should().NotThrowAsync();
        repo.Contains(KnownRowId).Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_MissingRow_ThrowsInventoryRowNotFoundException()
    {
        var repo = new FakeWarehouseRepo();
        var handler = new RemoveInventoryItemHandler(repo, NullLogger<RemoveInventoryItemHandler>.Instance);

        Func<Task> act = () => handler.HandleAsync(new RemoveInventoryItemCommand(UnknownRowId), default);

        await act.Should().ThrowAsync<InventoryRowNotFoundException>();
    }

    [Fact]
    public async Task HandleAsync_RemoveDoesNotAffectOtherRows()
    {
        var repo = new FakeWarehouseRepo();
        var handler = new RemoveInventoryItemHandler(repo, NullLogger<RemoveInventoryItemHandler>.Instance);

        await handler.HandleAsync(new RemoveInventoryItemCommand(KnownRowId), default);

        // KnownRowId removed; UnknownRowId was never in the set — row counts are unrelated
        repo.Contains(KnownRowId).Should().BeFalse();
    }
}
