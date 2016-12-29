using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trestel.SqlQueryAnalyzer.Extensions
{
    internal static class EnumerableExtensions
    {
        // TODO revise
        public static IEnumerable<TResult> FullJoin<TOuter, TInner, TKey, TResult>(this IEnumerable<TOuter> outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, TInner, TResult> resultSelector) where TKey : IComparable<TKey>
        {
            if (outer == null) throw new ArgumentNullException(nameof(outer));
            if (inner == null) throw new ArgumentNullException(nameof(inner));

            var innerEnumerator = inner.OrderBy(innerKeySelector).GetEnumerator();
            bool isInnerValid = innerEnumerator.MoveNext();
            foreach (var el in outer.OrderBy(outerKeySelector))
            {
                if (!isInnerValid || outerKeySelector(el).CompareTo(innerKeySelector(innerEnumerator.Current)) < 0)
                {
                    yield return resultSelector(el, default(TInner));
                    continue;
                }

                while (isInnerValid && outerKeySelector(el).CompareTo(innerKeySelector(innerEnumerator.Current)) > 0)
                {
                    var r = resultSelector(default(TOuter), innerEnumerator.Current);
                    isInnerValid = innerEnumerator.MoveNext();
                    yield return r;
                }

                if (!isInnerValid)
                {
                    yield return resultSelector(el, default(TInner));
                }
                else
                {
                    var t = resultSelector(el, innerEnumerator.Current);
                    isInnerValid = innerEnumerator.MoveNext();
                    yield return t;
                }
            }

            while (isInnerValid)
            {
                var r = resultSelector(default(TOuter), innerEnumerator.Current);
                isInnerValid = innerEnumerator.MoveNext();
                yield return r;
            }
        }
    }
}
