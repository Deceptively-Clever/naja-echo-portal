using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NajaEcho.Application.Abstractions;
using NajaEcho.Application.Features.Characters.GetCharacters;
using NajaEcho.Domain.Characters;

namespace NajaEcho.Application.Tests.Features.Characters;

public sealed class GetCharactersHandlerTests
{
    private static readonly Guid OwnerId = Guid.NewGuid();

    private sealed class FakeCharacterRepo(IReadOnlyList<Character> characters) : ICharacterRepository
    {
        public Task<bool> HandleExistsAsync(string handle, CancellationToken ct) => Task.FromResult(false);
        public Task AddAsync(Character character, CancellationToken ct) => Task.CompletedTask;
        public Task<IReadOnlyList<Character>> GetByOwnerAsync(Guid ownerUserId, CancellationToken ct)
            => Task.FromResult(characters);
    }

    private static GetCharactersHandler MakeHandler(IReadOnlyList<Character> characters)
        => new(new FakeCharacterRepo(characters), NullLogger<GetCharactersHandler>.Instance);

    [Fact]
    public async Task HandleAsync_OwnerWithTwoCharacters_ReturnsBothDtos()
    {
        var chars = new List<Character>
        {
            new() { Id = Guid.NewGuid(), OwnerUserId = OwnerId, Name = "Alpha", Handle = "alpha", CreatedAt = DateTimeOffset.UtcNow },
            new() { Id = Guid.NewGuid(), OwnerUserId = OwnerId, Name = "Beta", Handle = "beta", CreatedAt = DateTimeOffset.UtcNow.AddMinutes(1) },
        };

        var result = await MakeHandler(chars).HandleAsync(new GetCharactersQuery(OwnerId), default);

        result.Should().HaveCount(2);
        result[0].Handle.Should().Be("alpha");
        result[1].Handle.Should().Be("beta");
    }

    [Fact]
    public async Task HandleAsync_OwnerWithNoCharacters_ReturnsEmptyList()
    {
        var result = await MakeHandler([]).HandleAsync(new GetCharactersQuery(OwnerId), default);
        result.Should().BeEmpty();
    }
}
