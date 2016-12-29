using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trestel.SqlQueryAnalyzer.Design;
using Trestel.SqlQueryAnalyzer.Providers.SqlServer;

namespace Trestel.SqlQueryAnalyzer.Infrastructure
{
    internal static class ServiceFactoryExtensions
    {
        public static ServiceFactory BuildUp(this ServiceFactory serviceFactory)
        {
            if (serviceFactory == null) return null;

            serviceFactory.RegisterQueryValidationProviderFactory(DatabaseType.SqlServer, (string connectionString) => new SqlServerQueryValidationProvider(connectionString));
            return serviceFactory;
        }
    }
}
