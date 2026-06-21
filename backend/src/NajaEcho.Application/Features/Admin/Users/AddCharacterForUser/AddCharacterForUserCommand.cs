namespace NajaEcho.Application.Features.Admin.Users.AddCharacterForUser;

public sealed record AddCharacterForUserCommand(Guid TargetUserId, string Handle);
