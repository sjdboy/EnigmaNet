using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Text;

namespace EnigmaNet.AspNet.Utils
{
    public class SuccessIdentity : IIdentity
    {
        public SuccessIdentity(string name, string authenticationType)
        {
            AuthenticationType = authenticationType;
            Name = name;
        }

        public string AuthenticationType { get; }

        public bool IsAuthenticated => true;

        public string Name { get; }
    }
}
