namespace NajaEcho.Application.Features.Admin.Users.AssignRoles;

public sealed record AssignRolesCommand(Guid TargetUserId, IReadOnlyList<string> Roles);
