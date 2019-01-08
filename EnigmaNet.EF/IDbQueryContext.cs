using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnigmaNet.EF
{
    /// <summary>
    /// 数据查询上下文
    /// </summary>
    public interface IDbQueryContext : IDisposable
    {
        /// <summary>
        /// 查询实体
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        IQueryable<T> Query<T>() where T : class;
    }
}
