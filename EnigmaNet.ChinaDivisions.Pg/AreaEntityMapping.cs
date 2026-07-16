using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnigmaNet.ChinaDivisions.Pg;

public class AreaEntityMapping : IEntityTypeConfiguration<AreaEntity>
{
    public void Configure(EntityTypeBuilder<AreaEntity> builder)
    {
        //数据源：https://github.com/adyliu/china_area

        builder.ToTable("area_code").HasKey(m => m.Code);

        builder.Property(m => m.Code)
            .HasColumnName("code")
            .ValueGeneratedNever();

        builder.Property(m => m.Name)
            .HasColumnName("name")
            .HasMaxLength(128)
            .IsRequired(true)
            .HasComment("名称");

        builder.Property(m => m.Level)
            .HasColumnName("level")
            .HasConversion<int>()
            .HasColumnType("smallint")
            .HasComment("级别1-5,省市县镇村");

        builder.Property(m => m.ParentCode)
            .HasColumnName("pcode")
            .HasComment("父级区划代码");

        builder.Property(m => m.Category)
            .HasColumnName("category")
            .HasComment("城乡分类");

        builder.HasIndex(m => m.Name)
            .HasDatabaseName("idx_area_code_name");

        builder.HasIndex(m => m.Level)
            .HasDatabaseName("idx_area_code_level");

        builder.HasIndex(m => m.ParentCode)
            .HasDatabaseName("idx_area_code_pcode");

        builder.Property(m => m.Code).HasComment("区划代码");
    }
}
