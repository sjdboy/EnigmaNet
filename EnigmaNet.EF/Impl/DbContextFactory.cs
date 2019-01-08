using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace EnigmaNet.EF.Impl
{
    public class DbContextFactory : IDbContextFactory
    {
        #region private

        DbContext Create(bool useLazyLoading)
        {
            Type dbContextType = DbContextType;// Type.GetType(this.DbContextTypeName);

            if (string.IsNullOrEmpty(ConnectionString))
            {
                throw new ArgumentNullException(nameof(ConnectionString));
            }

            var args = new object[] { ConnectionString, useLazyLoading };

            var dbContext = (DbContext)Activator.CreateInstance(dbContextType, args);

            return dbContext;
        }

        #endregion

        #region property

        public Type DbContextType { get; set; }
        public string ConnectionString { get; set; }

        #endregion

        #region ctor

        public DbContextFactory() { }

        #endregion

        #region IDbContextFactory

        public DbContext Create()
        {
            return Create(false);
        }

        public DbContext CreateWithLazyLoading()
        {
            return Create(true);
        }

        #endregion
    }
}
