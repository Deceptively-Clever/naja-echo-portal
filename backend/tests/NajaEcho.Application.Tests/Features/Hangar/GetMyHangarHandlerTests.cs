using FluentAssertions;
using NajaEcho.Application.Abstractions;
using NajaEcho.Application.Features.Hangar;
using NajaEcho.Application.Features.Hangar.GetMyHangar;
using NajaEcho.Application.Features.Hangar.GetOrgHangar;
using NajaEcho.Application.Features.Hangar.GetOwningMembers;
using NajaEcho.Application.Features.Hangar.SearchCatalogShips;
using Xunit;

namespace NajaEcho.Application.Tests.Features.Hangar;

public sealed class GetMyHangarHandlerTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    private sealed class FakeHangarRepo : IHangarRepository
    {
        public List<ShipCard> MyHangarCards { get; } = [];

        public Task<PagedResult<ShipCard>> GetMyHangarAsync(
            Guid userId, string? search, int page, int pageSize, CancellationToken ct)
        {
            var filtered = MyHangarCards
                .Where(c => search == null || c.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
                .ToList();
            var total = filtered.Count;
            var items = filtered.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            var totalPages = (int)Math.Ceiling(total / (double)pageSize);
            return Task.FromResult(new PagedResult<ShipCard>(items, page, pageSize, total, totalPages));
        }

        public Task<PagedResult<OrgShipCard>> GetOrgHangarAsync(
            Guid currentUserId, string? search, bool mine, Guid? memberId, int page, int pageSize, CancellationToken ct)
            => throw new NotImplementedException();

        public Task<IReadOnlyList<OwningMember>> GetOwningMembersAsync(CancellationToken ct)
            => throw new NotImplementedException();

        public Task<PagedResult<CatalogSearchRow>> SearchCatalogAsync(
            Guid userId, string? search, int page, int pageSize, CancellationToken ct)
            => throw new NotImplementedException();

        public Task<bool> ExistsAsync(Guid userId, Guid shipId, CancellationToken ct)
            => Task.FromResult(false);

        public Task<ShipCard> AddAsync(Guid userId, Guid shipId, CancellationToken ct)
            => throw new NotImplementedException();

        public Task RemoveAsync(Guid userId, Guid shipId, CancellationToken ct)
            => throw new NotImplementedException();
    }

    private static ShipCard MakeCard(string name, string? urlPhoto = null) =>
        new(Guid.NewGuid(), name, "Aegis", urlPhoto, 10m, "1");

    [Fact]
    public async Task HandleAsync_ReturnsShipCardsForUser()
    {
        var repo = new FakeHangarRepo();
        repo.MyHangarCards.AddRange([MakeCard("Gladius"), MakeCard("Avenger")]);
        var handler = new GetMyHangarHandler(repo);

        var result = await handler.HandleAsync(new GetMyHangarQuery(UserId, null, 1, 25), default);

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task HandleAsync_WithSearch_FiltersCardsByName()
    {
        var repo = new FakeHangarRepo();
        repo.MyHangarCards.AddRange([MakeCard("Gladius"), MakeCard("Avenger"), MakeCard("Gladiator")]);
        var handler = new GetMyHangarHandler(repo);

        var result = await handler.HandleAsync(new GetMyHangarQuery(UserId, "glad", 1, 25), default);

        result.Items.Should().HaveCount(2);
        result.Items.Should().AllSatisfy(c => c.Name.Should().ContainEquivalentOf("glad"));
    }

    [Fact]
    public async Task HandleAsync_WithPaging_ReturnsCorrectPage()
    {
        var repo = new FakeHangarRepo();
        for (var i = 0; i < 30; i++)
            repo.MyHangarCards.Add(MakeCard($"Ship {i:D2}"));
        var handler = new GetMyHangarHandler(repo);

        var result = await handler.HandleAsync(new GetMyHangarQuery(UserId, null, 2, 25), default);

        result.Items.Should().HaveCount(5);
        result.Page.Should().Be(2);
        result.TotalCount.Should().Be(30);
        result.TotalPages.Should().Be(2);
    }

    [Fact]
    public async Task HandleAsync_EmptyHangar_ReturnsEmptyResult()
    {
        var repo = new FakeHangarRepo();
        var handler = new GetMyHangarHandler(repo);

        var result = await handler.HandleAsync(new GetMyHangarQuery(UserId, null, 1, 25), default);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task HandleAsync_ShipCardIncludesAllFields()
    {
        var repo = new FakeHangarRepo();
        repo.MyHangarCards.Add(new ShipCard(Guid.NewGuid(), "Gladius", "Aegis", "https://img.test/g.jpg", 10m, "1"));
        var handler = new GetMyHangarHandler(repo);

        var result = await handler.HandleAsync(new GetMyHangarQuery(UserId, null, 1, 25), default);

        var card = result.Items.Single();
        card.Name.Should().Be("Gladius");
        card.CompanyName.Should().Be("Aegis");
        card.UrlPhoto.Should().Be("https://img.test/g.jpg");
        card.Scu.Should().Be(10m);
        card.Crew.Should().Be("1");
    }
}
