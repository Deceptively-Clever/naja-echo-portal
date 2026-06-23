namespace NajaEcho.Api.Features.Warehouse.Contracts;

public sealed record LocationOption(Guid Id, string Name, string Type);

public sealed record LocationListResponse(IReadOnlyList<LocationOption> Locations);

public sealed record TransferLocationRequest(Guid LocationId, string LocationType);
