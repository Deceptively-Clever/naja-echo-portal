using FluentAssertions;
using NajaEcho.Application.Abstractions;
using NajaEcho.Application.Features.Auth.SignInWithDiscord;
using NajaEcho.Domain.Users;
using Xunit;

namespace NajaEcho.Application.Tests.Features.Auth;

public class SignInWithDiscordHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 8, 12, 0, 0, TimeSpan.Zero);

    private static DiscordProfile SampleProfile(string id = "disc123") => new()
    {
        Id = id,
        Username = "testuser",
        GlobalName = "Test User",
        Avatar = "avhash",
        Email = "test@example.com",
        Verified = true,
    };

    private sealed class FakeClock : IClock
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly List<UserProfile> _store = [];

        public UserProfile? Preset { get; set; }
        public UserProfile? Added { get; private set; }
        public bool SaveCalled { get; private set; }

        public Task<UserProfile?> FindByDiscordUserIdAsync(string discordUserId, CancellationToken ct) =>
            Task.FromResult(Preset);

        public Task<UserProfile?> FindByIdAsync(Guid id, CancellationToken ct) =>
            Task.FromResult(_store.FirstOrDefault(u => u.Id == id));

        public Task AddAsync(UserProfile user, CancellationToken ct)
        {
            Added = user;
            _store.Add(user);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUoW : IUnitOfWork
    {
        public bool SaveCalled { get; private set; }
        public Task SaveChangesAsync(CancellationToken ct)
        {
            SaveCalled = true;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task Handle_CreatesNewUser_WhenDiscordIdNotFound()
    {
        var repo = new FakeUserRepository { Preset = null };
        var uow = new FakeUoW();
        var handler = new SignInWithDiscordHandler(repo, uow, new FakeClock());

        var result = await handler.HandleAsync(new SignInWithDiscordCommand(SampleProfile()));

        result.Should().NotBeNull();
        result.DisplayName.Should().Be("Test User");
        repo.Added.Should().NotBeNull();
        repo.Added!.DiscordUserId.Should().Be("disc123");
        uow.SaveCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_UpdatesExistingUser_WhenDiscordIdFound()
    {
        var existing = UserProfile.CreateFromDiscord(
            SampleProfile(id: "disc123"), Now.AddDays(-1));
        var repo = new FakeUserRepository { Preset = existing };
        var uow = new FakeUoW();
        var handler = new SignInWithDiscordHandler(repo, uow, new FakeClock());

        var result = await handler.HandleAsync(new SignInWithDiscordCommand(SampleProfile()));

        result.UserId.Should().Be(existing.Id);
        repo.Added.Should().BeNull(); // no new user created
        uow.SaveCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ReturnsUpdatedDisplayName_AfterProfileChange()
    {
        var profile = SampleProfile();
        var existing = UserProfile.CreateFromDiscord(profile, Now.AddDays(-1));
        var repo = new FakeUserRepository { Preset = existing };
        var uow = new FakeUoW();
        var handler = new SignInWithDiscordHandler(repo, uow, new FakeClock());

        var changedProfile = new DiscordProfile
        {
            Id = "disc123",
            Username = "testuser",
            GlobalName = "New Display Name",
            Avatar = "avhash",
        };

        var result = await handler.HandleAsync(new SignInWithDiscordCommand(changedProfile));
        result.DisplayName.Should().Be("New Display Name");
    }
}
