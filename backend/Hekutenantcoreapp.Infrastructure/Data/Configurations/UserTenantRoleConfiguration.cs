using Hekutenantcoreapp.Domain.Entities;
using Hekutenantcoreapp.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hekutenantcoreapp.Infrastructure.Data.Configurations;

public class UserTenantRoleConfiguration : IEntityTypeConfiguration<UserTenantRole>
{
    public void Configure(EntityTypeBuilder<UserTenantRole> builder)
    {
        builder.HasKey(r => r.Id);
        // Filtered so the same role can be granted again after being revoked (rehire,
        // dual-hat employee deactivation, etc.) — at most one OPEN grant per role at a
        // time, but any number of closed ones for history.
        builder.HasIndex(r => new { r.UserId, r.TenantId, r.RoleId })
            .IsUnique()
            .HasFilter("revoked_at IS NULL");
        builder.HasOne(r => r.Tenant)
            .WithMany()
            .HasForeignKey(r => r.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<IdentityRole>()
            .WithMany()
            .HasForeignKey(r => r.RoleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
