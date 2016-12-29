using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Templates
{
    internal static class SourceCodeTemplates
    {
        public const string SimpleTemplate = @"
using System;
using Trestel.Database;
using Trestel.Database.Design;

[assembly: DatabaseHint(@""Data Source=.\SQLEXPRESS2014;Initial Catalog=AdventureWorks2014;Integrated Security=True;"")]

namespace TestNamespace
{{
    class Program
    {{
        static void Main(string[] args)
        {{
            var sql = Sql.From(""{0}"");
            Console.WriteLine(sql);
        }}
    }}
}}
";

        public const string TemplateWithResultMapping = @"
using System;
using System.Collections.Generic;
using Trestel.Database;
using Trestel.Database.Design;

[assembly: DatabaseHint(@""Data Source =.\SQLEXPRESS2014; Initial Catalog = AdventureWorks2014; Integrated Security = True;"")]

namespace TestNamespace
{{
    class Program
    {{
        static void Main(string[] args)
        {{
            foreach (var item in QueryMethodPlaceholder<{1}>(Sql.From(""{0}"")))
            {{
                Console.WriteLine(item);
            }}
        }}

        public IEnumerable<T> QueryMethodPlaceholder<T>(string query)
        {{
            return Enumerable.Empty<T>();
        }}
    }}

    {2}
}}
";

        public static string GetSourceCodeFromSimpleTemplate(string rawSqlQuery)
        {
            if (String.IsNullOrEmpty(rawSqlQuery)) throw new ArgumentException("Argument is null or empty.", nameof(rawSqlQuery));

            return String.Format(CultureInfo.InvariantCulture, SimpleTemplate, EscapeQueryForInclusionInSource(rawSqlQuery));
        }

        public static string GetSourceCodeFromTemplateWithResultMapping(string rawSqlQuery, string returnType, string additionalClass = "")
        {
            if (String.IsNullOrEmpty(rawSqlQuery)) throw new ArgumentException("Argument is null or empty.", nameof(rawSqlQuery));
            if (String.IsNullOrEmpty(returnType)) throw new ArgumentException("Argument is null or empty.", nameof(rawSqlQuery));

            return String.Format(CultureInfo.InvariantCulture, TemplateWithResultMapping, EscapeQueryForInclusionInSource(rawSqlQuery), returnType, additionalClass ?? "");
        }

        private static string EscapeQueryForInclusionInSource(string query)
        {
            return query.Replace("\"", "\\\"\"");
        }
    }
}
