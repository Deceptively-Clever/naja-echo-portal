namespace NajaEcho.Domain.Hangar;

public sealed class HangarEntry
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid ShipId { get; set; }
    public DateTimeOffset AddedAt { get; set; }
}
