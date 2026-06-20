using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NajaEcho.Application.Abstractions;
using NajaEcho.Application.Features.Characters.GetRegistration;
using NajaEcho.Domain.Characters;

namespace NajaEcho.Application.Tests.Features.Characters;

public sealed class GetRegistrationHandlerTests
{
    private static readonly Guid OwnerId = Guid.NewGuid();

    private sealed class FakePendingRepo(PendingCharacterRegistration? stored) : IPendingRegistrationRepository
    {
        public Task<PendingCharacterRegistration?> GetByOwnerAsync(Guid ownerUserId, CancellationToken ct)
            => Task.FromResult(stored);
        public Task UpsertAsync(PendingCharacterRegistration pending, CancellationToken ct) => Task.CompletedTask;
        public Task RemoveByOwnerAsync(Guid ownerUserId, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakeClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow => now;
    }

    private static GetRegistrationHandler MakeHandler(IPendingRegistrationRepository repo, DateTimeOffset? now = null)
        => new(repo, new FakeClock(now ?? DateTimeOffset.UtcNow), NullLogger<GetRegistrationHandler>.Instance);

    [Fact]
    public async Task HandleAsync_NonExpiredPending_ReturnsPendingRegistrationDto()
    {
        var now = DateTimeOffset.UtcNow;
        var pending = PendingCharacterRegistration.Create(OwnerId, now);
        var repo = new FakePendingRepo(pending);

        var dto = await MakeHandler(repo, now).HandleAsync(new GetRegistrationQuery(OwnerId), default);

        dto.Should().NotBeNull();
        dto!.Token.Should().Be(pending.Token);
        dto.ExpiresAt.Should().Be(pending.ExpiresAt);
    }

    [Fact]
    public async Task HandleAsync_ExpiredPending_ReturnsNull()
    {
        var past = DateTimeOffset.UtcNow.AddHours(-1);
        var pending = PendingCharacterRegistration.Create(OwnerId, past);
        var repo = new FakePendingRepo(pending);

        var dto = await MakeHandler(repo, DateTimeOffset.UtcNow).HandleAsync(new GetRegistrationQuery(OwnerId), default);

        dto.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_NoPending_ReturnsNull()
    {
        var repo = new FakePendingRepo(null);

        var dto = await MakeHandler(repo).HandleAsync(new GetRegistrationQuery(OwnerId), default);

        dto.Should().BeNull();
    }
}
