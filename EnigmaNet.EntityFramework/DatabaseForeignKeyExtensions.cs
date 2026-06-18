using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnigmaNet.EntityFramework;

public static class DatabaseForeignKeyExtensions
{
    public static ReferenceCollectionBuilder KeepDatabaseForeignKey(this ReferenceCollectionBuilder builder)
    {
        builder.Metadata.SetAnnotation(DatabaseForeignKeyAnnotationNames.KeepDatabaseForeignKey, true);
        return builder;
    }

    /// <summary>
    /// 为导航属性设置数据库外键约束，EFCore默认会删除外键约束，使用此方法可以保留外键约束
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TRelatedEntity"></typeparam>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static ReferenceCollectionBuilder<TEntity, TRelatedEntity> KeepDatabaseForeignKey<TEntity, TRelatedEntity>(this ReferenceCollectionBuilder<TEntity, TRelatedEntity> builder)
        where TEntity : class
        where TRelatedEntity : class
    {
        builder.Metadata.SetAnnotation(DatabaseForeignKeyAnnotationNames.KeepDatabaseForeignKey, true);
        return builder;
    }

    public static ReferenceReferenceBuilder KeepDatabaseForeignKey(this ReferenceReferenceBuilder builder)
    {
        builder.Metadata.SetAnnotation(DatabaseForeignKeyAnnotationNames.KeepDatabaseForeignKey, true);
        return builder;
    }

    public static ReferenceReferenceBuilder<TEntity, TRelatedEntity> KeepDatabaseForeignKey<TEntity, TRelatedEntity>(this ReferenceReferenceBuilder<TEntity, TRelatedEntity> builder)
        where TEntity : class
        where TRelatedEntity : class
    {
        builder.Metadata.SetAnnotation(DatabaseForeignKeyAnnotationNames.KeepDatabaseForeignKey, true);
        return builder;
    }
}