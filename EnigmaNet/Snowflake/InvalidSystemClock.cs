﻿using System;
using System.Collections.Generic;
using System.Text;

namespace EnigmaNet.Snowflake
{
    public class InvalidSystemClock : Exception
    {
        public InvalidSystemClock(string message) : base(message) { }
    }
}
