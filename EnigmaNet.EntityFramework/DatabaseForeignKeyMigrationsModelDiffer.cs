using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Internal;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Update.Internal;

namespace EnigmaNet.EntityFramework;

#pragma warning disable EF1001
public class DatabaseForeignKeyMigrationsModelDiffer : MigrationsModelDiffer
{
    public DatabaseForeignKeyMigrationsModelDiffer(
        IRelationalTypeMappingSource typeMappingSource,
        IMigrationsAnnotationProvider migrationsAnnotationProvider,
#if NET10_0_OR_GREATER
    IRelationalAnnotationProvider relationalAnnotationProvider,
#endif
        IRowIdentityMapFactory rowIdentityMapFactory,
        CommandBatchPreparerDependencies commandBatchPreparerDependencies)
#if NET10_0_OR_GREATER
    : base(typeMappingSource, migrationsAnnotationProvider, relationalAnnotationProvider, rowIdentityMapFactory, commandBatchPreparerDependencies)
#else
        : base(typeMappingSource, migrationsAnnotationProvider, rowIdentityMapFactory, commandBatchPreparerDependencies)
#endif
    {
    }

    protected override IEnumerable<MigrationOperation> Add(IForeignKeyConstraint target, DiffContext diffContext)
    {
        if (!ShouldGenerateDatabaseForeignKey(target))
        {
            yield break;
        }

        foreach (var operation in base.Add(target, diffContext))
        {
            yield return operation;
        }
    }

    protected override IEnumerable<MigrationOperation> Remove(IForeignKeyConstraint source, DiffContext diffContext)
    {
        if (!ShouldGenerateDatabaseForeignKey(source))
        {
            yield break;
        }

        foreach (var operation in base.Remove(source, diffContext))
        {
            yield return operation;
        }
    }

    protected override IEnumerable<MigrationOperation> Diff(IForeignKeyConstraint source, IForeignKeyConstraint target, DiffContext diffContext)
    {
        var sourceShouldGenerate = ShouldGenerateDatabaseForeignKey(source);
        var targetShouldGenerate = ShouldGenerateDatabaseForeignKey(target);

        if (sourceShouldGenerate == targetShouldGenerate)
        {
            yield break;
        }

        if (sourceShouldGenerate)
        {
            foreach (var operation in base.Remove(source, diffContext))
            {
                yield return operation;
            }
        }

        if (targetShouldGenerate)
        {
            foreach (var operation in base.Add(target, diffContext))
            {
                yield return operation;
            }
        }
    }

    private static bool ShouldGenerateDatabaseForeignKey(IForeignKeyConstraint foreignKey)
        => foreignKey.MappedForeignKeys.Any(mappedForeignKey => mappedForeignKey.FindAnnotation(DatabaseForeignKeyAnnotationNames.KeepDatabaseForeignKey)?.Value as bool? == true);
}
#pragma warning restore EF1001