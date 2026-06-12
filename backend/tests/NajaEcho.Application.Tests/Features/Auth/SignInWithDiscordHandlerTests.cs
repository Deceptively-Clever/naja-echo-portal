using FluentAssertions;
using NajaEcho.Application.Abstractions;
using NajaEcho.Application.Features.Auth.SignInWithDiscord;
using NajaEcho.Domain.Users;
using Xunit;

namespace NajaEcho.Application.Tests.Features.Auth;

public class SignInWithDiscordHandlerTests
{
    private static DiscordProfile SampleProfile(string id = "disc123") => new()
    {
        Id = id,
        Username = "testuser",
        GlobalName = "Test User",
    };

    private sealed class FakeExternalLoginService : IExternalLoginService
    {
        private readonly Dictionary<Guid, LocalUser> _store = [];

        public LocalUser? Preset { get; set; }
        public LocalUser? LastCreated { get; private set; }

        public Task<LocalUser> FindOrCreateAsync(DiscordProfile profile, CancellationToken ct = default)
        {
            var user = Preset ?? new LocalUser(Guid.NewGuid(), profile.DisplayName, profile.Username);
            LastCreated = user;
            _store[user.Id] = user;
            return Task.FromResult(user);
        }

        public Task<LocalUser?> GetByIdAsync(Guid userId, CancellationToken ct = default)
        {
            _store.TryGetValue(userId, out var user);
            return Task.FromResult(user);
        }
    }

    [Fact]
    public async Task Handle_DelegatesToExternalLoginService()
    {
        var service = new FakeExternalLoginService();
        var handler = new SignInWithDiscordHandler(service);

        var result = await handler.HandleAsync(new SignInWithDiscordCommand(SampleProfile()));

        result.Should().NotBeNull();
        result.DisplayName.Should().Be("Test User");
        result.DiscordUsername.Should().Be("testuser");
        service.LastCreated.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_ReturnsPresetUser_WhenProvided()
    {
        var preset = new LocalUser(Guid.NewGuid(), "Preset Name", "presetuser");
        var service = new FakeExternalLoginService { Preset = preset };
        var handler = new SignInWithDiscordHandler(service);

        var result = await handler.HandleAsync(new SignInWithDiscordCommand(SampleProfile()));

        result.UserId.Should().Be(preset.Id);
        result.DisplayName.Should().Be("Preset Name");
        result.DiscordUsername.Should().Be("presetuser");
    }

    [Fact]
    public async Task Handle_ReturnsUpdatedDisplayName_AfterProfileChange()
    {
        var service = new FakeExternalLoginService();
        var handler = new SignInWithDiscordHandler(service);

        var changedProfile = new DiscordProfile
        {
            Id = "disc123",
            Username = "testuser",
            GlobalName = "New Display Name",
        };

        var result = await handler.HandleAsync(new SignInWithDiscordCommand(changedProfile));
        result.DisplayName.Should().Be("New Display Name");
    }
}
