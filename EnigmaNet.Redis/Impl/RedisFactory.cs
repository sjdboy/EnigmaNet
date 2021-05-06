using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace EnigmaNet.Redis.Impl
{
    public class RedisFactory : IRedisFactory
    {
        ILogger _logger;
        IConnectionMultiplexer _connectionMultiplexer;
        object _connectionMultiplexerLocker = new object();
        Options.RedisOptions _options;

        IConnectionMultiplexer ConnectionMultiplexer
        {
            get
            {
                if (_connectionMultiplexer == null)
                {
                    lock (_connectionMultiplexerLocker)
                    {
                        if (_connectionMultiplexer == null)
                        {
                            _logger.LogInformation("Init ConnectionMultiplexer");
                            _connectionMultiplexer = StackExchange.Redis.ConnectionMultiplexer.Connect(_options.ConfigurationString);

                            _connectionMultiplexer.ConfigurationChangedBroadcast += ConnectionMultiplexer_ConfigurationChangedBroadcast;
                            _connectionMultiplexer.ErrorMessage += ConnectionMultiplexer_ErrorMessage;
                            _connectionMultiplexer.ConnectionFailed += ConnectionMultiplexer_ConnectionFailed;
                            _connectionMultiplexer.InternalError += ConnectionMultiplexer_InternalError;
                            _connectionMultiplexer.ConnectionRestored += ConnectionMultiplexer_ConnectionRestored;
                            _connectionMultiplexer.ConfigurationChanged += ConnectionMultiplexer_ConfigurationChanged;
                            _connectionMultiplexer.HashSlotMoved += ConnectionMultiplexer_HashSlotMoved;

                        }
                    }
                }

                return _connectionMultiplexer;
            }
        }

        void ConnectionMultiplexer_HashSlotMoved(object sender, HashSlotMovedEventArgs e)
        {
            _logger?.LogInformation($"HashSlotMoved,HashSlot:{e.HashSlot} OldEndPoint:{e.OldEndPoint} NewEndPoint:{e.NewEndPoint}");
        }

        void ConnectionMultiplexer_ConnectionRestored(object sender, ConnectionFailedEventArgs e)
        {
            _logger?.LogError(e.Exception, $"ConnectionRestored,ConnectionType:{e.ConnectionType} EndPoint:{e.EndPoint} FailureType:{e.FailureType} Exception:{e.Exception.Message}");
        }

        void ConnectionMultiplexer_InternalError(object sender, InternalErrorEventArgs e)
        {
            _logger?.LogError(e.Exception, $"InternalError,ConnectionType:{e.ConnectionType} EndPoint:{e.EndPoint} Origin:{e.Origin} Exception:{e.Exception.Message}");
        }

        void ConnectionMultiplexer_ConnectionFailed(object sender, ConnectionFailedEventArgs e)
        {
            _logger?.LogError(e.Exception, $"ConnectionFailed,ConnectionType:{e.ConnectionType} EndPoint:{e.EndPoint} FailureType:{e.FailureType} Exception:{e.Exception.Message}");
        }

        void ConnectionMultiplexer_ErrorMessage(object sender, RedisErrorEventArgs e)
        {
            _logger?.LogError($"ErrorMessage,EndPoint:{e.EndPoint} Message:{e.Message}");
        }

        void ConnectionMultiplexer_ConfigurationChangedBroadcast(object sender, EndPointEventArgs e)
        {
            _logger?.LogInformation($"ConfigurationChangedBroadcast,EndPoint:{e.EndPoint}");
        }

        void ConnectionMultiplexer_ConfigurationChanged(object sender, EndPointEventArgs e)
        {
            _logger?.LogInformation($"ConfigurationChanged,EndPoint:{e.EndPoint}");
        }

        public RedisFactory(ILoggerFactory logger, IOptionsMonitor<Options.RedisOptions> options)
        {
            _logger = logger.CreateLogger<RedisFactory>();

            _options = options.CurrentValue;

            options.OnChange(newValue =>
            {
                if (_options.ConfigurationString != newValue.ConfigurationString)
                {
                    _logger.LogInformation("Options ConfigurationString change");

                    IConnectionMultiplexer tempObject;

                    lock (_connectionMultiplexerLocker)
                    {
                        if (_connectionMultiplexer != null)
                        {
                            tempObject = _connectionMultiplexer;
                            _connectionMultiplexer = null;
                        }
                        else
                        {
                            tempObject = null;
                        }
                    }

                    if (tempObject != null && tempObject.IsConnected)
                    {
                        tempObject.Close();
                        tempObject.Dispose();
                    }

                }

                _options = newValue;
            });
        }

        public virtual IDatabase GetDatabase()
        {
            return ConnectionMultiplexer.GetDatabase(-1, new object());
        }
    }
}
