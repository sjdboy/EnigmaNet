using Microsoft.Extensions.Logging;

namespace EnigmaNet.EF.Impl
{
    public class DbQueryContextFactory : IDbQueryContextFactory
    {
        public IDbContextFactory DbContextFactory { get; set; }
        public ILoggerFactory LoggerFactory { get; set; }

        public IDbQueryContext Create()
        {
            return new DbQueryContext
            {
                DbContextFactory = DbContextFactory,
                LoggerFactory = LoggerFactory,
            };
        }

        public IDbQueryContext CreateWithLazyLoading()
        {
            return new DbQueryContext
            {
                DbContextFactory = DbContextFactory,
                LoggerFactory = LoggerFactory,
                UseLazyLoading = true
            };
        }
    }
}
