﻿// Copyright (c) Nejc Skofic. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.

using Trestel.SqlQueryAnalyzer.Common;

namespace Trestel.SqlQueryAnalyzer.Infrastructure.CallSiteAnalysis
{
    /// <summary>
    /// Represents call site analyzer responsible for normalizing call site information between different providers.
    /// </summary>
    public interface ICallSiteAnalyzer
    {
        /// <summary>
        /// Determines whether this instance can analyze call site given the specified context.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>
        ///   <c>true</c> if this instance can analyze call site; otherwise, <c>false</c>.
        /// </returns>
        bool CanAnalyzeCallSite(CallSiteContext context);

        /// <summary>
        /// Analyzes the call site.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>Result of call site normalization</returns>
        Result<NormalizedCallSite> AnalyzeCallSite(CallSiteContext context);
    }
}