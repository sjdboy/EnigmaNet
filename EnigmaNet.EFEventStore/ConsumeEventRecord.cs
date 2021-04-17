using System;
using System.Collections.Generic;
using System.Text;

namespace EnigmaNet.EF.Utils.EventManager
{
    public class ConsumeEventRecord
    {
        public byte[] RowVer { get; set; }
        public long Id { get; set; }
        public string EventId { get; set; }
        public string ConsumerId { get; set; }
        public DateTime DateTime { get; set; }
    }
}
