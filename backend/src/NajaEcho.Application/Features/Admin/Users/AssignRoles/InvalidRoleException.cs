namespace NajaEcho.Application.Features.Admin.Users.AssignRoles;

public sealed class InvalidRoleException(string role)
    : Exception($"Role '{role}' does not exist.") { }
