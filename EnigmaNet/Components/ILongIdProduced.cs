using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EnigmaNet.Components
{
    /// <summary>
    /// 长整数id生成
    /// </summary>
    public interface ILongIdProduced
    {
        /// <summary>
        /// 生成Id
        /// </summary>
        /// <returns></returns>
        Task<long> GenerateIdAsync();
    }
}
