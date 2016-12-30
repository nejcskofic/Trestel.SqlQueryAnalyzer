// Copyright (c) Nejc Skofic. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trestel.SqlQueryAnalyzer.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="IEnumerable{T}"/>.
    /// </summary>
    internal static class EnumerableExtensions
    {
        /// <summary>
        /// Performs full join over two enumerables.
        /// </summary>
        /// <typeparam name="TOuter">The type of the outer.</typeparam>
        /// <typeparam name="TInner">The type of the inner.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="outer">The outer.</param>
        /// <param name="inner">The inner.</param>
        /// <param name="outerKeySelector">The outer key selector.</param>
        /// <param name="innerKeySelector">The inner key selector.</param>
        /// <param name="resultSelector">The result selector.</param>
        /// <returns>Result of full join</returns>
        /// <exception cref="System.ArgumentNullException">
        /// outer
        /// or
        /// inner
        /// </exception>
        public static IEnumerable<TResult> FullJoin<TOuter, TInner, TKey, TResult>(this IEnumerable<TOuter> outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, TInner, TResult> resultSelector)
            where TKey : IComparable<TKey>
        {
            // TODO revise
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
