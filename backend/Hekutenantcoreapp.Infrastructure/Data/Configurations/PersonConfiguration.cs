using Hekutenantcoreapp.Domain.Common;
using Hekutenantcoreapp.Domain.Entities;
using Hekutenantcoreapp.Domain.Enums;
using Hekutenantcoreapp.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hekutenantcoreapp.Infrastructure.Data.Configurations;

public class PersonConfiguration : IEntityTypeConfiguration<Person>
{
    public void Configure(EntityTypeBuilder<Person> builder)
    {
        builder.HasKey(p => p.Id);
        builder.HasIndex(p => new { p.TenantId, p.DocumentType, p.DocumentId })
            .IsUnique()
            .HasFilter("document_type IS NOT NULL AND document_id IS NOT NULL");
        builder.HasIndex(p => new { p.TenantId, p.UserId })
            .IsUnique()
            .HasFilter("user_id IS NOT NULL");
        builder.HasOne(p => p.Tenant)
            .WithMany()
            .HasForeignKey(p => p.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
        // No nav property back to ApplicationUser: Domain must not reference Infrastructure.Identity.
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(p => p.Country)
            .WithMany()
            .HasForeignKey(p => p.CountryId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.State)
            .WithMany()
            .HasForeignKey(p => p.StateId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.City)
            .WithMany()
            .HasForeignKey(p => p.CityId)
            .OnDelete(DeleteBehavior.Restrict);

        // Stored as the enum's own name (e.g. "Male"), not its numeric value, and FK'd
        // to that enum's lookup table — see ReportableEnumCatalog.
        builder.Property(p => p.Gender).HasConversion<string>();
        builder.HasOne<EnumLookup<Gender>>()
            .WithMany()
            .HasForeignKey(p => p.Gender)
            .HasPrincipalKey(g => g.Code)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(p => p.DocumentType).HasConversion<string>();
        builder.HasOne<EnumLookup<DocumentType>>()
            .WithMany()
            .HasForeignKey(p => p.DocumentType)
            .HasPrincipalKey(d => d.Code)
            .OnDelete(DeleteBehavior.Restrict);

        // Birthday isn't covered by the AuditableEntity auto-index loop in HekutenantcoreappDbContext
        // (that only covers CreatedAt/UpdatedAt), so it needs its own explicit index here.
        builder.HasIndex(p => p.Birthday);
    }
}
