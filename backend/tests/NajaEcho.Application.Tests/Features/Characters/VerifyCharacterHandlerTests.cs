using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NajaEcho.Application.Abstractions;
using NajaEcho.Application.Features.Characters.VerifyCharacter;
using NajaEcho.Domain.Characters;

namespace NajaEcho.Application.Tests.Features.Characters;

public sealed class VerifyCharacterHandlerTests
{
    private static readonly Guid OwnerId = Guid.NewGuid();
    private const string ValidHandle = "g8r";
    private const string ScrapedMoniker = "G8trdone";

    private sealed class FakePendingRepo(PendingCharacterRegistration? stored) : IPendingRegistrationRepository
    {
        public bool RemovedCalled { get; private set; }
        public Task<PendingCharacterRegistration?> GetByOwnerAsync(Guid ownerUserId, CancellationToken ct)
            => Task.FromResult(stored);
        public Task UpsertAsync(PendingCharacterRegistration pending, CancellationToken ct) => Task.CompletedTask;
        public Task RemoveByOwnerAsync(Guid ownerUserId, CancellationToken ct)
        {
            RemovedCalled = true;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeCharacterRepo : ICharacterRepository
    {
        public bool HandleExists { get; set; }
        public Character? Added { get; private set; }
        public Task<bool> HandleExistsAsync(string handle, CancellationToken ct) => Task.FromResult(HandleExists);
        public Task AddAsync(Character character, CancellationToken ct)
        {
            Added = character;
            return Task.CompletedTask;
        }
        public Task<IReadOnlyList<Character>> GetByOwnerAsync(Guid ownerUserId, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<Character>>([]);
    }

    private sealed class FakeRsiClient(object result) : IRsiCitizenClient
    {
        public Task<object> FetchCitizenAsync(string handle, CancellationToken ct) => Task.FromResult(result);
    }

    private sealed class FakeClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow => now;
    }

    private static VerifyCharacterHandler MakeHandler(
        FakePendingRepo? pendingRepo = null,
        FakeCharacterRepo? charRepo = null,
        IRsiCitizenClient? rsiClient = null,
        DateTimeOffset? now = null)
    {
        var theNow = now ?? DateTimeOffset.UtcNow;
        var pending = PendingCharacterRegistration.Create(OwnerId, theNow);

        return new VerifyCharacterHandler(
            pendingRepo ?? new FakePendingRepo(pending),
            charRepo ?? new FakeCharacterRepo(),
            rsiClient ?? new FakeRsiClient(new RsiCitizenPage($"bio with token {pending.Token}", ScrapedMoniker)),
            new FakeClock(theNow),
            NullLogger<VerifyCharacterHandler>.Instance);
    }

    [Fact]
    public async Task HandleAsync_Success_CreatesCharacterWithScrapedMoniker()
    {
        var now = DateTimeOffset.UtcNow;
        var pending = PendingCharacterRegistration.Create(OwnerId, now);
        var pendingRepo = new FakePendingRepo(pending);
        var charRepo = new FakeCharacterRepo();
        var rsiClient = new FakeRsiClient(new RsiCitizenPage($"text {pending.Token} more", ScrapedMoniker));

        var handler = new VerifyCharacterHandler(pendingRepo, charRepo, rsiClient,
            new FakeClock(now), NullLogger<VerifyCharacterHandler>.Instance);

        var dto = await handler.HandleAsync(new VerifyCharacterCommand(OwnerId, ValidHandle), default);

        dto.Name.Should().Be(ScrapedMoniker);
        dto.Handle.Should().Be(ValidHandle);
        charRepo.Added.Should().NotBeNull();
        pendingRepo.RemovedCalled.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_MonikerAbsent_FallsBackToHandle()
    {
        var now = DateTimeOffset.UtcNow;
        var pending = PendingCharacterRegistration.Create(OwnerId, now);
        var pendingRepo = new FakePendingRepo(pending);
        var charRepo = new FakeCharacterRepo();
        var rsiClient = new FakeRsiClient(new RsiCitizenPage($"bio has {pending.Token}", null));

        var handler = new VerifyCharacterHandler(pendingRepo, charRepo, rsiClient,
            new FakeClock(now), NullLogger<VerifyCharacterHandler>.Instance);

        var dto = await handler.HandleAsync(new VerifyCharacterCommand(OwnerId, ValidHandle), default);

        dto.Name.Should().Be(ValidHandle);
    }

    [Fact]
    public async Task HandleAsync_ExpiredPending_ThrowsTokenExpiredException()
    {
        var past = DateTimeOffset.UtcNow.AddHours(-1);
        var pending = PendingCharacterRegistration.Create(OwnerId, past);
        var pendingRepo = new FakePendingRepo(pending);

        var handler = new VerifyCharacterHandler(pendingRepo, new FakeCharacterRepo(),
            new FakeRsiClient(new RsiCitizenPage("", null)),
            new FakeClock(DateTimeOffset.UtcNow), NullLogger<VerifyCharacterHandler>.Instance);

        var act = () => handler.HandleAsync(new VerifyCharacterCommand(OwnerId, ValidHandle), default);
        await act.Should().ThrowAsync<TokenExpiredException>();
    }

    [Fact]
    public async Task HandleAsync_NoPending_ThrowsTokenExpiredException()
    {
        var pendingRepo = new FakePendingRepo(null);

        var handler = new VerifyCharacterHandler(pendingRepo, new FakeCharacterRepo(),
            new FakeRsiClient(new RsiCitizenPage("", null)),
            new FakeClock(DateTimeOffset.UtcNow), NullLogger<VerifyCharacterHandler>.Instance);

        var act = () => handler.HandleAsync(new VerifyCharacterCommand(OwnerId, ValidHandle), default);
        await act.Should().ThrowAsync<TokenExpiredException>();
    }

    [Fact]
    public async Task HandleAsync_DuplicateHandle_ThrowsHandleAlreadyClaimedException()
    {
        var now = DateTimeOffset.UtcNow;
        var pending = PendingCharacterRegistration.Create(OwnerId, now);
        var charRepo = new FakeCharacterRepo { HandleExists = true };

        var handler = new VerifyCharacterHandler(new FakePendingRepo(pending), charRepo,
            new FakeRsiClient(new RsiCitizenPage("", null)),
            new FakeClock(now), NullLogger<VerifyCharacterHandler>.Instance);

        var act = () => handler.HandleAsync(new VerifyCharacterCommand(OwnerId, ValidHandle), default);
        await act.Should().ThrowAsync<HandleAlreadyClaimedException>();
    }

    [Fact]
    public async Task HandleAsync_RsiProfileNotFound_ThrowsRsiProfileNotFoundException()
    {
        var now = DateTimeOffset.UtcNow;
        var pending = PendingCharacterRegistration.Create(OwnerId, now);
        var rsiClient = new FakeRsiClient(new RsiProfileNotFound());

        var handler = new VerifyCharacterHandler(new FakePendingRepo(pending), new FakeCharacterRepo(),
            rsiClient, new FakeClock(now), NullLogger<VerifyCharacterHandler>.Instance);

        var act = () => handler.HandleAsync(new VerifyCharacterCommand(OwnerId, ValidHandle), default);
        await act.Should().ThrowAsync<RsiProfileNotFoundException>();
    }

    [Fact]
    public async Task HandleAsync_RsiUnreachable_ThrowsRsiUnreachableException()
    {
        var now = DateTimeOffset.UtcNow;
        var pending = PendingCharacterRegistration.Create(OwnerId, now);
        var rsiClient = new FakeRsiClient(new RsiUnreachable());

        var handler = new VerifyCharacterHandler(new FakePendingRepo(pending), new FakeCharacterRepo(),
            rsiClient, new FakeClock(now), NullLogger<VerifyCharacterHandler>.Instance);

        var act = () => handler.HandleAsync(new VerifyCharacterCommand(OwnerId, ValidHandle), default);
        await act.Should().ThrowAsync<RsiUnreachableException>();
    }

    [Fact]
    public async Task HandleAsync_TokenNotInPage_ThrowsTokenNotFoundException()
    {
        var now = DateTimeOffset.UtcNow;
        var pending = PendingCharacterRegistration.Create(OwnerId, now);
        var rsiClient = new FakeRsiClient(new RsiCitizenPage("no token here", ScrapedMoniker));

        var handler = new VerifyCharacterHandler(new FakePendingRepo(pending), new FakeCharacterRepo(),
            rsiClient, new FakeClock(now), NullLogger<VerifyCharacterHandler>.Instance);

        var act = () => handler.HandleAsync(new VerifyCharacterCommand(OwnerId, ValidHandle), default);
        await act.Should().ThrowAsync<TokenNotFoundException>();
    }
}
