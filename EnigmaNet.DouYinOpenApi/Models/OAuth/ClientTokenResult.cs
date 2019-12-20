using System;
using System.Collections.Generic;
using System.Text;

namespace EnigmaNet.DouYinOpenApi.Models.OAuth
{
    public class ClientTokenResult
    {
        public string AccessToken { get; set; }
        public int ExpiresSeconds { get; set; }
    }
}
