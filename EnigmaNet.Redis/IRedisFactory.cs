using System;

using StackExchange.Redis;

namespace EnigmaNet.Redis
{
    public interface IRedisFactory
    {
        IDatabase GetDatabase();
    }
}
