using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NajaEcho.Domain.Characters;
using NajaEcho.Infrastructure.Identity;

namespace NajaEcho.Infrastructure.Persistence.Configurations;

public sealed class PendingCharacterRegistrationConfiguration : IEntityTypeConfiguration<PendingCharacterRegistration>
{
    public void Configure(EntityTypeBuilder<PendingCharacterRegistration> builder)
    {
        builder.ToTable("pending_character_registrations");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id).HasColumnName("id");
        builder.Property(p => p.OwnerUserId).HasColumnName("owner_user_id").IsRequired();
        builder.Property(p => p.Token).HasColumnName("token").HasMaxLength(64).IsRequired();
        builder.Property(p => p.ExpiresAt).HasColumnName("expires_at").IsRequired();
        builder.Property(p => p.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasIndex(p => p.OwnerUserId)
            .IsUnique()
            .HasDatabaseName("ux_pending_character_registrations_owner_user_id");

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(p => p.OwnerUserId)
            .HasConstraintName("fk_pending_character_registrations_owner_user_id")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
