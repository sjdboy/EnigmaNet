using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EnigmaNet.ChinaDivisions.Pg
{
    public class AreaEntity
    {
        public long Code { get; set; }
        public required string Name { get; set; }
        public RegionLevel Level { get; set; }
        public long ParentCode { get; set; }
        public int Category { get; set; }
    }
}