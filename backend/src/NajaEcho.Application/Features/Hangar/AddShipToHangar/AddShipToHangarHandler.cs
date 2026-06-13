using NajaEcho.Application.Abstractions;
using NajaEcho.Application.Features.Hangar.GetMyHangar;
using NajaEcho.Domain.Ships;

namespace NajaEcho.Application.Features.Hangar.AddShipToHangar;

public sealed class AddShipToHangarHandler(IHangarRepository repository, IShipRepository shipRepository)
{
    public async Task<ShipCard> HandleAsync(AddShipToHangarCommand command, CancellationToken ct)
    {
        var ship = await shipRepository.GetByIdAsync(command.ShipId, ct);
        if (ship is null || ship.Status != ShipStatus.Active)
            throw new ShipNotFoundException(command.ShipId);

        // Repository pre-checks via alreadyOwned flag; DB unique constraint is the final guard
        var searchResult = await repository.SearchCatalogAsync(command.UserId, null, 1, 9999, ct);
        if (searchResult.Items.Any(r => r.ShipId == command.ShipId && r.AlreadyOwned))
            throw new ShipAlreadyOwnedException(command.ShipId);

        // Repository.AddAsync throws ShipAlreadyOwnedException on unique constraint violation
        return await repository.AddAsync(command.UserId, command.ShipId, ct);
    }
}
