using NajaEcho.Application.Abstractions;

namespace NajaEcho.Application.Features.Auth.GetCurrentUser;

public sealed class GetCurrentUserHandler(IUserRepository users)
{
    public async Task<CurrentUserDto?> HandleAsync(GetCurrentUserQuery query, CancellationToken ct = default)
    {
        var user = await users.FindByIdAsync(query.UserId, ct);
        if (user is null) return null;
        return new CurrentUserDto(user.Id, user.DisplayName, user.AvatarRef);
    }
}
