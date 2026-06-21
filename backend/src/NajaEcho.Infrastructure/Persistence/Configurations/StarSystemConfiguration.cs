using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NajaEcho.Domain.Locations;

namespace NajaEcho.Infrastructure.Persistence.Configurations;

public sealed class StarSystemConfiguration : IEntityTypeConfiguration<StarSystem>
{
    public void Configure(EntityTypeBuilder<StarSystem> builder)
    {
        builder.ToTable("star_systems", schema: "sc");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id).HasColumnName("id");
        builder.Property(s => s.UexId).HasColumnName("uex_id").IsRequired();
        builder.Property(s => s.Name).HasColumnName("name").HasMaxLength(256).IsRequired();
        builder.Property(s => s.Code).HasColumnName("code").HasMaxLength(32);
        builder.Property(s => s.IsAvailable).HasColumnName("is_available").IsRequired();
        builder.Property(s => s.IsVisible).HasColumnName("is_visible").IsRequired();

        builder.Property(s => s.Status)
            .HasColumnName("status")
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(s => s.RawData)
            .HasColumnName("raw_data")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(s => s.ImportedAt).HasColumnName("imported_at").IsRequired();
        builder.Property(s => s.UpdatedAt).HasColumnName("updated_at").IsRequired();
        builder.Property(s => s.SoftDeletedAt).HasColumnName("soft_deleted_at");

        builder.HasIndex(s => s.UexId).IsUnique().HasDatabaseName("ix_star_systems_uex_id");
        builder.HasIndex(s => s.Status).HasDatabaseName("ix_star_systems_status");
    }
}
