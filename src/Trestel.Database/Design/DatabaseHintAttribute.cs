using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trestel.Database.Design
{
    [Conditional("DatabaseHintAttribute")]
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
    public sealed class DatabaseHintAttribute : Attribute
    {
        public DatabaseHintAttribute(string connectionString) : this(connectionString, DatabaseType.SqlServer)
        {
        }

        public DatabaseHintAttribute(string connectionString, DatabaseType databaseType)
        {
            ConnectionString = connectionString;
            DatabaseType = databaseType;
        }

        public string ConnectionString
        {
            get;
        }

        public DatabaseType DatabaseType
        {
            get;
        }
    }
}
