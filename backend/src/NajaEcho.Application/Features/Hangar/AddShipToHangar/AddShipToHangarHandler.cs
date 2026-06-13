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

        // Cheap pre-check; DB unique constraint (handled in AddAsync) is the final guard against races
        if (await repository.ExistsAsync(command.UserId, command.ShipId, ct))
            throw new ShipAlreadyOwnedException(command.ShipId);

        // Repository.AddAsync throws ShipAlreadyOwnedException on unique constraint violation
        return await repository.AddAsync(command.UserId, command.ShipId, ct);
    }
}
