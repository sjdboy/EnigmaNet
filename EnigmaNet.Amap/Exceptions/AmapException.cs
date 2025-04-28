using System;

namespace EnigmaNet.Amap.Exceptions;

public class AmapException : Exception
{
    public AmapException(string message) : base(message)
    {
    }

    public AmapException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
