using System;
using System.Collections.Generic;
using System.Text;

namespace EnigmaNet.EF.Utils.LongIds
{
    public class IdRecord
    {
        public byte[] RowVer { get; set; }
        public string Key { get; set; }
        public long Value { get; set; }
    }
}
