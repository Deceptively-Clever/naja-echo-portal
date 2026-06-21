using Microsoft.Extensions.Logging;
using NajaEcho.Application.Abstractions;

namespace NajaEcho.Application.Features.Admin.Users.GetUsers;

public sealed class GetUsersHandler(
    IUserRepository userRepository,
    ILogger<GetUsersHandler> logger)
{
    public async Task<IReadOnlyList<AdminUserDto>> HandleAsync(GetUsersQuery query, CancellationToken ct)
    {
        var users = await userRepository.GetUsersWithRolesAndCharactersAsync(ct);

        logger.LogInformation("GetAdminUsers returned {Count} members", users.Count);

        return users;
    }
}
