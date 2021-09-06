using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnigmaNet.ElasticSearch.Options
{
    public class EsOptions
    {
        public string[] Urls { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string DefaultIndexName { get; set; }
    }
}
