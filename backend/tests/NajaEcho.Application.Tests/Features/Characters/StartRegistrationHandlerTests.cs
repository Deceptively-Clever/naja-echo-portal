using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NajaEcho.Application.Abstractions;
using NajaEcho.Application.Features.Characters.StartRegistration;
using NajaEcho.Domain.Characters;

namespace NajaEcho.Application.Tests.Features.Characters;

public sealed class StartRegistrationHandlerTests
{
    private static readonly Guid OwnerId = Guid.NewGuid();

    private sealed class FakePendingRepo : IPendingRegistrationRepository
    {
        public PendingCharacterRegistration? Stored { get; private set; }
        private readonly PendingCharacterRegistration? _initial;

        public FakePendingRepo(PendingCharacterRegistration? initial = null) => _initial = initial;

        public Task<PendingCharacterRegistration?> GetByOwnerAsync(Guid ownerUserId, CancellationToken ct)
            => Task.FromResult(_initial);

        public Task UpsertAsync(PendingCharacterRegistration pending, CancellationToken ct)
        {
            Stored = pending;
            return Task.CompletedTask;
        }

        public Task RemoveByOwnerAsync(Guid ownerUserId, CancellationToken ct)
            => Task.CompletedTask;
    }

    private sealed class FakeClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow => now;
    }

    private static StartRegistrationHandler MakeHandler(
        IPendingRegistrationRepository repo,
        DateTimeOffset? now = null) =>
        new(repo, new FakeClock(now ?? DateTimeOffset.UtcNow),
            NullLogger<StartRegistrationHandler>.Instance);

    [Fact]
    public async Task HandleAsync_NoPendingExists_CreatesFreshToken()
    {
        var repo = new FakePendingRepo();
        var dto = await MakeHandler(repo).HandleAsync(new StartRegistrationCommand(OwnerId), default);

        dto.Token.Should().StartWith("naja-");
        dto.ExpiresAt.Should().BeCloseTo(DateTimeOffset.UtcNow.AddMinutes(30), TimeSpan.FromSeconds(5));
        repo.Stored.Should().NotBeNull();
    }

    [Fact]
    public async Task HandleAsync_NonExpiredPendingExists_ReturnsSameToken()
    {
        var now = DateTimeOffset.UtcNow;
        var existing = PendingCharacterRegistration.Create(OwnerId, now);
        var repo = new FakePendingRepo(existing);

        var dto = await MakeHandler(repo, now).HandleAsync(new StartRegistrationCommand(OwnerId), default);

        dto.Token.Should().Be(existing.Token);
        dto.ExpiresAt.Should().Be(existing.ExpiresAt);
        repo.Stored.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_ExpiredPendingExists_GeneratesFreshToken()
    {
        var past = DateTimeOffset.UtcNow.AddHours(-1);
        var existing = PendingCharacterRegistration.Create(OwnerId, past);
        var repo = new FakePendingRepo(existing);
        var now = DateTimeOffset.UtcNow;

        var dto = await MakeHandler(repo, now).HandleAsync(new StartRegistrationCommand(OwnerId), default);

        dto.Token.Should().NotBe(existing.Token);
        repo.Stored.Should().NotBeNull();
        repo.Stored!.Token.Should().Be(dto.Token);
    }
}
