﻿using System;
using System.Collections.Generic;
using System.Text;

namespace EnigmaNet.DouYinOpenApi.Models.OAuth
{
    public class AccessTokenResult
    {
        public string AccessToken { get; set; }
        public int ExpiresSeconds { get; set; }
        public string RefreshToken { get; set; }
        public int RefreshExpiresSeconds { get; set; }
        public string OpenId { get; set; }
        public string[] Scopes { get; set; }
    }
}
