using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnigmaNet.EF.Utils.EventManager
{
    public class EventRecordMapping : IEntityTypeConfiguration<EventRecord>
    {
        public void Configure(EntityTypeBuilder<EventRecord> builder)
        {
            builder.ToTable("_EventRecord").HasKey(m => m.Id);
            builder.Property(m => m.RowVer).IsRowVersion();
            builder.Property(m => m.Id).ValueGeneratedNever();
            builder.Property(m => m.EventObjectJson);
            builder.Property(m => m.Processed);
            builder.Property(m => m.DateTime);
        }
    }
}
