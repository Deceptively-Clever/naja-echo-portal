using NajaEcho.Domain.Commodities;

namespace NajaEcho.Application.Abstractions;

public interface ICommodityRepository
{
    Task<(IReadOnlyList<Commodity> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, CancellationToken ct = default);

    Task<(int Inserted, int Updated, int Restored, int SoftDeleted)> BulkUpsertAsync(
        IReadOnlyList<Commodity> incoming, CancellationToken ct = default);
}
