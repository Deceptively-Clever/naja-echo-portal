namespace NajaEcho.Application.Features.Admin.Users.AddCharacterForUser;

public sealed class UserNotFoundException(Guid userId)
    : Exception($"User '{userId}' was not found.") { }
