using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if ANALYZER
namespace Trestel.SqlQueryAnalyzer.Design
#else
namespace Trestel.Database.Design
#endif
{
    public enum DatabaseType
    {
        SqlServer
    }
}
