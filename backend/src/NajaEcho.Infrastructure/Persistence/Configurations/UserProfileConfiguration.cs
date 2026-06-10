using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NajaEcho.Domain.Users;

namespace NajaEcho.Infrastructure.Persistence.Configurations;

public sealed class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("user_profiles");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).ValueGeneratedNever();

        builder.Property(u => u.DiscordUserId)
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(u => u.DiscordUserId)
            .IsUnique()
            .HasDatabaseName("uq_user_profiles_discord_user_id");

        builder.Property(u => u.DisplayName)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(u => u.AvatarRef)
            .HasMaxLength(64);

        builder.Property(u => u.Email)
            .HasMaxLength(254);

        builder.Property(u => u.CreatedAtUtc).IsRequired();
        builder.Property(u => u.LastLoginAtUtc).IsRequired();
        builder.Property(u => u.LastUpdatedAtUtc).IsRequired();
    }
}
