using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace EnigmaNet.EF
{
    public interface IDbContextFactory
    {
        DbContext Create();

        DbContext CreateWithLazyLoading();
    }
}
