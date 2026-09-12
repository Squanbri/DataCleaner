using DataCleaner.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataCleaner.Api.Data.Configurations;

public class CustomerRecordConfiguration : IEntityTypeConfiguration<CustomerRecord>
{
    public void Configure(EntityTypeBuilder<CustomerRecord> builder)
    {
        builder.Property(x => x.ExternalId).HasMaxLength(128);

        builder.Property(x => x.RawFullName).HasMaxLength(512);
        builder.Property(x => x.RawPhone).HasMaxLength(64);
        builder.Property(x => x.RawEmail).HasMaxLength(320);
        builder.Property(x => x.RawBirthDate).HasMaxLength(64);
        builder.Property(x => x.RawCity).HasMaxLength(256);

        builder.Property(x => x.LastName).HasMaxLength(128);
        builder.Property(x => x.FirstName).HasMaxLength(128);
        builder.Property(x => x.MiddleName).HasMaxLength(128);
        builder.Property(x => x.Phone).HasMaxLength(16);
        builder.Property(x => x.Email).HasMaxLength(320);
        builder.Property(x => x.City).HasMaxLength(256);

        builder.HasIndex(x => new { x.ImportBatchId, x.Phone });
        builder.HasIndex(x => new { x.ImportBatchId, x.Email });

        builder.HasOne(x => x.DuplicateGroup)
            .WithMany(x => x.Records)
            .HasForeignKey(x => x.DuplicateGroupId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
