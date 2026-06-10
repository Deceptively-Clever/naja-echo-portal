using Microsoft.EntityFrameworkCore;
using NajaEcho.Domain.Users;
using NajaEcho.Infrastructure.Persistence.Configurations;

namespace NajaEcho.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new UserProfileConfiguration());
    }
}
