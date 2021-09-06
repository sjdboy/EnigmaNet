using System;

using Nest;

namespace EnigmaNet.ElasticSearch
{
    public interface IEsClientFactory
    {
        ElasticClient CreateClient();
    }
}
