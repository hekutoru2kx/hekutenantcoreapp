using Hekutenantcoreapp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hekutenantcoreapp.Infrastructure.Data.Configurations;

public class LoggingCategorySettingsConfiguration : IEntityTypeConfiguration<LoggingCategorySettings>
{
    public void Configure(EntityTypeBuilder<LoggingCategorySettings> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Category).HasConversion<string>();
        builder.Property(s => s.MinimumLevel).HasConversion<string>();
        builder.HasIndex(s => s.Category).IsUnique();
    }
}
