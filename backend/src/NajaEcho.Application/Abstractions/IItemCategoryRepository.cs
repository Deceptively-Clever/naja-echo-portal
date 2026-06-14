using NajaEcho.Domain.ItemCategories;

namespace NajaEcho.Application.Abstractions;

public interface IItemCategoryRepository
{
    Task<(int Inserted, int Updated, int Unchanged)> BulkUpsertAsync(
        IReadOnlyList<ItemCategory> incoming, CancellationToken ct = default);

    Task<IReadOnlyList<ItemCategory>> GetAllAsync(CancellationToken ct = default);

    Task<IReadOnlyList<ItemCategory>> GetEligibleAsync(CancellationToken ct = default);

    Task<DateTimeOffset?> GetLastRefreshedAtAsync(CancellationToken ct = default);

    Task<int> GetActiveItemCountAsync(int categoryUexId, CancellationToken ct = default);

    Task<DateTimeOffset?> GetLastImportedAtAsync(int categoryUexId, CancellationToken ct = default);
}
