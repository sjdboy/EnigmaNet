using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnigmaNet.DouYinOpenApi.Models.DataExternals
{
    public class UserVideoDataItem
    {
        public DateTime Date { get; set; }
        public int NewIssue { get; set; }
        public int NewPlay { get; set; }
        public int TotalIssue { get; set; }
    }
}
