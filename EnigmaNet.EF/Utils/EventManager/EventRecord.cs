using System;
using System.Collections.Generic;
using System.Text;

namespace EnigmaNet.EF.Utils.EventManager
{
    public class EventRecord
    {
        public byte[] RowVer { get; set; }
        public long Id { get; set; }
        public string EventObjectJson { get; set; }
        public bool Processed { get; set; }
        public int ErrorTimes { get; set; }
        public DateTime ProcessDateTime { get; set; }
        public DateTime DateTime { get; set; }
    }
}
