using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trestel.Database
{
    public sealed class Sql
    {
        private readonly string _sqlString;

        private Sql(string sqlString)
        {
            _sqlString = sqlString;
        }

        public string SqlString
        {
            get
            {
                return _sqlString;
            }
        }

        public static Sql From(string sqlString)
        {
            if (String.IsNullOrEmpty(sqlString))
            {
                throw new ArgumentNullException(nameof(sqlString));
            }
            return new Sql(sqlString);
        }

        public static implicit operator string(Sql sql)
        {
            return sql?.SqlString;
        }

        public override string ToString()
        {
            return this;
        }
    }
}
