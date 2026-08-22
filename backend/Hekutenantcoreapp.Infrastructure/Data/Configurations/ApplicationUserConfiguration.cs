using Hekutenantcoreapp.Domain.Entities;
using Hekutenantcoreapp.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hekutenantcoreapp.Infrastructure.Data.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        // Shadow relation (no Tenant navigation on ApplicationUser — Identity models stay
        // free of Domain navigation properties elsewhere in this codebase too).
        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(u => u.DefaultTenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
