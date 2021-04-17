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
        IConnectionMultiplexer _connectionMultiplexer;
        object _connectionMultiplexerLocker = new object();
        string _configurationString;

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
                            _logger.LogInformation("init ConnectionMultiplexer");
                            _connectionMultiplexer = StackExchange.Redis.ConnectionMultiplexer.Connect(_configurationString);
                            _connectionMultiplexer.ConfigurationChangedBroadcast += _connectionMultiplexer_ConfigurationChangedBroadcast;
                            _connectionMultiplexer.ErrorMessage += _connectionMultiplexer_ErrorMessage;
                            _connectionMultiplexer.ConnectionFailed += _connectionMultiplexer_ConnectionFailed;
                            _connectionMultiplexer.InternalError += _connectionMultiplexer_InternalError;
                            _connectionMultiplexer.ConnectionRestored += _connectionMultiplexer_ConnectionRestored;
                            _connectionMultiplexer.ConfigurationChanged += _connectionMultiplexer_ConfigurationChanged;
                            _connectionMultiplexer.HashSlotMoved += _connectionMultiplexer_HashSlotMoved;
                        }
                    }
                }

                return _connectionMultiplexer;
            }
        }

        ILogger _logger;

        void _connectionMultiplexer_HashSlotMoved(object sender, HashSlotMovedEventArgs e)
        {
            _logger?.LogInformation($"HashSlotMoved,HashSlot:{e.HashSlot} OldEndPoint:{e.OldEndPoint} NewEndPoint:{e.NewEndPoint}");
        }

        void _connectionMultiplexer_ConnectionRestored(object sender, ConnectionFailedEventArgs e)
        {
            _logger?.LogError(e.Exception, $"ConnectionRestored,ConnectionType:{e.ConnectionType} EndPoint:{e.EndPoint} FailureType:{e.FailureType} Exception:{e.Exception.Message}");
        }

        void _connectionMultiplexer_InternalError(object sender, InternalErrorEventArgs e)
        {
            _logger?.LogError(e.Exception, $"InternalError,ConnectionType:{e.ConnectionType} EndPoint:{e.EndPoint} Origin:{e.Origin} Exception:{e.Exception.Message}");
        }

        void _connectionMultiplexer_ConnectionFailed(object sender, ConnectionFailedEventArgs e)
        {
            _logger?.LogError(e.Exception, $"ConnectionFailed,ConnectionType:{e.ConnectionType} EndPoint:{e.EndPoint} FailureType:{e.FailureType} Exception:{e.Exception.Message}");
        }

        void _connectionMultiplexer_ErrorMessage(object sender, RedisErrorEventArgs e)
        {
            _logger?.LogError($"ErrorMessage,EndPoint:{e.EndPoint} Message:{e.Message}");
        }

        void _connectionMultiplexer_ConfigurationChangedBroadcast(object sender, EndPointEventArgs e)
        {
            _logger?.LogInformation($"ConfigurationChangedBroadcast,EndPoint:{e.EndPoint}");
        }

        void _connectionMultiplexer_ConfigurationChanged(object sender, EndPointEventArgs e)
        {
            _logger?.LogInformation($"ConfigurationChanged,EndPoint:{e.EndPoint}");
        }

        public virtual string ConfigurationString
        {
            set
            {
                IConnectionMultiplexer tempObject;

                lock (_connectionMultiplexerLocker)
                {
                    _configurationString = value;
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
        }

        public RedisFactory(ILogger<RedisFactory> logger, IOptionsMonitor<Options.RedisOptions> options)
        {
            _logger = logger;

            if (options != null)
            {
                ConfigurationString = options.CurrentValue.ConfigurationString;

                options.OnChange(newValue =>
                {
                    ConfigurationString = newValue.ConfigurationString;
                });
            }
        }

        public virtual IDatabase GetDatabase()
        {
            return ConnectionMultiplexer.GetDatabase(-1, new object());
        }
    }
}
