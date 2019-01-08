using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;

using StackExchange.Redis;

namespace EnigmaNet.Redis.Impl
{
    public class RedisFactory : IRedisFactory
    {
        ConnectionMultiplexer _connectionMultiplexer;
        object _connectionMultiplexerLocker = new object();
        string _configurationString;
        ILogger _logger;

        ConnectionMultiplexer ConnectionMultiplexer
        {
            get
            {
                if (_connectionMultiplexer == null)
                {
                    lock (_connectionMultiplexerLocker)
                    {
                        if (_connectionMultiplexer == null)
                        {
                            Logger.LogInformation("init ConnectionMultiplexer");
                            _connectionMultiplexer = ConnectionMultiplexer.Connect(_configurationString);
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

        ILogger Logger
        {
            get
            {
                if (_logger == null)
                {
                    _logger = LoggerFactory.CreateLogger<RedisFactory>();
                }

                return _logger;
            }
        }

        private void _connectionMultiplexer_HashSlotMoved(object sender, HashSlotMovedEventArgs e)
        {
            Logger?.LogInformation($"HashSlotMoved,HashSlot:{e.HashSlot} OldEndPoint:{e.OldEndPoint} NewEndPoint:{e.NewEndPoint}");
        }

        private void _connectionMultiplexer_ConnectionRestored(object sender, ConnectionFailedEventArgs e)
        {
            Logger?.LogError(e.Exception, $"ConnectionRestored,ConnectionType:{e.ConnectionType} EndPoint:{e.EndPoint} FailureType:{e.FailureType} Exception:{e.Exception.Message}");
        }

        private void _connectionMultiplexer_InternalError(object sender, InternalErrorEventArgs e)
        {
            Logger?.LogError(e.Exception, $"InternalError,ConnectionType:{e.ConnectionType} EndPoint:{e.EndPoint} Origin:{e.Origin} Exception:{e.Exception.Message}");
        }

        private void _connectionMultiplexer_ConnectionFailed(object sender, ConnectionFailedEventArgs e)
        {
            Logger?.LogError(e.Exception, $"ConnectionFailed,ConnectionType:{e.ConnectionType} EndPoint:{e.EndPoint} FailureType:{e.FailureType} Exception:{e.Exception.Message}");
        }

        private void _connectionMultiplexer_ErrorMessage(object sender, RedisErrorEventArgs e)
        {
            Logger?.LogError($"ErrorMessage,EndPoint:{e.EndPoint} Message:{e.Message}");
        }

        private void _connectionMultiplexer_ConfigurationChangedBroadcast(object sender, EndPointEventArgs e)
        {
            Logger?.LogInformation($"ConfigurationChangedBroadcast,EndPoint:{e.EndPoint}");
        }

        private void _connectionMultiplexer_ConfigurationChanged(object sender, EndPointEventArgs e)
        {
            Logger?.LogInformation($"ConfigurationChanged,EndPoint:{e.EndPoint}");
        }

        public ILoggerFactory LoggerFactory { get; set; }

        public string ConfigurationString { set { _configurationString = value; } }

        public IDatabase GetDatabase()
        {
            return ConnectionMultiplexer.GetDatabase(-1, new object());
        }
    }
}
