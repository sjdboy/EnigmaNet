using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnigmaNet.IdGenerators.Impl
{
    public class SnowflakeIdGenerator : ILongIdGenerator
    {
        Snowflake.IdWorker _idWorker;

        public SnowflakeIdGenerator(long workerId, long datacenterId)
        {
            _idWorker = new Snowflake.IdWorker(workerId, datacenterId);
        }

        public Task<long> GenerateIdAsync()
        {
            var id = _idWorker.NextId();

            return Task.FromResult(id);
        }
    }
}
