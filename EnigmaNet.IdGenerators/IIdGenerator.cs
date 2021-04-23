using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnigmaNet.IdGenerators
{
    public interface IIdGenerator<T>
    {
        Task<T> GenerateIdAsync();
    }
}
