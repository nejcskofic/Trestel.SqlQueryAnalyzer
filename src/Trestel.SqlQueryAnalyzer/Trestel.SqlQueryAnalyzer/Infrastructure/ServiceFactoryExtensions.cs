// Copyright (c) Nejc Skofic. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        /// Builds up service factory. Responsible for initializing all services that analyzer is using.
        /// </summary>
        /// <param name="serviceFactory">The service factory.</param>
        /// <returns>Same instance as is received as parameter.</returns>
        public static ServiceFactory BuildUp(this ServiceFactory serviceFactory)
        {
            if (serviceFactory == null) return null;

            serviceFactory.RegisterQueryValidationProviderFactory(DatabaseType.SqlServer, (string connectionString) => new SqlServerQueryValidationProvider(connectionString));
            return serviceFactory;
        }
    }
}
