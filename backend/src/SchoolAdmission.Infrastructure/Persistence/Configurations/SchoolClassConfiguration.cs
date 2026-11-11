using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolAdmission.Domain.Entities;

namespace SchoolAdmission.Infrastructure.Persistence.Configurations;

public sealed class SchoolClassConfiguration : IEntityTypeConfiguration<SchoolClass>
{
    public void Configure(EntityTypeBuilder<SchoolClass> builder)
    {
        builder.ToTable("classes");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasColumnName("id")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(c => c.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(c => c.Name).IsUnique();

        builder.Property(c => c.TotalSeats)
            .HasColumnName("total_seats")
            .IsRequired();

        builder.Property(c => c.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasMany(c => c.Holds)
            .WithOne(h => h.SchoolClass)
            .HasForeignKey(h => h.ClassId)
            .OnDelete(DeleteBehavior.Cascade);

        // Ignore computed properties — derived from holds
        builder.Ignore(c => c.HeldSeats);
        builder.Ignore(c => c.ConfirmedSeats);
        builder.Ignore(c => c.AvailableSeats);
    }
}
