using FluentAssertions;
using NajaEcho.Domain.Users;
using Xunit;

namespace NajaEcho.Domain.Tests.Users;

public class UserProfileTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 8, 12, 0, 0, TimeSpan.Zero);

    private static DiscordProfile SampleProfile(
        string id = "123456789",
        string username = "testuser",
        string? globalName = "Test User",
        string? avatar = "abc123",
        string? email = "test@example.com",
        bool verified = true) =>
        new()
        {
            Id = id,
            Username = username,
            GlobalName = globalName,
            Avatar = avatar,
            Email = email,
            Verified = verified,
        };

    [Fact]
    public void CreateFromDiscord_SetsAllFields()
    {
        var profile = SampleProfile();
        var user = UserProfile.CreateFromDiscord(profile, Now);

        user.Id.Should().NotBe(Guid.Empty);
        user.DiscordUserId.Should().Be("123456789");
        user.DisplayName.Should().Be("Test User"); // GlobalName wins
        user.AvatarRef.Should().Be("abc123");
        user.Email.Should().Be("test@example.com");
        user.CreatedAtUtc.Should().Be(Now);
        user.LastLoginAtUtc.Should().Be(Now);
        user.LastUpdatedAtUtc.Should().Be(Now);
    }

    [Fact]
    public void CreateFromDiscord_UsesUsername_WhenGlobalNameIsNull()
    {
        var profile = SampleProfile(globalName: null);
        var user = UserProfile.CreateFromDiscord(profile, Now);
        user.DisplayName.Should().Be("testuser");
    }

    [Fact]
    public void CreateFromDiscord_OmitsEmail_WhenNotVerified()
    {
        var profile = SampleProfile(verified: false);
        var user = UserProfile.CreateFromDiscord(profile, Now);
        user.Email.Should().BeNull();
    }

    [Fact]
    public void CreateFromDiscord_OmitsEmail_WhenEmailIsNull()
    {
        var profile = SampleProfile(email: null);
        var user = UserProfile.CreateFromDiscord(profile, Now);
        user.Email.Should().BeNull();
    }

    [Fact]
    public void RecordLogin_AlwaysAdvancesLastLoginAtUtc()
    {
        var user = UserProfile.CreateFromDiscord(SampleProfile(), Now);
        var later = Now.AddHours(1);

        user.RecordLogin(SampleProfile(), later);

        user.LastLoginAtUtc.Should().Be(later);
    }

    [Fact]
    public void RecordLogin_UpdatesDisplayName_WhenChanged()
    {
        var user = UserProfile.CreateFromDiscord(SampleProfile(), Now);
        var later = Now.AddHours(1);

        user.RecordLogin(SampleProfile(globalName: "New Name"), later);

        user.DisplayName.Should().Be("New Name");
        user.LastUpdatedAtUtc.Should().Be(later);
    }

    [Fact]
    public void RecordLogin_UpdatesAvatar_WhenChanged()
    {
        var user = UserProfile.CreateFromDiscord(SampleProfile(), Now);
        var later = Now.AddHours(1);

        user.RecordLogin(SampleProfile(avatar: "newAvatar"), later);

        user.AvatarRef.Should().Be("newAvatar");
        user.LastUpdatedAtUtc.Should().Be(later);
    }

    [Fact]
    public void RecordLogin_DoesNotAdvanceLastUpdatedAtUtc_WhenNothingChanged()
    {
        var user = UserProfile.CreateFromDiscord(SampleProfile(), Now);
        var later = Now.AddHours(1);

        user.RecordLogin(SampleProfile(), later);

        user.LastUpdatedAtUtc.Should().Be(Now); // unchanged
        user.LastLoginAtUtc.Should().Be(later);
    }

    [Fact]
    public void DiscordProfile_DisplayName_PrefersGlobalName()
    {
        var p = new DiscordProfile { Id = "1", Username = "user", GlobalName = "Global" };
        p.DisplayName.Should().Be("Global");
    }

    [Fact]
    public void DiscordProfile_DisplayName_FallsBackToUsername()
    {
        var p = new DiscordProfile { Id = "1", Username = "user", GlobalName = null };
        p.DisplayName.Should().Be("user");
    }
}
