using Hekutenantcoreapp.Domain.Common;
using Hekutenantcoreapp.Domain.Entities;
using Hekutenantcoreapp.Domain.Enums;
using Hekutenantcoreapp.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hekutenantcoreapp.Infrastructure.Data.Configurations;

public class TenantMembershipConfiguration : IEntityTypeConfiguration<TenantMembership>
{
    public void Configure(EntityTypeBuilder<TenantMembership> builder)
    {
        builder.HasKey(m => m.Id);
        builder.HasIndex(m => new { m.UserId, m.TenantId }).IsUnique();
        builder.HasOne(m => m.Tenant)
            .WithMany()
            .HasForeignKey(m => m.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(m => m.Status).HasConversion<string>();
        builder.HasOne<EnumLookup<TenantMembershipStatus>>()
            .WithMany()
            .HasForeignKey(m => m.Status)
            .HasPrincipalKey(s => s.Code)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
