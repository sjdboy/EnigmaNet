using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace EnigmaNet.EF.Utils.EventManager
{
    public class ConsumeEventRecordMapping : IEntityTypeConfiguration<ConsumeEventRecord>
    {
        public void Configure(EntityTypeBuilder<ConsumeEventRecord> builder)
        {
            builder.ToTable("_ConsumeEventRecord").HasKey(m => m.Id);
            builder.Property(m => m.RowVer).IsRowVersion();
            builder.Property(m => m.Id).ValueGeneratedNever();
            builder.Property(m => m.EventId).HasMaxLength(50).IsRequired();
            builder.Property(m => m.ConsumerId).HasMaxLength(100).IsRequired();
            builder.Property(m => m.DateTime);

            builder.HasIndex(m => new { m.EventId, m.ConsumerId }).IsUnique();
        }
    }
}
