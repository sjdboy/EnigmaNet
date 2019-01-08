using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnigmaNet.Extensions
{
    public static class QueryableExtensions
    {
        /// <summary>
        /// 获取分页集合
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="pageSize">一页存放的记录数</param>
        /// <param name="pageIndex">页号（从0开始）</param>
        /// <returns></returns>
        public static IQueryable<T> GetPageList<T>(this IQueryable<T> source, int pageSize, int pageIndex)
        {
            return source.Skip(pageIndex * pageSize).Take(pageSize);
        }
    }
}
