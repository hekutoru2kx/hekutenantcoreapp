using Hekutenantcoreapp.Domain.Entities;
using Hekutenantcoreapp.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hekutenantcoreapp.Infrastructure.Data.Configurations;

public class DatabaseKeepAliveSettingsConfiguration : IEntityTypeConfiguration<DatabaseKeepAliveSettings>
{
    public void Configure(EntityTypeBuilder<DatabaseKeepAliveSettings> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.IntervalUnit).HasConversion<string>().HasDefaultValue(KeepAliveIntervalUnit.Minutes);
    }
}
