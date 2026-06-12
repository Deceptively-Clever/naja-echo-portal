using NajaEcho.Application.Abstractions;

namespace NajaEcho.Application.Features.Auth.GetCurrentUser;

public sealed class GetCurrentUserHandler(IExternalLoginService loginService)
{
    public Task<LocalUser?> HandleAsync(GetCurrentUserQuery query, CancellationToken ct = default) =>
        loginService.GetByIdAsync(query.UserId, ct);
}
