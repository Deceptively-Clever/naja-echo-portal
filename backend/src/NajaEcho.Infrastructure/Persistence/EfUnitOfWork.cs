using NajaEcho.Application.Abstractions;

namespace NajaEcho.Infrastructure.Persistence;

public sealed class EfUnitOfWork(AppDbContext db) : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
