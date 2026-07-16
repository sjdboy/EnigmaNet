using Microsoft.EntityFrameworkCore;

namespace EnigmaNet.ChinaDivisions.Pg;

public class DivisionDbContext : DbContext
{
    public DivisionDbContext(DbContextOptions<DivisionDbContext> options) : base(options)
    {
    }

    public DbSet<AreaEntity> AreaCodes => Set<AreaEntity>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseLazyLoadingProxies()
            .ConfigureWarnings(warnings =>
            {
                warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.DetachedLazyLoadingWarning);
            });

        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new AreaEntityMapping());
    }
}