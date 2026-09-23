using Hekutenantcoreapp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hekutenantcoreapp.Infrastructure.Data.Configurations;

public class LoggingRetentionSettingsConfiguration : IEntityTypeConfiguration<LoggingRetentionSettings>
{
    public void Configure(EntityTypeBuilder<LoggingRetentionSettings> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.RetentionDays).HasDefaultValue(30);
    }
}
