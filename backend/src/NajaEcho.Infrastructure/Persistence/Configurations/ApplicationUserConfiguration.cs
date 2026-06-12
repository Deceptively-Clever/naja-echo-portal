using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NajaEcho.Infrastructure.Identity;

namespace NajaEcho.Infrastructure.Persistence.Configurations;

public sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(u => u.DisplayName)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(u => u.DiscordUsername)
            .HasMaxLength(32)
            .IsRequired();
    }
}
