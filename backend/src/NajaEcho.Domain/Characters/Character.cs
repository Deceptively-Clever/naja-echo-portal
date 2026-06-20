namespace NajaEcho.Domain.Characters;

public sealed class Character
{
    public Guid Id { get; set; }
    public Guid OwnerUserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Handle { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}
