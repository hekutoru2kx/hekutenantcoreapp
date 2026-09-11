using Hekutenantcoreapp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hekutenantcoreapp.Infrastructure.Data.Configurations;

// AppSettings — singleton row (Id = 1), seeded by AppSettingsSeeder. The Content* defaults
// below matter beyond documentation: they're what backfills the row that already exists on
// every established install when this migration's AddColumn runs — without them the ALTER
// TABLE would fall back to the CLR defaults (0 / ""), which for ContentAllowedContentTypes
// means an empty allow-list that rejects every upload.
public class AppSettingsConfiguration : IEntityTypeConfiguration<AppSettings>
{
    public void Configure(EntityTypeBuilder<AppSettings> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.ContentMaxBytes).HasDefaultValue(5 * 1024 * 1024);
        builder.Property(s => s.ContentAllowedContentTypes).HasDefaultValue("image/jpeg,image/png,image/webp");
        builder.Property(s => s.ContentMaxImageDimension).HasDefaultValue(2048);
        builder.Property(s => s.ContentAvatarMaxDimension).HasDefaultValue(512);
    }
}
