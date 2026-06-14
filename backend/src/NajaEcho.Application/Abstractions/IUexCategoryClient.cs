using System.Text.Json;

namespace NajaEcho.Application.Abstractions;

public interface IUexCategoryClient
{
    Task<IReadOnlyList<JsonDocument>> FetchAllCategoriesAsync(CancellationToken ct = default);
}
