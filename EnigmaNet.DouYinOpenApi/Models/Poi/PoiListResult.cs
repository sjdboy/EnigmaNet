using System;
using System.Collections.Generic;
using System.Text;

namespace EnigmaNet.DouYinOpenApi.Models.Poi
{
    public class PoiListResult
    {
        public long Cursor { get; set; }
        public bool HasMore { get; set; }
        public List<PoiModel> List { get; set; }
    }
}
