using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace EnigmaNet.QCloud.Cls
{
    class ClsLogger : ILogger
    {
        string _categoryName;
        Func<LogLevel, bool> _checkEnableFunc;
        ClsWriter _writer;

        public ClsLogger(string categoryName, ClsWriter writer, Func<LogLevel, bool> checkEnableFunc)
        {
            _categoryName = categoryName;
            _writer = writer;
            _checkEnableFunc = checkEnableFunc;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return _checkEnableFunc(logLevel);
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            var activity = Activity.Current;

            var spanId = activity?.IdFormat switch
            {
                ActivityIdFormat.Hierarchical => activity.Id,
                ActivityIdFormat.W3C => activity.SpanId.ToHexString(),
                _ => null,
            };

            var traceId = activity?.IdFormat switch
            {
                ActivityIdFormat.Hierarchical => activity.RootId,
                ActivityIdFormat.W3C => activity.TraceId.ToHexString(),
                _ => null
            };

            var parentId = activity?.IdFormat switch
            {
                ActivityIdFormat.Hierarchical => activity.ParentId,
                ActivityIdFormat.W3C => activity.ParentSpanId.ToHexString(),
                _ => null
            };

            var info = new LogInfo
            {
                CategoryName = _categoryName,
                DateTime = DateTime.Now,
                Content = formatter(state, exception),
                Exception = exception,
                LogLevel = logLevel,
                EventId = eventId,
                ThreadId = Thread.CurrentThread.ManagedThreadId,
                TraceId = traceId,
                SpanId = spanId,
                ParentId = parentId,
            };

            _writer.AddLogInfo(info);
        }
    }
}
