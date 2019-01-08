using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnigmaNet.EF.Utils.LongIds
{
    class IdRecordMapping : IEntityTypeConfiguration<IdRecord>
    {
        public void Configure(EntityTypeBuilder<IdRecord> builder)
        {
            builder.ToTable("IdRecord").HasKey(m => m.Key);
            builder.Property(m => m.RowVer).IsRowVersion();
            builder.Property(m => m.Key).HasMaxLength(50).ValueGeneratedNever();
            builder.Property(m => m.Value);
        }
    }
}
