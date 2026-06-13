using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NajaEcho.Domain.Ships;

namespace NajaEcho.Infrastructure.Persistence.Configurations;

public sealed class ShipConfiguration : IEntityTypeConfiguration<Ship>
{
    public void Configure(EntityTypeBuilder<Ship> builder)
    {
        builder.ToTable("ships", schema: "sc");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id).HasColumnName("id");
        builder.Property(s => s.UexId).HasColumnName("uex_id").IsRequired();
        builder.Property(s => s.Uuid).HasColumnName("uuid").HasMaxLength(128);
        builder.Property(s => s.Name).HasColumnName("name").HasMaxLength(256).IsRequired();
        builder.Property(s => s.NameFull).HasColumnName("name_full").HasMaxLength(512);
        builder.Property(s => s.CompanyName).HasColumnName("company_name").HasMaxLength(256);

        builder.Property(s => s.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(s => s.RawData)
            .HasColumnName("raw_data")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(s => s.ImportedAt).HasColumnName("imported_at").IsRequired();
        builder.Property(s => s.UpdatedAt).HasColumnName("updated_at").IsRequired();
        builder.Property(s => s.SoftDeletedAt).HasColumnName("soft_deleted_at");

        builder.HasIndex(s => s.UexId).IsUnique().HasDatabaseName("ix_ships_uex_id");
        builder.HasIndex(s => s.Status).HasDatabaseName("ix_ships_status");
    }
}
