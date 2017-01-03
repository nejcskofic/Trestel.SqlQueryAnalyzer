// Copyright (c) Nejc Skofic. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Trestel.SqlQueryAnalyzer.Helpers
{
    /// <summary>
    /// Helper functions for running async code.
    /// </summary>
    internal static class AsyncHelper
    {
        private static readonly TaskFactory _taskFactory = new TaskFactory(
            CancellationToken.None,
            TaskCreationOptions.None,
            TaskContinuationOptions.None,
            TaskScheduler.Default);

        /// <summary>
        /// Runs the aync code synchronously.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="func">The function.</param>
        /// <returns>Returns result of async code.</returns>
        public static TResult RunSync<TResult>(Func<Task<TResult>> func)
        {
            return _taskFactory
              .StartNew<Task<TResult>>(func)
              .Unwrap<TResult>()
              .GetAwaiter()
              .GetResult();
        }

        /// <summary>
        /// Runs the async code synchronously.
        /// </summary>
        /// <param name="func">The function.</param>
        public static void RunSync(Func<Task> func)
        {
            _taskFactory
              .StartNew<Task>(func)
              .Unwrap()
              .GetAwaiter()
              .GetResult();
        }
    }
}
