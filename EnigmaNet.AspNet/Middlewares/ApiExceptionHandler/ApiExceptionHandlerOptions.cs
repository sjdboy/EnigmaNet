using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EnigmaNet.AspNet.Middlewares.ApiExceptionHandler
{
    public class ApiExceptionHandlerOptions
    {
        /// <summary>
        /// Api路径前缀
        /// </summary>
        public List<string> ApiPathPrefixs { get; set; }
    }
}
