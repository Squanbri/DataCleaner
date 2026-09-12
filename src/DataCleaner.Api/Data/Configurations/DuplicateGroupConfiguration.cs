using DataCleaner.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataCleaner.Api.Data.Configurations;

public class DuplicateGroupConfiguration : IEntityTypeConfiguration<DuplicateGroup>
{
    public void Configure(EntityTypeBuilder<DuplicateGroup> builder)
    {
        builder.Property(x => x.MatchReason).HasMaxLength(64).IsRequired();
    }
}
