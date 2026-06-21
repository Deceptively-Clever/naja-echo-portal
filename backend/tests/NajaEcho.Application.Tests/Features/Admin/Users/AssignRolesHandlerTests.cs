using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NajaEcho.Application.Abstractions;
using NajaEcho.Application.Features.Admin.Users.AddCharacterForUser;
using NajaEcho.Application.Features.Admin.Users.AssignRoles;
using NajaEcho.Application.Features.Admin.Users.GetUsers;

namespace NajaEcho.Application.Tests.Features.Admin.Users;

public sealed class AssignRolesHandlerTests
{
    private sealed class FakeUserRepo : IUserRepository
    {
        public bool Exists { get; set; } = true;
        public IReadOnlyList<string>? LastRolesSet { get; private set; }

        public Task<bool> ExistsAsync(Guid userId, CancellationToken ct) => Task.FromResult(Exists);
        public Task<IReadOnlyList<(Guid Id, string DisplayName)>> GetAllAsync(CancellationToken ct)
            => Task.FromResult<IReadOnlyList<(Guid, string)>>([]);
        public Task<IReadOnlyList<AdminUserDto>> GetUsersWithRolesAndCharactersAsync(CancellationToken ct)
            => Task.FromResult<IReadOnlyList<AdminUserDto>>([]);
        public Task SetRolesAsync(Guid userId, IReadOnlyList<string> roles, CancellationToken ct)
        {
            LastRolesSet = roles;
            return Task.CompletedTask;
        }
    }

    private static (AssignRolesHandler handler, FakeUserRepo repo) MakeHandler()
    {
        var repo = new FakeUserRepo();
        var handler = new AssignRolesHandler(repo, NullLogger<AssignRolesHandler>.Instance);
        return (handler, repo);
    }

    [Fact]
    public async Task HandleAsync_ValidRoles_CallsSetRolesAsync()
    {
        var (handler, repo) = MakeHandler();
        var userId = Guid.NewGuid();

        await handler.HandleAsync(new AssignRolesCommand(userId, ["Admin", "Quartermaster"]), default);

        repo.LastRolesSet.Should().BeEquivalentTo(["Admin", "Quartermaster"]);
    }

    [Fact]
    public async Task HandleAsync_EmptyRoles_CallsSetRolesAsyncWithEmpty()
    {
        var (handler, repo) = MakeHandler();
        var userId = Guid.NewGuid();

        await handler.HandleAsync(new AssignRolesCommand(userId, []), default);

        repo.LastRolesSet.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_UserNotFound_ThrowsUserNotFoundException()
    {
        var (handler, repo) = MakeHandler();
        repo.Exists = false;
        var userId = Guid.NewGuid();

        var act = async () => await handler.HandleAsync(new AssignRolesCommand(userId, ["Admin"]), default);

        await act.Should().ThrowAsync<UserNotFoundException>();
    }

    [Fact]
    public async Task HandleAsync_InvalidRole_ThrowsInvalidRoleException()
    {
        var (handler, _) = MakeHandler();
        var userId = Guid.NewGuid();

        var act = async () => await handler.HandleAsync(new AssignRolesCommand(userId, ["NotARealRole"]), default);

        await act.Should().ThrowAsync<InvalidRoleException>();
    }

    [Fact]
    public async Task HandleAsync_InvalidRoleAmongValidOnes_ThrowsInvalidRoleException()
    {
        var (handler, _) = MakeHandler();
        var userId = Guid.NewGuid();

        var act = async () => await handler.HandleAsync(
            new AssignRolesCommand(userId, ["Admin", "Hacker"]), default);

        await act.Should().ThrowAsync<InvalidRoleException>();
    }
}
