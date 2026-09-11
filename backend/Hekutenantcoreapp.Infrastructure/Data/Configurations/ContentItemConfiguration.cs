using Hekutenantcoreapp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hekutenantcoreapp.Infrastructure.Data.Configurations;

// TenantId's index/query-filter/insert-stamp all come for free from ITenantScoped (see
// HekutenantcoreappDbContext.OnModelCreating) — only the content-specific shape is configured
// here.
public class ContentItemConfiguration : IEntityTypeConfiguration<ContentItem>
{
    public void Configure(EntityTypeBuilder<ContentItem> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Title).HasMaxLength(200);
        builder.Property(c => c.Description).HasMaxLength(1000);
        builder.Property(c => c.Body).HasColumnType("text");
        builder.Property(c => c.Url).HasMaxLength(2048);

        builder.HasIndex(c => new { c.OwnerType, c.OwnerId, c.DisplayOrder });
        builder.HasIndex(c => new { c.OwnerType, c.OwnerId, c.Slot })
            .IsUnique()
            .HasFilter("slot IS NOT NULL");

        builder.HasOne(c => c.StoredFile)
            .WithMany()
            .HasForeignKey(c => c.StoredFileId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
