using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BSU.Core.Concurrency
{
    public static class LinqHelper
    {
        public static async Task<IEnumerable<T>> WhereAsync<T>(this IEnumerable<T> enumerable, Func<T, Task<bool>> predicate)
        {
            var tasks = enumerable.Select(async t => (t, await predicate(t))).ToList();
            await Task.WhenAll(tasks);
            return tasks.Where(t => t.Result.Item2).Select(t => t.Result.Item1);
        }

        public static async Task<IEnumerable<T2>> SelectAsync<T1, T2>(this IEnumerable<T1> enumerable, Func<T1, Task<T2>> selector)
        {
            var tasks = enumerable.Select(async t =>  await selector(t)).ToList();
            await Task.WhenAll(tasks);
            return tasks.Select(t => t.Result);
        }
    }
}
