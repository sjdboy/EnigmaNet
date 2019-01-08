using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EnigmaNet.EF.Impl
{
    public class DbQueryContext : IDbQueryContext, IDisposable
    {
        ILogger _log;
        ILogger Log
        {
            get
            {
                if (_log == null)
                {
                    _log = LoggerFactory?.CreateLogger<DbQueryContext>();
                }
                return _log;
            }
        }

        DbContext _dbContext;
        DbContext DbContext
        {
            get
            {
                if (_dbContext == null)
                {
                    if (UseLazyLoading)
                    {
                        _dbContext = DbContextFactory.CreateWithLazyLoading();
                    }
                    else
                    {
                        _dbContext = DbContextFactory.Create();
                    }
                }
                return _dbContext;
            }
        }

        public IDbContextFactory DbContextFactory { get; set; }

        public ILoggerFactory LoggerFactory { get; set; }

        public bool UseLazyLoading { get; set; }

        public IQueryable<T> Query<T>() where T : class
        {
            return DbContext.Set<T>()
                //.AsNoTracking()
                .AsQueryable()
                ;
        }

        public void Dispose()
        {
            if (DbContext != null)
            {
                //if (Log?.IsEnabled(LogLevel.Debug) == true)
                //{
                //    Log?.LogDebug("dispose,hash code:{0}", this.GetHashCode());
                //}
                DbContext.Dispose();
            }
        }
    }
}
