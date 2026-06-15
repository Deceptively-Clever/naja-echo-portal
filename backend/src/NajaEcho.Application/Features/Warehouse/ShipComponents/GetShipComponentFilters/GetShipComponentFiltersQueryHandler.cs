using Microsoft.Extensions.Logging;
using NajaEcho.Application.Abstractions;

namespace NajaEcho.Application.Features.Warehouse.ShipComponents.GetShipComponentFilters;

public sealed class GetShipComponentFiltersQueryHandler(
    IShipComponentRepository repository,
    ILogger<GetShipComponentFiltersQueryHandler> logger)
{
    public async Task<ShipComponentFiltersDto> HandleAsync(GetShipComponentFiltersQuery query, CancellationToken ct)
    {
        logger.LogInformation("GetShipComponentFilters");
        var dto = await repository.GetShipComponentFiltersAsync(ct);
        logger.LogInformation(
            "GetShipComponentFilters types={TypeCount} classes={ClassCount} unknownClass={UnknownClass}",
            dto.Types.Count, dto.Classes.Count, dto.UnknownClass);
        return dto;
    }
}
