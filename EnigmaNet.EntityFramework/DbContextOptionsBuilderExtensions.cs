using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;

namespace EnigmaNet.EntityFramework;

public static class DbContextOptionsBuilderExtensions
{
    public static DbContextOptionsBuilder UseExplicitDatabaseForeignKeys(this DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.ReplaceService<IMigrationsModelDiffer, DatabaseForeignKeyMigrationsModelDiffer>();
}