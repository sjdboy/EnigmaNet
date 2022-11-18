using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnigmaNet.FeiShuOpenApi.Models
{
    public class PagedList<T> where T:class
    {
        public bool HasMore { get; set; }
        public string PageToken { get; set; }
        public List<T> Items { get; set; }
    }
}
