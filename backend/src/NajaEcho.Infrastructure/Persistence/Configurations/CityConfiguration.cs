using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NajaEcho.Domain.Locations;

namespace NajaEcho.Infrastructure.Persistence.Configurations;

public sealed class CityConfiguration : IEntityTypeConfiguration<City>
{
    public void Configure(EntityTypeBuilder<City> builder)
    {
        builder.ToTable("cities", schema: "sc");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id).HasColumnName("id");
        builder.Property(c => c.UexId).HasColumnName("uex_id").IsRequired();
        builder.Property(c => c.StarSystemId).HasColumnName("star_system_id").IsRequired();
        builder.Property(c => c.Name).HasColumnName("name").HasMaxLength(256).IsRequired();
        builder.Property(c => c.Code).HasColumnName("code").HasMaxLength(64);
        builder.Property(c => c.IsAvailable).HasColumnName("is_available").IsRequired();
        builder.Property(c => c.IsAvailableLive).HasColumnName("is_available_live").IsRequired();
        builder.Property(c => c.IsVisible).HasColumnName("is_visible").IsRequired();

        builder.Property(c => c.Status)
            .HasColumnName("status")
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(c => c.RawData)
            .HasColumnName("raw_data")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(c => c.ImportedAt).HasColumnName("imported_at").IsRequired();
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at").IsRequired();
        builder.Property(c => c.SoftDeletedAt).HasColumnName("soft_deleted_at");

        builder.HasOne(c => c.StarSystem)
            .WithMany()
            .HasForeignKey(c => c.StarSystemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => c.UexId).IsUnique().HasDatabaseName("ix_cities_uex_id");
        builder.HasIndex(c => c.Status).HasDatabaseName("ix_cities_status");
        builder.HasIndex(c => c.StarSystemId).HasDatabaseName("ix_cities_star_system_id");
        builder.HasIndex(c => new { c.IsAvailable, c.IsVisible, c.Name })
            .HasDatabaseName("ix_cities_avail_visible_name");
    }
}
