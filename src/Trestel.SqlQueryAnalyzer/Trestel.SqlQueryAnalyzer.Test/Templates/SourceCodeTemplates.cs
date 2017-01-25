// Copyright (c) Nejc Skofic. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.

using System;
using System.Globalization;

namespace Templates
{
    internal static class SourceCodeTemplates
    {
        public const string SimpleTemplate = @"
using System;
using System.Linq;
using Trestel.Database;
using Trestel.Database.Design;

[assembly: DatabaseHint(@""<connection string>"")]

namespace TestNamespace
{{
    class Program
    {{
        static void Main(string[] args)
        {{
            {0}
        }}
    }}

    {1}
}}
";

        public const string GenericQueryMethodTemplate = @"
using System;
using System.Collections.Generic;
using System.Linq;
using Trestel.Database;
using Trestel.Database.Design;

[assembly: DatabaseHint(@""<connection string>"")]

namespace TestNamespace
{{
    class Program
    {{
        static void Main(string[] args)
        {{
            {0}
        }}

        public static IEnumerable<T> GenericQueryMethod<T>(string query)
        {{
            return Enumerable.Empty<T>();
        }}
    }}

    {1}
}}
";

        public const string DapperTemplate = @"
using Dapper;
using System;
using System.Linq;
using System.Data.SqlClient;
using Trestel.Database;
using Trestel.Database.Design;

[assembly: DatabaseHint(@""<connection string>"")]

namespace TestNamespace
{{
    class Program
    {{
        static void Main(string[] args)
        {{
            {0}
        }}
    }}

    {1}
}}
";

        public const string DapperAsyncTemplate = @"
using Dapper;
using System;
using System.Linq;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Trestel.Database;
using Trestel.Database.Design;

[assembly: DatabaseHint(@""<connection string>"")]

namespace TestNamespace
{{
    class Program
    {{
        static void Main(string[] args)
        {{
            MainAsync();
        }}

        private static async Task MainAsync()
        {{
            {0}
        }}
    }}

    {1}
}}
";

        public static string GetSourceCodeFromSimpleTemplate(string mainMethodImplementation, string additionalClass = "")
        {
            if (String.IsNullOrEmpty(mainMethodImplementation)) throw new ArgumentException("Argument is null or empty.", nameof(mainMethodImplementation));

            return String.Format(CultureInfo.InvariantCulture, SimpleTemplate, mainMethodImplementation, additionalClass ?? "");
        }

        public static string GetSourceCodeFromGenericMethodTemplate(string mainMethodImplementation, string additionalClass = "")
        {
            if (String.IsNullOrEmpty(mainMethodImplementation)) throw new ArgumentException("Argument is null or empty.", nameof(mainMethodImplementation));

            return String.Format(CultureInfo.InvariantCulture, GenericQueryMethodTemplate, mainMethodImplementation, additionalClass ?? "");
        }

        public static string GetSourceCodeFromDapperTemplate(string mainMethodImplementation, string additionalClass = "", bool isAsync = false)
        {
            if (String.IsNullOrEmpty(mainMethodImplementation)) throw new ArgumentException("Argument is null or empty.", nameof(mainMethodImplementation));

            return String.Format(CultureInfo.InvariantCulture, isAsync ? DapperAsyncTemplate : DapperTemplate, mainMethodImplementation, additionalClass ?? "");
        }

        public static string EscapeQueryForInclusionInSource(string query)
        {
            return query?.Replace("\"", "\\\"\"");
        }
    }
}
