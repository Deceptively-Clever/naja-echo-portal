using Microsoft.Extensions.Logging;
using NajaEcho.Application.Abstractions;

namespace NajaEcho.Application.Features.Warehouse.ShipComponents.GetShipComponents;

public sealed class GetShipComponentsQueryHandler(
    IShipComponentRepository repository,
    ILogger<GetShipComponentsQueryHandler> logger)
{
    public async Task<IReadOnlyList<ShipComponentRowDto>> HandleAsync(GetShipComponentsQuery query, CancellationToken ct)
    {
        logger.LogInformation(
            "GetShipComponents name={Name} types={Types} classes={Classes} sizes={Sizes} grades={Grades} owners={Owners} unknownClass={UnknownClass} unknownSize={UnknownSize} unknownGrade={UnknownGrade}",
            query.Name, query.Types, query.Classes, query.Sizes, query.Grades, query.OwnerUserIds,
            query.UnknownClass, query.UnknownSize, query.UnknownGrade);

        var rows = await repository.GetShipComponentsAsync(query, ct);

        logger.LogInformation("GetShipComponents returned {Count} rows", rows.Count);
        return rows;
    }
}
