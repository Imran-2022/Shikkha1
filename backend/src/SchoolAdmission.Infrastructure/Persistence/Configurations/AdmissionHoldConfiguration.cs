using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolAdmission.Domain.Entities;
using SchoolAdmission.Domain.Enums;

namespace SchoolAdmission.Infrastructure.Persistence.Configurations;

public sealed class AdmissionHoldConfiguration : IEntityTypeConfiguration<AdmissionHold>
{
    public void Configure(EntityTypeBuilder<AdmissionHold> builder)
    {
        builder.ToTable("admission_holds");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.Id)
            .HasColumnName("id")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(h => h.StudentId)
            .HasColumnName("student_id")
            .HasMaxLength(17)
            .IsRequired();

        builder.Property(h => h.ClassId)
            .HasColumnName("class_id")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(h => h.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(h => h.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(h => h.ExpiresAt)
            .HasColumnName("expires_at")
            .IsRequired();

        builder.Property(h => h.ConfirmedAt)
            .HasColumnName("confirmed_at");

        builder.Property(h => h.CancelledAt)
            .HasColumnName("cancelled_at");

        // Composite index: fast lookup for active holds per student per class
        builder.HasIndex(h => new { h.StudentId, h.ClassId })
            .HasDatabaseName("ix_holds_student_class");

        // Index for expiry background processing
        builder.HasIndex(h => new { h.Status, h.ExpiresAt })
            .HasDatabaseName("ix_holds_status_expires");

        // Ignore derived property
        builder.Ignore(h => h.IsExpired);
    }
}
