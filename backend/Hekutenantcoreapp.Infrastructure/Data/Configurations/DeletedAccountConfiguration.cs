using Hekutenantcoreapp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hekutenantcoreapp.Infrastructure.Data.Configurations;

public class DeletedAccountConfiguration : IEntityTypeConfiguration<DeletedAccount>
{
    public void Configure(EntityTypeBuilder<DeletedAccount> builder)
    {
        builder.HasKey(d => d.Id);
        builder.HasIndex(d => d.NormalizedEmail);
    }
}
