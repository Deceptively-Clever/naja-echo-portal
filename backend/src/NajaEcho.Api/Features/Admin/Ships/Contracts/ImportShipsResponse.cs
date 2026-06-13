namespace NajaEcho.Api.Features.Admin.Ships.Contracts;

public sealed record ImportShipsResponse(int Added, int Updated, int Reactivated, int SoftDeleted, int Total);
