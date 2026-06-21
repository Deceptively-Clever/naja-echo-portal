namespace NajaEcho.Api.Features.Warehouse.Contracts;

public sealed record StationOption(Guid Id, string Name);

public sealed record StationListResponse(IReadOnlyList<StationOption> Stations);
