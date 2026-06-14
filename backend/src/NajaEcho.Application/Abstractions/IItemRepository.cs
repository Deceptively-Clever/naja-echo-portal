using NajaEcho.Domain.Items;

namespace NajaEcho.Application.Abstractions;

public interface IItemRepository
{
    Task<(int Inserted, int Updated, int Unchanged, int SoftDeleted, int Restored)> BulkUpsertForCategoryAsync(
        int idCategory, IReadOnlyList<Item> incoming, CancellationToken ct = default);
}
