using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trestel.SqlQueryAnalyzer.Infrastructure
{
    // TODO: Can cache and introduce caching
    public interface IQueryValidationProvider
    {
        ValidationResult Validate(string rawSqlQuery);
    }
}
