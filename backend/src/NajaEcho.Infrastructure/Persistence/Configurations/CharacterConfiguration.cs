using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NajaEcho.Domain.Characters;
using NajaEcho.Infrastructure.Identity;

namespace NajaEcho.Infrastructure.Persistence.Configurations;

public sealed class CharacterConfiguration : IEntityTypeConfiguration<Character>
{
    public void Configure(EntityTypeBuilder<Character> builder)
    {
        builder.ToTable("characters");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id).HasColumnName("id");
        builder.Property(c => c.OwnerUserId).HasColumnName("owner_user_id").IsRequired();
        builder.Property(c => c.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(c => c.Handle).HasColumnName("handle").HasMaxLength(100).IsRequired();
        builder.Property(c => c.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasIndex(c => c.OwnerUserId)
            .HasDatabaseName("ix_characters_owner_user_id");

        // The migration is manually edited to use lower(handle) for the functional unique index.
        builder.HasIndex(c => c.Handle)
            .IsUnique()
            .HasDatabaseName("ux_characters_handle_lower");

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(c => c.OwnerUserId)
            .HasConstraintName("fk_characters_owner_user_id")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
