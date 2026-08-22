using Hekutenantcoreapp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hekutenantcoreapp.Infrastructure.Data.Configurations;

public class MultiTenantSettingsConfiguration : IEntityTypeConfiguration<MultiTenantSettings>
{
    public void Configure(EntityTypeBuilder<MultiTenantSettings> builder)
    {
        builder.HasKey(s => s.Id);

        builder.HasOne(s => s.DefaultTenant)
            .WithMany()
            .HasForeignKey(s => s.DefaultTenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
