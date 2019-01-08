using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EnigmaNet.Components
{
    /// <summary>
    /// 整数id生成
    /// </summary>
    public interface IIntIdProduced
    {
        /// <summary>
        /// 生成Id
        /// </summary>
        /// <returns></returns>
        Task<int> GenerateIdAsync();
    }
}
