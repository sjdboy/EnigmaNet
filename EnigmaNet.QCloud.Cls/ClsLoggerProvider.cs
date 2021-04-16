using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnigmaNet.QCloud.Cls
{
    class ClsLoggerProvider : ILoggerProvider
    {
        const string Default = "Default";
        ClsWriter _writer;
        LoggingOptions _loggingOptions;
        Dictionary<string, LogLevel> _logLevels;
        LogLevel _defaultLogLevel;
        ConcurrentDictionary<string, ClsLogger> _loggers = new ConcurrentDictionary<string, ClsLogger>();

        bool CheckLogLevel(string categoryName, LogLevel logLevel)
        {
            var logLevel2 = GetLogLevel(categoryName);

            return logLevel >= logLevel2;
        }

        LogLevel GetLogLevel(string categoryName)
        {
            if (_logLevels.ContainsKey(categoryName))
            {
                return _logLevels[categoryName];
            }

            var item = _logLevels.FirstOrDefault(m => categoryName.StartsWith(m.Key)).Key;
            if (item != null)
            {
                return _logLevels[item];
            }


            return _defaultLogLevel;
        }

        void TurnLoggerLevels()
        {
            _logLevels = new Dictionary<string, LogLevel>();
            foreach (var key in _loggingOptions.LogLevel.Keys)
            {
                var value = _loggingOptions.LogLevel[key];
                if(Enum.TryParse<LogLevel>(value,out var logLevel))
                {
                    _logLevels.Add(key, logLevel);
                }
            }

            if (_logLevels.ContainsKey(Default))
            {
                _defaultLogLevel = _logLevels[Default];
            }
            else
            {
                _defaultLogLevel = LogLevel.Information;
            }
        }

        public ClsLoggerProvider(ClsWriter writer, IOptionsMonitor<LoggingOptions> options)
        {
            _writer = writer;

            _loggingOptions = options.CurrentValue;
            TurnLoggerLevels();

            options.OnChange(newValue =>
            {
                _loggingOptions = newValue;
                TurnLoggerLevels();
            });
        }

        public ILogger CreateLogger(string categoryName)
        {
            return _loggers.GetOrAdd(categoryName, name =>
            {
                return new ClsLogger(name, _writer, logLevel => { return CheckLogLevel(name, logLevel); });
            });
        }

        public void Dispose()
        {
        }
    }
}
