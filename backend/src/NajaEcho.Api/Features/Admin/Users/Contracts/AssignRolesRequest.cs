namespace NajaEcho.Api.Features.Admin.Users.Contracts;

public sealed record AssignRolesRequest(IReadOnlyList<string> Roles);
