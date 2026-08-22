using Hekutenantcoreapp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hekutenantcoreapp.Infrastructure.Data.Configurations;

public class LocalizedTextConfiguration : IEntityTypeConfiguration<LocalizedText>
{
    public void Configure(EntityTypeBuilder<LocalizedText> builder)
    {
        builder.HasKey(l => l.Id);

        builder.HasIndex(l => new { l.EntityName, l.EntityId, l.FieldName, l.LanguageCode }).IsUnique();
    }
}
