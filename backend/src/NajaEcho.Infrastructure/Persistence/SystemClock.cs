using NajaEcho.Application.Abstractions;

namespace NajaEcho.Infrastructure.Persistence;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
