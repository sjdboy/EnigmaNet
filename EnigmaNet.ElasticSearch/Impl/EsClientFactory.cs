using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elasticsearch.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;

namespace EnigmaNet.ElasticSearch.Impl
{
    public class EsClientFactory : IEsClientFactory
    {
        ILogger _logger;
        Options.EsOptions _options;
        ConnectionSettings _settings;
        object _settingsLocker = new object();

        static string ToSnakeCase(string s)
        {
            var builder = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                var c = s[i];
                if (char.IsUpper(c))
                {
                    if (i == 0)
                    {
                        builder.Append(char.ToLowerInvariant(c));
                    }
                    else if (char.IsUpper(s[i - 1]))
                    {
                        builder.Append(char.ToLowerInvariant(c));
                    }
                    else
                    {
                        builder.Append("_");
                        builder.Append(char.ToLowerInvariant(c));
                    }
                }
                else
                {
                    builder.Append(c);
                }
            }

            return builder.ToString();
        }

        public EsClientFactory(ILoggerFactory logger, IOptionsMonitor<Options.EsOptions> options)
        {
            _logger = logger.CreateLogger<EsClientFactory>();

            _options = options.CurrentValue;

            options.OnChange(newValue =>
            {
                _options = newValue;

                lock (_settingsLocker)
                {
                    _settings = null;
                }
            });
        }

        public ElasticClient CreateClient()
        {
            if (_settings == null)
            {
                lock (_settingsLocker)
                {
                    if (_settings == null)
                    {
                        _logger.LogInformation($"create conn,url:{string.Join(",", _options.Urls)} username:{_options.UserName}");

                        var pool = new StaticConnectionPool(_options.Urls.Select(m => new Uri(m)));

                        _settings = new ConnectionSettings(pool, (a, b) =>
                        {
                            var setting = new Nest.JsonNetSerializer.JsonNetSerializer(a, b, () =>
                            {
                                return new Newtonsoft.Json.JsonSerializerSettings
                                {
                                    DateTimeZoneHandling= Newtonsoft.Json.DateTimeZoneHandling.Local,
                                };
                            });

                            return setting;
                        });

                        _settings.BasicAuthentication(_options.UserName, _options.Password);
                        _settings.DefaultFieldNameInferrer(n => ToSnakeCase(n));
                        if (!string.IsNullOrEmpty(_options.DefaultIndexName))
                        {
                            _settings.DefaultIndex(_options.DefaultIndexName);
                        }
                    }
                }
            }

            return new ElasticClient(_settings);
        }
    }
}
