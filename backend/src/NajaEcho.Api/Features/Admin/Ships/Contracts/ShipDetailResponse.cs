namespace NajaEcho.Api.Features.Admin.Ships.Contracts;

public sealed record ShipDetailResponse(Guid Id, string Status, IReadOnlyDictionary<string, object?> Fields);
