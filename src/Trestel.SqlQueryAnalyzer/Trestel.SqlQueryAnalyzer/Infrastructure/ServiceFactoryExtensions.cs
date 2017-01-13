// Copyright (c) Nejc Skofic. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trestel.SqlQueryAnalyzer.CallSiteAnalyzers;
using Trestel.SqlQueryAnalyzer.Design;
using Trestel.SqlQueryAnalyzer.Providers.SqlServer;

namespace Trestel.SqlQueryAnalyzer.Infrastructure
{
    /// <summary>
    /// Extension methods for <see cref="ServiceFactory"/>.
    /// </summary>
    internal static class ServiceFactoryExtensions
    {
        /// <summary>
        /// Adds required services to factory builder. Responsible for initializing all services that analyzer is using.
        /// </summary>
        /// <param name="builder">The service factory builder.</param>
        /// <returns>Same instance as is received as parameter.</returns>
        public static ServiceFactory.Builder AddServices(this ServiceFactory.Builder builder)
        {
            if (builder == null) return null;

            builder.RegisterQueryValidationProviderFactory(DatabaseType.SqlServer, (string connectionString) => new SqlServerQueryValidationProvider(connectionString));

            builder.RegisterCallSiteAnalyzerInstance(new GenericAnalyzer());

            return builder;
        }
    }
}
