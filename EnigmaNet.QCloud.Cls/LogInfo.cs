using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace EnigmaNet.QCloud.Cls
{
    public class LogInfo
    {
        public string CategoryName { get; set; }
        public LogLevel LogLevel { get; set; }
        public EventId EventId { get; set; }
        public int ThreadId { get; set; }
        public string Content { get; set; }
        public Exception Exception { get; set; }
        public DateTime DateTime { get; set; }
        public string TraceId { get; set; }
        public string SpanId { get; set; }
        public string ParentId { get; set; }
    }
}
