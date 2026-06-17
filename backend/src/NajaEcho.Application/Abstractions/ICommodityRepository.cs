using NajaEcho.Application.Features.Commodities.GetCommodities;
using NajaEcho.Domain.Commodities;

namespace NajaEcho.Application.Abstractions;

public interface ICommodityRepository
{
    Task<Commodity?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<(IReadOnlyList<CommodityListItem> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, CancellationToken ct = default);

    Task<(int Inserted, int Updated, int Unchanged, int Restored, int SoftDeleted)> BulkUpsertAsync(
        IReadOnlyList<Commodity> incoming, CancellationToken ct = default);
}
