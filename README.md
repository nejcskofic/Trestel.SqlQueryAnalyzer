# Trestel.SqlQueryAnalyzer
[![NuGet](https://img.shields.io/nuget/v/Trestel.SqlQueryAnalyzer.svg)](https://www.nuget.org/packages/Trestel.SqlQueryAnalyzer/)
[![AppVeyor branch](https://img.shields.io/appveyor/ci/nejcskofic/trestel-sqlqueryanalyzer/master.svg)](https://ci.appveyor.com/project/nejcskofic/trestel-sqlqueryanalyzer)

Library for static analysis of raw SQL queries in C# code built upon .NET compiler platform (aka Roslyn).

## Supported features
Currently supported features are as follows:
* Syntax analysis of SQL query
* Analysis of provided/expected parameters
 * Currently implemented only for [Dapper](https://github.com/StackExchange/dapper-dot-net) functions
* Analysis of expected/provided result set

Currently analyzer works only with MSSQL Server 2012 or newer since it relies on data management functions and stored procedures to provide required information. As such there are some limitations which constructs can be present in queries. See [Wiki](https://github.com/nejcskofic/Trestel.SqlQueryAnalyzer/wiki) for more details.

There are currently no code fix providers. See [Wiki](https://github.com/nejcskofic/Trestel.SqlQueryAnalyzer/wiki) for more information.

## Usage 
Installing NuGet package will add two libraries:
* **Trestel.Database** as project reference which contains utility types to guide analysis
* **Trestel.SqlQueryAnalyzer** as analyzer reference which performs raw query analysis

First define your design time connection string:
```C#
using Trestel.Database.Design;

[assembly: DatabaseHint(@"Data Source=.\SQL2016;Initial Catalog=AdventureWorks2014;Integrated Security=True;")]
```

You can specify this attribute as assembly attribute, class attribute or method attribute. Analyzer will take first one that is found working up from containing method. For example:
```C#
using Trestel.Database.Design;

[assembly: DatabaseHint(@"Data Source=.\SQL2016;Initial Catalog=AdventureWorks2014;Integrated Security=True;")]

namespace Test
{
    class Program
    {
        [DatabaseHint(@"Data Source=.\SQL2014;Initial Catalog=AdventureWorks2014;Integrated Security=True;")]
        static void Main(string[] args)
        {
        }
    }
}
```
Even though there is _DatabaseHint_ attribute specified on assembly level to connect to SQL2016 instance, analyzer will connect to SQL2014 instance for all checks inside _Main_ method.

After you define design time connection string you wrap raw SQL queries inside _SQL.From_ method to mark it as SQL query:
```C#
using Trestel.Database;

...
var query = SQL.From("SELECT BusinessEntityID, Title, FirstName, MiddleName, LastName, ModifiedDate FROM Person.Person");
```

Analyzer will perform static analysis for all queries that are wrapped in _SQL.From_.

If you want to perform parameter and result set analysis as well, you would have to use query directly as function/method argument:
```C#
using (var connection = new SqlConnection("<runtime connection string>"))
{
    var name = connection.ExecuteScalar<string>(
      Sql.From("SELECT FirstName FROM Person.Person WHERE BusinessEntityID = @p1"), 
      new { p1 = 1 });
    Console.WriteLine(name);
}
```
In this case analyzer will check that all required parameters are present and match by type as well as that result set matches one declared in code.

## Possible diagnostics
Sub category | Code | Description
-------------|------|------------
General errors | SQLA101 | SQL query analyzer threw error. Exact error is displayed.
 | SQLA102 | Parameter inside _SQL.From_ is not string literal and analysis cannot continue.
 | SQLA103 | Missing _DatabaseHint_ attribute and analysis cannot continue.
SQL query errors | SQLA201 | There are syntax errors or objects don't exist. Exact error is displayed.
Parameter mapping errors | SQLA301 | Input parameter types do not match. Exact error is displayed.
 | SQLA302 | Input parameter is missing. Exact error is displayed.
 | SQLA303 | Input parameter is unused. Exact error is displayed.
Result set mapping errors | SQLA401 | One or more columns are not returned by SQL query but are expected in code. Exact error is displayed.
 | SQLA402 | One or more columns are returned by SQL query but are not used in code. Exact error is displayed.
 | SQLA403 | One or more columns have mismatched types (for POCO entities). Exact error is displayed.
 | SQLA404 | Type does not match (for single column result set which are mapped to simple type). Exact error is displayed.
 | SQLA405 | Code expects single column result but query returns multiple columns. Exact error is displayed.

## License
MIT

