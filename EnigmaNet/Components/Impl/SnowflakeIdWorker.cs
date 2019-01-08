using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EnigmaNet.Components.Impl
{
    public class SnowflakeIdWorker : ILongIdProduced
    {
        Snowflake.IdWorker _idWorker;

        public SnowflakeIdWorker(long workerId, long datacenterId)
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
