using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NajaEcho.Application.Abstractions;
using NajaEcho.Application.Features.Warehouse.AddInventoryItem;
using NajaEcho.Application.Features.Warehouse.ChangeInventoryQuantity;
using NajaEcho.Application.Features.Warehouse.GetInventory;
using NajaEcho.Application.Features.Warehouse.GetInventoryFilters;
using NajaEcho.Application.Features.Warehouse.SearchCatalogItems;
using NajaEcho.Domain.Items;
using Xunit;

namespace NajaEcho.Application.Tests.Features.Warehouse;

public sealed class AddInventoryItemHandlerTests
{
    private static readonly Guid KnownItemId = Guid.NewGuid();
    private static readonly Guid KnownOwnerId = Guid.NewGuid();
    private static readonly Guid KnownRowId = Guid.NewGuid();

    private sealed class FakeWarehouseRepo : IWarehouseInventoryRepository
    {
        public bool NextIsNew { get; set; } = true;
        private readonly InventoryRowDto _row;
        public FakeWarehouseRepo() => _row = new(KnownRowId, KnownItemId, "Test Item", null, null, 1, KnownOwnerId, "Alice", "Bay 1");

        public Task<(InventoryRowDto Row, bool IsNew)> AddOrIncrementAsync(Guid itemId, Guid ownerUserId, string location, int quantity, CancellationToken ct) =>
            Task.FromResult((_row with { ItemId = itemId, OwnerUserId = ownerUserId, Location = location, Quantity = quantity }, NextIsNew));

        public Task<IReadOnlyList<InventoryRowDto>> GetInventoryAsync(string? name, string? type, string? subtype, Guid? ownerUserId, string? location, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<InventoryRowDto>>([]);
        public Task<InventoryFiltersDto> GetInventoryFiltersAsync(CancellationToken ct) =>
            Task.FromResult(new InventoryFiltersDto([], [], []));
        public Task<IReadOnlyList<CatalogItemResultDto>> SearchCatalogItemsAsync(string? search, int limit, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<CatalogItemResultDto>>([]);
        public Task<InventoryRowDto> UpdateQuantityAsync(Guid id, int quantity, CancellationToken ct) =>
            throw new NotImplementedException();
        public Task RemoveAsync(Guid id, CancellationToken ct) => throw new NotImplementedException();
    }

    private sealed class FakeItemRepo : IItemRepository
    {
        public bool ItemExists { get; set; } = true;
        public Task<Item?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(ItemExists ? (Item?)new Item { Id = id, Name = "Test Item", Status = ItemStatus.Active, Uuid = id.ToString(), UexId = 1, IdCategory = 1, RawData = System.Text.Json.JsonDocument.Parse("{}"), ImportedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow } : null);
        public Task<(int Inserted, int Updated, int Unchanged, int SoftDeleted, int Restored)> BulkUpsertForCategoryAsync(int idCategory, IReadOnlyList<Item> incoming, CancellationToken ct) =>
            throw new NotImplementedException();
    }

    private sealed class FakeUserRepo : IUserRepository
    {
        public bool UserExists { get; set; } = true;
        public Task<bool> ExistsAsync(Guid userId, CancellationToken ct) => Task.FromResult(UserExists);
        public Task<IReadOnlyList<(Guid Id, string DisplayName)>> GetAllAsync(CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<(Guid, string)>>([]);
    }

    private static AddInventoryItemHandler MakeHandler(FakeWarehouseRepo? repo = null, FakeItemRepo? itemRepo = null, FakeUserRepo? userRepo = null) =>
        new(repo ?? new FakeWarehouseRepo(), itemRepo ?? new FakeItemRepo(), userRepo ?? new FakeUserRepo(), NullLogger<AddInventoryItemHandler>.Instance);

    [Fact]
    public async Task HandleAsync_NewRow_ReturnsCreated()
    {
        var repo = new FakeWarehouseRepo { NextIsNew = true };
        var handler = MakeHandler(repo);

        var (row, isNew) = await handler.HandleAsync(
            new AddInventoryItemCommand(KnownItemId, KnownOwnerId, "Bay 1", 5), default);

        isNew.Should().BeTrue();
        row.Should().NotBeNull();
    }

    [Fact]
    public async Task HandleAsync_ExistingRow_ReturnsIncrement()
    {
        var repo = new FakeWarehouseRepo { NextIsNew = false };
        var handler = MakeHandler(repo);

        var (row, isNew) = await handler.HandleAsync(
            new AddInventoryItemCommand(KnownItemId, KnownOwnerId, "Bay 1", 3), default);

        isNew.Should().BeFalse();
        row.Should().NotBeNull();
    }

    [Fact]
    public async Task HandleAsync_LocationTrimmed_PassesTrimmedToRepository()
    {
        var repo = new FakeWarehouseRepo();
        var handler = MakeHandler(repo);
        var (row, _) = await handler.HandleAsync(
            new AddInventoryItemCommand(KnownItemId, KnownOwnerId, "  Bay 1  ", 1), default);

        row.Location.Should().Be("Bay 1");
    }

    [Fact]
    public async Task HandleAsync_UnknownItem_ThrowsItemNotFoundException()
    {
        var itemRepo = new FakeItemRepo { ItemExists = false };
        var handler = MakeHandler(itemRepo: itemRepo);

        Func<Task> act = () => handler.HandleAsync(
            new AddInventoryItemCommand(KnownItemId, KnownOwnerId, "Bay 1", 1), default);

        await act.Should().ThrowAsync<ItemNotFoundException>();
    }

    [Fact]
    public async Task HandleAsync_UnknownOwner_ThrowsOwnerNotFoundException()
    {
        var userRepo = new FakeUserRepo { UserExists = false };
        var handler = MakeHandler(userRepo: userRepo);

        Func<Task> act = () => handler.HandleAsync(
            new AddInventoryItemCommand(KnownItemId, KnownOwnerId, "Bay 1", 1), default);

        await act.Should().ThrowAsync<OwnerNotFoundException>();
    }

    [Fact]
    public async Task HandleAsync_EmptyLocation_ThrowsArgumentException()
    {
        var handler = MakeHandler();

        Func<Task> act = () => handler.HandleAsync(
            new AddInventoryItemCommand(KnownItemId, KnownOwnerId, "", 1), default);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task HandleAsync_WhitespaceLocation_ThrowsArgumentException()
    {
        var handler = MakeHandler();

        Func<Task> act = () => handler.HandleAsync(
            new AddInventoryItemCommand(KnownItemId, KnownOwnerId, "   ", 1), default);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task HandleAsync_ZeroQuantity_ThrowsArgumentOutOfRangeException()
    {
        var handler = MakeHandler();

        Func<Task> act = () => handler.HandleAsync(
            new AddInventoryItemCommand(KnownItemId, KnownOwnerId, "Bay 1", 0), default);

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task HandleAsync_NegativeQuantity_ThrowsArgumentOutOfRangeException()
    {
        var handler = MakeHandler();

        Func<Task> act = () => handler.HandleAsync(
            new AddInventoryItemCommand(KnownItemId, KnownOwnerId, "Bay 1", -1), default);

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }
}
