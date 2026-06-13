namespace NajaEcho.Api.Features.Admin.Ships.Contracts;

public sealed record ShipListItemResponse(Guid Id, string Name, string? CompanyName, string Status);
