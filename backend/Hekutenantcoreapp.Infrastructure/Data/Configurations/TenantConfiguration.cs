using Hekutenantcoreapp.Domain.Common;
using Hekutenantcoreapp.Domain.Entities;
using Hekutenantcoreapp.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hekutenantcoreapp.Infrastructure.Data.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.HasKey(t => t.Id);
        builder.HasOne(t => t.Country)
            .WithMany()
            .HasForeignKey(t => t.CountryId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(t => t.State)
            .WithMany()
            .HasForeignKey(t => t.StateId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(t => t.City)
            .WithMany()
            .HasForeignKey(t => t.CityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(t => t.TenantType).HasConversion<string>();
        builder.HasOne<EnumLookup<TenantType>>()
            .WithMany()
            .HasForeignKey(t => t.TenantType)
            .HasPrincipalKey(tt => tt.Code)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
