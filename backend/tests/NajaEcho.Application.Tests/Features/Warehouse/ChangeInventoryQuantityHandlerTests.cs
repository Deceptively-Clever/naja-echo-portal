using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NajaEcho.Application.Abstractions;
using NajaEcho.Application.Features.Warehouse.ChangeInventoryQuantity;
using NajaEcho.Application.Features.Warehouse.GetInventory;
using NajaEcho.Application.Features.Warehouse.GetInventoryFilters;
using NajaEcho.Application.Features.Warehouse.SearchCatalogItems;
using Xunit;

namespace NajaEcho.Application.Tests.Features.Warehouse;

public sealed class ChangeInventoryQuantityHandlerTests
{
    private static readonly Guid KnownRowId = Guid.NewGuid();
    private static readonly Guid KnownItemId = Guid.NewGuid();
    private static readonly Guid KnownOwnerId = Guid.NewGuid();

    private sealed class FakeWarehouseRepo : IWarehouseInventoryRepository
    {
        private int _storedQuantity = 3;
        private DateTimeOffset _storedUpdatedAt = DateTimeOffset.UtcNow.AddHours(-1);
        public bool RowExists { get; set; } = true;

        public Task<InventoryRowDto> UpdateQuantityAsync(Guid id, int quantity, CancellationToken ct)
        {
            if (!RowExists || id != KnownRowId)
                throw new InventoryRowNotFoundException(id);
            _storedQuantity = quantity;
            _storedUpdatedAt = DateTimeOffset.UtcNow;
            return Task.FromResult(new InventoryRowDto(id, KnownItemId, "Test Item", null, null, quantity, 500, KnownOwnerId, "Alice", "Bay 1"));
        }

        public Task<IReadOnlyList<InventoryRowDto>> GetInventoryAsync(string? name, string? type, string? subtype, Guid? ownerUserId, string? location, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<InventoryRowDto>>([]);
        public Task<InventoryFiltersDto> GetInventoryFiltersAsync(CancellationToken ct) =>
            Task.FromResult(new InventoryFiltersDto([], [], []));
        public Task<IReadOnlyList<CatalogItemResultDto>> SearchCatalogItemsAsync(string? search, int limit, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<CatalogItemResultDto>>([]);
        public Task<(InventoryRowDto Row, bool IsNew)> AddOrIncrementAsync(Guid itemId, Guid ownerUserId, string location, int quantity, int quality, CancellationToken ct) =>
            throw new NotImplementedException();
        public Task RemoveAsync(Guid id, CancellationToken ct) => throw new NotImplementedException();
    }

    private static ChangeInventoryQuantityHandler MakeHandler(FakeWarehouseRepo? repo = null) =>
        new(repo ?? new FakeWarehouseRepo(), NullLogger<ChangeInventoryQuantityHandler>.Instance);

    [Fact]
    public async Task HandleAsync_ReplacesQuantity_NotIncrements()
    {
        var repo = new FakeWarehouseRepo();
        var handler = MakeHandler(repo);

        var result = await handler.HandleAsync(new ChangeInventoryQuantityCommand(KnownRowId, 10), default);

        result.Quantity.Should().Be(10);
    }

    [Fact]
    public async Task HandleAsync_QuantityIsReplacement_NotAdditive()
    {
        var handler = MakeHandler();

        await handler.HandleAsync(new ChangeInventoryQuantityCommand(KnownRowId, 7), default);
        var result = await handler.HandleAsync(new ChangeInventoryQuantityCommand(KnownRowId, 3), default);

        result.Quantity.Should().Be(3);
    }

    [Fact]
    public async Task HandleAsync_MissingRow_ThrowsInventoryRowNotFoundException()
    {
        var repo = new FakeWarehouseRepo { RowExists = false };
        var handler = MakeHandler(repo);

        Func<Task> act = () => handler.HandleAsync(new ChangeInventoryQuantityCommand(KnownRowId, 5), default);

        await act.Should().ThrowAsync<InventoryRowNotFoundException>();
    }

    [Fact]
    public async Task HandleAsync_ZeroQuantity_ThrowsArgumentOutOfRangeException()
    {
        var handler = MakeHandler();

        Func<Task> act = () => handler.HandleAsync(new ChangeInventoryQuantityCommand(KnownRowId, 0), default);

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task HandleAsync_NegativeQuantity_ThrowsArgumentOutOfRangeException()
    {
        var handler = MakeHandler();

        Func<Task> act = () => handler.HandleAsync(new ChangeInventoryQuantityCommand(KnownRowId, -5), default);

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }
}
