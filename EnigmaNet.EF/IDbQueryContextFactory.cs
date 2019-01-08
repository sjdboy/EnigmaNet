using System;
using System.Collections.Generic;
using System.Text;

namespace EnigmaNet.EF
{
    public interface IDbQueryContextFactory
    {
        IDbQueryContext Create();
        IDbQueryContext CreateWithLazyLoading();
    }
}
